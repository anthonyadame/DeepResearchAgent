# 🌐 WEB API DOCUMENTATION

**Version**: 1.0  
**Project**: DeepResearchAgent.Api  
**Framework**: ASP.NET Core 8.0  
**Port**: 5000 (HTTP) / 5001 (HTTPS)  
**Updated**: 2026-01-16

---

## Quick Start

### Running the Web API

```bash
cd DeepResearchAgent.Api
dotnet run
```

The API will be available at `http://localhost:5000` with interactive documentation at `/swagger`

### Verify API is Running

```bash
# Check if API is responding
curl http://localhost:5000/api/health/all

# Expected: HTTP 200 OK with health status
```

---

## API Overview

The Deep Research Agent Web API provides RESTful endpoints to:
- Check health of all external services
- Execute research workflows
- Stream workflow updates in real-time
- Monitor system status

**Base URL**: `http://localhost:5000`  
**API Route Prefix**: `/api`  
**Response Format**: JSON

---

## Endpoints Reference

### 1. Health Check Endpoints

Health check endpoints verify external service connectivity. All return HTTP 200 if healthy, 503 if unhealthy.

#### Check Ollama Service

```
GET /api/health/ollama
```

**Description**: Verify Ollama LLM service is running and responsive

**Response** (200 OK):
```json
{
  "endpoint": "http://localhost:11434",
  "healthy": true,
  "models": [
    {
      "name": "gpt-oss:20b",
      "modifiedAt": "2026-01-16T10:00:00Z",
      "size": 12000000000
    }
  ],
  "testResponse": "Hello from Deep Research Agent! I'm ready to assist with research tasks.",
  "error": null
}
```

**Response** (503 Service Unavailable):
```json
{
  "endpoint": "http://localhost:11434",
  "healthy": false,
  "models": null,
  "testResponse": null,
  "error": "Failed to connect to Ollama at http://localhost:11434: Connection refused"
}
```

**Status Codes**:
- `200 OK` - Ollama is healthy and responsive
- `503 Service Unavailable` - Ollama is not responding or misconfigured

---

#### Check SearXNG Service

```
GET /api/health/searxng
```

**Description**: Verify SearXNG web search service is running

**Response** (200 OK):
```json
{
  "endpoint": "http://localhost:8080",
  "healthy": true,
  "sampleResponseLength": 15234,
  "error": null
}
```

**Response** (503 Service Unavailable):
```json
{
  "endpoint": "http://localhost:8080",
  "healthy": false,
  "sampleResponseLength": 0,
  "error": "Failed to connect to SearXNG at http://localhost:8080"
}
```

**Status Codes**:
- `200 OK` - SearXNG is healthy
- `503 Service Unavailable` - SearXNG is not responding

---

#### Check Crawl4AI Service

```
GET /api/health/crawl4ai
```

**Description**: Verify Crawl4AI web scraping service is running

**Response** (200 OK):
```json
{
  "endpoint": "http://localhost:11235",
  "healthy": true,
  "apiVersion": "1.0.0",
  "error": null
}
```

**Response** (503 Service Unavailable):
```json
{
  "endpoint": "http://localhost:11235",
  "healthy": false,
  "apiVersion": null,
  "error": "Failed to connect to Crawl4AI at http://localhost:11235"
}
```

**Status Codes**:
- `200 OK` - Crawl4AI is healthy
- `503 Service Unavailable` - Crawl4AI is not responding

---

#### Check Lightning Server

```
GET /api/health/lightning
```

**Description**: Verify Agent-Lightning server (APO + VERL) is running

**Response** (200 OK):
```json
{
  "endpoint": "http://localhost:9090",
  "healthy": true,
  "apoEnabled": true,
  "verlEnabled": true,
  "error": null
}
```

**Response** (503 Service Unavailable):
```json
{
  "endpoint": "http://localhost:9090",
  "healthy": false,
  "apoEnabled": false,
  "verlEnabled": false,
  "error": "Failed to connect to Lightning Server at http://localhost:9090"
}
```

