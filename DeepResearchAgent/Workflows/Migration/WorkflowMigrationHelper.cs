using DeepResearchAgent.Workflows.Abstractions;
using DeepResearchAgent.Workflows.Adapters;
using DeepResearchAgent.Workflows.Extensions;
using System.Runtime.CompilerServices;

namespace DeepResearchAgent.Workflows.Migration;

/// <summary>
/// Utilities for migrating from Phase 1 (custom abstractions) to Phase 2 (preview APIs).
/// Provides helpers for gradual, zero-downtime migration.
/// </summary>
public class WorkflowMigrationHelper
{
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly OrchestratorAdapter? _adapter;

    public WorkflowMigrationHelper(
        IWorkflowOrchestrator orchestrator,
        OrchestratorAdapter? adapter = null)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _adapter = adapter;
    }

    /// <summary>
    /// Check if adaptation is available (adapter registered).
    /// Used to determine migration capability.
    /// </summary>
    public bool IsAdaptationAvailable => _adapter != null;

    /// <summary>
    /// Get list of workflows available in Phase 1.
    /// </summary>
    public IReadOnlyList<string> GetPhase1Workflows()
    {
        var workflows = _orchestrator.GetRegisteredWorkflows();
        return workflows.Count > 0 ? workflows : new List<string>();
    }

    /// <summary>
    /// Get list of workflows available for Phase 2 (adapted).
    /// Returns empty if adapter not registered.
    /// </summary>
    public IReadOnlyList<string> GetPhase2Workflows()
    {
        if (_adapter == null) return new List<string>();
        return _adapter.GetAdaptedWorkflows();
    }

    /// <summary>
    /// Execute using Phase 1 API (original implementation).
    /// </summary>
    public async Task<WorkflowExecutionResult> ExecutePhase1Async(
        string workflowName,
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        return await _orchestrator.ExecuteWorkflowAsync(workflowName, context, cancellationToken);
    }

    /// <summary>
    /// Execute using Phase 2 API (adapter implementation).
    /// Throws if adapter not registered.
    /// </summary>
    public async Task<OrchestratorAdapterResult> ExecutePhase2Async(
        string workflowName,
        Dictionary<string, object> agentState,
        CancellationToken cancellationToken = default)
    {
        if (_adapter == null)
        {
            throw new InvalidOperationException(
                "Adapter not registered. Register with AddWorkflowAdaptersFromOrchestrator()");
        }

        return await _adapter.ExecuteAsync(workflowName, agentState, cancellationToken);
    }

    /// <summary>
    /// Execute with fallback: try Phase 2, fall back to Phase 1.
    /// Enables safe gradual migration.
    /// </summary>
    public async Task<(bool Success, string? ErrorMessage, object? Output)> ExecuteWithFallbackAsync(
        string workflowName,
        WorkflowContext context,
        CancellationToken cancellationToken = default)
    {
        // Try Phase 2 first if available
        if (_adapter != null)
        {
            try
            {
                var agentState = context.ToAgentState();
                var result = await _adapter.ExecuteAsync(workflowName, agentState, cancellationToken);
                return (result.Success, result.ErrorMessage, result.Output);
            }
            catch
            {
                // Fall back to Phase 1
            }
        }

        // Fall back to Phase 1
        var phase1Result = await _orchestrator.ExecuteWorkflowAsync(workflowName, context, cancellationToken);
        return (phase1Result.Success, phase1Result.ErrorMessage, phase1Result.Output);
    }

    /// <summary>
    /// Stream using Phase 1 API (original implementation).
    /// </summary>
    public async IAsyncEnumerable<WorkflowUpdate> StreamPhase1Async(
        string workflowName,
        WorkflowContext context,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in _orchestrator.StreamWorkflowAsync(workflowName, context, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>
    /// Stream using Phase 2 API (adapter implementation).
    /// Throws if adapter not registered.
    /// </summary>
    public async IAsyncEnumerable<OrchestratorAdapterUpdate> StreamPhase2Async(
        string workflowName,
        Dictionary<string, object> agentState,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_adapter == null)
        {
            throw new InvalidOperationException(
                "Adapter not registered. Register with AddWorkflowAdaptersFromOrchestrator()");
        }

        await foreach (var update in _adapter.StreamAsync(workflowName, agentState, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>
    /// Get migration status: which phase each workflow is on.
    /// </summary>
    public Dictionary<string, string> GetMigrationStatus()
    {
        var status = new Dictionary<string, string>();

        // Mark Phase 1 workflows
        foreach (var workflow in GetPhase1Workflows())
        {
            status[workflow] = "Phase 1";
        }

        // Mark Phase 2 workflows
        foreach (var workflow in GetPhase2Workflows())
        {
            if (status.ContainsKey(workflow))
            {
                status[workflow] = "Phase 1 & 2 (Ready to migrate)";
            }
            else
            {
                status[workflow] = "Phase 2";
            }
        }

        return status;
    }

    /// <summary>
    /// Get migration recommendations.
    /// </summary>
    public List<string> GetMigrationRecommendations()
    {
        var recommendations = new List<string>();

        if (!IsAdaptationAvailable)
        {
            recommendations.Add("Register adapter with AddWorkflowAdaptersFromOrchestrator()");
            recommendations.Add("Use ExecutePhase1Async() or ExecuteWithFallbackAsync()");
        }
        else
        {
            recommendations.Add("Adapters registered and ready");
            recommendations.Add("Use ExecutePhase2Async() for new code");
            recommendations.Add("Use ExecuteWithFallbackAsync() for gradual migration");
        }

        var phase1Count = GetPhase1Workflows().Count;
        var phase2Count = GetPhase2Workflows().Count;

        if (phase2Count == phase1Count && phase2Count > 0)
        {
            recommendations.Add("All workflows available in Phase 2 - ready for full migration");
        }
        else if (phase2Count > 0)
        {
            recommendations.Add($"Partial migration: {phase2Count}/{phase1Count} workflows adapted");
        }

        return recommendations;
    }
}
