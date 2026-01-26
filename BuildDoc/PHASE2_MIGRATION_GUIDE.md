# Phase 2: Microsoft.Agents.AI.Workflows Migration Guide

## Overview

This document outlines the plan to migrate from custom workflow abstractions to `Microsoft.Agents.AI.Workflows` preview APIs.

**Current Version:** Phase 1 (Custom abstractions)  
**Target Version:** Phase 2 (Microsoft.Agents.AI.Workflows)  
**Timeline:** 2-4 weeks (estimated)  
**Risk Level:** Low (backward compatible design)

## Migration Phases

### Phase 2a: Preparation (Week 1)
- Review Microsoft.Agents.AI.Workflows documentation
- Identify API mappings
- Design adapter layers
- Plan test strategy

### Phase 2b: Adapter Implementation (Weeks 2-3)
- Create adapter layer
- Implement AgentState bridge
- Add preview API wrappers
- Migrate to preview orchestrator

### Phase 2c: Testing & Validation (Week 4)
- Run all existing tests
- Test with preview APIs
- Performance validation
- Documentation update

### Phase 2d: Deployment (Week 5)
- Remove deprecated code
- Update DI configuration
- Final testing
- Production rollout

## API Mapping Reference

### WorkflowContext → AgentState

**Current (Phase 1):**
```csharp
var context = new WorkflowContext();
context.SetState("Key", value);
var value = context.GetState<T>("Key");
```

**Target (Phase 2):**
```csharp
// Microsoft.Agents.AI.Workflows
var state = new AgentState();
state["Key"] = value;  // Property access
var value = state["Key"];
```

### IWorkflowDefinition → Workflow<T>

