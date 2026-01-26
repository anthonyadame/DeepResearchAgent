# Testing Guide - Workflow Abstractions

## Overview

The workflow abstraction layer has **51+ unit tests** covering:
- Core contracts (Context, Results, Validation)
- Workflow definitions (Master, Supervisor, Researcher)
- Orchestrator registration and execution
- Integration scenarios
- Backward compatibility

## Test Organization

### Test File Structure

```
DeepResearchAgent.Tests/
└── Workflows/
    └── Abstractions/
        ├── WorkflowAbstractionTests.cs          # Core types
        ├── WorkflowDefinitionsTests.cs          # Workflow wrappers
        ├── WorkflowOrchestratorIntegrationTests.cs  # Integration
        ├── BackwardCompatibilityTests.cs        # Legacy support
        └── TestHelpers.cs                       # Utilities
```

### Test Categories

| Test Class | Tests | Purpose |
|-----------|-------|---------|
| WorkflowAbstractionTests | 20 | Core abstractions |
| WorkflowDefinitionsTests | 20 | Workflow wrappers |
| WorkflowOrchestratorIntegrationTests | 6 | Integration |
| BackwardCompatibilityTests | 6 | Legacy support |
| **Total** | **52** | **Comprehensive coverage** |

## Writing Tests

### 1. Testing WorkflowContext

```csharp
[Fact]
public void WorkflowContext_SetState_StoresAndRetrievesValue()
{
    // Arrange
    var context = new WorkflowContext();
    var testValue = "test data";

    // Act
    context.SetState("TestKey", testValue);
    var retrieved = context.GetState<string>("TestKey");

    // Assert
    Assert.Equal(testValue, retrieved);
}
```

### 2. Testing Validation

```csharp
[Fact]
public async Task MasterWorkflowDefinition_ExecuteAsync_ValidatesContext()
{
    // Arrange
    var mockWorkflow = CreateMockMasterWorkflow();
    var definition = new MasterWorkflowDefinition(mockWorkflow, logger);
    var context = new WorkflowContext(); // No UserQuery

    // Act
    var result = await definition.ExecuteAsync(context);

    // Assert
    Assert.False(result.Success);
    Assert.NotEmpty(result.Errors);
}
```

### 3. Testing Orchestrator Registration

```csharp
[Fact]
public void WorkflowOrchestrator_RegistersWorkflow()
{
    // Arrange
    var orchestrator = new WorkflowOrchestrator(mockLogger);
    var mockWorkflow = CreateMockWorkflowDefinition("TestWorkflow");

    // Act
    orchestrator.RegisterWorkflow(mockWorkflow);

    // Assert
    var registered = orchestrator.GetWorkflow("TestWorkflow");
    Assert.NotNull(registered);
    Assert.Equal("TestWorkflow", registered.WorkflowName);
}
```

### 4. Testing Execution

```csharp
[Fact]
public async Task Orchestrator_ExecutesMasterWorkflow()
{
    // Arrange
    var orchestrator = new WorkflowOrchestrator(mockLogger);
    var masterDef = CreateMockMasterDefinition("Final report");
    orchestrator.RegisterWorkflow(masterDef);
    var context = WorkflowExtensions.CreateMasterWorkflowContext("Test query");

    // Act
    var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);

    // Assert
    Assert.True(result.Success);
    Assert.NotNull(result.Output);
}
```

### 5. Testing Streaming

```csharp
[Fact]
public async Task MasterWorkflowDefinition_StreamExecutionAsync_YieldsUpdates()
{
    // Arrange
    var mockWorkflow = CreateMockMasterWorkflow();
    var definition = new MasterWorkflowDefinition(mockWorkflow, logger);
    var context = WorkflowExtensions.CreateMasterWorkflowContext("Test query");

    // Act
    var updates = new List<WorkflowUpdate>();
    await foreach (var update in definition.StreamExecutionAsync(context))
    {
        updates.Add(update);
    }

    // Assert
    Assert.NotEmpty(updates);
    Assert.True(updates.Any(u => u.Type == WorkflowUpdateType.StepStarted));
}
```

### 6. Testing Error Handling

