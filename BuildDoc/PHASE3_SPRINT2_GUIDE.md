# 🚀 PHASE 3 SPRINT 2 - ADVANCED FEATURES (4 hours)

**Status:** Ready to execute  
**Estimated Time:** 4 hours  
**Complexity:** Medium (optimization + new features)  
**Pattern:** Follow established patterns from Sprint 1  
**Blockers:** ZERO ✅

---

## 📋 SPRINT 2 BREAKDOWN

### Feature 2.1: Tool Result Caching (1.5 hours)

**Purpose:** Avoid duplicate search results, improve performance

#### Create ToolResultCacheService (45 min)
```bash
File: DeepResearchAgent/Services/ToolResultCacheService.cs

Key Features:
├─ Generic caching with TTL support
├─ GetOrExecuteAsync<T>() method
├─ Automatic cache expiry
├─ Thread-safe operations
└─ Default 1-hour TTL for WebSearch
```

**Code Template:**
```csharp
public class ToolResultCacheService
{
    private readonly Dictionary<string, (object Result, DateTime Expiry)> _cache = new();
    private readonly TimeSpan _defaultTtl;
    
    public ToolResultCacheService(TimeSpan? defaultTtl = null)
    {
        _defaultTtl = defaultTtl ?? TimeSpan.FromHours(1);
    }
    
    public async Task<T> GetOrExecuteAsync<T>(
        string cacheKey,
        Func<Task<T>> executor,
        TimeSpan? ttl = null)
    {
        // Check cache
        if (_cache.TryGetValue(cacheKey, out var cached))
        {
            if (DateTime.UtcNow < cached.Expiry)
            {
                return (T)cached.Result;
            }
            _cache.Remove(cacheKey);
        }
        
        // Execute if not cached
        var result = await executor();
        
        // Store with TTL
        var expiry = DateTime.UtcNow.Add(ttl ?? _defaultTtl);
        _cache[cacheKey] = (result, expiry);
        
        return result;
    }
    
    public void Clear() => _cache.Clear();
    public int Count => _cache.Count;
}
```

#### Add Cache Tests (30 min)
```bash
File: DeepResearchAgent.Tests/Services/ToolResultCacheServiceTests.cs

Test Scenarios:
├─ Cache hit (result returned from cache)
├─ Cache miss (executor called)
├─ Cache expiry (expired cache removed)
├─ TTL configuration (custom TTLs)
└─ Clear operation (cache cleared)
```

#### Integrate with ToolInvocationService (15 min)
```csharp
// In ToolInvocationService
private readonly ToolResultCacheService _cacheService;

public ToolInvocationService(
    SearCrawl4AIService searchService,
    OllamaService llmService,
    ILogger<ToolInvocationService>? logger = null)
{
    _tools = new ResearchToolsImplementation(searchService, llmService, null);
    _cacheService = new ToolResultCacheService(TimeSpan.FromHours(1));
}

// Cache WebSearch results
private async Task<object> WebSearchToolAsync(
    Dictionary<string, object> parameters,
    CancellationToken cancellationToken)
{
    var query = GetRequiredParameter<string>(parameters, "query");
    var cacheKey = $"websearch:{query}";
    
    return await _cacheService.GetOrExecuteAsync(
        cacheKey,
        () => _tools.WebSearchAsync(query, 10, cancellationToken),
        TimeSpan.FromHours(1)
    );
}
```

---

### Feature 2.2: Confidence Scoring (1 hour)

**Purpose:** Weight search results by confidence/relevance

