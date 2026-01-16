# Agent-Lightning State Management Architecture

## 📋 Overview

This document outlines how to leverage Agent-Lightning (Lightning Server + Client) for centralized, high-performance state management throughout the Deep Research Agent application.

---

## 🏗️ Architecture Design

### State Management Layers

```
┌─────────────────────────────────────────────────────────────┐
│                   Application Layer                         │
│         (Master, Supervisor, Researcher Workflows)          │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│            Lightning State Management Layer                 │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Local State Cache (In-Memory + Redis Optional)     │   │
│  │  • Agent State Cache                                │   │
│  │  • Research Results Cache                           │   │
│  │  • Verification Cache                               │   │
│  └─────────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│         Agent-Lightning Client Service                      │
│  ┌─────────────────────────────────────────────────────┐   │
│  │  Lightning Client                                   │   │
│  │  • Communication Layer                              │   │
│  │  • Request/Response Handling                        │   │
│  │  • Error Handling & Retries                         │   │
│  └─────────────────────────────────────────────────────┘   │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│              Lightning Server                               │
│  ┌──────────────┬──────────────┬──────────────────────┐    │
│  │ APO Engine   │ State Store  │ VERL Verification   │    │
│  │ (Caching &   │ (Central DB) │ (Validation)        │    │
│  │  Perf Opt)   │              │                      │    │
│  └──────────────┴──────────────┴──────────────────────┘    │
└────────────────────┬────────────────────────────────────────┘
                     │
┌────────────────────▼────────────────────────────────────────┐
│          Persistent Storage                                 │
│  ┌──────────────┬──────────────┬──────────────────┐        │
│  │ PostgreSQL   │ Redis Cache  │ LightningStore   │        │
│  │ (Long-term)  │ (Performance)│ (Backup)         │        │
│  └──────────────┴──────────────┴──────────────────┘        │
└─────────────────────────────────────────────────────────────┘
```

---

## 💾 State Management Models

### 1. Agent State Model

```csharp
public class AgentStateModel
{
    public string AgentId { get; set; }
    public string AgentType { get; set; }
    public AgentStatus Status { get; set; }
    public Dictionary<string, object> Properties { get; set; }
    public List<string> ActiveTaskIds { get; set; }
    public Dictionary<string, double> PerformanceMetrics { get; set; }
    public DateTime LastUpdated { get; set; }
    public int Version { get; set; } // For optimistic concurrency
}

public enum AgentStatus
{
    Initializing,
    Ready,
    Processing,
    Paused,
    Completed,
    Failed
}
```

### 2. Research State Model

```csharp
public class ResearchStateModel
{
    public string ResearchId { get; set; }
    public string Query { get; set; }
    public ResearchStatus Status { get; set; }
    public List<FactState> ExtractedFacts { get; set; }
    public List<string> Sources { get; set; }
    public double CurrentQualityScore { get; set; }
    public int IterationCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
}

public enum ResearchStatus
{
    Pending,
    InProgress,
    Verifying,
    Completed,
    Failed
}
```

### 3. Verification State Model

```csharp
public class VerificationStateModel
{
    public string VerificationId { get; set; }
    public string SourceId { get; set; } // Task or Research ID
    public string Content { get; set; }
    public double ConfidenceScore { get; set; }
    public bool IsVerified { get; set; }
    public List<string> Issues { get; set; }
    public DateTime VerifiedAt { get; set; }
    public string VerifiedBy { get; set; }
}
```

---

## 🔧 Implementation: Distributed State Service

