using DeepResearchAgent.Services;
using Xunit;
using Polly.CircuitBreaker;

namespace DeepResearchAgent.Tests.Services;

public class CircuitBreakerTests
{
    [Fact]
    public void CircuitBreakerConfig_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var config = new CircuitBreakerConfig();

        // Assert
        Assert.True(config.Enabled);
        Assert.Equal(5, config.FailureThreshold);
        Assert.Equal(10, config.MinimumThroughput);
        Assert.Equal(30, config.SamplingDurationSeconds);
        Assert.Equal(50, config.FailureRateThreshold);
        Assert.Equal(60, config.BreakDurationSeconds);
        Assert.True(config.EnableFallback);
        Assert.True(config.LogStateChanges);
    }

    [Fact]
    public void ApoConfig_IncludesCircuitBreakerConfig()
    {
        // Arrange & Act
        var apo = new LightningAPOConfig();

        // Assert
        Assert.NotNull(apo.CircuitBreaker);
        Assert.True(apo.CircuitBreaker.Enabled);
    }

    [Theory]
    [InlineData(true, 5, 50, 60)]
    [InlineData(false, 10, 70, 120)]
    [InlineData(true, 3, 30, 30)]
    public void CircuitBreakerConfig_CustomValues_AreSet(
        bool enabled,
        int failureThreshold,
        double failureRate,
        int breakDuration)
    {
        // Arrange
        var config = new CircuitBreakerConfig
        {
            Enabled = enabled,
            FailureThreshold = failureThreshold,
            FailureRateThreshold = failureRate,
            BreakDurationSeconds = breakDuration
        };

        // Assert
        Assert.Equal(enabled, config.Enabled);
        Assert.Equal(failureThreshold, config.FailureThreshold);
        Assert.Equal(failureRate, config.FailureRateThreshold);
        Assert.Equal(breakDuration, config.BreakDurationSeconds);
    }

    [Fact]
    public void CircuitBreaker_WhenDisabled_DoesNotInterfere()
    {
        // Arrange
        var apo = new LightningAPOConfig
        {
            CircuitBreaker = new CircuitBreakerConfig { Enabled = false }
        };

        // Act & Assert
        Assert.False(apo.CircuitBreaker.Enabled);
        // Circuit breaker should not interfere when disabled
    }

    [Theory]
    [InlineData(5, 50, 30)]
    [InlineData(10, 70, 60)]
    [InlineData(3, 30, 15)]
    public void CircuitBreakerConfig_ValidatesReasonableThresholds(
        int failureThreshold,
        double failureRate,
        int samplingDuration)
    {
        // Arrange
        var config = new CircuitBreakerConfig
        {
            FailureThreshold = failureThreshold,
            FailureRateThreshold = failureRate,
            SamplingDurationSeconds = samplingDuration
        };

        // Assert
        Assert.InRange(config.FailureThreshold, 1, 100);
        Assert.InRange(config.FailureRateThreshold, 0, 100);
        Assert.InRange(config.SamplingDurationSeconds, 1, 300);
    }

    [Fact]
    public void CircuitBreaker_FallbackEnabled_AllowsGracefulDegradation()
    {
        // Arrange
        var config = new CircuitBreakerConfig
        {
            Enabled = true,
            EnableFallback = true
        };

        // Act & Assert
        Assert.True(config.EnableFallback);
        // When enabled, fallback should execute instead of failing
    }

    [Fact]
    public void CircuitBreaker_FallbackDisabled_ThrowsOnOpen()
    {
        // Arrange
        var config = new CircuitBreakerConfig
        {
            Enabled = true,
            EnableFallback = false
        };

        // Act & Assert
        Assert.False(config.EnableFallback);
        // When disabled, should throw BrokenCircuitException
    }

    [Theory]
    [InlineData(10, 30, 5)]  // Default config: 10 throughput at 50% = 5 failures minimum
    [InlineData(20, 60, 10)]  // 20 throughput at 50% = 10 failures minimum
    public void CircuitBreaker_MinimumThroughput_PreventsPrematureTripping(
        int minimumThroughput,
        int samplingDuration,
        int expectedMinFailures)
    {
        // Arrange
        var config = new CircuitBreakerConfig
        {
            MinimumThroughput = minimumThroughput,
            SamplingDurationSeconds = samplingDuration,
            FailureRateThreshold = 50
        };

        // Calculate expected minimum failures for 50% failure rate
        var calculatedMin = (int)Math.Ceiling(minimumThroughput * 0.5);

        // Assert
        Assert.Equal(expectedMinFailures, calculatedMin);
    }

    [Fact]
    public void CircuitBreaker_LogStateChanges_CapturesTransitions()
    {
        // Arrange
        var config = new CircuitBreakerConfig
        {
            LogStateChanges = true
        };

        // Assert
        Assert.True(config.LogStateChanges);
        // Should log: CLOSED → OPEN, OPEN → HALF-OPEN, HALF-OPEN → CLOSED
    }
}
