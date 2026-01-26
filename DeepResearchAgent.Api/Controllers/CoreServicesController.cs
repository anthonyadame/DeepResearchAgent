namespace DeepResearchAgent.Api.Controllers;

using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using DeepResearchAgent.Api.DTOs.Common;
using DeepResearchAgent.Api.DTOs.Requests.Services;
using DeepResearchAgent.Api.DTOs.Responses.Services;
using DeepResearchAgent.Api.Services;

/// <summary>
/// API endpoints for core infrastructure services.
/// Tier 3: LLM, search, scraping, state management, vectors.
/// </summary>
[ApiController]
[Route("api/services")]
[Produces("application/json")]
public class CoreServicesController : ControllerBase
{
    private readonly ICoreService _coreService;
    private readonly ILogger<CoreServicesController> _logger;

    public CoreServicesController(ICoreService coreService, ILogger<CoreServicesController> logger)
    {
        _coreService = coreService;
        _logger = logger;
    }

    /// <summary>Invoke LLM with chat messages.</summary>
    [HttpPost("llm/invoke")]
    public async Task<IActionResult> InvokeLlm([FromBody] LlmInvokeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("LLM invocation endpoint called");
            var response = await _coreService.InvokeLlmAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in LLM invocation");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "LLM invocation failed", Code = "LLM_ERROR" });
        }
    }

    /// <summary>Perform web search.</summary>
    [HttpPost("search")]
    public async Task<IActionResult> Search([FromBody] SearchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Search endpoint called for query: {Query}", request.Query);
            var response = await _coreService.SearchAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in search");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Search failed", Code = "SEARCH_ERROR" });
        }
    }

    /// <summary>Scrape web page content.</summary>
    [HttpPost("scrape")]
    public async Task<IActionResult> Scrape([FromBody] ScrapeRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Scrape endpoint called for URL: {Url}", request.Url);
            var response = await _coreService.ScrapeAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in scraping");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Scraping failed", Code = "SCRAPE_ERROR" });
        }
    }

    /// <summary>Manage agent state - create or update.</summary>
    [HttpPost("state/manage")]
    public async Task<IActionResult> ManageState([FromBody] StateManagementRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("State management endpoint called");
            var response = await _coreService.ManageStateAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in state management");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "State management failed", Code = "STATE_ERROR" });
        }
    }

    /// <summary>Query agent state.</summary>
    [HttpPost("state/query")]
    public async Task<IActionResult> QueryState([FromBody] StateQueryRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("State query endpoint called");
            var response = await _coreService.QueryStateAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in state query");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "State query failed", Code = "STATE_ERROR" });
        }
    }

    /// <summary>Key-value store operations.</summary>
    [HttpPost("store")]
    public async Task<IActionResult> StoreOperation([FromBody] StoreOperationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Store operation endpoint called: {Operation}", request.Operation);
            var response = await _coreService.StoreOperationAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in store operation");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Store operation failed", Code = "STORE_ERROR" });
        }
    }

    /// <summary>Vector similarity search.</summary>
    [HttpPost("vectors/search")]
    public async Task<IActionResult> SearchVectors([FromBody] VectorSearchRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Vector search endpoint called");
            var response = await _coreService.SearchVectorsAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in vector search");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Vector search failed", Code = "VECTOR_ERROR" });
        }
    }

    /// <summary>Add vector to database.</summary>
    [HttpPost("vectors/add")]
    public async Task<IActionResult> AddVector([FromBody] VectorAddRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Add vector endpoint called");
            var response = await _coreService.AddVectorAsync(request, cancellationToken);
            return CreatedAtAction(nameof(SearchVectors), response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error adding vector");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Add vector failed", Code = "VECTOR_ERROR" });
        }
    }

    /// <summary>Invoke research tool.</summary>
    [HttpPost("tools/invoke")]
    public async Task<IActionResult> InvokeTool([FromBody] ToolInvocationRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Tool invocation endpoint called: {ToolName}", request.ToolName);
            var response = await _coreService.InvokeToolAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error in tool invocation");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Tool invocation failed", Code = "TOOL_ERROR" });
        }
    }

    /// <summary>Query metrics.</summary>
    [HttpPost("metrics")]
    public async Task<IActionResult> GetMetrics([FromBody] MetricsRequest request, CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogInformation("Metrics endpoint called: {Operation}", request.Operation);
            var response = await _coreService.GetMetricsAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting metrics");
            return StatusCode(StatusCodes.Status500InternalServerError, 
                new ApiError { Message = "Metrics query failed", Code = "METRICS_ERROR" });
        }
    }
}
