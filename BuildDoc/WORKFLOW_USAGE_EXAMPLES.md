# Workflow Abstractions - Documentation Guide

## Quick Start

### Basic Workflow Execution

```csharp
// Inject the pipeline orchestrator
var pipeline = serviceProvider.GetRequiredService<WorkflowPipelineOrchestrator>();

// Execute complete research pipeline
var result = await pipeline.ExecuteCompleteResearchPipelineAsync(
    "Research quantum computing advances",
    CancellationToken.None
);

if (result.Success)
{
    Console.WriteLine("Final Report:");
    Console.WriteLine(result.Output);
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

### Streaming with Real-Time Updates

```csharp
// Stream pipeline with progress updates
await foreach (var update in pipeline.StreamCompleteResearchPipelineAsync(
    "Your research topic",
    CancellationToken.None))
{
    Console.WriteLine($"[{update.Type}] {update.Content}");
    if (update.Progress.HasValue)
    {
        Console.WriteLine($"Progress: {update.Progress}%");
    }
}
```

### With Timeout and Metadata

```csharp
var context = WorkflowExtensions
    .CreateMasterWorkflowContext("Research AI safety implications")
    .WithDeadline(TimeSpan.FromMinutes(30))
    .WithMetadata("user_id", "user123")
    .WithMetadata("source", "api");

var orchestrator = serviceProvider.GetRequiredService<IWorkflowOrchestrator>();
var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);

Console.WriteLine(result.ToSummary());
```

## Working with Contexts

### Creating Contexts

```csharp
// Master workflow context
var masterContext = WorkflowExtensions
    .CreateMasterWorkflowContext("Your query");

// Supervisor workflow context
var supervisorContext = WorkflowExtensions
    .CreateSupervisorWorkflowContext(
        researchBrief: "Topic brief",
        draftReport: "Initial draft",
        maxIterations: 5
    );

// Researcher workflow context
var researcherContext = WorkflowExtensions
    .CreateResearcherWorkflowContext(
        topic: "Research topic",
        researchId: "research-001"
    );
```

### State Management

```csharp
var context = new WorkflowContext();

// Set state (type-safe)
context.SetState("UserQuery", "Your query");
context.SetState("MaxIterations", 5);
context.SetState("CustomData", new { Key = "Value" });

// Get state (type-safe with defaults)
var query = context.GetState<string>("UserQuery");  // Returns string or null
var iterations = context.GetState<int?>("MaxIterations");  // Returns int? or null

// Shared context (pipeline-wide)
context.SharedContext["global_data"] = "shared value";

// Metadata tracking
context.Metadata["source"] = "api";
context.Metadata["user_id"] = "123";
```

### Deadline Enforcement

```csharp
// Set 30-minute timeout
var context = new WorkflowContext()
    .WithDeadline(TimeSpan.FromMinutes(30));

// Check if exceeded
if (context.IsDeadlineExceeded)
{
    Console.WriteLine("Workflow exceeded deadline!");
}

// Get remaining time
var remaining = context.GetRemainingTime();
if (remaining.HasValue)
{
    Console.WriteLine($"Time remaining: {remaining.Value.TotalSeconds}s");
}
```

## Direct Workflow Access

### Using Individual Workflows

```csharp
// Access individual workflow definitions
var orchestrator = serviceProvider.GetRequiredService<IWorkflowOrchestrator>();

// Get registered workflows
var workflows = orchestrator.GetRegisteredWorkflows();
foreach (var name in workflows)
{
    Console.WriteLine($"Registered: {name}");
}

// Get workflow info
var info = pipeline.GetWorkflowInfo();
foreach (var (name, description) in info)
{
    Console.WriteLine($"{name}: {description}");
}

// Execute specific workflow
var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);
```

### Backward Compatibility

Existing direct workflow calls still work:

```csharp
// Old way (still works)
var report = await masterWorkflow.RunAsync("Your query");
await foreach (var update in masterWorkflow.StreamAsync("Your query"))
{
    Console.WriteLine(update);
}

