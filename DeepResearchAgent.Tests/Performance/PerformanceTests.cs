using System.Diagnostics;
using DeepResearchAgent.Agents;
using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.WebSearch;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;
using Xunit.Abstractions;

namespace DeepResearchAgent.Tests.Performance;

/// <summary>
/// Performance tests for Phase 5 pipeline.
/// Measures execution time, throughput, and validates performance requirements.
/// </summary>
public class PerformanceTests
{
    private readonly ITestOutputHelper _output;
    private readonly MasterWorkflow _masterWorkflow;
    private readonly ResearcherAgent _researcherAgent;
    private readonly AnalystAgent _analystAgent;
    private readonly ReportAgent _reportAgent;
    private readonly StateTransitioner _transitioner;
    private readonly AgentErrorRecovery _errorRecovery;

    public PerformanceTests(ITestOutputHelper output)
    {
        _output = output;

        var mockLlmService = new Mock<OllamaService>(null);
        var mockToolService = new Mock<ToolInvocationService>(Mock.Of<IWebSearchProvider>(), null);
        var mockStateService = new Mock<ILightningStateService>();
        var mockSearchProvider = new Mock<IWebSearchProvider>();
        mockSearchProvider.Setup(x => x.ProviderName).Returns("test");

        SetupMocks(mockLlmService, mockToolService, mockStateService);

        _masterWorkflow = new MasterWorkflow(
            mockStateService.Object,
            Mock.Of<SupervisorWorkflow>(),
            mockLlmService.Object,
            mockSearchProvider.Object);

        _researcherAgent = new ResearcherAgent(mockLlmService.Object, mockToolService.Object);
        _analystAgent = new AnalystAgent(mockLlmService.Object, mockToolService.Object);
        _reportAgent = new ReportAgent(mockLlmService.Object, mockToolService.Object);
        _transitioner = new StateTransitioner();
        _errorRecovery = new AgentErrorRecovery();
    }

    #region Execution Time Tests

