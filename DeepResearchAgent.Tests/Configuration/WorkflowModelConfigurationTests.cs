using DeepResearchAgent.Configuration;
using Xunit;

namespace DeepResearchAgent.Tests.Configuration;

/// <summary>
/// Unit tests for WorkflowModelConfiguration.
/// Tests model selection logic and configuration management.
/// </summary>
public class WorkflowModelConfigurationTests
{
    #region Configuration Initialization Tests

    [Fact]
    public void DefaultConfiguration_HasValidDefaults()
    {
        // Arrange & Act
        var config = new WorkflowModelConfiguration();

        // Assert
        Assert.NotEmpty(config.SupervisorBrainModel);
        Assert.NotEmpty(config.SupervisorToolsModel);
        Assert.NotEmpty(config.QualityEvaluatorModel);
        Assert.NotEmpty(config.RedTeamModel);
        Assert.NotEmpty(config.ContextPrunerModel);
    }

    [Fact]
    public void DefaultConfiguration_UsesOptimizedModels()
    {
        // Arrange & Act
        var config = new WorkflowModelConfiguration();

        // Assert
        // Brain and evaluators use more powerful models
        Assert.Equal("gpt-oss:20b", config.SupervisorBrainModel);
        Assert.Equal("gpt-oss:20b", config.QualityEvaluatorModel);
        Assert.Equal("gpt-oss:20b", config.RedTeamModel);

        // Tools and pruner use faster models
        Assert.Equal("mistral:7b", config.SupervisorToolsModel);
        Assert.Equal("mistral:7b", config.ContextPrunerModel);
    }

    #endregion

    #region Model Selection Tests

    [Fact]
    public void GetModelForFunction_SupervisorBrain_ReturnsCorrectModel()
    {
        // Arrange
        var config = new WorkflowModelConfiguration();

        // Act
        var model = config.GetModelForFunction(WorkflowFunction.SupervisorBrain);

        // Assert
        Assert.Equal(config.SupervisorBrainModel, model);
    }

    [Fact]
    public void GetModelForFunction_SupervisorTools_ReturnsCorrectModel()
    {
        // Arrange
        var config = new WorkflowModelConfiguration();

        // Act
        var model = config.GetModelForFunction(WorkflowFunction.SupervisorTools);

        // Assert
        Assert.Equal(config.SupervisorToolsModel, model);
    }

    [Fact]
    public void GetModelForFunction_QualityEvaluator_ReturnsCorrectModel()
    {
        // Arrange
        var config = new WorkflowModelConfiguration();

        // Act
        var model = config.GetModelForFunction(WorkflowFunction.QualityEvaluator);

        // Assert
        Assert.Equal(config.QualityEvaluatorModel, model);
    }

    [Fact]
    public void GetModelForFunction_RedTeam_ReturnsCorrectModel()
    {
        // Arrange
        var config = new WorkflowModelConfiguration();

        // Act
        var model = config.GetModelForFunction(WorkflowFunction.RedTeam);

        // Assert
        Assert.Equal(config.RedTeamModel, model);
    }

    [Fact]
    public void GetModelForFunction_ContextPruner_ReturnsCorrectModel()
    {
        // Arrange
        var config = new WorkflowModelConfiguration();

        // Act
        var model = config.GetModelForFunction(WorkflowFunction.ContextPruner);

        // Assert
        Assert.Equal(config.ContextPrunerModel, model);
    }

    #endregion

    #region Custom Configuration Tests

    [Fact]
    public void CustomConfiguration_AllowsModelOverride()
    {
        // Arrange
        var config = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "custom-brain:7b",
            SupervisorToolsModel = "custom-tools:3b",
            QualityEvaluatorModel = "custom-eval:5b",
            RedTeamModel = "custom-red:8b",
            ContextPrunerModel = "custom-prune:4b"
        };

        // Act & Assert
        Assert.Equal("custom-brain:7b", config.GetModelForFunction(WorkflowFunction.SupervisorBrain));
        Assert.Equal("custom-tools:3b", config.GetModelForFunction(WorkflowFunction.SupervisorTools));
        Assert.Equal("custom-eval:5b", config.GetModelForFunction(WorkflowFunction.QualityEvaluator));
        Assert.Equal("custom-red:8b", config.GetModelForFunction(WorkflowFunction.RedTeam));
        Assert.Equal("custom-prune:4b", config.GetModelForFunction(WorkflowFunction.ContextPruner));
    }

    [Fact]
    public void PartialCustomConfiguration_MixesDefaultsWithOverrides()
    {
        // Arrange
        var config = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "neural-chat:latest"
            // Other models use defaults
        };

        // Act & Assert
        Assert.Equal("neural-chat:latest", config.SupervisorBrainModel);
        Assert.Equal("mistral:7b", config.SupervisorToolsModel);
        Assert.Equal("gpt-oss:20b", config.QualityEvaluatorModel);
    }

    #endregion

    #region Function Enum Tests

    [Theory]
    [InlineData(WorkflowFunction.SupervisorBrain)]
    [InlineData(WorkflowFunction.SupervisorTools)]
    [InlineData(WorkflowFunction.QualityEvaluator)]
    [InlineData(WorkflowFunction.RedTeam)]
    [InlineData(WorkflowFunction.ContextPruner)]
    public void GetModelForFunction_WithValidFunction_ReturnsNonEmptyModel(WorkflowFunction function)
    {
        // Arrange
        var config = new WorkflowModelConfiguration();

        // Act
        var model = config.GetModelForFunction(function);

        // Assert
        Assert.NotEmpty(model);
    }

    #endregion
}
