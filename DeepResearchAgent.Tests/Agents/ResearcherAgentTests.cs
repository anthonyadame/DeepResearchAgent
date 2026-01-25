using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Agents;

/// <summary>
/// Unit tests for ResearcherAgent.
/// Tests research orchestration, tool invocation, and iterative refinement.
/// </summary>
public class ResearcherAgentTests
{
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<ToolInvocationService> _mockToolService;
    private readonly Mock<ILogger<ResearcherAgent>> _mockLogger;
    private readonly ResearcherAgent _agent;

    public ResearcherAgentTests()
    {
        _mockLlmService = new Mock<OllamaService>(null);
        _mockToolService = new Mock<ToolInvocationService>(null, null);
        _mockLogger = new Mock<ILogger<ResearcherAgent>>();
        _agent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object, _mockLogger.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ReturnsResearchOutput()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Quantum Computing",
            ResearchBrief = "Research recent quantum computing breakthroughs",
            MaxIterations = 1,
            MinQualityThreshold = 5.0f
        };

        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.Equal("Quantum Computing", input.Topic);
        Assert.True(result.IterationsUsed > 0);
    }

    [Fact]
    public async Task ExecuteAsync_CompletesSuccessfully_WithQualityThresholdMet()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "AI Safety",
            ResearchBrief = "Research AI safety concerns",
            MaxIterations = 3,
            MinQualityThreshold = 6.0f
        };

        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Findings);
        Assert.True(result.IterationsUsed >= 1);
        Assert.True(result.AverageQuality >= 0);
    }

    [Fact]
    public async Task ExecuteAsync_PopulatesResearchTopicsCovered()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Machine Learning",
            ResearchBrief = "Research ML applications",
            MaxIterations = 1
        };

        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ResearchTopicsCovered);
    }

    [Fact]
    public async Task ExecuteAsync_ExtractsFacts_AndPopulatesFindings()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Blockchain",
            ResearchBrief = "Research blockchain technology",
            MaxIterations = 1
        };

        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Findings);
        Assert.True(result.TotalFactsExtracted > 0);
    }

    #endregion

    #region Tool Integration Tests

    [Fact]
    public async Task ExecuteAsync_CallsWebSearchTool()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test Topic",
            ResearchBrief = "Test brief",
            MaxIterations = 1
        };

        SetupMocks();

        // Act
        await _agent.ExecuteAsync(input);

        // Assert
        _mockToolService.Verify(
            s => s.InvokeToolAsync(
                "websearch",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_CallsSummarizationTool()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test Topic",
            ResearchBrief = "Test brief",
            MaxIterations = 1
        };

        SetupMocks();

        // Act
        await _agent.ExecuteAsync(input);

        // Assert
        _mockToolService.Verify(
            s => s.InvokeToolAsync(
                "summarize",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_CallsFactExtractionTool()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test Topic",
            ResearchBrief = "Test brief",
            MaxIterations = 1
        };

        SetupMocks();

        // Act
        await _agent.ExecuteAsync(input);

        // Assert
        _mockToolService.Verify(
            s => s.InvokeToolAsync(
                "extractfacts",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    #endregion

    #region Quality Evaluation Tests

    [Fact]
    public async Task ExecuteAsync_CalculatesAverageQuality()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test",
            ResearchBrief = "Test brief",
            MaxIterations = 1
        };

        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.True(result.AverageQuality >= 0);
        Assert.True(result.AverageQuality <= 1.0f); // Normalized confidence
    }

    [Fact]
    public async Task ExecuteAsync_StopsAtQualityThreshold()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test",
            ResearchBrief = "Test brief",
            MaxIterations = 5,
            MinQualityThreshold = 0.1f // Very low threshold
        };

        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.True(result.IterationsUsed <= input.MaxIterations);
        Assert.Equal("completed_quality_threshold", result.CompletionStatus);
    }

    [Fact]
    public async Task ExecuteAsync_ReachesMaxIterations_WhenQualityLow()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test",
            ResearchBrief = "Test brief",
            MaxIterations = 2,
            MinQualityThreshold = 10.0f // Impossible threshold
        };

        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.Equal(input.MaxIterations, result.IterationsUsed);
        Assert.Equal("completed_max_iterations", result.CompletionStatus);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_WithNoSearchResults_HandlesGracefully()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Nonexistent Topic XYZ123",
            ResearchBrief = "Test brief",
            MaxIterations = 1
        };

        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage { Role = "assistant", Content = "[\"test_topic\"]" });

        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "websearch",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebSearchResult>()); // Empty results

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        // Should have attempted research even with no results
    }

    [Fact]
    public async Task ExecuteAsync_WithToolException_LogsWarningAndContinues()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test",
            ResearchBrief = "Test brief",
            MaxIterations = 1
        };

        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage { Role = "assistant", Content = "[\"topic1\"]" });

        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "websearch",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        // Act - Should not throw despite tool exception
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _agent.ExecuteAsync(null!));
    }

    #endregion

    #region Iteration Tests

    [Fact]
    public async Task ExecuteAsync_PerformsMultipleIterations()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test",
            ResearchBrief = "Test brief",
            MaxIterations = 3,
            MinQualityThreshold = 10.0f // Force all iterations
        };

        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.Equal(3, result.IterationsUsed);
    }

    [Fact]
    public async Task ExecuteAsync_MultipleIterations_CumulateFindings()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test",
            ResearchBrief = "Test brief",
            MaxIterations = 2,
            MinQualityThreshold = 10.0f // Force multiple iterations
        };

        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotEmpty(result.Findings);
        Assert.True(result.IterationsUsed > 1);
    }

    #endregion

    #region Helper Methods

    private void SetupMocks()
    {
        // Setup LLM to return topics
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage { Role = "assistant", Content = "[\"topic1\", \"topic2\"]" });

        // Setup WebSearch tool
        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "websearch",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebSearchResult>
            {
                new WebSearchResult
                {
                    Title = "Test Result",
                    Url = "https://example.com",
                    Content = "Test content with information about the topic"
                }
            });

        // Setup Summarization tool
        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "summarize",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PageSummaryResult
            {
                Summary = "Test summary of the content",
                KeyPoints = new List<string> { "Point 1", "Point 2" }
            });

        // Setup FactExtraction tool
        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "extractfacts",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FactExtractionResult
            {
                Facts = new List<ExtractedFact>
                {
                    new ExtractedFact
                    {
                        Statement = "Test fact about the topic",
                        Confidence = 0.85f,
                        Source = "test source",
                        Category = "research"
                    },
                    new ExtractedFact
                    {
                        Statement = "Another test fact",
                        Confidence = 0.90f,
                        Source = "test source",
                        Category = "research"
                    }
                }
            });
    }

    #endregion
}