```csharp
[Fact]
public async Task Orchestrator_HandlesWorkflowFailure()
{
    // Arrange
    var orchestrator = new WorkflowOrchestrator(mockLogger);
    var failingDef = CreateFailingWorkflowDefinition();
    orchestrator.RegisterWorkflow(failingDef);
    var context = new WorkflowContext();

    // Act
    var result = await orchestrator.ExecuteWorkflowAsync("FailingWorkflow", context);

    // Assert
    Assert.False(result.Success);
    Assert.NotEmpty(result.Errors);
}
```

### 7. Testing Backward Compatibility

```csharp
[Fact]
public async Task MasterWorkflow_RunAsync_StillWorks()
{
    // Arrange
    var mockWorkflow = CreateMockMasterWorkflow("Test result");

    // Act
    var result = await mockWorkflow.RunAsync("Test query");

    // Assert
    Assert.NotNull(result);
    Assert.NotEmpty(result);
}
```

## Mocking Patterns

### Mocking WorkflowDefinition

```csharp
private IWorkflowDefinition CreateMockWorkflowDefinition(string name)
{
    var mock = new Mock<IWorkflowDefinition>();
    mock.Setup(w => w.WorkflowName).Returns(name);
    mock.Setup(w => w.Description).Returns($"Description for {name}");
    mock.Setup(w => w.ValidateContext(It.IsAny<WorkflowContext>()))
        .Returns(new ValidationResult { IsValid = true });
    mock.Setup(w => w.ExecuteAsync(It.IsAny<WorkflowContext>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(new WorkflowExecutionResult { Success = true });

    return mock.Object;
}
```

### Mocking Workflows

```csharp
private MasterWorkflow CreateMockMasterWorkflow(string response = "Test")
{
    var mockWorkflow = new Mock<MasterWorkflow>(
        It.IsAny<ILightningStateService>(),
        It.IsAny<SupervisorWorkflow>(),
        It.IsAny<OllamaService>(),
        null, null, null, null, null, null
    );

    mockWorkflow
        .Setup(w => w.RunAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .ReturnsAsync(response);

    mockWorkflow
        .Setup(w => w.StreamAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
        .Returns(TestHelpers.CreateMockAsyncEnumerable(new[] { "Update" }));

    return mockWorkflow.Object;
}
```

### Creating Mock Async Enumerables

```csharp
public static async IAsyncEnumerable<T> CreateMockAsyncEnumerable<T>(
    IEnumerable<T> items)
{
    foreach (var item in items)
    {
        yield return item;
    }
}
```

## Running Tests

### Run All Tests

```bash
dotnet test
```

### Run Specific Test Class

```bash
dotnet test --filter "FullyQualifiedName~WorkflowAbstractionTests"
```

### Run Specific Test

```bash
dotnet test --filter "Name~MasterWorkflowDefinition_HasCorrectName"
```

### With Code Coverage

```bash
dotnet test /p:CollectCoverage=true /p:CoverageFormat=cobertura
```

### Run and Display Results

```bash
dotnet test --logger:"console;verbosity=detailed"
```

## Test Patterns

### AAA Pattern (Arrange-Act-Assert)

```csharp
[Fact]
public async Task Workflow_Scenario_ExpectedResult()
{
    // Arrange - Set up test data and mocks
    var context = WorkflowExtensions.CreateMasterWorkflowContext("query");
    var definition = new MasterWorkflowDefinition(mockWorkflow, mockLogger);

    // Act - Execute the code under test
    var result = await definition.ExecuteAsync(context);

    // Assert - Verify the results
    Assert.True(result.Success);
}
```

### Fluent Assertions

```csharp
Assert.NotNull(result);
Assert.True(result.Success);
Assert.Equal("Expected", result.Output);
Assert.Empty(result.Errors);
Assert.Single(result.ExecutedSteps);
Assert.Contains("item", result.ExecutedSteps);
```

## Testing Async Code

### Testing Async Methods

```csharp
[Fact]
public async Task Method_ReturnsCorrectResult()
{
    // Test async methods with async Task return
    var result = await orchestrator.ExecuteWorkflowAsync("Workflow", context);
    Assert.True(result.Success);
}
```

### Testing Async Enumerables

```csharp
[Fact]
public async Task StreamMethod_YieldsUpdates()
{
    var updates = new List<WorkflowUpdate>();
    await foreach (var update in definition.StreamExecutionAsync(context))
    {
        updates.Add(update);
    }
    Assert.NotEmpty(updates);
}
```

