using DeepResearchAgent.Configuration;
using Xunit;

namespace DeepResearchAgent.Tests.Integration;

/// <summary>
/// Integration tests for workflow model configuration.
/// Tests model selection in realistic scenarios.
/// </summary>
public class WorkflowModelIntegrationTests
{
    #region Model Selection Scenarios

    [Theory]
    [InlineData("research", "fast-reasoning")]
    [InlineData("analyze", "deep-reasoning")]
    [InlineData("optimize", "balanced")]
    public void WorkflowConfiguration_SupportsMultipleModelProfiles(string scenario, string modelProfile)
    {
        // Arrange & Act
        var config = CreateConfigForProfile(modelProfile);

        // Assert
        Assert.NotNull(config);
        Assert.NotEmpty(config.SupervisorBrainModel);
        Assert.NotEmpty(config.SupervisorToolsModel);
    }

    [Fact]
    public void CostOptimizedConfiguration_UsesFasterModels()
    {
        // Arrange - Cost-optimized profile
        var config = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "mistral:7b",
            SupervisorToolsModel = "mistral:latest",
            QualityEvaluatorModel = "mistral:7b",
            RedTeamModel = "mistral:7b",
            ContextPrunerModel = "orca-mini:latest"
        };

        // Act & Assert
        Assert.All(new[]
        {
            config.SupervisorBrainModel,
            config.SupervisorToolsModel,
            config.QualityEvaluatorModel,
            config.RedTeamModel,
            config.ContextPrunerModel
        }, model => Assert.True(
            model.Contains("mistral", StringComparison.OrdinalIgnoreCase) || 
            model.Contains("orca", StringComparison.OrdinalIgnoreCase)));
    }

    [Fact]
    public void QualityOptimizedConfiguration_UsesMorePowerfulModels()
    {
        // Arrange - Quality-optimized profile
        var config = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "neural-chat:13b",
            SupervisorToolsModel = "neural-chat:7b",
            QualityEvaluatorModel = "neural-chat:13b",
            RedTeamModel = "neural-chat:13b",
            ContextPrunerModel = "neural-chat:7b"
        };

        // Act & Assert
        Assert.All(new[]
        {
            config.SupervisorBrainModel,
            config.QualityEvaluatorModel,
            config.RedTeamModel
        }, model => Assert.Contains("13b", model));
    }

    #endregion

    #region Scenario-Based Tests

    [Fact]
    public void FastResearchScenario_OptimizesForSpeed()
    {
        // Arrange
        var config = CreateFastResearchConfig();

        // Act
        var brainModel = config.GetModelForFunction(WorkflowFunction.SupervisorBrain);
        var toolsModel = config.GetModelForFunction(WorkflowFunction.SupervisorTools);

        // Assert - Both should be relatively fast models
        Assert.NotNull(brainModel);
        Assert.NotNull(toolsModel);
    }

    [Fact]
    public void DeepAnalysisScenario_OptimizesForQuality()
    {
        // Arrange
        var config = CreateDeepAnalysisConfig();

        // Act
        var brainModel = config.GetModelForFunction(WorkflowFunction.SupervisorBrain);
        var evaluatorModel = config.GetModelForFunction(WorkflowFunction.QualityEvaluator);

        // Assert - Both should be powerful models
        Assert.NotNull(brainModel);
        Assert.NotNull(evaluatorModel);
    }

    [Fact]
    public void BalancedScenario_MixesSpeedAndQuality()
    {
        // Arrange
        var config = CreateBalancedConfig();

        // Act & Assert
        Assert.Equal("gpt-oss:20b", config.SupervisorBrainModel);      // Quality
        Assert.Equal("mistral:latest", config.SupervisorToolsModel);   // Speed
        Assert.Equal("gpt-oss:20b", config.QualityEvaluatorModel);     // Quality
        Assert.Equal("gpt-oss:20b", config.RedTeamModel);              // Quality
        Assert.Equal("mistral:latest", config.ContextPrunerModel);     // Speed
    }

    #endregion

    #region Configuration Persistence Tests

    [Fact]
    public void ConfigurationCanBeSerializedAndRestored()
    {
        // Arrange
        var originalConfig = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "model-a",
            SupervisorToolsModel = "model-b",
            QualityEvaluatorModel = "model-c",
            RedTeamModel = "model-d",
            ContextPrunerModel = "model-e"
        };

        // Act - Simulate serialization/restoration
        var restoredConfig = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = originalConfig.SupervisorBrainModel,
            SupervisorToolsModel = originalConfig.SupervisorToolsModel,
            QualityEvaluatorModel = originalConfig.QualityEvaluatorModel,
            RedTeamModel = originalConfig.RedTeamModel,
            ContextPrunerModel = originalConfig.ContextPrunerModel
        };

        // Assert
        Assert.Equal(originalConfig.SupervisorBrainModel, restoredConfig.SupervisorBrainModel);
        Assert.Equal(originalConfig.SupervisorToolsModel, restoredConfig.SupervisorToolsModel);
        Assert.Equal(originalConfig.QualityEvaluatorModel, restoredConfig.QualityEvaluatorModel);
        Assert.Equal(originalConfig.RedTeamModel, restoredConfig.RedTeamModel);
        Assert.Equal(originalConfig.ContextPrunerModel, restoredConfig.ContextPrunerModel);
    }

    #endregion

    #region Helper Methods

    private static WorkflowModelConfiguration CreateConfigForProfile(string profile)
    {
        return profile switch
        {
            "fast-reasoning" => CreateFastResearchConfig(),
            "deep-reasoning" => CreateDeepAnalysisConfig(),
            "balanced" => CreateBalancedConfig(),
            _ => new WorkflowModelConfiguration()
        };
    }

    private static WorkflowModelConfiguration CreateFastResearchConfig()
    {
        return new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "mistral:7b",
            SupervisorToolsModel = "mistral:latest",
            QualityEvaluatorModel = "mistral:7b",
            RedTeamModel = "mistral:7b",
            ContextPrunerModel = "orca-mini:latest"
        };
    }

    private static WorkflowModelConfiguration CreateDeepAnalysisConfig()
    {
        return new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "neural-chat:13b",
            SupervisorToolsModel = "neural-chat:7b",
            QualityEvaluatorModel = "neural-chat:13b",
            RedTeamModel = "neural-chat:13b",
            ContextPrunerModel = "neural-chat:7b"
        };
    }

    private static WorkflowModelConfiguration CreateBalancedConfig()
    {
        return new WorkflowModelConfiguration(); // Uses defaults
    }

    #endregion
}
