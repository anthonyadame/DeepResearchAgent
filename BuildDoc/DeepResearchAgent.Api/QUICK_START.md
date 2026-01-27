# Quick Start Guide - Deep Research Agent API

## Starting the Application

### Visual Studio
1. Press **F5** or click **Start Debugging**
2. Browser will automatically open to **Swagger UI** at `https://localhost:5001/`

### Command Line
```bash
cd DeepResearchAgent.Api
dotnet run
```
Then navigate to `https://localhost:5001/` in your browser.

### Docker (Future)
```bash
docker run -p 5000:80 deep-research-agent-api
```

## What You'll See

When the application starts, you'll see the **Swagger UI** homepage with:

### 📋 API Overview
- API Title, Version, Description
- Contact Information
- License
- Links to GitHub repository

### 🗂️ Endpoint Groups

#### **1. Workflows**
High-level orchestration endpoints for complete research workflows.

**Try this first:**
```http
POST /api/workflows/master
Content-Type: application/json

{
  "userQuery": "What are the latest developments in quantum computing?",
  "maxIterations": 3,
  "qualityThreshold": 0.75
}
```

#### **2. Agents**
Individual specialized agents for specific tasks.

**Example - Clarify a query:**
```http
POST /api/agents/clarify
Content-Type: application/json

{
  "conversationHistory": [
    {
      "role": "user",
      "content": "Tell me about AI"
    }
  ]
}
```

#### **3. Core Services**
Infrastructure services for LLM, search, and state management.

**Example - Search the web:**
```http
POST /api/core/search
Content-Type: application/json

{
  "query": "quantum computing breakthroughs 2024",
  "maxResults": 10
}
```

#### **4. Operations**
Tool invocations and system metrics.

**Example - Get metrics:**
```http
GET /api/operations/metrics
```

#### **5. Health**
System health monitoring.

**Example - Check health:**
```http
GET /health
```

## Testing Endpoints in Swagger

1. **Click** on any endpoint to expand it
2. **Click** "Try it out" button
3. **Fill in** the request body (JSON examples are provided)
4. **Click** "Execute"
5. **View** the response below

## Common Workflows

### Complete Research Pipeline
```
1. POST /api/workflows/master
   ↓
2. Monitor with GET /api/workflows/async/{jobId}/status
   ↓
3. Retrieve results with GET /api/workflows/async/{jobId}/results
```

### Step-by-Step Research
```
1. POST /api/agents/clarify          (Validate query)
   ↓
2. POST /api/agents/research-brief   (Generate brief)
   ↓
3. POST /api/agents/researcher       (Conduct research)
   ↓
4. POST /api/agents/analyst          (Analyze findings)
   ↓
5. POST /api/agents/draft-report     (Create draft)
   ↓
6. POST /api/agents/report           (Generate final report)
```

## Configuration

### Required External Services

Ensure these services are running before starting the API:

| Service | Default URL | Purpose |
|---------|-------------|---------|
| **Ollama** | `http://localhost:11434` | LLM inference |
| **SearXNG** | `http://localhost:8080` | Web search |
| **Crawl4AI** | `http://localhost:11235` | Web scraping |
| **Lightning** | `http://localhost:8090` | State management |

### Configuring URLs

Edit `appsettings.json`:
```json
{
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
    "ServerUrl": "http://localhost:8090"
  }
}
```

Or use environment variables:
```bash
export LIGHTNING_SERVER_URL=http://your-lightning-server:8090
```

## Response Format

All API responses follow this structure:

### Success Response
```json
{
  "success": true,
  "data": { /* Your data here */ },
  "correlationId": "550e8400-e29b-41d4-a716-446655440000",
  "timestamp": "2024-01-15T10:30:00Z"
}
```

### Error Response
```json
{
  "success": false,
  "error": {
    "code": "VALIDATION_ERROR",
    "message": "Invalid request parameters",
    "details": "Query cannot be empty",
    "statusCode": 400
  },
  "correlationId": "550e8400-e29b-41d4-a716-446655440000"
}
```

## CORS Configuration

The API is configured to accept requests from any origin in development:

```csharp
CORS Policy: "AllowUI"
- Allow Any Origin
- Allow Any Method
- Allow Any Header
```

## Health Checks

### Basic Health Check
```bash
curl http://localhost:5000/health
```

### Liveness Probe
```bash
curl http://localhost:5000/health/live
```

## Troubleshooting

### Swagger UI Not Loading
- Check if the app is running on the expected port
- Clear browser cache
- Try accessing via IP: `http://127.0.0.1:5000/`

### "Service Unavailable" Errors
- Verify external services (Ollama, SearXNG, etc.) are running
- Check `appsettings.json` URLs are correct
- Review logs in console output

### CORS Errors from Frontend
- Ensure CORS is enabled in `Program.cs`
- Check frontend is using correct API URL
- Verify `AllowUI` policy is active

### Build Errors
- Run `dotnet clean`
- Run `dotnet restore`
- Run `dotnet build`

## Next Steps

1. ✅ **Explore the API** - Try different endpoints in Swagger
2. 📚 **Read the docs** - Check `SWAGGER_CONFIGURATION.md`
3. 🧪 **Run tests** - Execute unit and integration tests
4. 🔧 **Customize** - Modify agents and workflows for your use case
5. 🚀 **Deploy** - Deploy to your preferred hosting platform

## Support

- GitHub Issues: https://github.com/anthonyadame/DeepResearchAgent/issues
- Documentation: See `BuildDoc/` folder
- API Reference: This Swagger UI!

---

**Happy Researching! 🚀**
