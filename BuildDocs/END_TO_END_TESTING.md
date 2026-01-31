# End-to-End Testing Plan: Deep Research Agent with StreamState

## Overview
This document outlines the complete end-to-end testing flow for the Deep Research Agent with real-time streaming via the new `POST /api/workflows/master/stream` endpoint.

## Architecture

```
┌─────────────────────────────────────────────────────────────────┐
│                        UI/Client                                 │
│  (Web Browser, Desktop App, or CLI Test Client)                 │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ HTTP POST + SSE
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│                   API Layer (ASP.NET Core)                       │
│  POST /api/workflows/master/stream                              │
│  ├─ Accepts: MasterWorkflowRequest { UserQuery }               │
│  └─ Returns: Server-Sent Events (StreamState objects)          │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ Calls
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│              MasterWorkflow.StreamStateAsync()                   │
│  5-Phase Pipeline with Real-time Progress:                      │
│  1. ClarifyWithUser → StreamState (status + query clarification)│
│  2. WriteResearchBrief → StreamState (briefPreview + research)  │
│  3. WriteDraftReport → StreamState (draftReport)                │
│  4. SupervisorLoop → StreamState (supervisorUpdate + refinement)│
│  5. GenerateFinalReport → StreamState (finalReport + summary)   │
└────────────────────────┬────────────────────────────────────────┘
                         │
                         │ Uses
                         ↓
┌─────────────────────────────────────────────────────────────────┐
│            External Services (All Must Be Running)              │
│  ✓ Ollama (LLM inference)                                       │
│  ✓ SearXNG (Web search)                                         │
│  ✓ Crawl4AI (Web scraping)                                      │
│  ✓ Agent-Lightning (APO/VERL server)                            │
│  ✓ Qdrant (Vector DB - optional)                               │
└─────────────────────────────────────────────────────────────────┘
```

## StreamState Object Flow

The `StreamState` object carries progressive state through the pipeline:

```csharp
public class StreamState
{
    public string Status { get; set; }              // Current phase status (JSON)
    public string ResearchId { get; set; }          // Unique research request ID
    public string UserQuery { get; set; }           // Original user input
    public string BriefPreview { get; set; }        // First 150 chars of brief
    public string ResearchBrief { get; set; }       // Full structured research plan
    public string DraftReport { get; set; }         // Initial "noisy" draft
    public string RefinedSummary { get; set; }      // Supervisor-refined output
    public string FinalReport { get; set; }         // Polished final report
    public string SupervisorUpdate { get; set; }    // Live refinement step
    public int SupervisorUpdateCount { get; set; }  // Count of refinement iterations
}
```

## Testing Phases

### Phase 1: Pre-flight Health Checks
```bash
# Start all required services
docker-compose up -d ollama searxng crawl4ai lightning-server qdrant

# Run health checks via CLI
# Program.cs already has menu option [6] Run All Health Checks
# OR use existing endpoints:

# Check Ollama
curl http://localhost:11434/api/models

# Check SearXNG
curl http://localhost:8080/healthz

# Check Agent-Lightning
curl http://localhost:8090/api/health

# Check API is running
curl http://localhost:5000/health
```

### Phase 2: Unit Testing (Existing Tests)
```bash
# Run existing unit tests for MasterWorkflow
dotnet test DeepResearchAgent.Tests/DeepResearchAgent.Tests.csproj \
  -k MasterWorkflowTests

# Run StreamStateAsync-specific tests
dotnet test DeepResearchAgent.Tests/DeepResearchAgent.Tests.csproj \
  -k "StreamStateAsync"
```

### Phase 3: Integration Testing - Direct API Call
```bash
# Test the new streaming endpoint
curl -X POST http://localhost:5000/api/workflows/master/stream \
  -H "Content-Type: application/json" \
  -d '{"userQuery": "How much would it cost to send satellites to Jupiter?"}'
```

Expected streaming response (Server-Sent Events format):
```
data: {"status":"connected","timestamp":"2024-01-15T10:30:00Z"}

data: {"status":"clarifying user intent","researchId":"abc-123"}

data: {"status":"completed","briefPreview":"Jupiter mission analysis...","researchBrief":"..."}

data: {"status":"generating initial draft report"}

data: {"draftReport":"..."}

data: {"supervisorUpdate":"Refining section 1/5"}

data: {"supervisorUpdate":"Refining section 2/5"}

... (more supervisor updates)

data: {"status":"generating final polished report"}

data: {"finalReport":"...","status":"completed"}
```

