using DeepResearchAgent.Workflows;
using DeepResearchAgent.Workflows.Abstractions;
using DeepResearchAgent.Workflows.Adapters;
using DeepResearchAgent.Workflows.Extensions;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics;
using Xunit;

namespace DeepResearchAgent.Tests.Workflows.Performance;

/// <summary>
/// Performance baseline tests for Phase 2 adapter layer.
/// Compares Phase 1 (direct) vs Phase 2 (adapter) execution times.
/// </summary>
public class Phase2PerformanceTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IWorkflowOrchestrator _orchestrator;
    private readonly OrchestratorAdapter _adapter;

    public Phase2PerformanceTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
        services.AddSingleton<MasterWorkflowDefinition>();
        services.AddSingleton<SupervisorWorkflowDefinition>();
        services.AddSingleton<ResearcherWorkflowDefinition>();
        services.AddWorkflowAdaptersFromOrchestrator();
        
        _serviceProvider = services.BuildServiceProvider();
        _orchestrator = _serviceProvider.GetRequiredService<IWorkflowOrchestrator>();
        _adapter = _serviceProvider.GetRequiredService<OrchestratorAdapter>();
    }

    [Fact]
    public async Task Phase2_ContextConversion_IsEfficient()
    {
        // Arrange
        var context = new WorkflowContext();
        context.SetState("Query", "test query");
        context.Metadata["key"] = "value";
        
        var stopwatch = Stopwatch.StartNew();
        const int iterations = 1000;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var state = context.ToAgentState();
            var restored = state.FromAgentState();
        }
        stopwatch.Stop();

        // Assert
        var msPerOp = stopwatch.ElapsedMilliseconds / (double)iterations;
        Assert.True(msPerOp < 1.0, $"Conversion too slow: {msPerOp}ms per operation");
    }

    [Fact]
    public async Task Phase2_Execution_WithinAcceptableOverhead()
    {
        // Arrange
        var context = WorkflowExtensions.CreateMasterWorkflowContext("Test");
        var agentState = context.ToAgentState();

        const int iterations = 10;
        var phase1Times = new List<long>();
        var phase2Times = new List<long>();

        // Act - Measure Phase 1
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await _orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);
            stopwatch.Stop();
            phase1Times.Add(stopwatch.ElapsedMilliseconds);
        }

        // Act - Measure Phase 2
        for (int i = 0; i < iterations; i++)
        {
            var stopwatch = Stopwatch.StartNew();
            await _adapter.ExecuteAsync("MasterWorkflow", agentState);
            stopwatch.Stop();
            phase2Times.Add(stopwatch.ElapsedMilliseconds);
        }

        // Assert - Phase 2 should not be significantly slower
        var phase1Avg = phase1Times.Average();
        var phase2Avg = phase2Times.Average();
        var overhead = (phase2Avg - phase1Avg) / phase1Avg * 100;

        // Allow up to 20% overhead from adapter layer
        Assert.True(overhead < 20.0, $"Phase 2 overhead too high: {overhead:F2}%");
    }

    [Fact]
    public void Phase2_Registration_IsQuick()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        const int iterations = 100;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var services = new ServiceCollection();
            services.AddLogging();
            services.AddSingleton<IWorkflowOrchestrator, WorkflowOrchestrator>();
            services.AddWorkflowAdaptersFromOrchestrator();
            var provider = services.BuildServiceProvider();
        }
        stopwatch.Stop();

        // Assert
        var msPerReg = stopwatch.ElapsedMilliseconds / (double)iterations;
        Assert.True(msPerReg < 100.0, $"Registration too slow: {msPerReg}ms per registration");
    }

    [Fact]
    public async Task Phase2_MemoryUsage_IsReasonable()
    {
        // Arrange
        var beforeGC = GC.GetTotalMemory(true);
        var contexts = new List<WorkflowContext>();

        // Act - Create many contexts
        for (int i = 0; i < 100; i++)
        {
            var context = new WorkflowContext();
            context.SetState("Key", $"Value{i}");
            contexts.Add(context);
        }

        var afterCreation = GC.GetTotalMemory(false);
        var memoryUsed = (afterCreation - beforeGC) / 1024 / 1024; // Convert to MB

        // Assert - Should use less than 10MB for 100 contexts
        Assert.True(memoryUsed < 10, $"Memory usage too high: {memoryUsed}MB");

        // Cleanup
        contexts.Clear();
        GC.Collect();
    }

    [Fact]
    public async Task Phase2_Streaming_Performance_IsAcceptable()
    {
        // Arrange
        var context = WorkflowExtensions.CreateMasterWorkflowContext("Test");
        var stopwatch = Stopwatch.StartNew();

        // Act
        int updateCount = 0;
        await foreach (var update in _orchestrator.StreamWorkflowAsync("MasterWorkflow", context))
        {
            updateCount++;
        }
        stopwatch.Stop();

        // Assert
        Assert.True(updateCount > 0);
        Assert.True(stopwatch.ElapsedMilliseconds < 5000, "Streaming too slow");
    }

    [Fact]
    public void Phase2_Adapter_Creation_IsCheap()
    {
        // Arrange
        var definition = _orchestrator.GetWorkflow("MasterWorkflow");
        var stopwatch = Stopwatch.StartNew();
        const int iterations = 10000;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var adapter = definition.AsAdapted();
        }
        stopwatch.Stop();

        // Assert
        var usPerOp = stopwatch.ElapsedTicks / (double)iterations / 10;
        Assert.True(usPerOp < 100, $"Adapter creation too slow: {usPerOp}μs per creation");
    }

    [Fact]
    public async Task Phase2_Concurrent_Performance_Scales()
    {
        // Arrange
        var context = WorkflowExtensions.CreateMasterWorkflowContext("Test");
        const int concurrency = 5;
        var stopwatch = Stopwatch.StartNew();

        // Act - Execute concurrently
        var tasks = Enumerable.Range(0, concurrency)
            .Select(_ => _orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context))
            .ToList();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert
        var msPerExecution = stopwatch.ElapsedMilliseconds / (double)concurrency;
        Assert.True(msPerExecution < 5000, $"Concurrent execution too slow: {msPerExecution}ms per");
    }
}