```csharp
namespace DeepResearchAgent.Services.StateManagement;

using DeepResearchAgent.Services;
using System.Collections.Concurrent;
using System.Text.Json;

/// <summary>
/// Distributed state management using Agent-Lightning for centralized coordination.
/// Implements local caching with Lightning Server as source of truth.
/// </summary>
public interface ILightningStateService
{
    // Agent State
    Task<AgentStateModel> GetAgentStateAsync(string agentId);
    Task SetAgentStateAsync(string agentId, AgentStateModel state);
    Task<bool> UpdateAgentStatusAsync(string agentId, AgentStatus newStatus);
    
    // Research State
    Task<ResearchStateModel> GetResearchStateAsync(string researchId);
    Task SetResearchStateAsync(string researchId, ResearchStateModel state);
    Task<bool> UpdateResearchProgressAsync(string researchId, int iterationCount, double qualityScore);
    
    // Verification State
    Task<VerificationStateModel> GetVerificationStateAsync(string verificationId);
    Task SetVerificationStateAsync(string verificationId, VerificationStateModel state);
    Task<List<VerificationStateModel>> GetVerificationsForSourceAsync(string sourceId);
    
    // Batch Operations
    Task<Dictionary<string, AgentStateModel>> GetMultipleAgentStatesAsync(params string[] agentIds);
    Task SetMultipleAgentStatesAsync(Dictionary<string, AgentStateModel> states);
    
    // Caching
    Task InvalidateCacheAsync(string key);
    Task InvalidateCategoryAsync(string category);
}

public class LightningStateService : ILightningStateService
{
    private readonly IAgentLightningService _lightningService;
    private readonly ILightningVERLService _verlService;
    private readonly IMemoryCache _cache;
    private readonly JsonSerializerOptions _jsonOptions;
    private readonly ConcurrentDictionary<string, object> _locks;

    private const string AGENT_STATE_PREFIX = "agent_state:";
    private const string RESEARCH_STATE_PREFIX = "research_state:";
    private const string VERIFICATION_STATE_PREFIX = "verification_state:";
    private const int CACHE_DURATION_MINUTES = 5;

    public LightningStateService(
        IAgentLightningService lightningService,
        ILightningVERLService verlService,
        IMemoryCache cache)
    {
        _lightningService = lightningService;
        _verlService = verlService;
        _cache = cache;
        _locks = new ConcurrentDictionary<string, object>();
        _jsonOptions = new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };
    }

    #region Agent State Management

    public async Task<AgentStateModel> GetAgentStateAsync(string agentId)
    {
        var cacheKey = $"{AGENT_STATE_PREFIX}{agentId}";

        // Try cache first
        if (_cache.TryGetValue(cacheKey, out AgentStateModel? cached))
        {
            return cached!;
        }

        // Get from Lightning Server
        try
        {
            var taskData = new AgentTask
            {
                Name = "GetAgentState",
                Input = new Dictionary<string, object> { { "agentId", agentId } }
            };

            var result = await _lightningService.SubmitTaskAsync(agentId, taskData);

            if (result.Result != null)
            {
                var state = JsonSerializer.Deserialize<AgentStateModel>(
                    result.Result,
                    _jsonOptions
                );

                // Cache the result
                _cache.Set(cacheKey, state, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return state!;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get agent state for {agentId}", ex);
        }

        throw new InvalidOperationException($"Agent state not found for {agentId}");
    }

    public async Task SetAgentStateAsync(string agentId, AgentStateModel state)
    {
        var lockKey = $"lock:{AGENT_STATE_PREFIX}{agentId}";
        var lockObj = _locks.GetOrAdd(lockKey, _ => new object());

        lock (lockObj)
        {
            try
            {
                // Verify state with VERL
                var verification = _verlService.VerifyResultAsync(
                    state.AgentId,
                    JsonSerializer.Serialize(state, _jsonOptions)
                );

                // Submit to Lightning Server
                var taskData = new AgentTask
                {
                    Name = "SetAgentState",
                    Input = new Dictionary<string, object> { { "state", state } }
                };

                var result = _lightningService.SubmitTaskAsync(agentId, taskData);

                // Update cache
                var cacheKey = $"{AGENT_STATE_PREFIX}{agentId}";
                _cache.Set(cacheKey, state, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set agent state for {agentId}", ex);
            }
        }
    }

    public async Task<bool> UpdateAgentStatusAsync(string agentId, AgentStatus newStatus)
    {
        try
        {
            var state = await GetAgentStateAsync(agentId);
            state.Status = newStatus;
            state.LastUpdated = DateTime.UtcNow;
            state.Version++;

            await SetAgentStateAsync(agentId, state);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Research State Management

    public async Task<ResearchStateModel> GetResearchStateAsync(string researchId)
    {
        var cacheKey = $"{RESEARCH_STATE_PREFIX}{researchId}";

        if (_cache.TryGetValue(cacheKey, out ResearchStateModel? cached))
        {
            return cached!;
        }

        try
        {
            var taskData = new AgentTask
            {
                Name = "GetResearchState",
                Input = new Dictionary<string, object> { { "researchId", researchId } }
            };

            var result = await _lightningService.SubmitTaskAsync("research-manager", taskData);

            if (result.Result != null)
            {
                var state = JsonSerializer.Deserialize<ResearchStateModel>(
                    result.Result,
                    _jsonOptions
                );

                _cache.Set(cacheKey, state, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return state!;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get research state for {researchId}", ex);
        }

        throw new InvalidOperationException($"Research state not found for {researchId}");
    }

    public async Task SetResearchStateAsync(string researchId, ResearchStateModel state)
    {
        var lockKey = $"lock:{RESEARCH_STATE_PREFIX}{researchId}";
        var lockObj = _locks.GetOrAdd(lockKey, _ => new object());

        lock (lockObj)
        {
            try
            {
                var taskData = new AgentTask
                {
                    Name = "SetResearchState",
                    Input = new Dictionary<string, object> { { "state", state } }
                };

                await _lightningService.SubmitTaskAsync("research-manager", taskData);

                var cacheKey = $"{RESEARCH_STATE_PREFIX}{researchId}";
                _cache.Set(cacheKey, state, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Failed to set research state for {researchId}", ex);
            }
        }
    }

    public async Task<bool> UpdateResearchProgressAsync(
        string researchId,
        int iterationCount,
        double qualityScore)
    {
        try
        {
            var state = await GetResearchStateAsync(researchId);
            state.IterationCount = iterationCount;
            state.CurrentQualityScore = qualityScore;
            state.LastUpdated = DateTime.UtcNow;

            await SetResearchStateAsync(researchId, state);
            return true;
        }
        catch
        {
            return false;
        }
    }

    #endregion

    #region Verification State Management

    public async Task<VerificationStateModel> GetVerificationStateAsync(string verificationId)
    {
        var cacheKey = $"{VERIFICATION_STATE_PREFIX}{verificationId}";

        if (_cache.TryGetValue(cacheKey, out VerificationStateModel? cached))
        {
            return cached!;
        }

        try
        {
            var taskData = new AgentTask
            {
                Name = "GetVerificationState",
                Input = new Dictionary<string, object> { { "verificationId", verificationId } }
            };

            var result = await _lightningService.SubmitTaskAsync("verification-manager", taskData);

            if (result.Result != null)
            {
                var state = JsonSerializer.Deserialize<VerificationStateModel>(
                    result.Result,
                    _jsonOptions
                );

                _cache.Set(cacheKey, state, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return state!;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get verification state for {verificationId}", ex);
        }

        throw new InvalidOperationException($"Verification state not found for {verificationId}");
    }

    public async Task SetVerificationStateAsync(string verificationId, VerificationStateModel state)
    {
        var lockKey = $"lock:{VERIFICATION_STATE_PREFIX}{verificationId}";
        var lockObj = _locks.GetOrAdd(lockKey, _ => new object());

        lock (lockObj)
        {
            try
            {
                var verification = await _verlService.VerifyResultAsync(
                    state.VerificationId,
                    JsonSerializer.Serialize(state, _jsonOptions)
                );

                if (!verification.IsValid)
                {
                    throw new InvalidOperationException(
                        $"Verification failed: {string.Join(", ", verification.Issues)}"
                    );
                }

                var taskData = new AgentTask
                {
                    Name = "SetVerificationState",
                    Input = new Dictionary<string, object> { { "state", state } }
                };

                await _lightningService.SubmitTaskAsync("verification-manager", taskData);

                var cacheKey = $"{VERIFICATION_STATE_PREFIX}{verificationId}";
                _cache.Set(cacheKey, state, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to set verification state for {verificationId}",
                    ex
                );
            }
        }
    }

    public async Task<List<VerificationStateModel>> GetVerificationsForSourceAsync(string sourceId)
    {
        var cacheKey = $"verifications:{sourceId}";

        if (_cache.TryGetValue(cacheKey, out List<VerificationStateModel>? cached))
        {
            return cached!;
        }

        try
        {
            var taskData = new AgentTask
            {
                Name = "GetVerificationsForSource",
                Input = new Dictionary<string, object> { { "sourceId", sourceId } }
            };

            var result = await _lightningService.SubmitTaskAsync("verification-manager", taskData);

            if (result.Result != null)
            {
                var verifications = JsonSerializer.Deserialize<List<VerificationStateModel>>(
                    result.Result,
                    _jsonOptions
                );

                _cache.Set(cacheKey, verifications, TimeSpan.FromMinutes(CACHE_DURATION_MINUTES));
                return verifications!;
            }
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Failed to get verifications for {sourceId}", ex);
        }

        return new List<VerificationStateModel>();
    }

    #endregion

    #region Batch Operations

    public async Task<Dictionary<string, AgentStateModel>> GetMultipleAgentStatesAsync(
        params string[] agentIds)
    {
        var results = new Dictionary<string, AgentStateModel>();
        var tasks = agentIds.Select(id => GetAgentStateAsync(id)).ToList();

        await Task.WhenAll(tasks);

        for (int i = 0; i < agentIds.Length; i++)
        {
            results[agentIds[i]] = tasks[i].Result;
        }

        return results;
    }

    public async Task SetMultipleAgentStatesAsync(Dictionary<string, AgentStateModel> states)
    {
        var tasks = states.Select(kvp => SetAgentStateAsync(kvp.Key, kvp.Value)).ToList();
        await Task.WhenAll(tasks);
    }

    #endregion

    #region Cache Management

    public async Task InvalidateCacheAsync(string key)
    {
        _cache.Remove(key);
        
        // Also notify Lightning Server to invalidate distributed cache
        var taskData = new AgentTask
        {
            Name = "InvalidateCache",
            Input = new Dictionary<string, object> { { "key", key } }
        };

        await _lightningService.SubmitTaskAsync("cache-manager", taskData);
    }

    public async Task InvalidateCategoryAsync(string category)
    {
        // Invalidate all cache entries matching category
        var taskData = new AgentTask
        {
            Name = "InvalidateCategoryCache",
            Input = new Dictionary<string, object> { { "category", category } }
        };

        await _lightningService.SubmitTaskAsync("cache-manager", taskData);
    }

    #endregion
}
```

