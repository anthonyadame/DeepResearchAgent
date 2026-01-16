# Quick Reference: LLM Integration & Master Workflow

## 🚀 Quick Start (2 minutes)

### 1. Start Ollama
```bash
ollama serve
```

### 2. Pull a Model
```bash
ollama pull mistral
```

### 3. Test the Connection
```bash
dotnet run
# Should show: ✓ Ollama is running and healthy
```

---

## 📦 Key Classes

### OllamaService
**File:** `DeepResearchAgent/Services/OllamaService.cs`

```csharp
// Create instance
var ollama = new OllamaService(
    baseUrl: "http://localhost:11434",
    defaultModel: "mistral"
);

// Check health
bool healthy = await ollama.IsHealthyAsync();

// Get models
List<string> models = await ollama.GetAvailableModelsAsync();

// Invoke LLM
var response = await ollama.InvokeAsync(
    new List<OllamaChatMessage>
    {
        new() { Role = "user", Content = "Hello!" }
    }
);

// Stream response
await foreach (var chunk in ollama.InvokeStreamingAsync(messages))
{
    Console.Write(chunk);
}

// Get JSON output
var result = await ollama.InvokeWithStructuredOutputAsync<MyType>(messages);
```

### MasterWorkflow
**File:** `DeepResearchAgent/Workflows/MasterWorkflow.cs`

```csharp
// Create instance
var master = new MasterWorkflow(supervisor, ollama);

// Run full pipeline
string result = await master.RunAsync("Research topic");

// Stream progress
await foreach (var update in master.StreamAsync("Research topic"))
{
    Console.WriteLine(update);
}
```

---

## 🔧 OllamaService Methods

| Method | Purpose | Returns |
|--------|---------|---------|
| `InvokeAsync()` | Get single LLM response | `OllamaChatMessage` |
| `InvokeStreamingAsync()` | Stream response chunks | `IAsyncEnumerable<string>` |
| `InvokeWithStructuredOutputAsync<T>()` | Get typed JSON response | `T` |
| `IsHealthyAsync()` | Check Ollama running | `bool` |
| `GetAvailableModelsAsync()` | List installed models | `List<string>` |

---

## 🎯 MasterWorkflow Steps

| Step | Input | Output | LLM Used |
|------|-------|--------|----------|
| 1. Clarify | User query | Clarification or proceed | Yes |
| 2. Brief | Clarified query | Research brief | Yes |
| 3. Draft | Research brief | Draft outline | Yes |
| 4. Supervisor | All above | Refined findings | Via supervisor |
| 5. Final | All above + findings | Final report | Yes |

---

## 📝 OllamaChatMessage

```csharp
public class OllamaChatMessage
{
    public required string Role { get; init; }     // "user", "assistant", "system"
    public required string Content { get; init; }  // Message text
}

// Usage
var message = new OllamaChatMessage
{
    Role = "user",
    Content = "What is AI?"
};
```

---

## 🔍 Error Handling

### Common Errors & Fixes

```csharp
// Error: "Failed to connect to Ollama"
// Fix: Start Ollama with: ollama serve

// Error: "No models available"
// Fix: Install a model: ollama pull mistral

// Error: "Failed to parse response"
// Fix: Check model output, try different model

// Graceful Fallback:
try
{
    var response = await ollama.InvokeAsync(messages);
    return response.Content;
}
catch (Exception ex)
{
    _logger?.LogError(ex, "LLM call failed");
    return "Fallback text"; // Never block workflow
}
```

---

## 🧪 Testing Patterns

### Test LLM Service
```csharp
[Fact]
public async Task OllamaService_IsHealthy()
{
    var ollama = new OllamaService();
    var healthy = await ollama.IsHealthyAsync();
    Assert.True(healthy);
}

[Fact]
public async Task OllamaService_InvokeAsync_ReturnsResponse()
{
    var ollama = new OllamaService();
    var response = await ollama.InvokeAsync(
        new List<OllamaChatMessage>
        {
            new() { Role = "user", Content = "Say hello" }
        }
    );
    Assert.NotEmpty(response.Content);
}
```

### Test Master Workflow
```csharp
[Fact]
public async Task MasterWorkflow_RunAsync_CompletesFullPipeline()
{
    var master = new MasterWorkflow(_supervisor, _ollama);
    var result = await master.RunAsync("AI research");
    Assert.NotEmpty(result);
}
```

---

## 📊 Architecture at a Glance

```
User Input
    ↓
MasterWorkflow (5 steps)
    ├─ Clarify (LLM)
    ├─ Brief (LLM)
    ├─ Draft (LLM)
    ├─ Supervise (LLM + Research)
    └─ Final (LLM)
    ↓
OllamaService
    ├─ InvokeAsync()
    ├─ InvokeStreamingAsync()
    └─ Structured Output
    ↓
HTTP to Ollama
    └─ /api/chat
    ↓
LLM Response
```

