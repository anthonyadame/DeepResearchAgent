using Microsoft.AspNetCore.Mvc;
using DeepResearchAgent.Api.Services;
using DeepResearchAgent.Api.DTOs;
using DeepResearchAgent.Api.DTOs.Requests.Chat;
using DeepResearchAgent.Api.DTOs.Responses.Chat;
using DeepResearchAgent.Models;
using System.ComponentModel.DataAnnotations;

namespace DeepResearchAgent.Api.Controllers;

/// <summary>
/// Chat Controller - Manages chat sessions and messaging for the UI
/// </summary>
[ApiController]
[Route("api/chat")]
[Produces("application/json")]
public class ChatController : ControllerBase
{
    private readonly IChatSessionService _sessionService;
    private readonly ChatIntegrationService _integrationService;
    private readonly ILogger<ChatController> _logger;

    public ChatController(
        IChatSessionService sessionService,
        ChatIntegrationService integrationService,
        ILogger<ChatController> logger)
    {
        _sessionService = sessionService;
        _integrationService = integrationService;
        _logger = logger;
    }

    /// <summary>
    /// Create a new chat session
    /// </summary>
    [HttpPost("sessions")]
    [ProducesResponseType(typeof(ChatSession), StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatSession>> CreateSession(
        [FromBody] CreateSessionRequest? request)
    {
        _logger.LogInformation("Creating new chat session with title: {Title}", request?.Title);

        try
        {
            var session = await _sessionService.CreateSessionAsync(request?.Title);
            _logger.LogInformation("Session created: Id={SessionId}, Title={Title}, Messages={MessageCount}", 
                session.Id, session.Title, session.Messages?.Count ?? 0);
            
            // DEBUG: Log the session object to see what we're trying to serialize
            _logger.LogInformation("Session object type: {Type}", session.GetType().FullName);
            _logger.LogInformation("Attempting to serialize session...");
            
            try
            {
                var testJson = System.Text.Json.JsonSerializer.Serialize(session);
                _logger.LogInformation("Serialized JSON length: {Length}", testJson.Length);
                _logger.LogInformation("Serialized JSON: {Json}", testJson);
            }
            catch (Exception serEx)
            {
                _logger.LogError(serEx, "Failed to serialize session!");
            }
            
            // Use Created() method - sets 201 status, Location header, and serializes body automatically
            return Created($"/api/chat/sessions/{session.Id}", session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return BadRequest(new { error = "Failed to create session", details = ex.Message });
        }
    }
    
    /// <summary>
    /// TEST: Return a simple hardcoded session to test serialization
    /// </summary>
    [HttpGet("test-session")]
    public IActionResult TestSession()
    {
        var testSession = new ChatSession
        {
            Id = "test-123",
            Title = "Test Session",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Messages = new List<DTOs.ChatMessage>(),
            Config = null
        };
        
        return Ok(testSession);
    }

    /// <summary>
    /// Get all chat sessions
    /// </summary>
    [HttpGet("sessions")]
    [ProducesResponseType(typeof(List<ChatSession>), StatusCodes.Status200OK)]
    public async Task<ActionResult<List<ChatSession>>> GetSessions()
    {
        _logger.LogInformation("Retrieving all chat sessions");

        try
        {
            var sessions = await _sessionService.GetSessionsAsync();
            return Ok(sessions);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving sessions");
            return StatusCode(500, new { error = "Failed to retrieve sessions", details = ex.Message });
        }
    }

    /// <summary>
    /// Get a specific chat session
    /// </summary>
    [HttpGet("sessions/{id}")]
    [ProducesResponseType(typeof(ChatSession), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatSession>> GetSession([Required] string id)
    {
        _logger.LogInformation("Retrieving session: {SessionId}", id);

        try
        {
            var session = await _sessionService.GetSessionAsync(id);
            if (session == null)
            {
                return NotFound(new { error = "Session not found", sessionId = id });
            }

            return Ok(session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving session {SessionId}", id);
            return StatusCode(500, new { error = "Failed to retrieve session", details = ex.Message });
        }
    }

    /// <summary>
    /// Delete a chat session
    /// </summary>
    [HttpDelete("sessions/{id}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> DeleteSession([Required] string id)
    {
        _logger.LogInformation("Deleting session: {SessionId}", id);

        try
        {
            await _sessionService.DeleteSessionAsync(id);
            return NoContent();
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Session not found", sessionId = id });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error deleting session {SessionId}", id);
            return StatusCode(500, new { error = "Failed to delete session", details = ex.Message });
        }
    }

    /// <summary>
    /// Send a message in a chat session (full pipeline - kept for backward compatibility)
    /// </summary>
    [HttpPost("{sessionId}/query")]
    [ProducesResponseType(typeof(DTOs.ChatMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<DTOs.ChatMessage>> SendMessage(
        [Required] string sessionId,
        [FromBody][Required] SendMessageRequest request)
    {
        _logger.LogInformation("Sending message to session: {SessionId}", sessionId);

        try
        {
            // Add user message to session
            var userMessage = new DTOs.ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Role = "user",
                Content = request.Message,
                Timestamp = DateTime.UtcNow,
                Metadata = null
            };

            await _sessionService.AddMessageAsync(sessionId, userMessage);

            // Process message through research workflow
            var assistantResponse = await _integrationService.ProcessChatMessageAsync(
                sessionId,
                request.Message,
                request.Config
            );

            // Add assistant message to session
            var assistantMessage = new DTOs.ChatMessage
            {
                Id = Guid.NewGuid().ToString(),
                Role = "assistant",
                Content = assistantResponse,
                Timestamp = DateTime.UtcNow,
                Metadata = new Dictionary<string, object>
                {
                    ["config"] = request.Config ?? new ResearchConfig()
                }
            };

            await _sessionService.AddMessageAsync(sessionId, assistantMessage);

            return Ok(assistantMessage);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Session not found", sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing message for session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to process message", details = ex.Message });
        }
    }

    /// <summary>
    /// Execute a single step of the research workflow (step-by-step mode).
    /// UI passes the current AgentState and gets back the updated state with formatted content.
    /// Each call executes exactly one step based on the state's current progress.
    /// 
    /// Flow:
    /// 1. User submits query → AgentState with NeedsQualityRepair=true → step 1 (clarify)
    /// 2. If clarification needed: return question, else set NeedsQualityRepair=false → step 2 (brief)
    /// 3. Step 2 → returns ResearchBrief
    /// 4. Step 3 → returns DraftReport
    /// 5. Step 4 → returns RawNotes (supervisor refinement)
    /// 6. Step 5 → returns FinalReport
    /// </summary>
    [HttpPost("step")]
    [ProducesResponseType(typeof(ChatStepResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<ChatStepResponse>> ExecuteStep(
        [FromBody][Required] ChatStepRequest request,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Executing workflow step - NeedsQualityRepair: {NeedsQualityRepair}", 
            request.CurrentState.NeedsQualityRepair);

        try
        {
            var response = await _integrationService.ProcessChatStepAsync(request, cancellationToken);
            return Ok(response);
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning(ex, "Invalid request for step execution");
            return BadRequest(new { error = "Invalid request", details = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error executing workflow step");
            return StatusCode(500, new { error = "Failed to execute step", details = ex.Message });
        }
    }

    /// <summary>
    /// Get chat history for a session
    /// </summary>
    [HttpGet("{sessionId}/history")]
    [ProducesResponseType(typeof(List<DTOs.ChatMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<DTOs.ChatMessage>>> GetHistory([Required] string sessionId)
    {
        _logger.LogInformation("Retrieving history for session: {SessionId}", sessionId);

        try
        {
            var history = await _sessionService.GetHistoryAsync(sessionId);
            return Ok(history);
        }
        catch (KeyNotFoundException)
        {
            return NotFound(new { error = "Session not found", sessionId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving history for session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to retrieve history", details = ex.Message });
        }
    }
}
