# SearCrawl4AI Service

A unified web search and scraping service that combines SearXNG (privacy-respecting metasearch) with Crawl4AI (advanced web scraping) for the Deep Research Agent.

## Overview

The `SearCrawl4AIService` provides three main capabilities:

1. **Web Search** - Query multiple search engines via SearXNG
2. **Web Scraping** - Extract clean content from web pages via Crawl4AI  
3. **Search & Scrape** - Combined operation for deep research

## Architecture

```
┌─────────────────────────────────────────────────┐
│          Deep Research Agent                     │
│                                                  │
│  ┌──────────────────────────────────────────┐  │
│  │      SearCrawl4AIService                  │  │
│  │                                            │  │
│  │  ┌─────────────┐    ┌─────────────┐      │  │
│  │  │   SearXNG   │───>│  Crawl4AI   │      │  │
│  │  │  (Search)   │    │  (Scrape)   │      │  │
│  │  └─────────────┘    └─────────────┘      │  │
│  └──────────────────────────────────────────┘  │
└─────────────────────────────────────────────────┘
```

## Quick Start

### 1. Start Services with Docker Compose

```bash
docker-compose up -d searxng crawl4ai
```

This starts:
- **SearXNG** on `http://localhost:8080`
- **Crawl4AI** on `http://localhost:8000`

### 2. Use in Code

```csharp
using DeepResearchAgent.Services;
using DeepResearchAgent.Models;

// Create service instance
var httpClient = new HttpClient();
var service = new SearCrawl4AIService(
    httpClient,
    searxngBaseUrl: "http://localhost:8080",
    crawl4aiBaseUrl: "http://localhost:8000"
);

// Example 1: Simple search
var searchResults = await service.SearchAsync("quantum computing", maxResults: 10);
foreach (var result in searchResults.Results)
{
    Console.WriteLine($"{result.Title}: {result.Url}");
}

// Example 2: Scrape specific URLs
var urls = new List<string> { "https://example.com", "https://example.org" };
var scrapeResponse = await service.ScrapeAsync(urls);
foreach (var content in scrapeResponse.Results)
{
    Console.WriteLine($"Scraped {content.Url}");
    Console.WriteLine(content.Markdown);
}

// Example 3: Combined search and scrape (most powerful)
var deepResults = await service.SearchAndScrapeAsync("AI agents", maxPages: 5);
foreach (var content in deepResults)
{
    Console.WriteLine($"## {content.Url}\n");
    Console.WriteLine(content.Markdown);
}
```

### 3. Use via Research Tools

```csharp
using DeepResearchAgent.Tools;

// Simple web search
var searchSummary = await ResearchTools.WebSearch(
    "deep learning architectures", 
    maxResults: 5,
    searchService: service
);

// Deep search with full content scraping
var deepReport = await ResearchTools.DeepWebSearch(
    "transformer models in NLP",
    maxPages: 3,
    searchService: service
);
```

## Configuration

### Environment Variables (Docker)

```yaml
environment:
  - SEARXNG_BASE_URL=http://searxng:8080
  - CRAWL4AI_BASE_URL=http://crawl4ai:8000
```

### Code Configuration

```csharp
var config = new SearCrawl4AIConfig
{
    SearXNGBaseUrl = "http://localhost:8080",
    Crawl4AIBaseUrl = "http://localhost:8000",
    DefaultMaxResults = 5,
    RequestTimeoutSeconds = 30,
    EnableLogging = true
};

var service = new SearCrawl4AIService(
    httpClient,
    config.SearXNGBaseUrl,
    config.Crawl4AIBaseUrl
);
```

## API Reference

### ISearCrawl4AIService Interface

#### SearchAsync

Search the web using SearXNG.

```csharp
Task<SearXNGResponse> SearchAsync(
    string query, 
    int maxResults = 10, 
    CancellationToken cancellationToken = default)
```

**Returns:** `SearXNGResponse` with search results including titles, URLs, and snippets.

#### ScrapeAsync

Scrape content from URLs using Crawl4AI.

```csharp
Task<Crawl4AIResponse> ScrapeAsync(
    List<string> urls, 
    Crawl4AIRequest? options = null, 
    CancellationToken cancellationToken = default)
```

**Returns:** `Crawl4AIResponse` with scraped content in HTML, Markdown, and cleaned formats.

#### SearchAndScrapeAsync

Combined operation: search then scrape top results.

