using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DeepResearchAgent.Services;

/// <summary>
/// Integration with Microsoft Agent-Lightning for distributed agent orchestration.
/// Supports Lightning Server/Client architecture with APO (Automatic Performance Optimization) and VERL (Verification and Reasoning Layer).
/// </summary>
public interface IAgentLightningService
{
    Task<bool> IsHealthyAsync();
    Task<LightningServerInfo> GetServerInfoAsync();
    Task<AgentRegistration> RegisterAgentAsync(string agentId, string agentType, Dictionary<string, object> capabilities);
    Task<AgentTaskResult> SubmitTaskAsync(string agentId, AgentTask task);
    Task<List<AgentTask>> GetPendingTasksAsync(string agentId);
    Task UpdateTaskStatusAsync(string taskId, TaskStatus status, string? result = null);
    Task<VerificationResult> VerifyResultAsync(string taskId, string result);
}

public class AgentLightningService : IAgentLightningService
{
    private readonly HttpClient _httpClient;
    private readonly string _lightningServerUrl;
    private readonly string _clientId;
    private readonly JsonSerializerOptions _jsonOptions;

    public AgentLightningService(HttpClient httpClient, string lightningServerUrl = "http://lightning-server:9090", string? clientId = null)
    {
        _httpClient = httpClient;
        _lightningServerUrl = lightningServerUrl;
        _clientId = clientId ?? $"research-agent-{Guid.NewGuid():N}";
        _jsonOptions = new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }

    public async Task<bool> IsHealthyAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_lightningServerUrl}/health");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public async Task<LightningServerInfo> GetServerInfoAsync()
    {
        try
        {
            var response = await _httpClient.GetAsync($"{_lightningServerUrl}/api/server/info");
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var info = JsonSerializer.Deserialize<LightningServerInfo>(content, _jsonOptions);
            return info ?? new LightningServerInfo();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to get Lightning Server info", ex);
        }
    }

    public async Task<AgentRegistration> RegisterAgentAsync(string agentId, string agentType, Dictionary<string, object> capabilities)
    {
        try
        {
            var registration = new AgentRegistration
            {
                AgentId = agentId,
                AgentType = agentType,
                ClientId = _clientId,
                Capabilities = capabilities,
                RegisteredAt = DateTime.UtcNow
            };

            var response = await _httpClient.PostAsJsonAsync(
                $"{_lightningServerUrl}/api/agents/register",
                registration,
                _jsonOptions
            );
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AgentRegistration>(content, _jsonOptions);
            return result ?? registration;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to register agent with Lightning Server", ex);
        }
    }

    public async Task<AgentTaskResult> SubmitTaskAsync(string agentId, AgentTask task)
    {
        try
        {
            task.SubmittedAt = DateTime.UtcNow;
            task.Status = TaskStatus.Submitted;

            var response = await _httpClient.PostAsJsonAsync(
                $"{_lightningServerUrl}/api/tasks/submit",
                new { agentId, task },
                _jsonOptions
            );
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var result = JsonSerializer.Deserialize<AgentTaskResult>(content, _jsonOptions);
            return result ?? new AgentTaskResult { TaskId = task.Id, Status = TaskStatus.Submitted };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to submit task to Lightning Server", ex);
        }
    }

    public async Task<List<AgentTask>> GetPendingTasksAsync(string agentId)
    {
        try
        {
            var response = await _httpClient.GetAsync(
                $"{_lightningServerUrl}/api/agents/{agentId}/tasks/pending"
            );
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var tasks = JsonSerializer.Deserialize<List<AgentTask>>(content, _jsonOptions);
            return tasks ?? new List<AgentTask>();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to get pending tasks", ex);
        }
    }

    public async Task UpdateTaskStatusAsync(string taskId, TaskStatus status, string? result = null)
    {
        try
        {
            var update = new { taskId, status, result, completedAt = DateTime.UtcNow };
            
            var response = await _httpClient.PutAsJsonAsync(
                $"{_lightningServerUrl}/api/tasks/{taskId}/status",
                update,
                _jsonOptions
            );
            
            response.EnsureSuccessStatusCode();
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Failed to update task status", ex);
        }
    }

    public async Task<VerificationResult> VerifyResultAsync(string taskId, string result)
    {
        try
        {
            var verification = new { taskId, result, verifiedAt = DateTime.UtcNow };
            
            var response = await _httpClient.PostAsJsonAsync(
                $"{_lightningServerUrl}/api/verl/verify",
                verification,
                _jsonOptions
            );
            
            response.EnsureSuccessStatusCode();
            
            var content = await response.Content.ReadAsStringAsync();
            var verificationResult = JsonSerializer.Deserialize<VerificationResult>(content, _jsonOptions);
            return verificationResult ?? new VerificationResult { IsValid = true, TaskId = taskId };
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("VERL verification failed", ex);
        }
    }
}

