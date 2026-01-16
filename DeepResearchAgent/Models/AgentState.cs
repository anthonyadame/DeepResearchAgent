namespace DeepResearchAgent.Models;

/// <summary>
/// The main state for the full multi-agent system, which accumulates all final artifacts.
/// </summary>
public class AgentState
{
    /// <summary>
    /// User-facing messages (conversation history).
    /// </summary>
    public List<ChatMessage> Messages { get; set; } = new();

    /// <summary>
    /// The research brief generated from user input.
    /// </summary>
    public string? ResearchBrief { get; set; }

    /// <summary>
    /// Supervisor-specific message history.
    /// </summary>
    public List<ChatMessage> SupervisorMessages { get; set; } = new();

    /// <summary>
    /// Raw, unprocessed notes from all research activities.
    /// </summary>
    public List<string> RawNotes { get; set; } = new();

    /// <summary>
    /// The final, curated notes for the writer (post-processing).
    /// </summary>
    public List<string> Notes { get; set; } = new();

    /// <summary>
    /// The initial draft report (pre-diffusion).
    /// </summary>
    public string DraftReport { get; set; } = string.Empty;

    /// <summary>
    /// The final polished report (post-diffusion).
    /// </summary>
    public string FinalReport { get; set; } = string.Empty;
}

/// <summary>
/// The initial input state, which only contains the user's messages.
/// </summary>
public class AgentInputState
{
    public required List<ChatMessage> Messages { get; init; }
}
