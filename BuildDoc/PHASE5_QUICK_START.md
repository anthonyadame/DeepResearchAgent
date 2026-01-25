# Phase 5 Quick Start Guide

**Get up and running with the complete research pipeline in 5 minutes**

---

## 🚀 Quick Start (2 minutes)

### Step 1: Create Services

```csharp
using DeepResearchAgent.Workflows;
using DeepResearchAgent.Services;
using DeepResearchAgent.Agents;

// LLM and tools
var llmService = new OllamaService(configuration);
var toolService = new ToolInvocationService(llmService, logger);
var stateService = new LightningStateService(configuration);

// Agents
var researcherAgent = new ResearcherAgent(llmService, toolService, logger);
var analystAgent = new AnalystAgent(llmService, toolService, logger);
var reportAgent = new ReportAgent(llmService, toolService, logger);

// Support services
var transitioner = new StateTransitioner(logger);
var errorRecovery = new AgentErrorRecovery(logger);

// MasterWorkflow
var supervisorWorkflow = new SupervisorWorkflow(...);
var masterWorkflow = new MasterWorkflow(
    stateService, supervisorWorkflow, llmService, logger);
```

### Step 2: Execute Pipeline

```csharp
var report = await masterWorkflow.ExecuteFullPipelineAsync(
    researcherAgent,
    analystAgent,
    reportAgent,
    transitioner,
    errorRecovery,
    topic: "Quantum Computing",
    researchBrief: "Research quantum computing breakthroughs"
);
```

### Step 3: Use Results

```csharp
Console.WriteLine($"Title: {report.Title}");
Console.WriteLine($"Summary: {report.ExecutiveSummary}");
Console.WriteLine($"Quality: {report.QualityScore:F2}");
Console.WriteLine($"Sections: {report.Sections.Count}");
```

**Done!** 🎉

---

## 📚 Common Scenarios

### Scenario 1: Basic Research Report

```csharp
// Create a simple research report
var report = await masterWorkflow.ExecuteFullPipelineAsync(
    researcherAgent, analystAgent, reportAgent,
    transitioner, errorRecovery,
    "AI Safety",
    "Research current AI safety concerns"
);

// Display report
foreach (var section in report.Sections)
{
    Console.WriteLine($"\n## {section.Heading}");
    Console.WriteLine(section.Content);
}
```

### Scenario 2: With State Tracking

```csharp
// Execute with state persistence
var researchId = "research-123";
var report = await masterWorkflow.ExecuteFullPipelineWithStateAsync(
    researcherAgent, analystAgent, reportAgent,
    transitioner, errorRecovery, stateService,
    "Machine Learning",
    "Research ML algorithms",
    researchId
);

// Check state
var state = await stateService.GetResearchStateAsync(researchId);
Console.WriteLine($"Status: {state.Status}");
Console.WriteLine($"Started: {state.StartedAt}");
Console.WriteLine($"Duration: {state.CompletedAt - state.StartedAt}");
```

### Scenario 3: Real-Time Progress

```csharp
// Stream progress to console
await foreach (var message in masterWorkflow.StreamFullPipelineAsync(
    researcherAgent, analystAgent, reportAgent,
    transitioner, errorRecovery,
    "Blockchain",
    "Research blockchain applications"))
{
    Console.WriteLine(message);
    // Output:
    // [pipeline] Starting research on: Blockchain
    // [pipeline] Phase 1/3: Research
    // [pipeline] Research complete: 15 facts extracted
    // [pipeline] Phase 2/3: Analysis
    // [pipeline] Analysis complete: 8 insights generated
    // [pipeline] Phase 3/3: Report Generation
    // [pipeline] Report complete: Blockchain Applications Report
    // [pipeline] Pipeline completed successfully!
}
```

### Scenario 4: Multiple Topics

```csharp
// Research multiple topics
var topics = new[] { "AI", "Quantum", "Blockchain" };

var tasks = topics.Select(async topic =>
    await masterWorkflow.ExecuteFullPipelineAsync(
        researcherAgent, analystAgent, reportAgent,
        transitioner, errorRecovery,
        topic,
        $"Research {topic} technology"
    )
);

var reports = await Task.WhenAll(tasks);

// Process results
foreach (var report in reports)
{
    Console.WriteLine($"\n=== {report.Title} ===");
    Console.WriteLine(report.ExecutiveSummary);
}
```

### Scenario 5: With Validation

