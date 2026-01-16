# SearCrawl4AI Integration Guide

This document provides a comprehensive guide for using the SearCrawl4AI service in the Deep Research Agent project.

## Overview

SearCrawl4AI combines two powerful open-source tools:
- **SearXNG**: Privacy-respecting metasearch engine that queries multiple search engines
- **Crawl4AI**: Advanced web scraping tool that extracts clean, structured content

## Quick Start

### 1. Start the Services

```bash
# Start all services including SearXNG and Crawl4AI
docker-compose up -d

# Or start only the search services
docker-compose up -d searxng crawl4ai
```

### 2. Verify Services Are Running

```bash
# Test SearXNG
curl "http://localhost:8080/search?q=test&format=json"

# Test Crawl4AI
curl -X POST "http://localhost:8000/crawl" \
  -H "Content-Type: application/json" \
  -d '{"urls": ["https://example.com"]}'
```

### 3. Use in Your Code

```csharp
using DeepResearchAgent.Services;
using DeepResearchAgent.Tools;

// Option 1: Direct service usage
var httpClient = new HttpClient();
var searchService = new SearCrawl4AIService(
    httpClient,
    "http://localhost:8080",
    "http://localhost:8000"
);

var results = await searchService.SearchAndScrapeAsync("quantum computing", maxResults: 5);

// Option 2: Via Research Tools
var summary = await ResearchTools.WebSearch("AI agents", maxResults: 10, searchService);
var deepReport = await ResearchTools.DeepWebSearch("transformer architecture", maxPages: 3, searchService);
```

## Use Cases

### Use Case 1: Fact-Finding (Fast)

When you need quick facts and summaries:

```csharp
var searchService = new SearCrawl4AIService(httpClient);
var results = await searchService.SearchAsync("capital of France", maxResults: 3);

foreach (var result in results.Results)
{
    Console.WriteLine($"{result.Title}: {result.Content}");
}
```

### Use Case 2: Deep Research (Comprehensive)

When you need full content from multiple sources:

```csharp
var scrapedPages = await searchService.SearchAndScrapeAsync(
    query: "attention mechanism in neural networks",
    maxResults: 5
);

foreach (var page in scrapedPages)
{
    // Process full markdown content
    var facts = ExtractFacts(page.Markdown);
    SaveToKnowledgeBase(facts, page.Url);
}
```

### Use Case 3: Targeted Scraping

When you have specific URLs to scrape:

```csharp
var urls = new List<string>
{
    "https://arxiv.org/abs/1706.03762",  // Research paper
    "https://en.wikipedia.org/wiki/Transformer_(machine_learning_model)"
};

var options = new Crawl4AIRequest
{
    ExtractionStrategy = "CosineStrategy",
    WordCountThreshold = "100",
    Bypass_cache = true
};

var scrapeResult = await searchService.ScrapeAsync(urls, options);
```

## Integration Patterns

### Pattern 1: Research Workflow Integration

```csharp
public async Task<ResearchReport> ConductResearch(string topic)
{
    var searchService = new SearCrawl4AIService(httpClient);
    
    // Phase 1: Broad search
    var initialResults = await searchService.SearchAsync(topic, maxResults: 20);
    
    // Phase 2: Filter and scrape top results
    var topUrls = initialResults.Results
        .OrderByDescending(r => r.Score ?? 0)
        .Take(5)
        .Select(r => r.Url)
        .ToList();
    
    var scrapedContent = await searchService.ScrapeAsync(topUrls);
    
    // Phase 3: Extract and synthesize
    var facts = scrapedContent.Results
        .Where(c => c.Success)
        .SelectMany(c => ExtractFacts(c.Markdown))
        .ToList();
    
    return new ResearchReport
    {
        Topic = topic,
        Sources = topUrls,
        Facts = facts,
        GeneratedAt = DateTime.UtcNow
    };
}
```

### Pattern 2: Iterative Refinement

