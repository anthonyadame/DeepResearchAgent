# Test Structure Best Practices for Deep Research Agent

## 📋 Table of Contents

1. [Project Organization](#project-organization)
2. [Naming Conventions](#naming-conventions)
3. [Test Classification](#test-classification)
4. [Fixture & Mock Organization](#fixture--mock-organization)
5. [Test Data Management](#test-data-management)
6. [Assertion Patterns](#assertion-patterns)
7. [Test Base Classes](#test-base-classes)
8. [CI/CD Integration](#cicd-integration)
9. [Performance & Load Testing](#performance--load-testing)
10. [Test Execution Guidelines](#test-execution-guidelines)

---

## 🏗️ Project Organization

### Recommended Folder Structure

```
DeepResearchAgent.Tests/
├── Unit/
│   ├── Services/
│   │   ├── AgentLightningServiceTests.cs
│   │   ├── LightningVERLServiceTests.cs
│   │   └── LightningAPOConfigTests.cs
│   ├── Workflows/
│   │   ├── MasterWorkflowTests.cs
│   │   ├── SupervisorWorkflowTests.cs
│   │   └── ResearcherWorkflowTests.cs
│   └── Models/
│       └── DataModelTests.cs
├── Integration/
│   ├── Workflows/
│   │   └── WorkflowIntegrationTests.cs
│   ├── Docker/
│   │   └── DockerServiceIntegrationTests.cs
│   └── Lightning/
│       └── LightningIntegrationTests.cs
├── Performance/
│   ├── Benchmarks/
│   │   ├── ResearcherBenchmarks.cs
│   │   ├── SupervisorBenchmarks.cs
│   │   └── MasterBenchmarks.cs
│   └── LoadTests/
│       └── ConcurrencyTests.cs
├── Error/
│   ├── ResilienceTests.cs
│   └── EdgeCaseTests.cs
├── Fixtures/
│   ├── ServiceFixtures.cs
│   ├── WorkflowFixtures.cs
│   ├── LightningServerFixture.cs
│   └── DataFixtures.cs
├── Mocks/
│   ├── MockServices.cs
│   ├── MockWorkflows.cs
│   └── MockHttpMessageHandler.cs
├── Helpers/
│   ├── TestDataBuilder.cs
│   ├── TestDataFactory.cs
│   ├── AssertionExtensions.cs
│   ├── DockerHealthCheck.cs
│   └── TestOutputWriter.cs
├── Collections/
│   ├── UnitTestCollection.cs
│   ├── IntegrationTestCollection.cs
│   ├── PerformanceTestCollection.cs
│   └── LightningServerCollection.cs
├── Base/
│   ├── AsyncTestBase.cs
│   └── ServiceTestBase.cs
└── DeepResearchAgent.Tests.csproj
```

### Key Principles

- **One responsibility per test file** - Each test class focuses on one component
- **Folder structure mirrors source code** - Easy navigation and understanding
- **Shared fixtures in dedicated folder** - Reusable setup/teardown logic
- **Helpers separate from tests** - Clear separation of concerns
- **Collections for test organization** - Control parallelization and ordering

---

## 📝 Naming Conventions

### Test Method Naming

**Format:** `MethodUnderTest_Scenario_ExpectedBehavior`

#### ✅ GOOD Examples

```csharp
[Fact]
public async Task RegisterAgentAsync_WithValidInput_ReturnsActiveRegistration()
{ }

[Theory]
[InlineData(null)]
[InlineData("")]
public async Task RegisterAgentAsync_WithInvalidAgentId_ThrowsArgumentException(string agentId)
{ }

[Fact]
public async Task SubmitTaskAsync_WhenServerUnavailable_HandlesGracefully()
{ }

[Fact]
public async Task GetPendingTasksAsync_WithMultipleTasks_ReturnsAllTasks()
{ }
```

#### ❌ AVOID Examples

```csharp
[Fact]
public async Task TestRegistration()  // Too vague

[Fact]
public void Test1()  // Meaningless

[Fact]
public async Task TestMethod()  // Generic

[Fact]
public async Task DoesTheThingCorrectly()  // Not specific enough
```

### Test Class Naming

**Format:** 
- Unit tests: `{ComponentName}Tests`
- Integration tests: `{Feature}IntegrationTests`
- Performance tests: `{ComponentName}Benchmarks` or `{Feature}PerformanceTests`

#### ✅ GOOD Examples

```csharp
public class AgentLightningServiceTests { }
public class LightningVERLServiceTests { }
public class WorkflowIntegrationTests { }
public class ResilienceTests { }
public class ResearcherBenchmarks { }
```

#### ❌ AVOID Examples

```csharp
public class TestAgentLightning { }  // Prefix naming
public class Tests { }  // Too generic
public class UnitTests { }  // Redundant
public class ServiceTest { }  // Singular instead of plural
```

### Fixture Class Naming

**Format:** `{ComponentName}Fixture` or `{Feature}TestFixture`

#### ✅ GOOD Examples

```csharp
public class LightningServerFixture : IAsyncLifetime { }
public class OllamaServiceFixture : IAsyncLifetime { }
public class WorkflowTestFixture : IAsyncLifetime { }
public class LightningServiceFixture : IAsyncLifetime { }
```

#### ❌ AVOID Examples

```csharp
public class Setup { }  // Too generic
public class TestSetup { }  // Redundant naming
public class Helpers { }  // Not a fixture
```

### Data Builder Class Naming

**Format:** `{Entity}Builder` or `Test{Entity}Builder`

```csharp
public class TestDataBuilder { }  // Generic builder for all test data
public class AgentTaskBuilder { }  // Specialized builder
public class ReasoningStepBuilder { }  // Specialized builder
```

---

## 🏷️ Test Classification

### Using XUnit Traits

Traits allow you to categorize, filter, and organize tests by multiple dimensions:

```csharp
[Fact]
[Trait("Category", "Unit")]
[Trait("Component", "Lightning")]
[Trait("Feature", "HealthCheck")]
public async Task IsHealthyAsync_WhenServerResponds_ReturnsTrue()
{
    // Test implementation
}

[Fact]
[Trait("Category", "Integration")]
[Trait("Component", "Lightning")]
[Trait("RequiresDocker", "true")]
public async Task FullWorkflow_WithAllServices_Succeeds()
{
    // Test implementation
}

[Fact]
[Trait("Category", "Performance")]
[Trait("Component", "Researcher")]
[Trait("Benchmark", "SingleQuery")]
[Trait("Threshold", "30000")]  // milliseconds
public async Task Researcher_SingleQuery_CompletesUnderThreshold()
{
    // Test implementation
}
```

### Standard Trait Dimensions

| Trait | Values | Purpose |
|-------|--------|---------|
| **Category** | Unit, Integration, Performance, Error | Test type classification |
| **Component** | Lightning, Ollama, SearXNG, Crawl4AI, Workflow | Component being tested |
| **Feature** | HealthCheck, Registration, TaskManagement, etc. | Feature being tested |
| **RequiresDocker** | true, false | Dependency indicator |
| **Benchmark** | SingleQuery, Parallel, Throughput, etc. | Performance benchmark type |
| **Threshold** | milliseconds, bytes, etc. | Performance threshold |

### Running Tests by Traits

```bash
# Run only unit tests
dotnet test --filter "Category=Unit"

# Run only integration tests
dotnet test --filter "Category=Integration"

# Run only performance tests
dotnet test --filter "Category=Performance"

# Run tests that don't require Docker
dotnet test --filter "RequiresDocker!=true"

# Run Lightning component tests
dotnet test --filter "Component=Lightning"

# Run specific feature tests
dotnet test --filter "Feature=Registration"

# Combine multiple filters
dotnet test --filter "Category=Integration & Component=Lightning"
```

### XUnit Collections for Test Isolation

Collections prevent tests from running in parallel:

```csharp
[CollectionDefinition("Lightning Server Collection", DisableParallelization = true)]
public class LightningServerCollection : ICollectionFixture<LightningServerFixture>
{
}

[Collection("Lightning Server Collection")]
public class LightningIntegrationTests
{
    private readonly LightningServerFixture _fixture;

    public LightningIntegrationTests(LightningServerFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Test1() { }
}
```

---

## 🔧 Fixture & Mock Organization

### Centralized Fixture Factory Pattern

```csharp
namespace DeepResearchAgent.Tests.Fixtures;

public static class TestFixtureFactory
{
    public static (IAgentLightningService Service, HttpClient Client) 
        CreateLightningServiceFixture()
    {
        var httpClient = CreateMockHttpClient(HttpStatusCode.OK);
        var service = new AgentLightningService(httpClient, "http://localhost:9090");
        return (service, httpClient);
    }

    public static HttpClient CreateMockHttpClient(
        HttpStatusCode statusCode = HttpStatusCode.OK,
        string? responseContent = null)
    {
        var handler = new MockHttpMessageHandler(
            new HttpResponseMessage
            {
                StatusCode = statusCode,
                Content = responseContent != null 
                    ? new StringContent(responseContent, Encoding.UTF8, "application/json")
                    : new StringContent("{}", Encoding.UTF8, "application/json")
            }
        );
        return new HttpClient(handler);
    }

    public static HttpClient CreateFailingHttpClient(string exceptionMessage = "Connection failed")
    {
        var handler = new MockHttpMessageHandler(
            throw: new HttpRequestException(exceptionMessage)
        );
        return new HttpClient(handler);
    }
}
```

### Fixture Implementation with IAsyncLifetime

```csharp
namespace DeepResearchAgent.Tests.Fixtures;

public class LightningServiceFixture : IAsyncLifetime
{
    private readonly HttpClient _httpClient;
    
    public IAgentLightningService Service { get; private set; }

    public LightningServiceFixture()
    {
        _httpClient = TestFixtureFactory.CreateMockHttpClient();
    }

    public Task InitializeAsync()
    {
        Service = new AgentLightningService(_httpClient, "http://localhost:9090");
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _httpClient?.Dispose();
        return Task.CompletedTask;
    }
}
```

---

## 📊 Test Data Management

### Fluent Test Data Builder

```csharp
namespace DeepResearchAgent.Tests.Helpers;

public class TestDataBuilder
{
    private string _agentId = Guid.NewGuid().ToString();
    private string _agentType = "TestAgent";
    private Dictionary<string, object> _capabilities = new();
    private TaskStatus _status = TaskStatus.Submitted;
    private double _confidence = 0.85;

    public TestDataBuilder WithAgentId(string agentId)
    {
        _agentId = agentId;
        return this;
    }

    public TestDataBuilder WithCapability(string name, bool enabled = true)
    {
        _capabilities[name] = enabled;
        return this;
    }

    public TestDataBuilder WithConfidence(double confidence)
    {
        _confidence = confidence;
        return this;
    }

    public AgentRegistration BuildAgentRegistration()
        => new()
        {
            AgentId = _agentId,
            AgentType = _agentType,
            Capabilities = _capabilities,
            RegisteredAt = DateTime.UtcNow,
            IsActive = true
        };

    public VerificationResult BuildVerificationResult()
        => new()
        {
            TaskId = Guid.NewGuid().ToString(),
            IsValid = _confidence >= 0.75,
            Confidence = _confidence,
            VerifiedAt = DateTime.UtcNow
        };
}
```

### Test Data Factory

```csharp
namespace DeepResearchAgent.Tests.Helpers;

public static class TestDataFactory
{
    public static AgentRegistration CreateValidResearchAgent()
        => new TestDataBuilder()
            .WithAgentId("test-research-agent")
            .WithAgentType("ResearchOrchestrator")
            .WithCapability("research", true)
            .WithCapability("synthesis", true)
            .WithCapability("verification", true)
            .BuildAgentRegistration();

    public static VerificationResult CreateVerificationResult(
        double confidence = 0.95,
        bool isValid = true)
        => new TestDataBuilder()
            .WithConfidence(confidence)
            .BuildVerificationResult();
}
```

---

## ✅ Assertion Patterns

### Custom Assertion Extensions

```csharp
namespace DeepResearchAgent.Tests.Helpers;

public static class LightningAssertions
{
    public static void ShouldBeValidAgentRegistration(this AgentRegistration? registration)
    {
        Assert.NotNull(registration);
        Assert.NotEmpty(registration.AgentId);
        Assert.NotEmpty(registration.AgentType);
        Assert.True(registration.IsActive, "Agent should be active");
    }

    public static void ShouldHaveHighConfidence(
        this VerificationResult? result, 
        double threshold = 0.80)
    {
        Assert.NotNull(result);
        Assert.True(result.Confidence >= threshold, 
            $"Expected confidence >= {threshold}, got {result.Confidence}");
    }
}
```

### Assertion Patterns - GOOD vs AVOID

#### ✅ GOOD - Specific and Meaningful

```csharp
[Fact]
public async Task SubmitTaskAsync_WithValidTask_ReturnsPendingStatus()
{
    var task = new TestDataBuilder().BuildAgentTask();
    var result = await _service.SubmitTaskAsync("agent-1", task);
    
    Assert.NotNull(result);
    Assert.NotEmpty(result.TaskId);
    Assert.Equal(TaskStatus.Submitted, result.Status);
}
```

#### ❌ AVOID - Too Vague

```csharp
[Fact]
public async Task SubmitTaskAsync_Works()
{
    var result = await _service.SubmitTaskAsync("agent-1", new AgentTask());
    Assert.NotNull(result);  // Too vague
}
```

---

## 🏛️ Test Base Classes

### Async Test Base Class

```csharp
namespace DeepResearchAgent.Tests.Base;

public abstract class AsyncTestBase : IAsyncLifetime
{
    protected ITestOutputHelper Output { get; }
    protected CancellationTokenSource CancellationTokenSource { get; }

    protected AsyncTestBase(ITestOutputHelper output)
    {
        Output = output;
        CancellationTokenSource = new CancellationTokenSource();
    }

    public virtual Task InitializeAsync() => Task.CompletedTask;

    public virtual async Task DisposeAsync()
    {
        CancellationTokenSource.Dispose();
        await Task.CompletedTask;
    }

    protected async Task<(T Result, TimeSpan Elapsed)> MeasureAsync<T>(Task<T> task)
    {
        var stopwatch = Stopwatch.StartNew();
        var result = await task;
        stopwatch.Stop();
        return (result, stopwatch.Elapsed);
    }

    protected void WriteOutput(string message)
    {
        Output?.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] {message}");
    }
}
```

### Usage in Tests

```csharp
[Collection("Lightning Services")]
public class AgentLightningServiceTests : AsyncTestBase
{
    private readonly LightningServiceFixture _fixture;

    public AgentLightningServiceTests(
        LightningServiceFixture fixture,
        ITestOutputHelper output)
        : base(output)
    {
        _fixture = fixture;
    }

    [Fact]
    [Trait("Category", "Unit")]
    public async Task RegisterAgentAsync_WithValidInput_CompletesQuickly()
    {
        var registration = TestDataFactory.CreateValidResearchAgent();
        var (result, elapsed) = await MeasureAsync(
            _fixture.Service.RegisterAgentAsync(
                registration.AgentId,
                registration.AgentType,
                registration.Capabilities
            )
        );

        WriteOutput($"Registration completed in {elapsed.TotalMilliseconds}ms");
        Assert.True(elapsed.TotalSeconds < 5);
        result.ShouldBeValidAgentRegistration();
    }
}
```

---

## ⚙️ CI/CD Integration

### XUnit Configuration

**File:** `DeepResearchAgent.Tests/xunit.runner.json`

```json
{
  "$schema": "https://xunit.net/schema/current/xunit.runner.schema.json",
  "diagnosticMessages": false,
  "methodDisplay": "method",
  "methodDisplayOptions": "all",
  "shadowCopy": false,
  "appDomain": "denied",
  "parallelizeAssembly": true,
  "parallelizeTestCollections": true,
  "maxParallelThreads": 4,
  "preEnumerateTheories": true,
  "longRunningTestSeconds": 10
}
```

### GitHub Actions Workflow

**File:** `.github/workflows/tests.yml`

```yaml
name: Tests & Coverage

on:
  push:
    branches: [ main, develop ]
  pull_request:
    branches: [ main, develop ]

jobs:
  test:
    runs-on: ubuntu-latest
    
    steps:
    - uses: actions/checkout@v3
    
    - name: Setup .NET
      uses: actions/setup-dotnet@v3
      with:
        dotnet-version: '8.0.x'
    
    - name: Restore dependencies
      run: dotnet restore
    
    - name: Build
      run: dotnet build --configuration Release --no-restore
    
    - name: Run Unit Tests
      run: |
        dotnet test DeepResearchAgent.Tests \
          --configuration Release \
          --filter "Category=Unit" \
          --logger "json" \
          --no-build
    
    - name: Run Integration Tests
      run: |
        docker-compose -f docker-compose.yml up -d
        sleep 30
        dotnet test DeepResearchAgent.Tests \
          --configuration Release \
          --filter "Category=Integration" \
          --logger "json" \
          --no-build
        docker-compose -f docker-compose.yml down
      continue-on-error: true
    
    - name: Collect Coverage
      run: |
        dotnet test DeepResearchAgent.Tests \
          --configuration Release \
          --no-build \
          /p:CollectCoverage=true \
          /p:CoverageFormat=opencover
    
    - name: Upload Coverage
      uses: codecov/codecov-action@v3
      with:
        files: ./coverage.opencover.xml
        fail_ci_if_error: false
```

---

## ⚡ Performance & Load Testing

### Performance Test Structure

```csharp
[Trait("Category", "Performance")]
[Trait("Component", "Researcher")]
public class ResearcherBenchmarks : AsyncTestBase
{
    [Fact]
    [Trait("Benchmark", "SingleQuery")]
    public async Task Researcher_SingleQuery_CompletesUnderThreshold()
    {
        const int ThresholdMs = 30000;
        var (_, elapsed) = await MeasureAsync(
            Task.Delay(100)  // Placeholder
        );

        WriteOutput($"Query completed in {elapsed.TotalMilliseconds}ms");
        Assert.True(elapsed.TotalMilliseconds < ThresholdMs);
    }
}
```

---

## 📋 Test Execution Guidelines

### Standard Test Commands

```bash
# Run all tests
dotnet test DeepResearchAgent.Tests

# Run only unit tests
dotnet test DeepResearchAgent.Tests --filter "Category=Unit"

# Run only integration tests
dotnet test DeepResearchAgent.Tests --filter "Category=Integration"

# Run specific test class
dotnet test DeepResearchAgent.Tests --filter "ClassName=AgentLightningServiceTests"

# Run with verbose output
dotnet test DeepResearchAgent.Tests --verbosity detailed

# Run with code coverage
dotnet test DeepResearchAgent.Tests /p:CollectCoverage=true /p:CoverageFormat=opencover
```

---

## 📊 Summary: Key Best Practices

| Category | Best Practice |
|----------|---------------|
| **Organization** | Folder structure mirrors source code |
| **Naming** | `Method_Scenario_Expected` format |
| **Classification** | Use Traits for categorization |
| **Fixtures** | Centralized factory pattern |
| **Data** | Fluent builders for test data |
| **Assertions** | Custom extensions for clarity |
| **Base Classes** | Shared functionality via inheritance |
| **CI/CD** | Automated execution with coverage |
| **Documentation** | Clear meaningful names |
| **Isolation** | Independent, order-agnostic tests |

---

**Last Updated:** 2024
**Version:** 2.0
**Status:** Production Ready

## 🎉 Test Structure Best Practices Document Complete!

This comprehensive guide has been successfully created and updated with:

✅ **Documentation Files:**
- `TEST_STRUCTURE_BEST_PRACTICES.md` - Complete best practices guide (comprehensive)
- `TEST_STRUCTURE_QUICK_START.md` - Quick start guide for implementation

✅ **Foundation Files Created:**
1. `DeepResearchAgent.Tests/Base/AsyncTestBase.cs` - Base class for async tests
2. `DeepResearchAgent.Tests/Helpers/AssertionExtensions.cs` - Custom assertion extensions
3. `DeepResearchAgent.Tests/Helpers/TestDataBuilder.cs` - Fluent test data builder
4. `DeepResearchAgent.Tests/Helpers/TestDataFactory.cs` - Test data factory methods
5. `DeepResearchAgent.Tests/Collections/TestCollections.cs` - XUnit collection definitions
6. `DeepResearchAgent.Tests/Fixtures/LightningServerFixture.cs` - Lightning server fixture

---

## 📖 How to Use This Guide

### 1. **Read the Best Practices Document**
   - Start with `TEST_STRUCTURE_BEST_PRACTICES.md`
   - Understand the recommended organization structure
   - Learn naming conventions and patterns

### 2. **Use the Quick Start Guide**
   - Follow `TEST_STRUCTURE_QUICK_START.md`
   - Implement patterns step-by-step
   - Copy examples into your tests

### 3. **Leverage the Foundation Files**
   - Inherit from `AsyncTestBase` for test timing and logging
   - Use `TestDataBuilder` for fluent test data creation
   - Use `TestDataFactory` for common test scenarios
   - Use custom assertions from `AssertionExtensions`
   - Use collection definitions for test organization

### 4. **Apply to Your Tests**
   - Refactor existing tests to follow patterns
   - Add `[Trait]` attributes for test categorization
   - Use builders and factories for test data
   - Use custom assertions for cleaner code

---

## 🎯 Key Takeaways

### Test Organization
```
DeepResearchAgent.Tests/
├── Unit/                  # Fast, isolated unit tests
├── Integration/           # Tests with external dependencies
├── Performance/           # Benchmarks and performance tests
├── Error/                 # Error scenarios and edge cases
├── Fixtures/              # Reusable test setup/teardown
├── Mocks/                 # Mock implementations
├── Helpers/               # Test utilities and builders
├── Collections/           # XUnit collection definitions
└── Base/                  # Base test classes
```

### Naming Conventions
```
Tests:    MethodUnderTest_Scenario_Expected
Classes:  {ComponentName}Tests or {Feature}IntegrationTests
Fixtures: {ComponentName}Fixture or {Feature}TestFixture
Builders: TestDataBuilder or {Entity}Builder
```

### Test Traits for Organization
```csharp
[Trait("Category", "Unit")]        // Unit/Integration/Performance
[Trait("Component", "Lightning")]  // Which component
[Trait("Feature", "HealthCheck")]  // What feature
[Trait("RequiresDocker", "true")]  // External dependencies
```

### Test Data Patterns
```csharp
// Use builder for readable test data
var agent = new TestDataBuilder()
    .WithAgentId("test-agent")
    .WithCapability("research", true)
    .BuildAgentRegistration();

// Use factory for common scenarios
var agent = TestDataFactory.CreateValidResearchAgent();
```

### Custom Assertions
```csharp
// Domain-specific, readable assertions
result.ShouldBeValidAgentRegistration();
result.ShouldHaveHighConfidence(threshold: 0.90);
```

---

## 📋 Implementation Checklist

- [ ] Create folder structure (Unit/, Integration/, Performance/, etc.)
- [ ] Copy foundation files to your project
- [ ] Add `[Trait]` attributes to existing tests
- [ ] Refactor test methods to `Method_Scenario_Expected` naming
- [ ] Add `using` statements to test files
- [ ] Update existing tests to use builders and factories
- [ ] Update existing tests to use custom assertions
- [ ] Create test collection definitions
- [ ] Configure `xunit.runner.json` for test execution
- [ ] Add GitHub Actions workflow for CI/CD
- [ ] Document team testing guidelines

---

## 🚀 Next Steps

1. **Immediate** (This Sprint):
   - Organize tests into appropriate folders
   - Add traits to categorize tests
   - Update naming conventions

2. **Short Term** (Next Sprint):
   - Refactor to use builders and factories
   - Add custom assertions
   - Create test fixtures

3. **Medium Term** (2-3 Sprints):
   - Achieve 80%+ code coverage
   - Setup CI/CD pipeline
   - Document team standards

4. **Long Term** (Ongoing):
   - Maintain test quality and coverage
   - Review and update patterns
   - Share knowledge with team

---

## 📚 Resources

- [XUnit Official Docs](https://xunit.net/)
- [MSTest Best Practices](https://docs.microsoft.com/en-us/dotnet/core/testing/)
- [Test Data Builders](https://www.jamesshore.com/v2/projects/lets-play-tdd/test-data-builder)
- [Custom Assertions](https://fluentassertions.com/)

---

## ✅ Summary

You now have:
- ✅ Comprehensive best practices documentation
- ✅ Quick start implementation guide
- ✅ Foundation helper classes ready to use
- ✅ Naming conventions and patterns defined
- ✅ Test organization structure
- ✅ CI/CD integration examples
- ✅ Clear path forward for improving test quality

**Status:** Ready for implementation! 🎯

---

**Document Version:** 2.0  
**Last Updated:** 2024  
**Status:** Production Ready
