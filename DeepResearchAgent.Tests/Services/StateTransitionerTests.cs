using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Services;

/// <summary>
/// Tests for StateTransitioner service.
/// Validates state transitions between agents in Phase 5.
/// </summary>
public class StateTransitionerTests
{
    private readonly StateTransitioner _transitioner;
    private readonly Mock<ILogger<StateTransitioner>> _mockLogger;

    public StateTransitionerTests()
    {
        _mockLogger = new Mock<ILogger<StateTransitioner>>();
        _transitioner = new StateTransitioner(_mockLogger.Object);
    }

    #region CreateAnalysisInput Tests

    [Fact]
    public void CreateAnalysisInput_WithValidResearchOutput_CreatesAnalysisInput()
    {
        // Arrange
        var research = CreateValidResearchOutput();
        var topic = "Quantum Computing";
        var brief = "Research quantum breakthroughs";

        // Act
        var result = _transitioner.CreateAnalysisInput(research, topic, brief);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(topic, result.Topic);
        Assert.Equal(brief, result.ResearchBrief);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public void CreateAnalysisInput_WithNullResearch_ThrowsArgumentNullException()
    {
        // Arrange
        ResearchOutput? research = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _transitioner.CreateAnalysisInput(research!, "topic", "brief"));
    }

    [Fact]
    public void CreateAnalysisInput_WithEmptyTopic_ThrowsArgumentException()
    {
        // Arrange
        var research = CreateValidResearchOutput();

        // Act & Assert
        Assert.Throws<ArgumentException>(() =>
            _transitioner.CreateAnalysisInput(research, "", "brief"));
    }

    [Fact]
    public void CreateAnalysisInput_WithNullBrief_UsesTopi()
    {
        // Arrange
        var research = CreateValidResearchOutput();
        var topic = "AI Safety";

        // Act
        var result = _transitioner.CreateAnalysisInput(research, topic, null!);

        // Assert
        Assert.Equal(topic, result.ResearchBrief);
    }

    #endregion

    #region CreateReportInput Tests

    [Fact]
    public void CreateReportInput_WithValidInputs_CreatesReportInput()
    {
        // Arrange
        var research = CreateValidResearchOutput();
        var analysis = CreateValidAnalysisOutput();
        var topic = "Machine Learning";

        // Act
        var result = _transitioner.CreateReportInput(research, analysis, topic);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(topic, result.Topic);
        Assert.Equal(research, result.Research);
        Assert.Equal(analysis, result.Analysis);
        Assert.Equal("Deep Research Agent", result.Author);
    }

    [Fact]
    public void CreateReportInput_WithCustomAuthor_UsesCustomAuthor()
    {
        // Arrange
        var research = CreateValidResearchOutput();
        var analysis = CreateValidAnalysisOutput();
        var topic = "AI";
        var author = "Custom Author";

        // Act
        var result = _transitioner.CreateReportInput(research, analysis, topic, author);

        // Assert
        Assert.Equal(author, result.Author);
    }

