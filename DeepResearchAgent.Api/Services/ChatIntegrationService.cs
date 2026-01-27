using DeepResearchAgent.Agents;
using DeepResearchAgent.Api.DTOs;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;

namespace DeepResearchAgent.Api.Services;

/// <summary>
/// Service that integrates chat messages with the research workflow
/// </summary>
public class ChatIntegrationService
{
    private readonly MasterWorkflow _masterWorkflow;
    private readonly ResearcherAgent _researcherAgent;
    private readonly AnalystAgent _analystAgent;
    private readonly ReportAgent _reportAgent;
    private readonly StateTransitioner _transitioner;
    private readonly AgentErrorRecovery _errorRecovery;
    private readonly ILogger<ChatIntegrationService> _logger;

    public ChatIntegrationService(
        MasterWorkflow masterWorkflow,
        ResearcherAgent researcherAgent,
        AnalystAgent analystAgent,
        ReportAgent reportAgent,
        StateTransitioner transitioner,
        AgentErrorRecovery errorRecovery,
        ILogger<ChatIntegrationService> logger)
    {
        _masterWorkflow = masterWorkflow;
        _researcherAgent = researcherAgent;
        _analystAgent = analystAgent;
        _reportAgent = reportAgent;
        _transitioner = transitioner;
        _errorRecovery = errorRecovery;
        _logger = logger;
    }

    /// <summary>
    /// Process a chat message through the research workflow
    /// </summary>
    /// <param name="sessionId">Chat session ID</param>
    /// <param name="userMessage">User's message/question</param>
    /// <param name="config">Optional research configuration</param>
    /// <returns>Assistant's response</returns>
    public async Task<string> ProcessChatMessageAsync(
        string sessionId,
        string userMessage,
        ResearchConfig? config)
    {
        _logger.LogInformation("Processing chat message for session {SessionId}", sessionId);

        try
        {
            // Extract topic from user message (simple approach - can be enhanced)
            var topic = userMessage;
            var brief = $"Research query from chat session {sessionId}: {userMessage}";

            // Execute the full research pipeline
            var reportOutput = await _masterWorkflow.ExecuteFullPipelineAsync(
                _researcherAgent,
                _analystAgent,
                _reportAgent,
                _transitioner,
                _errorRecovery,
                topic,
                brief,
                _logger,
                CancellationToken.None
            );

            _logger.LogInformation("Research completed for session {SessionId}", sessionId);

            // Format the response for chat
            var response = FormatResponseForChat(reportOutput, userMessage);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat message for session {SessionId}", sessionId);
            
            // Return error message to user
            return $"I encountered an error while processing your request: {ex.Message}. " +
                   "Please try rephrasing your question or contact support if the issue persists.";
        }
    }

    /// <summary>
    /// Format the research report for chat display
    /// </summary>
    private string FormatResponseForChat(ReportOutput report, string originalQuery)
    {
        var response = new System.Text.StringBuilder();
        
        response.AppendLine($"# {report.Title}");
        response.AppendLine();
        response.AppendLine($"**Your Query:** {originalQuery}");
        response.AppendLine();
        response.AppendLine("---");
        response.AppendLine();
        
        // Executive Summary
        response.AppendLine("## Executive Summary");
        response.AppendLine(report.ExecutiveSummary);
        response.AppendLine();
        
        // Sections
        foreach (var section in report.Sections)
        {
            response.AppendLine($"## {section.Heading}");
            response.AppendLine(section.Content);
            response.AppendLine();
        }
        
        // Citations
        if (report.Citations != null && report.Citations.Any())
        {
            response.AppendLine("## References");
            foreach (var citation in report.Citations)
            {
                response.AppendLine($"[{citation.Index}] {citation.Source}");
                if (!string.IsNullOrEmpty(citation.Url))
                {
                    response.AppendLine($"   URL: {citation.Url}");
                }
            }
            response.AppendLine();
        }
        
        // Quality Score
        response.AppendLine($"*Quality Score: {report.QualityScore:F2}/10*");
        
        return response.ToString();
    }
}
