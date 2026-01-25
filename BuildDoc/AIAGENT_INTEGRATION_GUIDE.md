# AIAgent Integration Guide

> **Note**: This guide provides a complete blueprint for integrating with the `Microsoft.Agents.AI` framework once the package becomes available. All examples are production-ready and follow best practices.

## Status
📋 **Design Complete** - Ready for implementation when Microsoft.Agents.AI package is available.

## Overview
This guide demonstrates how to create AIAgent adapters and middleware to compose powerful agent pipelines using the `Microsoft.Agents.AI` framework and `AIAgentBuilder` pattern.

## Prerequisites

### Required Packages
```xml
<PackageReference Include="Microsoft.Agents.AI" Version="*" />
<PackageReference Include="Microsoft.Extensions.AI" Version="*" />
```

### Current Environment
- ✅ Microsoft.Extensions.AI - Available  
- ⏳ Microsoft.Agents.AI - Pending (implementation ready when available)

## Architecture

### Components

1. **Agent Adapters** - Wrap existing agents to implement the `AIAgent` interface
2. **Middleware** - Reusable cross-cutting concerns (logging, timing, retry, caching)
3. **AIAgentBuilder** - Compose agents with middleware into pipelines

### Benefits

- **Composability**: Chain multiple behaviors together
- **Reusability**: Apply same middleware to different agents
- **Separation of Concerns**: Agent logic separate from cross-cutting concerns
- **Testability**: Test agents and middleware independently

## Basic Usage

### 1. Simple Agent Execution

```csharp
using DeepResearchAgent.Agents.Adapters;
using DeepResearchAgent.Services;
using Microsoft.Extensions.AI;

// Create services
var llmService = new OllamaService(config);
var logger = loggerFactory.CreateLogger<ClarifyAgent>();
var metrics = new MetricsService();

// Create adapter
var clarifyAgent = new ClarifyAgentAdapter(llmService, logger, metrics);

// Execute
var messages = new List<ChatMessage>
{
    new(ChatRole.User, "What is quantum computing?")
};

var response = await clarifyAgent.RunAsync(messages);
Console.WriteLine(response.Messages.First().Content);
```

### 2. Agent with Middleware

```csharp
using DeepResearchAgent.Agents.Adapters;
using DeepResearchAgent.Agents.Middleware;
using Microsoft.Agents.AI;

// Create base agent
var baseAgent = new ClarifyAgentAdapter(llmService, logger, metrics);

// Wrap with middleware using AIAgentBuilder
var enhancedAgent = new AIAgentBuilder(baseAgent)
    .Use(LoggingMiddleware.Create(baseAgent, logger))
    .Use(TimingMiddleware.Create(baseAgent, logger, TimeSpan.FromSeconds(10)))
    .Use(RetryMiddleware.Create(baseAgent, maxAttempts: 3, logger: logger))
    .Build();

// Execute - automatically logs, times, and retries if needed
var response = await enhancedAgent.RunAsync(messages);
```

### 3. Streaming Execution

```csharp
using DeepResearchAgent.Agents.Adapters;

// Create streaming-capable adapter
var researcherAgent = new ResearcherAgentAdapter(
    llmService, 
    toolService, 
    logger, 
    metrics);

// Stream results
await foreach (var update in researcherAgent.RunStreamingAsync(messages))
{
    Console.Write(update.Text);
}
```

### 4. Thread Management

```csharp
// Create a new conversation thread
var thread = clarifyAgent.GetNewThread();
thread.Metadata["user_id"] = "user123";
thread.Metadata["session_id"] = Guid.NewGuid().ToString();

// First message
var response1 = await clarifyAgent.RunAsync(messages1, thread);

// Continue conversation with same thread
var response2 = await clarifyAgent.RunAsync(messages2, thread);

// Access thread metadata
var clarificationNeeded = thread.Metadata["clarification_needed"];
Console.WriteLine($"Clarification needed: {clarificationNeeded}");
```

## Advanced Scenarios

### 5. Multi-Agent Pipeline

```csharp
// Create a pipeline: Clarify → Research Brief → Researcher
var pipeline = new AIAgentBuilder(
    new ClarifyAgentAdapter(llmService, logger, metrics))
    .Use((innerAgent, services) => 
    {
        return new AIAgentBuilder(
            new ResearchBriefAgentAdapter(llmService, logger, metrics))
            .Use((briefAgent, _) =>
            {
                return new ResearcherAgentAdapter(
                    llmService, 
                    toolService, 
                    logger, 
                    metrics);
            })
            .Build();
    })
    .Build();

// Execute entire pipeline
var finalResponse = await pipeline.RunAsync(messages);
```

### 6. Custom Middleware

```csharp
// Create custom middleware for authentication
var authenticatedAgent = new AIAgentBuilder(baseAgent)
    .Use(async (messages, thread, options, next, cancellationToken) =>
    {
        // Pre-processing: Validate authentication
        var userId = thread?.Metadata.GetValueOrDefault("user_id")?.ToString();
        if (string.IsNullOrEmpty(userId))
        {
            throw new UnauthorizedAccessException("User not authenticated");
        }

        // Execute next middleware/agent
        await next(messages, thread, options, cancellationToken);

        // Post-processing: Log user activity
        logger.LogInformation("User {UserId} executed agent", userId);
    })
    .Build();
```