// New way (recommended)
var pipeline = serviceProvider.GetRequiredService<WorkflowPipelineOrchestrator>();
var result = await pipeline.ExecuteCompleteResearchPipelineAsync("Your query");
```

## Error Handling

### Validation Errors

```csharp
var context = new WorkflowContext(); // No UserQuery set
var validation = masterDef.ValidateContext(context);

if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}

foreach (var warning in validation.Warnings)
{
    Console.WriteLine($"Warning: {warning}");
}
```

### Execution Errors

```csharp
var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);

if (!result.Success)
{
    Console.WriteLine($"Workflow failed: {result.ErrorMessage}");
    
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"  [{error.Step}] {error.Message}");
        if (error.ExceptionDetails != null)
        {
            Console.WriteLine($"  Details: {error.ExceptionDetails}");
        }
    }
}
else
{
    Console.WriteLine($"Steps executed: {string.Join(", ", result.ExecutedSteps)}");
    Console.WriteLine($"Duration: {result.Duration.TotalSeconds}s");
}
```

## Result Summarization

```csharp
var result = await pipeline.ExecuteCompleteResearchPipelineAsync("Topic");

// Get formatted summary
string summary = result.ToSummary();
Console.WriteLine(summary);

// Output example:
// Status: ✓ Succeeded
// Duration: 45.32s
// Steps Executed: 5
// Errors: 0
// Output: Final research report summary...
```

## Streaming Updates

```csharp
// Stream with progress tracking
await foreach (var update in pipeline.StreamCompleteResearchPipelineAsync("Topic"))
{
    switch (update.Type)
    {
        case WorkflowUpdateType.StepStarted:
            Console.WriteLine($"▶ {update.StepName}: {update.Content}");
            break;
        case WorkflowUpdateType.Progress:
            Console.WriteLine($"⌛ {update.Content}");
            if (update.Progress.HasValue)
                Console.WriteLine($"   [{new string('█', update.Progress.Value / 10)}{new string('░', 10 - update.Progress.Value / 10)}]");
            break;
        case WorkflowUpdateType.Completed:
            Console.WriteLine($"✓ {update.Content}");
            break;
        case WorkflowUpdateType.Error:
            Console.WriteLine($"✗ Error: {update.Content}");
            break;
    }
}
```

## Advanced Patterns

### Fluent API Chaining

```csharp
var context = WorkflowExtensions
    .CreateMasterWorkflowContext("AI Safety")
    .WithDeadline(TimeSpan.FromMinutes(45))
    .WithMetadata("priority", "high")
    .WithMetadata(new Dictionary<string, string>
    {
        { "user_id", "123" },
        { "source", "api" },
        { "version", "1.0" }
    });
```

### Pipeline Info Discovery

```csharp
var workflows = pipeline.GetWorkflowInfo();
var masterInfo = orchestrator.GetWorkflow("MasterWorkflow");

Console.WriteLine($"Name: {masterInfo.WorkflowName}");
Console.WriteLine($"Description: {masterInfo.Description}");
```

### Conditional Execution

```csharp
var context = WorkflowExtensions.CreateMasterWorkflowContext("Topic");

// Check validation before execution
var validation = orchestrator.GetWorkflow("MasterWorkflow")?.ValidateContext(context);
if (validation?.IsValid == true)
{
    var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);
}
```

## Testing Workflows

See `TESTING_GUIDE.md` for comprehensive unit test examples.

Quick example:

```csharp
[Fact]
public async Task MasterWorkflow_WithValidContext_ReturnsSuccess()
{
    // Arrange
    var context = WorkflowExtensions.CreateMasterWorkflowContext("Test query");
    var definition = new MasterWorkflowDefinition(mockWorkflow, mockLogger);

    // Act
    var result = await definition.ExecuteAsync(context);

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.Output);
}
```

## Architecture Overview

```
┌─────────────────────────────────────────────────────┐
│         WorkflowPipelineOrchestrator                 │
│  (High-level research pipeline facade)               │
└─────────────────┬───────────────────────────────────┘
                  │
                  ▼
