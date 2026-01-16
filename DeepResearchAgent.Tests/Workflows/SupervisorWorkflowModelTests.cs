using DeepResearchAgent.Configuration;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Workflows;

/// <summary>
/// Integration tests for SupervisorWorkflow with model configuration.
/// Verifies that correct models are used for each workflow function.
/// </summary>
public class SupervisorWorkflowModelTests
{
    private readonly Mock<ILightningStateService> _mockStateService;
    private readonly Mock<ResearcherWorkflow> _mockResearcher;
    private readonly Mock<OllamaService> _mockLlmService;
    private readonly WorkflowModelConfiguration _modelConfig;

    public SupervisorWorkflowModelTests()
    {
        _mockStateService = new Mock<ILightningStateService>();
        
        // Create real OllamaService instance for ResearcherWorkflow
        var realOllamaService = new OllamaService();
        _mockResearcher = new Mock<ResearcherWorkflow>(
            realOllamaService,
            new Mock<SearCrawl4AIService>(null, null, null).Object,
            new Mock<ILogger<ResearcherWorkflow>>().Object);
        
        // Create mock OllamaService for SupervisorWorkflow
        _mockLlmService = new Mock<OllamaService>();
        _modelConfig = new WorkflowModelConfiguration();

        SetupMockLlmService();
    }

    private void SetupMockLlmService()
    {
        // Mock by intercepting calls - OllamaService is not virtual so we use real instance behavior
        _mockLlmService
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((List<OllamaChatMessage> messages, string model, CancellationToken ct) =>
                new OllamaChatMessage
                {
                    Role = "assistant",
                    Content = $"Mock response from model: {model}"
                });
    }

    #region Brain Model Tests

    [Fact]
    public async Task SupervisorBrainAsync_UsesBrainModel()
    {
        // Arrange
        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            modelConfig: _modelConfig);

        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Test research brief";

        // Act
        await supervisor.SupervisorBrainAsync(state, CancellationToken.None);

        // Assert
        _mockLlmService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                _modelConfig.SupervisorBrainModel,
                It.IsAny<CancellationToken>()),
            Times.Once,
            $"SupervisorBrain should use {_modelConfig.SupervisorBrainModel} model");
    }

    [Fact]
    public async Task SupervisorBrainAsync_WithCustomModel_UsesCustomModel()
    {
        // Arrange
        var customConfig = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "custom-brain:7b"
        };

        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            modelConfig: customConfig);

        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Test research brief";

        // Act
        await supervisor.SupervisorBrainAsync(state, CancellationToken.None);

        // Assert
        _mockLlmService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                "custom-brain:7b",
                It.IsAny<CancellationToken>()),
            Times.Once,
            "SupervisorBrain should use custom model when configured");
    }

    #endregion

    #region Quality Evaluator Model Tests

    [Fact]
    public async Task EvaluateDraftQualityAsync_WithHighIterations_UsesQualityEvaluatorModel()
    {
        // Arrange
        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            modelConfig: _modelConfig);

        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Test brief";
        state.DraftReport = "Test report";
        state.ResearchIterations = 3; // >= 3 triggers LLM evaluation
        
        var fact = StateFactory.CreateFact("Test fact", "https://example.com", 85);
        state.KnowledgeBase.Add(fact);

        // Act
        await supervisor.EvaluateDraftQualityAsync(state, CancellationToken.None);

        // Assert
        _mockLlmService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                _modelConfig.QualityEvaluatorModel,
                It.IsAny<CancellationToken>()),
            Times.Once,
            $"QualityEvaluator should use {_modelConfig.QualityEvaluatorModel} model");
    }

    [Fact]
    public async Task EvaluateDraftQualityAsync_WithCustomEvaluatorModel_UsesCustomModel()
    {
        // Arrange
        var customConfig = new WorkflowModelConfiguration
        {
            QualityEvaluatorModel = "custom-eval:13b"
        };

        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            modelConfig: customConfig);

        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Test brief";
        state.DraftReport = "Test report";
        state.ResearchIterations = 3;
        state.KnowledgeBase.Add(StateFactory.CreateFact("Test", "https://example.com", 80));

        // Act
        await supervisor.EvaluateDraftQualityAsync(state, CancellationToken.None);

        // Assert
        _mockLlmService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                "custom-eval:13b",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Red Team Model Tests

    [Fact]
    public async Task RunRedTeamAsync_UsesRedTeamModel()
    {
        // Arrange
        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            modelConfig: _modelConfig);

        var draftReport = "Test draft report content";

        // Act
        await supervisor.RunRedTeamAsync(draftReport, CancellationToken.None);

        // Assert
        _mockLlmService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                _modelConfig.RedTeamModel,
                It.IsAny<CancellationToken>()),
            Times.Once,
            $"RedTeam should use {_modelConfig.RedTeamModel} model");
    }

    [Fact]
    public async Task RunRedTeamAsync_WithCustomRedTeamModel_UsesCustomModel()
    {
        // Arrange
        var customConfig = new WorkflowModelConfiguration
        {
            RedTeamModel = "critic:latest"
        };

        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            modelConfig: customConfig);

        var draftReport = "Test draft report";

        // Act
        await supervisor.RunRedTeamAsync(draftReport, CancellationToken.None);

        // Assert
        _mockLlmService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                "critic:latest",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Context Pruner Model Tests

    [Fact]
    public async Task ContextPrunerAsync_UsesContextPrunerModel()
    {
        // Arrange
        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            modelConfig: _modelConfig);

        var state = StateFactory.CreateSupervisorState();
        state.RawNotes.Add("Test research note with some facts");

        // Act
        await supervisor.ContextPrunerAsync(state, CancellationToken.None);

        // Assert
        _mockLlmService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                _modelConfig.ContextPrunerModel,
                It.IsAny<CancellationToken>()),
            Times.Once,
            $"ContextPruner should use {_modelConfig.ContextPrunerModel} model");
    }

    [Fact]
    public async Task ContextPrunerAsync_WithCustomPrunerModel_UsesCustomModel()
    {
        // Arrange
        var customConfig = new WorkflowModelConfiguration
        {
            ContextPrunerModel = "extractor:8b"
        };

        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            modelConfig: customConfig);

        var state = StateFactory.CreateSupervisorState();
        state.RawNotes.Add("Test note");

        // Act
        await supervisor.ContextPrunerAsync(state, CancellationToken.None);

        // Assert
        _mockLlmService.Verify(
            s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                "extractor:8b",
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    #endregion

    #region Default Configuration Tests

    [Fact]
    public void SupervisorWorkflow_WithNullConfig_UsesDefaultConfiguration()
    {
        // Arrange & Act
        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object,
            modelConfig: null); // Null config

        // Assert - Should not throw, should create default config
        Assert.NotNull(supervisor);
    }

    [Fact]
    public void SupervisorWorkflow_WithoutConfigParameter_UsesDefaultConfiguration()
    {
        // Arrange & Act
        var supervisor = new SupervisorWorkflow(
            _mockStateService.Object,
            _mockResearcher.Object,
            _mockLlmService.Object); // No config parameter

        // Assert - Should not throw, should use defaults
        Assert.NotNull(supervisor);
    }

    #endregion
}