**Status Codes**:
- `200 OK` - Lightning Server is healthy (APO & VERL available)
- `503 Service Unavailable` - Lightning Server is not responding

---

#### Check All Services

```
GET /api/health/all
```

**Description**: Run health checks on all external services simultaneously

**Response** (200 OK - All Services Healthy):
```json
{
  "timestamp": "2026-01-16T10:30:45.123Z",
  "allHealthy": true,
  "ollama": {
    "endpoint": "http://localhost:11434",
    "healthy": true,
    "models": [...],
    "testResponse": "...",
    "error": null
  },
  "searxng": {
    "endpoint": "http://localhost:8080",
    "healthy": true,
    "sampleResponseLength": 15234,
    "error": null
  },
  "crawl4ai": {
    "endpoint": "http://localhost:11235",
    "healthy": true,
    "apiVersion": "1.0.0",
    "error": null
  },
  "lightning": {
    "endpoint": "http://localhost:9090",
    "healthy": true,
    "apoEnabled": true,
    "verlEnabled": true,
    "error": null
  }
}
```

**Response** (503 Service Unavailable - One or More Services Down):
```json
{
  "timestamp": "2026-01-16T10:30:45.123Z",
  "allHealthy": false,
  "ollama": {
    "endpoint": "http://localhost:11434",
    "healthy": true,
    "error": null
  },
  "searxng": {
    "endpoint": "http://localhost:8080",
    "healthy": false,
    "error": "Connection refused"
  },
  "crawl4ai": {
    "endpoint": "http://localhost:11235",
    "healthy": true,
    "error": null
  },
  "lightning": {
    "endpoint": "http://localhost:9090",
    "healthy": true,
    "error": null
  }
}
```

**Status Codes**:
- `200 OK` - All services are healthy
- `503 Service Unavailable` - One or more services are unhealthy

**Response Time**: Typically 100-500ms (depends on service responsiveness)

---

### 2. Workflow Execution Endpoints

#### Run Research Workflow

```
POST /api/workflow/run
```

**Description**: Execute the Master Workflow for a research query. Returns updates as they stream in.

**Request**:
```json
{
  "query": "What is machine learning and how does it differ from deep learning?"
}
```

**Request Parameters**:
| Parameter | Type | Required | Description |
|-----------|------|----------|-------------|
| `query` | string | No | Research query to execute. Default: "Summarize the latest advancements in transformer architectures" |

**Response** (200 OK):
```json
{
  "query": "What is machine learning?",
  "updates": [
    "🔍 Clarifying research query with user...",
    "📝 Writing research brief...",
    "🔎 Conducting research on machine learning...",
    "📊 Analyzing and refining results...",
    "✅ Research complete. Final report: Machine learning is...",
  ],
  "completedAt": "2026-01-16T10:35:22.456Z",
  "totalDuration": 245000
}
```

**Response Fields**:
| Field | Type | Description |
|-------|------|-------------|
| `query` | string | The original research query |
| `updates` | string[] | Array of status updates during execution |
| `completedAt` | datetime | When the workflow completed |
| `totalDuration` | integer | Total execution time in milliseconds |

**Status Codes**:
- `200 OK` - Workflow completed successfully
- `400 Bad Request` - Invalid request (missing required fields)
- `500 Internal Server Error` - Workflow execution failed
- `504 Gateway Timeout` - Workflow took too long (> 5 minutes)

**Response Time**: Typically 2-5 minutes depending on research complexity

**Example cURL Request**:
```bash
curl -X POST http://localhost:5000/api/workflow/run \
  -H "Content-Type: application/json" \
  -d '{
    "query": "What are the latest developments in artificial intelligence?"
  }'
```

