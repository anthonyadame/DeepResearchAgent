using DeepResearchAgent.Services;
using DeepResearchAgent.Services.StateManagement;
using DeepResearchAgent.Workflows;
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
    ?? "http://lightning-server:9090";

// Services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddMemoryCache();
builder.Services.AddHttpClient();
builder.Services.AddSingleton(sp => sp.GetRequiredService<IHttpClientFactory>().CreateClient());
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
builder.Services.AddSingleton<ResearcherWorkflow>();
builder.Services.AddSingleton<SupervisorWorkflow>();
builder.Services.AddSingleton<MasterWorkflow>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}

app.UseSwagger();
app.UseSwaggerUI();

app.UseHttpsRedirection();

app.MapControllers();

app.Run();
