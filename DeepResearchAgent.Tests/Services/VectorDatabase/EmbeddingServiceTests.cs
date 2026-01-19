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
/// Unit tests for IEmbeddingService implementations.
/// Tests embedding generation and batch operations.
/// </summary>
public class OllamaEmbeddingServiceTests
{
    private readonly Mock<ILogger> _mockLogger;

    public OllamaEmbeddingServiceTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    #region Embed Tests

    [Fact]
    public async Task EmbedAsync_WithValidText_ReturnsEmbedding()
    {
        // Arrange
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f, 0.4f };
        var mockResponse = JsonSerializer.Serialize(new { embedding = expectedEmbedding });
        
        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new OllamaEmbeddingService(
            httpClient,
            baseUrl: "http://localhost:11434",
            model: "nomic-embed-text",
            dimension: 384,
            logger: _mockLogger.Object);

        // Act
        var result = await service.EmbedAsync("Test text for embedding");

        // Assert
        Assert.NotNull(result);
        Assert.Equal(expectedEmbedding.Length, result.Length);
        Assert.Equal(expectedEmbedding, result);
    }

    [Fact]
    public async Task EmbedAsync_WithEmptyText_ReturnsEmbedding()
    {
        // Arrange
        var expectedEmbedding = new float[] { 0.0f, 0.0f };
        var mockResponse = JsonSerializer.Serialize(new { embedding = expectedEmbedding });
        
        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new OllamaEmbeddingService(
            httpClient,
            dimension: 2,
            logger: _mockLogger.Object);

        // Act
        var result = await service.EmbedAsync("");

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result);
    }

    [Fact]
    public async Task EmbedAsync_WithLongText_ReturnsEmbedding()
    {
        // Arrange
        var longText = string.Join(" ", Enumerable.Repeat("word", 500));
        var expectedEmbedding = Enumerable.Range(0, 384).Select(i => (float)i / 384).ToArray();
        var mockResponse = JsonSerializer.Serialize(new { embedding = expectedEmbedding });
        
        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new OllamaEmbeddingService(httpClient, logger: _mockLogger.Object);

        // Act
        var result = await service.EmbedAsync(longText);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(384, result.Length);
    }

    [Fact]
    public async Task EmbedAsync_WithServiceError_ThrowsException()
    {
        // Arrange
        var httpClient = CreateMockHttpClient("", HttpStatusCode.InternalServerError);
        var service = new OllamaEmbeddingService(httpClient, logger: _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.EmbedAsync("test text"));
    }

    [Fact]
    public async Task EmbedAsync_WithInvalidResponse_ThrowsException()
    {
        // Arrange
        var httpClient = CreateMockHttpClient(@"{""invalid"": ""response""}", HttpStatusCode.OK);
        var service = new OllamaEmbeddingService(httpClient, logger: _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EmbedAsync("test text"));
    }

    [Fact]
    public async Task EmbedAsync_WithEmptyEmbedding_ThrowsException()
    {
        // Arrange
        var mockResponse = JsonSerializer.Serialize(new { embedding = new float[] { } });
        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new OllamaEmbeddingService(httpClient, logger: _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<InvalidOperationException>(
            () => service.EmbedAsync("test text"));
    }

    #endregion

    #region Batch Embed Tests

    [Fact]
    public async Task EmbedBatchAsync_WithMultipleTexts_ReturnsEmbeddingsForEach()
    {
        // Arrange
        var texts = new List<string> { "text1", "text2", "text3" };
        var expectedEmbedding = new float[] { 0.1f, 0.2f, 0.3f };
        var mockResponse = JsonSerializer.Serialize(new { embedding = expectedEmbedding });
        
        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new OllamaEmbeddingService(httpClient, logger: _mockLogger.Object);

        // Act
        var results = await service.EmbedBatchAsync(texts);

        // Assert
        Assert.NotNull(results);
        Assert.Equal(texts.Count, results.Count);
        foreach (var result in results)
        {
            Assert.NotNull(result);
            Assert.NotEmpty(result);
        }
    }

    [Fact]
    public async Task EmbedBatchAsync_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var texts = new List<string>();
        var httpClient = CreateMockHttpClient("", HttpStatusCode.OK);
        var service = new OllamaEmbeddingService(httpClient, logger: _mockLogger.Object);

        // Act
        var results = await service.EmbedBatchAsync(texts);

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task EmbedBatchAsync_WithLargeList_ProcessesAllTexts()
    {
        // Arrange
        var texts = Enumerable.Range(1, 100).Select(i => $"text {i}").ToList();
        var expectedEmbedding = new float[] { 0.5f };
        var mockResponse = JsonSerializer.Serialize(new { embedding = expectedEmbedding });
        
        var httpClient = CreateMockHttpClient(mockResponse, HttpStatusCode.OK);
        var service = new OllamaEmbeddingService(
            httpClient,
            dimension: 1,
            logger: _mockLogger.Object);

        // Act
        var results = await service.EmbedBatchAsync(texts);

        // Assert
        Assert.Equal(texts.Count, results.Count);
    }

    [Fact]
    public async Task EmbedBatchAsync_WithOneFailingText_PropagatesException()
    {
        // Arrange
        var texts = new List<string> { "text1", "text2" };
        
        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException("Connection error"));

        var httpClient = new HttpClient(mockHandler.Object);
        var service = new OllamaEmbeddingService(httpClient, logger: _mockLogger.Object);

        // Act & Assert
        await Assert.ThrowsAsync<HttpRequestException>(
            () => service.EmbedBatchAsync(texts));
    }

    #endregion

    #region Dimension Tests

    [Fact]
    public void GetEmbeddingDimension_ReturnsConfiguredDimension()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new OllamaEmbeddingService(
            httpClient,
            dimension: 768,
            logger: _mockLogger.Object);

        // Act
        var dimension = service.GetEmbeddingDimension();

        // Assert
        Assert.Equal(768, dimension);
    }

    [Fact]
    public void GetEmbeddingDimension_WithDefaultDimension_Returns384()
    {
        // Arrange
        var httpClient = new HttpClient();
        var service = new OllamaEmbeddingService(httpClient, logger: _mockLogger.Object);

        // Act
        var dimension = service.GetEmbeddingDimension();

        // Assert
        Assert.Equal(384, dimension);
    }

    [Fact]
    public void GetEmbeddingDimension_WithCustomDimension_ReturnsCustomValue()
    {
        // Arrange
        var customDimension = 512;
        var httpClient = new HttpClient();
        var service = new OllamaEmbeddingService(
            httpClient,
            dimension: customDimension,
            logger: _mockLogger.Object);

        // Act
        var dimension = service.GetEmbeddingDimension();

        // Assert
        Assert.Equal(customDimension, dimension);
    }

    #endregion

    #region Configuration Tests

    [Fact]
    public void OllamaEmbeddingService_WithValidConfiguration_Initializes()
    {
        // Arrange & Act
        var httpClient = new HttpClient();
        var service = new OllamaEmbeddingService(
            httpClient,
            baseUrl: "http://custom:11434",
            model: "custom-model",
            dimension: 256);

        // Assert
        Assert.NotNull(service);
        Assert.Equal(256, service.GetEmbeddingDimension());
    }

    [Fact]
    public void OllamaEmbeddingService_WithNullHttpClient_ThrowsException()
    {
        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => 
            new OllamaEmbeddingService(null!, logger: _mockLogger.Object));
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
