# Phase 5 Integration - API Reference

**Version:** 1.0  
**Last Updated:** Sprint 3 Completion  
**Status:** Production Ready  

---

## Overview

Phase 5 introduces a complete three-agent pipeline with state management, error recovery, and performance optimization. This API reference covers all new components.

---

## Table of Contents

1. [MasterWorkflowExtensions](#masterworkflowextensions)
2. [StateTransitioner](#statetransitioner)
3. [AgentErrorRecovery](#agenterrorrecovery)
4. [ResearcherWorkflowExtensions](#researcherworkflowextensions)
5. [Models](#models)

---

## MasterWorkflowExtensions

Extension methods for `MasterWorkflow` that enable complete pipeline execution.

### Namespace
```csharp
using DeepResearchAgent.Workflows;
```

### Methods

#### ExecuteFullPipelineAsync

Executes the complete research pipeline with error recovery.

**Signature:**
```csharp
public static async Task<ReportOutput> ExecuteFullPipelineAsync(
    this MasterWorkflow workflow,
    ResearcherAgent researcherAgent,
    AnalystAgent analystAgent,
    ReportAgent reportAgent,
    StateTransitioner transitioner,
    AgentErrorRecovery errorRecovery,
    string topic,
    string researchBrief,
    Microsoft.Extensions.Logging.ILogger? logger = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `workflow` - The MasterWorkflow instance
- `researcherAgent` - ResearcherAgent for research phase
- `analystAgent` - AnalystAgent for analysis phase
- `reportAgent` - ReportAgent for report generation
- `transitioner` - StateTransitioner for state mapping
- `errorRecovery` - AgentErrorRecovery for error handling
- `topic` - Research topic
- `researchBrief` - Research brief/description
- `logger` - Optional logger
- `cancellationToken` - Cancellation token

**Returns:** `Task<ReportOutput>` - Complete research report

**Example:**
```csharp
var report = await masterWorkflow.ExecuteFullPipelineAsync(
    researcherAgent,
    analystAgent,
    reportAgent,
    transitioner,
    errorRecovery,
    "Quantum Computing",
    "Research quantum computing breakthroughs",
    logger
);
```

**Throws:**
- `Exception` - If all retries fail and fallback fails

---

#### ExecuteFullPipelineWithStateAsync

Executes pipeline with state persistence to Lightning state service.

**Signature:**
```csharp
public static async Task<ReportOutput> ExecuteFullPipelineWithStateAsync(
    this MasterWorkflow workflow,
    ResearcherAgent researcherAgent,
    AnalystAgent analystAgent,
    ReportAgent reportAgent,
    StateTransitioner transitioner,
    AgentErrorRecovery errorRecovery,
    ILightningStateService stateService,
    string topic,
    string researchBrief,
    string? researchId = null,
    Microsoft.Extensions.Logging.ILogger? logger = null,
    CancellationToken cancellationToken = default)
```

**Additional Parameters:**
- `stateService` - Lightning state service for persistence
- `researchId` - Optional research ID (auto-generated if null)

**Example:**
```csharp
var report = await masterWorkflow.ExecuteFullPipelineWithStateAsync(
    researcherAgent,
    analystAgent,
    reportAgent,
    transitioner,
    errorRecovery,
    stateService,
    "AI Safety",
    "Research AI safety concerns",
    "research-123"
);

// Query state later
var state = await stateService.GetResearchStateAsync("research-123");
```

---

#### StreamFullPipelineAsync

Streams pipeline execution with real-time progress updates.

**Signature:**
```csharp
public static async IAsyncEnumerable<string> StreamFullPipelineAsync(
    this MasterWorkflow workflow,
    ResearcherAgent researcherAgent,
    AnalystAgent analystAgent,
    ReportAgent reportAgent,
    StateTransitioner transitioner,
    AgentErrorRecovery errorRecovery,
    string topic,
    string researchBrief,
    [EnumeratorCancellation] CancellationToken cancellationToken = default)
```

**Returns:** `IAsyncEnumerable<string>` - Stream of progress messages

**Example:**
```csharp
await foreach (var message in masterWorkflow.StreamFullPipelineAsync(
    researcherAgent,
    analystAgent,
    reportAgent,
    transitioner,
    errorRecovery,
    "Machine Learning",
    "Research ML algorithms"))
{
    Console.WriteLine(message);
}
```

---

## StateTransitioner

Manages state transitions between agents.

### Namespace
```csharp
using DeepResearchAgent.Services;
```

### Constructor
```csharp
public StateTransitioner(ILogger<StateTransitioner>? logger = null)
```

### Methods

#### CreateAnalysisInput

Maps ResearchOutput to AnalysisInput.

**Signature:**
```csharp
public AnalysisInput CreateAnalysisInput(
    ResearchOutput research,
    string topic,
    string researchBrief)
```

**Example:**
```csharp
var analysisInput = transitioner.CreateAnalysisInput(
    researchOutput,
    "Topic",
    "Research brief"
);
```

---

#### CreateReportInput

Maps ResearchOutput and AnalysisOutput to ReportInput.

**Signature:**
```csharp
public ReportInput CreateReportInput(
    ResearchOutput research,
    AnalysisOutput analysis,
    string topic,
    string? author = null)
```

**Example:**
```csharp
var reportInput = transitioner.CreateReportInput(
    researchOutput,
    analysisOutput,
    "Topic",
    "Author Name"
);
```

---

#### ValidateResearchOutput

Validates ResearchOutput for completeness.

**Signature:**
```csharp
public ValidationResult ValidateResearchOutput(ResearchOutput output)
```

**Returns:** `ValidationResult` with errors and warnings

**Example:**
```csharp
var validation = transitioner.ValidateResearchOutput(research);
if (!validation.IsValid)
{
    Console.WriteLine($"Errors: {string.Join(", ", validation.Errors)}");
}
```

---

#### ValidateAnalysisOutput

Validates AnalysisOutput for completeness.

**Signature:**
```csharp
public ValidationResult ValidateAnalysisOutput(AnalysisOutput output)
```

---

#### ValidatePipelineState

Validates complete pipeline state.

**Signature:**
```csharp
public ValidationResult ValidatePipelineState(
    ResearchOutput research,
    AnalysisOutput? analysis = null,
    string? topic = null)
```

---

#### GetResearchStatistics

Extracts statistics from ResearchOutput.

**Signature:**
```csharp
public ResearchStatistics GetResearchStatistics(ResearchOutput output)
```

**Returns:** `ResearchStatistics` with metrics

**Example:**
```csharp
var stats = transitioner.GetResearchStatistics(research);
Console.WriteLine($"Facts: {stats.TotalFacts}");
Console.WriteLine($"Quality: {stats.AverageQuality:F1}");
```

---

#### GetAnalysisStatistics

Extracts statistics from AnalysisOutput.

**Signature:**
```csharp
public AnalysisStatistics GetAnalysisStatistics(AnalysisOutput output)
```

---

## AgentErrorRecovery

Handles errors with retry and fallback mechanisms.

### Namespace
```csharp
using DeepResearchAgent.Services;
```

### Constructor
```csharp
public AgentErrorRecovery(
    ILogger<AgentErrorRecovery>? logger = null,
    int maxRetries = 2,
    TimeSpan? retryDelay = null)
```

**Parameters:**
- `logger` - Optional logger
- `maxRetries` - Maximum retry attempts (default: 2)
- `retryDelay` - Delay between retries (default: 1 second)

### Methods

#### ExecuteWithRetryAsync

Executes function with retry and fallback.

**Signature:**
```csharp
public async Task<TOutput> ExecuteWithRetryAsync<TInput, TOutput>(
    Func<TInput, CancellationToken, Task<TOutput>> agentFunc,
    TInput input,
    Func<TInput, TOutput> fallbackFunc,
    string agentName,
    CancellationToken cancellationToken = default)
    where TOutput : class
```

**Example:**
```csharp
var result = await errorRecovery.ExecuteWithRetryAsync(
    async (input, ct) => await agent.ExecuteAsync(input, ct),
    input,
    (input) => errorRecovery.CreateFallbackResearchOutput(topic, "Failed"),
    "AgentName"
);
```

---

#### CreateFallbackResearchOutput

Creates fallback ResearchOutput.

**Signature:**
```csharp
public ResearchOutput CreateFallbackResearchOutput(
    string topic,
    string errorMessage)
```

---

#### CreateFallbackAnalysisOutput

Creates fallback AnalysisOutput.

**Signature:**
```csharp
public AnalysisOutput CreateFallbackAnalysisOutput(
    string topic,
    string errorMessage)
```

---

#### CreateFallbackReportOutput

Creates fallback ReportOutput.

**Signature:**
```csharp
public ReportOutput CreateFallbackReportOutput(
    string topic,
    string errorMessage)
```

---

#### ValidateAndRepairResearchOutput

Validates and repairs ResearchOutput.

**Signature:**
```csharp
public ResearchOutput ValidateAndRepairResearchOutput(
    ResearchOutput output,
    string topic)
```

**Example:**
```csharp
research = errorRecovery.ValidateAndRepairResearchOutput(research, topic);
```

---

#### ValidateAndRepairAnalysisOutput

Validates and repairs AnalysisOutput.

**Signature:**
```csharp
public AnalysisOutput ValidateAndRepairAnalysisOutput(
    AnalysisOutput output,
    string topic)
```

---

#### ValidateAndRepairReportOutput

Validates and repairs ReportOutput.

**Signature:**
```csharp
public ReportOutput ValidateAndRepairReportOutput(
    ReportOutput output,
    string topic)
```

---

## ResearcherWorkflowExtensions

Extension methods for ResearcherWorkflow integration.

### Namespace
```csharp
using DeepResearchAgent.Workflows;
```

### Methods

#### ResearchWithAgentAsync

Simple delegation to ResearcherAgent.

**Signature:**
```csharp
public static async Task<ResearchOutput> ResearchWithAgentAsync(
    this ResearcherWorkflow workflow,
    ResearcherAgent agent,
    string topic,
    string researchBrief,
    int maxIterations = 3,
    float minQualityThreshold = 7.0f,
    CancellationToken cancellationToken = default)
```

---

#### ToFactState / ToFactStates

Converts ExtractedFact to FactState.

**Signature:**
```csharp
public static FactState ToFactState(this ExtractedFact fact)
public static IEnumerable<FactState> ToFactStates(
    this IEnumerable<ExtractedFact> facts)
```

**Example:**
```csharp
var factState = extractedFact.ToFactState();
var factStates = facts.ToFactStates();
```

---

## Models

### ValidationResult

Result of validation operations.

```csharp
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; }
    public List<string> Warnings { get; set; }
}
```

---

### ResearchStatistics

Research output statistics.

```csharp
public class ResearchStatistics
{
    public int TotalFindings { get; set; }
    public int TotalFacts { get; set; }
    public float AverageQuality { get; set; }
    public int IterationsUsed { get; set; }
    public float AverageConfidence { get; set; }
}
```

---

### AnalysisStatistics

Analysis output statistics.

```csharp
public class AnalysisStatistics
{
    public int TotalInsights { get; set; }
    public int TotalThemes { get; set; }
    public int TotalContradictions { get; set; }
    public float ConfidenceScore { get; set; }
    public int NarrativeLength { get; set; }
}
```

---

### ErrorRecoveryStats

Error recovery statistics.

```csharp
public class ErrorRecoveryStats
{
    public int TotalAttempts { get; set; }
    public int TotalRetries { get; set; }
    public int TotalFallbacks { get; set; }
    public int TotalRepairs { get; set; }
}
```

---

## Error Handling

### Exception Types

All methods may throw:
- `ArgumentNullException` - If required parameters are null
- `ArgumentException` - If parameters are invalid
- `AggregateException` - If retries and fallback both fail

### Error Recovery Flow

```
1. Execute agent function
2. On failure, retry with delay
3. If all retries fail, use fallback
4. If fallback fails, throw AggregateException
```

---

## Performance Considerations

### Execution Time

- Full pipeline: < 10 seconds (mocked)
- State transitions: < 1ms average
- Validation: < 1ms average
- Error recovery: < 1ms average

### Throughput

- Sequential: ~1.5 requests/second
- Concurrent (10): ~4 requests/second

### Memory Usage

- Per execution: < 100 MB
- Memory growth: < 50% over 10 iterations

---

## Best Practices

1. **Always use error recovery** for production workloads
2. **Validate outputs** before state transitions
3. **Use state persistence** for long-running research
4. **Stream progress** for user-facing applications
5. **Monitor statistics** for quality assurance
6. **Configure retry limits** based on requirements
7. **Log all operations** for debugging

---

## Version History

- **1.0** - Initial Phase 5 release
  - MasterWorkflowExtensions
  - StateTransitioner
  - AgentErrorRecovery
  - ResearcherWorkflowExtensions
  - Complete pipeline integration

---

## See Also

- [Integration Guide](PHASE5_INTEGRATION_GUIDE.md)
- [Quick Start](PHASE5_QUICK_START.md)
- [Best Practices](PHASE5_BEST_PRACTICES.md)
- [Examples](PHASE5_EXAMPLES.md)
