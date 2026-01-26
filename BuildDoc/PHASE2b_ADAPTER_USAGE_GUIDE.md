# Phase 2b Adapter Usage Guide

## Quick Start

### 1. Register Adapters in DI

```csharp
// Program.cs
builder.Services.AddDualWorkflowSupport();
```

This registers:
- ✅ Phase 1 (original) workflows
- ✅ Phase 2 (adapter) implementations
- ✅ Migration helper

### 2. Use Phase 1 API (Original - Still Works)

```csharp
var orchestrator = serviceProvider.GetRequiredService<IWorkflowOrchestrator>();

var context = WorkflowExtensions.CreateMasterWorkflowContext("Your query");
var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);

Console.WriteLine(result.Output);
```

### 3. Use Phase 2 API (New - With Adapters)

```csharp
// Using extension methods
var context = new WorkflowContext();
context.SetState("Query", "Your query");

var agentState = context.ToAgentState();  // Convert to dictionary

var definition = GetWorkflowDefinition("MasterWorkflow");
var result = await definition.ExecuteAdapted(agentState);

Console.WriteLine(result.Output);
```

### 4. Gradual Migration (Best Approach)

```csharp
var helper = serviceProvider.GetRequiredService<WorkflowMigrationHelper>();

// Try Phase 2, fall back to Phase 1
var (success, error, output) = await helper.ExecuteWithFallbackAsync(
    "MasterWorkflow",
    context
);

if (success)
{
    Console.WriteLine(output);
}
else
{
    Console.WriteLine($"Error: {error}");
}
```

---

## Extension Methods

### Context Conversion

```csharp
// Phase 1 → Phase 2 (Context → AgentState)
var context = new WorkflowContext();
context.SetState("Query", "test");

var agentState = context.ToAgentState();
// Result: Dictionary<string, object> { ["Query"] = "test", ... }

// Phase 2 → Phase 1 (AgentState → Context)
var restoredContext = agentState.FromAgentState();
// Result: WorkflowContext with all data preserved
```

### Workflow Adaptation

```csharp
var definition = orchestrator.GetWorkflow("MasterWorkflow");

// Create adapted context
var state = definition.CreateAdaptedContext(ctx =>
{
    ctx.SetState("Query", "Your query");
    ctx.Metadata["source"] = "api";
});

// Execute with adapter
var result = await definition.ExecuteAdapted(state);

// Stream with adapter
await foreach (var update in definition.StreamAdapted(state))
{
    Console.WriteLine($"[{update.Type}] {update.Content}");
}

// Validate with adapter
var validation = definition.ValidateAdapted(state);
if (!validation.IsValid)
{
    foreach (var error in validation.Errors)
    {
        Console.WriteLine($"Error: {error}");
    }
}
```

---

## Migration Helper

### Check Adaptation Status

```csharp
var helper = serviceProvider.GetRequiredService<WorkflowMigrationHelper>();

// Check if adapters are registered
if (helper.IsAdaptationAvailable)
{
    Console.WriteLine("Adapters available for Phase 2");
}

// List workflows in each phase
var phase1Workflows = helper.GetPhase1Workflows();  // Original
var phase2Workflows = helper.GetPhase2Workflows();  // Adapted

Console.WriteLine($"Phase 1: {string.Join(", ", phase1Workflows)}");
Console.WriteLine($"Phase 2: {string.Join(", ", phase2Workflows)}");
```

### Get Migration Status

```csharp
// Get detailed status
var status = helper.GetMigrationStatus();
foreach (var (workflow, phase) in status)
{
    Console.WriteLine($"{workflow}: {phase}");
}

// Get recommendations
var recommendations = helper.GetMigrationRecommendations();
foreach (var rec in recommendations)
{
    Console.WriteLine($"→ {rec}");
}

// Example output:
// → Adapters registered and ready
// → Use ExecutePhase2Async() for new code
// → Use ExecuteWithFallbackAsync() for gradual migration
// → All workflows available in Phase 2 - ready for full migration
```

### Execute with Phase Strategies

```csharp
var helper = serviceProvider.GetRequiredService<WorkflowMigrationHelper>();

// Execute Phase 1 (original)
var phase1Result = await helper.ExecutePhase1Async("Master", context);

// Execute Phase 2 (adapter)
var phase2Result = await helper.ExecutePhase2Async("Master", agentState);

// Execute with fallback (Phase 2 → Phase 1)
var (success, error, output) = await helper.ExecuteWithFallbackAsync("Master", context);

// Stream Phase 1
await foreach (var update in helper.StreamPhase1Async("Master", context))
{
    Console.WriteLine(update.Content);
}

// Stream Phase 2
await foreach (var update in helper.StreamPhase2Async("Master", agentState))
{
    Console.WriteLine(update.Content);
}
```

---

## Registration Options

