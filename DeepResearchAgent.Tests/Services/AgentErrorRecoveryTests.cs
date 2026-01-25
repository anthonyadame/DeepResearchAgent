using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Services;

/// <summary>
/// Tests for AgentErrorRecovery service.
/// Validates error handling and recovery mechanisms in Phase 5.
/// </summary>
public class AgentErrorRecoveryTests
{
    private readonly AgentErrorRecovery _recovery;
    private readonly Mock<ILogger<AgentErrorRecovery>> _mockLogger;

    public AgentErrorRecoveryTests()
    {
        _mockLogger = new Mock<ILogger<AgentErrorRecovery>>();
        _recovery = new AgentErrorRecovery(_mockLogger.Object, maxRetries: 2, retryDelay: TimeSpan.FromMilliseconds(10));
    }

    #region ExecuteWithRetryAsync Tests

    [Fact]
    public async Task ExecuteWithRetryAsync_WithSuccessfulExecution_ReturnsResult()
    {
        // Arrange
        var input = "test input";
        var expectedOutput = new ResearchOutput { AverageQuality = 8.5f };
        
        Func<string, CancellationToken, Task<ResearchOutput>> agentFunc =
            (inp, ct) => Task.FromResult(expectedOutput);
        
        Func<string, ResearchOutput> fallbackFunc =
            (inp) => new ResearchOutput { AverageQuality = 1.0f };

        // Act
        var result = await _recovery.ExecuteWithRetryAsync(
            agentFunc, input, fallbackFunc, "TestAgent");

        // Assert
        Assert.Equal(expectedOutput, result);
        Assert.Equal(8.5f, result.AverageQuality);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithTransientFailure_RetriesAndSucceeds()
    {
        // Arrange
        var input = "test input";
        var attemptCount = 0;
        var expectedOutput = new ResearchOutput { AverageQuality = 8.5f };

        Func<string, CancellationToken, Task<ResearchOutput>> agentFunc =
            (inp, ct) =>
            {
                attemptCount++;
                if (attemptCount < 2)
                    throw new Exception("Transient error");
                return Task.FromResult(expectedOutput);
            };

        Func<string, ResearchOutput> fallbackFunc =
            (inp) => new ResearchOutput { AverageQuality = 1.0f };

        // Act
        var result = await _recovery.ExecuteWithRetryAsync(
            agentFunc, input, fallbackFunc, "TestAgent");

        // Assert
        Assert.Equal(2, attemptCount);
        Assert.Equal(expectedOutput, result);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithPersistentFailure_UsesFallback()
    {
        // Arrange
        var input = "test input";
        var fallbackOutput = new ResearchOutput { AverageQuality = 1.0f };

        Func<string, CancellationToken, Task<ResearchOutput>> agentFunc =
            (inp, ct) => throw new Exception("Persistent error");

        Func<string, ResearchOutput> fallbackFunc =
            (inp) => fallbackOutput;

        // Act
        var result = await _recovery.ExecuteWithRetryAsync(
            agentFunc, input, fallbackFunc, "TestAgent");

        // Assert
        Assert.Equal(fallbackOutput, result);
        Assert.Equal(1.0f, result.AverageQuality);
    }

    [Fact]
    public async Task ExecuteWithRetryAsync_WithFailedFallback_ThrowsAggregateException()
    {
        // Arrange
        var input = "test input";

        Func<string, CancellationToken, Task<ResearchOutput>> agentFunc =
            (inp, ct) => throw new Exception("Agent error");

        Func<string, ResearchOutput> fallbackFunc =
            (inp) => throw new Exception("Fallback error");

        // Act & Assert
        await Assert.ThrowsAsync<AggregateException>(() =>
            _recovery.ExecuteWithRetryAsync(agentFunc, input, fallbackFunc, "TestAgent"));
    }

    #endregion

    #region Fallback Creation Tests

    [Fact]
    public void CreateFallbackResearchOutput_CreatesValidOutput()
    {
        // Arrange
        var topic = "Quantum Computing";
        var errorMessage = "Test error";

        // Act
        var result = _recovery.CreateFallbackResearchOutput(topic, errorMessage);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Findings);
        Assert.Single(result.Findings);
        Assert.NotEmpty(result.Findings[0].Facts);
        Assert.Contains(topic, result.Findings[0].Facts[0].Statement);
        Assert.Equal(1.0f, result.AverageQuality);
    }

    [Fact]
    public void CreateFallbackAnalysisOutput_CreatesValidOutput()
    {
        // Arrange
        var topic = "AI Safety";
        var errorMessage = "Test error";

        // Act
        var result = _recovery.CreateFallbackAnalysisOutput(topic, errorMessage);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SynthesisNarrative);
        Assert.Contains(topic, result.SynthesisNarrative);
        Assert.NotEmpty(result.KeyInsights);
        Assert.Equal(0.1f, result.ConfidenceScore);
        Assert.Contains("error_recovery", result.ThemesIdentified);
    }

    [Fact]
    public void CreateFallbackReportOutput_CreatesValidOutput()
    {
        // Arrange
        var topic = "Machine Learning";
        var errorMessage = "Test error";

        // Act
        var result = _recovery.CreateFallbackReportOutput(topic, errorMessage);

        // Assert
        Assert.NotNull(result);
        Assert.Contains(topic, result.Title);
        Assert.NotEmpty(result.ExecutiveSummary);
        Assert.NotEmpty(result.Sections);
        Assert.Equal(0.1f, result.QualityScore);
        Assert.Equal("completed_with_errors", result.CompletionStatus);
    }