**Example PowerShell Request**:
```powershell
$body = @{
    query = "What are the latest developments in artificial intelligence?"
} | ConvertTo-Json

Invoke-WebRequest -Uri "http://localhost:5000/api/workflow/run" `
  -Method Post `
  -ContentType "application/json" `
  -Body $body
```

**Example Python Request**:
```python
import requests

response = requests.post(
    'http://localhost:5000/api/workflow/run',
    json={'query': 'What are the latest developments in artificial intelligence?'},
    timeout=300
)

result = response.json()
print(f"Query: {result['query']}")
print(f"Updates: {len(result['updates'])} steps")
print(f"Duration: {result['totalDuration']}ms")
```

---

## Request/Response Models

### RunWorkflowRequest

```csharp
public class RunWorkflowRequest
{
    /// <summary>
    /// The research query to execute
    /// </summary>
    public string? Query { get; set; }
}
```

### RunWorkflowResponse

```csharp
public class RunWorkflowResponse
{
    /// <summary>
    /// The research query that was executed
    /// </summary>
    public string Query { get; set; }

    /// <summary>
    /// List of status updates during workflow execution
    /// </summary>
    public List<string> Updates { get; set; }

    /// <summary>
    /// When the workflow completed
    /// </summary>
    public DateTime CompletedAt { get; set; }

    /// <summary>
    /// Total execution time in milliseconds
    /// </summary>
    public long TotalDuration { get; set; }
}
```

### HealthSummaryResult

```csharp
public class HealthSummaryResult
{
    public DateTime Timestamp { get; set; }
    public bool AllHealthy { get; set; }
    public OllamaHealthResult Ollama { get; set; }
    public SearxHealthResult Searxng { get; set; }
    public CrawlHealthResult Crawl4AI { get; set; }
    public LightningHealthResult Lightning { get; set; }
}
```

---

## Configuration

The Web API is configured via `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "Ollama": {
    "BaseUrl": "http://localhost:11434",
    "DefaultModel": "gpt-oss:20b"
  },
  "SearXNG": {
    "BaseUrl": "http://localhost:8080"
  },
  "Crawl4AI": {
    "BaseUrl": "http://localhost:11235"
  },
  "Lightning": {
    "ServerUrl": "http://lightning-server:9090"
  }
}
```

### Configuration Options

| Setting | Default | Description |
|---------|---------|-------------|
| `Ollama:BaseUrl` | http://localhost:11434 | URL of Ollama service |
| `Ollama:DefaultModel` | gpt-oss:20b | LLM model to use for inference |
| `SearXNG:BaseUrl` | http://localhost:8080 | URL of SearXNG service |
| `Crawl4AI:BaseUrl` | http://localhost:11235 | URL of Crawl4AI service |
| `Lightning:ServerUrl` | http://lightning-server:9090 | URL of Lightning Server (APO/VERL) |

### Environment Variables

Override configuration using environment variables:

```bash
# Set Ollama base URL
export OLLAMA_BASEURL=http://my-ollama-server:11434

# Set Lightning Server URL
export LIGHTNING_SERVER_URL=http://my-lightning-server:9090

# Start API
dotnet run
```

---

## Swagger/OpenAPI Documentation

Interactive API documentation is available at:

```
http://localhost:5000/swagger
```

Features:
- ✅ Browse all endpoints
- ✅ Try out endpoints in browser
- ✅ View request/response models
- ✅ See example values
- ✅ Download OpenAPI specification

**Access Swagger**:
1. Start the Web API: `dotnet run`
2. Open browser: http://localhost:5000/swagger
3. Click on any endpoint to expand
4. Fill in parameters and click "Try it out"

---

## Error Handling

### Common Error Responses

#### 400 Bad Request
Returned when request is invalid (missing required fields, wrong format)

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.5.1",
  "title": "Bad Request",
  "status": 400,
  "detail": "The Query field is required."
}
```

#### 500 Internal Server Error
Returned when workflow execution fails unexpectedly

