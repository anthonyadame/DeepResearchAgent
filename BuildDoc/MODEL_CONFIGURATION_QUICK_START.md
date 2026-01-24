# Testing Model Configuration in SupervisorWorkflow - Quick Start

## Run Tests Immediately

```bash
# Run all model configuration tests
dotnet test --filter "WorkflowModel"

# Run with detailed output
dotnet test --filter "WorkflowModel" --logger "console;verbosity=detailed"
```

## Test Files Created

### Configuration Tests
- **File:** `DeepResearchAgent.Tests/Configuration/WorkflowModelConfigurationTests.cs`
- **Tests:** 13 unit tests
- **Focus:** Configuration class initialization and model selection logic

Run with:
```bash
dotnet test --filter "WorkflowModelConfigurationTests"
```

### Workflow Integration Tests
- **File:** `DeepResearchAgent.Tests/Workflows/SupervisorWorkflowModelTests.cs`
- **Tests:** 17 integration tests
- **Focus:** Verifies SupervisorWorkflow uses correct models for each function

Run with:
```bash
dotnet test --filter "SupervisorWorkflowModelTests"
```

### Integration Scenario Tests
- **File:** `DeepResearchAgent.Tests/Integration/WorkflowModelIntegrationTests.cs`
- **Tests:** 8 scenario tests
- **Focus:** Cost-optimized, quality-optimized, and balanced configurations

Run with:
```bash
dotnet test --filter "WorkflowModelIntegrationTests"
```

### Usage Examples
- **File:** `DeepResearchAgent.Tests/Examples/ModelConfigurationUsageExamples.cs`
- **Tests:** 8 usage examples
- **Focus:** Practical patterns for using model configuration

Run with:
```bash
dotnet test --filter "ModelConfigurationUsageExamples"
```

## What Each Test Verifies

### Configuration Tests (13 tests)

```
✅ Default configuration has all models set
✅ Default models use optimized strategy (reasoning: gpt-oss:20b, tools: mistral:latest)
✅ Model selection routes to correct function
✅ Custom model overrides work
✅ Partial customization preserves defaults
✅ Function enum maps to correct models
✅ All functions return non-empty model names
```

### Workflow Integration Tests (17 tests)

```
✅ SupervisorBrain uses brain model
✅ SupervisorBrain respects custom brain model
✅ QualityEvaluator uses evaluator model
✅ QualityEvaluator respects custom evaluator model
✅ RedTeam uses red team model
✅ RedTeam respects custom red team model
✅ ContextPruner uses pruner model
✅ ContextPruner respects custom pruner model
✅ Full workflow uses all models correctly
✅ Null configuration is handled gracefully
✅ Missing configuration parameter uses defaults
```

### Integration Tests (8 tests)

```
✅ Cost-optimized uses fast models (mistral, orca)
✅ Quality-optimized uses powerful models (neural-chat:13b)
✅ Balanced mixes speed and quality
✅ Fast research scenario optimizes for speed
✅ Deep analysis scenario optimizes for quality
✅ Configuration can be serialized and restored
```

### Usage Examples (8 tests)

```
✅ Default configuration setup
✅ Single model customization
✅ Cost-optimized configuration
✅ Quality-optimized configuration
✅ Dependency injection with defaults
✅ Dependency injection with custom models
✅ Dynamic model selection based on scenario
✅ Testing with specific models
```

## Quick Test Commands

### Run All Tests
```bash
dotnet test --filter "WorkflowModel"
```

### Run Specific Category
```bash
# Configuration only
dotnet test --filter "WorkflowModelConfigurationTests"

# Workflow tests only
dotnet test --filter "SupervisorWorkflowModelTests"

# Integration tests only
dotnet test --filter "WorkflowModelIntegrationTests"

# Usage examples only
dotnet test --filter "ModelConfigurationUsageExamples"
```

### Run Specific Test
```bash
# Test brain model selection
dotnet test --filter "Name~SupervisorBrainAsync_UsesBrainModel"

# Test custom configuration
dotnet test --filter "Name~CustomConfiguration_AllowsModelOverride"

# Test cost optimization
dotnet test --filter "Name~CostOptimizedConfiguration"
```

### With Detailed Output
```bash
dotnet test --filter "WorkflowModel" --logger "console;verbosity=detailed"
```

## Verifying Model Usage

The tests verify that correct models are passed to `OllamaService.InvokeAsync()`:

```csharp
[Fact]
public async Task SupervisorBrainAsync_UsesBrainModel()
{
    // Arrange
    var config = new WorkflowModelConfiguration();
    var supervisor = new SupervisorWorkflow(..., modelConfig: config);
    
    // Act
    await supervisor.SupervisorBrainAsync(state, CancellationToken.None);
    
    // Assert - Verify correct model was passed
    _mockLlmService.Verify(
        s => s.InvokeAsync(
            It.IsAny<List<OllamaChatMessage>>(),
            config.SupervisorBrainModel,  // Expected model
            It.IsAny<CancellationToken>()),
        Times.Once);
}
```

## Model Configuration Defaults

| Function | Default Model | Purpose |
|----------|---------------|---------|
| Supervisor Brain | `gpt-oss:20b` | Complex reasoning |
| Supervisor Tools | `mistral:latest` | Fast coordination |
| Quality Evaluator | `gpt-oss:20b` | Detailed analysis |
| Red Team | `gpt-oss:20b` | Critical thinking |
| Context Pruner | `mistral:latest` | Quick extraction |

## Common Test Scenarios

### Test 1: Verify Defaults Are Optimized

```bash
dotnet test --filter "Name~DefaultConfiguration"
```

Expected: All default models are set and follow optimization strategy.

### Test 2: Verify Custom Models Are Used

```bash
dotnet test --filter "Name~WithCustomModel"
```

Expected: SupervisorWorkflow uses custom models when configured.

### Test 3: Verify Cost-Optimized Configuration

```bash
dotnet test --filter "Name~CostOptimized"
```

Expected: Configuration uses fast/small models (mistral, orca-mini).

### Test 4: Verify Quality-Optimized Configuration

```bash
dotnet test --filter "Name~QualityOptimized"
```

Expected: Configuration uses powerful models (neural-chat:13b).

### Test 5: Verify Full Workflow

```bash
dotnet test --filter "Name~ExecuteAsync_UsesCorrectModels"
```

Expected: Full workflow execution uses all models in sequence.

## Expected Test Results

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

## Troubleshooting

### Tests Fail: "Model Not Found"

Check that:
1. `WorkflowModelConfiguration` is properly instantiated
2. Model parameter is passed to `_llmService.InvokeAsync()`
3. Mock setup includes the model parameter

### Unexpected Model Used

Check that:
1. Configuration is passed to SupervisorWorkflow constructor
2. No typos in model names
3. Reset mocks between tests if needed

### Test Isolation Issues

Run tests individually:
```bash
dotnet test --filter "Name~SpecificTest"
```

## Next Steps

1. **Run all tests:**
```
   dotnet test --filter "WorkflowModel"
```

2. **Verify in your environment:**
```
   dotnet test --filter "WorkflowModelConfigurationTests" --logger "console;verbosity=detailed"
```

3. **Create custom profiles for your use cases**

4. **Integrate into your CI/CD pipeline**
