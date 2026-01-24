using System.Net;
using System.Text.Json;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Moq;
using Moq.Protected;
using Xunit;
using FluentAssertions;

namespace DeepResearchAgent.Tests.Services;

public class LightningStoreTests
{
    private readonly Mock<HttpMessageHandler> _httpMessageHandlerMock;
    private readonly HttpClient _httpClient;
    private readonly LightningStoreOptions _options;

    public LightningStoreTests()
    {
        _httpMessageHandlerMock = new Mock<HttpMessageHandler>();
        _httpClient = new HttpClient(_httpMessageHandlerMock.Object);
        _options = new LightningStoreOptions
        {
            LightningServerUrl = "http://localhost:8090",
            UseLightningServer = true,
            DataDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()),
            ResourceNamespace = "test-facts"
        };
    }

    [Fact]
    public async Task SaveFactsAsync_WhenLightningServerAvailable_CallsAddResourcesEndpoint()
    {
        // Arrange
        SetupHealthCheckResponse(HttpStatusCode.OK);
        SetupAddResourcesResponse(HttpStatusCode.OK);

        var store = new LightningStore(_options, _httpClient);
        var facts = new List<FactState>
        {
            new FactState
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Test fact",
                SourceUrl = "https://example.com",
                Confidence = 0.95,
                ExtractedAt = DateTime.UtcNow
            }
        };

        // Act
        await store.SaveFactsAsync(facts);

        // Assert
        _httpMessageHandlerMock.Protected()
            .Verify("SendAsync",
                Times.Once(),
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/resources/add")),
                ItExpr.IsAny<CancellationToken>());
    }

    [Fact]
    public async Task SaveFactsAsync_WhenLightningServerUnavailable_FallsBackToFileStorage()
    {
        // Arrange
        SetupHealthCheckResponse(HttpStatusCode.ServiceUnavailable);

        var store = new LightningStore(_options, _httpClient);
        var facts = new List<FactState>
        {
            new FactState
            {
                Id = Guid.NewGuid().ToString(),
                Content = "Test fact",
                SourceUrl = "https://example.com"
            }
        };

        // Act
        await store.SaveFactsAsync(facts);

        // Assert
        var filePath = _options.FilePath;
        File.Exists(filePath).Should().BeTrue();

        var savedFacts = await JsonSerializer.DeserializeAsync<List<FactState>>(
            File.OpenRead(filePath));
        savedFacts.Should().HaveCount(1);
        savedFacts![0].Content.Should().Be("Test fact");

        // Cleanup
        Directory.Delete(_options.DataDirectory, true);
    }

    [Fact]
    public async Task GetAllFactsAsync_WhenLightningServerAvailable_RetrievesFromResources()
    {
        // Arrange
        SetupHealthCheckResponse(HttpStatusCode.OK);
        
        var resourcesUpdate = new ResourcesUpdate
        {
            ResourcesId = Guid.NewGuid().ToString(),
            Resources = new Dictionary<string, string>
            {
                ["test-facts:fact-1"] = JsonSerializer.Serialize(new FactState
                {
                    Id = "fact-1",
                    Content = "Retrieved fact",
                    SourceUrl = "https://example.com"
                })
            },
            UpdatedAt = DateTime.UtcNow
        };

        SetupGetLatestResourcesResponse(HttpStatusCode.OK, resourcesUpdate);

        var store = new LightningStore(_options, _httpClient);

        // Act
        var facts = await store.GetAllFactsAsync();

        // Assert
        facts.Should().HaveCount(1);
        facts[0].Content.Should().Be("Retrieved fact");
    }

    [Fact]
    public async Task SearchAsync_FiltersFactsByQueryString()
    {
        // Arrange
        SetupHealthCheckResponse(HttpStatusCode.OK);
        
        var resourcesUpdate = new ResourcesUpdate
        {
            ResourcesId = Guid.NewGuid().ToString(),
            Resources = new Dictionary<string, string>
            {
                ["test-facts:fact-1"] = JsonSerializer.Serialize(new FactState
                {
                    Id = "fact-1",
                    Content = "Machine learning is powerful",
                    SourceUrl = "https://ml.example.com"
                }),
                ["test-facts:fact-2"] = JsonSerializer.Serialize(new FactState
                {
                    Id = "fact-2",
                    Content = "Deep learning uses neural networks",
                    SourceUrl = "https://dl.example.com"
                })
            },
            UpdatedAt = DateTime.UtcNow
        };

        SetupGetLatestResourcesResponse(HttpStatusCode.OK, resourcesUpdate);

        var store = new LightningStore(_options, _httpClient);

        // Act
        var results = await store.SearchAsync("machine");

        // Assert
        results.Should().HaveCount(1);
        results[0].Content.Should().Contain("Machine learning");
    }

    [Fact]
    public async Task GetStatisticsAsync_ReturnsServerInfo()
    {
        // Arrange
        SetupHealthCheckResponse(HttpStatusCode.OK);
        
        var serverInfo = new
        {
            version = "1.0.0",
            apoEnabled = true,
            verlEnabled = true,
            registeredAgents = 5
        };

        SetupServerInfoResponse(HttpStatusCode.OK, serverInfo);

        var store = new LightningStore(_options, _httpClient);

        // Act
        var stats = await store.GetStatisticsAsync();

        // Assert
        stats.Should().NotBeNull();
        stats.ServerInfo.Should().NotBeNull();
    }

    // Helper methods
    private void SetupHealthCheckResponse(HttpStatusCode statusCode)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("/health")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent("{\"status\":\"healthy\"}")
            });
    }

    private void SetupAddResourcesResponse(HttpStatusCode statusCode)
    {
        var response = new ResourcesUpdate
        {
            ResourcesId = Guid.NewGuid().ToString(),
            Resources = new Dictionary<string, string>(),
            UpdatedAt = DateTime.UtcNow
        };

        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Post &&
                    req.RequestUri!.ToString().Contains("/api/resources/add")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(response))
            });
    }

    private void SetupGetLatestResourcesResponse(HttpStatusCode statusCode, ResourcesUpdate resourcesUpdate)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("/api/resources/latest")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(resourcesUpdate))
            });
    }

    private void SetupServerInfoResponse(HttpStatusCode statusCode, object serverInfo)
    {
        _httpMessageHandlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri!.ToString().Contains("/api/server/info")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(JsonSerializer.Serialize(serverInfo))
            });
    }
}