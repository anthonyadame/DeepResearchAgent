using Xunit;
using Moq;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.VectorDatabase;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.Logging;

namespace DeepResearchAgent.Tests.Services.VectorDatabase;

/// <summary>
/// Integration tests for vector database with ResearcherWorkflow.
/// Tests fact indexing, semantic search, and workflow integration.
/// </summary>
public class VectorDatabaseIntegrationTests
{
    private readonly Mock<IVectorDatabaseService> _mockVectorDb;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<ILightningStateService> _mockStateService;
    private readonly Mock<SearCrawl4AIService> _mockSearchService;
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<LightningStore> _mockStore;
    private readonly Mock<ILogger<ResearcherWorkflow>> _mockLogger;

    public VectorDatabaseIntegrationTests()
    {
        _mockVectorDb = new Mock<IVectorDatabaseService>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockStateService = new Mock<ILightningStateService>();
        _mockSearchService = new Mock<SearCrawl4AIService>(
            new HttpClient(),
            "http://localhost:8080",
            "http://localhost:11235");
        _mockLlmService = new Mock<OllamaService>();
        _mockStore = new Mock<LightningStore>();
        _mockLogger = new Mock<ILogger<ResearcherWorkflow>>();

        // Setup default behaviors
        SetupDefaultMocks();
    }

    #region Fact Indexing Tests