### Phase 4: UI Integration Testing

#### A. Browser-based Test Client (HTML/JavaScript)

Create `DeepResearchAgent.UI/test-stream.html`:

```html
<!DOCTYPE html>
<html>
<head>
    <title>Deep Research Agent - Stream Test</title>
    <style>
        body { font-family: monospace; margin: 20px; }
        #output { border: 1px solid #ccc; height: 600px; overflow-y: auto; padding: 10px; }
        .status { color: blue; }
        .brief { color: green; }
        .draft { color: purple; }
        .supervisor { color: orange; }
        .final { color: darkgreen; }
        .error { color: red; }
    </style>
</head>
<body>
    <h1>Deep Research Agent - Streaming Test</h1>
    <input type="text" id="query" placeholder="Enter research query..." 
           value="How much would it cost to send satellites to Jupiter?" style="width: 500px;">
    <button onclick="startStream()">Start Stream</button>
    <button onclick="stopStream()">Stop Stream</button>
    
    <div id="output"></div>

    <script>
        let eventSource = null;

        function startStream() {
            const query = document.getElementById('query').value;
            const output = document.getElementById('output');
            output.innerHTML = '';

            const request = { userQuery: query };

            eventSource = new EventSource(
                `/api/workflows/master/stream?userQuery=${encodeURIComponent(query)}`
            );

            // Manual fetch with streaming (since POST with SSE is complex)
            fetch('/api/workflows/master/stream', {
                method: 'POST',
                headers: { 'Content-Type': 'application/json' },
                body: JSON.stringify(request)
            })
            .then(response => {
                const reader = response.body.getReader();
                const decoder = new TextDecoder();

                function read() {
                    reader.read().then(({ done, value }) => {
                        if (done) {
                            output.innerHTML += '<p class="status">✓ Stream completed</p>';
                            return;
                        }

                        const chunk = decoder.decode(value);
                        const lines = chunk.split('\n');

                        lines.forEach(line => {
                            if (line.startsWith('data: ')) {
                                try {
                                    const data = JSON.parse(line.substring(6));
                                    displayStreamState(data);
                                } catch (e) {
                                    console.error('Parse error:', e);
                                }
                            }
                        });

                        read();
                    });
                }

                read();
            })
            .catch(error => {
                output.innerHTML += `<p class="error">Error: ${error.message}</p>`;
            });
        }

        function displayStreamState(state) {
            const output = document.getElementById('output');
            const div = document.createElement('div');

            if (state.status) {
                div.className = 'status';
                div.textContent = `[Status] ${state.status}`;
            } else if (state.researchBrief) {
                div.className = 'brief';
                div.textContent = `[Brief] ${state.briefPreview || state.researchBrief.substring(0, 100)}...`;
            } else if (state.draftReport) {
                div.className = 'draft';
                div.textContent = `[Draft] ${state.draftReport.substring(0, 100)}...`;
            } else if (state.supervisorUpdate) {
                div.className = 'supervisor';
                div.textContent = `[Supervisor] ${state.supervisorUpdate}`;
            } else if (state.finalReport) {
                div.className = 'final';
                div.textContent = `[Final] ${state.finalReport.substring(0, 100)}...`;
            }

            output.appendChild(div);
            output.scrollTop = output.scrollHeight;
        }

        function stopStream() {
            if (eventSource) {
                eventSource.close();
            }
        }
    </script>
</body>
</html>
```

Test Flow:
```
1. Open browser: http://localhost:5000/test-stream.html
2. Enter query: "Jupiter satellite mission cost analysis"
3. Click "Start Stream"
4. Observe real-time updates in the output panel
5. Verify all phases appear in sequence
6. Click "Stop Stream" when complete
```

#### B. CLI Test Client (.NET)

Create `DeepResearchAgent.Tests/StreamingEndpointTests.cs`:

