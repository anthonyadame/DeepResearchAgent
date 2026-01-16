using System.Text;
using System.Text.Json;
using System.Runtime.CompilerServices;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Prompts;
using Microsoft.Extensions.Logging;

namespace DeepResearchAgent.Workflows;

/// <summary>
/// Researcher Workflow: Conducts focused research using ReAct loop (LLM → Tools → Loop).
/// 
/// Core Functions:
/// 1. LLM Brain: Decides what to search for
/// 2. Tool Execution: Search, scrape, think
/// 3. Research Compression: Synthesize findings
/// 4. Fact Persistence: Store in knowledge base
/// 
/// Maps to Python lines 390-470 (Re Act loop pattern)
/// </summary>
public class ResearcherWorkflow
{
    private readonly ILightningStateService _stateService;
    private readonly SearCrawl4AIService _searchService;
    private readonly OllamaService _llmService;
    private readonly LightningStore _store;
    private readonly ILogger<ResearcherWorkflow>? _logger;

    public ResearcherWorkflow(
        ILightningStateService stateService,
        SearCrawl4AIService searchService,
        OllamaService llmService,
        LightningStore store,
        ILogger<ResearcherWorkflow>? logger = null)
    {
        _stateService = stateService ?? throw new ArgumentNullException(nameof(stateService));
        _searchService = searchService ?? throw new ArgumentNullException(nameof(searchService));
        _llmService = llmService ?? throw new ArgumentNullException(nameof(llmService));
        _store = store ?? throw new ArgumentNullException(nameof(store));
        _logger = logger;
    }

