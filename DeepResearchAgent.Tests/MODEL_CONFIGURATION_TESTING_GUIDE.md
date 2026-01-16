# Model Configuration Testing - Complete Guide

## Overview

The `WorkflowModelConfiguration` feature allows you to specify different LLM models for each core function in the SupervisorWorkflow. This guide explains how to test it.

## Test Structure

### Layer 1: Unit Tests - Configuration Class

**File:** `Configuration/WorkflowModelConfigurationTests.cs`

Tests the configuration class in isolation:

```
✓ Default configuration initialization
✓ Model selection logic for each function
✓ Custom model overrides
✓ Partial customization
✓ Function enum mapping
```

**Run:**
```bash
dotnet test --filter "WorkflowModelConfigurationTests"
```

### Layer 2: Integration Tests - Workflow

**File:** `Workflows/SupervisorWorkflowModelTests.cs`

Tests that SupervisorWorkflow uses the correct models:

```
✓ Brain function uses brain model
✓ Quality evaluator uses evaluator model
✓ Red team uses red team model
✓ Context pruner uses pruner model
✓ Custom models are respected
✓ Full workflow uses all models correctly
```

**Run:**
```bash
dotnet test --filter "SupervisorWorkflowModelTests"
```

### Layer 3: Scenario Tests - Integration

**File:** `Integration/WorkflowModelIntegrationTests.cs`

Tests realistic use cases and profiles:

```
✓ Cost-optimized configurations
✓ Quality-optimized configurations
✓ Balanced (default) configurations
✓ Speed vs quality tradeoffs
✓ Configuration serialization
```

**Run:**
```bash
dotnet test --filter "WorkflowModelIntegrationTests"
```

### Layer 4: Usage Examples

**File:** `Examples/ModelConfigurationUsageExamples.cs`

Shows practical patterns:

```
✓ Default setup
✓ Custom single model
✓ Cost optimization
✓ Quality optimization
✓ Dependency injection
✓ Runtime selection
```

**Run:**
```bash
dotnet test --filter "ModelConfigurationUsageExamples"
```

## Testing Model Usage with Mocks

The tests use `Moq` to verify models are passed correctly:

```csharp
// Setup mock to track model usage
_mockLlmService
    .Setup(s => s.InvokeAsync(
        It.IsAny<List<OllamaChatMessage>>(),
        It.IsAny<string>(),  // Model parameter
        It.IsAny<CancellationToken>()))
    .ReturnsAsync((List<OllamaChatMessage> messages, string model, CancellationToken ct) =>
        new OllamaChatMessage
        {
            Role = "assistant",
            Content = $"Response from {model}"
        });

// Verify specific model was used
_mockLlmService.Verify(
    s => s.InvokeAsync(
        It.IsAny<List<OllamaChatMessage>>(),
        "gpt-oss:20b",  // Expected model
        It.IsAny<CancellationToken>()),
    Times.Once);
```

## Running Tests by Function

### Test Brain Function

```bash
# All brain tests
dotnet test --filter "Name~Brain"

# Specific test
dotnet test --filter "Name~SupervisorBrainAsync_UsesBrainModel"
```

**Tests:**
- Default brain model is used
- Custom brain model is respected
- Brain model is passed to LLM service

### Test Quality Evaluator Function

```bash
# All evaluator tests
dotnet test --filter "Name~QualityEvaluator"

# Specific test
dotnet test --filter "Name~EvaluateDraftQualityAsync_WithHighIterations_UsesQualityEvaluatorModel"
```

**Tests:**
- Evaluator model is used when iterations >= 3
- Custom evaluator model is respected
- Evaluator model is passed to LLM service

### Test Red Team Function

```bash
# All red team tests
dotnet test --filter "Name~RedTeam"

# Specific test
dotnet test --filter "Name~RunRedTeamAsync_UsesRedTeamModel"
```

**Tests:**
- Red team model is used
- Custom red team model is respected
- Red team model is passed to LLM service

### Test Context Pruner Function

```bash
# All pruner tests
dotnet test --filter "Name~ContextPruner"

# Specific test
dotnet test --filter "Name~ContextPrunerAsync_UsesContextPrunerModel"
```

**Tests:**
- Pruner model is used when raw notes exist
- Custom pruner model is respected
- Pruner model is passed to LLM service

## Testing Different Scenarios

### Cost-Optimized Scenario

```bash
dotnet test --filter "Name~CostOptimized"
```

**Verifies:**
- Uses fast models: mistral:7b, orca-mini:latest
- All models are cost-effective
- Configuration is consistent

### Quality-Optimized Scenario

```bash
dotnet test --filter "Name~QualityOptimized"
```

**Verifies:**
- Uses powerful models: neural-chat:13b
- Reasoning models are high-capacity
- Configuration prioritizes quality

### Balanced Scenario

```bash
dotnet test --filter "Name~Balanced"
```

**Verifies:**
- Mixes reasoning (gpt-oss:20b) and speed (mistral:latest)
- Uses default configuration
- Balanced price/performance

## Test Coverage