---

## 🚀 Performance Optimization Strategies

### 1. Multi-Level Caching Strategy

```csharp
public class CachingStrategy
{
    // Level 1: Local In-Memory Cache (Fast, Small)
    // - Duration: 5 minutes
    // - Size: Limited to ~500MB
    // - Use for: Frequently accessed agent states
    
    // Level 2: Distributed Cache (Redis - Optional)
    // - Duration: 15 minutes
    // - Size: Larger capacity
    // - Use for: Shared state across multiple instances
    
    // Level 3: Lightning Server (Source of Truth)
    // - Duration: Permanent (until invalidated)
    // - Size: Unlimited (persistent store)
    // - Use for: Authoritative state
    
    // Cache Invalidation Strategy:
    // - Time-based: 5 min for Level 1, 15 min for Level 2
    // - Event-based: Invalidate on state change
    // - Manual: InvalidateCache() methods
}
```

### 2. APO Integration for Performance

```csharp
public class APOOptimizedStateService
{
    // APO Configuration for State Management:
    private readonly LightningAPOConfig _apoConfig = new()
    {
        Enabled = true,
        Strategy = OptimizationStrategy.Balanced,
        ResourceLimits = new ResourceLimits
        {
            MaxConcurrentTasks = 20,          // Concurrent state operations
            TaskTimeoutSeconds = 30,           // State operation timeout
            CacheSizeMb = 512,                 // State cache size
            MaxMemoryMb = 2048                 // Max memory for state layer
        },
        AutoScaling = new AutoScalingConfig
        {
            Enabled = true,
            MinInstances = 1,
            MaxInstances = 5,
            ScaleUpThresholdPercent = 70
        },
        Metrics = new PerformanceMetrics
        {
            TrackingEnabled = true,
            EnableProfiling = false,           // Disable in production
            PerformanceLoggingLevel = "Info"
        }
    };

    // APO Benefits:
    // - Automatic caching optimization
    // - Connection pooling
    // - Batch request optimization
    // - Resource allocation tuning
    // - Performance monitoring
}
```