┌─────────────────────────────────────────────────────┐
│         IWorkflowOrchestrator                        │
│  (Registry & execution runtime)                      │
└─────────────────┬───────────────────────────────────┘
                  │
        ┌─────────┼─────────┐
        ▼         ▼         ▼
┌─────────────┐ ┌─────────────────┐ ┌──────────────────┐
│Master       │ │Supervisor       │ │Researcher        │
│Definition   │ │Definition       │ │Definition        │
└────┬────────┘ └────┬────────────┘ └────┬─────────────┘
     │              │                     │
     ▼              ▼                     ▼
┌─────────────┐ ┌─────────────────┐ ┌──────────────────┐
│Master       │ │Supervisor       │ │Researcher        │
│Workflow     │ │Workflow         │ │Workflow          │
└─────────────┘ └─────────────────┘ └──────────────────┘
```

## Common Scenarios

### Scenario 1: Simple Research Query

```csharp
var pipeline = serviceProvider.GetRequiredService<WorkflowPipelineOrchestrator>();
var result = await pipeline.ExecuteResearchAsync("Research topic");
Console.WriteLine(result.Output);
```

### Scenario 2: Monitor Progress

```csharp
var progressReporter = new Progress<WorkflowUpdate>(update =>
{
    Console.WriteLine($"[{update.Type}] {update.Content}");
});

await foreach (var update in pipeline.StreamCompleteResearchPipelineAsync("Topic"))
{
    progressReporter.Report(update);
}
```

### Scenario 3: Handle Timeouts

```csharp
var context = WorkflowExtensions
    .CreateMasterWorkflowContext("Topic")
    .WithDeadline(TimeSpan.FromMinutes(15));

var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);
if (!result.Success)
{
    Console.WriteLine($"Failed: {result.ErrorMessage}");
}
```

### Scenario 4: Custom State Passing

```csharp
var context = new WorkflowContext();
context.SetState("UserQuery", "Your query");
context.SetState("CustomConfig", new { MaxResults = 10 });
context.Metadata["request_id"] = "req-123";

var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);
var customOutput = context.GetState<string>("ProcessedOutput");
```

## Troubleshooting

### Workflow Not Registered

```csharp
var workflow = orchestrator.GetWorkflow("NonExistent");
if (workflow == null)
{
    Console.WriteLine("Workflow not found!");
    var available = orchestrator.GetRegisteredWorkflows();
    Console.WriteLine($"Available: {string.Join(", ", available)}");
}
```

### Validation Failed

```csharp
var validation = definition.ValidateContext(context);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"Validation Error: {error}");
    }
}
```

### Execution Error

```csharp
var result = await orchestrator.ExecuteWorkflowAsync("Workflow", context);
if (!result.Success)
{
    // Check errors
    foreach (var error in result.Errors)
    {
        Console.WriteLine($"Error in {error.Step}: {error.Message}");
        Console.WriteLine($"Recoverable: {error.IsRecoverable}");
    }
}
```

## Best Practices

1. **Always validate context** before executing
2. **Use timeouts** for production workflows
3. **Stream for long operations** to provide feedback
4. **Handle errors** with specific error checks
5. **Use metadata** for tracking and diagnostics
6. **Test with mocks** before live execution
7. **Check backward compatibility** when refactoring
8. **Document custom state keys** in comments

## References

- `WORKFLOW_ABSTRACTION_GUIDE.md` - Architecture & design
- `UNIT_TESTS_IMPLEMENTATION_SUMMARY.md` - Test coverage
- `TESTING_GUIDE.md` - Unit test patterns
- `PHASE2_MIGRATION_GUIDE.md` - Future upgrade path
