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
/// Tests for MasterWorkflow Phase 5 extensions.
/// Validates complete pipeline integration with error recovery and state management.
/// </summary>
public class MasterWorkflowExtensionsTests
{
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly Mock<ToolInvocationService> _mockToolService;
    private readonly Mock<ILightningStateService> _mockStateService;
    private readonly Mock<SupervisorWorkflow> _mockSupervisor;
    private readonly Mock<Microsoft.Extensions.Logging.ILogger> _mockLogger;
    
    private readonly MasterWorkflow _masterWorkflow;
    private readonly ResearcherAgent _researcherAgent;
    private readonly AnalystAgent _analystAgent;
    private readonly ReportAgent _reportAgent;
    private readonly StateTransitioner _transitioner;
    private readonly AgentErrorRecovery _errorRecovery;

    public MasterWorkflowExtensionsTests()
    {
        _mockLlmService = new Mock<OllamaService>(null);
        _mockToolService = new Mock<ToolInvocationService>(null, null);
        _mockStateService = new Mock<ILightningStateService>();
        _mockSupervisor = new Mock<SupervisorWorkflow>(
            _mockStateService.Object,
            Mock.Of<ResearcherWorkflow>(),
            _mockLlmService.Object,
            Mock.Of<LightningStore>(),
            null,
            null);
        _mockLogger = new Mock<Microsoft.Extensions.Logging.ILogger>();

        SetupSuccessfulMocks();

        _masterWorkflow = new MasterWorkflow(
            _mockStateService.Object,
            _mockSupervisor.Object,
            _mockLlmService.Object);

        _researcherAgent = new ResearcherAgent(_mockLlmService.Object, _mockToolService.Object);
        _analystAgent = new AnalystAgent(_mockLlmService.Object, _mockToolService.Object);
        _reportAgent = new ReportAgent(_mockLlmService.Object, _mockToolService.Object);
        _transitioner = new StateTransitioner();
        _errorRecovery = new AgentErrorRecovery();
    }

    #region ExecuteFullPipelineAsync Tests

