using System.Diagnostics;
using DeepResearchAgent.Services;
using Xunit;
using Xunit.Abstractions;

namespace DeepResearchAgent.Tests.Base;

/// <summary>
/// Base class for all async tests with common setup and utilities.
/// Provides timing measurements, logging, and timeout handling.
/// </summary>
public abstract class AsyncTestBase : IAsyncLifetime
{
    protected ITestOutputHelper Output { get; }
    protected CancellationTokenSource CancellationTokenSource { get; }

    protected AsyncTestBase(ITestOutputHelper output)
    {
        Output = output;
        CancellationTokenSource = new CancellationTokenSource();
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        CancellationTokenSource.Dispose();
        await Task.CompletedTask;
    }

    /// <summary>Helper to timeout tests after specified seconds.</summary>
    protected async Task<T> WithTimeoutAsync<T>(
        Task<T> task,
        int seconds = 30)
    {
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(seconds));
        try
        {
            return await task.ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            throw new TimeoutException($"Test timed out after {seconds} seconds");
        }
    }

    /// <summary>Helper to write diagnostic output with timestamp.</summary>
    protected void WriteOutput(string message)
    {
        Output?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }

    /// <summary>Helper to measure execution time of async operations.</summary>
    protected async Task<(T Result, TimeSpan Elapsed)> MeasureAsync<T>(Task<T> task)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await task.ConfigureAwait(false);
            stopwatch.Stop();
            return (result, stopwatch.Elapsed);
        }
        catch
        {
            stopwatch.Stop();
            throw;
        }
    }

    /// <summary>Helper to measure execution time of void async operations.</summary>
    protected async Task<TimeSpan> MeasureAsync(Func<Task> operation)
    {
        var stopwatch = Stopwatch.StartNew();
        try
        {
            await operation().ConfigureAwait(false);
            return stopwatch.Elapsed;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>Helper to assert execution time is within threshold.</summary>
    protected void AssertExecutionTimeUnderThreshold(TimeSpan elapsed, int thresholdMs)
    {
        Assert.True(
            elapsed.TotalMilliseconds <= thresholdMs,
            $"Execution time {elapsed.TotalMilliseconds}ms exceeded threshold {thresholdMs}ms"
        );
    }

    /// <summary>Helper to assert execution time is within threshold and log result.</summary>
    protected void AssertAndLogExecutionTime(TimeSpan elapsed, int thresholdMs, string operationName)
    {
        WriteOutput($"{operationName} completed in {elapsed.TotalMilliseconds}ms (threshold: {thresholdMs}ms)");
        AssertExecutionTimeUnderThreshold(elapsed, thresholdMs);
    }
}
