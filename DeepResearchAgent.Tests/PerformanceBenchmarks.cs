using DeepResearchAgent.Models;
using DeepResearchAgent.Workflows;
using Xunit;

namespace DeepResearchAgent.Tests;

/// <summary>
/// Performance benchmarks and timing tests.
/// Tests execution speed, throughput, and resource usage.
/// </summary>
public class PerformanceBenchmarks
{
    #region Researcher Performance Tests

    [Fact]
    public async Task Researcher_CompletesQuickly()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var facts = await researcher.ResearchAsync("Machine learning");
        sw.Stop();

        // Assert
        Assert.NotNull(facts);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30),
            $"Research completed in {sw.Elapsed.TotalSeconds:F2}s (target: <30s)");
    }

    [Fact]
    public async Task Researcher_HandlesMultipleQueries()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();
        var topics = new[] { "AI", "ML", "NLP" };

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var allFacts = new List<IReadOnlyList<FactState>>();
        foreach (var topic in topics)
        {
            var facts = await researcher.ResearchAsync(topic);
            allFacts.Add(facts);
        }
        sw.Stop();

        // Assert
        Assert.Equal(3, allFacts.Count);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(90),
            $"3 queries completed in {sw.Elapsed.TotalSeconds:F2}s (target: <90s)");
    }

    [Fact]
    public async Task Researcher_ParallelQueries_Performance()
    {
        // Arrange
        var topics = new[] { "Topic1", "Topic2", "Topic3" };
        var tasks = new List<Task<IReadOnlyList<FactState>>>();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        foreach (var topic in topics)
        {
            var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();
            tasks.Add(researcher.ResearchAsync(topic));
        }
        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        Assert.Equal(3, tasks.Count);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(60),
            $"Parallel queries completed in {sw.Elapsed.TotalSeconds:F2}s (target: <60s)");
    }

    #endregion

    #region Supervisor Performance Tests

    [Fact]
    public async Task Supervisor_OneIterationPerformance()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await supervisor.SuperviseAsync("Topic", "Draft", maxIterations: 1);
        sw.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30),
            $"1 iteration completed in {sw.Elapsed.TotalSeconds:F2}s (target: <30s)");
    }

    [Fact]
    public async Task Supervisor_ThreeIterationsPerformance()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await supervisor.SuperviseAsync("Topic", "Draft", maxIterations: 3);
        sw.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(90),
            $"3 iterations completed in {sw.Elapsed.TotalSeconds:F2}s (target: <90s)");
    }

    [Fact]
    public async Task Supervisor_MaxIterationsPerformance()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await supervisor.SuperviseAsync("Topic", "Draft", maxIterations: 5);
        sw.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(180),
            $"5 iterations completed in {sw.Elapsed.TotalSeconds:F2}s (target: <180s)");
    }

    #endregion

    #region Master Workflow Performance Tests

    [Fact]
    public async Task Master_FullPipelinePerformance()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState("Research topic");

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await master.ExecuteAsync(input);
        sw.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.NotEmpty(result.FinalReport);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(120),
            $"Full pipeline completed in {sw.Elapsed.TotalSeconds:F2}s (target: <120s)");
    }

    [Fact]
    public async Task Master_ComplexQueryPerformance()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var complexQuery = "Provide comprehensive analysis of renewable energy";
        var input = TestFixtures.CreateTestAgentState(complexQuery);

        // Act
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = await master.ExecuteAsync(input);
        sw.Stop();

        // Assert
        Assert.NotNull(result);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(180),
            $"Complex query completed in {sw.Elapsed.TotalSeconds:F2}s (target: <180s)");
    }

    #endregion

    #region Throughput Tests

    [Fact]
    public async Task Researcher_ThroughputTest()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();
        var queryCount = 5;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < queryCount; i++)
        {
            var facts = await researcher.ResearchAsync($"Topic {i}");
            Assert.NotNull(facts);
        }
        sw.Stop();

        // Assert
        var qps = queryCount / sw.Elapsed.TotalSeconds;
        Assert.True(qps > 0.05, // At least 1 per 20 seconds
            $"Throughput: {qps:F3} queries/sec (target: >0.05)");
    }

    [Fact]
    public async Task Master_ThroughputTest()
    {
        // Arrange
        var queryCount = 3;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        for (int i = 0; i < queryCount; i++)
        {
            var (master, _) = TestFixtures.CreateMockMasterWorkflow();
            var input = TestFixtures.CreateTestAgentState($"Query {i}");
            var result = await master.ExecuteAsync(input);
            Assert.NotNull(result);
        }
        sw.Stop();

        // Assert
        var qps = queryCount / sw.Elapsed.TotalSeconds;
        Assert.True(qps > 0.01, // At least 1 per 100 seconds
            $"Throughput: {qps:F4} queries/sec (target: >0.01)");
    }

    #endregion

    #region Fact Extraction Performance Tests

    [Fact]
    public async Task Researcher_FactExtractionRate()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var facts = await researcher.ResearchAsync("AI");
        sw.Stop();

        // Assert
        var factRate = facts.Count / sw.Elapsed.TotalSeconds;
        Assert.True(factRate > 0.1, // At least 0.1 facts/sec
            $"Fact extraction rate: {factRate:F2} facts/sec (target: >0.1)");
    }

    [Fact]
    public async Task Supervisor_FactAccumulation()
    {
        // Arrange
        var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();

        // Act
        var result = await supervisor.SuperviseAsync("Topic", "Draft", maxIterations: 2);

        // Assert
        Assert.NotNull(result);
        Assert.True(result.Length > 0);
    }

    #endregion

    #region Memory Performance Tests

    [Fact]
    public async Task Researcher_MemoryUsage()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var facts = await researcher.ResearchAsync("Topic");

        // Assert
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB
        Assert.True(memoryUsed < 500, // Less than 500MB
            $"Memory used: {memoryUsed:F2}MB (target: <500MB)");
    }

    [Fact]
    public async Task Master_MemoryUsage()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var input = TestFixtures.CreateTestAgentState();
        var initialMemory = GC.GetTotalMemory(true);

        // Act
        var result = await master.ExecuteAsync(input);

        // Assert
        var finalMemory = GC.GetTotalMemory(false);
        var memoryUsed = (finalMemory - initialMemory) / 1024.0 / 1024.0; // MB
        Assert.True(memoryUsed < 1000, // Less than 1GB
            $"Memory used: {memoryUsed:F2}MB (target: <1000MB)");
    }

    #endregion

    #region Streaming Performance Tests

    [Fact]
    public async Task Researcher_StreamingPerformance()
    {
        // Arrange
        var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var updateCount = 0;

        // Act
        await foreach (var update in researcher.StreamResearchAsync("Topic"))
        {
            updateCount++;
        }
        sw.Stop();

        // Assert
        Assert.True(updateCount > 0);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(30),
            $"Streaming completed in {sw.Elapsed.TotalSeconds:F2}s");
    }

    [Fact]
    public async Task Master_StreamingPerformance()
    {
        // Arrange
        var (master, _) = TestFixtures.CreateMockMasterWorkflow();
        var query = "Research topic";
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var updateCount = 0;

        // Act
        await foreach (var update in master.StreamAsync(query))
        {
            updateCount++;
        }
        sw.Stop();

        // Assert
        Assert.True(updateCount >= 5, "Should have multiple updates");
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(120),
            $"Streaming completed in {sw.Elapsed.TotalSeconds:F2}s");
    }

    #endregion

    #region Scaling Tests

    [Fact]
    public async Task MultipleResearchers_ScalingTest()
    {
        // Arrange
        var researcherCount = 5;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < researcherCount; i++)
        {
            var (researcher, _, _) = TestFixtures.CreateMockResearcherWorkflow();
            tasks.Add(researcher.ResearchAsync($"Topic{i}"));
        }
        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(150),
            $"5 researchers completed in {sw.Elapsed.TotalSeconds:F2}s");
    }

    [Fact]
    public async Task MultipleSupervisors_ScalingTest()
    {
        // Arrange
        var supervisorCount = 3;
        var sw = System.Diagnostics.Stopwatch.StartNew();

        // Act
        var tasks = new List<Task>();
        for (int i = 0; i < supervisorCount; i++)
        {
            var (supervisor, _, _) = TestFixtures.CreateMockSupervisorWorkflow();
            tasks.Add(supervisor.SuperviseAsync($"Topic{i}", "Draft", maxIterations: 1));
        }
        await Task.WhenAll(tasks);
        sw.Stop();

        // Assert
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(120),
            $"3 supervisors completed in {sw.Elapsed.TotalSeconds:F2}s");
    }

    #endregion

    #region Benchmarking Summary

    [Fact]
    public void PrintPerformanceTargets()
    {
        var summary = @"
=== PERFORMANCE TARGETS ===

Research (single):         <30s
Research (3 parallel):     <60s  
Supervision (1 iter):      <30s
Supervision (3 iters):     <90s
Master (full pipeline):    <120s
Master (complex query):    <180s

Throughput:
  - Research:  >0.05 queries/sec
  - Master:    >0.01 queries/sec

Fact Extraction:           >0.1 facts/sec

Memory:
  - Research:  <500MB
  - Master:    <1GB

Scaling:
  - 5 researchers:         <150s
  - 3 supervisors:         <120s
";
        Assert.True(true); // Just for documentation
    }

    #endregion
}