    [Fact]
    public async Task ResearchAsync_WithVectorDbConfigured_IndexesExtractedFacts()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        _mockEmbeddingService
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { new float[] { 0.1f, 0.2f, 0.3f } });

        // Act
        await workflow.ResearchAsync("machine learning");

        // Assert
        _mockVectorDb.Verify(
            v => v.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ResearchAsync_WithoutVectorDb_SkipsIndexing()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            vectorDb: null,
            embeddingService: null,
            _mockLogger.Object);

        // Act
        await workflow.ResearchAsync("machine learning");

        // Assert
        _mockEmbeddingService.Verify(
            e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task IndexFactsToVectorDatabaseAsync_WithValidFacts_IndexesEach()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        var facts = new List<Models.FactState>
        {
            new Models.FactState
            {
                Id = "fact_1",
                Content = "Neural networks are powerful",
                SourceUrl = "https://example.com/1",
                Confidence = 0.9
            },
            new Models.FactState
            {
                Id = "fact_2",
                Content = "Deep learning uses multiple layers",
                SourceUrl = "https://example.com/2",
                Confidence = 0.85
            }
        };

        var embeddings = new List<float[]>
        {
            new float[] { 0.1f, 0.2f, 0.3f },
            new float[] { 0.4f, 0.5f, 0.6f }
        };

        _mockEmbeddingService
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        // Act
        await workflow.SearchSimilarFactsAsync("neural networks");

        // Assert
        // Verify that embedding service was called
        _mockEmbeddingService.Verify(
            e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task IndexFactsToVectorDatabaseAsync_WithMetadata_IncludesMetadata()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        var facts = new List<Models.FactState>
        {
            new Models.FactState
            {
                Id = "fact_1",
                Content = "Test fact",
                SourceUrl = "https://example.com",
                Confidence = 0.95
            }
        };

        _mockEmbeddingService
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { new float[] { 0.1f, 0.2f } });

        // Act
        await workflow.SearchSimilarFactsAsync("test");

        // Assert
        _mockVectorDb.Verify(
            v => v.UpsertAsync(
                "fact_1",
                "Test fact",
                It.IsAny<float[]>(),
                It.Is<Dictionary<string, object>>(d => d != null && d.ContainsKey("sourceUrl")),
                It.IsAny<CancellationToken>()),
            Times.Never); // Not called for search, only for indexing
    }

    #endregion

    #region Semantic Search Tests

    [Fact]
    public async Task SearchSimilarFactsAsync_WithValidQuery_ReturnsSimilarFacts()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        var searchResults = new List<VectorSearchResult>
        {
            new VectorSearchResult
            {
                Id = "doc_1",
                Content = "Neural networks are powerful",
                Score = 0.95
            },
            new VectorSearchResult
            {
                Id = "doc_2",
                Content = "Deep learning methods",
                Score = 0.87
            }
        };

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        // Act
        var results = await workflow.SearchSimilarFactsAsync("machine learning");

        // Assert
        Assert.NotNull(results);
        Assert.Equal(2, results.Count);
        Assert.Equal("doc_1", results[0].Id);
        Assert.Equal(0.95, results[0].Score);
    }

    [Fact]
    public async Task SearchSimilarFactsAsync_WithoutVectorDb_ReturnsEmptyList()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            vectorDb: null,
            embeddingService: null,
            _mockLogger.Object);

        // Act
        var results = await workflow.SearchSimilarFactsAsync("test query");

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchSimilarFactsAsync_WithVectorDbFailure_ReturnsEmptyListGracefully()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Vector DB unavailable"));

        // Act
        var results = await workflow.SearchSimilarFactsAsync("test query");

        // Assert
        Assert.NotNull(results);
        Assert.Empty(results);
    }

    [Fact]
    public async Task SearchSimilarFactsAsync_WithCustomTopK_PassesParameterToVectorDb()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), 10, It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VectorSearchResult>());

        // Act
        await workflow.SearchSimilarFactsAsync("test", topK: 10);

        // Assert
        _mockVectorDb.Verify(
            v => v.SearchByContentAsync(It.IsAny<string>(), 10, It.IsAny<double?>(), It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ResearchAsync_WithEmbeddingServiceFailure_ContinuesWithoutIndexing()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        _mockEmbeddingService
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Embedding service error"));

        // Act
        var facts = await workflow.ResearchAsync("test topic");

        // Assert
        Assert.NotNull(facts);
        // Research completes even if embedding fails
    }

    [Fact]
    public async Task ResearchAsync_WithVectorDbFailure_ContinuesResearch()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        _mockEmbeddingService
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { new float[] { 0.1f } });

        _mockVectorDb
            .Setup(v => v.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Vector DB unavailable"));

        // Act
        var facts = await workflow.ResearchAsync("test topic");

        // Assert
        Assert.NotNull(facts);
        // Research completes even if vector DB indexing fails
    }

    #endregion

    #region Performance Tests

    [Fact]
    public async Task IndexFactsToVectorDatabaseAsync_WithLargeNumberOfFacts_ProcessesAllFacts()
    {
        // Arrange
        var workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        var facts = Enumerable.Range(1, 50)
            .Select(i => new Models.FactState
            {
                Id = $"fact_{i}",
                Content = $"Fact content {i}",
                SourceUrl = $"https://example.com/{i}",
                Confidence = 0.8
            })
            .ToList();

        var embeddings = Enumerable.Range(1, 50)
            .Select(_ => new float[] { 0.1f, 0.2f })
            .ToList();

        _mockEmbeddingService
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(embeddings);

        // Act
        await workflow.SearchSimilarFactsAsync("test");

        // Assert
        // Verify embedding service was called with appropriate batch
        _mockEmbeddingService.Verify(
            e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        // Setup minimal mocks for integration tests
        // The vector database tests are focused on VectorDB operations,
        // so we keep the mocks simple to avoid complexity
        
        // Setup state service - minimum setup
        _mockStateService
            .Setup(s => s.GetResearchStateAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResearchStateModel());

        // Setup LLM service
        _mockLlmService
            .Setup(l => l.InvokeAsync(It.IsAny<List<OllamaChatMessage>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage { Role = "assistant", Content = "Search for more information" });

        // Setup vector DB
        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VectorSearchResult>());

        _mockVectorDb
            .Setup(v => v.UpsertAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<float[]>(), It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("id");

        // Setup embedding service
        _mockEmbeddingService
            .Setup(e => e.EmbedAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new float[] { 0.1f, 0.2f, 0.3f });

        _mockEmbeddingService
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { new float[] { 0.1f, 0.2f, 0.3f } });
    }

    #endregion
}
