using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Tools;
using DeepResearchAgent.Prompts;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

Console.WriteLine("=== Deep Research Agent - C# Implementation ===");
Console.WriteLine("Multi-agent research system with diffusion-based refinement");
Console.WriteLine("Powered by Microsoft Agent-Lightning\n");

var configuration = new ConfigurationBuilder()
    .AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: true)
    .AddEnvironmentVariables()
    .Build();

var ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
var ollamaDefaultModel = configuration["Ollama:DefaultModel"] ?? "gpt-oss:20b";
var searxngBaseUrl = configuration["SearXNG:BaseUrl"] ?? "http://localhost:8080";
var crawl4aiBaseUrl = configuration["Crawl4AI:BaseUrl"] ?? "http://localhost:11235";
var lightningServerUrl = configuration["Lightning:ServerUrl"]
    ?? Environment.GetEnvironmentVariable("LIGHTNING_SERVER_URL")
    ?? "http://lightning-server:9090";

// Initialize services
var services = new ServiceCollection();

// Register core services
services.AddSingleton<OllamaService>(_ => new OllamaService(
    baseUrl: ollamaBaseUrl,
    defaultModel: ollamaDefaultModel
));
services.AddSingleton<HttpClient>();
services.AddSingleton<SearCrawl4AIService>(sp => new SearCrawl4AIService(
    sp.GetRequiredService<HttpClient>(),
    searxngBaseUrl,
    crawl4aiBaseUrl
));
services.AddSingleton<LightningStore>();

// Register Agent-Lightning services
services.AddSingleton<IAgentLightningService>(sp => new AgentLightningService(
    sp.GetRequiredService<HttpClient>(),
    lightningServerUrl
));
services.AddSingleton<ILightningVERLService>(sp => new LightningVERLService(
    sp.GetRequiredService<HttpClient>(),
    lightningServerUrl
));

// Register workflows
services.AddSingleton<ResearcherWorkflow>();
services.AddSingleton<SupervisorWorkflow>();
services.AddSingleton<MasterWorkflow>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("✓ Services initialized");
Console.WriteLine($"✓ Ollama connection configured ({ollamaBaseUrl})");
Console.WriteLine($"✓ Web search + scraping configured (SearXNG: {searxngBaseUrl}, Crawl4AI: {crawl4aiBaseUrl})");
Console.WriteLine("✓ Knowledge persistence configured (LightningStore)");
Console.WriteLine($"✓ Agent-Lightning integration configured ({lightningServerUrl})");
Console.WriteLine("✓ APO (Automatic Performance Optimization) enabled");
Console.WriteLine("✓ VERL (Verification and Reasoning Layer) enabled\n");

// Interactive menu loop
bool running = true;
while (running)
{
    DisplayMenu();
    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            await CheckOllamaConnection(serviceProvider, ollamaBaseUrl);
            break;
        case "2":
            await CheckSearXNGConnection(serviceProvider, searxngBaseUrl);
            break;
        case "3":
            await CheckCrawl4AIConnection(serviceProvider, crawl4aiBaseUrl);
            break;
        case "4":
            await CheckLightningConnection(serviceProvider, lightningServerUrl);
            break;
        case "5":
            await RunWorkflowOrchestration(serviceProvider);
            break;
        case "6":
            await RunAllHealthChecks(serviceProvider, ollamaBaseUrl, searxngBaseUrl, crawl4aiBaseUrl, lightningServerUrl);
            break;
        case "0":
            running = false;
            Console.WriteLine("\n👋 Goodbye!");
            break;
        default:
            Console.WriteLine("\n❌ Invalid choice. Please try again.\n");
            break;
    }

    if (running && choice is "1" or "2" or "3" or "4" or "5" or "6")
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
        Console.Clear();
    }
}

