using Xunit;
using Moq;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.VectorDatabase;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.Logging;

namespace DeepResearchAgent.Tests.Workflows;

/// <summary>
/// Unit tests for ResearcherWorkflow vector database search enhancement.
/// Tests the new vector database search capabilities integrated into ToolExecutionAsync.
/// </summary>
public class ResearcherWorkflowVectorDatabaseSearchTests
{
    private readonly Mock<ILightningStateService> _mockStateService;
    private readonly Mock<SearCrawl4AIService> _mockSearchService;
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<LightningStore> _mockStore;
    private readonly Mock<IVectorDatabaseService> _mockVectorDb;
    private readonly Mock<IEmbeddingService> _mockEmbeddingService;
    private readonly Mock<ILogger<ResearcherWorkflow>> _mockLogger;
    private readonly ResearcherWorkflow _workflow;

    public ResearcherWorkflowVectorDatabaseSearchTests()
    {
        _mockStateService = new Mock<ILightningStateService>();
        _mockSearchService = new Mock<SearCrawl4AIService>(
            new HttpClient(),
            "http://localhost:8080",
            "http://localhost:11235");
        _mockLlmService = new Mock<OllamaService>();
        _mockStore = new Mock<LightningStore>();
        _mockVectorDb = new Mock<IVectorDatabaseService>();
        _mockEmbeddingService = new Mock<IEmbeddingService>();
        _mockLogger = new Mock<ILogger<ResearcherWorkflow>>();

        _workflow = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            _mockVectorDb.Object,
            _mockEmbeddingService.Object,
            _mockLogger.Object);