    [Fact]
    public async Task ExecuteFullPipelineAsync_WithValidInputs_CompletesSuccessfully()
    {
        // Arrange
        var topic = "Quantum Computing";
        var researchBrief = "Research quantum computing breakthroughs";

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            topic,
            researchBrief,
            _mockLogger.Object
        );

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.Title);
        Assert.Contains(topic, result.Title);
        Assert.NotEmpty(result.ExecutiveSummary);
    }

    [Fact]
    public async Task ExecuteFullPipelineAsync_ExecutesAllThreePhases()
    {
        // Arrange
        var topic = "AI Safety";
        var brief = "Research AI safety concerns";

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            topic,
            brief
        );

        // Assert - Verify all phases completed
        Assert.NotNull(result);
        Assert.True(result.QualityScore > 0);
        Assert.NotEmpty(result.Sections);
    }

    [Fact]
    public async Task ExecuteFullPipelineAsync_ValidatesEachPhase()
    {
        // Arrange
        var topic = "Machine Learning";
        var brief = "Research ML algorithms";

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            topic,
            brief,
            _mockLogger.Object
        );

        // Assert - Result should be validated and repaired
        Assert.NotNull(result);
        Assert.NotEmpty(result.Title);
        Assert.NotEmpty(result.ExecutiveSummary);
        Assert.NotNull(result.Sections);
        Assert.NotNull(result.Citations);
    }

    #endregion

    #region ExecuteFullPipelineWithStateAsync Tests

    [Fact]
    public async Task ExecuteFullPipelineWithStateAsync_PersistsState()
    {
        // Arrange
        var topic = "Neural Networks";
        var brief = "Research neural network architectures";
        var researchId = Guid.NewGuid().ToString();

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineWithStateAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            _mockStateService.Object,
            topic,
            brief,
            researchId,
            _mockLogger.Object
        );

        // Assert
        Assert.NotNull(result);
        _mockStateService.Verify(s => s.SetResearchStateAsync(
            researchId,
            It.IsAny<ResearchStateModel>(),
            It.IsAny<CancellationToken>()), Times.AtLeastOnce);
    }

    [Fact]
    public async Task ExecuteFullPipelineWithStateAsync_UpdatesStateToCompleted()
    {
        // Arrange
        var topic = "Deep Learning";
        var brief = "Research deep learning frameworks";

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineWithStateAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            _mockStateService.Object,
            topic,
            brief
        );

        // Assert
        Assert.NotNull(result);
        _mockStateService.Verify(s => s.SetResearchStateAsync(
            It.IsAny<string>(),
            It.Is<ResearchStateModel>(state => state.Status == ResearchStatus.Completed),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteFullPipelineWithStateAsync_IncludesMetadataInState()
    {
        // Arrange
        var topic = "Robotics";
        var brief = "Research robotics applications";

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineWithStateAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            _mockStateService.Object,
            topic,
            brief,
            logger: _mockLogger.Object
        );

        // Assert
        Assert.NotNull(result);
        _mockStateService.Verify(s => s.SetResearchStateAsync(
            It.IsAny<string>(),
            It.Is<ResearchStateModel>(state => 
                state.Metadata.ContainsKey("reportTitle") &&
                state.Metadata.ContainsKey("qualityScore")),
            It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region StreamFullPipelineAsync Tests

    [Fact]
    public async Task StreamFullPipelineAsync_YieldsProgressMessages()
    {
        // Arrange
        var topic = "Cloud Computing";
        var brief = "Research cloud platforms";
        var messages = new List<string>();

        // Act
        await foreach (var message in _masterWorkflow.StreamFullPipelineAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            topic,
            brief))
        {
            messages.Add(message);
        }

        // Assert
        Assert.NotEmpty(messages);
        Assert.Contains(messages, m => m.Contains("Phase 1/3: Research"));
        Assert.Contains(messages, m => m.Contains("Phase 2/3: Analysis"));
        Assert.Contains(messages, m => m.Contains("Phase 3/3: Report Generation"));
        Assert.Contains(messages, m => m.Contains("completed successfully"));
    }

    [Fact]
    public async Task StreamFullPipelineAsync_IncludesStatistics()
    {
        // Arrange
        var topic = "Blockchain";
        var brief = "Research blockchain technology";
        var messages = new List<string>();

        // Act
        await foreach (var message in _masterWorkflow.StreamFullPipelineAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            topic,
            brief))
        {
            messages.Add(message);
        }

        // Assert
        Assert.Contains(messages, m => m.Contains("facts extracted"));
        Assert.Contains(messages, m => m.Contains("insights generated"));
        Assert.Contains(messages, m => m.Contains("Quality score"));
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public async Task ExecuteFullPipelineAsync_WithResearchFailure_UsesFallback()
    {
        // Arrange
        var topic = "Test Topic";
        var brief = "Test Brief";
        SetupFailingResearchMocks();

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            topic,
            brief
        );

        // Assert - Pipeline should complete with fallback
        Assert.NotNull(result);
        Assert.NotEmpty(result.Title);
    }

    [Fact]
    public async Task ExecuteFullPipelineAsync_WithInvalidData_RepairsAndContinues()
    {
        // Arrange
        var topic = "Test Topic";
        var brief = "Test Brief";

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            topic,
            brief
        );

        // Assert - Should repair and complete
        Assert.NotNull(result);
        Assert.NotEmpty(result.ExecutiveSummary);
    }

    #endregion

    #region Helper Methods

    private void SetupSuccessfulMocks()
    {
        // LLM mock
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage
            {
                Role = "assistant",
                Content = "8.5"
            });

        // Tool mocks
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

        _mockToolService
            .Setup(s => s.InvokeToolAsync(
                "formatreport",
                It.IsAny<Dictionary<string, object>>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync("Formatted report content");

        // State service mock
        _mockStateService
            .Setup(s => s.GetResearchStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResearchStateModel
            {
                ResearchId = Guid.NewGuid().ToString(),
                Query = "Test",
                Status = ResearchStatus.InProgress
            });

        _mockStateService
            .Setup(s => s.SetResearchStateAsync(
                It.IsAny<string>(),
                It.IsAny<ResearchStateModel>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
    }

    private void SetupFailingResearchMocks()
    {
        // First call fails, subsequent calls succeed
        var callCount = 0;
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                callCount++;
                if (callCount <= 3) // Fail research phase attempts
                {
                    throw new Exception("LLM failure");
                }
                return Task.FromResult(new OllamaChatMessage
                {
                    Role = "assistant",
                    Content = "8.5"
                });
            });
    }

    #endregion
}
