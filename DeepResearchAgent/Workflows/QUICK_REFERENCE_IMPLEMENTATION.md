# Quick Reference - Vector Database Search Enhancement

**For Developers: How to Implement These Changes**

---

## 📋 What to Add

### Step 1: Add ExecuteVectorDatabaseSearchAsync Method

Add this method to `ResearcherWorkflow.cs` after the `ExecuteSearchAsync` method:

```csharp
/// <summary>
/// Execute a vector database search for existing knowledge resources.
/// Searches pre-existing or external knowledge bases for related information.
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
```

---

### Step 2: Add FormatVectorSearchResult Method

Add this method to `ResearcherWorkflow.cs` after the ExecuteVectorDatabaseSearchAsync method:

```csharp
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
    return text.Length > 280 ? text[..280] + "..." : text;
}
```

---

### Step 3: Update ToolExecutionAsync Method

Replace the current ToolExecutionAsync implementation with this:

```csharp
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

        // Aggregate results
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
```

---

## 🎯 What Changed

### New Methods (2)
1. `ExecuteVectorDatabaseSearchAsync` - Search vector databases
2. `FormatVectorSearchResult` - Format results

### Updated Methods (1)
1. `ToolExecutionAsync` - Now includes vector DB search

### No Breaking Changes
- Existing code continues to work
- Vector DB search is optional
- Graceful degradation if not configured

---

## ✅ Verification

### After Implementation

1. **Compile Test**
   ```bash
   dotnet build
   ```
   ✅ Should compile successfully

2. **Unit Tests**
   ```bash
   dotnet test
   ```
   ✅ All tests should pass

3. **Integration Test**
   ```csharp
   var facts = await workflow.ResearchAsync("test topic");
   // Should gather facts from both web and vector database
   ```

---

## 📊 Behavior Comparison

### Method Behavior

| Method | Purpose | Input | Output |
|--------|---------|-------|--------|
| `ExecuteSearchAsync` | Web search | Query string | List of web results |
| `ExecuteVectorDatabaseSearchAsync` | Vector DB search | Query string | List of knowledge base facts |
| `FormatVectorSearchResult` | Format results | VectorSearchResult | Formatted note string |

### Execution Model

| Component | Before | After |
|-----------|--------|-------|
| **Searches per iteration** | 1-2 web | 2-4 (web + vector DB) |
| **Parallel execution** | Sequential | Parallel |
| **Sources** | Web only | Web + Knowledge base |
| **Performance** | Medium | Faster (parallel) |

---

## 🔧 Configuration

### Required
```json
{
  "VectorDatabase": {
    "Enabled": true
  }
}
```

### Optional
```json
{
  "VectorDatabase": {
    "Enabled": true,
    "Qdrant": {
      "BaseUrl": "http://localhost:6333"
    }
  }
}
```

---

## 💬 Logging Output

### Expected Log Messages

**When Vector DB is available:**
```
Vector database search for: query
Vector database search found 5 relevant facts
ToolExecution: gathered 8 notes from combined sources
```

**When Vector DB is not available:**
```
Vector database not configured - skipping vector search
ToolExecution: gathered 3 notes from web search
```

**On error:**
```
Vector database search for 'query' failed
```

---

## 🧪 Testing

### Test Scenarios

1. **Vector DB Available**
   - ✅ Both web and vector DB searches execute
   - ✅ Results are combined
   - ✅ Formatting is correct

2. **Vector DB Unavailable**
   - ✅ Only web search executes
   - ✅ Research continues normally
   - ✅ No errors thrown

3. **No Results**
   - ✅ Empty list returned gracefully
   - ✅ Combined search continues
   - ✅ No null reference errors

4. **Error Handling**
   - ✅ Exceptions caught and logged
   - ✅ Partial results if one search fails
   - ✅ Research continues

---

## 📚 Related Files

| File | Purpose |
|------|---------|
| `VECTOR_DATABASE_SEARCH_ENHANCEMENT.md` | Code examples |
| `VECTOR_DATABASE_SEARCH_INTEGRATION.md` | Detailed guide |
| `VECTOR_DATABASE_SEARCH_SUMMARY.md` | Overview |
| `VECTOR_DATABASE.md` | Vector DB user guide |
| `VectorDatabaseServiceTests.cs` | Test examples |

---

## 🚀 Next Steps

1. **Review** - Read the enhancement and integration docs
2. **Implement** - Add the three changes above
3. **Test** - Run unit and integration tests
4. **Deploy** - Configure and deploy to production
5. **Monitor** - Check logs for proper execution

---

## ❓ FAQ

**Q: Will this break existing code?**
A: No, it's fully backward compatible.

**Q: What if vector database isn't configured?**
A: It gracefully skips vector DB search and uses web search only.

**Q: How much slower will research be?**
A: Faster! Parallel execution means combined time is dominated by web search.

**Q: Can I customize search behavior?**
A: Yes, see `VECTOR_DATABASE_SEARCH_INTEGRATION.md` for customization options.

**Q: How do I know if it's working?**
A: Check logs for "Vector database search found X relevant facts"

---

**Version**: 0.6.5-beta  
**Last Updated**: 2024  
**Status**: ✅ Ready to Implement
