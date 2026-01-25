using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Workflows;

/// <summary>
/// Tests for Phase 5 ResearcherWorkflow extensions.
/// Validates ResearcherAgent integration via extension methods.
/// </summary>
public class ResearcherWorkflowExtensionsTests
{
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<ToolInvocationService> _mockToolService;
    private readonly ResearcherAgent _researcherAgent;
    private readonly ResearcherWorkflow _workflow;

    public ResearcherWorkflowExtensionsTests()
    {
        _mockLlmService = new Mock<OllamaService>(null);
        _mockToolService = new Mock<ToolInvocationService>(null, null);
        
        _researcherAgent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object, null);
        
        var mockStateService = new Mock<ILightningStateService>();
        var mockSearchService = new Mock<SearCrawl4AIService>(new HttpClient(), "http://test", "http://test");
        var mockStore = new Mock<LightningStore>();
        
        _workflow = new ResearcherWorkflow(
            mockStateService.Object,
            mockSearchService.Object,
            _mockLlmService.Object,
            mockStore.Object);
    }

    #region ResearchWithAgentAsync Tests

    [Fact]
    public async Task ResearchWithAgentAsync_WithValidTopic_ReturnsFactStates()
    {
        // Arrange
        var topic = "Quantum Computing";
        SetupMocks();

        // Act
        var result = await _workflow.ResearchWithAgentAsync(_researcherAgent, topic);

        // Assert
        Assert.NotNull(result);
        Assert.IsAssignableFrom<IReadOnlyList<Models.FactState>>(result);
    }

    [Fact]
    public async Task ResearchWithAgentAsync_WithCustomParameters_UsesParameters()
    {
        // Arrange
        var topic = "AI Safety";
        var researchBrief = "Research AI safety concerns";
        var maxIterations = 5;
        var minQuality = 8.5f;
        SetupMocks();

        // Act
        var result = await _workflow.ResearchWithAgentAsync(
            _researcherAgent, 
            topic, 
            researchBrief, 
            maxIterations, 
            minQuality);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ResearchWithAgentAsync_WhenAgentReturnsFindings_MapsToFactStates()
    {
        // Arrange
        var topic = "Machine Learning";
        SetupMocksWithFindings();

        // Act
        var result = await _workflow.ResearchWithAgentAsync(_researcherAgent, topic);

        // Assert
        Assert.NotEmpty(result);
        Assert.All(result, fact =>
        {
            Assert.NotEmpty(fact.Id);
            Assert.NotEmpty(fact.Content);
            Assert.NotEmpty(fact.SourceUrl);
        });
    }

    #endregion

    #region GetResearchMetrics Tests

    [Fact]
    public void GetResearchMetrics_WithValidOutput_ReturnsCorrectMetrics()
    {
        // Arrange
        var output = new ResearchOutput
        {
            Findings = new List<FactExtractionResult>
            {
                new()
                {
                    Facts = new List<ExtractedFact>
                    {
                        new() { Statement = "Fact 1", Confidence = 0.9f },
                        new() { Statement = "Fact 2", Confidence = 0.8f }
                    }
                }
            },
            AverageQuality = 8.5f,
            IterationsUsed = 3
        };

        // Act
        var (quality, facts, iterations) = output.GetResearchMetrics();

        // Assert
        Assert.Equal(8.5f, quality);
        Assert.Equal(2, facts);
        Assert.Equal(3, iterations);
    }

    [Fact]
    public void GetResearchMetrics_WithNullOutput_ReturnsZeroMetrics()
    {
        // Arrange
        ResearchOutput? output = null;

        // Act
        var (quality, facts, iterations) = output.GetResearchMetrics();

        // Assert
        Assert.Equal(0f, quality);
        Assert.Equal(0, facts);
        Assert.Equal(0, iterations);
    }

    #endregion

    #region ToFactState Tests

    [Fact]
    public void ToFactState_WithValidExtractedFact_CreatesFactState()
    {
        // Arrange
        var extractedFact = new ExtractedFact
        {
            Statement = "Test statement",
            Source = "http://test.com",
            Confidence = 0.85f,
            Category = "research"
        };

        // Act
        var factState = extractedFact.ToFactState();

        // Assert
        Assert.NotNull(factState);
        Assert.Equal("Test statement", factState.Content);
        Assert.Equal("http://test.com", factState.SourceUrl);
        Assert.Equal(0.85, factState.Confidence);
        Assert.Contains("research", factState.Tags);
    }

    [Fact]
    public void ToFactState_WithCustomCategory_UsesCategory()
    {
        // Arrange
        var extractedFact = new ExtractedFact
        {
            Statement = "Test",
            Confidence = 0.9f
        };

        // Act
        var factState = extractedFact.ToFactState("custom-category");

        // Assert
        Assert.Contains("custom-category", factState.Tags);
    }

    #endregion

    #region ToFactStates Tests

    [Fact]
    public void ToFactStates_WithMultipleFacts_ConvertsAll()
    {
        // Arrange
        var facts = new List<ExtractedFact>
        {
            new() { Statement = "Fact 1", Confidence = 0.9f },
            new() { Statement = "Fact 2", Confidence = 0.8f },
            new() { Statement = "Fact 3", Confidence = 0.7f }
        };

        // Act
        var factStates = facts.ToFactStates();

        // Assert
        Assert.Equal(3, factStates.Count);
        Assert.All(factStates, fs =>
        {
            Assert.NotEmpty(fs.Id);
            Assert.NotEmpty(fs.Content);
        });
    }

    [Fact]
    public void ToFactStates_WithEmptyList_ReturnsEmptyList()
    {
        // Arrange
        var facts = new List<ExtractedFact>();

        // Act
        var factStates = facts.ToFactStates();

        // Assert
        Assert.Empty(factStates);
    }

    #endregion

    #region Helper Methods

    private void SetupMocks()
    {
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage
            {
                Role = "assistant",
                Content = "Research content"
            });

        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebSearchResult>
            {
                new() { Title = "Test", Content = "Test content" }
            });
    }

    private void SetupMocksWithFindings()
    {
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage
            {
                Role = "assistant",
                Content = "8.5" // Quality score
            });

        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "websearch",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebSearchResult>
            {
                new()
                {
                    Title = "Test Article",
                    Url = "http://test.com",
                    Content = "Test content about machine learning"
                }
            });

        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "extractfacts",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FactExtractionResult
            {
                Facts = new List<ExtractedFact>
                {
                    new()
                    {
                        Statement = "Machine learning is a subset of AI",
                        Source = "http://test.com",
                        Confidence = 0.9f,
                        Category = "research"
                    }
                }
            });
    }

    #endregion
}