## Test Data Builders

### Creating Contexts

```csharp
var context = WorkflowExtensions
    .CreateMasterWorkflowContext("query")
    .WithDeadline(TimeSpan.FromMinutes(30))
    .WithMetadata("key", "value");
```

### Creating Results

```csharp
var result = new WorkflowExecutionResult
{
    Success = true,
    Output = "Output",
    Duration = TimeSpan.FromSeconds(5),
    ExecutedSteps = new List<string> { "Step1", "Step2" }
};
```

## Common Test Scenarios

### Test Success Path

```csharp
[Fact]
public async Task Workflow_WithValidInput_Succeeds()
{
    var context = WorkflowExtensions.CreateMasterWorkflowContext("query");
    var result = await definition.ExecuteAsync(context);
    
    Assert.True(result.Success);
    Assert.NotNull(result.Output);
    Assert.Empty(result.Errors);
}
```

### Test Failure Path

```csharp
[Fact]
public async Task Workflow_WithInvalidInput_Fails()
{
    var context = new WorkflowContext(); // No query
    var result = await definition.ExecuteAsync(context);
    
    Assert.False(result.Success);
    Assert.NotEmpty(result.Errors);
}
```

### Test Streaming

```csharp
[Fact]
public async Task Workflow_Streams_MultipleUpdates()
{
    var updates = new List<WorkflowUpdate>();
    await foreach (var update in definition.StreamExecutionAsync(context))
    {
        updates.Add(update);
    }
    
    Assert.NotEmpty(updates);
    Assert.Contains(updates, u => u.Type == WorkflowUpdateType.StepStarted);
}
```

### Test Deadline

```csharp
[Fact]
public void Context_WithPastDeadline_IsExceeded()
{
    var context = new WorkflowContext
    {
        Deadline = DateTime.UtcNow.AddSeconds(-1)
    };
    
    Assert.True(context.IsDeadlineExceeded);
}
```

## Performance Testing

### Measuring Duration

```csharp
[Fact]
public async Task Workflow_ExecutesWithinTimeLimit()
{
    var stopwatch = System.Diagnostics.Stopwatch.StartNew();
    var result = await definition.ExecuteAsync(context);
    stopwatch.Stop();
    
    Assert.True(result.Duration.TotalSeconds < 10);
    Assert.True(stopwatch.Elapsed.TotalSeconds < 10);
}
```

## Integration Test Examples

### Testing Pipeline

```csharp
[Fact]
public async Task Pipeline_ExecutesCompleteWorkflow()
{
    // Arrange
    var orchestrator = new WorkflowOrchestrator(mockLogger);
    orchestrator.RegisterWorkflow(CreateMockMasterDefinition());
    orchestrator.RegisterWorkflow(CreateMockSupervisorDefinition());
    orchestrator.RegisterWorkflow(CreateMockResearcherDefinition());

    // Act
    var result = await orchestrator.ExecuteWorkflowAsync("MasterWorkflow", context);

    // Assert
    Assert.True(result.Success);
}
```

## Best Practices

1. **Test public APIs** - Focus on public methods
2. **Use descriptive names** - Test names should explain what's being tested
3. **Keep tests focused** - One assertion per test when possible
4. **Mock external dependencies** - Isolate units under test
5. **Test error cases** - Don't just test the happy path
6. **Use fixtures** - Reuse common setup code
7. **Verify async behavior** - Test async patterns correctly
8. **Document complex tests** - Add comments for non-obvious logic

## Troubleshooting Tests

### Test Timeout

```csharp
[Fact(Timeout = 5000)] // 5 second timeout
public async Task Method_CompletesQuickly()
{
    // Test code
}
```

### Flaky Tests

- Avoid hardcoded delays
- Mock time-dependent behavior
- Use test fixtures for consistent state

### Assertion Failures

```csharp
// Provide helpful messages
Assert.True(result.Success, $"Expected success but got: {result.ErrorMessage}");
```

## Code Coverage Goals

- Target: ≥ 80% coverage for new code
- Critical paths: 100% coverage
- Error handling: Full coverage

## References

- [xUnit.net Documentation](https://xunit.net/)
- [Moq Documentation](https://github.com/moq/moq4)
- [Microsoft Test Patterns](https://docs.microsoft.com/en-us/dotnet/core/testing/)
