using System.Text.Json.Serialization;

namespace DeepResearchAgent.Services;

/// <summary>
/// Configuration for Agent-Lightning APO (Automatic Performance Optimization).
/// Manages performance tuning, resource allocation, and optimization strategies.
/// </summary>
public class LightningAPOConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("optimizationStrategy")]
    public OptimizationStrategy Strategy { get; set; } = OptimizationStrategy.Balanced;

    [JsonPropertyName("resourceLimits")]
    public ResourceLimits ResourceLimits { get; set; } = new();

    [JsonPropertyName("performanceMetrics")]
    public PerformanceMetrics Metrics { get; set; } = new();

    [JsonPropertyName("autoScaling")]
    public AutoScalingConfig AutoScaling { get; set; } = new();
}

public enum OptimizationStrategy
{
    /// <summary>Prioritize speed over resource usage</summary>
    HighPerformance,
    
    /// <summary>Balanced approach between speed and resources</summary>
    Balanced,
    
    /// <summary>Minimize resource usage, accept slower execution</summary>
    LowResource,
    
    /// <summary>Optimize for cost efficiency</summary>
    CostOptimized
}

public class ResourceLimits
{
    [JsonPropertyName("maxCpuPercent")]
    public int MaxCpuPercent { get; set; } = 80;

    [JsonPropertyName("maxMemoryMb")]
    public int MaxMemoryMb { get; set; } = 2048;

    [JsonPropertyName("maxConcurrentTasks")]
    public int MaxConcurrentTasks { get; set; } = 10;

    [JsonPropertyName("taskTimeoutSeconds")]
    public int TaskTimeoutSeconds { get; set; } = 300;

    [JsonPropertyName("cacheSizeMb")]
    public int CacheSizeMb { get; set; } = 512;
}

public class PerformanceMetrics
{
    [JsonPropertyName("trackingEnabled")]
    public bool TrackingEnabled { get; set; } = true;

    [JsonPropertyName("metricsInterval")]
    public int MetricsIntervalSeconds { get; set; } = 10;

    [JsonPropertyName("performanceLoggingLevel")]
    public string PerformanceLoggingLevel { get; set; } = "Info";

    [JsonPropertyName("enableTracing")]
    public bool EnableTracing { get; set; } = true;

    [JsonPropertyName("enableProfiling")]
    public bool EnableProfiling { get; set; } = false;
}

public class AutoScalingConfig
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; } = true;

    [JsonPropertyName("minInstances")]
    public int MinInstances { get; set; } = 1;

    [JsonPropertyName("maxInstances")]
    public int MaxInstances { get; set; } = 5;

    [JsonPropertyName("scaleUpThreshold")]
    public int ScaleUpThresholdPercent { get; set; } = 70;

    [JsonPropertyName("scaleDownThreshold")]
    public int ScaleDownThresholdPercent { get; set; } = 30;

    [JsonPropertyName("cooldownSeconds")]
    public int CooldownSeconds { get; set; } = 60;
}