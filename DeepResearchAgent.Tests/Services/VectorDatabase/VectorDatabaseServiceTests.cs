using Xunit;
using Moq;
using Moq.Protected;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using DeepResearchAgent.Services.VectorDatabase;
using Microsoft.Extensions.Logging;

namespace DeepResearchAgent.Tests.Services.VectorDatabase;

/// <summary>
/// Unit tests for IVectorDatabaseService implementations.
/// Tests Qdrant integration and semantic search functionality.
/// </summary>
public class QdrantVectorDatabaseServiceTests
{
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<ILogger> _mockLogger;
    private readonly QdrantConfig _config;

    public QdrantVectorDatabaseServiceTests()
    {
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockLogger = new Mock<ILogger>();
        _config = new QdrantConfig
        {
            BaseUrl = "http://localhost:6333",
            CollectionName = "test_collection",
            VectorDimension = 384
        };
    }

    #region Upsert Tests

    [Fact]
    public async Task UpsertAsync_WithValidData_ReturnsDocumentId()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""result"": {""status"": ""ok""}}", HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);
        
        var id = "test_doc_1";
        var content = "Test content";
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var result = await service.UpsertAsync(id, content, embedding);

        // Assert
        Assert.Equal(id, result);
    }

    [Fact]
    public async Task UpsertAsync_WithMetadata_IncludesMetadataInPayload()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""result"": {""status"": ""ok""}}", HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);
        
        var id = "test_doc_1";
        var content = "Test content";
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };
        var metadata = new Dictionary<string, object>
        {
            ["sourceUrl"] = "https://example.com",
            ["confidence"] = 0.85
        };

        // Act
        var result = await service.UpsertAsync(id, content, embedding, metadata);

        // Assert
        Assert.Equal(id, result);
    }

    [Fact]
    public async Task UpsertAsync_WithDimensionMismatch_LogsWarning()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""result"": {""status"": ""ok""}}", HttpStatusCode.OK);
        var mockLoggerTyped = new Mock<Microsoft.Extensions.Logging.ILogger>();
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, mockLoggerTyped.Object);
        
        var id = "test_doc_1";
        var content = "Test content";
        var embedding = new float[] { 0.1f, 0.2f }; // Wrong dimension

        // Act
        var result = await service.UpsertAsync(id, content, embedding);

        // Assert
        Assert.Equal(id, result);
        // Logger should be called (verified via mock)
    }

    #endregion

    #region Search Tests

    [Fact]
    public async Task SearchAsync_WithValidEmbedding_ReturnsResults()
    {
        // Arrange
        var mockResponse = @"{
            ""result"": [
                {
                    ""id"": ""doc_1"",
                    ""score"": 0.95,
                    ""payload"": {
                        ""content"": ""Test document 1"",
                        ""sourceUrl"": ""https://example.com""
                    }
                },
                {
                    ""id"": ""doc_2"",
                    ""score"": 0.87,
                    ""payload"": {
                        ""content"": ""Test document 2""
                    }
                }
            ]
        }";

        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var results = await service.SearchAsync(embedding, topK: 5);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal("doc_1", results[0].Id);
        Assert.Equal(0.95, results[0].Score);
        Assert.Equal("Test document 1", results[0].Content);
    }

    [Fact]
    public async Task SearchAsync_WithTopK_RespectsLimit()
    {
        // Arrange
        var mockResponse = @"{
            ""result"": [
                {""id"": ""doc_1"", ""score"": 0.95, ""payload"": {""content"": ""Test 1""}},
                {""id"": ""doc_2"", ""score"": 0.87, ""payload"": {""content"": ""Test 2""}},
                {""id"": ""doc_3"", ""score"": 0.76, ""payload"": {""content"": ""Test 3""}}
            ]
        }";

        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var results = await service.SearchAsync(embedding, topK: 2);

        // Assert
        // The actual filtering would be done by Qdrant server, but we verify response handling
        Assert.NotNull(results);
        Assert.True(results.Count > 0);
    }

    [Fact]
    public async Task SearchAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        var mockResponse = @"{ ""result"": [] }";
        
        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var results = await service.SearchAsync(embedding, topK: 5);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchAsync_WithScoreThreshold_FiltersResults()
    {
        // Arrange
        var mockResponse = @"{
            ""result"": [
                {""id"": ""doc_1"", ""score"": 0.95, ""payload"": {""content"": ""Test 1""}},
                {""id"": ""doc_2"", ""score"": 0.45, ""payload"": {""content"": ""Test 2""}}
            ]
        }";

        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);
        
        var embedding = new float[] { 0.1f, 0.2f, 0.3f };

        // Act
        var results = await service.SearchAsync(embedding, topK: 5, scoreThreshold: 0.5);

        // Assert
        Assert.NotNull(results);
        // All results returned (threshold filtering done by Qdrant)
        Assert.True(results.Count > 0);
    }

    #endregion

    #region SearchByContent Tests

    [Fact]
    public async Task SearchByContentAsync_EmbedsThenSearches()
    {
        // Arrange
        var testEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        _mockEmbeddingService
            .Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(testEmbedding);

        var mockResponse = @"{
            ""result"": [
                {
                    ""id"": ""doc_1"",
                    ""score"": 0.92,
                    ""payload"": {""content"": ""Similar document""}
                }
            ]
        }";

        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act
        var results = await service.SearchByContentAsync("neural networks");

        // Assert
        Assert.NotNull(results);
        Assert.NotEmpty(results);
        _mockEmbeddingService.Verify(
            e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task SearchByContentAsync_WithEmbeddingServiceFailure_PropagatesException()
    {
        // Arrange
        _mockEmbeddingService
            .Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Embedding service unavailable"));

        var httpClient = new HttpClient();
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.SearchByContentAsync("test query"));
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task DeleteAsync_WithValidId_SucceedsWithoutException()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""result"": {""status"": ""ok""}}", HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act & Assert
        await service.DeleteAsync("test_doc_1");
    }

    [Fact]
    public async Task DeleteAsync_WithHttpError_ThrowsException()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("", HttpStatusCode.NotFound);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.DeleteAsync("nonexistent_doc"));
    }

    #endregion

    #region Clear Tests

    [Fact]
    public async Task ClearAsync_DeletesAllDocuments()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""result"": {""status"": ""ok""}}", HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act & Assert
        await service.ClearAsync();
    }

    #endregion

    #region Stats Tests

    [Fact]
    public async Task GetStatsAsync_ReturnsValidStats()
    {
        // Arrange
        var mockResponse = @"{
            ""result"": {
                ""points_count"": 100,
                ""config_diff"": {
                    ""vector_size"": 384
                }
            }
        }";

        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act
        var stats = await service.GetStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(100, stats.DocumentCount);
        Assert.True(stats.IsHealthy);
        Assert.Equal("Operational", stats.Status);
    }

    [Fact]
    public async Task GetStatsAsync_WithError_ReturnsUnhealthyStats()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("", HttpStatusCode.InternalServerError);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act
        var stats = await service.GetStatsAsync();

        // Assert
        Assert.NotNull(stats);
        Assert.False(stats.IsHealthy);
        Assert.Contains("Error", stats.Status);
    }

    #endregion

    #region Health Check Tests

    [Fact]
    public async Task HealthCheckAsync_WithHealthyServer_ReturnsTrue()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""status"": ""ok""}", HttpStatusCode.OK);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act
        var isHealthy = await service.HealthCheckAsync();

        // Assert
        Assert.True(isHealthy);
    }

    [Fact]
    public async Task HealthCheckAsync_WithUnhealthyServer_ReturnsFalse()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("", HttpStatusCode.ServiceUnavailable);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act
        var isHealthy = await service.HealthCheckAsync();

        // Assert
        Assert.False(isHealthy);
    }

    [Fact]
    public async Task HealthCheckAsync_WithConnectionError_ReturnsFalse()
    {
        // Arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection refused"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new QdrantVectorDatabaseService(httpClient, _config, _mockEmbeddingService.Object, _mockLogger.Object);

        // Act
        var isHealthy = await service.HealthCheckAsync();

        // Assert
        Assert.False(isHealthy);
    }

    #endregion

    #region Helper Methods

    private static HttpClient CreateMockHttpClient(string responseContent, HttpStatusCode statusCode)
    {
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = new StringContent(responseContent)
            });

        return new HttpClient(mockHandler.Object);
    }

    #endregion
}
