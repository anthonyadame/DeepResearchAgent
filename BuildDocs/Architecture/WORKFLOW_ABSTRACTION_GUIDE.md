# Workflow Abstraction Refactor - Implementation Guide

## Overview

This refactor introduces **Microsoft.Agents.AI.Workflows-compatible abstractions** for the Deep Research Agent pipeline. It bridges existing workflows (`MasterWorkflow`, `SupervisorWorkflow`, `ResearcherWorkflow`) with a standardized workflow definition and orchestration pattern.

## Architecture

### 1. Core Abstractions (`Workflows/Abstractions/`)

#### `IWorkflowDefinition` (IWorkflowDefinition.cs)
Defines the contract all workflows must implement:
- **ExecuteAsync**: Non-streaming workflow execution with state context
- **StreamExecutionAsync**: Real-time progress updates via `IAsyncEnumerable<WorkflowUpdate>`
- **ValidateContext**: Pre-execution validation of workflow state

**Key Types:**
- `WorkflowContext`: Shared execution state across workflow pipeline
  - `State`: Workflow-specific key-value store (type-safe getters/setters)
  - `SharedContext`: Pipeline-wide shared data
  - `Metadata`: Tracking and observability fields
  - `Deadline`: Optional timeout enforcement
- `WorkflowExecutionResult`: Result, errors, execution trace
- `WorkflowUpdate`: Streaming updates (typed by `WorkflowUpdateType`)
- `ValidationResult`: Pre-execution validation feedback

#### `IWorkflowOrchestrator` (IWorkflowOrchestrator.cs)
Manages workflow lifecycle and execution:
- `RegisterWorkflow()`: Register workflow definitions
- `ExecuteWorkflowAsync()`: Run a named workflow
- `StreamWorkflowAsync()`: Stream workflow with real-time updates
- `GetRegisteredWorkflows()`: List all registered workflows

**Implementation:** `WorkflowOrchestrator` provides a singleton registry and lifecycle management.

### 2. Workflow Definitions (Abstractions/)

Each workflow wraps an existing implementation:

#### `MasterWorkflowDefinition`
- Wraps: `MasterWorkflow`
- Steps: Clarify → Brief → Draft → Supervise → FinalReport
- Input: `UserQuery` (state key)
- Output: `FinalReport`

#### `SupervisorWorkflowDefinition`
- Wraps: `SupervisorWorkflow`
- Steps: Brain → Tools → Evaluate → RedTeam → ContextPruner
- Inputs: `ResearchBrief`, `DraftReport`, `MaxIterations`
- Output: `RefinedSummary`

#### `ResearcherWorkflowDefinition`
- Wraps: `ResearcherWorkflow`
- Steps: LLMCall → ToolExecution → Compress → Persist
- Input: `Topic`
- Output: `ExtractedFacts`

### 3. Pipeline Orchestrator (WorkflowPipelineOrchestrator.cs)

Facade for executing the complete research pipeline:
- `ExecuteCompleteResearchPipelineAsync()`: Runs Master workflow end-to-end
- `StreamCompleteResearchPipelineAsync()`: Streams pipeline progress

### 4. Helper Extensions (WorkflowExtensions.cs)

Fluent API for common patterns:
```csharp
var context = WorkflowExtensions
    .CreateMasterWorkflowContext(userQuery)
    .WithDeadline(TimeSpan.FromMinutes(30))
    .WithMetadata("user_id", "12345");
```

## Usage

### Basic Execution (Non-Streaming)

```csharp
// Inject the pipeline orchestrator
var pipeline = serviceProvider.GetRequiredService<WorkflowPipelineOrchestrator>();

// Execute complete research
var result = await pipeline.ExecuteCompleteResearchPipelineAsync(
    "Research quantum computing advances",
    CancellationToken.None
);

if (result.Success)
{
    Console.WriteLine(result.Output); // Final report
}
else
{
    Console.WriteLine($"Error: {result.ErrorMessage}");
}
```

### Streaming Execution

```csharp
// Stream with real-time updates
await foreach (var update in pipeline.StreamCompleteResearchPipelineAsync(
    "Research AI safety",
    CancellationToken.None))
{
    Console.WriteLine($"[{update.Type}] {update.Content}");
}
```

### Direct Workflow Execution

```csharp
var orchestrator = serviceProvider.GetRequiredService<IWorkflowOrchestrator>();

var context = WorkflowExtensions.CreateMasterWorkflowContext("Your query");
var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);
```

### State Management