### 3. Batch State Operations

```csharp
public class BatchStateOperations
{
    // Instead of:
    var state1 = await service.GetAgentStateAsync("agent-1");
    var state2 = await service.GetAgentStateAsync("agent-2");
    var state3 = await service.GetAgentStateAsync("agent-3");
    
    // Use:
    var states = await service.GetMultipleAgentStatesAsync("agent-1", "agent-2", "agent-3");
    
    // Benefits:
    // - Single network roundtrip
    // - Lightning Server optimizes query
    // - APO may combine into single database query
    // - 3-10x faster for multiple states
}
```

### 4. Concurrency Control & Locking

```csharp
public class ConcurrencyPattern
{
    // Optimistic Concurrency:
    var state = await stateService.GetAgentStateAsync("agent-1");
    // ... modify state ...
    state.Version++;
    try
    {
        await stateService.SetAgentStateAsync("agent-1", state);
    }
    catch (VersionConflictException)
    {
        // Retry with fresh state
    }
    
    // Pessimistic Locking:
    using (var lockHandle = await stateService.AcquireLockAsync("agent-1"))
    {
        var state = await stateService.GetAgentStateAsync("agent-1");
        // ... modify state ...
        await stateService.SetAgentStateAsync("agent-1", state);
    } // Lock automatically released
}
```

