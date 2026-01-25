using DeepResearchAgent.Workflows.Abstractions;

namespace DeepResearchAgent.Workflows;

/// <summary>
/// Extension methods for simplified workflow execution through the orchestrator.
/// Provides fluent API for common workflow execution patterns.
/// </summary>
public static class WorkflowExtensions
{
    /// <summary>
    /// Execute a research workflow with a user query using the orchestrator.
    /// Provides a simplified entry point compared to direct workflow calls.
    /// </summary>
    public static async Task<WorkflowExecutionResult> ExecuteResearchAsync(
        this WorkflowPipelineOrchestrator pipeline,
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        return await pipeline.ExecuteCompleteResearchPipelineAsync(userQuery, cancellationToken);
    }

    /// <summary>
    /// Stream research workflow execution updates.
    /// </summary>
    public static IAsyncEnumerable<WorkflowUpdate> StreamResearchAsync(
        this WorkflowPipelineOrchestrator pipeline,
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        return pipeline.StreamCompleteResearchPipelineAsync(userQuery, cancellationToken);
    }

    /// <summary>
    /// Create a workflow context pre-configured for master workflow execution.
    /// </summary>
    public static WorkflowContext CreateMasterWorkflowContext(string userQuery)
    {
        var context = new WorkflowContext
        {
            ExecutionId = Guid.NewGuid().ToString(),
            CurrentStep = "Initialize"
        };
        context.SetState("UserQuery", userQuery);
        return context;
    }

    /// <summary>
    /// Create a workflow context pre-configured for supervisor workflow execution.
    /// </summary>
    public static WorkflowContext CreateSupervisorWorkflowContext(
        string researchBrief,
        string draftReport,
        int maxIterations = 5)
    {
        var context = new WorkflowContext
        {
            ExecutionId = Guid.NewGuid().ToString(),
            CurrentStep = "Initialize"
        };
        context.SetState("ResearchBrief", researchBrief);
        context.SetState("DraftReport", draftReport);
        context.SetState("MaxIterations", maxIterations);
        return context;
    }

    /// <summary>
    /// Create a workflow context pre-configured for researcher workflow execution.
    /// </summary>
    public static WorkflowContext CreateResearcherWorkflowContext(
        string topic,
        string? researchId = null)
    {
        var context = new WorkflowContext
        {
            ExecutionId = Guid.NewGuid().ToString(),
            CurrentStep = "Initialize"
        };
        context.SetState("Topic", topic);
        if (researchId != null)
        {
            context.SetState("ResearchId", researchId);
        }
        return context;
    }

    /// <summary>
    /// Set a deadline for workflow execution.
    /// </summary>
    public static WorkflowContext WithDeadline(
        this WorkflowContext context,
        TimeSpan timeout)
    {
        context.Deadline = DateTime.UtcNow.Add(timeout);
        return context;
    }

    /// <summary>
    /// Add metadata to workflow context.
    /// </summary>
    public static WorkflowContext WithMetadata(
        this WorkflowContext context,
        string key,
        string value)
    {
        context.Metadata[key] = value;
        return context;
    }

    /// <summary>
    /// Fluently add multiple metadata entries.
    /// </summary>
    public static WorkflowContext WithMetadata(
        this WorkflowContext context,
        Dictionary<string, string> metadata)
    {
        foreach (var (key, value) in metadata)
        {
            context.Metadata[key] = value;
        }
        return context;
    }

    /// <summary>
    /// Convert execution result to string summary.
    /// </summary>
    public static string ToSummary(this WorkflowExecutionResult result)
    {
        var lines = new List<string>
        {
            $"Status: {(result.Success ? "✓ Succeeded" : "✗ Failed")}",
            $"Duration: {result.Duration.TotalSeconds:F2}s",
            $"Steps Executed: {result.ExecutedSteps.Count}",
            $"Errors: {result.Errors.Count}"
        };

        if (!string.IsNullOrEmpty(result.ErrorMessage))
        {
            lines.Add($"Error Message: {result.ErrorMessage}");
        }

        if (result.Output != null)
        {
            var outputStr = result.Output.ToString();
            if (!string.IsNullOrEmpty(outputStr))
            {
                var preview = outputStr.Length > 100 
                    ? outputStr[..97] + "..." 
                    : outputStr;
                lines.Add($"Output: {preview}");
            }
        }

        return string.Join("\n", lines);
    }
}
