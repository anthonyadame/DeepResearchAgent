# Agent-Lightning State Management - Implementation Best Practices

## 🚀 Quick Start

### 1. Register Services

```csharp
// Program.cs
services.AddMemoryCache(options => 
    options.SizeLimit = 500 * 1024 * 1024  // 500 MB
);

services.AddSingleton<ILightningStateService>(provider =>
    new LightningStateService(
        provider.GetRequiredService<IAgentLightningService>(),
        provider.GetRequiredService<ILightningVERLService>(),
        provider.GetRequiredService<IMemoryCache>()
    )
);
```

### 2. Use in Workflow

```csharp
public class MasterWorkflow
{
    private readonly ILightningStateService _stateService;

    public async Task ExecuteAsync(string query, CancellationToken ct = default)
    {
        var researchId = Guid.NewGuid().ToString();
        
        // Initialize state
        var researchState = new ResearchStateModel
        {
            ResearchId = researchId,
            Query = query,
            Status = ResearchStatus.Pending,
            StartedAt = DateTime.UtcNow
        };
        
        await _stateService.SetResearchStateAsync(researchId, researchState, ct);

        try
        {
            // Update progress
            researchState.Status = ResearchStatus.InProgress;
            await _stateService.SetResearchStateAsync(researchId, researchState, ct);

            // Execute research
            // ... your logic here ...

            // Update final state
            researchState.Status = ResearchStatus.Completed;
            researchState.CompletedAt = DateTime.UtcNow;
            await _stateService.SetResearchStateAsync(researchId, researchState, ct);
        }
        catch (Exception ex)
        {
            researchState.Status = ResearchStatus.Failed;
            await _stateService.SetResearchStateAsync(researchId, researchState, ct);
            throw;
        }
    }
}
```

---

## 💡 Best Practices

### 1. State Consistency

