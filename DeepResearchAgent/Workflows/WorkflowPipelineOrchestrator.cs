using System.Runtime.CompilerServices;
using DeepResearchAgent.Workflows.Abstractions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeepResearchAgent.Workflows;

/// <summary>
/// Facade that manages the complete research pipeline using the workflow orchestrator.
/// Provides a unified interface for executing workflows and managing state across the pipeline.
/// </summary>
public class WorkflowPipelineOrchestrator
{
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly ILogger<WorkflowPipelineOrchestrator>? _logger;

    public WorkflowPipelineOrchestrator(
        IWorkflowOrchestrator orchestrator,
        ILogger<WorkflowPipelineOrchestrator>? logger = null)
    {
        _orchestrator = orchestrator ?? throw new ArgumentNullException(nameof(orchestrator));
        _logger = logger;
    }

    /// <summary>
    /// Execute the complete research pipeline: Master → Supervisor → Final Report.
    /// </summary>
    public async Task<WorkflowExecutionResult> ExecuteCompleteResearchPipelineAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        var pipelineResult = new WorkflowExecutionResult
        {
            ExecutedSteps = new List<string>()
        };
        var startTime = DateTime.UtcNow;

        try
        {
            _logger?.LogInformation("Starting complete research pipeline for query: {Query}", userQuery);

            // Create shared pipeline context
            var context = new WorkflowContext
            {
                ExecutionId = Guid.NewGuid().ToString(),
                Metadata = new Dictionary<string, string>
                {
                    { "PipelineStartTime", DateTime.UtcNow.ToString("O") }
                }
            };

            // Step 1: Master Workflow
            _logger?.LogInformation("Step 1: Executing Master Workflow");
            context.SetState("UserQuery", userQuery);

            var masterResult = await _orchestrator.ExecuteWorkflowAsync(
                "MasterWorkflow",
                context,
                cancellationToken
            );

            if (!masterResult.Success)
            {
                pipelineResult.Success = false;
                pipelineResult.ErrorMessage = $"Master workflow failed: {masterResult.ErrorMessage}";
                pipelineResult.Errors = masterResult.Errors;
                return pipelineResult;
            }

            pipelineResult.ExecutedSteps.AddRange(masterResult.ExecutedSteps);
            context.SharedContext["MasterResult"] = masterResult;

            _logger?.LogInformation("Master workflow completed successfully");

            // Final report is now in context
            var finalReport = context.GetState<string>("FinalReport");

            pipelineResult.Success = true;
            pipelineResult.Output = finalReport;
            pipelineResult.FinalContext = context;

            _logger?.LogInformation("Research pipeline completed successfully");
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Research pipeline failed");
            pipelineResult.Success = false;
            pipelineResult.ErrorMessage = $"Pipeline error: {ex.Message}";
            pipelineResult.Errors.Add(new WorkflowError
            {
                Step = "Pipeline",
                Message = ex.Message,
                ExceptionDetails = ex.ToString()
            });
        }
        finally
        {
            pipelineResult.Duration = DateTime.UtcNow - startTime;
        }

        return pipelineResult;
    }

    /// <summary>
    /// Stream complete research pipeline with real-time updates.
    /// </summary>
    public async IAsyncEnumerable<WorkflowUpdate> StreamCompleteResearchPipelineAsync(
        string userQuery,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        await foreach (var update in StreamWithoutTryAsync(userQuery, cancellationToken))
        {
            yield return update;
        }
    }

    /// <summary>
    /// Internal streaming without try-catch to comply with C# yield restrictions.
    /// </summary>
    private async IAsyncEnumerable<WorkflowUpdate> StreamWithoutTryAsync(
        string userQuery,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        yield return new WorkflowUpdate
        {
            Type = WorkflowUpdateType.Message,
            Content = "Initializing research pipeline..."
        };

        var context = new WorkflowContext
        {
            ExecutionId = Guid.NewGuid().ToString()
        };
        context.SetState("UserQuery", userQuery);

        // Stream master workflow
        yield return new WorkflowUpdate
        {
            Type = WorkflowUpdateType.StepStarted,
            StepName = "MasterWorkflow",
            Content = "Starting master workflow..."
        };

        await foreach (var update in _orchestrator.StreamWorkflowAsync(
            "MasterWorkflow",
            context,
            cancellationToken))
        {
            yield return update;
        }

        yield return new WorkflowUpdate
        {
            Type = WorkflowUpdateType.Completed,
            Content = "Research pipeline completed successfully"
        };
    }

    /// <summary>
    /// Get information about registered workflows.
    /// </summary>
    public Dictionary<string, string> GetWorkflowInfo()
    {
        var info = new Dictionary<string, string>();
        foreach (var name in _orchestrator.GetRegisteredWorkflows())
        {
            var workflow = _orchestrator.GetWorkflow(name);
            if (workflow != null)
            {
                info[name] = workflow.Description;
            }
        }
        return info;
    }
}
