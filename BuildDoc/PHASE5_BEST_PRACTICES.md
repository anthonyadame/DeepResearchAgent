# Phase 5 Best Practices

**Recommended patterns and practices for production deployments**

---

## Table of Contents

1. [Error Handling](#error-handling)
2. [State Management](#state-management)
3. [Performance](#performance)
4. [Security](#security)
5. [Monitoring](#monitoring)
6. [Testing](#testing)
7. [Code Organization](#code-organization)

---

## Error Handling

### ✅ DO: Use Error Recovery

```csharp
// GOOD: Use error recovery with retry and fallback
var errorRecovery = new AgentErrorRecovery(logger, maxRetries: 3);

var report = await errorRecovery.ExecuteWithRetryAsync(
    async (input, ct) => await agent.ExecuteAsync(input, ct),
    input,
    (input) => errorRecovery.CreateFallbackResearchOutput(topic, "Failed"),
    "AgentName"
);
```

```csharp
// BAD: Direct execution without error handling
var report = await agent.ExecuteAsync(input); // Can throw without recovery
```

### ✅ DO: Validate and Repair

```csharp
// GOOD: Validate and repair outputs
var report = await masterWorkflow.ExecuteFullPipelineAsync(...);
report = errorRecovery.ValidateAndRepairReportOutput(report, topic);

var validation = transitioner.ValidatePipelineState(research, analysis, topic);
if (!validation.IsValid)
{
    logger.LogWarning("Validation errors: {Errors}", validation.Errors);
    // Handle appropriately
}
```

### ✅ DO: Configure Retry Appropriately

```csharp
// GOOD: Configure based on expected failure rate
var errorRecovery = new AgentErrorRecovery(
    logger,
    maxRetries: 3, // Reasonable for transient failures
    retryDelay: TimeSpan.FromSeconds(2) // Exponential backoff
);
```

```csharp
// BAD: Too many retries or no delay
var errorRecovery = new AgentErrorRecovery(logger, maxRetries: 100); // Excessive
```

### ❌ DON'T: Swallow Exceptions

```csharp
// BAD: Catching and ignoring exceptions
try
{
    await agent.ExecuteAsync(input);
}
catch
{
    // Silent failure - never do this
}

// GOOD: Log and handle appropriately
try
{
    await agent.ExecuteAsync(input);
}
catch (Exception ex)
{
    logger.LogError(ex, "Agent execution failed");
    throw; // or handle with fallback
}
```

---

## State Management

### ✅ DO: Use State Persistence for Long Operations

```csharp
// GOOD: Persist state for long-running research
var researchId = Guid.NewGuid().ToString();

var report = await masterWorkflow.ExecuteFullPipelineWithStateAsync(
    researcherAgent, analystAgent, reportAgent,
    transitioner, errorRecovery, stateService,
    topic, brief, researchId
);

// Can query state at any time
var state = await stateService.GetResearchStateAsync(researchId);
```

### ✅ DO: Include Relevant Metadata

```csharp
// GOOD: Store useful metadata
researchState.Metadata["userId"] = userId;
researchState.Metadata["priority"] = "high";
researchState.Metadata["tags"] = string.Join(",", tags);
researchState.Metadata["startTime"] = DateTime.UtcNow.ToString("O");
```

### ✅ DO: Clean Up Old State

```csharp
// GOOD: Implement cleanup policy
public async Task CleanupOldStateAsync()
{
    var cutoff = DateTime.UtcNow.AddDays(-30);
    var oldStates = await stateService.GetStatesOlderThan(cutoff);
    
    foreach (var state in oldStates)
    {
        if (state.Status == ResearchStatus.Completed)
        {
            await stateService.DeleteResearchStateAsync(state.ResearchId);
        }
    }
}
```

### ❌ DON'T: Store Sensitive Data in State

```csharp
// BAD: Storing sensitive data
researchState.Metadata["apiKey"] = apiKey; // Never do this
researchState.Metadata["password"] = password; // Never do this

// GOOD: Store references only
researchState.Metadata["userIdHash"] = HashUserId(userId);
```

---

## Performance

### ✅ DO: Use Concurrent Execution

```csharp
// GOOD: Execute multiple research topics concurrently
var topics = new[] { "AI", "Quantum", "Blockchain" };

var tasks = topics.Select(topic => 
    masterWorkflow.ExecuteFullPipelineAsync(
        researcherAgent, analystAgent, reportAgent,
        transitioner, errorRecovery, topic, $"Research {topic}"
    )
);

var reports = await Task.WhenAll(tasks);
```

### ✅ DO: Stream for Real-Time Updates

```csharp
// GOOD: Stream progress for better UX
await foreach (var message in masterWorkflow.StreamFullPipelineAsync(...))
{
    await signalRHub.Clients.All.SendAsync("Progress", message);
}
```

### ✅ DO: Implement Caching

```csharp
// GOOD: Cache research results
var cacheKey = $"research:{topic.GetHashCode()}";
var cached = await cache.GetAsync<ResearchOutput>(cacheKey);

if (cached != null && !cached.IsExpired())
{
    return cached;
}

var research = await researcherAgent.ExecuteAsync(input);
await cache.SetAsync(cacheKey, research, TimeSpan.FromHours(24));
return research;
```

### ✅ DO: Monitor Performance

```csharp
// GOOD: Track execution time and resource usage
var stopwatch = Stopwatch.StartNew();
var report = await masterWorkflow.ExecuteFullPipelineAsync(...);
stopwatch.Stop();

metrics.RecordMetric("pipeline.duration.ms", stopwatch.ElapsedMilliseconds);
metrics.RecordMetric("pipeline.memory.mb", GC.GetTotalMemory(false) / 1024.0 / 1024.0);
```

### ❌ DON'T: Execute Sequentially When Not Needed

```csharp
// BAD: Sequential execution
foreach (var topic in topics)
{
    var report = await ExecutePipelineAsync(topic); // Slow
}

// GOOD: Concurrent execution
var tasks = topics.Select(topic => ExecutePipelineAsync(topic));
var reports = await Task.WhenAll(tasks);
```

---

## Security

### ✅ DO: Validate Inputs

```csharp
// GOOD: Validate user inputs
public async Task<ReportOutput> ResearchAsync(string topic)
{
    if (string.IsNullOrWhiteSpace(topic))
        throw new ArgumentException("Topic cannot be empty", nameof(topic));
    
    if (topic.Length > 200)
        throw new ArgumentException("Topic too long", nameof(topic));
    
    // Sanitize input
    topic = SanitizeInput(topic);
    
    return await masterWorkflow.ExecuteFullPipelineAsync(...);
}
```

### ✅ DO: Use Secure Connections

```csharp
// GOOD: Use HTTPS for external services
var config = new OllamaConfig
{
    Endpoint = "https://ollama.example.com", // HTTPS
    ApiKey = configuration["Ollama:ApiKey"] // From secure storage
};
```

### ✅ DO: Implement Rate Limiting

```csharp
// GOOD: Rate limit per user
[RateLimit(PerMinute = 10, PerHour = 100)]
public async Task<ReportOutput> ResearchAsync(string topic)
{
    return await masterWorkflow.ExecuteFullPipelineAsync(...);
}
```

### ❌ DON'T: Log Sensitive Data

```csharp
// BAD: Logging sensitive information
logger.LogInformation("API Key: {ApiKey}", apiKey); // Never

// GOOD: Log safely
logger.LogInformation("API call to {Service}", serviceName);
```

---

## Monitoring

### ✅ DO: Track Key Metrics

```csharp
// GOOD: Comprehensive metrics tracking
public async Task<ReportOutput> MonitoredExecutionAsync(string topic)
{
    var stopwatch = Stopwatch.StartNew();
    
    try
    {
        var report = await masterWorkflow.ExecuteFullPipelineAsync(...);
        
        // Success metrics
        metrics.Increment("pipeline.success");
        metrics.RecordMetric("pipeline.duration", stopwatch.ElapsedMilliseconds);
        metrics.RecordMetric("report.quality", report.QualityScore);
        
        // Component metrics
        var researchStats = transitioner.GetResearchStatistics(researchOutput);
        metrics.RecordMetric("research.facts", researchStats.TotalFacts);
        metrics.RecordMetric("research.quality", researchStats.AverageQuality);
        
        return report;
    }
    catch (Exception ex)
    {
        metrics.Increment("pipeline.failure");
        metrics.Increment($"pipeline.error.{ex.GetType().Name}");
        throw;
    }
    finally
    {
        metrics.RecordMetric("pipeline.total", stopwatch.ElapsedMilliseconds);
    }
}
```

### ✅ DO: Use Structured Logging

```csharp
// GOOD: Structured logging with context
logger.LogInformation(
    "Pipeline completed for topic {Topic} in {Duration}ms with quality {Quality}",
    topic,
    duration,
    report.QualityScore
);
```

### ✅ DO: Set Up Alerts

```csharp
// GOOD: Alert on anomalies
if (report.QualityScore < 5.0f)
{
    logger.LogWarning("Low quality report: {Quality}", report.QualityScore);
    await alertService.SendAsync("Low quality report detected");
}

if (stopwatch.ElapsedMilliseconds > 30000)
{
    logger.LogWarning("Slow pipeline execution: {Duration}ms", duration);
}
```

---

## Testing

### ✅ DO: Write Unit Tests

```csharp
// GOOD: Unit test for state transition
[Fact]
public void CreateAnalysisInput_WithValidResearch_CreatesInput()
{
    var transitioner = new StateTransitioner();
    var research = CreateSampleResearchOutput();
    
    var result = transitioner.CreateAnalysisInput(research, "Topic", "Brief");
    
    Assert.NotNull(result);
    Assert.Equal("Topic", result.Topic);
    Assert.NotEmpty(result.Findings);
}
```

### ✅ DO: Write Integration Tests

```csharp
// GOOD: Integration test for full pipeline
[Fact]
public async Task FullPipeline_WithAllComponents_CompletesSuccessfully()
{
    var report = await masterWorkflow.ExecuteFullPipelineAsync(
        researcherAgent, analystAgent, reportAgent,
        transitioner, errorRecovery,
        "Test Topic", "Test Brief"
    );
    
    Assert.NotNull(report);
    Assert.NotEmpty(report.Title);
    Assert.True(report.QualityScore > 0);
}
```

### ✅ DO: Test Error Scenarios

```csharp
// GOOD: Test error recovery
[Fact]
public async Task Pipeline_WithFailure_UsesFallback()
{
    SetupFailingMocks();
    
    var report = await errorRecovery.ExecuteWithRetryAsync(
        async (input, ct) => await agent.ExecuteAsync(input, ct),
        input,
        (input) => errorRecovery.CreateFallbackResearchOutput("Topic", "Failed"),
        "AgentName"
    );
    
    Assert.Equal(1.0f, report.AverageQuality); // Fallback indicator
}
```

### ✅ DO: Write Performance Tests

```csharp
// GOOD: Performance test
[Fact]
public async Task Pipeline_CompletesWithinTimeLimit()
{
    var stopwatch = Stopwatch.StartNew();
    
    var report = await masterWorkflow.ExecuteFullPipelineAsync(...);
    
    stopwatch.Stop();
    Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10));
}
```

---

## Code Organization

### ✅ DO: Use Dependency Injection

```csharp
// GOOD: DI-based architecture
public class ResearchService
{
    private readonly MasterWorkflow _workflow;
    private readonly StateTransitioner _transitioner;
    private readonly AgentErrorRecovery _errorRecovery;
    private readonly ILogger<ResearchService> _logger;
    
    public ResearchService(
        MasterWorkflow workflow,
        StateTransitioner transitioner,
        AgentErrorRecovery errorRecovery,
        ILogger<ResearchService> logger)
    {
        _workflow = workflow;
        _transitioner = transitioner;
        _errorRecovery = errorRecovery;
        _logger = logger;
    }
}
```

### ✅ DO: Separate Concerns

```csharp
// GOOD: Separate service layers
public class ResearchOrchestrator
{
    public async Task<ReportOutput> ExecuteAsync(string topic)
    {
        // Orchestration logic
    }
}

public class ResearchValidator
{
    public ValidationResult Validate(ResearchOutput output)
    {
        // Validation logic
    }
}

public class ResearchMetrics
{
    public void Track(ResearchOutput output)
    {
        // Metrics logic
    }
}
```

### ✅ DO: Use Configuration

```csharp
// GOOD: Configuration-based settings
public class PipelineOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromSeconds(1);
    public int MaxIterations { get; set; } = 3;
    public float MinQualityThreshold { get; set; } = 7.0f;
}

// Register
builder.Services.Configure<PipelineOptions>(
    configuration.GetSection("Pipeline"));
```

### ❌ DON'T: Hard-Code Values

```csharp
// BAD: Hard-coded values
var errorRecovery = new AgentErrorRecovery(logger, 3); // Magic number

// GOOD: Use configuration
var errorRecovery = new AgentErrorRecovery(
    logger, 
    options.Value.MaxRetries);
```

---

## Summary

### Critical Practices

1. ✅ **Always use error recovery** with retry and fallback
2. ✅ **Validate outputs** before state transitions
3. ✅ **Track metrics** for monitoring and debugging
4. ✅ **Use state persistence** for long operations
5. ✅ **Stream progress** for better UX
6. ✅ **Write comprehensive tests** for reliability
7. ✅ **Use dependency injection** for maintainability
8. ✅ **Monitor performance** and set up alerts
9. ✅ **Secure sensitive data** and validate inputs
10. ✅ **Document your code** for team collaboration

### Anti-Patterns to Avoid

1. ❌ Direct execution without error handling
2. ❌ Swallowing exceptions
3. ❌ Storing sensitive data in state
4. ❌ Sequential execution when concurrent is possible
5. ❌ Logging sensitive information
6. ❌ Hard-coded configuration values
7. ❌ Missing validation
8. ❌ No performance monitoring
9. ❌ Ignoring validation warnings

---

## Next Steps

- Review [API Reference](PHASE5_API_REFERENCE.md)
- See [Integration Guide](PHASE5_INTEGRATION_GUIDE.md)
- Check [Examples](PHASE5_EXAMPLES.md)
- Try [Quick Start](PHASE5_QUICK_START.md)