```csharp
using Xunit;
using DeepResearchAgent.Models;
using DeepResearchAgent.Api.DTOs.Requests.Workflows;
using DeepResearchAgent.Services;
using System.Text.Json;

namespace DeepResearchAgent.Tests;

public class StreamingEndpointTests : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    private readonly string _baseUrl;

    public StreamingEndpointTests()
    {
        _baseUrl = "http://localhost:5000";
        _httpClient = new HttpClient();
    }

    public async Task InitializeAsync()
    {
        // Verify API is running
        try
        {
            var response = await _httpClient.GetAsync($"{_baseUrl}/health");
            Assert.True(response.IsSuccessStatusCode);
        }
        catch
        {
            throw new InvalidOperationException("API server not running at " + _baseUrl);
        }
    }

    public async Task DisposeAsync()
    {
        _httpClient?.Dispose();
    }

    [Fact]
    public async Task StreamMasterWorkflow_ReceivesAllPhases()
    {
        // Arrange
        var request = new MasterWorkflowRequest
        {
            UserQuery = "What is the impact of quantum computing on cryptography by 2030?"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/workflows/master/stream",
            content
        );

        // Assert
        Assert.True(response.IsSuccessStatusCode);
        Assert.Equal("text/event-stream", response.Content.Headers.ContentType?.MediaType);

        var streamedStates = new List<StreamState>();
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var reader = new StreamReader(stream))
        {
            string? line;
            while ((line = await reader.ReadLineAsync()) != null)
            {
                if (line.StartsWith("data: "))
                {
                    var jsonLine = line.Substring(6);
                    var state = JsonSerializer.Deserialize<StreamState>(jsonLine);
                    if (state != null)
                    {
                        streamedStates.Add(state);
                    }
                }
            }
        }

        // Verify all phases are present
        Assert.NotEmpty(streamedStates);
        Assert.Contains(streamedStates, s => !string.IsNullOrEmpty(s.ResearchBrief));
        Assert.Contains(streamedStates, s => !string.IsNullOrEmpty(s.DraftReport));
        Assert.Contains(streamedStates, s => !string.IsNullOrEmpty(s.FinalReport));
    }

    [Fact]
    public async Task StreamMasterWorkflow_DisplaysProgress()
    {
        // Arrange
        var request = new MasterWorkflowRequest
        {
            UserQuery = "Simple test query"
        };

        var content = new StringContent(
            JsonSerializer.Serialize(request),
            System.Text.Encoding.UTF8,
            "application/json"
        );

        // Act
        var response = await _httpClient.PostAsync(
            $"{_baseUrl}/api/workflows/master/stream",
            content
        );

        // Assert & Display
        using (var stream = await response.Content.ReadAsStreamAsync())
        using (var reader = new StreamReader(stream))
        {
            string? line;
            int count = 0;
            while ((line = await reader.ReadLineAsync()) != null && count < 50) // Limit for testing
            {
                if (line.StartsWith("data: "))
                {
                    var jsonLine = line.Substring(6);
                    var state = JsonSerializer.Deserialize<StreamState>(jsonLine);
                    if (state != null)
                    {
                        // Use the formatter helper
                        var summary = StreamStateFormatter.GetProgressSummary(state);
                        System.Console.WriteLine($"[{++count}] {summary}");
                    }
                }
            }
        }
    }
}
```

Run the test:
```bash
dotnet test DeepResearchAgent.Tests/DeepResearchAgent.Tests.csproj \
  -k StreamingEndpointTests \
  --logger "console;verbosity=normal"
```

### Phase 5: Performance & Load Testing

```csharp
[Fact(Timeout = 300000)] // 5 minute timeout
public async Task StreamMasterWorkflow_HandlesTimeout()
{
    var cts = new CancellationTokenSource(TimeSpan.FromMinutes(4));
    
    // Should complete within 4 minutes
    await StreamMasterWorkflow_ReceivesAllPhases();
}

[Fact]
public async Task StreamMasterWorkflow_ClarificationFlow()
{
    // Test with vague query requiring clarification
    var request = new MasterWorkflowRequest
    {
        UserQuery = "research things"  // Intentionally vague
    };

    // Should yield clarification_needed status
    // Then accept follow-up with "clarification_provided:"
}
```

### Phase 6: Monitoring & Observability

```bash
# View streaming metrics in Prometheus
curl http://localhost:9090/api/v1/query?query=stream_requests_total

# Check OpenTelemetry traces
# Navigate to: http://localhost:16686/ (Jaeger UI)
# Search for: master_workflow_stream traces
```