```json
{
  "type": "https://tools.ietf.org/html/rfc7231#section-6.6.1",
  "title": "Internal Server Error",
  "status": 500,
  "detail": "An error occurred while executing the workflow: ...",
  "traceId": "0HMVK0T3G2J5D:00000001"
}
```

#### 503 Service Unavailable
Returned when external services are not responding (health check endpoints)

```json
{
  "endpoint": "http://localhost:11434",
  "healthy": false,
  "error": "Failed to connect to service"
}
```

---

## Performance & Limits

### Request Limits

| Resource | Limit | Notes |
|----------|-------|-------|
| Query Length | 10,000 characters | Validated on input |
| Response Size | 10 MB | Large reports may exceed this |
| Timeout | 5 minutes | Workflows exceeding this will timeout |
| Concurrent Requests | Unlimited | Limited by system resources |

### Performance Expectations

| Operation | Typical Duration | Range |
|-----------|------------------|-------|
| Health Check (single) | 100 ms | 50-500 ms |
| Health Check (all) | 400 ms | 200-1000 ms |
| Workflow Execution | 3 minutes | 2-5 minutes |
| Workflow with APO | 2.5 minutes | 1.5-4 minutes |

### Memory Usage

| Component | Idle | During Workflow |
|-----------|------|-----------------|
| Web API | ~100 MB | ~200-400 MB |
| With Workflow | ~200 MB | ~800 MB - 1 GB |

---

## Deployment

### Docker

Build and run the API in a Docker container:

```dockerfile
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS base
WORKDIR /app
EXPOSE 5000 5001

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src
COPY ["DeepResearchAgent.Api/DeepResearchAgent.Api.csproj", "DeepResearchAgent.Api/"]
COPY ["DeepResearchAgent/DeepResearchAgent.csproj", "DeepResearchAgent/"]
RUN dotnet restore "DeepResearchAgent.Api/DeepResearchAgent.Api.csproj"
COPY . .
RUN dotnet build "DeepResearchAgent.Api/DeepResearchAgent.Api.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "DeepResearchAgent.Api/DeepResearchAgent.Api.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "DeepResearchAgent.Api.dll"]
```

### Docker Compose

Run API with supporting services:

```yaml
version: '3.8'
services:
  deep-research-api:
    build: .
    ports:
      - "5000:5000"
      - "5001:5001"
    environment:
      - OLLAMA_BASEURL=http://ollama:11434
      - LIGHTNING_SERVER_URL=http://lightning-server:9090
    depends_on:
      - ollama
      - lightning-server
  
  ollama:
    image: ollama/ollama:latest
    ports:
      - "11434:11434"
  
  lightning-server:
    image: lightning-server:latest
    ports:
      - "9090:9090"
```

---

## Monitoring & Logging

### Logging

Logs are output to the console and can be configured in `appsettings.json`:

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning",
      "DeepResearchAgent": "Debug"
    }
  }
}
```

Log levels (from lowest to highest detail):
- `Critical` - Application is failing
- `Error` - Significant error occurred
- `Warning` - Warning about potential issues
- `Information` - General informational messages
- `Debug` - Detailed debug information

### Health Monitoring

Use health check endpoints to monitor system:

```bash
# Health check script
#!/bin/bash

while true; do
  curl -s http://localhost:5000/api/health/all | jq '.allHealthy'
  sleep 30
done
```

### Metrics

Recommended monitoring metrics:
- Request count per endpoint
- Request latency (p50, p95, p99)
- Error rate
- Service health status
- Memory usage
- CPU usage

Integrate with Prometheus, Grafana, or AppInsights for production monitoring.

---

## Security Considerations

### Current State
- ✅ No authentication required (suitable for internal use)
- ✅ CORS enabled for localhost
- ✅ HTTPS redirect in development

### For Production

Before deploying to production, consider:

1. **Authentication**: Add API key or OAuth2
2. **Authorization**: Implement role-based access control
3. **HTTPS**: Require HTTPS only
4. **Rate Limiting**: Add rate limiting middleware
5. **CORS**: Restrict allowed origins
6. **Logging**: Enable request/response logging
7. **Monitoring**: Setup alerts for failures
8. **Backup**: Regular backups of knowledge base

---

## Troubleshooting

### API Won't Start

**Error**: `System.Net.Sockets.SocketException: Address already in use`

**Solution**:
```bash
# Find what's using port 5000
netstat -ano | findstr :5000

