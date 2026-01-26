using DeepResearchAgent.Services;
using Xunit;

namespace DeepResearchAgent.Tests.Services;

public class ApoIntegrationTests
{
    [Fact]
    public void ApoConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var apo = new LightningAPOConfig();

        // Assert
        Assert.True(apo.Enabled);
        Assert.Equal(OptimizationStrategy.Balanced, apo.Strategy);
        Assert.Equal(10, apo.ResourceLimits.MaxConcurrentTasks);
        Assert.Equal(300, apo.ResourceLimits.TaskTimeoutSeconds);
        Assert.True(apo.Metrics.TrackingEnabled);
    }

    [Theory]
    [InlineData(OptimizationStrategy.HighPerformance, false, true)]
    [InlineData(OptimizationStrategy.Balanced, true, true)]
    [InlineData(OptimizationStrategy.LowResource, true, true)]
    [InlineData(OptimizationStrategy.CostOptimized, true, true)]
    public void ApoExtensions_ShouldVerify_RespectsStrategy(
        OptimizationStrategy strategy, 
        bool taskRequiresVerification,
        bool expectedResult)
    {
        // Arrange
        var apo = new LightningAPOConfig { Strategy = strategy };

        // Act
        var result = apo.ShouldVerify(taskRequiresVerification);

        // Assert
        if (strategy == OptimizationStrategy.HighPerformance)
        {
            Assert.False(result); // HighPerformance always skips verification
        }
        else
        {
            Assert.Equal(expectedResult, result);
        }
    }

    [Theory]
    [InlineData(OptimizationStrategy.HighPerformance, 10)]
    [InlineData(OptimizationStrategy.Balanced, 5)]
    [InlineData(OptimizationStrategy.LowResource, 3)]
    [InlineData(OptimizationStrategy.CostOptimized, 4)]
    public void ApoExtensions_GetTaskPriority_ReturnsCorrectValue(
        OptimizationStrategy strategy,
        int expectedPriority)
    {
        // Arrange
        var apo = new LightningAPOConfig { Strategy = strategy };

        // Act
        var priority = apo.GetTaskPriority();

        // Assert
        Assert.Equal(expectedPriority, priority);
    }

    [Fact]
    public void ApoExtensions_GetTaskPriority_CustomPriorityOverridesDefault()
    {
        // Arrange
        var apo = new LightningAPOConfig { Strategy = OptimizationStrategy.Balanced };

        // Act
        var priority = apo.GetTaskPriority(customPriority: 8);

        // Assert
        Assert.Equal(8, priority);
    }

    [Fact]
    public void ApoExecutionOptions_MergeWith_OverridesStrategy()
    {
        // Arrange
        var baseConfig = new LightningAPOConfig 
        { 
            Strategy = OptimizationStrategy.Balanced 
        };
        var options = new ApoExecutionOptions 
        { 
            StrategyOverride = OptimizationStrategy.HighPerformance 
        };

        // Act
        var merged = options.MergeWith(baseConfig);

        // Assert
        Assert.Equal(OptimizationStrategy.HighPerformance, merged.Strategy);
    }

    [Fact]
    public void ApoExecutionOptions_MergeWith_DisableApo_DisablesApo()
    {
        // Arrange
        var baseConfig = new LightningAPOConfig { Enabled = true };
        var options = new ApoExecutionOptions { DisableApo = true };

        // Act
        var merged = options.MergeWith(baseConfig);

        // Assert
        Assert.False(merged.Enabled);
    }

    [Fact]
    public void ApoExecutionOptions_MergeWith_CustomTimeout_OverridesDefault()
    {
        // Arrange
        var baseConfig = new LightningAPOConfig();
        var options = new ApoExecutionOptions 
        { 
            Timeout = TimeSpan.FromSeconds(120) 
        };

        // Act
        var merged = options.MergeWith(baseConfig);

        // Assert
        Assert.Equal(120, merged.ResourceLimits.TaskTimeoutSeconds);
    }

    [Fact]
    public void ApoExtensions_CreateConcurrencyGate_CreatesCorrectSemaphore()
    {
        // Arrange
        var apo = new LightningAPOConfig();
        apo.ResourceLimits.MaxConcurrentTasks = 5;

        // Act
        using var gate = AgentLightningServiceExtensions.CreateConcurrencyGate(apo);

        // Assert
        Assert.NotNull(gate);
        Assert.Equal(5, gate.CurrentCount);
    }

    [Fact]
    public void ApoExtensions_CreateRetryPolicy_CreatesPolicy()
    {
        // Arrange
        var apo = new LightningAPOConfig 
        { 
            Strategy = OptimizationStrategy.Balanced 
        };

        // Act
        var policy = AgentLightningServiceExtensions.CreateRetryPolicy(apo);

        // Assert
        Assert.NotNull(policy);
    }
}
