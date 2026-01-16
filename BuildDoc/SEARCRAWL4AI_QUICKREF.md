# SearCrawl4AI Quick Reference

## Service Endpoints

| Service | URL | Port | Purpose |
|---------|-----|------|---------|
| SearXNG | http://localhost:8080 | 8080 | Web search (metasearch) |
| Crawl4AI | http://localhost:8000 | 8000 | Web scraping |

## Start Services

```bash
docker-compose up -d searxng crawl4ai
```

## Basic Usage

### Initialize Service
```csharp
using DeepResearchAgent.Services;

var httpClient = new HttpClient();
var service = new SearCrawl4AIService(
    httpClient,
    "http://localhost:8080",  // SearXNG
    "http://localhost:8000"   // Crawl4AI
);
```

### Search Only
```csharp
var results = await service.SearchAsync("quantum computing", maxResults: 10);
foreach (var result in results.Results)
{
    Console.WriteLine($"{result.Title}: {result.Url}");
}
```

### Scrape Only
```csharp
var urls = new List<string> { "https://example.com" };
var scraped = await service.ScrapeAsync(urls);
foreach (var content in scraped.Results)
{
    Console.WriteLine(content.Markdown);
}
```

### Search & Scrape (Recommended)
```csharp
var results = await service.SearchAndScrapeAsync("AI research", maxResults: 5);
foreach (var content in results)
{
    Console.WriteLine($"## {content.Url}\n{content.Markdown}");
}
```

## Research Tools

```csharp
using DeepResearchAgent.Tools;

// Quick search
var summary = await ResearchTools.WebSearch("topic", 10, service);

// Deep research
var report = await ResearchTools.DeepWebSearch("topic", 3, service);
```

## Advanced Options

```csharp
var options = new Crawl4AIRequest
{
    ExtractionStrategy = "CosineStrategy",
    WordCountThreshold = "100",
    CssSelector = "article",
    Bypass_cache = true,
    Screenshot = false
};

var results = await service.ScrapeAsync(urls, options);
```

## Common Patterns

### Validate Facts
```csharp
var results = await service.SearchAndScrapeAsync("claim to verify", 10);
var sources = results.Select(r => new { r.Url, r.Markdown }).ToList();
// Analyze for agreement/disagreement
```

### Extract Citations
```csharp
var results = await service.SearchAsync("topic", 20);
var citations = results.Results.Select(r => 
    $"[{r.Title}]({r.Url})").ToList();
```

### Deep Analysis
```csharp
var results = await service.SearchAndScrapeAsync("research topic", 10);
var facts = results.SelectMany(r => ExtractFacts(r.Markdown)).ToList();
```

## Error Handling

```csharp
try
{
    var results = await service.SearchAndScrapeAsync(query);
    var successful = results.Where(r => r.Success).ToList();
    var failed = results.Where(r => !r.Success).ToList();
}
catch (InvalidOperationException ex)
{
    // Service error
}
catch (HttpRequestException ex)
{
    // Network error
}
```

## Configuration

### Environment Variables
```bash
export SEARXNG_BASE_URL=http://localhost:8080
export CRAWL4AI_BASE_URL=http://localhost:8000
```

### Code Config
```csharp
var config = new SearCrawl4AIConfig
{
    SearXNGBaseUrl = "http://localhost:8080",
    Crawl4AIBaseUrl = "http://localhost:8000",
    DefaultMaxResults = 5,
    RequestTimeoutSeconds = 30
};
```

## Troubleshooting

### Check Service Health
```bash
curl http://localhost:8080/search?q=test&format=json
curl http://localhost:8000/health
```

### View Logs
```bash
docker-compose logs -f searxng
docker-compose logs -f crawl4ai
```

### Restart Services
```bash
docker-compose restart searxng crawl4ai
```

## Data Models

### SearchResult
- `Title` - Result title
- `Url` - Source URL
- `Content` - Snippet/summary
- `Engine` - Search engine name
- `Score` - Relevance score

### ScrapedContent
- `Url` - Scraped URL
- `Markdown` - Clean markdown
- `Html` - Raw HTML
- `CleanedHtml` - Cleaned HTML
- `Metadata` - Page metadata
- `Success` - Scrape status

## Performance Tips

1. **Start small**: Begin with 3-5 results
2. **Use caching**: Don't bypass unless needed
3. **Filter first**: Pre-filter URLs before scraping
4. **Parallel scraping**: Crawl4AI handles multiple URLs efficiently

## Examples

See: `DeepResearchAgent/Examples/SearCrawl4AIExample.cs`

```bash
cd DeepResearchAgent
dotnet run
```

## Documentation

- **API Reference**: `DeepResearchAgent/Services/README_SEARCRAWL4AI.md`
- **Usage Guide**: `SEARCRAWL4AI_GUIDE.md`
- **Implementation**: `IMPLEMENTATION_SUMMARY_SEARCRAWL4AI.md`

## Common Use Cases

| Use Case | Method | Max Results | Time |
|----------|--------|-------------|------|
| Quick fact | `SearchAsync` | 3-5 | ~1s |
| Verify claim | `SearchAsync` | 10-20 | ~2s |
| Deep research | `SearchAndScrapeAsync` | 5-10 | ~30s |
| Specific sources | `ScrapeAsync` | N/A | 2-5s/page |

## Integration

```csharp
// In research workflow
var searchService = new SearCrawl4AIService(httpClient);

// Phase 1: Broad search
var searchResults = await searchService.SearchAsync(topic, 20);

// Phase 2: Deep dive
var topUrls = searchResults.Results.Take(5).Select(r => r.Url).ToList();
var scraped = await searchService.ScrapeAsync(topUrls);

// Phase 3: Extract facts
var facts = scraped.Results
    .Where(r => r.Success)
    .SelectMany(r => ExtractFacts(r.Markdown))
    .ToList();
```

---

**Quick Start**: `docker-compose up -d` → `var service = new SearCrawl4AIService(httpClient)` → `await service.SearchAndScrapeAsync("query")`
