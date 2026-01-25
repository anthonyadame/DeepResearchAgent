using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Agents;

/// <summary>
/// Unit tests for AnalystAgent.
/// Tests analysis, theme identification, contradiction detection, and synthesis.
/// </summary>
public class AnalystAgentTests
{
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<ToolInvocationService> _mockToolService;
    private readonly Mock<ILogger<AnalystAgent>> _mockLogger;
    private readonly AnalystAgent _agent;

    public AnalystAgentTests()
    {
        _mockLlmService = new Mock<OllamaService>(null);
        _mockToolService = new Mock<ToolInvocationService>(null, null);
        _mockLogger = new Mock<ILogger<AnalystAgent>>();
        _agent = new AnalystAgent(_mockLlmService.Object, _mockToolService.Object, _mockLogger.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ReturnsAnalysisOutput()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SynthesisNarrative);
        Assert.True(result.ConfidenceScore >= 0 && result.ConfidenceScore <= 1);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesKeyInsights()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.KeyInsights);
        foreach (var insight in result.KeyInsights)
        {
            Assert.NotEmpty(insight.Statement);
            Assert.True(insight.Importance >= 0 && insight.Importance <= 1);
        }
    }

    [Fact]
    public async Task ExecuteAsync_IdentifiesThemes()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ThemesIdentified);
    }

    [Fact]
    public async Task ExecuteAsync_CalculatesConfidenceScore()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.True(result.ConfidenceScore >= 0);
        Assert.True(result.ConfidenceScore <= 1);
    }

    #endregion

    #region Analysis Feature Tests

    [Fact]
    public async Task ExecuteAsync_DetectsContradictions()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result.Contradictions);
        // Contradictions may be empty or populated depending on findings
    }

    [Fact]
    public async Task ExecuteAsync_WithHighQualityFindings_HighConfidence()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        // High-quality findings should result in reasonable confidence
        Assert.True(result.ConfidenceScore > 0);
    }

    [Fact]
    public async Task ExecuteAsync_WithMultipleThemes_DiverseAnalysis()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage 
            { 
                Role = "assistant", 
                Content = "[\"Theme1\", \"Theme2\", \"Theme3\", \"Theme4\"]" 
            });

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotEmpty(result.ThemesIdentified);
    }

    #endregion

    #region LLM Integration Tests

    [Fact]
    public async Task ExecuteAsync_CallsLLMForEvaluation()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        SetupMocks();

        // Act
        await _agent.ExecuteAsync(input);

        // Assert
        _mockLlmService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()),
            Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteAsync_ParsesJsonResponses()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage 
            { 
                Role = "assistant", 
                Content = "[\"Theme1\", \"Theme2\"]" 
            });

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.ThemesIdentified);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteAsync_WithNullInput_ThrowsArgumentNullException()
    {
        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => _agent.ExecuteAsync(null!));
    }

    [Fact]
    public async Task ExecuteAsync_WithNoFindings_HandlesGracefully()
    {
        // Arrange
        var input = new AnalysisInput
        {
            Findings = new List<FactExtractionResult>(),
            ResearchBrief = "Test",
            Topic = "Test Topic"
        };
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithLLMException_LogsWarningAndContinues()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new HttpRequestException("Service unavailable"));

        // Act - Should not throw
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidJsonResponse_FallsBackGracefully()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage 
            { 
                Role = "assistant", 
                Content = "Not valid JSON" 
            });

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
    }

    #endregion

    #region Synthesis Tests

    [Fact]
    public async Task ExecuteAsync_GeneratesSynthesisNarrative()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result.SynthesisNarrative);
        Assert.NotEmpty(result.SynthesisNarrative);
        Assert.True(result.SynthesisNarrative.Length > 10);
    }

    [Fact]
    public async Task ExecuteAsync_NarrativeIncludesInsights()
    {
        // Arrange
        var input = CreateTestAnalysisInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotEmpty(result.SynthesisNarrative);
        Assert.NotEmpty(result.KeyInsights);
    }

    #endregion

    #region Helper Methods

    private AnalysisInput CreateTestAnalysisInput()
    {
        return new AnalysisInput
        {
            Findings = new List<FactExtractionResult>
            {
                new FactExtractionResult
                {
                    Facts = new List<ExtractedFact>
                    {
                        new ExtractedFact
                        {
                            Statement = "Finding 1: Important discovery",
                            Confidence = 0.9f,
                            Source = "source1",
                            Category = "research"
                        },
                        new ExtractedFact
                        {
                            Statement = "Finding 2: Another important finding",
                            Confidence = 0.85f,
                            Source = "source2",
                            Category = "research"
                        }
                    }
                },
                new FactExtractionResult
                {
                    Facts = new List<ExtractedFact>
                    {
                        new ExtractedFact
                        {
                            Statement = "Finding 3: Supporting evidence",
                            Confidence = 0.8f,
                            Source = "source3",
                            Category = "analysis"
                        }
                    }
                }
            },
            ResearchBrief = "Test research brief",
            Topic = "Test Topic"
        };
    }

    private void SetupMocks()
    {
        // Default: quality score
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<OllamaChatMessage> messages, string? _, CancellationToken _) =>
            {
                var content = messages.FirstOrDefault()?.Content ?? "";
                
                if (content.Contains("quality") || content.Contains("importance"))
                    return new OllamaChatMessage { Role = "assistant", Content = "8.5" };
                
                if (content.Contains("theme"))
                    return new OllamaChatMessage { Role = "assistant", Content = "[\"Theme1\", \"Theme2\", \"Theme3\"]" };
                
                if (content.Contains("contradict"))
                    return new OllamaChatMessage { Role = "assistant", Content = "[]" };
                
                if (content.Contains("synthesis"))
                    return new OllamaChatMessage { Role = "assistant", Content = "This analysis synthesizes the key findings about the research topic." };
                
                return new OllamaChatMessage { Role = "assistant", Content = "7.5" };
            });
    }

    #endregion
}
