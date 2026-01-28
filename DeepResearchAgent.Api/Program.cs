using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Services.WebSearch;
using DeepResearchAgent.Services.Telemetry;
using DeepResearchAgent.Services.VectorDatabase;
using DeepResearchAgent.Workflows;
using DeepResearchAgent.Workflows.Extensions;
using DeepResearchAgent.Api.Extensions;
using DeepResearchAgent.Api.Services;
using DeepResearchAgent.Agents;
using DeepResearchAgent.Configuration;
using DeepResearchAgent.Models;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configuration - Add additional config files like the working DeepResearchAgent
var configuration = builder.Configuration;

configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.json"), optional: true, reloadOnChange: true);
configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.websearch.json"), optional: true, reloadOnChange: true);
configuration.AddJsonFile(Path.Combine(AppContext.BaseDirectory, "appsettings.apo.json"), optional: true, reloadOnChange: true);
configuration.AddEnvironmentVariables();
builder.Configuration.AddYamlFile("config.yml", optional: true, reloadOnChange: true);
builder.Configuration.AddEnvironmentVariables();

//configuration.AddJsonFile("appsettings.websearch.json", optional: true, reloadOnChange: true);
//configuration.AddJsonFile("appsettings.apo.json", optional: true, reloadOnChange: true);

var ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
var ollamaDefaultModel = configuration["Ollama:DefaultModel"] ?? "gpt-oss:20b";
var searxngBaseUrl = configuration["SearXNG:BaseUrl"] ?? "http://localhost:8080";
var crawl4aiBaseUrl = configuration["Crawl4AI:BaseUrl"] ?? "http://localhost:11235";
var lightningServerUrl = configuration["Lightning:ServerUrl"]
    ?? Environment.GetEnvironmentVariable("LIGHTNING_SERVER_URL")
    ?? "http://localhost:8090";

// APO configuration
var apoConfig = new LightningAPOConfig();
configuration.GetSection("Lightning:APO").Bind(apoConfig);

// Vector database configuration
var vectorDbEnabled = configuration.GetValue("VectorDatabase:Enabled", false);
var qdrantBaseUrl = configuration["VectorDatabase:Qdrant:BaseUrl"] ?? "http://localhost:6333";
var qdrantApiKey = configuration["VectorDatabase:Qdrant:ApiKey"] ?? configuration["ApiKeys:Qdrant"];
var qdrantCollectionName = configuration["VectorDatabase:Qdrant:CollectionName"] ?? "research";
var qdrantVectorDimension = configuration.GetValue("VectorDatabase:Qdrant:VectorDimension", 384);
var qdrantTimeoutMs = configuration.GetValue("VectorDatabase:Qdrant:TimeoutMs", 30000);
var embeddingModel = configuration["VectorDatabase:EmbeddingModel"] ?? "nomic-embed-text";
var embeddingApiUrl = configuration["VectorDatabase:EmbeddingApiUrl"] ?? ollamaBaseUrl;

// Core Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();

// API Services - Phase 3
builder.Services.AddApiServices();
builder.Services.AddApiCors("ApiCorsPolicy");
builder.Services.AddApiDocumentation();
builder.Services.AddApiCompression();
builder.Services.AddApiHealthChecks();
builder.Services.AddLogging();

// CORS Configuration for UI
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowUI", builder =>
    {
        builder.AllowAnyOrigin()
               .AllowAnyMethod()
               .AllowAnyHeader();
    });
});

// Register APO config
builder.Services.AddSingleton(apoConfig);

// Register LightningStore with proper configuration
builder.Services.AddSingleton<LightningStoreOptions>(sp => new LightningStoreOptions
{
    DataDirectory = configuration["LightningStore:DataDirectory"] ?? "data",
    FileName = configuration["LightningStore:FileName"] ?? "lightningstore.json",
    LightningServerUrl = lightningServerUrl,
    UseLightningServer = configuration.GetValue("LightningStore:UseLightningServer", true),
    ResourceNamespace = configuration["LightningStore:ResourceNamespace"] ?? "facts"
});

builder.Services.AddSingleton<ILightningStore>(sp => new LightningStore(
    sp.GetRequiredService<LightningStoreOptions>(),
    sp.GetRequiredService<IHttpClientFactory>().CreateClient()
));