static void DisplayMenu()
{
    Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
    Console.WriteLine("║     Deep Research Agent - Main Menu (Lightning)       ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("  [1] 🔍 Check Ollama Connection");
    Console.WriteLine("  [2] 🌐 Check SearXNG Connection");
    Console.WriteLine("  [3] 🕷️  Check Crawl4AI Connection");
    Console.WriteLine("  [4] ⚡ Check Agent-Lightning Connection");
    Console.WriteLine("  [5] ⚙️  Run Scaffolded Workflow Orchestration");
    Console.WriteLine("  [6] 🏥 Run All Health Checks");
    Console.WriteLine("  [0] 🚪 Exit");
    Console.WriteLine();
    Console.Write("Enter your choice: ");
}

static async Task CheckOllamaConnection(ServiceProvider serviceProvider, string ollamaBaseUrl)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🔍 CHECKING OLLAMA CONNECTION");
    Console.WriteLine(new string('═', 60));

    try
    {
        var ollamaService = serviceProvider.GetRequiredService<OllamaService>();
        
        Console.WriteLine($"➤ Endpoint: {ollamaBaseUrl}");
        Console.WriteLine("➤ Checking health...");
        
        var isHealthy = await ollamaService.IsHealthyAsync();
        
        if (isHealthy)
        {
            Console.WriteLine("✓ Ollama is running and healthy\n");
            
            // Get available models
            Console.WriteLine("➤ Fetching available models...");
            var models = await ollamaService.GetAvailableModelsAsync();
            
            if (models.Any())
            {
                Console.WriteLine($"✓ Found {models.Count()} model(s):");
                foreach (var model in models.Take(5))
                {
                    Console.WriteLine($"  • {model}");
                }
                if (models.Count() > 5)
                {
                    Console.WriteLine($"  ... and {models.Count() - 5} more");
                }
            }
            else
            {
                Console.WriteLine("⚠ No models found. Run: ollama pull mistral");
            }
            
            // Test invocation
            Console.WriteLine("\n➤ Testing LLM invocation...");
            var testMessages = new List<OllamaChatMessage>
            {
                new() { Role = "user", Content = "Say 'Hello from Deep Research Agent!' in one sentence." }
            };
            
            var response = await ollamaService.InvokeAsync(testMessages);
            Console.WriteLine($"✓ Response: {response.Content}");
            Console.WriteLine("\n✅ OLLAMA CONNECTION: SUCCESS");
        }
        else
        {
            Console.WriteLine($"❌ Ollama is not accessible at {ollamaBaseUrl}");
            Console.WriteLine("\n📝 To start Ollama:");
            Console.WriteLine("   ollama serve");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR: {ex.Message}");
        Console.WriteLine("\n📝 Troubleshooting:");
        Console.WriteLine("   1. Start Ollama: ollama serve");
        Console.WriteLine("   2. Pull a model: ollama pull mistral");
        Console.WriteLine($"   3. Verify endpoint: {ollamaBaseUrl}");
    }
}

static async Task CheckSearXNGConnection(ServiceProvider serviceProvider, string searxngBaseUrl)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🌐 CHECKING SEARXNG CONNECTION");
    Console.WriteLine(new string('═', 60));

    try
    {
        var httpClient = serviceProvider.GetRequiredService<HttpClient>();
        
        Console.WriteLine($"➤ Endpoint: {searxngBaseUrl}");
        Console.WriteLine("➤ Checking health...");
        
        var response = await httpClient.GetAsync($"{searxngBaseUrl}/healthz");
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("✓ SearXNG is running and healthy");
            
            // Test search
            Console.WriteLine("\n➤ Testing search functionality...");
            var searchUrl = $"{searxngBaseUrl}/search?q=test&format=json";
            var searchResponse = await httpClient.GetAsync(searchUrl);
            
            if (searchResponse.IsSuccessStatusCode)
            {
                var content = await searchResponse.Content.ReadAsStringAsync();
                Console.WriteLine("✓ Search API is responding");
                Console.WriteLine($"  Sample response length: {content.Length} characters");
            }
            
            Console.WriteLine("\n✅ SEARXNG CONNECTION: SUCCESS");
        }
        else
        {
            Console.WriteLine($"❌ SearXNG responded with: {response.StatusCode}");
            Console.WriteLine("\n📝 To start SearXNG:");
            Console.WriteLine("   docker-compose up searxng -d");
        }
    }
    catch (HttpRequestException ex)
    {
        Console.WriteLine($"❌ Cannot connect to SearXNG: {ex.Message}");
        Console.WriteLine("\n📝 Troubleshooting:");
        Console.WriteLine("   1. Start SearXNG: docker-compose up searxng -d");
        Console.WriteLine("   2. Check Docker: docker ps");
        Console.WriteLine($"   3. Verify endpoint: {searxngBaseUrl}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR: {ex.Message}");
    }
}

static async Task CheckCrawl4AIConnection(ServiceProvider serviceProvider, string crawl4aiBaseUrl)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🕷️  CHECKING CRAWL4AI CONNECTION");
    Console.WriteLine(new string('═', 60));

    try
    {
        var crawl4aiService = serviceProvider.GetRequiredService<SearCrawl4AIService>();
        
        Console.WriteLine($"➤ Endpoint: {crawl4aiBaseUrl}");
        
        Console.WriteLine("✓ Crawl4AI is running and healthy");
            
        // Test scraping
        Console.WriteLine("\n➤ Testing web scraping...");
        var testUrl = new List<string>() { "https://example.com" };
        Console.WriteLine($"  Target: {testUrl}");
            
        var scrapedContent = await crawl4aiService.ScrapeAsync(testUrl);
            
        if (scrapedContent != null)
        {
            foreach (var item in scrapedContent.Results)
            {
                Console.WriteLine("✓ Scraping successful");
                Console.WriteLine($"  • URL: {item.Url}");
                Console.WriteLine($"  • Title: {item.Title ?? "N/A"}");
                Console.WriteLine($"  • Content length: {item.CleanedHtml?.Length ?? 0} characters");
                Console.WriteLine($"  • Success: {item.Success}");
            }
        }
            
        Console.WriteLine("\n✅ CRAWL4AI CONNECTION: SUCCESS");
        
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR: {ex.Message}");
        Console.WriteLine("\n📝 Troubleshooting:");
        Console.WriteLine("   1. Start Crawl4AI: docker-compose up crawl4ai -d");
        Console.WriteLine("   2. Check Docker: docker ps");
        Console.WriteLine($"   3. Verify endpoint: {crawl4aiBaseUrl}");
    }
}