```csharp
// Execute pipeline
var report = await masterWorkflow.ExecuteFullPipelineAsync(
    researcherAgent, analystAgent, reportAgent,
    transitioner, errorRecovery,
    "Neural Networks",
    "Research neural network architectures"
);

// Validate and get statistics
var researchStats = transitioner.GetResearchStatistics(researchOutput);
var analysisStats = transitioner.GetAnalysisStatistics(analysisOutput);

Console.WriteLine($"Research: {researchStats.TotalFacts} facts");
Console.WriteLine($"Analysis: {analysisStats.TotalInsights} insights");
Console.WriteLine($"Report Quality: {report.QualityScore:F2}");
```

---

## 🎯 Essential Patterns

### Pattern 1: Error Handling

```csharp
try
{
    var report = await masterWorkflow.ExecuteFullPipelineAsync(...);
    return report;
}
catch (Exception ex)
{
    logger.LogError(ex, "Pipeline failed");
    return CreateErrorReport(ex);
}
```

### Pattern 2: Validation Before Use

```csharp
var report = await masterWorkflow.ExecuteFullPipelineAsync(...);

// Validate
var validation = transitioner.ValidatePipelineState(
    researchOutput, analysisOutput, topic);

if (!validation.IsValid)
{
    logger.LogWarning("Validation errors: {Errors}", 
        string.Join(", ", validation.Errors));
}

// Repair if needed
report = errorRecovery.ValidateAndRepairReportOutput(report, topic);
```

### Pattern 3: Statistics Tracking

```csharp
var report = await masterWorkflow.ExecuteFullPipelineAsync(...);

// Extract statistics
var researchStats = transitioner.GetResearchStatistics(researchOutput);
var analysisStats = transitioner.GetAnalysisStatistics(analysisOutput);

// Log metrics
metrics.RecordMetric("research.facts", researchStats.TotalFacts);
metrics.RecordMetric("analysis.insights", analysisStats.TotalInsights);
metrics.RecordMetric("report.quality", report.QualityScore);
```

### Pattern 4: State Persistence

```csharp
var researchId = Guid.NewGuid().ToString();

var report = await masterWorkflow.ExecuteFullPipelineWithStateAsync(
    researcherAgent, analystAgent, reportAgent,
    transitioner, errorRecovery, stateService,
    topic, brief, researchId
);

// Query state later
var state = await stateService.GetResearchStateAsync(researchId);
```

### Pattern 5: Streaming Updates

```csharp
await foreach (var message in masterWorkflow.StreamFullPipelineAsync(...))
{
    // Update UI
    await hub.Clients.All.SendAsync("Progress", message);
    
    // Log progress
    logger.LogInformation(message);
}
```

---

## 🔧 Configuration

### Minimal Configuration

```csharp
// appsettings.json
{
  "Ollama": {
    "Endpoint": "http://localhost:11434",
    "Model": "llama2"
  },
  "Lightning": {
    "Endpoint": "http://localhost:8000"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  }
}
```

### Service Registration (ASP.NET Core)

```csharp
// Program.cs
builder.Services.AddSingleton<OllamaService>();
builder.Services.AddSingleton<ToolInvocationService>();
builder.Services.AddSingleton<ILightningStateService, LightningStateService>();
builder.Services.AddSingleton<ResearcherAgent>();
builder.Services.AddSingleton<AnalystAgent>();
builder.Services.AddSingleton<ReportAgent>();
builder.Services.AddSingleton<StateTransitioner>();
builder.Services.AddSingleton<AgentErrorRecovery>();
builder.Services.AddSingleton<MasterWorkflow>();
```

---

## 📖 Next Steps

### Learn More
- [Integration Guide](PHASE5_INTEGRATION_GUIDE.md) - Complete integration details
- [API Reference](PHASE5_API_REFERENCE.md) - Full API documentation
- [Best Practices](PHASE5_BEST_PRACTICES.md) - Recommended patterns
- [Examples](PHASE5_EXAMPLES.md) - More code examples

### Advanced Topics
- State management strategies
- Error recovery customization
- Performance optimization
- Monitoring and metrics

---

## 💡 Tips

1. **Use logging** - Enable detailed logging for debugging
2. **Validate outputs** - Always validate before using results
3. **Handle errors** - Use try-catch for production code
4. **Track metrics** - Monitor performance and quality
5. **Stream progress** - For better user experience

---

## 🎉 You're Ready!

You now know how to:
- ✅ Execute the complete pipeline
- ✅ Track state and progress
- ✅ Handle errors gracefully
- ✅ Validate and repair outputs
- ✅ Monitor performance

Start building amazing research applications! 🚀