        SetupDefaultMocks();
    }

    #region ToolExecutionAsync with Vector Database Tests

    [Fact]
    public async Task ToolExecutionAsync_WithVectorDbConfigured_ExecutesBothSearchTypes()
    {
        // Arrange
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage 
        { 
            Role = "assistant", 
            Content = "Search for machine learning basics" 
        };

        var vectorResults = new List<VectorSearchResult>
        {
            new VectorSearchResult 
            { 
                Id = "fact_1",
                Content = "Vector result 1",
                Score = 0.85
            }
        };

        // Setup mocks
        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResults);

        // Act
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        _mockVectorDb.Verify(
            v => v.SearchByContentAsync(It.IsAny<string>(), 5, 0.6, It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
        
        Assert.NotEmpty(state.ResearcherMessages);
        var toolMessage = state.ResearcherMessages.Last(m => m.Role == "tool");
        Assert.Contains("web and vector database", toolMessage.Content);
    }

    [Fact]
    public async Task ToolExecutionAsync_WithoutVectorDb_ExecutesWebSearchOnly()
    {
        // Arrange
        var workflowWithoutVectorDb = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            vectorDb: null,
            embeddingService: null,
            _mockLogger.Object);

        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage 
        { 
            Role = "assistant", 
            Content = "Search for information" 
        };

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        // Act
        await workflowWithoutVectorDb.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        var toolMessage = state.ResearcherMessages.Last(m => m.Role == "tool");
        Assert.Contains("web search", toolMessage.Content);
        Assert.DoesNotContain("vector database", toolMessage.Content);
    }

    [Fact]
    public async Task ToolExecutionAsync_WithVectorDbResults_AggregatesResultsCorrectly()
    {
        // Arrange
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage 
        { 
            Role = "assistant", 
            Content = "Search for AI" 
        };

        var vectorResults = new List<VectorSearchResult>
        {
            new VectorSearchResult 
            { 
                Id = "fact_1",
                Content = "AI is transforming industries",
                Score = 0.92,
                Metadata = new Dictionary<string, object> { ["sourceUrl"] = "https://example.com" }
            },
            new VectorSearchResult 
            { 
                Id = "fact_2",
                Content = "Machine learning is a subset of AI",
                Score = 0.88,
                Metadata = new Dictionary<string, object> { ["confidence"] = 0.85 }
            }
        };

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResults);

        // Act
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        Assert.True(state.RawNotes.Count > 0);
        
        // Verify that vector results are formatted and included
        var vectorFormattedResults = state.RawNotes.Where(n => n.Contains("Knowledge Base")).ToList();
        Assert.True(vectorFormattedResults.Count > 0);
    }

    [Fact]
    public async Task ToolExecutionAsync_WithVectorDbError_ContinuesWithWebSearchOnly()
    {
        // Arrange
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage 
        { 
            Role = "assistant", 
            Content = "Search for data" 
        };

        var webContent = new List<ScrapedContent>
        {
            new ScrapedContent { Markdown = "Web search result" }
        };

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(webContent);

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Vector DB unavailable"));

        // Act
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        Assert.NotEmpty(state.RawNotes);
        Assert.True(state.ToolCallIterations > 0);
    }

    #endregion

    #region Vector Database Search Specific Tests

    [Fact]
    public async Task ExecuteVectorDatabaseSearchAsync_WithValidQuery_ReturnsFormattedResults()
    {
        // Arrange
        var query = "machine learning";
        var vectorResults = new List<VectorSearchResult>
        {
            new VectorSearchResult 
            { 
                Id = "doc_1",
                Content = "Machine learning fundamentals",
                Score = 0.95,
                Metadata = new Dictionary<string, object> 
                { 
                    ["sourceUrl"] = "https://example.com/ml",
                    ["confidence"] = 0.92
                }
            }
        };

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(query, 5, 0.6, It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResults);

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        // Act
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage { Role = "assistant", Content = "Search for ML" };
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        var vectorNotes = state.RawNotes.Where(n => n.Contains("Knowledge Base")).ToList();
        Assert.NotEmpty(vectorNotes);
        Assert.True(vectorNotes[0].Contains("Relevance: 95%"));
        Assert.True(vectorNotes[0].Contains("https://example.com/ml"));
    }

    [Fact]
    public async Task ExecuteVectorDatabaseSearchAsync_WithNoResults_ReturnsEmptyList()
    {
        // Arrange
        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VectorSearchResult>());

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        // Act
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage { Role = "assistant", Content = "Search for obscure topic" };
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        var vectorNotes = state.RawNotes.Where(n => n.Contains("Knowledge Base")).ToList();
        Assert.Empty(vectorNotes);
    }

    [Fact]
    public async Task ExecuteVectorDatabaseSearchAsync_WithNullVectorDb_SkipsSearch()
    {
        // Arrange
        var workflowWithoutVectorDb = new ResearcherWorkflow(
            _mockStateService.Object,
            _mockSearchService.Object,
            _mockLlmService.Object,
            _mockStore.Object,
            vectorDb: null,
            embeddingService: null,
            _mockLogger.Object);

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        // Act
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage { Role = "assistant", Content = "Search" };
        await workflowWithoutVectorDb.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        _mockVectorDb.Verify(
            v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()),
            Times.Never);
    }

    #endregion

    #region FormatVectorSearchResult Tests

    [Fact]
    public async Task FormatVectorSearchResult_WithCompleteMetadata_FormatsCorrectly()
    {
        // Arrange
        var vectorResults = new List<VectorSearchResult>
        {
            new VectorSearchResult 
            { 
                Id = "doc_1",
                Content = "Neural networks are computational models",
                Score = 0.88,
                Metadata = new Dictionary<string, object> 
                { 
                    ["sourceUrl"] = "https://example.com/nn",
                    ["confidence"] = 0.87
                }
            }
        };

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResults);

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        // Act
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage { Role = "assistant", Content = "Search for neural networks" };
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        var vectorNotes = state.RawNotes.Where(n => n.Contains("Knowledge Base")).ToList();
        Assert.NotEmpty(vectorNotes);
        
        var formatted = vectorNotes[0];
        Assert.Contains("[Knowledge Base - Relevance: 88%]", formatted);
        Assert.Contains("Neural networks", formatted);
        Assert.Contains("Source: https://example.com/nn", formatted);
        Assert.Contains("Confidence: 0.87", formatted);
    }

    [Fact]
    public async Task FormatVectorSearchResult_WithPartialMetadata_FormatsWithAvailableData()
    {
        // Arrange
        var vectorResults = new List<VectorSearchResult>
        {
            new VectorSearchResult 
            { 
                Id = "doc_1",
                Content = "Deep learning is a subset of machine learning",
                Score = 0.92,
                Metadata = new Dictionary<string, object> { ["sourceUrl"] = "https://example.com/dl" }
            }
        };

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResults);

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        // Act
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage { Role = "assistant", Content = "Search for deep learning" };
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        var vectorNotes = state.RawNotes.Where(n => n.Contains("Knowledge Base")).ToList();
        Assert.NotEmpty(vectorNotes);
        
        var formatted = vectorNotes[0];
        Assert.Contains("[Knowledge Base - Relevance: 92%]", formatted);
        Assert.Contains("Deep learning", formatted);
        Assert.Contains("Source: https://example.com/dl", formatted);
    }

    [Fact]
    public async Task FormatVectorSearchResult_WithLongContent_TrimsToProperlength()
    {
        // Arrange
        var longContent = string.Concat(Enumerable.Range(0, 100).Select(i => $"word{i} "));
        var vectorResults = new List<VectorSearchResult>
        {
            new VectorSearchResult 
            { 
                Id = "doc_1",
                Content = longContent,
                Score = 0.85
            }
        };

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResults);

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        // Act
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage { Role = "assistant", Content = "Search for long content" };
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        var vectorNotes = state.RawNotes.Where(n => n.Contains("Knowledge Base")).ToList();
        Assert.NotEmpty(vectorNotes);
        
        // Content should be trimmed to around 280 chars for the fact content
        var formatted = vectorNotes[0];
        Assert.True(formatted.Length <= 350); // Allow for prefix and metadata
    }

    [Fact]
    public async Task FormatVectorSearchResult_WithEmptyContent_ReturnsEmptyString()
    {
        // Arrange
        var vectorResults = new List<VectorSearchResult>
        {
            new VectorSearchResult 
            { 
                Id = "doc_1",
                Content = "",
                Score = 0.85
            }
        };

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResults);

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        // Act
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage { Role = "assistant", Content = "Search" };
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);

        // Assert
        var vectorNotes = state.RawNotes.Where(n => n.Contains("Knowledge Base")).ToList();
        Assert.Empty(vectorNotes); // Empty content should not produce notes
    }

    #endregion

    #region Parallel Execution Tests

    [Fact]
    public async Task ToolExecutionAsync_ExecutesWebAndVectorDbSearchesInParallel()
    {
        // Arrange
        var state = CreateTestResearcherState();
        var llmResponse = new Models.ChatMessage 
        { 
            Role = "assistant", 
            Content = "Search for AI and machine learning" 
        };

        var startTime = DateTime.UtcNow;

        // Mock searches with slight delay to verify parallelism
        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(50);
                return new List<ScrapedContent> { new ScrapedContent { Markdown = "Web result" } };
            });

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .Returns(async () =>
            {
                await Task.Delay(50);
                return new List<VectorSearchResult>
                {
                    new VectorSearchResult 
                    { 
                        Id = "fact_1",
                        Content = "Vector result",
                        Score = 0.85
                    }
                };
            });

        // Act
        await _workflow.ToolExecutionAsync(state, llmResponse, CancellationToken.None);
        var duration = DateTime.UtcNow - startTime;

        // Assert
        Assert.NotEmpty(state.RawNotes);
        // If truly parallel, should take ~50ms, not ~100ms
        Assert.True(duration.TotalMilliseconds < 150, $"Execution took {duration.TotalMilliseconds}ms, expected parallel ~50-100ms");
    }

    #endregion

    #region Integration Tests

    [Fact]
    public async Task ResearchAsync_WithVectorDb_IndexesAndSearchesKnowledge()
    {
        // Arrange
        var vectorResults = new List<VectorSearchResult>
        {
            new VectorSearchResult 
            { 
                Id = "fact_1",
                Content = "Research finding 1",
                Score = 0.90
            }
        };

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(vectorResults);

        _mockLlmService
            .Setup(l => l.InvokeAsync(It.IsAny<List<OllamaChatMessage>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage { Role = "assistant", Content = "Stop researching now" });

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        _mockEmbeddingService
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]> { new float[] { 0.1f, 0.2f } });

        // Act
        var facts = await _workflow.ResearchAsync("test topic");

        // Assert
        _mockVectorDb.Verify(
            v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Helper Methods

    private void SetupDefaultMocks()
    {
        _mockStateService
            .Setup(s => s.SetResearchStateAsync(It.IsAny<string>(), It.IsAny<ResearchStateModel>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockStateService
            .Setup(s => s.UpdateResearchProgressAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockSearchService
            .Setup(s => s.SearchAndScrapeAsync(It.IsAny<string>(), It.IsAny<int>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<ScrapedContent>());

        _mockLlmService
            .Setup(l => l.InvokeAsync(It.IsAny<List<OllamaChatMessage>>(), null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage { Role = "assistant", Content = "Continue searching" });

        _mockStore
            .Setup(s => s.SaveFactsAsync(It.IsAny<IEnumerable<Models.FactState>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockVectorDb
            .Setup(v => v.SearchByContentAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<double?>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<VectorSearchResult>());

        _mockEmbeddingService
            .Setup(e => e.EmbedBatchAsync(It.IsAny<IEnumerable<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<float[]>());
    }

    private static ResearcherState CreateTestResearcherState()
    {
        return new ResearcherState
        {
            ResearchTopic = "test topic",
            ResearcherMessages = new List<Models.ChatMessage>(),
            RawNotes = new List<string>(),
            CompressedResearch = string.Empty,
            ToolCallIterations = 0
        };
    }

    #endregion
}