```csharp
public async Task<string> RefineQuery(string initialQuery, int iterations = 3)
{
    var searchService = new SearCrawl4AIService(httpClient);
    var currentQuery = initialQuery;
    
    for (int i = 0; i < iterations; i++)
    {
        // Search and scrape
        var results = await searchService.SearchAndScrapeAsync(currentQuery, maxResults: 3);
        
        // Analyze results and refine query
        var keywords = ExtractKeywords(results);
        currentQuery = RefineQueryWithKeywords(currentQuery, keywords);
        
        Console.WriteLine($"Iteration {i + 1}: {currentQuery}");
    }
    
    return currentQuery;
}
```

### Pattern 3: Multi-Source Validation

```csharp
public async Task<ValidatedFact> ValidateFact(string claim)
{
    var searchService = new SearCrawl4AIService(httpClient);
    
    // Search for the claim
    var results = await searchService.SearchAndScrapeAsync(claim, maxResults: 10);
    
    // Analyze sources for agreement/disagreement
    var sources = results.Select(r => new
    {
        Url = r.Url,
        Content = r.Markdown,
        Supports = AnalyzeSupport(r.Markdown, claim)
    }).ToList();
    
    var supportCount = sources.Count(s => s.Supports);
    var confidence = (double)supportCount / sources.Count;
    
    return new ValidatedFact
    {
        Claim = claim,
        Confidence = confidence,
        Sources = sources.Select(s => s.Url).ToList()
    };
}
```

## Configuration

### Environment Variables

Set these in your environment or docker-compose.yml:

```bash
SEARXNG_BASE_URL=http://localhost:8080
CRAWL4AI_BASE_URL=http://localhost:8000
```

### Code Configuration

```csharp
var config = new SearCrawl4AIConfig
{
    SearXNGBaseUrl = Environment.GetEnvironmentVariable("SEARXNG_BASE_URL") ?? "http://localhost:8080",
    Crawl4AIBaseUrl = Environment.GetEnvironmentVariable("CRAWL4AI_BASE_URL") ?? "http://localhost:8000",
    DefaultMaxResults = 5,
    RequestTimeoutSeconds = 30,
    EnableLogging = true
};

var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(config.RequestTimeoutSeconds)
};

var service = new SearCrawl4AIService(
    httpClient,
    config.SearXNGBaseUrl,
    config.Crawl4AIBaseUrl
);
```

## Advanced Features

### Custom Scraping Strategies

Crawl4AI supports multiple extraction strategies:

```csharp
// Cosine similarity-based extraction (best for articles)
var options = new Crawl4AIRequest
{
    ExtractionStrategy = "CosineStrategy",
    WordCountThreshold = "50"
};

// CSS selector-based extraction
var options = new Crawl4AIRequest
{
    CssSelector = "article.main-content",
    ChunkingStrategy = "RegexChunking"
};

// Full page extraction
var options = new Crawl4AIRequest
{
    ExtractionStrategy = "NoExtractionStrategy"
};
```

### Search Engine Selection

Configure SearXNG to use specific search engines via `searxng/settings.yml`:

```yaml
engines:
  - name: google
    disabled: false
  - name: scholar
    disabled: false  # Enable for academic papers
  - name: arxiv
    disabled: false  # Enable for research papers
```

### Caching and Performance

```csharp
// Bypass cache for fresh results
var options = new Crawl4AIRequest
{
    Bypass_cache = true
};

// Use cache for repeated queries (default)
var options = new Crawl4AIRequest
{
    Bypass_cache = false
};
```

## Error Handling

### Robust Error Handling

```csharp
try
{
    var results = await searchService.SearchAndScrapeAsync(query, maxResults: 5);
    
    // Check for partial failures
    var successfulScrapes = results.Where(r => r.Success).ToList();
    var failedScrapes = results.Where(r => !r.Success).ToList();
    
    if (failedScrapes.Any())
    {
        Console.WriteLine($"Warning: {failedScrapes.Count} scrapes failed");
        foreach (var failed in failedScrapes)
        {
            Console.WriteLine($"  {failed.Url}: {failed.ErrorMessage}");
        }
    }
    
    // Process successful results
    ProcessResults(successfulScrapes);
}
catch (InvalidOperationException ex)
{
    // Service connection error
    Console.WriteLine($"Service unavailable: {ex.Message}");
    // Fallback to alternative search method
}
catch (HttpRequestException ex)
{
    // Network error
    Console.WriteLine($"Network error: {ex.Message}");
    // Retry logic
}
```

