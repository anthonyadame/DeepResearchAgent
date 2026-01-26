namespace DeepResearchAgent.Api.DTOs.Requests.Services;

using DeepResearchAgent.Api.DTOs.Common;

/// <summary>
/// Request for LLM invocation - Raw call to language model.
/// </summary>
public class LlmInvokeRequest
{
    /// <summary>
    /// Chat messages to send to LLM.
    /// </summary>
    public required List<ChatMessageDto> Messages { get; set; }

    /// <summary>
    /// Optional session context.
    /// </summary>
    public SessionContextDto? Session { get; set; }

    /// <summary>
    /// Optional LLM model override.
    /// </summary>
    public string? Model { get; set; }

    /// <summary>
    /// Temperature for response generation (0.0-1.0).
    /// </summary>
    public double? Temperature { get; set; }

    /// <summary>
    /// Maximum tokens for response.
    /// </summary>
    public int? MaxTokens { get; set; }

    /// <summary>
    /// Request structured JSON output instead of freeform.
    /// </summary>
    public bool RequestStructuredOutput { get; set; } = false;

    /// <summary>
    /// JSON schema for structured output if requested.
    /// </summary>
    public string? OutputJsonSchema { get; set; }

    /// <summary>
    /// Timeout in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Enable detailed logging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;
}

/// <summary>
/// Chat message for LLM.
/// </summary>
public class ChatMessageDto
{
    public required string Role { get; set; }
    public required string Content { get; set; }
    public DateTime? Timestamp { get; set; }
}
