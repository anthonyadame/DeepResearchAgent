using DeepResearchAgent.Services;
using Xunit;
using Xunit.Abstractions;
using TaskStatus = DeepResearchAgent.Services.TaskStatus;

namespace DeepResearchAgent.Tests.Helpers;

/// <summary>
/// Domain-specific assertion extensions for Lightning services.
/// Provides fluent, readable assertions with clear failure messages.
/// </summary>
public static class LightningAssertions
{
    /// <summary>Assert that an agent registration is valid and complete.</summary>
    public static void ShouldBeValidAgentRegistration(this AgentRegistration? registration)
    {
        Assert.NotNull(registration);
        Assert.NotEmpty(registration.AgentId);
        Assert.NotEmpty(registration.AgentType);
        Assert.True(registration.IsActive, "Agent should be active");
        Assert.NotNull(registration.Capabilities);
        Assert.NotNull(registration.RegisteredAt);
    }

    /// <summary>Assert that a task result is properly formed and not in submitted state.</summary>
    public static void ShouldBeValidTaskResult(this AgentTaskResult? result)
    {
        Assert.NotNull(result);
        Assert.NotEmpty(result.TaskId);
        Assert.NotEqual(TaskStatus.Submitted, result.Status);
    }

    /// <summary>Assert that a task has progressed from submitted state.</summary>
    public static void ShouldHaveProgressed(this AgentTaskResult? result)
    {
        Assert.NotNull(result);
        Assert.NotEqual(TaskStatus.Submitted, result.Status);
    }

    /// <summary>Assert that verification result has high confidence above threshold.</summary>
    public static void ShouldHaveHighConfidence(
        this VerificationResult? result,
        double threshold = 0.80)
    {
        Assert.NotNull(result);
        Assert.True(
            result.Confidence >= threshold,
            $"Expected confidence >= {threshold:P}, got {result.Confidence:P}"
        );
    }

    /// <summary>Assert that verification result is valid with acceptable confidence.</summary>
    public static void ShouldBeValidVerification(this VerificationResult? result)
    {
        Assert.NotNull(result);
        Assert.True(result.IsValid, "Verification should be valid");
        Assert.InRange(result.Confidence, 0, 1);
        Assert.NotNull(result.VerifiedAt);
    }

    /// <summary>Assert that reasoning chain is valid with proper scoring.</summary>
    public static void ShouldBeValidReasoningChain(this ReasoningChainValidation? validation)
    {
        Assert.NotNull(validation);
        Assert.True(validation.IsValid, "Reasoning chain should be valid");
        Assert.InRange(validation.Score, 0, 1);
        Assert.Empty(validation.Errors);
    }

    /// <summary>Assert that fact check extracted reasonable number of facts.</summary>
    public static void ShouldHaveExtractedFacts(
        this FactCheckResult? result,
        int minimumFacts = 1)
    {
        Assert.NotNull(result);
        Assert.True(
            result.VerifiedCount >= minimumFacts,
            $"Expected at least {minimumFacts} facts, got {result.VerifiedCount}"
        );
        Assert.InRange(result.VerificationScore, 0, 1);
    }

    /// <summary>Assert that consistency check found no contradictions.</summary>
    public static void ShouldBeConsistent(this ConsistencyCheckResult? result)
    {
        Assert.NotNull(result);
        Assert.True(result.IsConsistent, "Statements should be consistent");
        Assert.InRange(result.Score, 0, 1);
        Assert.Empty(result.Contradictions);
    }

    /// <summary>Assert that server info indicates both APO and VERL are enabled.</summary>
    public static void ShouldHaveBothOptimizationsEnabled(this LightningServerInfo? info)
    {
        Assert.NotNull(info);
        Assert.True(info.ApoEnabled, "APO should be enabled");
        Assert.True(info.VerlEnabled, "VERL should be enabled");
    }
}

/// <summary>
/// Extensions for asserting performance characteristics.
/// </summary>
public static class PerformanceAssertions
{
    /// <summary>Assert that operation completed within specified milliseconds.</summary>
    public static void ShouldCompleteWithin(this TimeSpan elapsed, int thresholdMs)
    {
        Assert.True(
            elapsed.TotalMilliseconds <= thresholdMs,
            $"Expected completion within {thresholdMs}ms, took {elapsed.TotalMilliseconds}ms"
        );
    }

    /// <summary>Assert that operation completed faster than threshold with logging.</summary>
    public static void ShouldCompleteWithin(
        this TimeSpan elapsed,
        int thresholdMs,
        string operationName,
        ITestOutputHelper? output = null)
    {
        elapsed.ShouldCompleteWithin(thresholdMs);
        output?.WriteLine($"✓ {operationName} completed in {elapsed.TotalMilliseconds}ms (threshold: {thresholdMs}ms)");
    }

    /// <summary>Assert that throughput meets minimum requirements.</summary>
    public static void ShouldMeetThroughput(
        this int itemsProcessed,
        TimeSpan elapsed,
        double minimumItemsPerSecond)
    {
        var itemsPerSecond = itemsProcessed / elapsed.TotalSeconds;
        Assert.True(
            itemsPerSecond >= minimumItemsPerSecond,
            $"Expected {minimumItemsPerSecond} items/sec, achieved {itemsPerSecond:F2} items/sec"
        );
    }

    /// <summary>Assert that memory usage is within acceptable limits.</summary>
    public static void ShouldBeWithinMemoryLimit(this long bytesUsed, long limitBytes)
    {
        Assert.True(
            bytesUsed <= limitBytes,
            $"Memory usage {bytesUsed / 1024 / 1024}MB exceeded limit {limitBytes / 1024 / 1024}MB"
        );
    }
}

/// <summary>
/// Extensions for asserting collection characteristics.
/// </summary>
public static class CollectionAssertions
{
    /// <summary>Assert that collection has expected count with helpful message.</summary>
    public static void ShouldHaveCount<T>(this IEnumerable<T> collection, int expectedCount)
    {
        var count = collection.Count();
        Assert.True(
            count == expectedCount,
            $"Expected {expectedCount} items, found {count}"
        );
    }

    /// <summary>Assert that collection has at least minimum count.</summary>
    public static void ShouldHaveAtLeast<T>(this IEnumerable<T> collection, int minimumCount)
    {
        var count = collection.Count();
        Assert.True(
            count >= minimumCount,
            $"Expected at least {minimumCount} items, found {count}"
        );
    }

    /// <summary>Assert that all items in collection match predicate.</summary>
    public static void ShouldAllMatch<T>(
        this IEnumerable<T> collection,
        Func<T, bool> predicate,
        string description = "")
    {
        var allMatch = collection.All(predicate);
        Assert.True(allMatch, $"Not all items matched criteria: {description}");
    }
}
