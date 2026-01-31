using System.Text.Json;
using DeepResearchAgent.Models;

namespace DeepResearchAgent.Api.Clients;

/// <summary>
/// Strongly-typed client for consuming the MasterWorkflow streaming endpoint.
/// Simplifies Server-Sent Events (SSE) handling for UI and test consumers.
/// </summary>
public class MasterWorkflowStreamClient
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public MasterWorkflowStreamClient(HttpClient httpClient, string baseUrl = "http://localhost:5000")
    {
        _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
        _baseUrl = baseUrl.TrimEnd('/');
    }

    /// <summary>
    /// Stream the MasterWorkflow with real-time progress updates.
    /// Handles Server-Sent Events parsing and StreamState deserialization.
    /// </summary>
    /// <param name="userQuery">Research query from user</param>
    /// <param name="onStateReceived">Callback for each StreamState object received</param>
    /// <param name="onError">Callback for any streaming errors</param>
    /// <param name="cancellationToken">Cancellation token</param>
    public async Task StreamMasterWorkflowAsync(
        string userQuery,
        Action<StreamState> onStateReceived,
        Action<Exception>? onError = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(userQuery))
            throw new ArgumentNullException(nameof(userQuery));

        try
        {
            var request = new { userQuery };
            var content = new StringContent(
                JsonSerializer.Serialize(request),
                System.Text.Encoding.UTF8,
                "application/json"
            );

            using var response = await _httpClient.PostAsync(
                $"{_baseUrl}/api/workflows/master/stream",
                content,
                cancellationToken
            );

            if (!response.IsSuccessStatusCode)
                throw new HttpRequestException($"API returned {response.StatusCode}: {response.ReasonPhrase}");

            using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            using var reader = new StreamReader(stream);

            string? line;
            while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (line.StartsWith("data: "))
                {
                    try
                    {
                        var jsonData = line.Substring(6);
                        var state = JsonSerializer.Deserialize<StreamState>(jsonData);

                        if (state != null)
                        {
                            onStateReceived(state);
                        }
                    }
                    catch (JsonException ex)
                    {
                        onError?.Invoke(new InvalidOperationException($"Failed to parse StreamState: {line}", ex));
                    }
                }
            }
        }
        catch (OperationCanceledException)
        {
            // Expected when cancellation is requested
        }
        catch (Exception ex)
        {
            onError?.Invoke(ex);
            throw;
        }
    }

    /// <summary>
    /// Stream the MasterWorkflow and collect all states into a list.
    /// Useful for testing and verification scenarios.
    /// </summary>
    public async Task<List<StreamState>> CollectStreamAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        var states = new List<StreamState>();
        await StreamMasterWorkflowAsync(
            userQuery,
            state => states.Add(state),
            null,
            cancellationToken
        );
        return states;
    }

    /// <summary>
    /// Stream the MasterWorkflow and display progress to console.
    /// Useful for CLI testing and debugging.
    /// </summary>
    public async Task DisplayStreamAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        await StreamMasterWorkflowAsync(
            userQuery,
            state =>
            {
                var summary = GetStateSummary(state);
                Console.WriteLine($"📊 {summary}");
            },
            ex =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine($"❌ Error: {ex.Message}");
                Console.ResetColor();
            },
            cancellationToken
        );
    }

    /// <summary>
    /// Creates a human-readable summary of a StreamState.
    /// </summary>
    private static string GetStateSummary(StreamState state)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(state.Status))
            parts.Add($"[{state.Status}]");

        if (!string.IsNullOrWhiteSpace(state.ResearchBrief) && string.IsNullOrWhiteSpace(state.DraftReport))
            parts.Add($"📝 Brief ({state.ResearchBrief.Length} chars)");

        if (!string.IsNullOrWhiteSpace(state.DraftReport) && string.IsNullOrWhiteSpace(state.RefinedSummary))
            parts.Add($"📄 Draft ({state.DraftReport.Length} chars)");

        if (!string.IsNullOrWhiteSpace(state.SupervisorUpdate))
            parts.Add($"🔄 {state.SupervisorUpdate}");

        if (!string.IsNullOrWhiteSpace(state.RefinedSummary) && string.IsNullOrWhiteSpace(state.FinalReport))
            parts.Add($"✨ Refined ({state.RefinedSummary.Length} chars)");

        if (!string.IsNullOrWhiteSpace(state.FinalReport))
            parts.Add($"✅ Final ({state.FinalReport.Length} chars)");

        return string.Join(" → ", parts);
    }
}