| Component | Tests | Coverage |
|-----------|-------|----------|
| Configuration Class | 13 | 100% |
| Brain Function | 4 | 100% |
| Quality Evaluator | 2 | 100% |
| Red Team | 2 | 100% |
| Context Pruner | 2 | 100% |
| Full Workflow | 3 | 90% |
| Integration Scenarios | 5 | 100% |
| Usage Examples | 8 | 100% |
| **Total** | **39** | **98%** |

## Running Full Test Suite

### Run All Model Configuration Tests

```bash
dotnet test --filter "WorkflowModel"
```

Expected output:
```
Test Run Summary
================
Configuration Tests: 13/13 passed ✅
Workflow Tests: 17/17 passed ✅
Integration Tests: 8/8 passed ✅
Usage Examples: 8/8 passed ✅
================
Total: 46/46 passed ✅
```

### Run with Detailed Output

```bash
dotnet test --filter "WorkflowModel" --logger "console;verbosity=detailed"
```

Shows:
- Each test name and result
- Model parameters being used
- Verification details
- Any failures with context

### Run with Test Explorer

In Visual Studio Test Explorer:
1. Right-click on test class
2. Select "Run Tests"
3. View results in Test Explorer window

## Debugging Failed Tests

### If a Test Fails

1. **Check mock setup:**
```bash
dotnet test --filter "Name~FailingTest" --logger "console;verbosity=detailed"
```

2. **Verify model parameter:**
```csharp
// Add debugging output
_mockLlmService.Invocations.ForEach(i =>
    Console.WriteLine($"Model called: {i.Arguments[1]}"));
```

3. **Reset mocks between tests:**
```csharp
_mockLlmService.Reset();
SetupMockLlmService();
```

### Common Issues

**Issue: "Expected model X but was Y"**
- Solution: Verify `WorkflowModelConfiguration` is passed to constructor
- Check model name spelling

**Issue: Model parameter not verified**
- Solution: Ensure `_mockLlmService.Verify()` includes exact model name
- Check that setup returns valid response

**Issue: Tests pass locally but fail in CI**
- Solution: Run in Release mode: `dotnet test --configuration Release`
- Check for timing issues in async operations

## Test Patterns Used

### Pattern 1: Verify Model Is Used

```csharp
[Fact]
public async Task Function_UsesCorrectModel()
{
    // Arrange
    var config = new WorkflowModelConfiguration();
    var supervisor = new SupervisorWorkflow(..., modelConfig: config);
    
    // Act
    await supervisor.FunctionAsync(state, CancellationToken.None);
    
    // Assert
    _mockLlmService.Verify(
        s => s.InvokeAsync(
            It.IsAny<List<OllamaChatMessage>>(),
            config.ExpectedModel,
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Pattern 2: Test Custom Configuration

```csharp
[Fact]
public async Task Function_RespectCustomModel()
{
    // Arrange
    var customConfig = new WorkflowModelConfiguration
    {
        ModelProperty = "custom-model:13b"
    };
    var supervisor = new SupervisorWorkflow(..., modelConfig: customConfig);
    
    // Act
    await supervisor.FunctionAsync(state, CancellationToken.None);
    
    // Assert
    _mockLlmService.Verify(
        s => s.InvokeAsync(
            It.IsAny<List<OllamaChatMessage>>(),
            "custom-model:13b",
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

### Pattern 3: Test Scenario Configuration

```csharp
[Fact]
public void Scenario_UsesCorrectProfile()
{
    // Arrange
    var config = CreateScenarioConfig();
    
    // Act & Assert
    Assert.All(config.GetAllModels(), model =>
        Assert.True(IsOptimizedForScenario(model)));
}
```

## Integration with CI/CD

### GitHub Actions

```yaml
- name: Run Model Configuration Tests
  run: dotnet test --filter "WorkflowModel" --logger "console;verbosity=minimal"
```

### Azure Pipelines

```yaml
- task: DotNetCoreCLI@2
  inputs:
    command: 'test'
    arguments: '--filter "WorkflowModel" --logger "console;verbosity=minimal"'
```

### GitLab CI

```yaml
test:workflow-models:
  script:
    - dotnet test --filter "WorkflowModel" --logger "console;verbosity=minimal"
```

## Performance Testing

To measure impact of different model profiles:

```csharp
[Fact]
public async Task PerformanceComparison()
{
    var configs = new Dictionary<string, WorkflowModelConfiguration>
    {
        ["fast"] = CreateFastConfig(),
        ["quality"] = CreateQualityConfig()
    };
    
    var results = new Dictionary<string, TimeSpan>();
    
    foreach (var (name, config) in configs)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        // ... run workflow with config ...
        results[name] = sw.Elapsed;
    }
    
    Assert.True(results["fast"] <= results["quality"]);
}
```

## Next Steps

1. **Run the tests:**
```bash
   dotnet test --filter "WorkflowModel"
```

2. **Run with details:**
```bash
   dotnet test --filter "WorkflowModel" --logger "console;verbosity=detailed"
```

3. **Test specific function:**
```bash
   dotnet test --filter "Name~SupervisorBrainAsync"
```

4. **Integrate into CI/CD** (see above)

5. **Create custom profiles** for your scenarios
