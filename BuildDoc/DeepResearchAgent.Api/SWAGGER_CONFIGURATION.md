# Swagger/OpenAPI Configuration

## Overview
The Deep Research Agent API now features a comprehensive Swagger UI implementation that serves as the default landing page for the application.

## Key Improvements

### 1. **Swagger as Default Startup Page**
- **Route**: `http://localhost:<port>/` (root URL)
- Previously required navigating to `/swagger`
- Now opens directly when you start the application

### 2. **Enhanced Documentation**
```csharp
Title: "Deep Research Agent API"
Version: "v1.0"
Contact: Deep Research Agent Team
License: MIT
GitHub: https://github.com/anthonyadame/DeepResearchAgent
```

### 3. **Rich API Description**
The Swagger page includes:
- **5-Tier Architecture** overview
  1. Workflows (Master, Supervisor, Researcher)
  2. Agents (6 specialized agents)
  3. Core Services (LLM, Search, State Management)
  4. Operations (Tools & Metrics)
  5. Health Monitoring

- **Key Features**:
  - Multi-agent research orchestration
  - Vector-based knowledge retrieval
  - State management with Lightning
  - Real-time metrics and monitoring
  - Extensible tool framework

- **Getting Started Guide**:
  - Quick links to essential endpoints
  - Workflow usage patterns
  - Agent-specific tasks

### 4. **XML Documentation**
Enabled in `DeepResearchAgent.Api.csproj`:
```xml
<GenerateDocumentationFile>true</GenerateDocumentationFile>
<NoWarn>$(NoWarn);1591</NoWarn>
```

This automatically includes XML comments from:
- Controllers
- DTOs
- Request/Response models
- Service interfaces

### 5. **Enhanced UI Features**

```csharp
options.DisplayOperationId();          // Show operation IDs
options.DisplayRequestDuration();      // Show request timing
options.EnableTryItOutByDefault();     // "Try it out" enabled by default
options.EnableDeepLinking();           // Direct links to operations
options.EnableFilter();                // Search/filter endpoints
options.ShowExtensions();              // Show custom extensions
options.DefaultModelsExpandDepth(2);   // Expand models 2 levels deep
options.DocExpansion(DocExpansion.List); // Show endpoints in list view
```

### 6. **Endpoint Organization**

- **Grouped by Controllers**: Automatically groups endpoints by controller name
- **Ordered Alphabetically**: Operations sorted by relative path
- **Tagged Properly**: Uses controller names as tags

## Accessing Swagger

### Development
```
http://localhost:5000/
https://localhost:5001/
```

### Production
```
https://your-api-domain.com/
```

### Swagger JSON
```
https://your-api-domain.com/swagger/v1/swagger.json
```

## Endpoints Available

### Workflows
- `POST /api/workflows/master` - Complete research pipeline
- `POST /api/workflows/supervisor` - Iterative refinement
- `POST /api/workflows/researcher` - Focused research

### Agents
- `POST /api/agents/clarify` - Query clarification
- `POST /api/agents/research-brief` - Research brief generation
- `POST /api/agents/researcher` - Research execution
- `POST /api/agents/analyst` - Analysis
- `POST /api/agents/draft-report` - Draft generation
- `POST /api/agents/report` - Final report

### Core Services
- `POST /api/core/llm/invoke` - LLM invocation
- `POST /api/core/search` - Web search
- `POST /api/core/scrape` - Web scraping
- `POST /api/core/state/manage` - State management
- `POST /api/core/vectors/search` - Vector search

### Health & Operations
- `GET /health` - Overall health
- `GET /health/live` - Liveness probe
- `POST /api/operations/tools/invoke` - Tool invocation
- `GET /api/operations/metrics` - System metrics

## Configuration Files

### Program.cs
- Swagger registration: `builder.Services.AddApiDocumentation()`
- Swagger middleware: `app.UseSwagger()` and `app.UseSwaggerUI(...)`
- **RoutePrefix**: Set to `string.Empty` for root access

### ServiceCollectionExtensions.cs
- Contains `AddApiDocumentation()` method
- Configures SwaggerGen options
- Includes XML documentation
- Sets up endpoint grouping and ordering

### DeepResearchAgent.Api.csproj
- Enables XML documentation generation
- Suppresses CS1591 warnings (missing XML comments)

## Best Practices

1. **Always document public APIs** with XML comments
2. **Use descriptive operation IDs** for client generation
3. **Group related endpoints** using controller names
4. **Provide examples** in request/response DTOs
5. **Test endpoints** directly from Swagger UI

## Future Enhancements

Consider adding:
- [ ] Request/Response examples
- [ ] Authentication/Authorization documentation
- [ ] Rate limiting information
- [ ] Versioning strategy
- [ ] Multiple Swagger documents for different API versions
- [ ] Custom CSS styling for branding

## Troubleshooting

### XML Documentation Not Showing
- Verify `<GenerateDocumentationFile>true</GenerateDocumentationFile>` in `.csproj`
- Rebuild the project
- Check XML file is generated in output directory

### Swagger UI Not Loading
- Ensure `app.UseSwagger()` is called before `app.UseSwaggerUI()`
- Check middleware order in `Program.cs`
- Verify `RoutePrefix` is set to `string.Empty`

### Missing Endpoints
- Ensure controllers inherit from `ControllerBase` or `Controller`
- Add `[ApiController]` attribute to controllers
- Verify routes are properly defined with `[Route]` attributes

## Resources

- [Swashbuckle Documentation](https://github.com/domaindrivendev/Swashbuckle.AspNetCore)
- [OpenAPI Specification](https://swagger.io/specification/)
- [ASP.NET Core Web API Documentation](https://learn.microsoft.com/en-us/aspnet/core/tutorials/web-api-help-pages-using-swagger)