    /// <summary>
    /// Execute focused research on a topic using ReAct loop.
    /// Returns list of extracted facts.
    /// </summary>
    public async Task<IReadOnlyList<Models.FactState>> ResearchAsync(
        string topic,
        string? researchId = null,
        CancellationToken cancellationToken = default)
    {
        var localResearchId = researchId ?? Guid.NewGuid().ToString();
        _logger?.LogInformation("ResearcherWorkflow starting for topic: {topic}, Research ID: {researchId}", topic, localResearchId);

        try
        {
            // Initialize research state
            var researchState = new ResearchStateModel
            {
                ResearchId = localResearchId,
                Query = topic,
                Status = ResearchStatus.InProgress,
                StartedAt = DateTime.UtcNow
            };

            await _stateService.SetResearchStateAsync(localResearchId, researchState, cancellationToken);

            var researcherState = CreateResearcherState(topic);
            
            // Execute ReAct loop: LLM → Tools → Loop
            const int maxIterations = 5;
            for (int iteration = 0; iteration < maxIterations; iteration++)
            {
                _logger?.LogDebug("Research iteration {iter}/{max}", iteration + 1, maxIterations);

                // Step 1: LLM Call - Decide next research action
                var llmResponse = await LLMCallAsync(researcherState, cancellationToken);
                researcherState.ResearcherMessages.Add(llmResponse);

                // Step 2: Check if we should continue (loop or compress)
                if (!ShouldContinue(researcherState, iteration, maxIterations))
                {
                    _logger?.LogDebug("Research loop ending - should compress");
                    break;
                }

                // Step 3: Execute tools (search, scrape, think)
                await ToolExecutionAsync(researcherState, llmResponse, cancellationToken);

                // Update progress
                var progressQuality = CalculateResearchQuality(researcherState);
                await _stateService.UpdateResearchProgressAsync(
                    localResearchId,
                    iteration + 1,
                    progressQuality,
                    cancellationToken
                );

                _logger?.LogDebug("Research iteration {iter} completed, quality: {quality:P}", iteration + 1, progressQuality);
            }

            // Step 4: Compress research into coherent summary
            var compressedFindings = await CompressResearchAsync(researcherState, cancellationToken);
            researcherState.CompressedResearch = compressedFindings;

            // Step 5: Extract and persist facts
            var facts = ExtractFactsFromFindings(compressedFindings);
            if (facts.Count > 0)
            {
                await _store.SaveFactsAsync(facts.AsEnumerable(), cancellationToken);
                _logger?.LogInformation("Persisted {count} facts to knowledge base", facts.Count);
            }

            // Update final research state
            researchState.Status = ResearchStatus.Completed;
            researchState.CompletedAt = DateTime.UtcNow;
            await _stateService.SetResearchStateAsync(localResearchId, researchState, cancellationToken);

            _logger?.LogInformation("Research complete for topic: {topic} - {count} facts extracted", 
                topic, facts.Count);
            
            return facts;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "ResearcherWorkflow failed for topic: {topic}", topic);
            throw;
        }
    }

    /// <summary>
    /// Stream research progress for real-time output.
    /// </summary>
    public async IAsyncEnumerable<string> StreamResearchAsync(
        string topic,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        _logger?.LogInformation("ResearcherWorkflow streaming for topic: {topic}", topic);
        yield return $"[researcher] starting research on: {topic}";

        var researcherState = CreateResearcherState(topic);
        int iteration = 0;
        const int maxIterations = 5;

        for (iteration = 0; iteration < maxIterations; iteration++)
        {
            // LLM Call (with error handling outside yield)
            var llmDecision = "";
            try
            {
                var llmResponse = await LLMCallAsync(researcherState, cancellationToken);
                researcherState.ResearcherMessages.Add(llmResponse);
                llmDecision = llmResponse.Content;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "LLM call failed");
                llmDecision = $"ERROR: {ex.Message}";
            }

            yield return $"[researcher] iteration {iteration + 1}/{maxIterations}";
            if (llmDecision.StartsWith("ERROR"))
            {
                yield return llmDecision;
                yield break;
            }

            var preview = llmDecision.Substring(0, Math.Min(60, llmDecision.Length));
            yield return $"[researcher] llm: {preview}...";

            // Should continue check
            if (!ShouldContinue(researcherState, iteration, maxIterations))
            {
                yield return "[researcher] converging to compression phase";
                break;
            }

            // Tool execution (with error handling outside yield)
            int notesGathered = 0;
            try
            {
                await ToolExecutionAsync(researcherState, 
                    new Models.ChatMessage { Role = "assistant", Content = llmDecision }, 
                    cancellationToken);
                notesGathered = researcherState.RawNotes.Count;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Tool execution failed");
                notesGathered = -1;
            }

            if (notesGathered < 0)
            {
                yield return "[researcher] error during tool execution, stopping";
                break;
            }

            yield return $"[researcher] tools: gathered {notesGathered} notes";
        }

        // Compression phase (with error handling outside yield)
        string compressedFindings = "";
        int extractedFacts = 0;
        try
        {
            compressedFindings = await CompressResearchAsync(researcherState, cancellationToken);
            researcherState.CompressedResearch = compressedFindings;
            
            var facts = ExtractFactsFromFindings(compressedFindings);
            if (facts.Count > 0)
            {
                await _store.SaveFactsAsync(facts.AsEnumerable(), cancellationToken);
                extractedFacts = facts.Count;
            }
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Compression failed");
            compressedFindings = $"ERROR: {ex.Message}";
        }

        yield return "[researcher] compressing findings...";
        if (compressedFindings.StartsWith("ERROR"))
        {
            yield return compressedFindings;
            yield break;
        }

        yield return $"[researcher] compressed summary: {compressedFindings.Length} chars";
        if (extractedFacts > 0)
        {
            yield return $"[researcher] extracted and persisted {extractedFacts} facts";
        }

        yield return $"[researcher] research complete - {iteration + 1} iterations";
    }

    /// <summary>
    /// Step 1: LLM Call - Decide what research action to take next.
    /// Maps to Python lines 390-400
    /// </summary>
    public async Task<Models.ChatMessage> LLMCallAsync(
        ResearcherState state,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogDebug("LLMCall: deciding research direction");

            var currentDate = GetTodayString();
            
            // Build context for LLM
            var contextBuilder = new StringBuilder();
            contextBuilder.AppendLine("=== RESEARCH CONTEXT ===");
            contextBuilder.AppendLine($"Date: {currentDate}");
            contextBuilder.AppendLine($"Research Topic: {state.ResearchTopic}");
            contextBuilder.AppendLine($"Iteration: {state.ToolCallIterations}");
            contextBuilder.AppendLine();

            if (state.RawNotes.Any())
            {
                contextBuilder.AppendLine("=== GATHERED NOTES ===");
                foreach (var note in state.RawNotes.Take(3))
                {
                    var preview = note.Substring(0, Math.Min(100, note.Length));
                    contextBuilder.AppendLine($"- {preview}...");
                }
                contextBuilder.AppendLine();
            }

            var systemPrompt = PromptTemplates.ResearchAgentPrompt ?? $@"You are an expert research agent conducting focused research.

{contextBuilder}

Your task:
1. Assess what you've learned so far (if anything)
2. Identify the most important next search or question
3. Decide whether to:
   - Search for a specific aspect
   - Ask a clarifying question (reflect)
   - Stop researching (you have enough)

Be concise and strategic. Focus on filling knowledge gaps.";

            var messages = new List<OllamaChatMessage>
            {
                new() { Role = "system", Content = systemPrompt }
            };

            // Add research history
            foreach (var msg in state.ResearcherMessages.Cast<OllamaChatMessage>())
            {
                messages.Add(msg);
            }

            var response = await _llmService.InvokeAsync(messages, cancellationToken: cancellationToken);
            
            _logger?.LogDebug("LLM decision: {length} chars", response.Content.Length);
            
            // Convert to Models.ChatMessage
            return new Models.ChatMessage 
            { 
                Role = response.Role, 
                Content = response.Content 
            };
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "LLMCall failed - using fallback");
            return new Models.ChatMessage 
            { 
                Role = "assistant", 
                Content = $"Search for more information about: {state.ResearchTopic}" 
            };
        }
    }

    /// <summary>
    /// Step 2: Tool Execution - Execute search, scraping, and thinking.
    /// Maps to Python lines 400-420
    /// </summary>
    public async Task ToolExecutionAsync(
        ResearcherState state,
        Models.ChatMessage llmResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogDebug("ToolExecution: executing LLM decision");

            // Extract research queries from LLM response
            var queries = ExtractSearchQueries(llmResponse.Content, state.ResearchTopic);

            if (queries.Count == 0)
            {
                _logger?.LogDebug("No search queries identified");
                return;
            }

            // Execute searches in parallel
            var searchTasks = queries
                .Take(2) // Max 2 concurrent searches
                .Select(q => ExecuteSearchAsync(q, cancellationToken))
                .ToList();

            var searchResults = await Task.WhenAll(searchTasks);

            // Aggregate results
            foreach (var results in searchResults)
            {
                foreach (var result in results)
                {
                    state.RawNotes.Add(result);
                }
            }

            // Record tool execution in messages
            state.ResearcherMessages.Add(new Models.ChatMessage
            {
                Role = "tool",
                Content = $"Searched {queries.Count} topics and gathered {searchResults.Sum(r => r.Count)} pieces of information."
            });

            state.ToolCallIterations++;

            _logger?.LogInformation("ToolExecution: gathered {count} notes", searchResults.Sum(r => r.Count));
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "ToolExecution failed");
        }
    }

    /// <summary>
    /// Execute a single search and scrape operation.
    /// </summary>
    private async Task<List<string>> ExecuteSearchAsync(
        string query,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogDebug("Searching for: {query}", query);

            var results = await _searchService.SearchAndScrapeAsync(
                query,
                maxResults: 3,
                cancellationToken: cancellationToken
            );

            var notes = results
                .Select(r => BuildFactContent(r))
                .Where(content => !string.IsNullOrWhiteSpace(content))
                .ToList();

            _logger?.LogDebug("Search found {count} results", notes.Count);

            return notes;
        }
        catch (Exception ex)
        {
            _logger?.LogWarning(ex, "Search for '{query}' failed", query);
            return new List<string>();
        }
    }

    /// <summary>
    /// Step 3: Check if research should continue or move to compression.
    /// Maps to Python lines 420-430
    /// </summary>
    public static bool ShouldContinue(
        ResearcherState state,
        int currentIteration,
        int maxIterations)
    {
        // Continue if:
        // 1. Haven't hit max iterations
        // 2. We still have notes to process
        // 3. LLM didn't suggest stopping
        
        if (currentIteration >= maxIterations - 1)
            return false; // Last iteration, compress

        if (state.RawNotes.Count == 0)
            return false; // No data to compress

        // Check if LLM suggested stopping (heuristic)
        var lastMessage = state.ResearcherMessages.LastOrDefault()?.Content ?? "";
        if (lastMessage.Contains("enough", StringComparison.OrdinalIgnoreCase) ||
            lastMessage.Contains("sufficient", StringComparison.OrdinalIgnoreCase) ||
            lastMessage.Contains("stop", StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return true;
    }

    /// <summary>
    /// Step 4: Compress research findings into coherent summary.
    /// Maps to Python lines 430-460
    /// </summary>
    public async Task<string> CompressResearchAsync(
        ResearcherState state,
        CancellationToken cancellationToken)
    {
        try
        {
            _logger?.LogDebug("CompressResearch: synthesizing findings");

            if (state.RawNotes.Count == 0)
            {
                return "No research findings to compress.";
            }

            var currentDate = GetTodayString();
            
            // Aggregate raw notes
            var aggregatedNotes = string.Join("\n\n", state.RawNotes.Take(15));

            var compressionPrompt = PromptTemplates.CompressResearchSystemPrompt ?? $@"You are a research compression expert. Synthesize raw research findings into a concise, well-organized summary.

Date: {currentDate}

Research Topic: {state.ResearchTopic}

Raw Research Notes:
{aggregatedNotes}

Task: Create a comprehensive but concise summary that:
1. Extracts key findings and insights
2. Preserves important data and quotes
3. Mentions sources where identified
4. Organizes information logically
5. Removes redundancy

Write the compressed research summary:";

            var messages = new List<OllamaChatMessage>
            {
                new() { Role = "system", Content = compressionPrompt }
            };

            var response = await _llmService.InvokeAsync(messages, cancellationToken: cancellationToken);
            var compressed = response.Content ?? "Unable to compress research findings.";

            _logger?.LogInformation("CompressResearch: {length} chars", compressed.Length);

            return compressed;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "CompressResearch failed");
            return string.Join("\n", state.RawNotes.Take(5)); // Fallback: return raw notes
        }
    }

    /// <summary>
    /// Extract search queries from LLM decision.
    /// Simple heuristic: parse for search suggestions.
    /// </summary>
    private static List<string> ExtractSearchQueries(string llmDecision, string researchTopic)
    {
        var queries = new List<string>();

        // Always include the main topic
        queries.Add(researchTopic);

        // Look for specific aspects mentioned
        if (llmDecision.Contains("search for", StringComparison.OrdinalIgnoreCase))
        {
            // Extract words after "search for"
            var idx = llmDecision.IndexOf("search for", StringComparison.OrdinalIgnoreCase);
            if (idx >= 0)
            {
                var portion = llmDecision.Substring(idx + 10, Math.Min(100, llmDecision.Length - idx - 10));
                var words = portion.Split(new[] { ' ', ',', '.', ':' }, StringSplitOptions.RemoveEmptyEntries)
                    .Take(3)
                    .Where(w => w.Length > 2);
                
                if (words.Any())
                {
                    queries.Add($"{researchTopic} {string.Join(" ", words)}");
                }
            }
        }

        // Add variations
        if (researchTopic.Length > 10)
        {
            queries.Add($"{researchTopic} applications");
            queries.Add($"{researchTopic} benefits");
        }

        return queries.Distinct().Take(3).ToList();
    }

    /// <summary>
    /// Extract facts from compressed research findings.
    /// </summary>
    private static List<Models.FactState> ExtractFactsFromFindings(string compressedFindings)
    {
        var facts = new List<Models.FactState>();

        if (string.IsNullOrWhiteSpace(compressedFindings))
            return facts;

        // Split by sentences or paragraphs
        var paragraphs = compressedFindings
            .Split(new[] { "\n\n", "\n", ". " }, StringSplitOptions.RemoveEmptyEntries)
            .Where(p => p.Length > 20)
            .Take(20); // Max 20 facts

        int idx = 1;
        foreach (var paragraph in paragraphs)
        {
            var fact = StateFactory.CreateFact(
                paragraph.Trim(),
                "compressed_research",
                confidenceScore: 75  // Confidence from compression
            );
            facts.Add(fact);
            idx++;
        }

        return facts;
    }

    /// <summary>
    /// Create initial researcher state.
    /// </summary>
    private static ResearcherState CreateResearcherState(string topic)
    {
        return new ResearcherState
        {
            ResearchTopic = topic,
            ResearcherMessages = new List<Models.ChatMessage>(),
            RawNotes = new List<string>(),
            CompressedResearch = string.Empty,
            ToolCallIterations = 0
        };
    }

    /// <summary>
    /// Build fact content from scraped web result.
    /// </summary>
    private static string BuildFactContent(ScrapedContent content)
    {
        var text = !string.IsNullOrWhiteSpace(content.Markdown)
            ? content.Markdown
            : content.CleanedHtml;

        if (string.IsNullOrWhiteSpace(text))
        {
            text = content.Html ?? string.Empty;
        }

        return text.Length > 280 ? text[..280] + "..." : text;
    }

    /// <summary>
    /// Calculate current research quality based on gathered notes and messages.
    /// Returns a value between 0 and 1.
    /// </summary>
    private static double CalculateResearchQuality(ResearcherState state)
    {
        double quality = 0.0;

        // Factor 1: Number of notes gathered (0-0.3)
        var notesFactor = Math.Min(state.RawNotes.Count / 10.0, 0.3);
        quality += notesFactor;

        // Factor 2: Number of researcher messages (0-0.3)
        var messageFactor = Math.Min(state.ResearcherMessages.Count / 5.0, 0.3);
        quality += messageFactor;

        // Factor 3: Tool call iterations (0-0.2)
        var iterationFactor = Math.Min(state.ToolCallIterations / 5.0, 0.2);
        quality += iterationFactor;

        // Factor 4: Length of compressed research (0-0.2)
        var contentLength = state.CompressedResearch?.Length ?? 0;
        var contentFactor = Math.Min(contentLength / 5000.0, 0.2);
        quality += contentFactor;

        return Math.Min(quality, 1.0);  // Cap at 1.0
    }

    /// <summary>
    /// Format today's date for prompt consistency.
    /// </summary>
    private static string GetTodayString()
    {
        return DateTime.Now.ToString("ddd MMM d, yyyy");
    }
}