# Kill the process
taskkill /PID <PID> /F

# Or change port in appsettings.json
```

### Health Checks Failing

**Error**: All health checks return 503

**Solution**:
1. Verify all services are running (Ollama, SearXNG, Crawl4AI, Lightning)
2. Check firewall rules
3. Verify correct URLs in appsettings.json
4. Check logs for specific error messages

### Workflows Timing Out

**Error**: 504 Gateway Timeout on /api/workflow/run

**Solution**:
1. Increase timeout in nginx/reverse proxy if using one
2. Check if Ollama is slow (reduce concurrent requests)
3. Reduce iteration limits in SupervisorWorkflow
4. Check system resources (CPU, memory)

---

## Examples

### Complete Example - Health Check & Run Workflow

```csharp
// C# example using HttpClient
using System.Net.Http;
using System.Text.Json;

var client = new HttpClient();
var baseUrl = "http://localhost:5000";

// 1. Check health
var healthResponse = await client.GetAsync($"{baseUrl}/api/health/all");
if (!healthResponse.IsSuccessStatusCode)
{
    Console.WriteLine("Services are not healthy");
    return;
}

var healthJson = await healthResponse.Content.ReadAsStringAsync();
Console.WriteLine($"Health: {healthJson}");

// 2. Run workflow
var workflowRequest = new { query = "What is machine learning?" };
var content = new StringContent(
    JsonSerializer.Serialize(workflowRequest),
    Encoding.UTF8,
    "application/json"
);

var workflowResponse = await client.PostAsync($"{baseUrl}/api/workflow/run", content);
var result = await workflowResponse.Content.ReadAsStringAsync();
Console.WriteLine($"Result: {result}");
```

### Complete Example - Python

```python
import requests
import json
import time

BASE_URL = "http://localhost:5000"

# 1. Health check
print("Checking services...")
health_resp = requests.get(f"{BASE_URL}/api/health/all")
health = health_resp.json()

if not health['allHealthy']:
    print("❌ Not all services are healthy")
    print(json.dumps(health, indent=2))
    exit(1)

print("✅ All services healthy")

# 2. Run workflow
print("\nRunning workflow...")
start = time.time()

response = requests.post(
    f"{BASE_URL}/api/workflow/run",
    json={"query": "What is artificial intelligence?"},
    timeout=300
)

elapsed = time.time() - start

result = response.json()
print(f"\n✅ Workflow completed in {elapsed:.1f}s")
print(f"Query: {result['query']}")
print(f"\nUpdates ({len(result['updates'])} steps):")
for update in result['updates']:
    print(f"  - {update}")

print(f"\nTotal Duration: {result['totalDuration']}ms")
```

---

## Additional Resources

- **ASP.NET Core Docs**: https://docs.microsoft.com/aspnet/core
- **Swagger/OpenAPI**: https://swagger.io
- **JSON Schema**: https://json-schema.org
- **REST API Guidelines**: https://restfulapi.net

---

## Support

For issues or questions:
1. Check troubleshooting section above
2. Review logs in console output
3. Check PHASE3_KICKOFF_GUIDE.md for Phase 3 troubleshooting
4. Review OperationsController.cs source code
5. Check related service documentation (Ollama, SearXNG, Crawl4AI)

---

**Web API Documentation**  
**Version**: 1.0  
**Last Updated**: 2026-01-16  
**Status**: ✅ Complete
