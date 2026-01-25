using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Integration;

/// <summary>
/// Phase 5 Integration Tests: Verifies complete pipeline with all components.
/// Tests ResearcherAgent → AnalystAgent → ReportAgent workflow with state management and error recovery.
/// </summary>
public class Phase5IntegrationTests
{
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<ToolInvocationService> _mockToolService;
    private readonly Mock<ILogger<StateTransitioner>> _mockStateLogger;
    private readonly Mock<ILogger<AgentErrorRecovery>> _mockErrorLogger;

    public Phase5IntegrationTests()
    {
        _mockLlmService = new Mock<OllamaService>(null);
        _mockToolService = new Mock<ToolInvocationService>(null, null);
        _mockStateLogger = new Mock<ILogger<StateTransitioner>>();
        _mockErrorLogger = new Mock<ILogger<AgentErrorRecovery>>();
    }

    #region Full Pipeline Integration Tests

    [Fact]
    public async Task FullPipeline_WithAllComponents_CompletesSuccessfully()
    {
        // Arrange
        var topic = "Quantum Computing";
        var researchBrief = "Research quantum computing breakthroughs";
        
        SetupSuccessfulMocks();

        var researcherAgent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object);
        var analystAgent = new AnalystAgent(_mockLlmService.Object, _mockToolService.Object);
        var reportAgent = new ReportAgent(_mockLlmService.Object, _mockToolService.Object);
        
        var transitioner = new StateTransitioner(_mockStateLogger.Object);
        var errorRecovery = new AgentErrorRecovery(_mockErrorLogger.Object);

        // Act - Execute full pipeline
        var researchInput = new ResearchInput
        {
            Topic = topic,
            ResearchBrief = researchBrief,
            MaxIterations = 3,
            MinQualityThreshold = 7.0f
        };

        var researchOutput = await errorRecovery.ExecuteWithRetryAsync(
            async (input, ct) => await researcherAgent.ExecuteAsync(input, ct),
            researchInput,
            (input) => errorRecovery.CreateFallbackResearchOutput(topic, "Research failed"),
            "ResearcherAgent"
        );

        var analysisInput = transitioner.CreateAnalysisInput(researchOutput, topic, researchBrief);
        
        var analysisOutput = await errorRecovery.ExecuteWithRetryAsync(
            async (input, ct) => await analystAgent.ExecuteAsync(input, ct),
            analysisInput,
            (input) => errorRecovery.CreateFallbackAnalysisOutput(topic, "Analysis failed"),
            "AnalystAgent"
        );

        var reportInput = transitioner.CreateReportInput(researchOutput, analysisOutput, topic);
        
        var reportOutput = await errorRecovery.ExecuteWithRetryAsync(
            async (input, ct) => await reportAgent.ExecuteAsync(input, ct),
            reportInput,
            (input) => errorRecovery.CreateFallbackReportOutput(topic, "Report failed"),
            "ReportAgent"
        );

