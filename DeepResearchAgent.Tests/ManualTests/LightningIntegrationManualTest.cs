using DeepResearchAgent.Services;
using DeepResearchAgent.Tests.Helpers;

namespace DeepResearchAgent.Tests.ManualTests;

/// <summary>
/// Manual test script for Agent-Lightning integration.
/// Run with: dotnet run --project DeepResearchAgent.Tests -- manual-lightning-test
/// </summary>
public class LightningIntegrationManualTest
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("╔════════════════════════════════════════════════╗");
        Console.WriteLine("║   Agent-Lightning Integration Manual Test     ║");
        Console.WriteLine("╚════════════════════════════════════════════════╝\n");

        // Step 1: Check Docker services
        Console.WriteLine("Step 1: Checking Docker services health...\n");
        var status = await DockerHealthCheck.CheckDockerComposeHealthAsync();
        Console.WriteLine(status);

        if (!status.AllHealthy)
        {
            Console.WriteLine("\n⚠ Not all services are healthy. Starting Docker Compose...\n");
            await DockerHealthCheck.StartDockerComposeAsync();
            
            // Wait for services
            Console.WriteLine("\n⏳ Waiting for services to become healthy...\n");
            await Task.Delay(15000);
            
            status = await DockerHealthCheck.CheckDockerComposeHealthAsync();
            Console.WriteLine(status);

            if (!status.AllHealthy)
            {
                Console.WriteLine("\n❌ Services failed to start. Exiting.");
                return;
            }
        }

        // Step 2: Test Agent-Lightning connection
        Console.WriteLine("\nStep 2: Testing Agent-Lightning connection...\n");
        var httpClient = new HttpClient();
        var lightningService = new AgentLightningService(httpClient, "http://localhost:8090");

        try
        {
            var isHealthy = await lightningService.IsHealthyAsync();
            Console.WriteLine($"✓ Lightning Server connection: {(isHealthy ? "✓ Success" : "✗ Failed")}");

            if (isHealthy)
            {
                var serverInfo = await lightningService.GetServerInfoAsync();
                Console.WriteLine($"✓ Server Version: {serverInfo.Version}");
                Console.WriteLine($"✓ APO Enabled: {serverInfo.ApoEnabled}");
                Console.WriteLine($"✓ VERL Enabled: {serverInfo.VerlEnabled}");
                Console.WriteLine($"✓ Registered Agents: {serverInfo.RegisteredAgents}");
                Console.WriteLine($"✓ Active Connections: {serverInfo.ActiveConnections}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            return;
        }

        // Step 3: Test agent registration
        Console.WriteLine("\nStep 3: Testing agent registration...\n");
        try
        {
            var agentId = $"test-agent-{Guid.NewGuid():N}";
            var capabilities = new Dictionary<string, object>
            {
                { "research", true },
                { "synthesis", true },
                { "verification", true }
            };

            var registration = await lightningService.RegisterAgentAsync(
                agentId,
                "ResearchAgent",
                capabilities
            );

            Console.WriteLine($"✓ Agent Registration Success");
            Console.WriteLine($"  Agent ID: {registration.AgentId}");
            Console.WriteLine($"  Agent Type: {registration.AgentType}");
            Console.WriteLine($"  Status: {(registration.IsActive ? "Active" : "Inactive")}");

            // Step 4: Test task submission
            Console.WriteLine("\nStep 4: Testing task submission...\n");
            var task = new AgentTask
            {
                Name = "Research Task",
                Description = "Test research task for integration testing",
                Input = new Dictionary<string, object>
                {
                    { "query", "Latest advancements in AI" },
                    { "depth", "comprehensive" }
                }
            };

            var taskResult = await lightningService.SubmitTaskAsync(agentId, task);
            Console.WriteLine($"✓ Task Submission Success");
            Console.WriteLine($"  Task ID: {taskResult.TaskId}");
            Console.WriteLine($"  Status: {taskResult.Status}");

            // Step 5: Test verification
            Console.WriteLine("\nStep 5: Testing VERL verification...\n");
            var testResult = "The research shows that transformers improve NLP accuracy by 25%";
            var verification = await lightningService.VerifyResultAsync(taskResult.TaskId, testResult);
            
            Console.WriteLine($"✓ Verification Success");
            Console.WriteLine($"  Is Valid: {verification.IsValid}");
            Console.WriteLine($"  Confidence: {verification.Confidence:P}");
            if (verification.Issues.Any())
            {
                Console.WriteLine($"  Issues: {string.Join(", ", verification.Issues)}");
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Error: {ex.Message}");
            Console.WriteLine($"Stack Trace: {ex.StackTrace}");
        }

        Console.WriteLine("\n╔════════════════════════════════════════════════╗");
        Console.WriteLine("║              Test Complete                     ║");
        Console.WriteLine("╚════════════════════════════════════════════════╝");
    }
}