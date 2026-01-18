using System.Net.Http;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using FluentAssertions;

namespace DeepResearchAgent.Tests.Integration;

[Collection("Integration Tests")]
public class LightningStoreIntegrationTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly LightningStore _store;
    private readonly string _testDataDir;

    public LightningStoreIntegrationTests()
    {
        _testDataDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        _httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(30)
        };

        var options = new LightningStoreOptions
        {
            LightningServerUrl = "http://localhost:9090", // Assumes Lightning Server is running
            UseLightningServer = true,
            DataDirectory = _testDataDir,
            ResourceNamespace = "integration-test-facts"
        };

        _store = new LightningStore(options, _httpClient);
    }

    public async Task InitializeAsync()
    {
        // Wait for Lightning Server to be ready
        var maxRetries = 10;
        var delay = TimeSpan.FromSeconds(2);

        for (int i = 0; i < maxRetries; i++)
        {
            try
            {
                var response = await _httpClient.GetAsync("http://localhost:9090/health");
                if (response.IsSuccessStatusCode)
                {
                    return;
                }
            }
            catch
            {
                // Server not ready yet
            }

            if (i < maxRetries - 1)
            {
                await Task.Delay(delay);
            }
        }

        throw new InvalidOperationException("Lightning Server is not available for integration tests. " +
            "Run 'docker-compose up lightning-server' first.");
    }

    public Task DisposeAsync()
    {
        _httpClient.Dispose();
        if (Directory.Exists(_testDataDir))
        {
            Directory.Delete(_testDataDir, true);
        }
        return Task.CompletedTask;
    }

    [Fact]
    public async Task EndToEnd_SaveAndRetrieveFacts()
    {
        // Arrange
        var facts = new List<FactState>
        {
            new FactState
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Integration test fact 1",
                SourceUrl = "https://test1.com",
                Confidence = 0.9,
                ExtractedAt = DateTime.UtcNow
            },
            new FactState
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Integration test fact 2",
                SourceUrl = "https://test2.com",
                Confidence = 0.85,
                ExtractedAt = DateTime.UtcNow
            }
        };

        // Act - Save
        await _store.SaveFactsAsync(facts);

        // Act - Retrieve
        await Task.Delay(1000); // Give server time to process
        var retrievedFacts = await _store.GetAllFactsAsync();

        // Assert
        retrievedFacts.Should().NotBeEmpty();
        retrievedFacts.Should().Contain(f => f.Content == "Integration test fact 1");
        retrievedFacts.Should().Contain(f => f.Content == "Integration test fact 2");
    }

    [Fact]
    public async Task Search_ReturnsMatchingFacts()
    {
        // Arrange
        var facts = new List<FactState>
        {
            new FactState
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Agent Lightning provides orchestration",
                SourceUrl = "https://lightning.com",
                Confidence = 0.95
            },
            new FactState
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Docker containers enable deployment",
                SourceUrl = "https://docker.com",
                Confidence = 0.88
            }
        };

        await _store.SaveFactsAsync(facts);
        await Task.Delay(1000);

        // Act
        var searchResults = await _store.SearchAsync("Lightning");

        // Assert
        searchResults.Should().HaveCountGreaterThan(0);
        searchResults.Should().Contain(f => f.Content.Contains("Agent Lightning"));
    }

    [Fact]
    public async Task GetStatistics_ReturnsServerMetrics()
    {
        // Act
        var stats = await _store.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.ServerInfo.Should().NotBeNull();
    }
}