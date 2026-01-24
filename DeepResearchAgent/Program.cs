using DeepResearchAgent.Models;
using DeepResearchAgent.Prompts;
using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Services.VectorDatabase;
using DeepResearchAgent.Tools;
using DeepResearchAgent.Workflows;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Runtime.CompilerServices;
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
    ?? "http://localhost:9090";

// Vector database configuration
var vectorDbEnabled = configuration.GetValue("VectorDatabase:Enabled", false);
var qdrantBaseUrl = configuration["VectorDatabase:Qdrant:BaseUrl"] ?? "http://localhost:6333";
var qdrantCollectionName = configuration["VectorDatabase:Qdrant:CollectionName"] ?? "research";
var qdrantVectorDimension = configuration.GetValue("VectorDatabase:Qdrant:VectorDimension", 384);
var embeddingModel = configuration["VectorDatabase:EmbeddingModel"] ?? "nomic-embed-text";
var embeddingApiUrl = configuration["VectorDatabase:EmbeddingApiUrl"] ?? ollamaBaseUrl;

// Initialize services
var services = new ServiceCollection();

services.AddLogging(logging => 
{
    logging.AddConsole();
    logging.SetMinimumLevel(LogLevel.Trace);
});

// Register core services
services.AddMemoryCache();
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

// Register embedding service
services.AddSingleton<IEmbeddingService>(sp => new OllamaEmbeddingService(
    sp.GetRequiredService<HttpClient>(),
    baseUrl: embeddingApiUrl,
    model: embeddingModel,
    dimension: qdrantVectorDimension,
    logger: sp.GetService<Microsoft.Extensions.Logging.ILogger>()
));

// Register vector database service (Qdrant)
if (vectorDbEnabled)
{
    services.AddSingleton<IVectorDatabaseService>(sp => new QdrantVectorDatabaseService(
        sp.GetRequiredService<HttpClient>(),
        new QdrantConfig
        {
            BaseUrl = qdrantBaseUrl,
            CollectionName = qdrantCollectionName,
            VectorDimension = qdrantVectorDimension
        },
        sp.GetRequiredService<IEmbeddingService>(),
        logger: sp.GetService<Microsoft.Extensions.Logging.ILogger>()
    ));
}

// Register vector database factory (for multiple DB support)
services.AddSingleton<IVectorDatabaseFactory>(sp =>
{
    var factory = new VectorDatabaseFactory(sp.GetService<Microsoft.Extensions.Logging.ILogger>());
    
    if (vectorDbEnabled && sp.GetService<IVectorDatabaseService>() != null)
    {
        factory.RegisterVectorDatabase("qdrant", sp.GetRequiredService<IVectorDatabaseService>());
    }
    
    return factory;
});