### Retry Logic

```csharp
public async Task<List<ScrapedContent>> SearchWithRetry(
    string query, 
    int maxRetries = 3,
    int delayMs = 1000)
{
    var searchService = new SearCrawl4AIService(httpClient);
    
    for (int attempt = 0; attempt < maxRetries; attempt++)
    {
        try
        {
            return await searchService.SearchAndScrapeAsync(query);
        }
        catch (Exception ex)
        {
            if (attempt == maxRetries - 1) throw;
            
            Console.WriteLine($"Attempt {attempt + 1} failed: {ex.Message}");
            await Task.Delay(delayMs * (attempt + 1));
        }
    }
    
    return new List<ScrapedContent>();
}
```

## Performance Tips

1. **Limit Results**: Start with fewer results and increase if needed
   ```csharp
   var results = await searchService.SearchAsync(query, maxResults: 3);  // Start small
   ```

2. **Parallel Scraping**: Crawl4AI handles multiple URLs efficiently
   ```csharp
   var urls = searchResults.Select(r => r.Url).Take(10).ToList();
   var scrapeResponse = await searchService.ScrapeAsync(urls);  // Scrapes in parallel
   ```

3. **Use Caching**: Don't bypass cache unless necessary
   ```csharp
   var options = new Crawl4AIRequest { Bypass_cache = false };
   ```

4. **Filter Before Scraping**: Pre-filter URLs to reduce scraping load
   ```csharp
   var relevantUrls = searchResults.Results
       .Where(r => IsRelevant(r.Content))
       .Select(r => r.Url)
       .ToList();
   ```

## Monitoring and Debugging

### Enable Logging

```csharp
var logger = new ConsoleLogger();  // Or your ILogger implementation
var service = new SearCrawl4AIService(httpClient, searxngUrl, crawl4aiUrl, logger);
```

### Check Service Health

```bash
# SearXNG health
curl http://localhost:8080/healthz

# Crawl4AI health
curl http://localhost:8000/health

# View logs
docker-compose logs -f searxng
docker-compose logs -f crawl4ai
```

## Troubleshooting

### Issue: SearXNG returns no results

**Solution:**
```bash
# Check SearXNG settings
cat searxng/settings.yml

# Verify engines are enabled
docker-compose exec searxng cat /etc/searxng/settings.yml

# Restart SearXNG
docker-compose restart searxng
```

### Issue: Crawl4AI scraping fails

**Solution:**
```csharp
// Try with different strategies
var options = new Crawl4AIRequest
{
    ExtractionStrategy = "NoExtractionStrategy",  // Get full page
    Bypass_cache = true,
    Verbose = true
};

// Check for blocked user agents
options.UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";
```

### Issue: Timeout errors

**Solution:**
```csharp
// Increase timeout
var httpClient = new HttpClient
{
    Timeout = TimeSpan.FromSeconds(60)  // Increase from default
};
```

## Examples

See `/DeepResearchAgent/Examples/SearCrawl4AIExample.cs` for complete working examples.

Run examples:
```bash
cd DeepResearchAgent
dotnet run --project DeepResearchAgent.csproj
```

## API Reference

Full API documentation available in:
- `/DeepResearchAgent/Services/README_SEARCRAWL4AI.md`
- Inline XML documentation in service files

## Next Steps

1. Integrate with LiteDB for persistent caching
2. Add result ranking and relevance scoring
3. Implement parallel search across multiple queries
4. Add support for image and video search
5. Build custom extraction rules for common sites

## Support

For issues and questions:
- Check docker-compose logs: `docker-compose logs`
- Verify service connectivity
- Review configuration files
- Check network settings

## License

Part of the Deep Research Agent project.
