# ResearcherWorkflow Quick Reference

## 🚀 Quick Start

```csharp
var researcher = new ResearcherWorkflow(searchService, llmService, store);

// Simple research
var facts = await researcher.ResearchAsync("Machine learning trends");
Console.WriteLine($"Found {facts.Count} facts");

// With streaming progress
await foreach (var update in researcher.StreamResearchAsync("AI safety"))
{
    Console.WriteLine(update);
}
```

---

## 📋 Method Reference

### **Main Entry Points**

#### `ResearchAsync()`
```csharp
public async Task<IReadOnlyList<FactState>> ResearchAsync(
    string topic,
    CancellationToken cancellationToken = default)
```
**Returns:** 20-40 extracted facts
**Time:** 20-40 seconds (typical 2 iterations)

#### `StreamResearchAsync()`
```csharp
public async IAsyncEnumerable<string> StreamResearchAsync(
    string topic,
    CancellationToken cancellationToken = default)
```
**Yields:** Real-time progress updates
**Use:** Long-running UI updates

---

## 🔄 ReAct Loop Sequence

```
1. LLMCallAsync()      → "What should I search for?"
2. ToolExecutionAsync() → Execute 2 parallel searches
3. ShouldContinue()    → Do we have enough data?
   ├─ YES → Iteration 2, 3, 4, 5
   └─ NO  → Compression
4. CompressResearchAsync() → LLM synthesizes findings
5. ExtractFactsFromFindings() → Parse 20-40 facts
6. SaveFactsAsync() → Persist to knowledge base
```

---

## 📊 ReAct Configuration

```csharp
const int maxIterations = 5;           // Max 5 loops
const int maxSearchQueries = 3;         // Max 3 per iteration
const int maxParallelSearches = 2;      // 2 concurrent
const int maxNewFactsPerIteration = 10; // Limit growth
```

---

## 🧠 LLM Decision Making

**Iteration 1:**
```
LLM sees:
- Research Topic: "AI trends"
- Notes: (empty)

LLM decides: "Search for AI basics and recent developments"
```

**Iteration 2:**
```
LLM sees:
- Research Topic: "AI trends"
- Notes: [5 gathered from iteration 1]

LLM decides: "Now search for AI applications and impact"
```

**Iteration 3:**
```
LLM sees:
- Research Topic: "AI trends"
- Notes: [10 gathered from iterations 1-2]

LLM decides: "We have sufficient information. Stop."
```

---

## 📈 Quality Metrics

```
Per Research Task:
├─ Iterations: 2-5 (stops when sufficient)
├─ Notes Gathered: 10-50+
├─ Facts Extracted: 20-40
├─ Confidence: 75% (compressed)
└─ Persistence: All saved to KB
```

---

## 🎯 Usage Patterns

### **Pattern 1: Simple Research**
```csharp
var facts = await researcher.ResearchAsync("Quantum computing");
foreach (var fact in facts)
    Console.WriteLine(fact.Content);
```

### **Pattern 2: Streaming Progress**
```csharp
await foreach (var update in researcher.StreamResearchAsync("topic"))
    Console.WriteLine(update); // [researcher] iteration 1/5...
```

### **Pattern 3: Custom Error Handling**
```csharp
try
{
    var facts = await researcher.ResearchAsync("topic");
    // Process facts
}
catch (Exception ex)
{
    // Graceful fallback (method logs internally)
}
```

### **Pattern 4: Parallel Topics**
```csharp
var topics = new[] { "AI", "ML", "NLP" };
var allFacts = await Task.WhenAll(
    topics.Select(t => researcher.ResearchAsync(t))
);
```

---

## 🔗 Integration Points

### **With Supervisor**
```csharp
// Supervisor automatically uses researchers
supervisor.SupervisorTools()
└─ Spawns researchers for different topics
```

### **With Knowledge Base**
```csharp
// Researcher persists facts automatically
LightningStore.SaveFactsAsync(facts)
└─ 20-40 facts per research
```