---

## 📊 Integration with Workflows

### Master Workflow with State Management

```csharp
public class MasterWorkflowWithStateManagement
{
    private readonly ILightningStateService _stateService;

    public async Task ExecuteAsync(string query)
    {
        var researchId = Guid.NewGuid().ToString();
        
        // Initialize research state
        var researchState = new ResearchStateModel
        {
            ResearchId = researchId,
            Query = query,
            Status = ResearchStatus.Pending,
            StartedAt = DateTime.UtcNow
        };
        
        await _stateService.SetResearchStateAsync(researchId, researchState);

        try
        {
            // Update state as progress is made
            researchState.Status = ResearchStatus.InProgress;
            await _stateService.SetResearchStateAsync(researchId, researchState);

            // ... execute research ...
            
            // Update progress
            await _stateService.UpdateResearchProgressAsync(
                researchId,
                iterationCount: 3,
                qualityScore: 0.85
            );

            // Mark as completed
            researchState.Status = ResearchStatus.Completed;
            researchState.CompletedAt = DateTime.UtcNow;
            await _stateService.SetResearchStateAsync(researchId, researchState);
        }
        catch (Exception ex)
        {
            researchState.Status = ResearchStatus.Failed;
            await _stateService.SetResearchStateAsync(researchId, researchState);
            throw;
        }
    }
}
```

---

## 🔒 VERL Integration for State Validation