// Models
public class LightningServerInfo
{
    [JsonPropertyName("version")]
    public string Version { get; set; } = "1.0.0";

    [JsonPropertyName("apoEnabled")]
    public bool ApoEnabled { get; set; } = true;

    [JsonPropertyName("verlEnabled")]
    public bool VerlEnabled { get; set; } = true;

    [JsonPropertyName("registeredAgents")]
    public int RegisteredAgents { get; set; }

    [JsonPropertyName("activeConnections")]
    public int ActiveConnections { get; set; }

    [JsonPropertyName("startedAt")]
    public DateTime StartedAt { get; set; }
}

public class AgentRegistration
{
    [JsonPropertyName("agentId")]
    public string AgentId { get; set; } = string.Empty;

    [JsonPropertyName("agentType")]
    public string AgentType { get; set; } = string.Empty;

    [JsonPropertyName("clientId")]
    public string ClientId { get; set; } = string.Empty;

    [JsonPropertyName("capabilities")]
    public Dictionary<string, object> Capabilities { get; set; } = new();

    [JsonPropertyName("registeredAt")]
    public DateTime RegisteredAt { get; set; }

    [JsonPropertyName("isActive")]
    public bool IsActive { get; set; } = true;
}

public class AgentTask
{
    [JsonPropertyName("id")]
    public string Id { get; set; } = Guid.NewGuid().ToString();

    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("description")]
    public string Description { get; set; } = string.Empty;

    [JsonPropertyName("input")]
    public Dictionary<string, object> Input { get; set; } = new();

    [JsonPropertyName("status")]
    public TaskStatus Status { get; set; }

    [JsonPropertyName("priority")]
    public int Priority { get; set; } = 0;

    [JsonPropertyName("submittedAt")]
    public DateTime SubmittedAt { get; set; }

    [JsonPropertyName("resultData")]
    public string? ResultData { get; set; }

    [JsonPropertyName("verificationRequired")]
    public bool VerificationRequired { get; set; } = true;
}

public class AgentTaskResult
{
    [JsonPropertyName("taskId")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public TaskStatus Status { get; set; }

    [JsonPropertyName("result")]
    public string? Result { get; set; }

    [JsonPropertyName("completedAt")]
    public DateTime? CompletedAt { get; set; }
}

public class VerificationResult
{
    [JsonPropertyName("taskId")]
    public string TaskId { get; set; } = string.Empty;

    [JsonPropertyName("isValid")]
    public bool IsValid { get; set; }

    [JsonPropertyName("confidence")]
    public double Confidence { get; set; }

    [JsonPropertyName("issues")]
    public List<string> Issues { get; set; } = new();

    [JsonPropertyName("verifiedAt")]
    public DateTime VerifiedAt { get; set; }
}

public enum TaskStatus
{
    Submitted,
    Pending,
    InProgress,
    Completed,
    Failed,
    VerificationRequired,
    VerificationPassed,
    VerificationFailed
}