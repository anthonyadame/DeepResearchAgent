using System.Diagnostics;

namespace DeepResearchAgent.Tests.Helpers;

/// <summary>
/// Utility to check and manage Docker container health during testing.
/// </summary>
public class DockerHealthCheck
{
    public static async Task<bool> WaitForServiceHealthAsync(
        string serviceName,
        string healthUrl,
        int timeoutSeconds = 60,
        int intervalMilliseconds = 2000)
    {
        var stopwatch = Stopwatch.StartNew();
        using var httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(5) };

        while (stopwatch.Elapsed.TotalSeconds < timeoutSeconds)
        {
            try
            {
                var response = await httpClient.GetAsync(healthUrl);
                if (response.IsSuccessStatusCode)
                {
                    Console.WriteLine($"✓ {serviceName} is healthy");
                    return true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"⏳ Waiting for {serviceName}... ({ex.Message})");
            }

            await Task.Delay(intervalMilliseconds);
        }

        Console.WriteLine($"✗ {serviceName} did not become healthy within {timeoutSeconds} seconds");
        return false;
    }

    public static async Task<DockerComposeStatus> CheckDockerComposeHealthAsync()
    {
        var status = new DockerComposeStatus();

        var services = new[]
        {
            ("Ollama", "http://localhost:11434/api/health"),
            ("SearXNG", "http://localhost:8080/healthz"),
            ("Crawl4AI", "http://localhost:11235/health"),
            ("Lightning Server", "http://localhost:9090/health")
        };

        foreach (var (serviceName, healthUrl) in services)
        {
            var isHealthy = await WaitForServiceHealthAsync(serviceName, healthUrl, timeoutSeconds: 5);
            status.ServiceHealth[serviceName] = isHealthy;
        }

        return status;
    }

    public static async Task<bool> StartDockerComposeAsync(string composeFilePath = "docker-compose.yml")
    {
        try
        {
            Console.WriteLine("🐳 Starting Docker Compose services...");
            var result = await RunDockerCommandAsync("docker-compose", $"up -d -f {composeFilePath}");
            return result.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to start Docker Compose: {ex.Message}");
            return false;
        }
    }

    public static async Task<bool> StopDockerComposeAsync(string composeFilePath = "docker-compose.yml")
    {
        try
        {
            Console.WriteLine("🛑 Stopping Docker Compose services...");
            var result = await RunDockerCommandAsync("docker-compose", $"down -f {composeFilePath}");
            return result.Success;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to stop Docker Compose: {ex.Message}");
            return false;
        }
    }

    private static async Task<CommandResult> RunDockerCommandAsync(string command, string arguments)
    {
        return await Task.Run(() =>
        {
            var processInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = Process.Start(processInfo);
            if (process == null)
                return new CommandResult { Success = false, Output = "Failed to start process" };

            var output = process.StandardOutput.ReadToEnd();
            var error = process.StandardError.ReadToEnd();
            process.WaitForExit();

            return new CommandResult
            {
                Success = process.ExitCode == 0,
                Output = output,
                Error = error
            };
        });
    }
}

public class DockerComposeStatus
{
    public Dictionary<string, bool> ServiceHealth { get; } = new();

    public bool AllHealthy => ServiceHealth.Values.All(h => h);

    public override string ToString()
    {
        var sb = new System.Text.StringBuilder();
        sb.AppendLine("Docker Compose Service Status:");
        foreach (var (service, healthy) in ServiceHealth)
        {
            var status = healthy ? "✓ Healthy" : "✗ Unhealthy";
            sb.AppendLine($"  {service}: {status}");
        }
        sb.AppendLine($"Overall: {(AllHealthy ? "✓ All Services Healthy" : "✗ Some Services Unhealthy")}");
        return sb.ToString();
    }
}

public class CommandResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = string.Empty;
    public string Error { get; set; } = string.Empty;
}