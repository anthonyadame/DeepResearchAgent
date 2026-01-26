namespace DeepResearchAgent.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeepResearchAgent.Api.DTOs.Common;
using DeepResearchAgent.Api.DTOs.Requests.Workflows;
using DeepResearchAgent.Api.DTOs.Responses.Workflows;
using DeepResearchAgent.Api.Services;

/// <summary>
/// API endpoints for workflow orchestration.
/// Tier 1: High-level research pipelines.
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Produces("application/json")]
public class WorkflowsController : ControllerBase
{
    private readonly IWorkflowService _workflowService;
    private readonly ILogger<WorkflowsController> _logger;

    public WorkflowsController(IWorkflowService workflowService, ILogger<WorkflowsController> logger)
    {
        _workflowService = workflowService;
        _logger = logger;
    }

    /// <summary>
    /// Execute MasterWorkflow - Complete 5-step research pipeline.
    /// </summary>
    [HttpPost("master")]
    public async Task<IActionResult> ExecuteMasterWorkflow(
        [FromBody] MasterWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("MasterWorkflow endpoint called for query: {Query}", request.UserQuery);
            var response = await _workflowService.ExecuteMasterWorkflowAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Validation error in MasterWorkflow");
            return BadRequest(new ApiError { Message = ex.Message, Code = "VALIDATION_ERROR" });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in MasterWorkflow endpoint");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR", StackTrace = ex.StackTrace });
        }
    }

    /// <summary>
    /// Execute SupervisorWorkflow - Iterative refinement loop.
    /// </summary>
    [HttpPost("supervisor")]
    public async Task<IActionResult> ExecuteSupervisorWorkflow(
        [FromBody] SupervisorWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("SupervisorWorkflow endpoint called with {MaxIterations} iterations", request.MaxIterations);
            var response = await _workflowService.ExecuteSupervisorWorkflowAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in SupervisorWorkflow endpoint");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// Execute ResearcherWorkflow - Focused research execution.
    /// </summary>
    [HttpPost("researcher")]
    public async Task<IActionResult> ExecuteResearcherWorkflow(
        [FromBody] ResearcherWorkflowRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("ResearcherWorkflow endpoint called for topic: {Topic}", request.Topic);
            var response = await _workflowService.ExecuteResearcherWorkflowAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResearcherWorkflow endpoint");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// Get status of async workflow execution.
    /// </summary>
    [HttpGet("status/{jobId}")]
    public async Task<IActionResult> GetAsyncStatus(string jobId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting async status for job: {JobId}", jobId);
            var response = await _workflowService.GetAsyncStatusAsync(jobId, cancellationToken);
            
            if (!response.Success)
                return NotFound(new ApiError { Message = "Job not found", Code = "NOT_FOUND" });
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting async status");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }

    /// <summary>
    /// Get results of completed async workflow.
    /// </summary>
    [HttpGet("results/{jobId}")]
    public async Task<IActionResult> GetAsyncResults(string jobId, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Getting async results for job: {JobId}", jobId);
            var response = await _workflowService.GetAsyncResultsAsync(jobId, cancellationToken);
            
            if (!response.Success)
                return Accepted(new { message = "Job still processing" });
            
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting async results");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }
}
