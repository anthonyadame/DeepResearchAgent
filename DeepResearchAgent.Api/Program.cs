using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Services.WebSearch;
using DeepResearchAgent.Services.Telemetry;
using DeepResearchAgent.Workflows;
using DeepResearchAgent.Api.Extensions;
using DeepResearchAgent.Api.Services;
using DeepResearchAgent.Agents; // Add this
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplication.CreateBuilder(args);

// Configuration
var configuration = builder.Configuration;

var ollamaBaseUrl = configuration["Ollama:BaseUrl"] ?? "http://localhost:11434";
var ollamaDefaultModel = configuration["Ollama:DefaultModel"] ?? "gpt-oss:20b";
var searxngBaseUrl = configuration["SearXNG:BaseUrl"] ?? "http://localhost:8080";
var crawl4aiBaseUrl = configuration["Crawl4AI:BaseUrl"] ?? "http://localhost:11235";
var lightningServerUrl = configuration["Lightning:ServerUrl"]
    ?? Environment.GetEnvironmentVariable("LIGHTNING_SERVER_URL")
    ?? "http://localhost:8090";

// Core Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient());

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

// DLL Services (original)
builder.Services.AddSingleton<OllamaService>(_ => new OllamaService(
    baseUrl: ollamaBaseUrl,
    defaultModel: ollamaDefaultModel
));
builder.Services.AddSingleton<SearCrawl4AIService>(sp => new SearCrawl4AIService(
    sp.GetRequiredService<HttpClient>(),
    searxngBaseUrl,
    crawl4aiBaseUrl
));
builder.Services.AddSingleton<LightningStore>();
builder.Services.AddSingleton<IAgentLightningService>(sp => new AgentLightningService(
    sp.GetRequiredService<HttpClient>(),
    lightningServerUrl
));
builder.Services.AddSingleton<ILightningVERLService>(sp => new LightningVERLService(
    sp.GetRequiredService<HttpClient>(),
    lightningServerUrl
));
builder.Services.AddSingleton<ILightningStateService, LightningStateService>();

// Metrics Service
builder.Services.AddSingleton<MetricsService>();

// Web Search Provider (required by workflows)
builder.Services.AddSingleton<IWebSearchProvider>(sp => new SearCrawl4AIAdapter(
    sp.GetRequiredService<SearCrawl4AIService>(),
    sp.GetRequiredService<ILogger<SearCrawl4AIAdapter>>()
));

// Tool Invocation Service (required by agents)
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
    vectorDb: null,
    embeddingService: null,
    sp.GetRequiredService<ILogger<ResearcherWorkflow>>(),
    sp.GetRequiredService<MetricsService>(),
    sp.GetRequiredService<IAgentLightningService>(),
    apoConfig: null
));

builder.Services.AddSingleton<SupervisorWorkflow>(sp => new SupervisorWorkflow(
    sp.GetRequiredService<ILightningStateService>(),
    sp.GetRequiredService<ResearcherWorkflow>(),
    sp.GetRequiredService<OllamaService>(),
    sp.GetRequiredService<IWebSearchProvider>(),
    sp.GetRequiredService<LightningStore>(),
    sp.GetRequiredService<ILogger<SupervisorWorkflow>>(),
    stateManager: null,
    modelConfig: null,
    sp.GetRequiredService<MetricsService>()
));

builder.Services.AddSingleton<MasterWorkflow>(sp => new MasterWorkflow(
    sp.GetRequiredService<ILightningStateService>(),
    sp.GetRequiredService<SupervisorWorkflow>(),
    sp.GetRequiredService<OllamaService>(),
    sp.GetRequiredService<IWebSearchProvider>(),
    sp.GetRequiredService<ILogger<MasterWorkflow>>(),
    stateManager: null,
    sp.GetRequiredService<MetricsService>(),
    researcherAgent: null,
    analystAgent: null,
    reportAgent: null
));

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
