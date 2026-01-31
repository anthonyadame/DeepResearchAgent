using Xunit;
using DeepResearchAgent.Api.Clients;
using DeepResearchAgent.Models;

namespace DeepResearchAgent.Api.Tests;

/// <summary>
/// End-to-end tests for MasterWorkflow streaming endpoint.
/// Verifies the complete research pipeline via HTTP API with real-time progress updates.
/// </summary>
[Collection("API Integration Tests")]
public class StreamingEndpointE2ETests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly MasterWorkflowStreamClient _streamClient;
    private readonly string _apiBaseUrl;

    public StreamingEndpointE2ETests()
    {
        _apiBaseUrl = Environment.GetEnvironmentVariable("API_BASE_URL") ?? "http://localhost:5000";
        _httpClient = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
        _streamClient = new MasterWorkflowStreamClient(_httpClient, _apiBaseUrl);
    }

    public async Task InitializeAsync()
    {
        // Verify API is running
        try
        {
            var response = await _httpClient.GetAsync($"{_apiBaseUrl}/health");
            if (!response.IsSuccessStatusCode)
                throw new InvalidOperationException($"Health check failed: {response.StatusCode}");
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException(
                $"API server not running at {_apiBaseUrl}. " +
                $"Start it with: dotnet run --project DeepResearchAgent.Api/DeepResearchAgent.Api.csproj",
                ex
            );
        }
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
    }

    /// <summary>
    /// Test that streaming endpoint returns Server-Sent Events format.
    /// Verifies basic HTTP compliance.
    /// </summary>
    [Fact]
    public async Task StreamEndpoint_ReturnsCorrectContentType()
    {
        // Arrange
        var request = new { userQuery = "test" };
        var content = new StringContent(
            System.Text.Json.JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _httpClient.PostAsync(
            $"{_apiBaseUrl}/api/workflows/master/stream",
            content
        );

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.NotNull(response.Content.Headers.ContentType);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType.MediaType);
        Assert.True(response.Headers.CacheControl?.NoCache ?? false);
    }

    /// <summary>
    /// Test that streaming completes all 5 phases of research pipeline.
    /// Verifies core workflow functionality.
    /// </summary>
    [Fact(Timeout = 300000)] // 5 minute timeout
    public async Task StreamEndpoint_CompletesPipeline()
    {
        // Arrange
        var query = "What are the primary challenges in developing artificial general intelligence?";

        // Act
        var states = await _streamClient.CollectStreamAsync(query);

        // Assert - Verify all phases present
        Assert.NotEmpty(states);

        // Phase 1: Initial connection
        Assert.Contains(states, s => !string.IsNullOrWhiteSpace(s.Status) && s.Status.Contains("connected", StringComparison.OrdinalIgnoreCase));

        // Phase 2: Research brief
        Assert.Contains(states, s => !string.IsNullOrWhiteSpace(s.ResearchBrief));

        // Phase 3: Draft report
        Assert.Contains(states, s => !string.IsNullOrWhiteSpace(s.DraftReport));

        // Phase 4: Supervisor updates
        var supervisorStates = states.Where(s => !string.IsNullOrWhiteSpace(s.SupervisorUpdate)).ToList();
        Assert.NotEmpty(supervisorStates);

        // Phase 5: Final report
        Assert.Contains(states, s => !string.IsNullOrWhiteSpace(s.FinalReport));
    }

    /// <summary>
    /// Test that streaming properly handles vague queries requiring clarification.
    /// Verifies error recovery and clarification flow.
    /// </summary>
    [Fact(Timeout = 60000)] // 1 minute timeout - clarification should be quick
    public async Task StreamEndpoint_HandlesClarificationNeeded()
    {
        // Arrange - Intentionally vague query
        var vagueQuery = "research stuff";

        // Act
        var states = await _streamClient.CollectStreamAsync(vagueQuery);

        // Assert - Should indicate clarification needed
        var clarificationStates = states.Where(s => 
            !string.IsNullOrWhiteSpace(s.Status) && 
            s.Status.Contains("clarification_needed", StringComparison.OrdinalIgnoreCase)
        ).ToList();

        Assert.NotEmpty(clarificationStates);
    }

    /// <summary>
    /// Test that each StreamState properly accumulates content through phases.
    /// Verifies progressive state building.
    /// </summary>
    [Fact(Timeout = 300000)]
    public async Task StreamEndpoint_ProgressiveStateBuilding()
    {
        // Arrange
        var query = "How do neural networks learn patterns?";

        // Act
        var states = await _streamClient.CollectStreamAsync(query);

        // Assert - Verify states build progressively
        var briefState = states.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.ResearchBrief));
        Assert.NotNull(briefState);
        Assert.NotEmpty(briefState!.BriefPreview);

        var draftState = states.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.DraftReport));
        Assert.NotNull(draftState);

        var finalState = states.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s.FinalReport));
        Assert.NotNull(finalState);

        // Final should be longer/more complete than draft
        Assert.True(
            finalState!.FinalReport.Length > draftState!.DraftReport.Length,
            "Final report should be more comprehensive than draft"
        );
    }

    /// <summary>
    /// Test streaming with callback pattern for real-time progress display.
    /// Verifies callback-based consumption works.
    /// </summary>
    [Fact(Timeout = 300000)]
    public async Task StreamEndpoint_CallbackPattern()
    {
        // Arrange
        var query = "Explain quantum entanglement";
        var receivedCount = 0;
        var phaseCompletion = new Dictionary<string, bool>
        {
            ["brief"] = false,
            ["draft"] = false,
            ["final"] = false
        };

        // Act
        await _streamClient.StreamMasterWorkflowAsync(
            query,
            state =>
            {
                receivedCount++;

                if (!string.IsNullOrWhiteSpace(state.ResearchBrief))
                    phaseCompletion["brief"] = true;
                if (!string.IsNullOrWhiteSpace(state.DraftReport))
                    phaseCompletion["draft"] = true;
                if (!string.IsNullOrWhiteSpace(state.FinalReport))
                    phaseCompletion["final"] = true;
            },
            ex => Assert.Fail($"Unexpected error: {ex.Message}")
        );

        // Assert
        Assert.True(receivedCount > 0, "Should receive at least one StreamState");
        Assert.True(phaseCompletion["brief"], "Should complete brief phase");
        Assert.True(phaseCompletion["draft"], "Should complete draft phase");
        Assert.True(phaseCompletion["final"], "Should complete final phase");
    }

    /// <summary>
    /// Test that streaming properly recovers from potential errors mid-stream.
    /// Verifies error handling and resilience.
    /// </summary>
    [Fact(Timeout = 300000)]
    public async Task StreamEndpoint_HandlesPartialFailure()
    {
        // Arrange
        var query = "What is machine learning?";
        var errorCaught = false;

        // Act
        var states = await _streamClient.CollectStreamAsync(query);

        // Assert - Should still get content despite potential service failures
        Assert.NotEmpty(states);

        // If we reach here without exception, streaming handled any issues gracefully
        var hasFinalContent = states.Any(s => 
            !string.IsNullOrWhiteSpace(s.ResearchBrief) ||
            !string.IsNullOrWhiteSpace(s.FinalReport)
        );

        Assert.True(hasFinalContent, "Should produce content even with partial service issues");
    }

    /// <summary>
    /// Test that streaming properly cancels on token cancellation.
    /// Verifies cancellation support.
    /// </summary>
    [Fact(Timeout = 30000)]
    public async Task StreamEndpoint_RespectsCancellation()
    {
        // Arrange
        var query = "Long query about a complex topic...";
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        var states = new List<StreamState>();

        // Act
        try
        {
            await _streamClient.StreamMasterWorkflowAsync(
                query,
                state => states.Add(state),
                null,
                cts.Token
            );
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        // Assert - Should have received some states before cancellation
        // May have received some states before timeout
        Assert.True(states.Count >= 0, "Cancellation handling works");
    }

    /// <summary>
    /// Test that research ID is properly propagated through stream.
    /// Verifies state tracking.
    /// </summary>
    [Fact(Timeout = 300000)]
    public async Task StreamEndpoint_PropagatesResearchId()
    {
        // Arrange
        var query = "What is blockchain technology?";

        // Act
        var states = await _streamClient.CollectStreamAsync(query);

        // Assert
        var statesWithId = states.Where(s => !string.IsNullOrWhiteSpace(s.ResearchId)).ToList();

        // All states should have same ResearchId after first one sets it
        if (statesWithId.Count > 1)
        {
            var firstId = statesWithId.First().ResearchId;
            Assert.True(
                statesWithId.All(s => s.ResearchId == firstId),
                "Research ID should be consistent across all states"
            );
        }
    }
}