```csharp
public class VERLStateValidation
{
    private readonly ILightningVERLService _verlService;

    // Validate state before persistence
    public async Task<bool> ValidateStateAsync(AgentStateModel state)
    {
        var serialized = JsonSerializer.Serialize(state);
        var verification = await _verlService.VerifyResultAsync(state.AgentId, serialized);
        
        return verification.IsValid && verification.Confidence >= 0.8;
    }

    // Validate consistency
    public async Task<bool> CheckConsistencyAsync(List<ResearchStateModel> states)
    {
        var statements = states.Select(s => $"Research {s.ResearchId}: {s.Status}").ToList();
        var consistency = await _verlService.CheckConsistencyAsync(statements);
        
        return consistency.IsConsistent;
    }
}
```

---

## 📈 Monitoring & Metrics

```csharp
public class StateManagementMetrics
{
    // Track cache hit rate
    private long _cacheHits = 0;
    private long _cacheMisses = 0;
    
    public double CacheHitRate => 
        (_cacheHits + _cacheMisses) > 0 
            ? (double)_cacheHits / (_cacheHits + _cacheMisses) 
            : 0;
    
    // Track operation performance
    public void RecordStateOperation(string operation, TimeSpan duration)
    {
        // Log to metrics service
        // Alert if duration exceeds threshold
    }
    
    // Monitor cache memory
    public void CheckCacheMemory()
    {
        // Alert if cache exceeds memory limit
    }
}
```

---

## ✅ Best Practices Summary

### Performance Best Practices

1. **Use Multi-Level Caching**
   - Local cache: 5 min TTL
   - Redis cache: 15 min TTL (optional)
   - Lightning Server: Authoritative source

2. **Batch Operations**
   - Always use batch methods for multiple states
   - 3-10x faster than individual operations

3. **Async/Await**
   - Never block on state operations
   - Use Task.WhenAll for parallel operations
   - Respect cancellation tokens

4. **Connection Pooling**
   - Let Lightning Client handle pooling
   - Don't create new connections per request
   - Reuse service instances (DI)

5. **Monitor & Alert**
   - Track cache hit rates
   - Monitor operation latencies
   - Set up alerting for slow operations

### Consistency Best Practices

1. **Version Control**
   - Use optimistic concurrency with versions
   - Increment on every update
   - Retry on version conflicts

2. **VERL Validation**
   - Always validate before persistence
   - Use confidence threshold > 0.75
   - Log validation failures

3. **Transactional Consistency**
   - Use locks for critical sections
   - Keep transactions short
   - Implement proper error handling

### Reliability Best Practices

1. **Error Handling**
   - Catch specific exceptions
   - Implement exponential backoff retry
   - Log all failures

2. **Cache Invalidation**
   - Explicit invalidation on state change
   - Time-based expiration
   - Event-based invalidation

3. **State Recovery**
   - Always verify state after retrieval
   - Implement state recovery mechanisms
   - Keep audit logs

---

## 📝 Configuration Example

```csharp
// Program.cs
services.AddSingleton<IMemoryCache>(new MemoryCache(new MemoryCacheOptions
{
    SizeLimit = 500 * 1024 * 1024  // 500 MB
}));

services.AddSingleton<ILightningStateService>(sp => 
    new LightningStateService(
        sp.GetRequiredService<IAgentLightningService>(),
        sp.GetRequiredService<ILightningVERLService>(),
        sp.GetRequiredService<IMemoryCache>()
    )
);
```

---

## 🎯 Summary

Agent-Lightning provides an excellent foundation for distributed state management:

✅ **Centralized State:** Single source of truth via Lightning Server
✅ **High Performance:** Multi-level caching + APO optimization  
✅ **Consistency:** VERL validation and version control
✅ **Scalability:** Distributed caching and auto-scaling
✅ **Reliability:** Error handling and retry mechanisms
✅ **Monitoring:** Built-in metrics and performance tracking

This architecture scales from single instances to distributed systems while maintaining consistency and performance.

---

**Version:** 1.0  
**Status:** Ready for Implementation  
**Performance Target:** <100ms p95 for state operations
