# Service Registration Quick Reference

## Registered Services in Program.cs

### Core Services
```
✅ OllamaService          - LLM integration
✅ HttpClient             - HTTP communication
✅ SearCrawl4AIService    - Web search and scraping
✅ MetricsService         - Observability metrics (SINGLETON)
```

### State & Storage
```
✅ LightningStoreOptions  - Configuration
✅ ILightningStore        - Fact persistence
✅ LightningStore         - Concrete implementation
✅ ILightningStateService - Lightning state management
✅ LightningStateService  - Concrete implementation
✅ StateManager           - State snapshot tracking
```

### Search & Embedding
```
✅ IWebSearchProvider              - Web search abstraction
✅ IWebSearchProviderResolver      - Multi-provider support
✅ IEmbeddingService               - Text embeddings
✅ IVectorDatabaseService          - Vector storage (optional)
✅ IVectorDatabaseFactory          - Vector DB factory
```

### Agent-Lightning Integration
```
✅ IAgentLightningService   - Lightning client
✅ ILightningVERLService    - VERL verification
✅ LightningAPOConfig       - APO configuration
✅ LightningApoScaler       - Auto-scaling service
```

### Phase 4 Agents
```
✅ ResearcherAgent  - Research orchestration
✅ AnalystAgent     - Analysis and synthesis
✅ ReportAgent      - Report formatting
```

### Workflows
```
✅ ResearcherWorkflow    - Focused research
✅ SupervisorWorkflow    - Diffusion loop
✅ MasterWorkflow        - Master orchestration
```

### Configuration
```
✅ WorkflowModelConfiguration - Model selection
```

## Dependency Resolution Order

1. **Configuration** (appsettings.json, environment variables)
2. **Core Services** (HTTP, LLM, Metrics)
3. **Storage & State** (LightningStore, StateManager)
4. **Search & Embedding** (Web search, vector DB)
5. **Agent-Lightning** (APO, VERL, state service)
6. **Phase 4 Agents** (ResearcherAgent, AnalystAgent, ReportAgent)
7. **Workflows** (ResearcherWorkflow → SupervisorWorkflow → MasterWorkflow)
8. **Build ServiceProvider** (all services fully initialized)

## Common Issues & Solutions

### Issue: "LightningStore not found"
**Solution:** Ensure `LightningStoreOptions` is registered before `ILightningStore`

### Issue: "MetricsService is null"
**Solution:** Register as singleton: `services.AddSingleton<MetricsService>();`

### Issue: "Agent constructor fails"
**Solution:** Ensure all agent dependencies are registered:
- OllamaService ✓
- ToolInvocationService (created inline)
- ILogger<TAgent> (optional, GetService)
- MetricsService ✓

### Issue: "StateManager not available"
**Solution:** Register explicitly: `services.AddSingleton<StateManager>();`

### Issue: "WorkflowModelConfiguration missing"
**Solution:** Register with default: `services.AddSingleton<WorkflowModelConfiguration>();`

## Singleton vs Transient Decisions

| Service | Lifetime | Reason |
|---------|----------|--------|
| MetricsService | Singleton | Shared metrics across all components |
| LightningStore | Singleton | Persistent fact storage |
| StateManager | Singleton | Shared state snapshots |
| Workflows | Singleton | Single execution pipeline |
| Agents | Singleton | Reusable agent instances |
| OllamaService | Singleton | Shared LLM connection |
| HttpClient | Singleton | Connection pooling |
| ToolInvocationService | Created inline | Per-agent instance |

## Configuration Points

### Lightning Server
```csharp
var lightningServerUrl = configuration["Lightning:ServerUrl"] 
    ?? "http://localhost:8090";
```

### APO Configuration
```csharp
var apoConfig = new LightningAPOConfig();
configuration.GetSection("Lightning:APO").Bind(apoConfig);
```

### LightningStore Configuration
```csharp
new LightningStoreOptions
{
    DataDirectory = "data",
    FileName = "lightningstore.json",
    UseLightningServer = true,
    ResourceNamespace = "facts"
}
```

### Vector Database Configuration
```csharp
var vectorDbEnabled = configuration.GetValue("VectorDatabase:Enabled", false);
```

## Debugging Registration

### Verify Service Registration
```csharp
var descriptor = services.FirstOrDefault(d => 
    d.ServiceType == typeof(LightningStore));
Console.WriteLine($"LightningStore: {descriptor?.Lifetime}");
```

### Check Service Resolution
```csharp
try {
    var service = serviceProvider.GetRequiredService<LightningStore>();
    Console.WriteLine("✓ LightningStore resolved successfully");
} catch (Exception ex) {
    Console.WriteLine($"❌ Resolution failed: {ex.Message}");
}
```

### View All Registered Services
```csharp
foreach (var descriptor in services)
{
    Console.WriteLine($"{descriptor.ServiceType.Name}: {descriptor.Lifetime}");
}
```

## Performance Considerations

| Service | Memory | CPU | Network |
|---------|--------|-----|---------|
| MetricsService | Low | Low | None |
| LightningStore | Medium | Low | High (if Lightning enabled) |
| StateManager | Low | Low | None |
| OllamaService | High | High | Local |
| HttpClient | Low | Low | Variable |

## Adding New Services

1. **Define the service/interface**
2. **Register in Program.cs** after core services
3. **Add using directive** if needed
4. **Inject into workflows** via constructor
5. **Test service resolution** in health checks

## Health Check Endpoints

- Ollama: `GET http://localhost:11434/api/tags`
- SearXNG: `GET http://localhost:8080/healthz`
- Crawl4AI: Service initialization check
- Lightning: `GET http://localhost:8090/health`

## Related Documentation

- `DEPENDENCY_INJECTION_AUDIT.md` - Full audit report
- `CIRCUIT_BREAKER_GUIDE.md` - Fault tolerance configuration
- `CIRCUIT_BREAKER_SUMMARY.md` - Implementation summary
- `APO_INTEGRATION_SUMMARY.md` - APO features
- `AGENT_LIGHTNING_INTEGRATION.md` - Lightning integration details

## Quick Checklist

- [x] LightningStore registered
- [x] MetricsService registered as singleton
- [x] Phase 4 agents registered
- [x] Supporting services registered
- [x] Workflow registrations updated
- [x] Using directives complete
- [x] Build successful
- [x] No orphaned dependencies

**Status: ✅ COMPLETE AND READY**