    [Fact]
    public async Task FullPipeline_CompletesWithinTimeLimit()
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var maxAllowedTime = TimeSpan.FromSeconds(10); // Adjust based on requirements

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            "Performance Test Topic",
            "Performance test brief"
        );

        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        _output.WriteLine($"Full pipeline execution time: {stopwatch.ElapsedMilliseconds}ms");
        Assert.True(stopwatch.Elapsed < maxAllowedTime, 
            $"Pipeline took {stopwatch.Elapsed.TotalSeconds}s, expected less than {maxAllowedTime.TotalSeconds}s");
    }

    [Fact]
    public async Task ResearchPhase_MeasureExecutionTime()
    {
        // Arrange
        var input = new ResearchInput
        {
            Topic = "Test Topic",
            ResearchBrief = "Test Brief",
            MaxIterations = 3,
            MinQualityThreshold = 7.0f
        };

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _researcherAgent.ExecuteAsync(input);

        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        _output.WriteLine($"Research phase execution time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Findings: {result.Findings?.Count ?? 0}");
        _output.WriteLine($"Average quality: {result.AverageQuality:F2}");
    }

    [Fact]
    public async Task AnalysisPhase_MeasureExecutionTime()
    {
        // Arrange
        var research = CreateSampleResearchOutput();
        var input = _transitioner.CreateAnalysisInput(research, "Test", "Test");

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _analystAgent.ExecuteAsync(input);

        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        _output.WriteLine($"Analysis phase execution time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Insights: {result.KeyInsights?.Count ?? 0}");
        _output.WriteLine($"Confidence: {result.ConfidenceScore:F2}");
    }

    [Fact]
    public async Task ReportPhase_MeasureExecutionTime()
    {
        // Arrange
        var research = CreateSampleResearchOutput();
        var analysis = CreateSampleAnalysisOutput();
        var input = _transitioner.CreateReportInput(research, analysis, "Test");

        var stopwatch = Stopwatch.StartNew();

        // Act
        var result = await _reportAgent.ExecuteAsync(input);

        stopwatch.Stop();

        // Assert
        Assert.NotNull(result);
        _output.WriteLine($"Report phase execution time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Sections: {result.Sections?.Count ?? 0}");
        _output.WriteLine($"Quality score: {result.QualityScore:F2}");
    }

    #endregion

    #region Throughput Tests

    [Theory]
    [InlineData(10)]
    [InlineData(25)]
    [InlineData(50)]
    public async Task SequentialExecution_MeasureThroughput(int iterations)
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;

        // Act
        for (int i = 0; i < iterations; i++)
        {
            try
            {
                await _masterWorkflow.ExecuteFullPipelineAsync(
                    _researcherAgent,
                    _analystAgent,
                    _reportAgent,
                    _transitioner,
                    _errorRecovery,
                    $"Topic {i}",
                    $"Brief {i}"
                );
                successCount++;
            }
            catch (Exception ex)
            {
                _output.WriteLine($"Iteration {i} failed: {ex.Message}");
            }
        }

        stopwatch.Stop();

        // Assert & Report
        var throughput = successCount / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Sequential execution: {iterations} iterations");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per iteration: {stopwatch.ElapsedMilliseconds / iterations}ms");
        _output.WriteLine($"Throughput: {throughput:F2} requests/second");
        _output.WriteLine($"Success rate: {(successCount * 100.0 / iterations):F1}%");

        Assert.True(successCount >= iterations * 0.95, "Success rate should be at least 95%");
    }

    [Theory]
    [InlineData(5)]
    [InlineData(10)]
    public async Task ConcurrentExecution_MeasureThroughput(int concurrencyLevel)
    {
        // Arrange
        var stopwatch = Stopwatch.StartNew();
        var successCount = 0;

        // Act
        var tasks = Enumerable.Range(0, concurrencyLevel)
            .Select(async i =>
            {
                try
                {
                    await _masterWorkflow.ExecuteFullPipelineAsync(
                        _researcherAgent,
                        _analystAgent,
                        _reportAgent,
                        _transitioner,
                        _errorRecovery,
                        $"Concurrent Topic {i}",
                        $"Concurrent Brief {i}"
                    );
                    Interlocked.Increment(ref successCount);
                    return true;
                }
                catch
                {
                    return false;
                }
            })
            .ToList();

        await Task.WhenAll(tasks);
        stopwatch.Stop();

        // Assert & Report
        var throughput = successCount / stopwatch.Elapsed.TotalSeconds;
        _output.WriteLine($"Concurrent execution: {concurrencyLevel} concurrent requests");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Throughput: {throughput:F2} requests/second");
        _output.WriteLine($"Success rate: {(successCount * 100.0 / concurrencyLevel):F1}%");

        Assert.True(successCount >= concurrencyLevel * 0.95, "Success rate should be at least 95%");
    }

    #endregion

    #region State Management Performance

    [Fact]
    public void StateTransition_Performance()
    {
        // Arrange
        var research = CreateSampleResearchOutput();
        var iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var _ = _transitioner.CreateAnalysisInput(research, "Test", "Test");
        }

        stopwatch.Stop();

        // Assert & Report
        var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;
        _output.WriteLine($"State transitions: {iterations} iterations");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per transition: {avgTime:F3}ms");

        Assert.True(avgTime < 1.0, "State transition should take less than 1ms on average");
    }

    [Fact]
    public void Validation_Performance()
    {
        // Arrange
        var research = CreateSampleResearchOutput();
        var iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var _ = _transitioner.ValidateResearchOutput(research);
        }

        stopwatch.Stop();

        // Assert & Report
        var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;
        _output.WriteLine($"Validations: {iterations} iterations");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per validation: {avgTime:F3}ms");

        Assert.True(avgTime < 1.0, "Validation should take less than 1ms on average");
    }

    [Fact]
    public void ErrorRecovery_RepairPerformance()
    {
        // Arrange
        var research = CreateSampleResearchOutput();
        var iterations = 1000;
        var stopwatch = Stopwatch.StartNew();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            var _ = _errorRecovery.ValidateAndRepairResearchOutput(research, "Test");
        }

        stopwatch.Stop();

        // Assert & Report
        var avgTime = stopwatch.ElapsedMilliseconds / (double)iterations;
        _output.WriteLine($"Repair operations: {iterations} iterations");
        _output.WriteLine($"Total time: {stopwatch.ElapsedMilliseconds}ms");
        _output.WriteLine($"Average time per repair: {avgTime:F3}ms");

        Assert.True(avgTime < 1.0, "Repair should take less than 1ms on average");
    }

    #endregion

    #region Memory and Resource Tests

    [Fact]
    public async Task FullPipeline_MemoryUsage()
    {
        // Arrange
        GC.Collect();
        GC.WaitForPendingFinalizers();
        GC.Collect();

        var beforeMemory = GC.GetTotalMemory(false);

        // Act
        var result = await _masterWorkflow.ExecuteFullPipelineAsync(
            _researcherAgent,
            _analystAgent,
            _reportAgent,
            _transitioner,
            _errorRecovery,
            "Memory Test Topic",
            "Memory test brief"
        );

        var afterMemory = GC.GetTotalMemory(false);
        var memoryUsed = afterMemory - beforeMemory;

        // Assert & Report
        Assert.NotNull(result);
        _output.WriteLine($"Memory before: {beforeMemory / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"Memory after: {afterMemory / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"Memory used: {memoryUsed / 1024.0 / 1024.0:F2} MB");

        // Memory usage should be reasonable (adjust threshold as needed)
        Assert.True(memoryUsed < 100 * 1024 * 1024, "Memory usage should be less than 100 MB");
    }

    [Fact]
    public async Task MultipleExecutions_NoMemoryLeak()
    {
        // Arrange
        var iterations = 10;
        var memorySnapshots = new List<long>();

        // Act
        for (int i = 0; i < iterations; i++)
        {
            await _masterWorkflow.ExecuteFullPipelineAsync(
                _researcherAgent,
                _analystAgent,
                _reportAgent,
                _transitioner,
                _errorRecovery,
                $"Topic {i}",
                $"Brief {i}"
            );

            // Force GC and measure
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();

            memorySnapshots.Add(GC.GetTotalMemory(false));
        }

        // Assert & Report
        _output.WriteLine("Memory snapshots:");
        for (int i = 0; i < memorySnapshots.Count; i++)
        {
            _output.WriteLine($"  Iteration {i + 1}: {memorySnapshots[i] / 1024.0 / 1024.0:F2} MB");
        }

        // Check that memory doesn't grow unbounded
        var firstHalf = memorySnapshots.Take(iterations / 2).Average();
        var secondHalf = memorySnapshots.Skip(iterations / 2).Average();
        var growthRate = (secondHalf - firstHalf) / firstHalf;

        _output.WriteLine($"First half average: {firstHalf / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"Second half average: {secondHalf / 1024.0 / 1024.0:F2} MB");
        _output.WriteLine($"Growth rate: {growthRate * 100:F1}%");

        Assert.True(growthRate < 0.5, "Memory growth should be less than 50% between first and second half");
    }

    #endregion

    #region Helper Methods

    private static void SetupMocks(
        Mock<OllamaService> mockLlm,
        Mock<ToolInvocationService> mockTool,
        Mock<ILightningStateService> mockState)
    {
        mockLlm
            .Setup(s => s.InvokeAsync(
                It.IsAny<List<OllamaChatMessage>>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new OllamaChatMessage { Role = "assistant", Content = "8.5" });

        mockTool
            .Setup(s => s.InvokeToolAsync("websearch", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<WebSearchResult> { new() { Title = "Test", Content = "Test" } });

        mockTool
            .Setup(s => s.InvokeToolAsync("extractfacts", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FactExtractionResult
            {
                Facts = new List<ExtractedFact> { new() { Statement = "Test", Confidence = 0.9f } }
            });

        mockTool
            .Setup(s => s.InvokeToolAsync("evaluatequality", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QualityEvaluationResult 
            { 
                OverallScore = 8.5f, 
                DimensionScores = new Dictionary<string, DimensionScore>(),
                Summary = "Good quality"
            });

        mockTool
            .Setup(s => s.InvokeToolAsync("formatreport", It.IsAny<Dictionary<string, object>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync("Formatted report content");

        mockState
            .Setup(s => s.SetResearchStateAsync(
                It.IsAny<string>(),
                It.IsAny<ResearchStateModel>(),
                It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        mockState
            .Setup(s => s.GetResearchStateAsync(
                It.IsAny<string>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new ResearchStateModel
            {
                ResearchId = Guid.NewGuid().ToString(),
                Query = "Test",
                Status = ResearchStatus.InProgress
            });
    }

    private static ResearchOutput CreateSampleResearchOutput()
    {
        return new ResearchOutput
        {
            Findings = new List<FactExtractionResult>
            {
                new()
                {
                    Facts = new List<ExtractedFact>
                    {
                        new() { Statement = "Test fact 1", Confidence = 0.9f, Source = "test", Category = "test" },
                        new() { Statement = "Test fact 2", Confidence = 0.8f, Source = "test", Category = "test" }
                    }
                }
            },
            AverageQuality = 8.0f,
            IterationsUsed = 3
        };
    }

    private static AnalysisOutput CreateSampleAnalysisOutput()
    {
        return new AnalysisOutput
        {
            SynthesisNarrative = "Test analysis narrative",
            KeyInsights = new List<KeyInsight>
            {
                new() { Statement = "Test insight", Importance = 0.9f, SourceFacts = new List<string> { "fact1" } }
            },
            Contradictions = new List<Contradiction>(),
            ConfidenceScore = 0.85f,
            ThemesIdentified = new List<string> { "theme1", "theme2" }
        };
    }

    #endregion
}