---

## ⚙️ Configuration

### Default Settings
```csharp
var ollama = new OllamaService(
    baseUrl: "http://localhost:11434",  // Default Ollama port
    defaultModel: "mistral"              // Default model
);
```

### Custom Configuration
```csharp
var ollama = new OllamaService(
    baseUrl: "http://my-server:11434",
    defaultModel: "neural-chat",
    httpClient: customHttpClient,
    logger: logger
);
```

### Available Models
```bash
# Small & fast
ollama pull neural-chat

# Medium
ollama pull mistral

# Large
ollama pull llama2

# Code-focused
ollama pull code-llama
```

---

## 💡 Common Patterns

### Pattern 1: Ask a Question
```csharp
var response = await ollama.InvokeAsync(
    new List<OllamaChatMessage>
    {
        new() { Role = "system", Content = "You are a helpful assistant." },
        new() { Role = "user", Content = "What is machine learning?" }
    }
);
```

### Pattern 2: Multi-turn Conversation
```csharp
var messages = new List<OllamaChatMessage>
{
    new() { Role = "user", Content = "What is Python?" },
    new() { Role = "assistant", Content = "Python is a programming language..." },
    new() { Role = "user", Content = "How do I install it?" }
};
var response = await ollama.InvokeAsync(messages);
```

### Pattern 3: Streaming Output
```csharp
await foreach (var chunk in ollama.InvokeStreamingAsync(messages))
{
    Console.Write(chunk);
}
```

### Pattern 4: Get Structured Output
```csharp
public class ResearchPlan
{
    public string Title { get; set; }
    public List<string> Topics { get; set; }
}

var plan = await ollama.InvokeWithStructuredOutputAsync<ResearchPlan>(messages);
```

---

## 🚨 Troubleshooting

| Problem | Solution |
|---------|----------|
| "No Ollama" | Run `ollama serve` in another terminal |
| "No models" | Run `ollama pull mistral` |
| "Slow response" | Try smaller model or streaming |
| "Empty response" | Check LLM output, verify model works |
| "Connection refused" | Verify port 11434 accessible |

---

## 📈 Performance Tips

1. **Use Streaming for Better UX**
   ```csharp
   // Instead of waiting for full response
   await foreach (var chunk in ollama.InvokeStreamingAsync(messages))
       Console.Write(chunk);
   ```

2. **Use Smaller Models for Speed**
   ```bash
   ollama pull neural-chat  # Faster than mistral
   ```

3. **Cache Common Prompts**
   - Save responses for repeated queries
   - Avoid redundant LLM calls

4. **Batch Calls When Possible**
   - Process multiple items together
   - Reduce overhead

---

## 🔗 Integration Points

### Where OllamaService is Used
- **MasterWorkflow** - All 5 steps
- **SupervisorWorkflow** - Quality evaluation (soon)
- **Red Team** - Critique generation (soon)
- **Context Pruner** - Fact extraction (soon)

### Where to Add More LLM Calls
1. Quality scoring
2. Red team critique
3. Fact extraction
4. Prompt refinement
5. Response ranking

---

## 📚 Files You Need to Know

| File | Purpose |
|------|---------|
| `OllamaService.cs` | LLM integration |
| `MasterWorkflow.cs` | Research orchestration |
| `SupervisorWorkflow.cs` | Refinement loop |
| `PromptTemplates.cs` | Prompt definitions |
| `StateFactory.cs` | State creation |
| `StateValidator.cs` | State validation |

---

## 🎯 Next Steps

1. **Test OllamaService**
   - Run `dotnet run`
   - Verify "✓ Ollama is running"

2. **Test MasterWorkflow**
   - Create instance with ollama
   - Call `RunAsync()` with query
   - Monitor streaming output

3. **Enhance SupervisorWorkflow**
   - Add LLM to supervisor brain
   - Implement quality evaluation
   - Add tool calling

4. **Add Red Team**
   - Implement adversarial critique
   - Inject critique into loop
   - Measure self-correction

---

## ✅ Checklist Before You Start

- [ ] Ollama installed
- [ ] `ollama serve` running
- [ ] Model installed (`ollama pull mistral`)
- [ ] `dotnet build` succeeds
- [ ] No compilation errors
- [ ] Can access http://localhost:11434

---

## 🎓 Key Takeaways

- **OllamaService** is your LLM bridge
- **HTTP API** is simpler than libraries
- **MasterWorkflow** orchestrates 5 LLM steps
- **Error handling** with fallbacks prevents blocking
- **Logging** helps debug issues
- **Streaming** improves UX

---

**Ready?** Start with `ollama serve` and `dotnet run`! 🚀