#### Create Confidence Scoring Models (20 min)
```csharp
// Add to Models/ToolResultModels.cs

public class ScoredSearchResult
{
    public WebSearchResult Result { get; set; }
    public float RelevanceScore { get; set; }      // 0-1
    public float SourceCredibility { get; set; }   // 0-1
    public float OverallConfidence { get; set; }   // 0-1
}

public class ConfidenceScorer
{
    public static float CalculateRelevanceScore(
        string query,
        string content)
    {
        // Simple: check keyword matches
        var queryWords = query.ToLower().Split();
        var contentLower = content.ToLower();
        var matches = queryWords.Count(w => contentLower.Contains(w));
        return Math.Min(1f, (float)matches / queryWords.Length);
    }
    
    public static float GetSourceCredibility(string url)
    {
        // Credibility based on domain
        if (url.Contains("gov") || url.Contains("edu")) return 0.95f;
        if (url.Contains("org")) return 0.85f;
        return 0.75f;
    }
    
    public static float CalculateOverallConfidence(
        float relevance,
        float credibility)
    {
        return (relevance * 0.6f) + (credibility * 0.4f);
    }
}
```

#### Update WebSearchTool (20 min)
```csharp
// In ResearchToolsImplementation
private async Task<List<ScoredSearchResult>> WebSearchWithScoringAsync(
    string query,
    int maxResults = 10,
    CancellationToken cancellationToken = default)
{
    var results = await WebSearchAsync(query, maxResults, cancellationToken);
    
    return results
        .Select(r => new ScoredSearchResult
        {
            Result = r,
            RelevanceScore = ConfidenceScorer.CalculateRelevanceScore(query, r.Content),
            SourceCredibility = ConfidenceScorer.GetSourceCredibility(r.Url),
            OverallConfidence = /* calculated */
        })
        .OrderByDescending(s => s.OverallConfidence)
        .ToList();
}
```

#### Add Scoring Tests (20 min)
```bash
Test Scenarios:
├─ Relevance score calculation
├─ Source credibility assessment
├─ Overall confidence combination
└─ Sorting by confidence
```

---

### Feature 2.3: Tool Chaining (1 hour)

**Purpose:** Enable output → input pipelines

#### Create ToolChainService (30 min)
```csharp
public class ToolChainService
{
    private readonly ToolInvocationService _toolService;
    private readonly ILogger<ToolChainService>? _logger;
    
    public async Task<List<object>> ChainToolsAsync(
        List<(string ToolName, Dictionary<string, object> Parameters)> tools,
        CancellationToken cancellationToken = default)
    {
        var results = new List<object>();
        object previousOutput = null;
        
        foreach (var (toolName, parameters) in tools)
        {
            try
            {
                // Use previous output as input if available
                if (previousOutput is List<WebSearchResult> searchResults)
                {
                    // First result URL can feed to next tool
                    parameters["pageContent"] = searchResults.First().Content;
                }
                
                var result = await _toolService.InvokeToolAsync(
                    toolName, parameters, cancellationToken);
                
                results.Add(result);
                previousOutput = result;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Tool {Tool} failed in chain", toolName);
                throw;
            }
        }
        
        return results;
    }
}
```

#### Example Chain Usage
```csharp
// WebSearch → Summarize → ExtractFacts
var chain = new List<(string, Dictionary<string, object>)>
{
    ("websearch", new Dictionary<string, object> 
    {
        { "query", "quantum computing" },
        { "maxResults", 1 }
    }),
    ("summarize", new Dictionary<string, object>
    {
        { "pageContent", "" } // Will be filled from previous
    }),
    ("extractfacts", new Dictionary<string, object>
    {
        { "content", "" }, // Will be filled from previous
        { "topic", "quantum computing" }
    })
};

var results = await _chainService.ChainToolsAsync(chain);
```

#### Add Chaining Tests (30 min)
```bash
Test Scenarios:
├─ Simple 2-tool chain
├─ Complex 4-tool chain
├─ Error in middle of chain
└─ Output type compatibility
```

---

### Feature 2.4: Final Optimization (30 min)

#### Performance Tuning
- [ ] Add parallel execution for independent searches
- [ ] Implement request batching for LLM calls
- [ ] Add streaming support for long summarizations
- [ ] Profile tool execution times