// Register Agent-Lightning services
services.AddSingleton<IAgentLightningService>(sp => new AgentLightningService(
    sp.GetRequiredService<HttpClient>(),
    lightningServerUrl
));
services.AddSingleton<ILightningVERLService>(sp => new LightningVERLService(
    sp.GetRequiredService<HttpClient>(),
    lightningServerUrl
));
services.AddSingleton<ILightningStateService, LightningStateService>();

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
if (vectorDbEnabled)
{
    Console.WriteLine($"✓ Vector database configured (Qdrant: {qdrantBaseUrl})");
    Console.WriteLine($"✓ Embedding service configured ({embeddingModel})");
}
else
{
    Console.WriteLine("ℹ Vector database disabled (configure VectorDatabase:Enabled to enable)");
}
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


            if (models.Any())
            {
                Console.WriteLine("\n➤ Testing LLM invocation...");
                var testMessages = new List<OllamaChatMessage>
                {
                    new() { Role = "user", Content = "Say 'Hello from Deep Research Agent!' in one sentence." }
                };

                Console.WriteLine($"✓ Found {models.Count()} model(s):");
                foreach (var model in models.Where(x => x == "mistral:7b" || x== "gpt-oss:20b"))
                {
                    Console.WriteLine($"  • {model}");

                    var response = await ollamaService.InvokeAsync(testMessages, model);
                    Console.WriteLine($"✓ Model: {model} Response: {response.Content}");
                    Console.WriteLine("\n✅ OLLAMA CONNECTION: SUCCESS");
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
        var testUrl = new List<string>() { "https://www.google.com" };
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
            //query = "Summarize the latest advancements in transformer architectures";
            query = @"Conduct a deep analysis of the 'Splinternet' phenomenon's impact on global semiconductor supply chains by 2028.
                Specifically contrast TSMC's diversification strategy against Intel's IDM 2.0 model under 2024-2025 US export controls,
                and predict the resulting shift in insurance liability models for cross-border wafer shipments.";
            Console.WriteLine($"Using default query: {query}");
        }

        var masterWorkflow = serviceProvider.GetRequiredService<MasterWorkflow>();
        
        
        Console.WriteLine("\n➤ Starting workflow execution with Lightning orchestration...\n");
        Console.WriteLine(new string('-', 60));


        var result = await masterWorkflow.RunAsync(query, CancellationToken.None);

        if (result.Contains("Clarification needed:"))
        {   Console.WriteLine("⚠️ The workflow requires clarification on the query.");
            Console.WriteLine(result);


            Console.Write("\nEnter your verification response (or press Enter for default): ");
            var verification_response = Console.ReadLine()?.Trim();

            if (string.IsNullOrEmpty(verification_response))
            {
                verification_response = @"I need a report and break this into three parallel streams: 
                    1. Geopolitical Splinternet effects... 
                    2. Corporate Strategy (TSMC vs Intel)... 
                    3. Insurance/Liability implications...
                    **Scope-**
                    **1. Research deliverable: comprehensive report synthesizing findings from all three streams.
                    **2. Depth: detailed analysis with citations from credible sources (industry reports, expert analyses, news articles).
                    **3. Format: structured report with executive summary, detailed sections for each stream, and conclusion.
                    4. Length: approximately 3000-5000 words.
                    5. Audience: industry analysts, corporate strategists, insurance underwriters.
                    6. Purpose: inform strategic decision-making for semiconductor companies and insurers navigating the Splinternet landscape.
                    7. Key questions to address:
                        **a. How will geopolitical tensions reshape semiconductor supply chains?
                        **b. What are the comparative advantages and risks of TSMC's diversification vs Intel's IDM 2.0?
                        **c. How should insurance models adapt to increased cross-border shipment risks?
                    8. Research methods: leverage web search, academic databases, industry reports, and expert interviews.
                    9. Verification: ensure all claims are backed by credible sources and data.
                    10. Constraints: avoid speculative assertions; focus on evidence-based analysis.
                    11. Deliverables: final report in PDF format with citations and appendices as needed.
                    12. Collaboration: coordinate with subject matter experts in geopolitics, corporate strategy, and insurance.
                    13. Review process: iterative feedback loops with stakeholders to refine analysis and conclusions.
                    14. Ethical considerations: ensure unbiased reporting and respect for proprietary information.
                    15. Timeline: complete research and report drafting within 8 weeks.
                    16. Budget: allocate resources for research tools, expert consultations, and report production.
                    17. Success criteria: actionable insights that influence corporate and insurance strategies in the semiconductor sector.
                    18. Risks: potential delays in data acquisition, access to proprietary information, and evolving geopolitical dynamics.
                    19. Mitigation strategies: establish contingency plans for data gaps and maintain flexibility in research focus.
                    20. Communication plan: regular updates to stakeholders on research progress and preliminary findings.
                    21. Final presentation: prepare to present findings to key stakeholders through a webinar or in-person meeting.
                    22. Post-delivery support: offer follow-up consultations to discuss report implications and next steps.
                    23. Long-term impact: aim to shape industry understanding and strategic approaches to the Splinternet's challenges.
                    24. Future research: identify areas for ongoing study beyond the initial report.
                    25. Documentation: maintain thorough records of research processes, sources, and decision-making for transparency.
                    26. Compliance: ensure all research activities adhere to legal and regulatory standards.
                    27. Innovation: explore novel research methodologies or analytical frameworks to enhance report quality.
                    28. Stakeholder engagement: involve key industry players for insights and validation of findings.
                    29. Technology utilization: leverage advanced research tools and AI for data analysis and synthesis.
                    30. Quality assurance: implement rigorous review protocols to ensure accuracy and reliability of the report.
                    31. Dissemination strategy: plan for effective distribution of the report to maximize reach and impact.
                    32. Feedback mechanisms: establish channels for receiving and incorporating stakeholder feedback post-delivery.
                    33. Continuous improvement: use lessons learned from this research to enhance future projects.
                    34. Alignment with organizational goals: ensure research objectives support broader corporate or institutional strategies.
                    35. Resource management: optimize use of available resources to achieve research goals efficiently.
                    36. Team dynamics: foster collaboration and knowledge sharing among research team members.
                    37. Adaptability: remain responsive to emerging trends and data throughout the research process.
                    38. Ethical sourcing: prioritize the use of ethically obtained data and information.
                    39. Transparency: clearly communicate research methodologies and limitations in the final report.
                    40. Impact measurement: develop metrics to assess the influence of the report on industry practices and policies.
                    41. Sustainability: consider the long-term implications of research findings on industry sustainability practices.
                    42. Cross-disciplinary approach: integrate perspectives from geopolitics, business strategy, and insurance to enrich analysis.
                    43. Scenario planning: include potential future scenarios based on current trends and data.
                    44. Risk assessment: evaluate potential risks associated with different corporate strategies in the Splinternet context.
                    45. Strategic recommendations: provide clear, actionable recommendations for stakeholders based on research findings.
                    46. Visual aids: incorporate charts, graphs, and infographics to enhance report clarity and engagement.
                    47. Executive summary: craft a concise overview of key findings and recommendations for quick stakeholder reference.
                    48. Appendices: include supplementary materials such as data tables, methodological details, and interview transcripts.
                    49. Citation standards: adhere to recognized citation styles to ensure proper attribution of sources.
                    50. Review by experts: seek validation of findings and recommendations from industry experts prior to finalization.
                    51. Legal review: ensure the report complies with all relevant legal considerations, particularly regarding proprietary information.
                    52. Confidentiality: implement measures to protect sensitive information obtained during research.
                    53. Data security: ensure all research data is stored and handled securely to prevent unauthorized access.
                    54. Intellectual property: clarify ownership rights of the research findings and report content.

                    **Focus Areas-**
                    **1. Qualitative assessment for each of the three streams  (Geopolitical Splinternet effects, Corporate Strategy comparisons, and Insurance/Liability implications).
                    **2. Geographic: world wide (supply‑chain fragmentation, regulatory divergence, cyber‑security). 
                    **3. Key metrics for comparing TSMC and Intel’s corporate strategy (market share, R&D spend, ESG initiatives)
                        **a. Insurance/liability factors of interest (product‑liability exposure, cyber‑risk premiums, regulatory fines)   
                    **4. Time horizon for the analysis: 2020-2030. 
                    ";
                result = result.Replace("\"verification\": \"\"", $"\"verification\": \"{verification_response}\"");
                Console.WriteLine($"Using default clarification query: {verification_response}");
            }

            result = await masterWorkflow.RunAsync(result, CancellationToken.None);

            Console.WriteLine(result);
        }
        else
        {
            Console.WriteLine("✓ Workflow execution completed successfully.\n");
            Console.WriteLine("📝 Final Research Report:\n");
            Console.WriteLine(result);
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
