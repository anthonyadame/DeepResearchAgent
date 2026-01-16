using Xunit;
using DeepResearchAgent.Tests.Helpers;
using System.Diagnostics;

namespace DeepResearchAgent.Tests.Fixtures;

/// <summary>
/// Xunit fixture for Lightning Server lifecycle management.
/// </summary>
public class LightningServerFixture : IAsyncLifetime
{
    private const int StartupTimeoutSeconds = 60;
    private readonly string _dockerComposeFile;

    public LightningServerFixture(string dockerComposeFile = "docker-compose.yml")
    {
        _dockerComposeFile = dockerComposeFile;
    }

    public async Task InitializeAsync()
    {
        Console.WriteLine("🚀 Initializing Lightning Server fixture...");

        // Check if server is already running
        var isHealthy = await DockerHealthCheck.WaitForServiceHealthAsync(
            "Lightning Server",
            "http://localhost:9090/health",
            timeoutSeconds: 10,
            intervalMilliseconds: 1000
        );

        if (isHealthy)
        {
            Console.WriteLine("✓ Lightning Server already running");
            return;
        }

        // Start Docker Compose
        var started = await DockerHealthCheck.StartDockerComposeAsync(_dockerComposeFile);
        if (!started)
            throw new InvalidOperationException("Failed to start Docker Compose");

        // Wait for server to become healthy
        var stopwatch = Stopwatch.StartNew();
        while (stopwatch.Elapsed.TotalSeconds < StartupTimeoutSeconds)
        {
            isHealthy = await DockerHealthCheck.WaitForServiceHealthAsync(
                "Lightning Server",
                "http://localhost:9090/health",
                timeoutSeconds: 5,
                intervalMilliseconds: 2000
            );

            if (isHealthy)
                return;
        }

        throw new TimeoutException(
            $"Lightning Server did not become healthy within {StartupTimeoutSeconds} seconds"
        );
    }

    public async Task DisposeAsync()
    {
        Console.WriteLine("🧹 Cleaning up Lightning Server fixture...");
        await DockerHealthCheck.StopDockerComposeAsync(_dockerComposeFile);
    }
}

/// <summary>
/// Xunit collection definition for tests requiring Lightning Server.
/// </summary>
[CollectionDefinition("Lightning Server Collection")]
public class LightningServerCollection : ICollectionFixture<LightningServerFixture>
{
}