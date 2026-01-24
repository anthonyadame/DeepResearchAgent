// ENHANCEMENT: Vector Database Search Integration for ResearcherWorkflow
// This file shows the new methods to add to ResearcherWorkflow.cs

// =====================================================================
// 1. Update ToolExecutionAsync method to include vector database search
// =====================================================================

/// <summary>
/// Step 2: Tool Execution - Execute search, scraping, and semantic knowledge search.
/// Maps to Python lines 400-420
/// Now includes vector database search for external knowledge resources.
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

        // Execute searches in parallel (web + vector database)
        var searchTasks = new List<Task<List<string>>>();
        
        // Add web search tasks
        searchTasks.AddRange(queries
            .Take(2) // Max 2 concurrent web searches
            .Select(q => ExecuteSearchAsync(q, cancellationToken))
            .ToList());
        
        // Add vector database search tasks (if available)
        if (_vectorDb != null && _embeddingService != null)
        {
            searchTasks.AddRange(queries
                .Take(2) // Max 2 concurrent vector database searches
                .Select(q => ExecuteVectorDatabaseSearchAsync(q, cancellationToken))
                .ToList());
        }

        var searchResults = await Task.WhenAll(searchTasks);

        // Aggregate results from both web and vector database searches
        foreach (var results in searchResults)
        {
            foreach (var result in results)
            {
                state.RawNotes.Add(result);
            }
        }

        // Record tool execution in messages
        var searchSourceInfo = _vectorDb != null && _embeddingService != null 
            ? "web and vector database"
            : "web search";
        
        state.ResearcherMessages.Add(new Models.ChatMessage
        {
            Role = "tool",
            Content = $"Searched {queries.Count} topics across {searchSourceInfo} and gathered {searchResults.Sum(r => r.Count)} pieces of information."
        });

        state.ToolCallIterations++;

        _logger?.LogInformation("ToolExecution: gathered {count} notes from combined sources", searchResults.Sum(r => r.Count));
    }
    catch (Exception ex)
    {
        _logger?.LogWarning(ex, "ToolExecution failed");
    }
}

// =====================================================================
// 2. NEW METHOD: Execute vector database search for external knowledge
// =====================================================================

/// <summary>
/// Execute a vector database search for existing knowledge resources.
/// Searches pre-existing or external knowledge bases for related information.
/// Similar to ExecuteSearchAsync but queries semantic embeddings instead of web.
/// </summary>
private async Task<List<string>> ExecuteVectorDatabaseSearchAsync(
    string query,
    CancellationToken cancellationToken)
{
    try
    {
        _logger?.LogDebug("Vector database search for: {query}", query);

        if (_vectorDb == null || _embeddingService == null)
        {
            _logger?.LogDebug("Vector database not configured - skipping vector search");
            return new List<string>();
        }

        // Search for similar facts in vector database
        // Use higher threshold (0.6) for quality relevance in knowledge bases
        var searchResults = await _vectorDb.SearchByContentAsync(
            query,
            topK: 5,
            scoreThreshold: 0.6,
            cancellationToken: cancellationToken);

        if (searchResults.Count == 0)
        {
            _logger?.LogDebug("Vector database search returned no results for: {query}", query);
            return new List<string>();
        }

        // Convert vector search results to notes for research synthesis
        var notes = searchResults
            .Select(result => FormatVectorSearchResult(result))
            .Where(content => !string.IsNullOrWhiteSpace(content))
            .ToList();

        _logger?.LogDebug("Vector database search found {count} relevant facts", notes.Count);

        return notes;
    }
    catch (Exception ex)
    {
        _logger?.LogWarning(ex, "Vector database search for '{query}' failed", query);
        return new List<string>();
    }
}

// =====================================================================
// 3. NEW METHOD: Format vector search results
// =====================================================================

/// <summary>
/// Format vector search result into note content with source attribution.
/// Includes the fact content, relevance score, and metadata for research context.
/// </summary>
private static string FormatVectorSearchResult(VectorSearchResult result)
{
    if (string.IsNullOrWhiteSpace(result.Content))
        return string.Empty;

    var formatted = new StringBuilder();
    formatted.AppendLine($"[Knowledge Base - Relevance: {result.Score:P0}]");
    formatted.AppendLine(result.Content);

    if (result.Metadata != null)
    {
        if (result.Metadata.TryGetValue("sourceUrl", out var sourceUrl) && sourceUrl != null)
        {
            formatted.AppendLine($"Source: {sourceUrl}");
        }

        if (result.Metadata.TryGetValue("confidence", out var confidence) && confidence != null)
        {
            formatted.AppendLine($"Confidence: {confidence}");
        }
    }

    var text = formatted.ToString();
    // Trim to reasonable length (consistent with web search results)
    return text.Length > 280 ? text[..280] + "..." : text;
}

// =====================================================================
// KEY FEATURES:
// =====================================================================
// 1. ExecuteVectorDatabaseSearchAsync - Searches external vector databases
//    - Queries pre-existing knowledge bases
//    - Returns relevant facts with similarity scores
//    - Handles missing vector database gracefully
//
// 2. Integrated into ToolExecutionAsync - Works alongside web search
//    - Runs both web and vector DB searches in parallel
//    - Combines results from both sources
//    - Updates research notes with aggregated information
//
// 3. FormatVectorSearchResult - Prepares results for synthesis
//    - Preserves source attribution
//    - Includes confidence scores
//    - Formats consistently with web search results
//
// 4. Search Strategy
//    - topK: 5 results per query
//    - scoreThreshold: 0.6 (higher quality threshold for knowledge bases)
//    - Graceful degradation if vector DB unavailable
//    - Parallel execution for performance
//
// 5. Integration Points
//    - Requires IVectorDatabaseService and IEmbeddingService
//    - Optional - research continues if not available
//    - Seamlessly integrates with existing ReAct loop
//    - Enhances research quality with external knowledge
