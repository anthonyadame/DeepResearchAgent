using System.Runtime.CompilerServices;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Prompts;
using Microsoft.Extensions.Logging;

namespace DeepResearchAgent.Workflows;

/// <summary>
/// Master Workflow: Orchestrates the entire Deep Research Agent pipeline.
/// 
/// 5-Step Process:
/// 1. ClarifyWithUser - Ensure user query is sufficiently detailed
/// 2. WriteResearchBrief - Transform query into structured research brief
/// 3. WriteDraftReport - Generate initial "noisy" draft (diffusion starting point)
/// 4. ExecuteSupervisor - Hand off to supervisor for iterative refinement
/// 5. GenerateFinalReport - Polish and synthesize findings into final report
/// 
/// Maps to Python lines 870-920 (scoping) and 2119-2140 (full integration)
/// </summary>
public class MasterWorkflow
{
    private readonly ILightningStateService _stateService;
    private readonly SupervisorWorkflow _supervisor;
    private readonly OllamaService _llmService;
    private readonly ILogger<MasterWorkflow>? _logger;
    private readonly StateManager? _stateManager;

    public MasterWorkflow(
        ILightningStateService stateService,
        SupervisorWorkflow supervisor,
        OllamaService llmService,
        ILogger<MasterWorkflow>? logger = null,
        StateManager? stateManager = null)
    {
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _supervisor = supervisor ?? throw new ArgumentNullException(nameof(supervisor));
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _logger = logger;
        _stateManager = stateManager;
    }