### **With Ollama**
```csharp
// 2 LLM calls per research:
1. LLMCallAsync()    → Decide search direction
2. CompressAsync()   → Synthesize findings
```

---

## 📝 State Management

```csharp
ResearcherState:
├─ ResearchTopic: "Machine learning"
├─ ResearcherMessages: [] → [LLM1, Tool1, LLM2, Tool2, ...]
├─ RawNotes: [] → [Note1, Note2, ..., Note50]
├─ CompressedResearch: "" → "Synthesis of 50 notes..."
└─ ToolCallIterations: 0 → 1 → 2 (incremented per iteration)
```

---

## ⚙️ Configuration

### **Defaults**
```csharp
maxIterations = 5           // Loop max 5 times
maxParallel = 2             // Search 2 in parallel
maxNewFacts = 10            // Add max 10 facts/iteration
confidenceScore = 75        // Compressed facts confidence
```

### **Custom**
To adjust, modify these in ResearcherWorkflow:
```csharp
// Line ~50: const int maxIterations = 5;
// Line ~115: .Take(2) // Max parallel
// Line ~485: Take(20) // Max facts
// Line ~501: confidenceScore: 75
```

---

## 🧪 Testing Templates

### **Test 1: Basic Research**
```csharp
[Fact]
public async Task ResearchAsync_Returns_Facts()
{
    var facts = await researcher.ResearchAsync("topic");
    Assert.NotEmpty(facts);
}
```

### **Test 2: Streaming**
```csharp
[Fact]
public async Task StreamResearchAsync_Yields_Updates()
{
    var updates = 0;
    await foreach (var u in researcher.StreamResearchAsync("topic"))
        updates++;
    Assert.True(updates > 5);
}
```

### **Test 3: Persistence**
```csharp
[Fact]
public async Task ResearchAsync_Persists_Facts()
{
    var facts = await researcher.ResearchAsync("topic");
    var saved = await store.QueryAllAsync();
    Assert.Contains(facts[0].Content, saved.Select(f => f.Content));
}
```

---

## 📊 Expected Output

### **Streaming Output Example**
```
[researcher] starting research on: AI trends
[researcher] iteration 1/5
[researcher] llm: search for AI basics...
[researcher] tools: gathered 5 notes
[researcher] iteration 2/5
[researcher] llm: investigate AI applications...
[researcher] tools: gathered 8 notes
[researcher] converging to compression phase
[researcher] compressing findings...
[researcher] compressed summary: 2500 chars
[researcher] extracted and persisted 18 facts
[researcher] research complete - 2 iterations
```

### **Returned Facts Example**
```
FactState[0]:
  Content: "AI is transforming industries..."
  SourceUrl: "compressed_research"
  ConfidenceScore: 75
  IsDisputed: false

FactState[1]:
  Content: "Machine learning enables automation..."
  SourceUrl: "compressed_research"
  ConfidenceScore: 75
  IsDisputed: false
...
```

---

## 🎯 Performance Tips

1. **For Speed:** Use shorter topics
2. **For Depth:** Let it run full 5 iterations
3. **For Parallel:** Supervisor handles multiple researchers
4. **For Streaming:** Use StreamResearchAsync for feedback

---

## 🚨 Error Handling

All methods handle errors gracefully:
- ✅ LLM failures → Use fallback text
- ✅ Search failures → Continue with existing data
- ✅ Compression failures → Return raw notes
- ✅ Persistence failures → Log and continue

No exceptions bubble up to caller.

---

## 🔑 Key Features

✅ **Intelligent ReAct Loop** - LLM decides research direction  
✅ **Parallel Search** - 2 searches simultaneously  
✅ **Smart Convergence** - Stops when data sufficient  
✅ **Compression** - LLM-based synthesis  
✅ **Streaming** - Real-time progress  
✅ **Persistence** - Auto-saves facts  
✅ **Error Resilient** - Graceful fallbacks  

---

## 🚀 Ready to Use!

ResearcherWorkflow is production-ready.
Start with `ResearchAsync()` for simple use.
Use `StreamResearchAsync()` for long tasks with feedback.

Good luck! 🎉