    #endregion

    #region ValidateAndRepair Tests - ResearchOutput

    [Fact]
    public void ValidateAndRepairResearchOutput_WithNullOutput_CreatesFallback()
    {
        // Arrange
        ResearchOutput? output = null;
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairResearchOutput(output!, topic);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public void ValidateAndRepairResearchOutput_WithNullFindings_Repairs()
    {
        // Arrange
        var output = new ResearchOutput
        {
            Findings = null!,
            AverageQuality = 8.0f
        };
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairResearchOutput(output, topic);

        // Assert
        Assert.NotNull(result.Findings);
        Assert.NotEmpty(result.Findings);
    }

    [Fact]
    public void ValidateAndRepairResearchOutput_WithEmptyFindings_Repairs()
    {
        // Arrange
        var output = new ResearchOutput
        {
            Findings = new List<FactExtractionResult>(),
            AverageQuality = 8.0f
        };
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairResearchOutput(output, topic);

        // Assert
        Assert.NotEmpty(result.Findings);
        Assert.Single(result.Findings);
    }

    [Fact]
    public void ValidateAndRepairResearchOutput_WithNullFacts_Repairs()
    {
        // Arrange
        var output = new ResearchOutput
        {
            Findings = new List<FactExtractionResult>
            {
                new() { Facts = null! }
            },
            AverageQuality = 8.0f
        };
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairResearchOutput(output, topic);

        // Assert
        Assert.NotNull(result.Findings[0].Facts);
    }

    #endregion

    #region ValidateAndRepair Tests - AnalysisOutput

    [Fact]
    public void ValidateAndRepairAnalysisOutput_WithNullOutput_CreatesFallback()
    {
        // Arrange
        AnalysisOutput? output = null;
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairAnalysisOutput(output!, topic);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.SynthesisNarrative);
    }

    [Fact]
    public void ValidateAndRepairAnalysisOutput_WithEmptyNarrative_Repairs()
    {
        // Arrange
        var output = new AnalysisOutput
        {
            SynthesisNarrative = "",
            ConfidenceScore = 0.8f
        };
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairAnalysisOutput(output, topic);

        // Assert
        Assert.NotEmpty(result.SynthesisNarrative);
        Assert.Contains(topic, result.SynthesisNarrative);
    }

    [Fact]
    public void ValidateAndRepairAnalysisOutput_WithNullCollections_Repairs()
    {
        // Arrange
        var output = new AnalysisOutput
        {
            SynthesisNarrative = "Test",
            KeyInsights = null!,
            Contradictions = null!,
            ThemesIdentified = null!
        };
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairAnalysisOutput(output, topic);

        // Assert
        Assert.NotNull(result.KeyInsights);
        Assert.NotNull(result.Contradictions);
        Assert.NotNull(result.ThemesIdentified);
        Assert.NotEmpty(result.KeyInsights); // Should have default insight
    }

    #endregion

    #region ValidateAndRepair Tests - ReportOutput

    [Fact]
    public void ValidateAndRepairReportOutput_WithNullOutput_CreatesFallback()
    {
        // Arrange
        ReportOutput? output = null;
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairReportOutput(output!, topic);

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Title);
    }

    [Fact]
    public void ValidateAndRepairReportOutput_WithEmptyTitle_Repairs()
    {
        // Arrange
        var output = new ReportOutput
        {
            Title = "",
            ExecutiveSummary = "Test"
        };
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairReportOutput(output, topic);

        // Assert
        Assert.NotEmpty(result.Title);
        Assert.Contains(topic, result.Title);
    }

    [Fact]
    public void ValidateAndRepairReportOutput_WithNullSections_Repairs()
    {
        // Arrange
        var output = new ReportOutput
        {
            Title = "Test",
            ExecutiveSummary = "Test",
            Sections = null!
        };
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairReportOutput(output, topic);

        // Assert
        Assert.NotNull(result.Sections);
        Assert.NotEmpty(result.Sections); // Should have default section
    }

    [Fact]
    public void ValidateAndRepairReportOutput_WithDefaultCreatedAt_Repairs()
    {
        // Arrange
        var output = new ReportOutput
        {
            Title = "Test",
            ExecutiveSummary = "Test",
            CreatedAt = default
        };
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairReportOutput(output, topic);

        // Assert
        Assert.NotEqual(default, result.CreatedAt);
    }

    [Fact]
    public void ValidateAndRepairReportOutput_WithEmptyCompletionStatus_Repairs()
    {
        // Arrange
        var output = new ReportOutput
        {
            Title = "Test",
            ExecutiveSummary = "Test",
            CompletionStatus = ""
        };
        var topic = "Test Topic";

        // Act
        var result = _recovery.ValidateAndRepairReportOutput(output, topic);

        // Assert
        Assert.NotEmpty(result.CompletionStatus);
    }

    #endregion

    #region GetStats Tests

    [Fact]
    public void GetStats_ReturnsStatistics()
    {
        // Act
        var stats = _recovery.GetStats();

        // Assert
        Assert.NotNull(stats);
        Assert.Equal(0, stats.TotalAttempts);
        Assert.Equal(0, stats.TotalRetries);
        Assert.Equal(0, stats.TotalFallbacks);
        Assert.Equal(0, stats.TotalRepairs);
    }

    #endregion
}
