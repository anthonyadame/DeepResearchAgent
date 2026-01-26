using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.WebSearch;
using DeepResearchAgent.Workflows;
using DeepResearchAgent.Services.StateManagement;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Workflows;

/// <summary>
/// Integration tests for SupervisorWorkflow with ToolInvocationService.
/// Tests the tool execution pipeline: WebSearch → Summarization → FactExtraction
/// </summary>
public class SupervisorWorkflowToolIntegrationTests
{
    private readonly Mock<ILightningStateService> _mockStateService;
    private readonly Mock<ResearcherWorkflow> _mockResearcher;
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<IWebSearchProvider> _mockSearchProvider;
    private readonly Mock<ILogger<SupervisorWorkflow>> _mockLogger;
    private readonly SupervisorWorkflow _workflow;

    public SupervisorWorkflowToolIntegrationTests()
    {
        _mockStateService = new Mock<ILightningStateService>();
        _mockResearcher = new Mock<ResearcherWorkflow>(null, null, null);
        _mockLlmService = new Mock<OllamaService>(null);
        _mockSearchProvider = new Mock<IWebSearchProvider>();
        _mockSearchProvider.Setup(x => x.ProviderName).Returns("test");
        _mockLogger = new Mock<ILogger<SupervisorWorkflow>>();
        
        _workflow = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            _mockSearchProvider.Object,
            null,
            _mockLogger.Object
        );
    }

    #region Tool Integration Tests

    [Fact]
    public async Task SupervisorToolsAsync_WithValidBrainDecision_ExecutesWebSearchTool()
    {
        // Arrange
        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Research on quantum computing breakthroughs";
        
        var brainDecision = new ChatMessage
        {
            Role = "brain",
            Content = "We need to search for latest quantum computing developments and IBM Willow"
        };

        var searchResults = new List<WebSearchResult>
        {
            new WebSearchResult
            {
                Title = "IBM Willow Quantum Breakthrough",
                Url = "https://example.com/willow",
                Content = "IBM announced a significant quantum chip advancement...",
                Engine = "test"
            }
        };

        _mockSearchProvider
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<PageSummaryResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageSummaryResult
            {
                Summary = "IBM's Willow quantum chip shows significant error correction improvements",
                KeyPoints = new List<string> { "Error correction breakthrough", "Improved qubit count" }
            });

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<FactExtractionResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FactExtractionResult
            {
                Facts = new List<ExtractedFact>
                {
                    new ExtractedFact
                    {
                        Statement = "IBM Willow achieved improved error correction",
                        Confidence = 0.95f,
                        Source = "IBM announcement",
                        Category = "quantum breakthrough"
                    }
                }
            });

        // Act
        await _workflow.SupervisorToolsAsync(state, brainDecision);

        // Assert
        Assert.NotEmpty(state.KnowledgeBase);
        Assert.Single(state.KnowledgeBase);
        var fact = state.KnowledgeBase.First();
        Assert.Contains("error correction", fact.Content.ToLower());
    }

    [Fact]
    public async Task SupervisorToolsAsync_ExecutesFullPipeline_WebSearchToFactExtraction()
    {
        // Arrange
        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Climate change impacts on ecosystems";
        
        var brainDecision = new ChatMessage
        {
            Role = "brain",
            Content = "Research recent climate data and ecosystem impacts"
        };

        var searchResults = new List<WebSearchResult>
        {
            new WebSearchResult
            {
                Title = "Climate Change Report 2024",
                Url = "https://example.com/climate",
                Content = "Global temperatures rising at alarming rate. Ecosystems facing stress...",
                Engine = "test"
            },
            new WebSearchResult
            {
                Title = "Biodiversity Loss Study",
                Url = "https://example.com/biodiversity",
                Content = "Rapid biodiversity loss observed in tropical regions...",
                Engine = "test"
            }
        };

        _mockSearchProvider
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        var summaryCounter = 0;
        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<PageSummaryResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns((List<OllamaChatMessage> _, string? _, CancellationToken _) =>
            {
                summaryCounter++;
                return Task.FromResult(new PageSummaryResult
                {
                    Summary = $"Summary {summaryCounter}: Key findings about climate impact",
                    KeyPoints = new List<string> { "Point 1", "Point 2" }
                });
            });

        var factCounter = 0;
        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<FactExtractionResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns((List<OllamaChatMessage> _, string? _,
                CancellationToken _) =>
            {
                factCounter++;
                return Task.FromResult(new FactExtractionResult
                {
                    Facts = new List<ExtractedFact>
                    {
                        new ExtractedFact
                        {
                            Statement = $"Fact {factCounter}: Climate change impact",
                            Confidence = 0.9f,
                            Source = "research",
                            Category = "climate"
                        }
                    }
                });
            });

        // Act
        await _workflow.SupervisorToolsAsync(state, brainDecision);

        // Assert
        Assert.NotEmpty(state.KnowledgeBase);
        Assert.Equal(2, state.KnowledgeBase.Count); // 2 search results processed
        _mockSearchProvider.Verify(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SupervisorToolsAsync_HandlesSearchWithNoResults()
    {
        // Arrange
        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Obscure research topic";
        
        var brainDecision = new ChatMessage
        {
            Role = "brain",
            Content = "Research very specific topic"
        };

        var emptyResults = new List<WebSearchResult>();

        _mockSearchProvider
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(emptyResults);

        // Act
        await _workflow.SupervisorToolsAsync(state, brainDecision);

        // Assert
        Assert.Empty(state.KnowledgeBase); // No facts extracted
    }

    [Fact]
    public async Task SupervisorToolsAsync_HandlesToolFailureGracefully()
    {
        // Arrange
        var state = StateFactory.CreateSupervisorState();
        var brainDecision = new ChatMessage
        {
            Role = "brain",
            Content = "Research topic"
        };

        _mockSearchProvider
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Search service unavailable"));

        // Act
        await _workflow.SupervisorToolsAsync(state, brainDecision);

        // Assert - Should handle error gracefully
        Assert.NotEmpty(state.SupervisorMessages); // Error message recorded
        var lastMessage = state.SupervisorMessages.Last();
        Assert.Contains("failed", lastMessage.Content.ToLower());
    }

    [Fact]
    public async Task SupervisorToolsAsync_AddsFactsToKnowledgeBase_WithCorrectMetadata()
    {
        // Arrange
        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Test research";
        
        var brainDecision = new ChatMessage
        {
            Role = "brain",
            Content = "Search for information"
        };

        var searchResults = new List<WebSearchResult>
        {
            new WebSearchResult
            {
                Title = "Test Article",
                Url = "https://example.com/test",
                Content = "Test content",
                Engine = "test"
            }
        };

        _mockSearchProvider
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<PageSummaryResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageSummaryResult
            {
                Summary = "Test summary",
                KeyPoints = new List<string>()
            });

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<FactExtractionResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FactExtractionResult
            {
                Facts = new List<ExtractedFact>
                {
                    new ExtractedFact
                    {
                        Statement = "Test fact",
                        Confidence = 0.85f,
                        Source = "test source",
                        Category = "test category"
                    }
                }
            });

        // Act
        await _workflow.SupervisorToolsAsync(state, brainDecision);

        // Assert
        Assert.Single(state.KnowledgeBase);
        var fact = state.KnowledgeBase.First();
        Assert.Equal("Test fact", fact.Content);
        Assert.Equal(0.85, fact.Confidence);
        Assert.Contains("example.com", fact.SourceUrl);
        Assert.NotEqual(default, fact.ExtractedAt);
    }

    #endregion

    #region Tool Routing Tests

    [Fact]
    public async Task SupervisorToolsAsync_WithMultipleTopics_ProcessesEachTopic()
    {
        // Arrange
        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Multi-topic research";
        
        var brainDecision = new ChatMessage
        {
            Role = "brain",
            Content = "Investigate quantum computing, AI safety, and climate change impacts"
        };

        var searchResults = new List<WebSearchResult>
        {
            new WebSearchResult
            {
                Title = "Topic 1",
                Url = "https://example.com/topic1",
                Content = "Content 1",
                Engine = "test"
            }
        };

        _mockSearchProvider
            .Setup(s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(searchResults);

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<PageSummaryResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageSummaryResult { Summary = "Summary", KeyPoints = new List<string>() });

        _mockLlmService
            .Setup(s => s.InvokeWithStructuredOutputAsync<FactExtractionResult>(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FactExtractionResult
            {
                Facts = new List<ExtractedFact>
                {
                    new ExtractedFact 
                    { 
                        Statement = "Fact", 
                        Confidence = 0.9f, 
                        Source = "source", 
                        Category = "category" 
                    }
                }
            });

        // Act
        await _workflow.SupervisorToolsAsync(state, brainDecision);

        // Assert
        // Should limit to max 3 topics per iteration
        _mockSearchProvider.Verify(
            s => s.SearchAsync(It.IsAny<string>(), It.IsAny<int>(), It.IsAny<List<string>>(), It.IsAny<CancellationToken>()),
            Times.Between(1, 3, Moq.Range.Inclusive)); // Could be 1-3 searches depending on topic extraction
    }

    #endregion
}