## CLI Quick Test Script

Create `test-stream.sh`:

```bash
#!/bin/bash

echo "🔍 Deep Research Agent - Stream Endpoint Test"
echo "=============================================="

# Colors
GREEN='\033[0;32m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Test query
QUERY="How much would it cost to send satellites to Jupiter for telescope observations using gravitational lensing?"

echo -e "${BLUE}Query:${NC} $QUERY"
echo ""
echo -e "${BLUE}Streaming...${NC}"
echo ""

curl -X POST http://localhost:5000/api/workflows/master/stream \
  -H "Content-Type: application/json" \
  -d "{\"userQuery\": \"$QUERY\"}" \
  -N \
  --http2 | while IFS= read -r line; do
    if [[ $line == data:* ]]; then
        echo -e "${GREEN}$(echo $line | sed 's/data: //')${NC}"
    fi
  done

echo ""
echo -e "${GREEN}✓ Stream test complete${NC}"
```

## Success Criteria

A successful end-to-end test demonstrates:

- [ ] ✅ All health checks pass (Ollama, SearXNG, Crawl4AI, Lightning)
- [ ] ✅ `/api/workflows/master/stream` endpoint receives POST request
- [ ] ✅ Response `Content-Type: text/event-stream` is set correctly
- [ ] ✅ First StreamState arrives with initial status
- [ ] ✅ ResearchBrief populated with query analysis
- [ ] ✅ DraftReport populated with initial research
- [ ] ✅ SupervisorUpdate messages stream in real-time
- [ ] ✅ FinalReport populated with refined output
- [ ] ✅ Stream completes without errors
- [ ] ✅ UI receives all StreamState updates properly formatted
- [ ] ✅ Metrics recorded in Prometheus
- [ ] ✅ Duration ≤ 5 minutes for typical queries

## Troubleshooting

### Issue: Stream times out
```bash
# Check if services are running
docker ps | grep -E "ollama|searxng|crawl4ai|lightning"

# Increase timeout in request
curl --max-time 600 ... # 10 minute timeout
```

### Issue: Empty StreamState objects
```bash
# Check MasterWorkflow.StreamStateAsync isn't returning null values
# Verify WriteResearchBriefAsync, WriteDraftReportAsync complete successfully
# Check Ollama response times: curl -w "@curl-format.txt" http://localhost:11434/api/models
```

### Issue: Missing SupervisorUpdate events
```bash
# Verify SupervisorWorkflow.StreamSuperviseAsync is properly yielding updates
# Check if supervisor loop is completing or timing out
# Review logs: grep -i "supervisor" logs/*.log
```

## Helper Functions for UI Implementation

Use `StreamStateFormatter` helpers (located in `DeepResearchAgent.Services`):

```csharp
// In your UI layer:
using DeepResearchAgent.Services;

// Display single field
StreamStateFormatter.WriteStreamStateField("ResearchBrief", state.ResearchBrief);

// Display all fields
StreamStateFormatter.WriteStreamStateFields(state);

// Get progress summary for status bar
var progress = StreamStateFormatter.GetProgressSummary(state);
// Output: "Status: generating final polished report | ✓ Research Brief | ✓ Draft Report..."

// Get most relevant content for current phase
var content = StreamStateFormatter.GetPhaseContent(state);
// Returns: FinalReport (if complete) → RefinedSummary → DraftReport → ResearchBrief → BriefPreview
```

## Next Steps

1. **Run health checks**: Verify all services are accessible
2. **Execute Phase 2 tests**: Confirm MasterWorkflow unit tests pass
3. **Test Phase 3 curl**: Make a single streaming request
4. **Deploy Phase 4**: Set up UI client (browser or CLI)
5. **Execute Phase 5**: Run load tests if needed
6. **Monitor Phase 6**: Check metrics in Prometheus/Jaeger

---

## References
- [MasterWorkflow.StreamStateAsync](../Workflows/MasterWorkflow.cs)
- [StreamState Model](../Models/StreamState.cs)
- [WorkflowsController Endpoint](../Api/Controllers/WorkflowsController.cs)
- [StreamStateFormatter Helper](../Services/StreamStateFormatter.cs)
- [Server-Sent Events MDN](https://developer.mozilla.org/en-US/docs/Web/API/Server-sent_events)
