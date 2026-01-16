using Xunit;
using DeepResearchAgent.Services;
using DeepResearchAgent.Models;
using System.Net;
using System.Net.Http;
using Moq;
using Moq.Protected;

namespace DeepResearchAgent.Tests.Services;

/// <summary>
/// Unit tests for SearCrawl4AI service
/// Note: Add xUnit and Moq NuGet packages to run these tests
/// </summary>
public class SearCrawl4AIServiceTests
{
    [Fact]
    public async Task SearchAsync_ReturnsResults_WhenSearXNGRespondsSuccessfully()
    {
        // Arrange
        var mockResponse = @"{
            ""query"": ""test query"",
            ""results"": [
                {
                    ""title"": ""Test Result"",
                    ""url"": ""https://example.com"",
                    ""content"": ""Test content"",
                    ""engine"": ""google"",
                    ""score"": 1.0
                }
            ]
        }";

        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new SearCrawl4AIService(httpClient, "http://test:8080", "http://test:8000");

        // Act
        var result = await service.SearchAsync("test query", maxResults: 10);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("test query", result.Query);
        Assert.Single(result.Results);
        Assert.Equal("Test Result", result.Results[0].Title);
        Assert.Equal("https://example.com", result.Results[0].Url);
    }

    [Fact]
    public async Task SearchAsync_LimitsResults_WhenMaxResultsSpecified()
    {
        // Arrange
        var mockResponse = @"{
            ""results"": [
                {""title"": ""Result 1"", ""url"": ""https://example1.com"", ""content"": ""Content 1""},
                {""title"": ""Result 2"", ""url"": ""https://example2.com"", ""content"": ""Content 2""},
                {""title"": ""Result 3"", ""url"": ""https://example3.com"", ""content"": ""Content 3""}
            ]
        }";

        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new SearCrawl4AIService(httpClient, "http://test:8080", "http://test:8000");

        // Act
        var result = await service.SearchAsync("test", maxResults: 2);

        // Assert
        Assert.Equal(2, result.Results.Count);
    }

    [Fact]
    public async Task ScrapeAsync_ReturnsScrapedContent_WhenCrawl4AIRespondsSuccessfully()
    {
        // Arrange
        var mockResponse = @"{
            ""results"": [
                {
                    ""url"": ""https://example.com"",
                    ""html"": ""<html>Test</html>"",
                    ""markdown"": ""# Test"",
                    ""cleaned_html"": ""<p>Test</p>"",
                    ""success"": true
                }
            ]
        }";

        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new SearCrawl4AIService(httpClient, "http://test:8080", "http://test:8000");

        // Act
        var result = await service.ScrapeAsync(new List<string> { "https://example.com" });

        // Assert
        Assert.True(result.Success);
        Assert.Single(result.Results);
        Assert.Equal("https://example.com", result.Results[0].Url);
        Assert.Equal("# Test", result.Results[0].Markdown);
        Assert.True(result.Results[0].Success);
    }

    [Fact]
    public async Task ScrapeAsync_HandlesFailedScrapes_Gracefully()
    {
        // Arrange
        var mockResponse = @"{
            ""results"": [
                {
                    ""url"": ""https://example.com"",
                    ""success"": false,
                    ""error_message"": ""Connection timeout""
                }
            ]
        }";

        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new SearCrawl4AIService(httpClient, "http://test:8080", "http://test:8000");

        // Act
        var result = await service.ScrapeAsync(new List<string> { "https://example.com" });

        // Assert
        Assert.True(result.Success);  // Service call succeeded
        Assert.Single(result.Results);
        Assert.False(result.Results[0].Success);  // But scraping failed
        Assert.Equal("Connection timeout", result.Results[0].ErrorMessage);
    }

    [Fact]
    public async Task SearchAndScrapeAsync_CombinesSearchAndScrape_Successfully()
    {
        // Arrange
        var searchResponse = @"{
            ""results"": [
                {""title"": ""Test"", ""url"": ""https://example.com"", ""content"": ""Test content""}
            ]
        }";

        var scrapeResponse = @"{
            ""results"": [
                {
                    ""url"": ""https://example.com"",
                    ""markdown"": ""# Test Content"",
                    ""success"": true
                }
            ]
        }";

        // Create a mock that returns different responses based on the request
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .SetupSequence<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(searchResponse)
            })
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent(scrapeResponse)
            });

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new SearCrawl4AIService(httpClient, "http://test:8080", "http://test:8000");

        // Act
        var results = await service.SearchAndScrapeAsync("test query", maxResults: 1);

        // Assert
        Assert.Single(results);
        Assert.Equal("https://example.com", results[0].Url);
        Assert.Equal("# Test Content", results[0].Markdown);
        Assert.True(results[0].Success);
    }

    [Fact]
    public async Task SearchAsync_ThrowsException_WhenSearXNGReturnsError()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("Error", HttpStatusCode.InternalServerError);
        var service = new SearCrawl4AIService(httpClient, "http://test:8080", "http://test:8000");

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            async () => await service.SearchAsync("test query")
        );
    }

    [Fact]
    public void SearCrawl4AIConfig_HasDefaultValues()
    {
        // Arrange & Act
        var config = new SearCrawl4AIConfig();

        // Assert
        Assert.Equal("http://localhost:8080", config.SearXNGBaseUrl);
        Assert.Equal("http://localhost:11235", config.Crawl4AIBaseUrl);
        Assert.Equal(5, config.DefaultMaxResults);
        Assert.True(config.EnableLogging);
    }

    // Helper method to create a mock HttpClient
    private static HttpClient CreateMockHttpClient(string responseContent, HttpStatusCode statusCode)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler.Protected()
            .Setup<Task<HttpResponseMessage>>("SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });

        return new HttpClient(mockHandler.Object);
    }
}

/// <summary>
/// Integration tests for SearCrawl4AI service
/// These require actual SearXNG and Crawl4AI services to be running
/// </summary>
public class SearCrawl4AIServiceIntegrationTests
{
    private readonly bool _servicesAvailable;

    public SearCrawl4AIServiceIntegrationTests()
    {
        // Check if services are available before running tests
        _servicesAvailable = CheckServicesAvailable().Result;
    }

    [Fact(Skip = "Integration test - requires SearXNG and Crawl4AI services")]
    public async Task SearchAsync_Integration_ReturnsRealResults()
    {
        if (!_servicesAvailable) return;

        // Arrange
        var httpClient = new HttpClient();
        var service = new SearCrawl4AIService(httpClient);

        // Act
        var results = await service.SearchAsync("artificial intelligence", maxResults: 5);

        // Assert
        Assert.NotEmpty(results.Results);
        Assert.All(results.Results, r => Assert.NotEmpty(r.Url));
    }

    [Fact(Skip = "Integration test - requires SearXNG and Crawl4AI services")]
    public async Task SearchAndScrapeAsync_Integration_ReturnsScrappedContent()
    {
        if (!_servicesAvailable) return;

        // Arrange
        var httpClient = new HttpClient();
        var service = new SearCrawl4AIService(httpClient);

        // Act
        var results = await service.SearchAndScrapeAsync("machine learning", maxResults: 2);

        // Assert
        Assert.NotEmpty(results);
        Assert.All(results, r => Assert.NotEmpty(r.Markdown));
    }

    private async Task<bool> CheckServicesAvailable()
    {
        try
        {
            using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(2) };
            
            var searxngResponse = await httpClient.GetAsync("http://localhost:8080");
            var crawl4aiResponse = await httpClient.GetAsync("http://localhost:8000");
            
            return searxngResponse.IsSuccessStatusCode && crawl4aiResponse.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