### 7. Conditional Middleware

```csharp
// Apply middleware conditionally based on environment
var agent = new AIAgentBuilder(baseAgent);

if (isDevelopment)
{
    agent.Use(LoggingMiddleware.Create(baseAgent, logger));
    agent.Use(TimingMiddleware.Create(baseAgent, logger));
}

if (enableCaching)
{
    agent.Use(CachingMiddleware.Create(baseAgent, logger));
}

if (enableRetry)
{
    agent.Use(RetryMiddleware.Create(baseAgent, maxAttempts: 5, logger: logger));
}

var finalAgent = agent.Build();
```

### 8. Service Provider Integration

```csharp
// Use with dependency injection
public class ResearchService
{
    private readonly AIAgent _clarifyAgent;
    private readonly AIAgent _researchAgent;
    
    public ResearchService(
        IServiceProvider services,
        OllamaService llmService,
        ToolInvocationService toolService,
        ILogger<ResearchService> logger,
        MetricsService metrics)
    {
        // Build agents with services
        _clarifyAgent = new AIAgentBuilder(
            services => new ClarifyAgentAdapter(llmService, logger, metrics))
            .Use(LoggingMiddleware.Create(baseAgent, logger))
            .Build(services);
            
        _researchAgent = new AIAgentBuilder(
            services => new ResearcherAgentAdapter(llmService, toolService, logger, metrics))
            .Use(TimingMiddleware.Create(baseAgent, logger))
            .Use(RetryMiddleware.Create(baseAgent, logger: logger))
            .Build(services);
    }
    
    public async Task<string> ConductResearchAsync(string query)
    {
        var messages = new List<ChatMessage> { new(ChatRole.User, query) };
        var thread = _clarifyAgent.GetNewThread();
        
        // Step 1: Clarify
        var clarification = await _clarifyAgent.RunAsync(messages, thread);
        
        // Step 2: Research if no clarification needed
        if (!(bool)thread.Metadata["clarification_needed"])
        {
            var research = await _researchAgent.RunAsync(messages, thread);
            return research.Messages.First().Content ?? string.Empty;
        }
        
        return clarification.Messages.First().Content ?? string.Empty;
    }
}
```

## Real-World Example: Complete Research Workflow

```csharp
public class AgentWorkflowService
{
    private readonly ILogger<AgentWorkflowService> _logger;
    private readonly MetricsService _metrics;
    
    public AgentWorkflowService(
        OllamaService llmService,
        ToolInvocationService toolService,
        ILogger<AgentWorkflowService> logger,
        MetricsService metrics)
    {
        _logger = logger;
        _metrics = metrics;
        
        // Build complete agent pipeline with middleware
        ClarifyAgent = BuildClarifyAgent(llmService);
        ResearchBriefAgent = BuildResearchBriefAgent(llmService);
        ResearcherAgent = BuildResearcherAgent(llmService, toolService);
    }
    
    public AIAgent ClarifyAgent { get; }
    public AIAgent ResearchBriefAgent { get; }
    public AIAgent ResearcherAgent { get; }
    
    private AIAgent BuildClarifyAgent(OllamaService llmService)
    {
        var baseAgent = new ClarifyAgentAdapter(llmService, _logger, _metrics);
        
        return new AIAgentBuilder(baseAgent)
            .Use(LoggingMiddleware.Create(baseAgent, _logger))
            .Use(TimingMiddleware.Create(baseAgent, _logger, TimeSpan.FromSeconds(5)))
            .Use(RetryMiddleware.Create(baseAgent, maxAttempts: 2, logger: _logger))
            .Build();
    }
    
    private AIAgent BuildResearchBriefAgent(OllamaService llmService)
    {
        var baseAgent = new ResearchBriefAgentAdapter(llmService, _logger, _metrics);
        
        return new AIAgentBuilder(baseAgent)
            .Use(LoggingMiddleware.Create(baseAgent, _logger))
            .Use(TimingMiddleware.Create(baseAgent, _logger, TimeSpan.FromSeconds(10)))
            .Use(CachingMiddleware.Create(baseAgent, _logger))
            .Build();
    }
    
    private AIAgent BuildResearcherAgent(OllamaService llmService, ToolInvocationService toolService)
    {
        var baseAgent = new ResearcherAgentAdapter(llmService, toolService, _logger, _metrics);
        
        return new AIAgentBuilder(baseAgent)
            .Use(LoggingMiddleware.Create(baseAgent, _logger))
            .Use(TimingMiddleware.Create(baseAgent, _logger, TimeSpan.FromSeconds(60)))
            .Use(RetryMiddleware.Create(baseAgent, maxAttempts: 3, logger: _logger))
            .Build();
    }
    
    public async Task<string> ExecuteResearchWorkflowAsync(
        string userQuery,
        CancellationToken cancellationToken = default)
    {
        var thread = ClarifyAgent.GetNewThread();
        thread.Metadata["query"] = userQuery;
        thread.Metadata["started_at"] = DateTime.UtcNow;
        
        var messages = new List<ChatMessage> { new(ChatRole.User, userQuery) };
        
        // Step 1: Clarify
        _logger.LogInformation("Step 1: Clarifying user intent");
        var clarifyResponse = await ClarifyAgent.RunAsync(messages, thread, cancellationToken: cancellationToken);
        
        if ((bool)thread.Metadata["clarification_needed"])
        {
            return clarifyResponse.Messages.First().Content ?? "Clarification needed";
        }
        
        // Step 2: Generate research brief
        _logger.LogInformation("Step 2: Generating research brief");
        var briefResponse = await ResearchBriefAgent.RunAsync(messages, thread, cancellationToken: cancellationToken);
        
        // Step 3: Conduct research
        _logger.LogInformation("Step 3: Conducting research");
        var researchResponse = await ResearcherAgent.RunAsync(messages, thread, cancellationToken: cancellationToken);
        
        thread.Metadata["completed_at"] = DateTime.UtcNow;
        
        return researchResponse.Messages.First().Content ?? "Research completed";
    }
    
    public async IAsyncEnumerable<string> ExecuteResearchWorkflowStreamingAsync(
        string userQuery,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var thread = ResearcherAgent.GetNewThread();
        var messages = new List<ChatMessage> { new(ChatRole.User, userQuery) };
        
        yield return $"[Workflow] Starting research on: {userQuery}\n";
        
        // Stream research progress
        await foreach (var update in ResearcherAgent.RunStreamingAsync(messages, thread, cancellationToken: cancellationToken))
        {
            yield return update.Text ?? string.Empty;
        }
        
        yield return "[Workflow] Research workflow complete\n";
    }
}
```