builder.Services.AddSingleton<LightningStore>(sp => (LightningStore)sp.GetRequiredService<ILightningStore>());

// Core Services
builder.Services.AddSingleton<OllamaService>(_ => new OllamaService(
    baseUrl: ollamaBaseUrl,
    defaultModel: ollamaDefaultModel
));

builder.Services.AddSingleton<SearCrawl4AIService>(sp => new SearCrawl4AIService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    searxngBaseUrl,
    crawl4aiBaseUrl
));

// Register web search providers (SearXNG + Tavily) and resolver
builder.Services.AddHttpClient("SearXNG");
//builder.Services.AddHttpClient("TavilyClient");
builder.Services.AddWebSearchProviders(configuration);

// Register embedding service
builder.Services.AddSingleton<IEmbeddingService>(sp => new OllamaEmbeddingService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    baseUrl: embeddingApiUrl,
    model: embeddingModel,
    dimension: qdrantVectorDimension,
    logger: sp.GetService<Microsoft.Extensions.Logging.ILogger>()
));

// Register vector database service (Qdrant)
if (vectorDbEnabled)
{
    builder.Services.AddSingleton<IVectorDatabaseService>(sp => new QdrantVectorDatabaseService(
        sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
        new QdrantConfig
        {
            BaseUrl = qdrantBaseUrl,
            ApiKey = qdrantApiKey,
            CollectionName = qdrantCollectionName,
            VectorDimension = qdrantVectorDimension,
            TimeoutMs = qdrantTimeoutMs
        },
        sp.GetRequiredService<IEmbeddingService>(),
        logger: sp.GetService<Microsoft.Extensions.Logging.ILogger>()
    ));
}

// Register vector database factory (for multiple DB support)
builder.Services.AddSingleton<IVectorDatabaseFactory>(sp =>
{
    var factory = new VectorDatabaseFactory(sp.GetService<Microsoft.Extensions.Logging.ILogger>());
    
    if (vectorDbEnabled && sp.GetService<IVectorDatabaseService>() != null)
    {
        factory.RegisterVectorDatabase("qdrant", sp.GetRequiredService<IVectorDatabaseService>());
    }
    
    return factory;
});

// Register Agent-Lightning services
builder.Services.AddHttpClient<IAgentLightningService, AgentLightningService>();
builder.Services.AddSingleton<IAgentLightningService>(sp =>
{
    var httpClient = sp.GetRequiredService<IHttpClientFactory>().CreateClient(nameof(AgentLightningService));
    var apo = sp.GetRequiredService<LightningAPOConfig>();
    var metrics = sp.GetRequiredService<MetricsService>();
    
    // Configure HTTP client timeout based on APO config
    httpClient.Timeout = TimeSpan.FromSeconds(apo.ResourceLimits.TaskTimeoutSeconds);
    
    return new AgentLightningService(
        httpClient,
        lightningServerUrl,
        clientId: null,
        apo: apo,
        metrics: metrics);
});

builder.Services.AddSingleton<ILightningVERLService>(sp => new LightningVERLService(
    sp.GetRequiredService<IHttpClientFactory>().CreateClient(),
    lightningServerUrl
));
builder.Services.AddSingleton<ILightningStateService, LightningStateService>();

// Register APO auto-scaler as hosted service (if enabled)
builder.Services.AddHostedService<LightningApoScaler>();

// Metrics Service
builder.Services.AddSingleton<MetricsService>();

// Register supporting services for workflows
builder.Services.AddSingleton<StateManager>();
builder.Services.AddSingleton<WorkflowModelConfiguration>();

// Tool Invocation Service (required by agents) - singleton to be reused
builder.Services.AddSingleton<ToolInvocationService>(sp => new ToolInvocationService(
    sp.GetRequiredService<IWebSearchProvider>(),
    sp.GetRequiredService<OllamaService>(),
    sp.GetService<ILogger<ToolInvocationService>>()
));

// Phase 5 Support Services (required by ChatIntegrationService)
builder.Services.AddSingleton<StateTransitioner>(sp => new StateTransitioner(
    sp.GetService<ILogger<StateTransitioner>>()
));

builder.Services.AddSingleton<AgentErrorRecovery>(sp => new AgentErrorRecovery(
    sp.GetService<ILogger<AgentErrorRecovery>>(),
    maxRetries: 2,
    retryDelay: TimeSpan.FromSeconds(1)
));