        // Assert
        Assert.NotNull(researchOutput);
        Assert.NotNull(analysisOutput);
        Assert.NotNull(reportOutput);
        Assert.NotEmpty(reportOutput.Title);
        Assert.Contains(topic, reportOutput.Title);
    }

    [Fact]
    public async Task FullPipeline_WithStateValidation_ValidatesAtEachStep()
    {
        // Arrange
        var topic = "AI Safety";
        SetupSuccessfulMocks();

        var researcherAgent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object);
        var analystAgent = new AnalystAgent(_mockLlmService.Object, _mockToolService.Object);
        var reportAgent = new ReportAgent(_mockLlmService.Object, _mockToolService.Object);
        
        var transitioner = new StateTransitioner(_mockStateLogger.Object);

        // Act
        var researchOutput = await researcherAgent.ExecuteAsync(
            new ResearchInput { Topic = topic, MaxIterations = 2 });

        var researchValidation = transitioner.ValidateResearchOutput(researchOutput);
        Assert.True(researchValidation.IsValid);

        var analysisInput = transitioner.CreateAnalysisInput(researchOutput, topic, topic);
        var analysisOutput = await analystAgent.ExecuteAsync(analysisInput);

        var analysisValidation = transitioner.ValidateAnalysisOutput(analysisOutput);
        Assert.True(analysisValidation.IsValid);

        var reportInput = transitioner.CreateReportInput(researchOutput, analysisOutput, topic);
        var reportOutput = await reportAgent.ExecuteAsync(reportInput);

        // Assert
        Assert.NotNull(reportOutput);
        Assert.NotEmpty(reportOutput.ExecutiveSummary);
    }

    [Fact]
    public async Task FullPipeline_WithStatistics_TracksMetrics()
    {
        // Arrange
        var topic = "Machine Learning";
        SetupSuccessfulMocks();

        var researcherAgent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object);
        var analystAgent = new AnalystAgent(_mockLlmService.Object, _mockToolService.Object);
        
        var transitioner = new StateTransitioner(_mockStateLogger.Object);

        // Act
        var researchOutput = await researcherAgent.ExecuteAsync(
            new ResearchInput { Topic = topic, MaxIterations = 2 });

        var researchStats = transitioner.GetResearchStatistics(researchOutput);

        var analysisInput = transitioner.CreateAnalysisInput(researchOutput, topic, topic);
        var analysisOutput = await analystAgent.ExecuteAsync(analysisInput);

        var analysisStats = transitioner.GetAnalysisStatistics(analysisOutput);

        // Assert
        Assert.True(researchStats.TotalFacts > 0);
        Assert.True(analysisStats.TotalInsights > 0);
        Assert.True(researchStats.AverageQuality > 0);
        Assert.True(analysisStats.ConfidenceScore > 0);
    }

    #endregion

    #region Error Recovery Integration Tests

    [Fact]
    public async Task Pipeline_WithResearcherFailure_UsesFallbackAndContinues()
    {
        // Arrange
        var topic = "Quantum Physics";
        SetupFailingResearcherMock();
        SetupSuccessfulAnalystMock();
        SetupSuccessfulReportMock();

        var researcherAgent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object);
        var analystAgent = new AnalystAgent(_mockLlmService.Object, _mockToolService.Object);
        var reportAgent = new ReportAgent(_mockLlmService.Object, _mockToolService.Object);
        
        var transitioner = new StateTransitioner(_mockStateLogger.Object);
        var errorRecovery = new AgentErrorRecovery(_mockErrorLogger.Object, maxRetries: 1);

        // Act
        var researchOutput = await errorRecovery.ExecuteWithRetryAsync(
            async (input, ct) => await researcherAgent.ExecuteAsync(input, ct),
            new ResearchInput { Topic = topic },
            (input) => errorRecovery.CreateFallbackResearchOutput(topic, "Test error"),
            "ResearcherAgent"
        );

        // Verify fallback was used
        Assert.Equal(1.0f, researchOutput.AverageQuality); // Fallback quality indicator

        // Continue pipeline with fallback
        var analysisInput = transitioner.CreateAnalysisInput(researchOutput, topic, topic);
        var analysisOutput = await analystAgent.ExecuteAsync(analysisInput);

        var reportInput = transitioner.CreateReportInput(researchOutput, analysisOutput, topic);
        var reportOutput = await reportAgent.ExecuteAsync(reportInput);

        // Assert - Pipeline completed despite research failure
        Assert.NotNull(reportOutput);
        Assert.NotEmpty(reportOutput.Title);
    }

    [Fact]
    public async Task Pipeline_WithInvalidResearchOutput_RepairsAndContinues()
    {
        // Arrange
        var topic = "Neural Networks";
        var invalidResearchOutput = new ResearchOutput
        {
            Findings = null!, // Invalid - will be repaired
            AverageQuality = 8.0f
        };

        var transitioner = new StateTransitioner(_mockStateLogger.Object);
        var errorRecovery = new AgentErrorRecovery(_mockErrorLogger.Object);

        // Act
        var repairedResearch = errorRecovery.ValidateAndRepairResearchOutput(
            invalidResearchOutput, topic);

        // Assert - Should be repaired and valid
        Assert.NotNull(repairedResearch.Findings);
        Assert.NotEmpty(repairedResearch.Findings);

        // Should be able to create analysis input
        var analysisInput = transitioner.CreateAnalysisInput(repairedResearch, topic, topic);
        Assert.NotNull(analysisInput);
        Assert.NotEmpty(analysisInput.Findings);
    }

    [Fact]
    public async Task Pipeline_WithMultipleFailures_UsesMultipleFallbacks()
    {
        // Arrange
        var topic = "Robotics";
        SetupFailingMocks();

        var researcherAgent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object);
        var analystAgent = new AnalystAgent(_mockLlmService.Object, _mockToolService.Object);
        var reportAgent = new ReportAgent(_mockLlmService.Object, _mockToolService.Object);
        
        var transitioner = new StateTransitioner(_mockStateLogger.Object);
        var errorRecovery = new AgentErrorRecovery(_mockErrorLogger.Object, maxRetries: 0);

        // Act - All agents fail, all use fallbacks
        var researchOutput = await errorRecovery.ExecuteWithRetryAsync(
            async (input, ct) => await researcherAgent.ExecuteAsync(input, ct),
            new ResearchInput { Topic = topic },
            (input) => errorRecovery.CreateFallbackResearchOutput(topic, "Research failed"),
            "ResearcherAgent"
        );

        var analysisInput = transitioner.CreateAnalysisInput(researchOutput, topic, topic);
        var analysisOutput = await errorRecovery.ExecuteWithRetryAsync(
            async (input, ct) => await analystAgent.ExecuteAsync(input, ct),
            analysisInput,
            (input) => errorRecovery.CreateFallbackAnalysisOutput(topic, "Analysis failed"),
            "AnalystAgent"
        );

        var reportInput = transitioner.CreateReportInput(researchOutput, analysisOutput, topic);
        var reportOutput = await errorRecovery.ExecuteWithRetryAsync(
            async (input, ct) => await reportAgent.ExecuteAsync(input, ct),
            reportInput,
            (input) => errorRecovery.CreateFallbackReportOutput(topic, "Report failed"),
            "ReportAgent"
        );

        // Assert - Pipeline completed with all fallbacks
        Assert.Equal(1.0f, researchOutput.AverageQuality); // Fallback indicator
        Assert.Equal(0.1f, analysisOutput.ConfidenceScore); // Fallback indicator
        Assert.Equal(0.1f, reportOutput.QualityScore); // Fallback indicator
        Assert.Equal("completed_with_errors", reportOutput.CompletionStatus);
    }

    #endregion

    #region Extension Method Integration Tests

    [Fact]
    public async Task ResearcherWorkflowExtensions_WithAgent_ExecutesSuccessfully()
    {
        // Arrange
        var topic = "Cloud Computing";
        SetupSuccessfulMocks();

        var mockWorkflow = new Mock<ResearcherWorkflow>(
            Mock.Of<ILightningStateService>(),
            Mock.Of<SearCrawl4AIService>(),
            _mockLlmService.Object,
            Mock.Of<LightningStore>());

        var agent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object);

        // Act
        var result = await mockWorkflow.Object.ResearchWithAgentAsync(
            agent, topic, researchBrief: "Test brief");

        // Assert
        Assert.NotNull(result);
        // Extension method should return facts
    }

    [Fact]
    public void ExtractedFact_ToFactState_ConvertsCorrectly()
    {
        // Arrange
        var extractedFact = new ExtractedFact
        {
            Statement = "Test fact",
            Source = "http://test.com",
            Confidence = 0.9f,
            Category = "research"
        };

        // Act
        var factState = extractedFact.ToFactState();

        // Assert
        Assert.NotNull(factState);
        Assert.Equal("Test fact", factState.Content);
        Assert.Equal("http://test.com", factState.SourceUrl);
        Assert.Equal(0.9, factState.Confidence);
        Assert.Contains("research", factState.Tags);
    }

    #endregion

    #region State Transition Integration Tests

    [Fact]
    public void StateTransitioner_WithCompleteWorkflow_MapsCorrectly()
    {
        // Arrange
        var topic = "Blockchain";
        var transitioner = new StateTransitioner(_mockStateLogger.Object);

        var research = CreateSampleResearchOutput();
        var analysis = CreateSampleAnalysisOutput();

        // Act
        var analysisInput = transitioner.CreateAnalysisInput(research, topic, topic);
        var reportInput = transitioner.CreateReportInput(research, analysis, topic);

        // Assert
        Assert.Equal(topic, analysisInput.Topic);
        Assert.Equal(research.Findings, analysisInput.Findings);
        Assert.Equal(topic, reportInput.Topic);
        Assert.Equal(research, reportInput.Research);
        Assert.Equal(analysis, reportInput.Analysis);
    }

    [Fact]
    public void StateTransitioner_ValidatePipelineState_ChecksAllComponents()
    {
        // Arrange
        var topic = "Cybersecurity";
        var transitioner = new StateTransitioner(_mockStateLogger.Object);

        var research = CreateSampleResearchOutput();
        var analysis = CreateSampleAnalysisOutput();

        // Act
        var validation = transitioner.ValidatePipelineState(research, analysis, topic);

        // Assert
        Assert.True(validation.IsValid);
        Assert.Empty(validation.Errors);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulMocks()
    {
        // LLM responses
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

        // Tool responses
        SetupSuccessfulToolMocks();
    }

    private void SetupSuccessfulToolMocks()
    {
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
                    Content = "Test content"
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
                        Statement = "Test fact",
                        Source = "http://test.com",
                        Confidence = 0.9f
                    }
                }
            });
    }

    private void SetupSuccessfulAnalystMock()
    {
        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "evaluatequality",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityEvaluationResult
            {
                OverallScore = 8.5f,
                DimensionScores = new Dictionary<string, DimensionScore>(),
                Summary = "Good quality"
            });
    }

    private void SetupSuccessfulReportMock()
    {
        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "formatreport",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Formatted report content");
    }

    private void SetupFailingResearcherMock()
    {
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("LLM failure"));
    }

    private void SetupFailingMocks()
    {
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Service failure"));

        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                It.IsAny<string>(),
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ThrowsAsync(new Exception("Tool failure"));
    }

    private ResearchOutput CreateSampleResearchOutput()
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
                            Statement = "Sample fact",
                            Source = "http://sample.com",
                            Confidence = 0.85f
                        }
                    }
                }
            },
            AverageQuality = 8.0f,
            IterationsUsed = 3
        };
    }

    private AnalysisOutput CreateSampleAnalysisOutput()
    {
        return new AnalysisOutput
        {
            SynthesisNarrative = "Sample analysis narrative",
            KeyInsights = new List<KeyInsight>
            {
                new()
                {
                    Statement = "Sample insight",
                    Importance = 0.9f,
                    SourceFacts = new List<string> { "fact1" }
                }
            },
            ConfidenceScore = 0.85f,
            ThemesIdentified = new List<string> { "theme1" }
        };
    }

    #endregion
}