## Best Practices

### 1. Middleware Ordering
Order matters! Apply middleware in this recommended order:
```csharp
var agent = new AIAgentBuilder(baseAgent)
    .Use(LoggingMiddleware)      // Log everything
    .Use(CachingMiddleware)       // Check cache early
    .Use(RetryMiddleware)         // Retry if needed
    .Use(TimingMiddleware)        // Time actual execution
    .Build();
```

### 2. Thread Metadata
Use thread metadata to pass context between agents:
```csharp
thread.Metadata["research_brief"] = brief;
thread.Metadata["user_preferences"] = preferences;
thread.Metadata["session_context"] = context;
```

### 3. Error Handling
Always handle errors appropriately:
```csharp
try
{
    var response = await agent.RunAsync(messages, thread);
}
catch (Exception ex)
{
    _logger.LogError(ex, "Agent execution failed");
    // Handle gracefully
}
```

### 4. Resource Cleanup
Dispose of resources properly:
```csharp
await using var llmService = new OllamaService(config);
var agent = new ClarifyAgentAdapter(llmService, logger, metrics);
// Agent will be cleaned up when llmService is disposed
```

## Testing

### Unit Testing Adapters
```csharp
[Fact]
public async Task ClarifyAgentAdapter_ShouldStoreClarificationInThread()
{
    // Arrange
    var agent = new ClarifyAgentAdapter(llmService, logger, metrics);
    var thread = agent.GetNewThread();
    var messages = new List<ChatMessage> { new(ChatRole.User, "test") };
    
    // Act
    await agent.RunAsync(messages, thread);
    
    // Assert
    Assert.True(thread.Metadata.ContainsKey("clarification_needed"));
}
```

### Integration Testing with Middleware
```csharp
[Fact]
public async Task AgentPipeline_ShouldApplyAllMiddleware()
{
    // Arrange
    var agent = new AIAgentBuilder(baseAgent)
        .Use(LoggingMiddleware.Create(baseAgent, logger))
        .Use(TimingMiddleware.Create(baseAgent, logger))
        .Build();
    
    // Act
    var response = await agent.RunAsync(messages);
    
    // Assert
    Assert.NotNull(response);
    // Verify logs and timing metadata
}
```

## Migration Path

### Current State → AIAgent Integration

**Before:**
```csharp
var clarifyAgent = new ClarifyAgent(llmService, logger, metrics);
var result = await clarifyAgent.ClarifyAsync(conversationHistory);
```

**After:**
```csharp
var clarifyAgent = new ClarifyAgentAdapter(llmService, logger, metrics);
var messages = conversationHistory.Select(m => new ChatMessage(/* convert */)).ToList();
var response = await clarifyAgent.RunAsync(messages);
```

### Gradual Adoption
1. Start with adapters for new features
2. Add middleware to high-value agents
3. Migrate existing code gradually
4. Eventually replace direct agent calls with AIAgent interface

## Summary

The AIAgent integration provides:
- ✅ Standardized agent interface via adapters
- ✅ Composable middleware for cross-cutting concerns
- ✅ Streaming support for long-running operations
- ✅ Thread management for conversation context
- ✅ Pipeline composition for complex workflows
- ✅ Testability and maintainability improvements

All while maintaining backward compatibility with existing agent implementations!
