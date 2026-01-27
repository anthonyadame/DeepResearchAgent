using Microsoft.AspNetCore.Mvc;
using DeepResearchAgent.Api.Services;
using DeepResearchAgent.Api.DTOs;
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
        _logger.LogInformation("Creating new chat session");

        try
        {
            var session = await _sessionService.CreateSessionAsync(request?.Title);
            return CreatedAtAction(nameof(GetSession), new { id = session.Id }, session);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating session");
            return BadRequest(new { error = "Failed to create session", details = ex.Message });
        }
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
    /// Send a message in a chat session
    /// </summary>
    [HttpPost("{sessionId}/query")]
    [ProducesResponseType(typeof(ChatMessage), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<ChatMessage>> SendMessage(
        [Required] string sessionId,
        [FromBody][Required] SendMessageRequest request)
    {
        _logger.LogInformation("Sending message to session: {SessionId}", sessionId);

        try
        {
            // Add user message to session
            var userMessage = new ChatMessage
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
            var assistantMessage = new ChatMessage
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
    /// Get chat history for a session
    /// </summary>
    [HttpGet("{sessionId}/history")]
    [ProducesResponseType(typeof(List<ChatMessage>), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<List<ChatMessage>>> GetHistory([Required] string sessionId)
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

    /// <summary>
    /// Upload a file to a chat session
    /// </summary>
    [HttpPost("{sessionId}/files")]
    [ProducesResponseType(typeof(FileUploadResponse), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<FileUploadResponse>> UploadFile(
        [Required] string sessionId,
        [FromForm][Required] IFormFile file)
    {
        _logger.LogInformation("Uploading file to session: {SessionId}", sessionId);

        try
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest(new { error = "No file provided" });
            }

            // TODO: Implement file upload logic
            // For now, return mock response
            var response = new FileUploadResponse
            {
                Id = Guid.NewGuid().ToString(),
                Name = file.FileName,
                Size = file.Length,
                ContentType = file.ContentType,
                UploadedAt = DateTime.UtcNow
            };

            _logger.LogInformation("File uploaded successfully: {FileName}", file.FileName);
            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error uploading file to session {SessionId}", sessionId);
            return StatusCode(500, new { error = "Failed to upload file", details = ex.Message });
        }
    }
}