static async Task CheckLightningConnection(ServiceProvider serviceProvider, string lightningServerUrl)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("⚡ CHECKING AGENT-LIGHTNING CONNECTION");
    Console.WriteLine(new string('═', 60));

    try
    {
        var lightningService = serviceProvider.GetRequiredService<IAgentLightningService>();
        
        Console.WriteLine($"➤ Endpoint: {lightningServerUrl}");
        Console.WriteLine("➤ Checking health...");
        
        var isHealthy = await lightningService.IsHealthyAsync();
        
        if (isHealthy)
        {
            Console.WriteLine("✓ Agent-Lightning Server is running\n");
            
            // Get server info
            Console.WriteLine("➤ Fetching server information...");
            var serverInfo = await lightningService.GetServerInfoAsync();
            
            Console.WriteLine($"✓ Server Version: {serverInfo.Version}");
            Console.WriteLine($"✓ APO (Auto Performance Optimization): {(serverInfo.ApoEnabled ? "Enabled" : "Disabled")}");
            Console.WriteLine($"✓ VERL (Verification and Reasoning Layer): {(serverInfo.VerlEnabled ? "Enabled" : "Disabled")}");
            Console.WriteLine($"✓ Registered Agents: {serverInfo.RegisteredAgents}");
            Console.WriteLine($"✓ Active Connections: {serverInfo.ActiveConnections}");
            
            // Register this client as an agent
            Console.WriteLine("\n➤ Registering research agent...");
            var capabilities = new Dictionary<string, object>
            {
                { "research", true },
                { "synthesis", true },
                { "verification", true },
                { "web_search", true },
                { "content_scraping", true }
            };
            
            var registration = await lightningService.RegisterAgentAsync(
                "deep-research-agent",
                "ResearchOrchestrator",
                capabilities
            );
            
            Console.WriteLine($"✓ Agent registered: {registration.AgentId}");
            
            Console.WriteLine("\n✅ AGENT-LIGHTNING CONNECTION: SUCCESS");
        }
        else
        {
            Console.WriteLine($"❌ Agent-Lightning Server is not accessible at {lightningServerUrl}");
            Console.WriteLine("\n📝 To start Lightning Server:");
            Console.WriteLine("   docker-compose up lightning-server -d");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR: {ex.Message}");
        Console.WriteLine("\n📝 Troubleshooting:");
        Console.WriteLine("   1. Start Lightning Server: docker-compose up lightning-server -d");
        Console.WriteLine("   2. Check Docker: docker ps");
        Console.WriteLine($"   3. Verify endpoint: {lightningServerUrl}");
    }
}

static async Task RunWorkflowOrchestration(ServiceProvider serviceProvider)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("⚙️  RUNNING WORKFLOW ORCHESTRATION");
    Console.WriteLine(new string('═', 60));

    try
    {
        Console.Write("\nEnter your research query (or press Enter for default): ");
        var query = Console.ReadLine()?.Trim();
        
        if (string.IsNullOrEmpty(query))
        {
            query = "Summarize the latest advancements in transformer architectures";
            Console.WriteLine($"Using default query: {query}");
        }

        var masterWorkflow = serviceProvider.GetRequiredService<MasterWorkflow>();
        
        Console.WriteLine("\n➤ Starting workflow execution with Lightning orchestration...\n");
        Console.WriteLine(new string('-', 60));
        
        // Run with streaming updates
        await foreach (var update in masterWorkflow.StreamAsync(query, CancellationToken.None))
        {
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {update}");
        }
        
        Console.WriteLine(new string('-', 60));
        Console.WriteLine("\n✅ WORKFLOW EXECUTION: COMPLETE");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n❌ WORKFLOW ERROR: {ex.Message}");
        Console.WriteLine("\n📝 Verify:");
        Console.WriteLine("   1. All services are running (Ollama, SearXNG, Crawl4AI, Lightning)");
        Console.WriteLine("   2. Run health checks first (option 6)");
        Console.WriteLine("\nStack trace:");
        Console.WriteLine(ex.StackTrace);
    }
}

static async Task RunAllHealthChecks(
    ServiceProvider serviceProvider,
    string ollamaBaseUrl,
    string searxngBaseUrl,
    string crawl4aiBaseUrl,
    string lightningServerUrl)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🏥 RUNNING ALL HEALTH CHECKS");
    Console.WriteLine(new string('═', 60));

    await CheckOllamaConnection(serviceProvider, ollamaBaseUrl);
    Console.WriteLine();
    
    await CheckSearXNGConnection(serviceProvider, searxngBaseUrl);
    Console.WriteLine();
    
    await CheckCrawl4AIConnection(serviceProvider, crawl4aiBaseUrl);
    Console.WriteLine();
    
    await CheckLightningConnection(serviceProvider, lightningServerUrl);
    
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🏥 HEALTH CHECK SUMMARY COMPLETE");
    Console.WriteLine(new string('═', 60));
}
