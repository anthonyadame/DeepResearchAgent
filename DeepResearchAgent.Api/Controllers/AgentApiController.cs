namespace DeepResearchAgent.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeepResearchAgent.Api.DTOs.Common;
using DeepResearchAgent.Api.DTOs.Requests.Agents;
using DeepResearchAgent.Api.DTOs.Responses.Agents;
using DeepResearchAgent.Api.Services;

/// <summary>
/// API endpoints for agent operations.
/// Tier 2: Specialized agent operations.
/// </summary>
[ApiController]
[Route("api/agents")]
[Produces("application/json")]
public class AgentApiController : ControllerBase
{
    private readonly IAgentService _agentService;
    private readonly ILogger<AgentApiController> _logger;

    public AgentApiController(IAgentService agentService, ILogger<AgentApiController> logger)
    {
        _agentService = agentService;
        _logger = logger;
    }

    /// <summary>Execute ClarifyAgent - Validates if user query is sufficiently detailed.</summary>
    [HttpPost("clarify")]
    public async Task<IActionResult> ExecuteClarifyAgent([FromBody] ClarifyAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("ClarifyAgent endpoint called");
            var response = await _agentService.ExecuteClarifyAgentAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ClarifyAgent endpoint");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }

    /// <summary>Execute ResearchBriefAgent - Transforms user query into structured research brief.</summary>
    [HttpPost("brief")]
    public async Task<IActionResult> ExecuteResearchBriefAgent([FromBody] ResearchBriefAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("ResearchBriefAgent endpoint called");
            var response = await _agentService.ExecuteResearchBriefAgentAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResearchBriefAgent endpoint");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }

    /// <summary>Execute DraftReportAgent - Generates initial draft report from research brief.</summary>
    [HttpPost("draft")]
    public async Task<IActionResult> ExecuteDraftReportAgent([FromBody] DraftReportAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("DraftReportAgent endpoint called");
            var response = await _agentService.ExecuteDraftReportAgentAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in DraftReportAgent endpoint");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }

    /// <summary>Execute ResearcherAgent - Plans and executes research on a topic.</summary>
    [HttpPost("researcher")]
    public async Task<IActionResult> ExecuteResearcherAgent([FromBody] ResearcherAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("ResearcherAgent endpoint called for topic: {Topic}", request.Topic);
            var response = await _agentService.ExecuteResearcherAgentAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ResearcherAgent endpoint");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }

    /// <summary>Execute AnalystAgent - Analyzes research findings and synthesizes insights.</summary>
    [HttpPost("analyst")]
    public async Task<IActionResult> ExecuteAnalystAgent([FromBody] AnalystAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("AnalystAgent endpoint called");
            var response = await _agentService.ExecuteAnalystAgentAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in AnalystAgent endpoint");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }

    /// <summary>Execute ReportAgent - Generates final synthesized report from research and analysis.</summary>
    [HttpPost("report")]
    public async Task<IActionResult> ExecuteReportAgent([FromBody] ReportAgentRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("ReportAgent endpoint called");
            var response = await _agentService.ExecuteReportAgentAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in ReportAgent endpoint");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Internal server error", Code = "INTERNAL_ERROR" });
        }
    }
}