    [Fact]
    public void CreateReportInput_WithNullResearch_ThrowsArgumentNullException()
    {
        // Arrange
        var analysis = CreateValidAnalysisOutput();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _transitioner.CreateReportInput(null!, analysis, "topic"));
    }

    [Fact]
    public void CreateReportInput_WithNullAnalysis_ThrowsArgumentNullException()
    {
        // Arrange
        var research = CreateValidResearchOutput();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            _transitioner.CreateReportInput(research, null!, "topic"));
    }

    #endregion

    #region ValidateResearchOutput Tests

    [Fact]
    public void ValidateResearchOutput_WithValidOutput_ReturnsValid()
    {
        // Arrange
        var output = CreateValidResearchOutput();

        // Act
        var result = _transitioner.ValidateResearchOutput(output);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateResearchOutput_WithNullOutput_ReturnsInvalid()
    {
        // Arrange
        ResearchOutput? output = null;

        // Act
        var result = _transitioner.ValidateResearchOutput(output!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("ResearchOutput is null", result.Errors);
    }

    [Fact]
    public void ValidateResearchOutput_WithNoFindings_ReturnsInvalid()
    {
        // Arrange
        var output = new ResearchOutput
        {
            Findings = new List<FactExtractionResult>(),
            AverageQuality = 8.0f,
            IterationsUsed = 3
        };

        // Act
        var result = _transitioner.ValidateResearchOutput(output);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("ResearchOutput has no findings", result.Errors);
    }

    [Fact]
    public void ValidateResearchOutput_WithLowQuality_ReturnsWarning()
    {
        // Arrange
        var output = CreateValidResearchOutput();
        output.AverageQuality = 4.0f;

        // Act
        var result = _transitioner.ValidateResearchOutput(output);

        // Assert
        Assert.True(result.IsValid); // Still valid, just warning
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("quality is low"));
    }

    #endregion

    #region ValidateAnalysisOutput Tests

    [Fact]
    public void ValidateAnalysisOutput_WithValidOutput_ReturnsValid()
    {
        // Arrange
        var output = CreateValidAnalysisOutput();

        // Act
        var result = _transitioner.ValidateAnalysisOutput(output);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public void ValidateAnalysisOutput_WithNullOutput_ReturnsInvalid()
    {
        // Arrange
        AnalysisOutput? output = null;

        // Act
        var result = _transitioner.ValidateAnalysisOutput(output!);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("AnalysisOutput is null", result.Errors);
    }

    [Fact]
    public void ValidateAnalysisOutput_WithNoNarrative_ReturnsInvalid()
    {
        // Arrange
        var output = new AnalysisOutput
        {
            SynthesisNarrative = "",
            KeyInsights = new List<KeyInsight> { new() { Statement = "Test" } },
            ConfidenceScore = 0.8f
        };

        // Act
        var result = _transitioner.ValidateAnalysisOutput(output);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("AnalysisOutput has no synthesis narrative", result.Errors);
    }

    [Fact]
    public void ValidateAnalysisOutput_WithLowConfidence_ReturnsWarning()
    {
        // Arrange
        var output = CreateValidAnalysisOutput();
        output.ConfidenceScore = 0.4f;

        // Act
        var result = _transitioner.ValidateAnalysisOutput(output);

        // Assert
        Assert.True(result.IsValid); // Still valid, just warning
        Assert.NotEmpty(result.Warnings);
        Assert.Contains(result.Warnings, w => w.Contains("confidence is low"));
    }

    #endregion

    #region ValidatePipelineState Tests

    [Fact]
    public void ValidatePipelineState_WithValidResearch_ReturnsValid()
    {
        // Arrange
        var research = CreateValidResearchOutput();

        // Act
        var result = _transitioner.ValidatePipelineState(research);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidatePipelineState_WithValidResearchAndAnalysis_ReturnsValid()
    {
        // Arrange
        var research = CreateValidResearchOutput();
        var analysis = CreateValidAnalysisOutput();

        // Act
        var result = _transitioner.ValidatePipelineState(research, analysis);

        // Assert
        Assert.True(result.IsValid);
    }

    [Fact]
    public void ValidatePipelineState_WithInvalidResearch_ReturnsInvalid()
    {
        // Arrange
        var research = new ResearchOutput
        {
            Findings = new List<FactExtractionResult>()
        };

        // Act
        var result = _transitioner.ValidatePipelineState(research);

        // Assert
        Assert.False(result.IsValid);
        Assert.NotEmpty(result.Errors);
    }

    [Fact]
    public void ValidatePipelineState_WithEmptyTopic_ReturnsInvalid()
    {
        // Arrange
        var research = CreateValidResearchOutput();

        // Act
        var result = _transitioner.ValidatePipelineState(research, null, "");

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Topic is empty or whitespace", result.Errors);
    }

    #endregion

    #region GetResearchStatistics Tests

    [Fact]
    public void GetResearchStatistics_WithValidOutput_ReturnsStatistics()
    {
        // Arrange
        var output = CreateValidResearchOutput();

        // Act
        var stats = _transitioner.GetResearchStatistics(output);

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalFindings > 0);
        Assert.True(stats.TotalFacts > 0);
        Assert.True(stats.AverageQuality > 0);
        Assert.True(stats.IterationsUsed > 0);
    }

    [Fact]
    public void GetResearchStatistics_WithNullOutput_ReturnsEmptyStatistics()
    {
        // Arrange
        ResearchOutput? output = null;

        // Act
        var stats = _transitioner.GetResearchStatistics(output!);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalFindings);
        Assert.Equal(0, stats.TotalFacts);
    }

    #endregion

    #region GetAnalysisStatistics Tests

    [Fact]
    public void GetAnalysisStatistics_WithValidOutput_ReturnsStatistics()
    {
        // Arrange
        var output = CreateValidAnalysisOutput();

        // Act
        var stats = _transitioner.GetAnalysisStatistics(output);

        // Assert
        Assert.NotNull(stats);
        Assert.True(stats.TotalInsights > 0);
        Assert.True(stats.ConfidenceScore > 0);
        Assert.True(stats.NarrativeLength > 0);
    }

    [Fact]
    public void GetAnalysisStatistics_WithNullOutput_ReturnsEmptyStatistics()
    {
        // Arrange
        AnalysisOutput? output = null;

        // Act
        var stats = _transitioner.GetAnalysisStatistics(output!);

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalInsights);
        Assert.Equal(0, stats.TotalThemes);
    }

    #endregion

    #region Helper Methods

    private ResearchOutput CreateValidResearchOutput()
    {
        return new ResearchOutput
        {
            Findings = new List<FactExtractionResult>
            {
                new()
                {
                    Facts = new List<ExtractedFact>
                    {
                        new()
                        {
                            Statement = "Test fact 1",
                            Source = "http://test.com",
                            Confidence = 0.9f,
                            Category = "research"
                        },
                        new()
                        {
                            Statement = "Test fact 2",
                            Source = "http://test2.com",
                            Confidence = 0.8f,
                            Category = "research"
                        }
                    }
                }
            },
            AverageQuality = 8.5f,
            IterationsUsed = 3
        };
    }

    private AnalysisOutput CreateValidAnalysisOutput()
    {
        return new AnalysisOutput
        {
            SynthesisNarrative = "This is a comprehensive analysis of the research findings.",
            KeyInsights = new List<KeyInsight>
            {
                new()
                {
                    Statement = "Key insight 1",
                    Importance = 0.9f,
                    SourceFacts = new List<string> { "fact1", "fact2" }
                },
                new()
                {
                    Statement = "Key insight 2",
                    Importance = 0.8f,
                    SourceFacts = new List<string> { "fact3" }
                }
            },
            Contradictions = new List<Contradiction>(),
            ConfidenceScore = 0.85f,
            ThemesIdentified = new List<string> { "Theme 1", "Theme 2" }
        };
    }

    #endregion
}
