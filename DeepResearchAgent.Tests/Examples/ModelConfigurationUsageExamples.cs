using DeepResearchAgent.Configuration;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace DeepResearchAgent.Tests.Examples;

/// <summary>
/// Example usage patterns for WorkflowModelConfiguration.
/// Shows how to configure and use different model strategies.
/// </summary>
public class ModelConfigurationUsageExamples
{
    #region Basic Usage Examples

    [Fact]
    public void Example_DefaultConfiguration_UsesOptimizedDefaults()
    {
        // Default configuration is already optimized
        var config = new WorkflowModelConfiguration();

        var supervisor = new SupervisorWorkflow(
            CreateMockStateService(),
            CreateMockResearcherWorkflow(),
            CreateMockOllamaService(),
            modelConfig: config);

        Assert.NotNull(supervisor);
    }

    [Fact]
    public void Example_CustomizeSingleModel()
    {
        // Override just the brain model for more reasoning capability
        var config = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "neural-chat:13b"  // More powerful reasoning
        };

        var supervisor = new SupervisorWorkflow(
            CreateMockStateService(),
            CreateMockResearcherWorkflow(),
            CreateMockOllamaService(),
            modelConfig: config);

        Assert.NotNull(supervisor);
    }

    [Fact]
    public void Example_CostOptimizedSetup()
    {
        // All fast/small models for cost efficiency
        var config = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "mistral:7b",
            SupervisorToolsModel = "mistral:7b",
            QualityEvaluatorModel = "mistral:7b",
            RedTeamModel = "mistral:7b",
            ContextPrunerModel = "orca-mini:latest"
        };

        var supervisor = new SupervisorWorkflow(
            CreateMockStateService(),
            CreateMockResearcherWorkflow(),
            CreateMockOllamaService(),
            modelConfig: config);

        Assert.NotNull(supervisor);
    }

    [Fact]
    public void Example_QualityOptimizedSetup()
    {
        // All larger/more capable models for best quality
        var config = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "neural-chat:13b",
            SupervisorToolsModel = "neural-chat:7b",
            QualityEvaluatorModel = "neural-chat:13b",
            RedTeamModel = "neural-chat:13b",
            ContextPrunerModel = "neural-chat:7b"
        };

        var supervisor = new SupervisorWorkflow(
            CreateMockStateService(),
            CreateMockResearcherWorkflow(),
            CreateMockOllamaService(),
            modelConfig: config);

        Assert.NotNull(supervisor);
    }

    #endregion

    #region Dependency Injection Examples

    [Fact]
    public void Example_DIConfiguration_WithDefaultModels()
    {
        // Setup DI container with default configuration
        var services = new ServiceCollection();
        services.AddSingleton<WorkflowModelConfiguration>();
        services.AddSingleton<OllamaService>(new OllamaService());
        services.AddSingleton<ILightningStateService>(CreateMockStateService());
        services.AddSingleton<ResearcherWorkflow>(CreateMockResearcherWorkflow());
        services.AddSingleton<SupervisorWorkflow>();

        var provider = services.BuildServiceProvider();
        var supervisor = provider.GetRequiredService<SupervisorWorkflow>();

        Assert.NotNull(supervisor);
    }

    [Fact]
    public void Example_DIConfiguration_WithCustomModels()
    {
        // Setup DI container with custom configuration
        var services = new ServiceCollection();
        
        // Register custom configuration
        services.AddSingleton(_ => new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "custom-model:13b",
            RedTeamModel = "critic:latest"
        });
        
        services.AddSingleton<OllamaService>(new OllamaService());
        services.AddSingleton<ILightningStateService>(CreateMockStateService());
        services.AddSingleton<ResearcherWorkflow>(CreateMockResearcherWorkflow());
        services.AddSingleton<SupervisorWorkflow>();

        var provider = services.BuildServiceProvider();
        var config = provider.GetRequiredService<WorkflowModelConfiguration>();

        Assert.Equal("custom-model:13b", config.SupervisorBrainModel);
        Assert.Equal("critic:latest", config.RedTeamModel);
    }

    #endregion

    #region Model Selection Strategy Examples

    [Fact]
    public void Example_SelectModelBasedOnScenario()
    {
        // Different configurations for different scenarios
        var configs = new Dictionary<string, WorkflowModelConfiguration>
        {
            ["fast"] = CreateFastConfig(),
            ["balanced"] = CreateBalancedConfig(),
            ["quality"] = CreateQualityConfig()
        };

        var scenario = "balanced";
        var config = configs[scenario];

        Assert.NotNull(config);
    }

    [Fact]
    public void Example_DynamicModelSelection()
    {
        // Select model based on runtime conditions
        var baseConfig = new WorkflowModelConfiguration();

        // Upgrade brain model for complex queries
        var isComplexQuery = true;
        if (isComplexQuery)
        {
            baseConfig.SupervisorBrainModel = "neural-chat:13b";
        }

        Assert.Equal("neural-chat:13b", baseConfig.SupervisorBrainModel);
    }

    #endregion

    #region Testing with Models

    [Fact]
    public async Task Example_TestWithSpecificModel()
    {
        // Test with a specific model to verify behavior
        var testConfig = new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "test-model:latest"
        };

        var supervisor = new SupervisorWorkflow(
            CreateMockStateService(),
            CreateMockResearcherWorkflow(),
            CreateMockOllamaService(),
            modelConfig: testConfig);

        var state = StateFactory.CreateSupervisorState();
        state.ResearchBrief = "Test research";

        var result = await supervisor.SupervisorBrainAsync(state, CancellationToken.None);
        Assert.NotNull(result);
    }

    #endregion

    #region Helper Methods

    private static WorkflowModelConfiguration CreateFastConfig()
    {
        return new WorkflowModelConfiguration
        {
            SupervisorBrainModel = "mistral:7b",
            SupervisorToolsModel = "mistral:7b",
            QualityEvaluatorModel = "mistral:7b",
            RedTeamModel = "mistral:7b",
            ContextPrunerModel = "orca-mini:latest"
        };
    }

    private static WorkflowModelConfiguration CreateBalancedConfig()
    {
        return new WorkflowModelConfiguration(); // Default is balanced
    }

    private static WorkflowModelConfiguration CreateQualityConfig()
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

    private static ILightningStateService CreateMockStateService()
    {
        var mock = new Mock<ILightningStateService>();
        mock.Setup(s => s.UpdateResearchProgressAsync(
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<double>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        return mock.Object;
    }

    private static ResearcherWorkflow CreateMockResearcherWorkflow()
    {
        var mock = new Mock<ResearcherWorkflow>(
            new Mock<OllamaService>().Object,
            new Mock<SearCrawl4AIService>(null, null, null).Object,
            new Mock<ILogger<ResearcherWorkflow>>().Object);
        
        var emptyList = new List<Models.FactState>();
        mock.Setup(r => r.ResearchAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>(),
                It.IsAny<ApoExecutionOptions>()))
            .ReturnsAsync((IReadOnlyList<Models.FactState>)emptyList);
        return mock.Object;
    }

    private static OllamaService CreateMockOllamaService()
    {
        var mock = new Mock<OllamaService>();
        mock.Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage
            {
                Role = "assistant",
                Content = "Mock response"
            });
        return mock.Object;
    }

    #endregion
}