    /// <summary>
    /// Execute the complete master workflow.
    /// Entry point for the entire research pipeline.
    /// </summary>
    public async Task<string> RunAsync(string userQuery, CancellationToken cancellationToken = default)
    {
        var researchId = Guid.NewGuid().ToString();
        _logger?.LogInformation("Starting MasterWorkflow with research ID: {researchId}", researchId);

        try
        {
            // Initialize research state
            var researchState = new ResearchStateModel
            {
                ResearchId = researchId,
                Query = userQuery,
                Status = ResearchStatus.Pending,
                StartedAt = DateTime.UtcNow,
                Metadata = new Dictionary<string, object> { { "phase", "initialization" } }
            };
            
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
            _logger?.LogInformation("Research {ResearchId} initialized", researchId);

            // Step 1: Clarify with user (check if query is detailed enough)
            _logger?.LogInformation("Step 1: Clarifying user intent");
            researchState.Status = ResearchStatus.InProgress;
            researchState.Metadata["phase"] = "clarification";
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
            
            var (needsClarification, clarificationQuestion) = await ClarifyWithUserAsync(userQuery, cancellationToken);
            
            if (needsClarification)
            {
                _logger?.LogInformation("User clarification needed");
                researchState.Status = ResearchStatus.Failed;
                researchState.Metadata["failure_reason"] = "Clarification required";
                await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
                return $"Clarification needed:\n\n{clarificationQuestion}";
            }

            // Step 2: Write research brief
            _logger?.LogInformation("Step 2: Writing research brief");
            researchState.Metadata["phase"] = "brief_writing";
            researchState.IterationCount = 1;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
            
            var researchBrief = await WriteResearchBriefAsync(userQuery, cancellationToken);
            researchState.Metadata["researchBrief"] = researchBrief;

            // Step 3: Write initial draft
            _logger?.LogInformation("Step 3: Generating initial draft report");
            researchState.Metadata["phase"] = "draft_writing";
            researchState.IterationCount = 2;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
            
            var draftReport = await WriteDraftReportAsync(researchBrief, cancellationToken);
            researchState.Metadata["draftReport"] = draftReport;

            // Step 4: Execute supervisor loop
            _logger?.LogInformation("Step 4: Executing supervisor loop (diffusion process)");
            researchState.Status = ResearchStatus.Verifying;
            researchState.Metadata["phase"] = "supervision";
            researchState.IterationCount = 3;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
            
            var refinedSummary = await _supervisor.SuperviseAsync(researchBrief, draftReport, cancellationToken: cancellationToken);
            researchState.Metadata["refinedSummary"] = refinedSummary;

            // Step 5: Generate final report
            _logger?.LogInformation("Step 5: Generating final polished report");
            researchState.Metadata["phase"] = "final_report";
            researchState.IterationCount = 4;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);
            
            var finalReport = await GenerateFinalReportAsync(userQuery, researchBrief, draftReport, refinedSummary, cancellationToken);
            researchState.Metadata["finalReport"] = finalReport;

            // Mark completed
            researchState.Status = ResearchStatus.Completed;
            researchState.CompletedAt = DateTime.UtcNow;
            researchState.IterationCount = 5;
            await _stateService.SetResearchStateAsync(researchId, researchState, cancellationToken);

            _logger?.LogInformation("MasterWorkflow {ResearchId} completed successfully", researchId);
            var metrics = _stateService.GetMetrics();
            _logger?.LogInformation("State service metrics - Cache hit rate: {HitRate:P}", metrics.CacheHitRate);
            
            return finalReport;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "MasterWorkflow {ResearchId} failed", researchId);
            try
            {
                var failedState = await _stateService.GetResearchStateAsync(researchId, cancellationToken);
                failedState.Status = ResearchStatus.Failed;
                failedState.Metadata["error"] = ex.Message;
                await _stateService.SetResearchStateAsync(researchId, failedState, cancellationToken);
            }
            catch (Exception stateEx)
            {
                _logger?.LogWarning(stateEx, "Failed to update error state for {ResearchId}", researchId);
            }
            throw;
        }
    }

    /// <summary>
    /// Execute the complete master workflow with AgentState input/output.
    /// Overload for test compatibility.
    /// </summary>
    public async Task<AgentState> ExecuteAsync(AgentState input, CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting MasterWorkflow.ExecuteAsync with AgentState");

        try
        {
            // Extract query from initial message
            var userQuery = input.Messages?.FirstOrDefault()?.Content ?? "Conduct research";
            
            // Step 1: Clarify with user
            _logger?.LogInformation("Step 1: Clarifying user intent");
            var (needsClarification, clarificationQuestion) = await ClarifyWithUserAsync(userQuery, cancellationToken);
            
            if (needsClarification)
            {
                _logger?.LogInformation("User clarification needed");
                input.ResearchBrief = $"Clarification needed: {clarificationQuestion}";
                return input;
            }

            // Step 2: Write research brief
            _logger?.LogInformation("Step 2: Writing research brief");
            var researchBrief = await WriteResearchBriefAsync(userQuery, cancellationToken);
            input.ResearchBrief = researchBrief;

            // Step 3: Write initial draft
            _logger?.LogInformation("Step 3: Generating initial draft report");
            var draftReport = await WriteDraftReportAsync(researchBrief, cancellationToken);
            input.DraftReport = draftReport;

            // Step 4: Execute supervisor loop
            _logger?.LogInformation("Step 4: Executing supervisor loop (diffusion process)");
            var supervisorState = StateFactory.CreateSupervisorState(researchBrief, draftReport, input.SupervisorMessages);
            var refinedState = await _supervisor.ExecuteAsync(supervisorState, cancellationToken);
            input.SupervisorMessages = refinedState.SupervisorMessages;
            input.RawNotes = refinedState.RawNotes;

            // Step 5: Generate final report
            _logger?.LogInformation("Step 5: Generating final polished report");
            var refinedSummary = refinedState.DraftReport;
            var finalReport = await GenerateFinalReportAsync(userQuery, researchBrief, draftReport, refinedSummary, cancellationToken);
            input.FinalReport = finalReport;

            _logger?.LogInformation("MasterWorkflow.ExecuteAsync completed successfully");
            return input;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "MasterWorkflow.ExecuteAsync failed");
            throw;
        }
    }

    /// <summary>
    /// Stream real-time updates from master and supervisor workflows.
    /// </summary>
    public async IAsyncEnumerable<string> StreamAsync(string userQuery, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("Starting MasterWorkflow stream");
        yield return $"[master] received query: {userQuery}";

        // Step 1: Clarify
        yield return "[master] step 1: clarifying user intent...";
        var (needsClarification, clarificationQuestion) = await ClarifyWithUserAsync(userQuery, cancellationToken);
        
        if (needsClarification)
        {
            yield return $"[master] clarification needed: {clarificationQuestion}";
            yield break;
        }
        yield return "[master] query is sufficiently detailed";

        // Step 2: Research brief
        yield return "[master] step 2: writing research brief...";
        var researchBrief = await WriteResearchBriefAsync(userQuery, cancellationToken);
        var briefPreview = researchBrief.Substring(0, Math.Min(100, researchBrief.Length));
        yield return $"[master] research brief: {briefPreview}...";

        // Step 3: Initial draft
        yield return "[master] step 3: generating initial draft report...";
        var draftReport = await WriteDraftReportAsync(researchBrief, cancellationToken);
        yield return $"[master] initial draft generated ({draftReport.Length} chars)";

        // Step 4: Supervisor loop (stream its progress)
        yield return "[master] step 4: starting supervisor loop (diffusion process)...";
        await foreach (var supervisorUpdate in _supervisor.StreamSuperviseAsync(researchBrief, draftReport, cancellationToken: cancellationToken))
        {
            yield return supervisorUpdate;
        }

        // Step 5: Final report
        yield return "[master] step 5: generating final polished report...";
        var refinedSummary = await _supervisor.SuperviseAsync(researchBrief, draftReport, cancellationToken: cancellationToken);
        var finalReport = await GenerateFinalReportAsync(userQuery, researchBrief, draftReport, refinedSummary, cancellationToken);
        yield return $"[master] final report generated ({finalReport.Length} chars)";

        yield return "[master] workflow complete";
    }

    /// <summary>
    /// Step 1: Clarify with user - Check if query is detailed enough.
    /// Uses LLM to evaluate query clarity.
    /// </summary>
    public async Task<(bool needsClarification, string message)> ClarifyWithUserAsync(
        string userQuery, CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogDebug("Step 1: ClarifyWithUser - evaluating query clarity");
            
            // Simple heuristic check first
            if (string.IsNullOrWhiteSpace(userQuery) || userQuery.Length < 10)
            {
                return (true, "Please provide a more detailed research query (at least 10 characters). Include what you want to learn about and any specific focus areas.");
            }

            // Use LLM to evaluate clarity
            var currentDate = GetTodayString();
            var clarifyPrompt = PromptTemplates.ClarifyWithUserInstructions
                .Replace("{messages}", userQuery)
                .Replace("{date}", currentDate);

            var messages = new List<OllamaChatMessage>
            {
                new() { Role = "system", Content = clarifyPrompt }
            };

            var response = await _llmService.InvokeAsync(messages, cancellationToken: cancellationToken);
            var responseText = response.Content ?? "";

            _logger?.LogDebug("LLM clarification response: {length} chars", responseText.Length);

            // Simple heuristic: if response suggests clarification, ask for it
            if (responseText.Contains("clarif", StringComparison.OrdinalIgnoreCase) ||
                responseText.Contains("need", StringComparison.OrdinalIgnoreCase))
            {
                return (true, responseText);
            }

            return (false, "");
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Error in ClarifyWithUserAsync - proceeding anyway");
            return (false, ""); // Don't block workflow on clarification errors
        }
    }

    /// <summary>
    /// Step 2: Write research brief - Transform query into structured research brief.
    /// Uses LLM to create detailed research direction.
    /// </summary>
    public async Task<string> WriteResearchBriefAsync(string userQuery, CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogDebug("Step 2: WriteResearchBrief - transforming query to structured brief");
            
            var currentDate = GetTodayString();
            var briefPrompt = PromptTemplates.TransformMessagesIntoResearchTopicPrompt
                .Replace("{messages}", userQuery)
                .Replace("{date}", currentDate);

            var messages = new List<OllamaChatMessage>
            {
                new() { Role = "system", Content = briefPrompt }
            };

            var response = await _llmService.InvokeAsync(messages, cancellationToken: cancellationToken);
            var researchBrief = response.Content ?? $"Research Brief: {userQuery}";

            _logger?.LogInformation("Research brief generated: {length} chars", researchBrief.Length);
            
            return researchBrief;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating research brief - using query as fallback");
            return $"Research Brief: {userQuery}";
        }
    }

    /// <summary>
    /// Step 3: Write initial draft report - Generate "noisy" starting point for diffusion.
    /// Uses LLM to create initial draft without external research.
    /// </summary>
    public async Task<string> WriteDraftReportAsync(string researchBrief, CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogDebug("Step 3: WriteDraftReport - generating initial draft outline");
            
            var currentDate = GetTodayString();
            var draftPrompt = PromptTemplates.DraftReportGenerationPrompt
                .Replace("{research_brief}", researchBrief)
                .Replace("{date}", currentDate);

            var messages = new List<OllamaChatMessage>
            {
                new() { Role = "system", Content = draftPrompt }
            };

            var response = await _llmService.InvokeAsync(messages, cancellationToken: cancellationToken);
            var draftReport = response.Content ?? $"Initial draft based on: {researchBrief}";

            _logger?.LogInformation("Draft report generated: {length} chars", draftReport.Length);
            
            return draftReport;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating draft report - using fallback");
            return $"Initial draft based on: {researchBrief}";
        }
    }

    /// <summary>
    /// Step 4: Execute supervisor loop (handled by SupervisorWorkflow).
    /// Already implemented via _supervisor.SuperviseAsync()
    /// </summary>

    /// <summary>
    /// Step 5: Generate final report - Polish and synthesize findings.
    /// Uses LLM to create polished final report.
    /// </summary>
    public async Task<string> GenerateFinalReportAsync(
        string userQuery, string researchBrief, string draftReport, 
        string refinedSummary, CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogDebug("Step 5: GenerateFinalReport - synthesizing and polishing findings");
            
            var currentDate = GetTodayString();
            var finalPrompt = $@"You are a professional research report writer.
Your task is to synthesize research findings into a polished, well-structured final report.

Original User Query:
{userQuery}

Research Brief:
{researchBrief}

Initial Draft:
{draftReport}

Research Findings:
{refinedSummary}

Current Date: {currentDate}

Create a professional, comprehensive final report that:
1. Directly addresses the original user query
2. Incorporates the research findings naturally
3. Maintains clear structure and flow
4. Includes proper citations where mentioned
5. Provides clear conclusions and insights
6. Is suitable for professional presentation

Write the final report:";

            var messages = new List<OllamaChatMessage>
            {
                new() { Role = "system", Content = finalPrompt }
            };

            var response = await _llmService.InvokeAsync(messages, cancellationToken: cancellationToken);
            var finalReport = response.Content ?? 
                $@"=== Final Research Report ===

Original Query: {userQuery}

Findings Summary:
{refinedSummary}

This report synthesizes the research findings on the requested topic.";

            _logger?.LogInformation("Final report generated: {length} chars", finalReport.Length);
            
            return finalReport;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Error generating final report - using fallback");
            return $@"=== Final Research Report ===

Original Query: {userQuery}

Research Findings:
{refinedSummary}";
        }
    }

    /// <summary>
    /// Format today's date in "Day Mon Day, Year" format to match Python's strftime
    /// Example: "Mon Dec 23, 2024"
    /// </summary>
    private static string GetTodayString()
    {
        return DateTime.Now.ToString("ddd MMM d, yyyy");
    }
}
