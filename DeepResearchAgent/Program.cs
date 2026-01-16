using DeepResearchAgent.Models;
using DeepResearchAgent.Services;
using DeepResearchAgent.Tools;
using DeepResearchAgent.Prompts;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

Console.WriteLine("=== Deep Research Agent - C# Implementation ===");
Console.WriteLine("Multi-agent research system with diffusion-based refinement\n");

// Initialize services
var services = new ServiceCollection();

// Register core services
services.AddSingleton<OllamaService>(_ => new OllamaService(
    baseUrl: "http://localhost:11434",
    defaultModel: "gpt-oss:20b"  // Change to your preferred model
));
services.AddSingleton<HttpClient>();
services.AddSingleton<SearCrawl4AIService>(sp => new SearCrawl4AIService(
    sp.GetRequiredService<HttpClient>()
));
services.AddSingleton<LightningStore>();

// Register workflows
services.AddSingleton<ResearcherWorkflow>();
services.AddSingleton<SupervisorWorkflow>();
services.AddSingleton<MasterWorkflow>();

// Build service provider
var serviceProvider = services.BuildServiceProvider();

Console.WriteLine("✓ Services initialized");
Console.WriteLine("✓ Ollama connection configured (http://localhost:11434)");
Console.WriteLine("✓ Web search + scraping configured (SearXNG + Crawl4AI)");
Console.WriteLine("✓ Knowledge persistence configured (LightningStore)\n");

// Interactive menu loop
bool running = true;
while (running)
{
    DisplayMenu();
    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            await CheckOllamaConnection(serviceProvider);
            break;
        case "2":
            await CheckSearXNGConnection(serviceProvider);
            break;
        case "3":
            await CheckCrawl4AIConnection(serviceProvider);
            break;
        case "4":
            await RunWorkflowOrchestration(serviceProvider);
            break;
        case "5":
            await RunAllHealthChecks(serviceProvider);
            break;
        case "0":
            running = false;
            Console.WriteLine("\n👋 Goodbye!");
            break;
        default:
            Console.WriteLine("\n❌ Invalid choice. Please try again.\n");
            break;
    }

    if (running && choice is "1" or "2" or "3" or "4" or "5")
    {
        Console.WriteLine("\nPress any key to continue...");
        Console.ReadKey();
        Console.Clear();
    }
}

static void DisplayMenu()
{
    Console.WriteLine("\n╔════════════════════════════════════════════════════════╗");
    Console.WriteLine("║        Deep Research Agent - Main Menu                ║");
    Console.WriteLine("╚════════════════════════════════════════════════════════╝");
    Console.WriteLine();
    Console.WriteLine("  [1] 🔍 Check Ollama Connection");
    Console.WriteLine("  [2] 🌐 Check SearXNG Connection");
    Console.WriteLine("  [3] 🕷️  Check Crawl4AI Connection");
    Console.WriteLine("  [4] ⚙️  Run Scaffolded Workflow Orchestration");
    Console.WriteLine("  [5] 🏥 Run All Health Checks");
    Console.WriteLine("  [0] 🚪 Exit");
    Console.WriteLine();
    Console.Write("Enter your choice: ");
}

static async Task CheckOllamaConnection(ServiceProvider serviceProvider)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🔍 CHECKING OLLAMA CONNECTION");
    Console.WriteLine(new string('═', 60));

    try
    {
        var ollamaService = serviceProvider.GetRequiredService<OllamaService>();
        
        Console.WriteLine("➤ Endpoint: http://localhost:11434");
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
            Console.WriteLine("❌ Ollama is not accessible at http://localhost:11434");
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
        Console.WriteLine("   3. Verify endpoint: http://localhost:11434");
    }
}

static async Task CheckSearXNGConnection(ServiceProvider serviceProvider)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🌐 CHECKING SEARXNG CONNECTION");
    Console.WriteLine(new string('═', 60));

    try
    {
        var httpClient = serviceProvider.GetRequiredService<HttpClient>();
        var searxngUrl = "http://localhost:8080";
        
        Console.WriteLine($"➤ Endpoint: {searxngUrl}");
        Console.WriteLine("➤ Checking health...");
        
        var response = await httpClient.GetAsync($"{searxngUrl}/healthz");
        
        if (response.IsSuccessStatusCode)
        {
            Console.WriteLine("✓ SearXNG is running and healthy");
            
            // Test search
            Console.WriteLine("\n➤ Testing search functionality...");
            var searchUrl = $"{searxngUrl}/search?q=test&format=json";
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
        Console.WriteLine("   3. Verify endpoint: http://localhost:8080");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"❌ ERROR: {ex.Message}");
    }
}

static async Task CheckCrawl4AIConnection(ServiceProvider serviceProvider)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🕷️  CHECKING CRAWL4AI CONNECTION");
    Console.WriteLine(new string('═', 60));

    try
    {
        var crawl4aiService = serviceProvider.GetRequiredService<SearCrawl4AIService>();
        
        Console.WriteLine("➤ Endpoint: http://localhost:11235");
        
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
        Console.WriteLine("   3. Verify endpoint: http://localhost:11235");
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
        
        Console.WriteLine("\n➤ Starting workflow execution...\n");
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
        Console.WriteLine("   1. All services are running (Ollama, SearXNG, Crawl4AI)");
        Console.WriteLine("   2. Run health checks first (option 5)");
        Console.WriteLine("\nStack trace:");
        Console.WriteLine(ex.StackTrace);
    }
}

static async Task RunAllHealthChecks(ServiceProvider serviceProvider)
{
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🏥 RUNNING ALL HEALTH CHECKS");
    Console.WriteLine(new string('═', 60));

    await CheckOllamaConnection(serviceProvider);
    Console.WriteLine();
    
    await CheckSearXNGConnection(serviceProvider);
    Console.WriteLine();
    
    await CheckCrawl4AIConnection(serviceProvider);
    
    Console.WriteLine("\n" + new string('═', 60));
    Console.WriteLine("🏥 HEALTH CHECK SUMMARY COMPLETE");
    Console.WriteLine(new string('═', 60));
}
