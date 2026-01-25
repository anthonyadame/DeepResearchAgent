using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Agents;

/// <summary>
/// Unit tests for ReportAgent.
/// Tests report structure, formatting, citations, and validation.
/// </summary>
public class ReportAgentTests
{
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<ToolInvocationService> _mockToolService;
    private readonly Mock<ILogger<ReportAgent>> _mockLogger;
    private readonly ReportAgent _agent;

    public ReportAgentTests()
    {
        _mockLlmService = new Mock<OllamaService>(null);
        _mockToolService = new Mock<ToolInvocationService>(null, null);
        _mockLogger = new Mock<ILogger<ReportAgent>>();
        _agent = new ReportAgent(_mockLlmService.Object, _mockToolService.Object, _mockLogger.Object);
    }

    #region Happy Path Tests

    [Fact]
    public async Task ExecuteAsync_WithValidInput_ReturnsCompleteReport()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Title);
        Assert.NotEmpty(result.ExecutiveSummary);
        Assert.NotEmpty(result.Sections);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesTitle()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result.Title);
        Assert.NotEmpty(result.Title);
        Assert.True(result.Title.Length > 5);
    }

    [Fact]
    public async Task ExecuteAsync_GeneratesExecutiveSummary()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result.ExecutiveSummary);
        Assert.NotEmpty(result.ExecutiveSummary);
        Assert.True(result.ExecutiveSummary.Length > 50);
    }

    [Fact]
    public async Task ExecuteAsync_CreatesMultipleSections()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotEmpty(result.Sections);
        Assert.True(result.Sections.Count >= 3);
    }

    #endregion

    #region Report Structure Tests

    [Fact]
    public async Task ExecuteAsync_IncludesIntroductionSection()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.True(result.Sections.Any(s => s.Heading.Contains("Introduction")));
    }

    [Fact]
    public async Task ExecuteAsync_IncludesKeyFindingsSection()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.True(result.Sections.Any(s => s.Heading.Contains("Finding") || s.Heading.Contains("Analysis")));
    }

    [Fact]
    public async Task ExecuteAsync_IncludesConclusionSection()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.True(result.Sections.Any(s => s.Heading.Contains("Conclusion") || 
                                             s.Heading.Contains("Insight")));
    }

    [Fact]
    public async Task ExecuteAsync_EachSectionHasContent()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.All(result.Sections, section =>
        {
            Assert.NotEmpty(section.Heading);
            Assert.NotEmpty(section.Content);
        });
    }

    #endregion

    #region Citation Tests

    [Fact]
    public async Task ExecuteAsync_GeneratesCitations()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result.Citations);
        // Citations may be empty or populated depending on findings
    }

    [Fact]
    public async Task ExecuteAsync_CitationsHaveValidStructure()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        foreach (var citation in result.Citations)
        {
            Assert.True(citation.Index > 0);
            Assert.NotEmpty(citation.Source);
        }
    }

    #endregion

    #region Quality Tests

    [Fact]
    public async Task ExecuteAsync_CalculatesQualityScore()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.True(result.QualityScore >= 0);
        Assert.True(result.QualityScore <= 1);
    }

    [Fact]
    public async Task ExecuteAsync_ValidatesCompleteness()
    {
        // Arrange
        var input = CreateTestReportInput();
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.NotNull(result.CompletionStatus);
        Assert.True(result.CompletionStatus == "complete" || result.CompletionStatus == "incomplete");
    }

    [Fact]
    public async Task ExecuteAsync_WithHighQualityInput_HighQualityOutput()
    {
        // Arrange
        var input = CreateTestReportInput();
        input.Research.AverageQuality = 0.9f;
        input.Analysis.ConfidenceScore = 0.85f;
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        // Assert
        Assert.True(result.QualityScore > 0.5f);
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
    public async Task ExecuteAsync_WithEmptyResearch_HandlesGracefully()
    {
        // Arrange
        var input = new ReportInput
        {
            Research = new ResearchOutput(),
            Analysis = new AnalysisOutput { SynthesisNarrative = "Test" },
            Topic = "Test"
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
        var input = CreateTestReportInput();
        
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

    #endregion

    #region Metadata Tests

    [Fact]
    public async Task ExecuteAsync_SetsCreatedAt()
    {
        // Arrange
        var input = CreateTestReportInput();
        var beforeExecution = DateTime.UtcNow;
        SetupMocks();

        // Act
        var result = await _agent.ExecuteAsync(input);

        var afterExecution = DateTime.UtcNow;

        // Assert
        Assert.True(result.CreatedAt >= beforeExecution && result.CreatedAt <= afterExecution);
    }

    #endregion

    #region Helper Methods

    private ReportInput CreateTestReportInput()
    {
        return new ReportInput
        {
            Research = new ResearchOutput
            {
                Findings = new List<FactExtractionResult>
                {
                    new FactExtractionResult
                    {
                        Facts = new List<ExtractedFact>
                        {
                            new ExtractedFact
                            {
                                Statement = "Key finding 1",
                                Confidence = 0.9f,
                                Source = "https://source1.com",
                                Category = "research"
                            },
                            new ExtractedFact
                            {
                                Statement = "Key finding 2",
                                Confidence = 0.85f,
                                Source = "https://source2.com",
                                Category = "research"
                            }
                        }
                    }
                },
                AverageQuality = 0.87f,
                ResearchTopicsCovered = new List<string> { "topic1", "topic2" }
            },
            Analysis = new AnalysisOutput
            {
                SynthesisNarrative = "This analysis shows important findings about the topic.",
                KeyInsights = new List<KeyInsight>
                {
                    new KeyInsight
                    {
                        Statement = "Key insight 1",
                        Importance = 0.9f
                    },
                    new KeyInsight
                    {
                        Statement = "Key insight 2",
                        Importance = 0.8f
                    }
                },
                ConfidenceScore = 0.85f
            },
            Topic = "Test Topic",
            Author = "Test Author"
        };
    }

    private void SetupMocks()
    {
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<OllamaChatMessage> messages, string? _, CancellationToken _) =>
            {
                var content = messages.FirstOrDefault()?.Content ?? "";
                
                if (content.Contains("title"))
                    return new OllamaChatMessage { Role = "assistant", Content = "Comprehensive Research Report on Test Topic" };
                
                if (content.Contains("summary"))
                    return new OllamaChatMessage { Role = "assistant", Content = "This executive summary provides an overview of the research findings and key insights about the topic." };
                
                if (content.Contains("conclusion"))
                    return new OllamaChatMessage { Role = "assistant", Content = "The research demonstrates important findings that advance our understanding of the topic." };
                
                if (content.Contains("Polish"))
                    return new OllamaChatMessage { Role = "assistant", Content = "The polished content maintains accuracy while improving clarity and professional presentation." };
                
                return new OllamaChatMessage { Role = "assistant", Content = "Default response" };
            });
    }

    #endregion
}
