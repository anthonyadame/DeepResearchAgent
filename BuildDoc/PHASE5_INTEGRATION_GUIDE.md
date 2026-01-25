# Phase 5 Integration Guide

**Complete guide to integrating the three-agent research pipeline**

---

## Table of Contents

1. [Overview](#overview)
2. [Prerequisites](#prerequisites)
3. [Basic Integration](#basic-integration)
4. [Advanced Integration](#advanced-integration)
5. [State Management](#state-management)
6. [Error Handling](#error-handling)
7. [Performance Optimization](#performance-optimization)
8. [Troubleshooting](#troubleshooting)

---

## Overview

Phase 5 provides a complete research pipeline that combines:
- **ResearcherAgent** - Web search and fact extraction
- **AnalystAgent** - Analysis and insight generation
- **ReportAgent** - Report formatting and generation

With supporting services:
- **StateTransitioner** - State mapping between agents
- **AgentErrorRecovery** - Error handling and fallbacks
- **MasterWorkflowExtensions** - Pipeline orchestration

---

## Prerequisites

### Required Services

```csharp
// Core services
var llmService = new OllamaService(config);
var toolService = new ToolInvocationService(llmService, logger);
var stateService = new LightningStateService(config);

// Create agents
var researcherAgent = new ResearcherAgent(llmService, toolService, logger);
var analystAgent = new AnalystAgent(llmService, toolService, logger);
var reportAgent = new ReportAgent(llmService, toolService, logger);

// Create support services
var transitioner = new StateTransitioner(logger);
var errorRecovery = new AgentErrorRecovery(logger, maxRetries: 3);

// Create MasterWorkflow
var masterWorkflow = new MasterWorkflow(
    stateService,
    supervisorWorkflow,
    llmService,
    logger
);
```

### Dependencies

```xml
<PackageReference Include="Microsoft.Extensions.Logging" Version="8.0.0" />
<PackageReference Include="OllamaSharp" Version="..." />
```

---

## Basic Integration

### Simplest Usage

```csharp
using DeepResearchAgent.Workflows;
using DeepResearchAgent.Services;

// Execute pipeline
var report = await masterWorkflow.ExecuteFullPipelineAsync(
    researcherAgent,
    analystAgent,
    reportAgent,
    transitioner,
    errorRecovery,
    topic: "Quantum Computing",
    researchBrief: "Research recent quantum computing breakthroughs"
);

// Use the report
Console.WriteLine($"Title: {report.Title}");
Console.WriteLine($"Summary: {report.ExecutiveSummary}");
Console.WriteLine($"Quality: {report.QualityScore:F2}");
```

### Step-by-Step Explanation

1. **MasterWorkflow** orchestrates the pipeline
2. **ResearcherAgent** conducts research
3. **StateTransitioner** maps research → analysis input
4. **AnalystAgent** performs analysis
5. **StateTransitioner** maps analysis → report input
6. **ReportAgent** generates final report
7. **ErrorRecovery** handles failures at each step

---

## Advanced Integration

### With Validation

```csharp
// Execute pipeline
var report = await masterWorkflow.ExecuteFullPipelineAsync(
    researcherAgent,
    analystAgent,
    reportAgent,
    transitioner,
    errorRecovery,
    topic,
    brief,
    logger
);

// Validate each phase's output
var researchValidation = transitioner.ValidateResearchOutput(researchOutput);
if (!researchValidation.IsValid)
{
    logger.LogWarning("Research validation failed: {Errors}", 
        string.Join(", ", researchValidation.Errors));
}

var analysisValidation = transitioner.ValidateAnalysisOutput(analysisOutput);
if (!analysisValidation.IsValid)
{
    logger.LogWarning("Analysis validation failed: {Errors}",
        string.Join(", ", analysisValidation.Errors));
}

// Validate complete pipeline
var pipelineValidation = transitioner.ValidatePipelineState(
    researchOutput, 
    analysisOutput, 
    topic
);
```

### With Statistics Tracking

```csharp
// Execute pipeline
var report = await masterWorkflow.ExecuteFullPipelineAsync(...);

// Get statistics
var researchStats = transitioner.GetResearchStatistics(researchOutput);
logger.LogInformation(
    "Research: {Facts} facts, quality {Quality:F1}/10, confidence {Confidence:F2}",
    researchStats.TotalFacts,
    researchStats.AverageQuality,
    researchStats.AverageConfidence
);

var analysisStats = transitioner.GetAnalysisStatistics(analysisOutput);
logger.LogInformation(
    "Analysis: {Insights} insights, {Themes} themes, confidence {Confidence:F2}",
    analysisStats.TotalInsights,
    analysisStats.TotalThemes,
    analysisStats.ConfidenceScore
);

// Log to monitoring system
metrics.RecordMetric("research.facts.count", researchStats.TotalFacts);
metrics.RecordMetric("research.quality", researchStats.AverageQuality);
metrics.RecordMetric("analysis.insights.count", analysisStats.TotalInsights);
metrics.RecordMetric("report.quality", report.QualityScore);
```

### With Custom Error Recovery

```csharp
// Configure error recovery
var errorRecovery = new AgentErrorRecovery(
    logger,
    maxRetries: 5,
    retryDelay: TimeSpan.FromSeconds(2)
);

// Execute with retry and fallback
var research = await errorRecovery.ExecuteWithRetryAsync(
    async (input, ct) => await researcherAgent.ExecuteAsync(input, ct),
    researchInput,
    (input) => {
        logger.LogWarning("Research failed, using fallback");
        return errorRecovery.CreateFallbackResearchOutput(topic, "Research phase failed");
    },
    "ResearcherAgent"
);

// Repair if needed
research = errorRecovery.ValidateAndRepairResearchOutput(research, topic);
```

---

## State Management

### With State Persistence

```csharp
// Generate research ID
var researchId = Guid.NewGuid().ToString();

// Execute with state persistence
var report = await masterWorkflow.ExecuteFullPipelineWithStateAsync(
    researcherAgent,
    analystAgent,
    reportAgent,
    transitioner,
    errorRecovery,
    stateService,
    topic: "AI Safety",
    researchBrief: "Research AI safety concerns and solutions",
    researchId: researchId,
    logger: logger
);

// Query state later
var state = await stateService.GetResearchStateAsync(researchId);
Console.WriteLine($"Status: {state.Status}");
Console.WriteLine($"Started: {state.StartedAt}");
Console.WriteLine($"Completed: {state.CompletedAt}");
Console.WriteLine($"Quality: {state.Metadata["qualityScore"]}");
```

### State Lifecycle

```
1. Initial State (Pending)
   ↓
2. Research Phase (InProgress, phase="research")
   ↓
3. Analysis Phase (InProgress, phase="analysis")
   ↓
4. Report Phase (InProgress, phase="report")
   ↓
5. Completed (Completed, with metadata)
```

### State Metadata

```csharp
var state = await stateService.GetResearchStateAsync(researchId);

// Access metadata
var phase = state.Metadata["phase"];
var brief = state.Metadata["researchBrief"];
var title = state.Metadata["reportTitle"];
var quality = state.Metadata["qualityScore"];
```

---

## Error Handling

### Error Recovery Pattern

```csharp
try
{
    // Attempt execution
    var report = await masterWorkflow.ExecuteFullPipelineAsync(...);
    return report;
}
catch (AggregateException ex)
{
    // Both retries and fallback failed
    logger.LogError(ex, "Pipeline failed completely");
    
    // Notify user
    await notificationService.SendAsync(
        $"Research on '{topic}' failed: {ex.Message}"
    );
    
    // Return error report
    return CreateErrorReport(topic, ex);
}
catch (Exception ex)
{
    // Unexpected error
    logger.LogError(ex, "Unexpected pipeline error");
    throw;
}
```

### Graceful Degradation

```csharp
// Execute pipeline with fallbacks
var research = await errorRecovery.ExecuteWithRetryAsync(
    async (input, ct) => await researcherAgent.ExecuteAsync(input, ct),
    researchInput,
    (input) => errorRecovery.CreateFallbackResearchOutput(topic, "Research failed"),
    "ResearcherAgent"
);

// Check if fallback was used
if (research.AverageQuality == 1.0f)
{
    logger.LogWarning("Research used fallback data");
    // Continue with degraded data
}

// Proceed with analysis
var analysis = await analystAgent.ExecuteAsync(analysisInput);
```

### Error State Updates

```csharp
try
{
    var report = await masterWorkflow.ExecuteFullPipelineWithStateAsync(...);
}
catch (Exception ex)
{
    // Error state is automatically updated
    var errorState = await stateService.GetResearchStateAsync(researchId);
    
    Console.WriteLine($"Status: {errorState.Status}"); // Failed
    Console.WriteLine($"Error: {errorState.Metadata["error"]}");
    Console.WriteLine($"Error Type: {errorState.Metadata["errorType"]}");
}
```

---

## Performance Optimization

### Concurrent Execution

```csharp
// Execute multiple research topics concurrently
var topics = new[] { "AI", "Quantum", "Blockchain" };

var tasks = topics.Select(async topic =>
{
    return await masterWorkflow.ExecuteFullPipelineAsync(
        researcherAgent,
        analystAgent,
        reportAgent,
        transitioner,
        errorRecovery,
        topic,
        $"Research {topic}",
        logger
    );
});

var reports = await Task.WhenAll(tasks);
```

### Streaming for Real-Time Updates

```csharp
// Stream progress to UI
await foreach (var message in masterWorkflow.StreamFullPipelineAsync(
    researcherAgent,
    analystAgent,
    reportAgent,
    transitioner,
    errorRecovery,
    topic,
    brief))
{
    // Update UI
    await statusHub.SendAsync("ProgressUpdate", message);
    
    // Log progress
    logger.LogInformation(message);
}
```

### Caching Strategy

```csharp
// Cache research results
var cacheKey = $"research:{topic.GetHashCode()}";
var cachedResearch = await cache.GetAsync<ResearchOutput>(cacheKey);

if (cachedResearch != null)
{
    logger.LogInformation("Using cached research for {Topic}", topic);
    researchOutput = cachedResearch;
}
else
{
    researchOutput = await researcherAgent.ExecuteAsync(researchInput);
    await cache.SetAsync(cacheKey, researchOutput, TimeSpan.FromHours(24));
}
```

---

## Troubleshooting

### Common Issues

#### Issue: Pipeline fails with AggregateException

**Cause:** All retries exhausted and fallback also failed

**Solution:**
```csharp
// Increase retry count
var errorRecovery = new AgentErrorRecovery(logger, maxRetries: 5);

// Add delay between retries
var errorRecovery = new AgentErrorRecovery(
    logger, 
    maxRetries: 3,
    retryDelay: TimeSpan.FromSeconds(5)
);

// Ensure fallback functions don't throw
Func<ResearchInput, ResearchOutput> safeFallback = (input) =>
{
    try
    {
        return errorRecovery.CreateFallbackResearchOutput(topic, "Failed");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "Fallback creation failed");
        // Return minimal valid output
        return new ResearchOutput
        {
            Findings = new List<FactExtractionResult>(),
            AverageQuality = 0.1f
        };
    }
};
```

#### Issue: Validation fails with errors

**Cause:** Output is missing required data

**Solution:**
```csharp
// Use repair functions
research = errorRecovery.ValidateAndRepairResearchOutput(research, topic);
analysis = errorRecovery.ValidateAndRepairAnalysisOutput(analysis, topic);
report = errorRecovery.ValidateAndRepairReportOutput(report, topic);

// Check validation result
var validation = transitioner.ValidateResearchOutput(research);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        logger.LogError("Validation error: {Error}", error);
    }
    // Repair
    research = errorRecovery.ValidateAndRepairResearchOutput(research, topic);
}
```

#### Issue: State not persisting

**Cause:** State service not configured correctly

**Solution:**
```csharp
// Ensure state service is properly initialized
var stateService = new LightningStateService(configuration);

// Use state-aware method
var report = await masterWorkflow.ExecuteFullPipelineWithStateAsync(
    researcherAgent,
    analystAgent,
    reportAgent,
    transitioner,
    errorRecovery,
    stateService, // Don't forget this parameter
    topic,
    brief
);
```

#### Issue: Poor performance / slow execution

**Cause:** Sequential execution or insufficient resources

**Solution:**
```csharp
// Use concurrent execution for multiple topics
var tasks = topics.Select(topic => ExecutePipelineAsync(topic));
await Task.WhenAll(tasks);

// Enable parallel tool execution
// (already implemented in agents)

// Monitor performance
var stopwatch = Stopwatch.StartNew();
var report = await masterWorkflow.ExecuteFullPipelineAsync(...);
stopwatch.Stop();
logger.LogInformation("Pipeline completed in {Ms}ms", stopwatch.ElapsedMilliseconds);
```

### Debugging Tips

1. **Enable detailed logging:**
   ```csharp
   builder.Services.AddLogging(config =>
   {
       config.SetMinimumLevel(LogLevel.Debug);
       config.AddConsole();
   });
   ```

2. **Track metrics:**
   ```csharp
   var stats = transitioner.GetResearchStatistics(research);
   logger.LogDebug("Research stats: {@Stats}", stats);
   ```

3. **Validate at each step:**
   ```csharp
   var validation = transitioner.ValidateResearchOutput(research);
   logger.LogDebug("Research validation: {@Validation}", validation);
   ```

4. **Use streaming for visibility:**
   ```csharp
   await foreach (var message in masterWorkflow.StreamFullPipelineAsync(...))
   {
       Console.WriteLine(message);
   }
   ```

---

## Next Steps

- Review [API Reference](PHASE5_API_REFERENCE.md)
- See [Examples](PHASE5_EXAMPLES.md)
- Read [Best Practices](PHASE5_BEST_PRACTICES.md)
- Check [Performance Guide](PHASE5_PERFORMANCE.md)

---

## Support

For issues or questions:
1. Check the troubleshooting section
2. Review the API reference
3. See the examples
4. Open an issue on GitHub
