# DeepResearchAgent.Api Configuration Alignment

## Summary
Aligned DeepResearchAgent.Api with the working DeepResearchAgent configuration to resolve service registration and configuration issues.

## Changes Made

### 1. Configuration Files Added
- **appsettings.websearch.json** - Web search provider configuration (SearXNG, Tavily)
- **appsettings.apo.json** - APO (Automatic Performance Optimization), vector database, LightningStore, and telemetry configuration

### 2. Program.cs Updates

#### Added Using Directives
- `DeepResearchAgent.Services.VectorDatabase`
- `DeepResearchAgent.Workflows.Extensions`
- `DeepResearchAgent.Models`

#### Configuration Loading
- Added loading of `appsettings.websearch.json`
- Added loading of `appsettings.apo.json`
- Added APO configuration binding
- Added vector database configuration variables

#### Service Registrations Added

**Configuration Services:**
- `LightningAPOConfig` - APO configuration singleton
- `LightningStoreOptions` - LightningStore configuration
- `StateManager` - State management for workflows
- `WorkflowModelConfiguration` - Model selection configuration

**Lightning Store:**
- `ILightningStore` with proper `LightningStoreOptions`
- Using `IHttpClientFactory` instead of direct `HttpClient`

**Web Search:**
- `AddWebSearchProviders(configuration)` - Configures SearXNG and Tavily providers
- HTTP clients for "SearXNG" and "TavilyClient"

**Vector Database (optional):**
- `IEmbeddingService` using `OllamaEmbeddingService`
- `IVectorDatabaseService` using `QdrantVectorDatabaseService` (when enabled)
- `IVectorDatabaseFactory` with Qdrant registration

**Agent-Lightning:**
- Updated `IAgentLightningService` with APO config and metrics
- HTTP client timeout configuration based on APO settings
- `LightningApoScaler` as hosted service

**Tools & Agents:**
- `ToolInvocationService` - Now a singleton (was being created inline for each agent)

**Workflows:**
- Updated `ResearcherWorkflow` with vector DB and APO parameters
- Updated `SupervisorWorkflow` with `StateManager` and `WorkflowModelConfiguration`
- Updated `MasterWorkflow` with `StateManager`

#### HTTP Client Factory Usage
- Replaced `AddSingleton<HttpClient>()` with proper `IHttpClientFactory` pattern
- All services now use `IHttpClientFactory.CreateClient()` for proper client management

### 3. appsettings.json Updates
- Added `LightningStore` configuration section
- Added `VectorDatabase` configuration section
- Added `WebSearch` configuration section
- Added `Telemetry` configuration section

### 4. Service Lifetime Fix (CRITICAL)

**Issue:** Service lifetime mismatch error when calling MasterWorkflow
```
"Cannot resolve scoped service 'DeepResearchAgent.Services.WebSearch.IWebSearchProvider' from root provider."
```

**Root Cause:** 
- Web search providers were registered as **Scoped** in `WebSearchProviderExtensions.cs`
- But they were being injected into **Singleton** workflows (MasterWorkflow, SupervisorWorkflow, etc.)
- Singleton services cannot depend on Scoped services

**Fix Applied:**
Changed all web search provider registrations from `AddScoped` to `AddSingleton` in `WebSearchProviderExtensions.cs`:
- `SearCrawl4AIAdapter`: Scoped → Singleton
- `TavilySearchService`: Scoped → Singleton
- `IWebSearchProviderResolver`: Scoped → Singleton
- `IWebSearchProvider`: Scoped → Singleton

**Justification:**
- Web search providers don't maintain per-request state
- They are stateless services that use IHttpClientFactory for HTTP calls
- Using Singleton lifetime is appropriate and required for injection into Singleton workflows
- This aligns with the architecture where workflows are long-lived singletons

## Configuration Comparison

### Working: DeepResearchAgent
- ✅ Loads appsettings.websearch.json
- ✅ Loads appsettings.apo.json
- ✅ Registers all required services
- ✅ Uses IHttpClientFactory pattern
- ✅ Configures APO and vector database

### Fixed: DeepResearchAgent.Api
- ✅ Now loads appsettings.websearch.json
- ✅ Now loads appsettings.apo.json
- ✅ Registers all required services
- ✅ Uses IHttpClientFactory pattern
- ✅ Configures APO and vector database
- ✅ Fixed service lifetime mismatch

## Key Differences Resolved

1. **Missing Configuration Files**: Added websearch and APO config files
2. **Service Lifetime Issues**: Fixed HTTP client to use factory pattern
3. **Missing Dependencies**: Added StateManager, WorkflowModelConfiguration, web search resolver
4. **Incomplete Service Registration**: Added vector DB, embedding, APO scaler services
5. **Tool Service Duplication**: Made ToolInvocationService a singleton instead of creating per agent
6. **Service Lifetime Mismatch**: Changed web search providers from Scoped to Singleton (CRITICAL FIX)

## Service Lifetime Architecture

```
┌─────────────────────────────────────────────────────┐
│                  Singleton Services                  │
├─────────────────────────────────────────────────────┤
│ - MasterWorkflow                                     │
│ - SupervisorWorkflow                                 │
│ - ResearcherWorkflow                                 │
│ - Agents (ResearcherAgent, AnalystAgent, etc.)       │
│ - IWebSearchProvider ← Fixed to Singleton            │
│ - IWebSearchProviderResolver ← Fixed to Singleton    │
│ - OllamaService                                      │
│ - SearCrawl4AIService                                │
│ - LightningStore                                     │
│ - MetricsService                                     │
└─────────────────────────────────────────────────────┘
```

## WebSearch Provider Configuration

The API now supports configurable web search providers via `appsettings.websearch.json`:

```json
{
  "WebSearch": {
    "Provider": "searxng",  // or "tavily"
    "TavilyApiKey": "ultrasecretkey",
    "RequestTimeoutSeconds": 30,
    "DefaultMaxResults": 10
  }
}
```

Set `WebSearch:Provider` to `searxng` to select SearXNG.

## Build Status
✅ Build successful - all errors resolved

## Issues Resolved
- ✅ Missing configuration files
- ✅ Service registration gaps
- ✅ HTTP client lifetime management
- ✅ Service lifetime mismatch (Scoped → Singleton)
- ✅ Dependency injection errors

## Next Steps
- Test the API endpoints to ensure proper functionality
- Verify web search provider selection works correctly
- Confirm workflow execution with all dependencies properly injected
- Monitor APO and metrics services functionality