**Current (Phase 1):**
```csharp
public class MasterWorkflowDefinition : IWorkflowDefinition
{
    public async Task<WorkflowExecutionResult> ExecuteAsync(
        WorkflowContext context,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

**Target (Phase 2):**
```csharp
public class MasterWorkflow : Workflow<AgentState>
{
    public override async Task<AgentRunResponse> RunAsync(
        AgentRunInput input,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### IWorkflowOrchestrator → WorkflowRunner

**Current (Phase 1):**
```csharp
var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);
```

**Target (Phase 2):**
```csharp
var runner = new WorkflowRunner();
var result = await runner.RunAsync(workflow, input);
```

### Streaming → AgentRunResponseUpdate

**Current (Phase 1):**
```csharp
await foreach (var update in definition.StreamExecutionAsync(context))
{
    // Handle WorkflowUpdate
}
```

**Target (Phase 2):**
```csharp
await foreach (var update in workflow.StreamAsync(input))
{
    // Handle AgentRunResponseUpdate
}
```

## Adapter Layer Design

### 1. WorkflowContextAdapter

```csharp
/// <summary>
/// Bridges WorkflowContext to Microsoft.Agents.AI.Workflows AgentState
/// </summary>
public class WorkflowContextAdapter
{
    private readonly WorkflowContext _context;
    private readonly AgentState _state;

    public WorkflowContextAdapter(WorkflowContext context)
    {
        _context = context;
        _state = ConvertToAgentState(context);
    }

    public AgentState ToAgentState() => _state;
    public WorkflowContext FromAgentState(AgentState state) => 
        ConvertFromAgentState(state);

    private AgentState ConvertToAgentState(WorkflowContext context)
    {
        var state = new AgentState();
        foreach (var kvp in context.State)
        {
            state[kvp.Key] = kvp.Value;
        }
        return state;
    }

    private WorkflowContext ConvertFromAgentState(AgentState state)
    {
        var context = new WorkflowContext();
        foreach (var kvp in state)
        {
            context.State[kvp.Key] = kvp.Value;
        }
        return context;
    }
}
```

### 2. WorkflowDefinitionAdapter

```csharp
/// <summary>
/// Adapts IWorkflowDefinition to Microsoft.Agents.AI.Workflows Workflow<T>
/// </summary>
public class WorkflowDefinitionAdapter : Workflow<AgentState>
{
    private readonly IWorkflowDefinition _definition;
    private readonly WorkflowContextAdapter _contextAdapter;

    public override async Task<AgentRunResponse> RunAsync(
        AgentRunInput input,
        CancellationToken cancellationToken)
    {
        // Convert AgentState to WorkflowContext
        var context = _contextAdapter.FromAgentState(input.State);

        // Execute using old API
        var result = await _definition.ExecuteAsync(context, cancellationToken);

        // Convert result back to AgentRunResponse
        return ConvertToAgentRunResponse(result);
    }

    public override async IAsyncEnumerable<AgentRunResponseUpdate> StreamAsync(
        AgentRunInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        var context = _contextAdapter.FromAgentState(input.State);

        await foreach (var update in _definition.StreamExecutionAsync(context, cancellationToken))
        {
            yield return ConvertToAgentRunResponseUpdate(update);
        }
    }

    private AgentRunResponse ConvertToAgentRunResponse(WorkflowExecutionResult result)
    {
        return new AgentRunResponse
        {
            IsComplete = result.Success,
            Messages = new[] { new ChatMessage(ChatRole.Assistant, result.Output?.ToString() ?? "") }
        };
    }

    private AgentRunResponseUpdate ConvertToAgentRunResponseUpdate(WorkflowUpdate update)
    {
        return new AgentRunResponseUpdate
        {
            Content = update.Content,
            Metadata = update.Metadata
        };
    }
}
```

### 3. OrchestratorAdapter

```csharp
/// <summary>
/// Adapts WorkflowOrchestrator to Microsoft.Agents.AI.Workflows WorkflowRunner
/// </summary>
public class OrchestratorAdapter
{
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly Dictionary<string, Workflow<AgentState>> _adaptedWorkflows;

    public OrchestratorAdapter(IWorkflowOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
        _adaptedWorkflows = new Dictionary<string, Workflow<AgentState>>();
        
        // Adapt all registered workflows
        foreach (var name in _orchestrator.GetRegisteredWorkflows())
        {
            var definition = _orchestrator.GetWorkflow(name);
            _adaptedWorkflows[name] = new WorkflowDefinitionAdapter(definition);
        }
    }

    public async Task<AgentRunResponse> ExecuteAsync(
        string workflowName,
        AgentState state,
        CancellationToken cancellationToken)
    {
        if (!_adaptedWorkflows.TryGetValue(workflowName, out var workflow))
        {
            throw new InvalidOperationException($"Workflow '{workflowName}' not found");
        }

        var input = new AgentRunInput { State = state };
        return await workflow.RunAsync(input, cancellationToken);
    }

    public IAsyncEnumerable<AgentRunResponseUpdate> StreamAsync(
        string workflowName,
        AgentState state,
        CancellationToken cancellationToken)
    {
        if (!_adaptedWorkflows.TryGetValue(workflowName, out var workflow))
        {
            throw new InvalidOperationException($"Workflow '{workflowName}' not found");
        }

        var input = new AgentRunInput { State = state };
        return workflow.StreamAsync(input, cancellationToken);
    }
}
```

## DI Configuration Strategy

### Phase 1 (Current)
```csharp
services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
services.AddSingleton<WorkflowPipelineOrchestrator>();
services.AddSingleton<MasterWorkflowDefinition>();
// ... register definitions
```

### Phase 2 (Preview APIs)
```csharp
// Option A: Keep both for gradual migration
services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
services.AddSingleton<OrchestratorAdapter>();
services.AddSingleton<WorkflowContextAdapter>();

// Option B: Full migration to preview APIs
services.AddSingleton<WorkflowRunner>();
services.AddSingleton<Workflow<AgentState>, MasterWorkflow>();
services.AddSingleton<Workflow<AgentState>, SupervisorWorkflow>();
services.AddSingleton<Workflow<AgentState>, ResearcherWorkflow>();
```

## Implementation Steps

### Step 1: Create Adapter Layer

```
DeepResearchAgent/Workflows/Adapters/
├── WorkflowContextAdapter.cs
├── WorkflowDefinitionAdapter.cs
├── OrchestratorAdapter.cs
└── PreviewApiExtensions.cs
```

### Step 2: Implement Adapters

- ✅ WorkflowContextAdapter - Convert between context types
- ✅ WorkflowDefinitionAdapter - Bridge definition interface
- ✅ OrchestratorAdapter - Wrap orchestrator
- ✅ Extension methods for migration helpers

### Step 3: Update Tests

```csharp
// Add adapter tests
public class WorkflowContextAdapterTests
{
    [Fact]
    public void Adapter_ConvertsContextToAgentState()
    {
        var context = new WorkflowContext();
        context.SetState("Key", "Value");
        
        var adapter = new WorkflowContextAdapter(context);
        var state = adapter.ToAgentState();
        
        Assert.Equal("Value", state["Key"]);
    }
}
```

### Step 4: Migrate DI Configuration

```csharp
// Program.cs
if (usePreviewApis)
{
    // Register preview API implementations
    services.AddSingleton<WorkflowRunner>();
    // ...
}
else
{
    // Register current implementations
    services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
    // ...
}
```

### Step 5: Gradual Migration

```csharp
// Step 1: Register both
services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
services.AddSingleton<OrchestratorAdapter>();

// Step 2: Use adapter in new code
var adapter = serviceProvider.GetRequiredService<OrchestratorAdapter>();
await adapter.ExecuteAsync("Master", state);

// Step 3: Migrate old code
// Eventually remove direct IWorkflowOrchestrator usage

// Step 4: Remove old implementations
// After all code migrated
```

## Testing Strategy

### Unit Tests for Adapters

```csharp
[Fact]
public void WorkflowContextAdapter_Converts_Correctly()
{
    // Test conversion
}

[Fact]
public async Task WorkflowDefinitionAdapter_Executes_Correctly()
{
    // Test execution
}
```

### Integration Tests

```csharp
[Fact]
public async Task Pipeline_Works_With_Preview_APIs()
{
    // Test end-to-end
}
```

### Backward Compatibility Tests

```csharp
[Fact]
public async Task Old_API_Still_Works_During_Migration()
{
    // Ensure old code continues working
}
```

## Risk Mitigation

| Risk | Mitigation |
|------|-----------|
| **API Changes** | Keep custom abstractions as fallback |
| **Performance** | Profile adapters before migration |
| **Breaking Changes** | Extensive testing and gradual rollout |
| **Integration Issues** | Test with preview APIs early |

## Rollback Plan

1. Keep current abstractions in separate branch
2. Maintain feature flags for API selection
3. Deploy adapters first, core migrations last
4. Monitor metrics during rollout
5. Quick rollback if issues detected

## Timeline

| Phase | Duration | Owner | Deliverables |
|-------|----------|-------|--------------|
| 2a - Preparation | 3-5 days | DevLead | Design docs |
| 2b - Implementation | 5-7 days | Dev Team | Adapters, tests |
| 2c - Validation | 3-5 days | QA | Test reports |
| 2d - Deployment | 2-3 days | DevOps | Release notes |

## Success Criteria

- ✅ All existing tests pass
- ✅ New adapter tests pass (100% coverage)
- ✅ Backward compatibility maintained
- ✅ Performance within 5% of current
- ✅ Documentation updated
- ✅ Zero breaking changes during migration
- ✅ Deployment with zero downtime

## References

- Microsoft.Agents.AI.Workflows Documentation (TBD - when preview released)
- Current Phase 1 implementation in `Workflows/Abstractions/`
- Test suite in `DeepResearchAgent.Tests/Workflows/Abstractions/`
- Architecture overview in `WORKFLOW_ABSTRACTION_GUIDE.md`

## Questions & Next Steps

1. **When will preview APIs stabilize?** - Monitor Microsoft announcements
2. **Should we wait or migrate incrementally?** - Gradual approach recommended
3. **How to handle version skew?** - Use adapter pattern for compatibility
4. **What about other agents?** - Apply same pattern to all workflows

## Contact

For questions about Phase 2 migration:
- Architecture: See `WORKFLOW_ABSTRACTION_GUIDE.md`
- Testing: See `TESTING_GUIDE.md`
- Implementation: See code in `Workflows/Adapters/`