### Option 1: Dual Support (Recommended for Migration)

```csharp
builder.Services.AddDualWorkflowSupport();

// Provides:
// - IWorkflowOrchestrator (Phase 1)
// - WorkflowPipelineOrchestrator (Phase 1 facade)
// - All workflow definitions
// - OrchestratorAdapter (Phase 2)
// - WorkflowMigrationHelper
```

### Option 2: Only Adapters

```csharp
builder.Services.AddWorkflowAdapters(useAdapters: true);

// Provides:
// - WorkflowContextAdapter
// - WorkflowDefinitionAdapter
// - OrchestratorAdapter
```

### Option 3: Adapters from Orchestrator

```csharp
builder.Services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
builder.Services.AddWorkflowAdaptersFromOrchestrator();

// Provides:
// - IWorkflowOrchestrator (Phase 1)
// - OrchestratorAdapter (Phase 2, created from Phase 1)
```

---

## Migration Scenarios

### Scenario 1: New Code Using Phase 2

```csharp
// New feature - use Phase 2 API from the start
public class NewResearchService
{
    private readonly OrchestratorAdapter _adapter;

    public NewResearchService(OrchestratorAdapter adapter)
    {
        _adapter = adapter;
    }

    public async Task ResearchAsync(string topic)
    {
        var state = new Dictionary<string, object> { { "Topic", topic } };
        var result = await _adapter.ExecuteAsync("Researcher", state);
        
        return result.Output;
    }
}
```

### Scenario 2: Existing Code with Fallback

```csharp
// Existing feature - use fallback for safe migration
public class LegacyResearchService
{
    private readonly WorkflowMigrationHelper _helper;

    public async Task ResearchAsync(string topic)
    {
        var context = WorkflowExtensions.CreateResearcherWorkflowContext(topic);
        
        // Try Phase 2, fall back to Phase 1
        var (success, error, output) = await _helper.ExecuteWithFallbackAsync(
            "Researcher",
            context
        );

        if (!success)
            throw new InvalidOperationException(error);

        return output;
    }
}
```

### Scenario 3: Full Phase 2 Migration

```csharp
// After all code migrated to Phase 2
public class ModernResearchService
{
    private readonly OrchestratorAdapter _adapter;

    public async Task ResearchAsync(string topic)
    {
        var state = new Dictionary<string, object>
        {
            { "Topic", topic },
            { "__deadline", DateTime.UtcNow.AddMinutes(30) }
        };

        var result = await _adapter.ExecuteAsync("Researcher", state);
        return result.Output;
    }
}
```

---

## Best Practices

### 1. Use Extension Methods for Conversions

```csharp
// Good
var agentState = context.ToAgentState();
var restored = agentState.FromAgentState();

// Avoid
var adapter = new WorkflowContextAdapter(context);
var state = adapter.ToAgentState();
```

### 2. Use Migration Helper for Strategy Decision

```csharp
// Good - helper decides which phase to use
var (success, error, output) = await helper.ExecuteWithFallbackAsync(...);

// Less ideal - always specify phase
var result = await helper.ExecutePhase1Async(...);
```

### 3. Check Recommendations

```csharp
// Good - follow recommendations
var recommendations = helper.GetMigrationRecommendations();
foreach (var rec in recommendations)
{
    Console.WriteLine(rec);
}

// Apply recommended strategy
```

### 4. Use Extension Methods on Definitions

```csharp
// Good - fluent API
var result = await definition
    .CreateAdaptedContext(ctx => ctx.SetState("Query", "test"))
    .AsAdapted()
    .ExecuteAsync(...);

// Avoid - repetitive
var adapter = definition.AsAdapted();
var state = ...;
await adapter.ExecuteAsync(state);
```

---

## Troubleshooting

### Adapter Not Registered

```csharp
// Error: Adapter not registered
await helper.ExecutePhase2Async(...);

// Solution: Register adapters
builder.Services.AddDualWorkflowSupport();
```

### Phase 2 Execution Failed

```csharp
// Use fallback strategy
var (success, error, output) = await helper.ExecuteWithFallbackAsync(...);

if (!success)
{
    Console.WriteLine($"Failed: {error}");
    // Falls back to Phase 1 automatically
}
```

### State Conversion Issues

```csharp
// Ensure proper conversion
var context = new WorkflowContext();
context.SetState("Key", value);

var state = context.ToAgentState();  // Convert
var restored = state.FromAgentState();  // Restore

// Verify state preserved
Assert.Equal(value, restored.GetState<T>("Key"));
```

---

## Next Steps

1. **Register adapters** in Program.cs
2. **Write tests** for your migration strategy
3. **Monitor logs** during gradual migration
4. **Plan Phase 2c** testing and validation
5. **Prepare Phase 2d** deployment

---

**Ready for Phase 2c (Testing & Validation)?**
