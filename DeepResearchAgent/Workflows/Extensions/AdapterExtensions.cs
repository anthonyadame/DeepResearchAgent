using DeepResearchAgent.Workflows.Abstractions;
using DeepResearchAgent.Workflows.Adapters;
using System.Runtime.CompilerServices;

namespace DeepResearchAgent.Workflows.Extensions;

/// <summary>
/// Extension methods for working with workflow adapters.
/// Provides fluent API for adapter usage and migration patterns.
/// </summary>
public static class AdapterExtensions
{
    /// <summary>
    /// Convert WorkflowContext to AgentState (dictionary) for preview APIs.
    /// </summary>
    public static Dictionary<string, object> ToAgentState(this WorkflowContext context)
    {
        var adapter = new WorkflowContextAdapter(context);
        return adapter.ToAgentState();
    }

    /// <summary>
    /// Convert AgentState (dictionary) back to WorkflowContext.
    /// </summary>
    public static WorkflowContext FromAgentState(this Dictionary<string, object> state)
    {
        return WorkflowContextAdapter.FromAgentState(state);
    }

    /// <summary>
    /// Adapt a workflow definition to work with preview API pattern.
    /// </summary>
    public static WorkflowDefinitionAdapter AsAdapted(
        this IWorkflowDefinition definition)
    {
        return new WorkflowDefinitionAdapter(definition);
    }

    /// <summary>
    /// Create a context adapted for preview API execution.
    /// Returns AgentState dictionary representation.
    /// </summary>
    public static Dictionary<string, object> CreateAdaptedContext(
        this IWorkflowDefinition definition,
        Action<WorkflowContext> configure)
    {
        var context = new WorkflowContext();
        configure(context);
        return context.ToAgentState();
    }

    /// <summary>
    /// Execute workflow using adapter with preview API pattern.
    /// </summary>
    public static async Task<WorkflowAdapterResult> ExecuteAdapted(
        this IWorkflowDefinition definition,
        Dictionary<string, object> agentState,
        CancellationToken cancellationToken = default)
    {
        var adapter = definition.AsAdapted();
        return await adapter.ExecuteAsync(agentState, cancellationToken);
    }

    /// <summary>
    /// Stream workflow execution using adapter with preview API pattern.
    /// </summary>
    public static async IAsyncEnumerable<WorkflowAdapterUpdate> StreamAdapted(
        this IWorkflowDefinition definition,
        Dictionary<string, object> agentState,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var adapter = definition.AsAdapted();
        await foreach (var update in adapter.StreamAsync(agentState, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>
    /// Validate workflow context using adapter pattern.
    /// </summary>
    public static WorkflowAdapterValidation ValidateAdapted(
        this IWorkflowDefinition definition,
        Dictionary<string, object> agentState)
    {
        var adapter = definition.AsAdapted();
        return adapter.ValidateAsync(agentState);
    }
}
