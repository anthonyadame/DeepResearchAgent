using System.Globalization;
using DeepResearchAgent.Models;
using DeepResearchAgent.Prompts;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.Telemetry;
using Microsoft.Extensions.Logging;

namespace DeepResearchAgent.Agents;

/// <summary>
/// ResearchBriefAgent: Transforms user intent into formal research brief.
/// 
/// Responsibility:
/// - Extract key research objectives from conversation
/// - Generate formal, unambiguous research brief
/// - Define scope constraints and boundaries
/// - Create the "guidance signal" for all research agents
/// 
/// Maps to Python's write_research_brief node (lines ~950 in rd-code.py)
/// Enhanced with metrics tracking for observability.
/// </summary>
public class ResearchBriefAgent
{
    private readonly OllamaService _llmService;
    private readonly ILogger<ResearchBriefAgent>? _logger;
    private readonly MetricsService _metrics;

    protected virtual string AgentName => "ResearchBriefAgent";

    public ResearchBriefAgent(
        OllamaService llmService, 
        ILogger<ResearchBriefAgent>? logger = null,
        MetricsService? metrics = null)
    {
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _logger = logger;
        _metrics = metrics ?? new MetricsService();
    }

    /// <summary>
    /// Generate a formal research brief from conversation history.
    /// This brief is used as the guidance signal for all research work.
    /// </summary>
    public async Task<ResearchQuestion> GenerateResearchBriefAsync(
        List<ChatMessage> conversationHistory,
        CancellationToken cancellationToken = default)
    {
        var stopwatch = _metrics.StartTimer();
        _metrics.RecordRequest(AgentName, "started");

        try
        {
            var messagesText = FormatMessagesToString(conversationHistory);
            var currentDate = GetTodayString();
            
            var prompt = PromptTemplates.TransformMessagesIntoResearchTopicPrompt
                .Replace("{messages}", messagesText)
                .Replace("{date}", currentDate);
            
            _logger?.LogInformation("ResearchBriefAgent: Generating research brief from {MessageCount} messages", 
                conversationHistory.Count);
            
            // Convert ChatMessage to OllamaChatMessage for the service
            var ollamaMessages = new List<OllamaChatMessage>
            {
                new OllamaChatMessage { Role = "user", Content = prompt }
            };
            
            var response = await _llmService.InvokeWithStructuredOutputAsync<ResearchQuestion>(
                ollamaMessages,
                cancellationToken: cancellationToken);
            
            _logger?.LogInformation(
                "ResearchBriefAgent: Research brief generated with {ObjectiveCount} objectives",
                response.Objectives?.Count ?? 0);
            
            _metrics.RecordRequest(AgentName, "succeeded", stopwatch.Elapsed.TotalMilliseconds);
            return response;
        }
        catch (Exception ex)
        {
            _metrics.RecordError(AgentName, ex.GetType().Name);
            _metrics.RecordRequest(AgentName, "failed", stopwatch.Elapsed.TotalMilliseconds);
            _logger?.LogError(ex, "ResearchBriefAgent: Error generating research brief");
            throw new InvalidOperationException("Failed to generate research brief from conversation", ex);
        }
    }

    /// <summary>
    /// Format messages into a readable string representation for the LLM.
    /// Similar to Python's get_buffer_string().
    /// </summary>
    private static string FormatMessagesToString(List<ChatMessage> messages)
    {
        if (messages == null || messages.Count == 0)
        {
            return "[No messages]";
        }

        var formattedMessages = new System.Text.StringBuilder();
        
        foreach (var msg in messages)
        {
            var roleLabel = msg.Role switch
            {
                "user" => "USER",
                "assistant" => "ASSISTANT",
                "system" => "SYSTEM",
                _ => msg.Role.ToUpper()
            };
            
            formattedMessages.AppendLine($"{roleLabel}: {msg.Content}");
            formattedMessages.AppendLine();
        }
        
        return formattedMessages.ToString().TrimEnd();
    }

    /// <summary>
    /// Get today's date in human-readable format.
    /// Similar to Python's get_today_str() function.
    /// </summary>
    private static string GetTodayString()
    {
        var today = DateTime.Now;
        // Format: "Mon Dec 25, 2024"
        return today.ToString("ddd MMM d, yyyy", CultureInfo.InvariantCulture);
    }
}