✅ **DO:**
- Always verify state before persistence with VERL
- Use version control for optimistic concurrency
- Validate state transitions (e.g., can't go from Completed to InProgress)
- Use transactions for multi-step state updates

❌ **DON'T:**
- Bypass VERL validation
- Allow arbitrary state transitions
- Update multiple states without coordination
- Ignore version conflicts

```csharp
// Good: Verify before saving
public async Task SafeUpdateAsync(ResearchStateModel state)
{
    var verification = await _verlService.VerifyResultAsync(
        state.ResearchId,
        JsonSerializer.Serialize(state)
    );
    
    if (!verification.IsValid)
        throw new InvalidOperationException("State verification failed");
        
    await _stateService.SetResearchStateAsync(state.ResearchId, state);
}
```

### 2. Performance Optimization

✅ **DO:**
- Batch multiple state reads
- Use appropriate cache TTLs
- Monitor cache hit rates
- Implement cache warming for hot data

❌ **DON'T:**
- Read states in loops (N+1 problem)
- Cache everything indefinitely
- Ignore cache invalidation
- Overload memory cache

```csharp
// Good: Batch operations
var agentStates = await _stateService.GetMultipleAgentStatesAsync(ct, agentIds);

// Bad: N+1 queries
foreach (var agentId in agentIds)
{
    var state = await _stateService.GetAgentStateAsync(agentId, ct);
}
```

### 3. Error Handling

✅ **DO:**
- Handle specific exceptions
- Implement exponential backoff
- Log all state operation failures
- Provide meaningful error messages

❌ **DON'T:**
- Swallow exceptions silently
- Use generic error handling
- Retry indefinitely
- Expose internal errors to users

```csharp
// Good: Specific error handling
try
{
    await _stateService.SetAgentStateAsync(agentId, state, ct);
}
catch (InvalidOperationException ex) when (ex.Message.Contains("verification"))
{
    _logger.LogWarning(ex, "State verification failed for {AgentId}", agentId);
    // Handle verification failure
}
catch (OperationCanceledException)
{
    _logger.LogInformation("State operation cancelled");
    throw;
}
catch (Exception ex)
{
    _logger.LogError(ex, "Unexpected error setting agent state");
    throw;
}
```

### 4. Concurrency Control

✅ **DO:**
- Use pessimistic locking for critical sections
- Implement optimistic concurrency with versions
- Use CancellationToken properly
- Release locks in finally blocks

❌ **DON'T:**
- Ignore concurrency issues
- Hold locks too long
- Mix pessimistic and optimistic approaches
- Ignore cancellation requests

```csharp
// Good: Proper lock management
var lockObj = locks.GetOrAdd($"lock:{id}", _ => new SemaphoreSlim(1, 1));
await lockObj.WaitAsync(ct);
try
{
    // Critical section
}
finally
{
    lockObj.Release();
}
```

### 5. Monitoring & Alerting

✅ **DO:**
- Track cache hit rates
- Monitor operation latencies
- Alert on slow operations
- Log state transitions

❌ **DON'T:**
- Ignore performance metrics
- Log everything (too noisy)
- Set unrealistic thresholds
- Skip production monitoring

```csharp
// Good: Monitor metrics
var metrics = _stateService.GetMetrics();
if (metrics.CacheHitRate < 0.7)
{
    _logger.LogWarning("Low cache hit rate: {Rate}", metrics.CacheHitRate);
}

var avgDuration = metrics.GetAverageOperationDuration("SetAgentState");
if (avgDuration > 1000) // > 1 second
{
    _logger.LogWarning("Slow state operation: {Duration}ms", avgDuration);
}
```

---

## 🏗️ Architecture Patterns

### State Transition Machine

```csharp
public class StateTransitionValidator
{
    // Valid transitions for research states
    private static readonly Dictionary<ResearchStatus, HashSet<ResearchStatus>> ValidTransitions = new()
    {
        { ResearchStatus.Pending, new() { ResearchStatus.InProgress } },
        { ResearchStatus.InProgress, new() { ResearchStatus.Verifying, ResearchStatus.Failed } },
        { ResearchStatus.Verifying, new() { ResearchStatus.Completed, ResearchStatus.InProgress, ResearchStatus.Failed } },
        { ResearchStatus.Completed, new() { } },  // Terminal state
        { ResearchStatus.Failed, new() { } }      // Terminal state
    };

    public bool IsValidTransition(ResearchStatus from, ResearchStatus to)
    {
        return ValidTransitions.TryGetValue(from, out var validTargets) && 
               validTargets.Contains(to);
    }

    public void ValidateTransitionOrThrow(ResearchStatus from, ResearchStatus to)
    {
        if (!IsValidTransition(from, to))
            throw new InvalidOperationException(
                $"Invalid state transition from {from} to {to}"
            );
    }
}
```

### Snapshot & Recovery

```csharp
public class StateSnapshotManager
{
    private readonly ILightningStateService _stateService;

    // Create snapshot for disaster recovery
    public async Task<ApplicationStateSnapshot> CreateSnapshotAsync(CancellationToken ct = default)
    {
        return new ApplicationStateSnapshot
        {
            SnapshotId = Guid.NewGuid().ToString(),
            CapturedAt = DateTime.UtcNow,
            // ... populate from state service ...
        };
    }

    // Restore from snapshot
    public async Task RestoreFromSnapshotAsync(
        ApplicationStateSnapshot snapshot,
        CancellationToken ct = default)
    {
        foreach (var (agentId, state) in snapshot.AgentStates)
        {
            await _stateService.SetAgentStateAsync(agentId, state, ct);
        }

        foreach (var (researchId, state) in snapshot.ResearchStates)
        {
            await _stateService.SetResearchStateAsync(researchId, state, ct);
        }
    }
}
```

---

## 📊 Performance Characteristics

### Latency Targets

```
Operation                    Target p95    Typical
────────────────────────────────────────────────────
GetAgentState (cache hit)    <5ms          2-3ms
GetAgentState (cache miss)   <100ms        50-80ms
SetAgentState                <150ms        80-120ms
GetMultipleAgentStates(10)   <200ms        100-150ms
InvalidateCache              <50ms         20-30ms
```

### Throughput Targets

```
Sequential state operations:   > 1000 ops/sec
Concurrent state operations:   > 500 ops/sec (depends on locking)
Cache hit rate:               > 75% (with proper TTLs)
Memory overhead:              < 500MB for typical workloads
```

---

## 🔍 Testing

### Unit Test Pattern

```csharp
[Collection("State Management Tests")]
public class LightningStateServiceTests
{
    private readonly Mock<IAgentLightningService> _mockLightning;
    private readonly Mock<ILightningVERLService> _mockVerl;
    private readonly IMemoryCache _cache;
    private readonly LightningStateService _service;

    [Fact]
    public async Task SetAgentStateAsync_WithValidState_UpdatesCache()
    {
        // Arrange
        var state = new AgentStateModel { AgentId = "test" };
        _mockVerl.Setup(m => m.VerifyResultAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new VerificationResult { IsValid = true });

        // Act
        await _service.SetAgentStateAsync("test", state);

        // Assert
        var cached = await _service.GetAgentStateAsync("test");
        Assert.NotNull(cached);
    }

    [Fact]
    public async Task GetAgentStateAsync_WithInvalidVerification_Throws()
    {
        // Arrange
        _mockVerl.Setup(m => m.VerifyResultAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync(new VerificationResult { IsValid = false });

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => _service.SetAgentStateAsync("test", new AgentStateModel())
        );
    }
}
```

---

## 🛡️ Security Considerations

1. **Authentication**
   - Verify agent identity before state operations
   - Use credentials for Lightning Server communication

2. **Authorization**
   - Agents can only access/modify their own state
   - Implement role-based access control

3. **Data Protection**
   - Encrypt sensitive state data at rest
   - Use HTTPS for Lightning Server communication
   - Implement audit logging for state changes

4. **Input Validation**
   - Validate all state input before persistence
   - Prevent state injection attacks
   - Sanitize serialized data

---

## 📈 Monitoring Dashboard Metrics

```
Key Metrics to Track:
├── Cache Performance
│   ├── Hit Rate (target > 75%)
│   ├── Miss Rate
│   └── Memory Usage
├── Operation Performance
│   ├── p50, p95, p99 latencies
│   ├── Throughput (ops/sec)
│   └── Error rate
├── State Health
│   ├── Invalid state attempts
│   ├── Verification failures
│   └── Concurrent access conflicts
└── System Health
    ├── Memory pressure
    ├── Lock contention
    └── Active state operations
```

---

## 🔄 Migration Guide from Local State

If migrating from local state management:

1. **Parallel Run:** Keep both systems running initially
2. **Gradual Rollout:** Migrate workflows one at a time
3. **Data Migration:** Bulk-load existing state to Lightning Server
4. **Validation:** Verify state consistency before cutover
5. **Monitoring:** Track metrics closely during transition

---

## 🎯 Summary

Agent-Lightning state management provides:

✅ **Centralized State:** Single source of truth via Lightning Server
✅ **High Performance:** Multi-level caching with APO optimization
✅ **Consistency:** VERL validation and version control
✅ **Scalability:** Distributed caching and auto-scaling
✅ **Reliability:** Comprehensive error handling
✅ **Observability:** Built-in metrics and monitoring

---

**Version:** 1.0  
**Status:** Ready for Production  
**Last Updated:** 2024