```csharp
// Create context with typed state
var context = new WorkflowContext();
context.SetState("UserQuery", "Research topic");
context.SetState("MaxIterations", 5);

// Retrieve typed state
var query = context.GetState<string>("UserQuery");
var iterations = context.GetState<int?>("MaxIterations");

// Shared context across pipeline
context.SharedContext["custom_key"] = "custom_value";
```

## DI Registration

In `Program.cs`:

```csharp
// Register workflow implementations
services.AddSingleton<ResearcherWorkflow>();
services.AddSingleton<SupervisorWorkflow>();
services.AddSingleton<MasterWorkflow>();

// Register workflow abstractions
services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
services.AddSingleton<WorkflowPipelineOrchestrator>();
services.AddSingleton<MasterWorkflowDefinition>();
services.AddSingleton<SupervisorWorkflowDefinition>();
services.AddSingleton<ResearcherWorkflowDefinition>();

// Build and initialize
var serviceProvider = services.BuildServiceProvider();
var orchestrator = serviceProvider.GetRequiredService<IWorkflowOrchestrator>();
orchestrator.RegisterWorkflow(serviceProvider.GetRequiredService<MasterWorkflowDefinition>());
orchestrator.RegisterWorkflow(serviceProvider.GetRequiredService<SupervisorWorkflowDefinition>());
orchestrator.RegisterWorkflow(serviceProvider.GetRequiredService<ResearcherWorkflowDefinition>());
```

## Migration Path to Microsoft.Agents.AI.Workflows

### Current State
- ✅ Workflow definitions conform to a standardized interface
- ✅ Execution is orchestrator-driven
- ✅ State is managed through a context object
- ✅ Streaming updates use a type-safe envelope

### Next Steps (Future)
1. **Step 1**: Map `WorkflowContext` → `Microsoft.Agents.AI.Workflows.AgentState`
2. **Step 2**: Implement `IWorkflowDefinition` as wrapper around `Workflow<TState>`
3. **Step 3**: Replace `WorkflowOrchestrator` with `WorkflowRunner` from the preview package
4. **Step 4**: Adopt preview streaming/update patterns

## Key Benefits

| Aspect | Benefit |
|--------|---------|
| **Standardization** | All workflows implement the same interface |
| **Testability** | Workflows can be tested independently with mock orchestrators |
| **Observability** | Unified logging, tracing, and metrics via `WorkflowExecutionResult` |
| **Composability** | Workflows can be chained, nested, or run conditionally |
| **Framework Compatibility** | Ready to adopt `Microsoft.Agents.AI.Workflows` preview APIs |
| **Backward Compatibility** | Existing direct calls still work (e.g., `masterWorkflow.RunAsync()`) |

## File Structure

```
Workflows/
├── Abstractions/
│   ├── IWorkflowDefinition.cs           # Core contracts
│   ├── IWorkflowOrchestrator.cs         # Orchestrator interface
│   ├── MasterWorkflowDefinition.cs      # Master workflow adapter
│   ├── SupervisorWorkflowDefinition.cs  # Supervisor workflow adapter
│   └── ResearcherWorkflowDefinition.cs  # Researcher workflow adapter
├── WorkflowPipelineOrchestrator.cs      # Pipeline facade
└── WorkflowExtensions.cs                # Fluent API helpers
```

## Streaming and Error Handling

**Note on C# Restrictions**: `yield return` cannot appear in `try-catch` blocks. To work around this:
- Streaming methods delegate to internal iterator methods without try-catch
- Error handling is deferred to the orchestrator or caller
- Use `result.Errors` for structured error reporting from non-streaming execution

## Examples

### Full Pipeline with Timeout

```csharp
var context = WorkflowExtensions
    .CreateMasterWorkflowContext("Analyze cryptocurrency trends")
    .WithDeadline(TimeSpan.FromMinutes(15))
    .WithMetadata("source", "api")
    .WithMetadata("user_id", "user123");

var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);
Console.WriteLine(result.ToSummary());
```

### Getting Workflow Information

```csharp
var workflows = pipeline.GetWorkflowInfo();
foreach (var (name, description) in workflows)
{
    Console.WriteLine($"{name}: {description}");
}
```

## Testing

Each workflow definition can be unit-tested:

```csharp
[Fact]
public async Task MasterWorkflowDefinition_WithValidQuery_ReturnsSuccess()
{
    var context = WorkflowExtensions.CreateMasterWorkflowContext("Test query");
    var definition = new MasterWorkflowDefinition(mockWorkflow, mockLogger);
    
    var result = await definition.ExecuteAsync(context);
    
    Assert.True(result.Success);
    Assert.NotNull(result.Output);
}
```