#### Build & Test
```bash
dotnet build          # Verify 0 errors
dotnet test           # Verify all tests passing
```

---

## 🎯 EXECUTION CHECKLIST

### Feature 2.1: Caching
- [ ] Create ToolResultCacheService.cs
- [ ] Implement GetOrExecuteAsync method
- [ ] Add Clear() and Count property
- [ ] Create ToolResultCacheServiceTests.cs
- [ ] Add 4-5 unit tests
- [ ] Integrate with ToolInvocationService
- [ ] Update WebSearch to use cache
- [ ] Build & test ✅

### Feature 2.2: Scoring
- [ ] Create ScoredSearchResult model
- [ ] Create ConfidenceScorer class
- [ ] Update WebSearchTool signature
- [ ] Implement confidence calculation
- [ ] Update ToolInvocationService routing
- [ ] Create scoring tests (3-4 tests)
- [ ] Build & test ✅

### Feature 2.3: Chaining
- [ ] Create ToolChainService.cs
- [ ] Implement ChainToolsAsync method
- [ ] Add error handling in chain
- [ ] Create ToolChainServiceTests.cs
- [ ] Test 2-tool chain
- [ ] Test 4-tool chain
- [ ] Test error handling
- [ ] Build & test ✅

### Feature 2.4: Optimization
- [ ] Profile execution
- [ ] Add parallel where applicable
- [ ] Final build verification
- [ ] Final test run

---

## ⏱️ TIME ALLOCATION

```
Feature 2.1: Caching (1.5 hours)
├─ Service implementation: 45 min
├─ Unit tests: 30 min
└─ Integration: 15 min

Feature 2.2: Confidence Scoring (1 hour)
├─ Model creation: 20 min
├─ Tool update: 20 min
└─ Tests: 20 min

Feature 2.3: Tool Chaining (1 hour)
├─ Service creation: 30 min
└─ Tests: 30 min

Feature 2.4: Optimization (30 min)
├─ Performance tuning: 20 min
└─ Final verification: 10 min

TOTAL: 4 hours
```

---

## 🚨 IMPORTANT NOTES

### What to Watch For
1. **Caching Key Format:** Use consistent keys (e.g., `"websearch:query"`)
2. **TTL Handling:** Verify expired entries are removed
3. **Thread Safety:** Consider locks if parallel access
4. **Chain Order:** Results must be compatible with next tool's input
5. **Error Propagation:** Errors in chain should fail the whole chain

### Testing Strategy
- Unit test each feature independently
- Integration test with real tool invocations
- Profile performance improvements
- Verify no regression in existing tests

---

## 📞 SUPPORT

**Reference Files:**
- `PHASE3_SPRINT1_COMPLETE.md` - Sprint 1 results
- `ToolInvocationService.cs` - Integration pattern
- `ResearchToolsImplementation.cs` - Tool implementations

**Code Examples:**
- Cache: Use `GetOrExecuteAsync<T>(cacheKey, executor, ttl)`
- Score: Use `ConfidenceScorer.CalculateOverallConfidence()`
- Chain: Use `ChainToolsAsync(tools)` for pipelines

---

## 🎯 SUCCESS CRITERIA

**After Sprint 2 Completion:**
- [ ] ToolResultCacheService implemented & tested (4 tests)
- [ ] Confidence scoring implemented & tested (3 tests)
- [ ] Tool chaining implemented & tested (3 tests)
- [ ] Build: 0 errors, 0 warnings
- [ ] Tests: 60+ passing (50 + 10 new)
- [ ] Phase 3: 100% COMPLETE ✅
- [ ] Ready for Phase 4 ✅

---

**Sprint 2: Advanced Features Ready to Execute! 🚀**

**Expected Completion: 4 hours**

**Then: Phase 3 100% DONE + Ready for Phase 4!**

**LET'S DO THIS! 💪**