// Register Agents (ResearcherAgent, AnalystAgent, ReportAgent)
builder.Services.AddSingleton<ResearcherAgent>(sp => new ResearcherAgent(
    sp.GetRequiredService<OllamaService>(),
    sp.GetRequiredService<ToolInvocationService>(),
    sp.GetService<ILogger<ResearcherAgent>>(),
    sp.GetRequiredService<MetricsService>()
));

builder.Services.AddSingleton<AnalystAgent>(sp => new AnalystAgent(
    sp.GetRequiredService<OllamaService>(),
    sp.GetRequiredService<ToolInvocationService>(),
    sp.GetService<ILogger<AnalystAgent>>(),
    sp.GetRequiredService<MetricsService>()
));

builder.Services.AddSingleton<ReportAgent>(sp => new ReportAgent(
    sp.GetRequiredService<OllamaService>(),
    sp.GetRequiredService<ToolInvocationService>(),
    sp.GetService<ILogger<ReportAgent>>(),
    sp.GetRequiredService<MetricsService>()
));

// Workflow Services with proper dependency injection
builder.Services.AddSingleton<ResearcherWorkflow>(sp => new ResearcherWorkflow(
    sp.GetRequiredService<ILightningStateService>(),
    sp.GetRequiredService<SearCrawl4AIService>(),
    sp.GetRequiredService<OllamaService>(),
    sp.GetRequiredService<LightningStore>(),
    sp.GetService<IVectorDatabaseService>(),
    sp.GetService<IEmbeddingService>(),
    sp.GetService<ILogger<ResearcherWorkflow>>(),
    sp.GetRequiredService<MetricsService>(),
    sp.GetService<IAgentLightningService>(),
    sp.GetService<LightningAPOConfig>()
));

builder.Services.AddSingleton<SupervisorWorkflow>(sp => new SupervisorWorkflow(
    sp.GetRequiredService<ILightningStateService>(),
    sp.GetRequiredService<ResearcherWorkflow>(),
    sp.GetRequiredService<OllamaService>(),
    sp.GetRequiredService<IWebSearchProvider>(),
    sp.GetRequiredService<LightningStore>(),
    sp.GetService<ILogger<SupervisorWorkflow>>(),
    sp.GetRequiredService<StateManager>(),
    sp.GetRequiredService<WorkflowModelConfiguration>(),
    sp.GetRequiredService<MetricsService>()
));

builder.Services.AddSingleton<MasterWorkflow>(sp => new MasterWorkflow(
    sp.GetRequiredService<ILightningStateService>(),
    sp.GetRequiredService<SupervisorWorkflow>(),
    sp.GetRequiredService<OllamaService>(),
    sp.GetRequiredService<IWebSearchProvider>(),
    sp.GetService<ILogger<MasterWorkflow>>(),
    sp.GetRequiredService<StateManager>(),
    sp.GetRequiredService<MetricsService>(),
    sp.GetService<ResearcherAgent>(),
    sp.GetService<AnalystAgent>(),
    sp.GetService<ReportAgent>()
));

// Chat Services (required by ChatController)
builder.Services.AddSingleton<IChatSessionService, ChatSessionService>();
builder.Services.AddSingleton<ChatIntegrationService>();

var app = builder.Build();

// Middleware Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

// Swagger - Always enabled (UI and JSON)
app.UseSwagger();
app.UseSwaggerUI(options =>
{
    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Deep Research Agent API v1");
    options.DocumentTitle = "Deep Research Agent API";
    options.RoutePrefix = string.Empty; // Set Swagger UI at root
    options.DisplayOperationId();
    options.DisplayRequestDuration();
    options.EnableTryItOutByDefault();
    options.EnableDeepLinking();
    options.EnableFilter();
    options.ShowExtensions();
    options.DefaultModelsExpandDepth(2);
    options.DefaultModelExpandDepth(2);
    options.DocExpansion(Swashbuckle.AspNetCore.SwaggerUI.DocExpansion.List);
});

// API Middleware
app.UseApiMiddleware();

// Response Compression
app.UseApiCompression();

// Enable CORS
app.UseCors("AllowUI");

app.UseHttpsRedirection();

// Health Checks
app.MapApiHealthChecks();

app.MapControllers();

app.Run();
