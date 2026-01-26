namespace DeepResearchAgent.Api.DTOs.Requests.Agents;

using DeepResearchAgent.Api.DTOs.Common;

/// <summary>
/// Request for ClarifyAgent - Validates if user query is sufficiently detailed.
/// </summary>
public class ClarifyAgentRequest
{
    /// <summary>
    /// Conversation history to analyze for clarification needs.
    /// </summary>
    public required List<ChatMessageDto> ConversationHistory { get; set; }

    /// <summary>
    /// Optional session context.
    /// </summary>
    public SessionContextDto? Session { get; set; }

    /// <summary>
    /// Enable detailed logging.
    /// </summary>
    public bool EnableDetailedLogging { get; set; } = false;

    /// <summary>
    /// Timeout in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Chat message in conversation history.
/// </summary>
public class ChatMessageDto
{
    /// <summary>
    /// Message role: "user" or "assistant".
    /// </summary>
    public required string Role { get; set; }

    /// <summary>
    /// Message content.
    /// </summary>
    public required string Content { get; set; }

    /// <summary>
    /// When the message was sent.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