```csharp
Task<List<ScrapedContent>> SearchAndScrapeAsync(
    string query, 
    int maxResults = 5, 
    Crawl4AIRequest? scrapeOptions = null,
    CancellationToken cancellationToken = default)
```

**Returns:** List of `ScrapedContent` objects with full page content.

## Data Models

### SearchResult

```csharp
public class SearchResult
{
    public string Title { get; set; }
    public string Url { get; set; }
    public string Content { get; set; }
    public string Engine { get; set; }
    public double? Score { get; set; }
}
```

### ScrapedContent

```csharp
public class ScrapedContent
{
    public string Url { get; set; }
    public string Html { get; set; }
    public string Markdown { get; set; }
    public string CleanedHtml { get; set; }
    public string Media { get; set; }
    public string Links { get; set; }
    public Dictionary<string, object> Metadata { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}
```

### Crawl4AIRequest

```csharp
public class Crawl4AIRequest
{
    public List<string> Urls { get; set; }
    public string WordCountThreshold { get; set; } = "10";
    public string ExtractionStrategy { get; set; } = "NoExtractionStrategy";
    public string ChunkingStrategy { get; set; } = "RegexChunking";
    public bool Bypass_cache { get; set; } = false;
    public string CssSelector { get; set; } = string.Empty;
    public bool Screenshot { get; set; } = false;
    public string UserAgent { get; set; } = string.Empty;
    public bool Verbose { get; set; } = true;
}
```

## SearXNG Configuration

Create a `searxng/settings.yml` file to customize search behavior:

```yaml
general:
  instance_name: "Deep Research SearXNG"
  
search:
  formats:
    - json
    - html

server:
  secret_key: "changeme"
  limiter: false
  
engines:
  - name: google
    disabled: false
  - name: duckduckgo
    disabled: false
  - name: wikipedia
    disabled: false
```

## Crawl4AI Options

Advanced scraping options via `Crawl4AIRequest`:

```csharp
var options = new Crawl4AIRequest
{
    ExtractionStrategy = "CosineStrategy",  // or "NoExtractionStrategy"
    ChunkingStrategy = "RegexChunking",     // or "NlpChunking"
    WordCountThreshold = "50",               // Min words per chunk
    CssSelector = "article",                 // Target specific elements
    Screenshot = true,                       // Capture screenshots
    Bypass_cache = true                      // Force fresh scrape
};

var results = await service.ScrapeAsync(urls, options);
```

## Error Handling

The service includes comprehensive error handling:

```csharp
try
{
    var results = await service.SearchAndScrapeAsync("query");
}
catch (InvalidOperationException ex)
{
    // Handle SearXNG or Crawl4AI connection errors
    Console.WriteLine($"Service error: {ex.Message}");
}
catch (HttpRequestException ex)
{
    // Handle network errors
    Console.WriteLine($"Network error: {ex.Message}");
}
```

## Testing

```bash
# Test SearXNG
curl "http://localhost:8080/search?q=test&format=json"

# Test Crawl4AI
curl -X POST "http://localhost:8000/crawl" \
  -H "Content-Type: application/json" \
  -d '{"urls": ["https://example.com"]}'
```

## Troubleshooting

### SearXNG not responding

```bash
docker-compose logs searxng
docker-compose restart searxng
```

### Crawl4AI scraping fails

- Check if URLs are accessible
- Try with `Bypass_cache = true`
- Increase timeout in `Crawl4AIRequest`

### No search results

- Verify SearXNG engines are enabled in `settings.yml`
- Check SearXNG logs for API errors
- Test with simpler queries

## Integration with Research Agent

The service integrates seamlessly with the research workflow:

1. **Researcher agents** use `WebSearch()` for quick fact-finding
2. **Deep research** uses `SearchAndScrapeAsync()` for comprehensive analysis
3. **Context pruner** extracts facts from scraped markdown content
4. **Knowledge base** stores URLs and content for provenance tracking

## Performance Considerations

- SearXNG queries are fast (~500ms typical)
- Crawl4AI scraping is slower (2-5s per page)
- Use `maxResults` wisely to balance speed vs. comprehensiveness
- Consider caching scraped content for repeated queries

## Future Enhancements

- [ ] Result caching and deduplication
- [ ] Parallel scraping for faster throughput
- [ ] Intelligent chunk extraction for large pages
- [ ] PDF and document scraping support
- [ ] Rate limiting and retry logic
- [ ] Integration with LiteDB for persistence

## License

Part of the Deep Research Agent project.
