# Deep Research Agent 🔬

A sophisticated multi-agent research system that conducts in-depth investigations on topics, refines findings through iterative feedback, and synthesizes comprehensive reports.

## 🎯 Tagline
### Better answers, through a better ask. 

## 🎯 Overview

The Deep Research Agent implements a **diffusion-based research refinement loop** that:

1. **Clarifies** user intent and research goals
2. **Researches** topics comprehensively using web search and scraping
3. **Evaluates** findings for quality and accuracy
4. **Refines** through adversarial critique (red team)
5. **Synthesizes** into professional research reports

## 🏗️ Architecture

### Core Workflows

- **MasterWorkflow** - Orchestrates the entire 5-step research pipeline
- **SupervisorWorkflow** - Manages iterative refinement through diffusion loop
- **ResearcherWorkflow** - Conducts focused research on specific topics
- **SearCrawl4AIService** - Handles web search and content scraping

### State Management

- **LightningStateService** - High-performance state caching and persistence
- **StateFactory** - Creates properly initialized state objects
- **SupervisorState** - Tracks research progress and findings

### LLM Integration

- **OllamaService** - Unified LLM interface with model support
- **WorkflowModelConfiguration** - Model selection for each workflow function

## 🚀 Quick Start

### Installation

1. **Clone the repository:**
```bash
git clone https://github.com/anthonyadame/DeepResearchAgent.git
cd DeepResearchAgent
```

2. **Build the solution:**
```bash
dotnet build
```

3. **Install Ollama** (for local LLM):
   - Download from https://ollama.ai
   - Pull a model: `ollama pull gpt-oss:20b`

4. **Set up monitoring (optional but recommended):**
   ```bash
   cd monitoring
   # Windows PowerShell
   .\setup.ps1
   
   # Linux/macOS
   chmod +x setup.sh
   ./setup.sh
   ```
   Access Grafana at http://localhost:3000 (admin/admin)

### Basic Usage

```csharp
// Setup services
var services = new ServiceCollection();
services.AddScoped<ILightningStateService, LightningStateService>();
services.AddScoped<OllamaService>();
services.AddScoped<ResearcherWorkflow>();
services.AddScoped<SupervisorWorkflow>();
services.AddScoped<MasterWorkflow>();

var provider = services.BuildServiceProvider();
var master = provider.GetRequiredService<MasterWorkflow>();

// Run research
string query = "Latest developments in quantum computing";
string result = await master.RunAsync(query);
Console.WriteLine(result);
```

## 📊 Key Features

### 1. Intelligent Research
- **Web Search & Scraping** - Real-time information gathering
- **ReAct Loop** - Reasoning and acting based on findings
- **Fact Extraction** - Automated knowledge base population

### 2. Quality Assurance
- **Red Team Critique** - Adversarial evaluation for weaknesses
- **Quality Scoring** - Multi-dimensional quality assessment
- **Convergence Detection** - Stops when quality threshold reached

### 3. Model Flexibility
- **Multi-Model Support** - Use different models for different tasks
- **Cost Optimization** - Fast/cheap models for coordination
- **Quality Optimization** - Powerful models for reasoning
- **Custom Profiles** - Create your own model configurations

### 4. Performance
- **LightningStore** - Fast in-memory caching
- **Parallel Execution** - Concurrent research tasks
- **State Persistence** - Resume from interruptions

### 5. Agent-Lightning APO (Automatic Performance Optimization) ⚡
- **Smart Resource Management** - Automatic concurrency control and backpressure
- **Adaptive Retry Policies** - Intelligent exponential backoff with jitter
- **Performance Strategies** - Choose from HighPerformance, Balanced, LowResource, or CostOptimized
- **VERL Integration** - Verification and Reasoning Layer for output quality
- **Auto-Scaling** - Dynamic instance scaling based on load thresholds
- **Telemetry** - Built-in metrics, tracing, and profiling support
- **Circuit Breaker** - Automatic failure detection and graceful degradation when Lightning server unavailable

## 📈 Performance

### APO Benefits

Agent-Lightning APO provides significant performance improvements:

1. **Concurrency Control**
   - Prevents thread pool exhaustion with semaphore-based gating
   - Configurable concurrent task limits per strategy
   - Automatic backpressure when limits reached

2. **Resilient HTTP Communication**
   - Decorrelated jitter exponential backoff (prevents thundering herd)
   - Automatic retry on transient failures (429, 5xx errors)
   - Strategy-based retry counts (2-5 retries)

3. **Intelligent Verification**
   - Skip VERL for HighPerformance (2-3x throughput increase)
   - Async verification for Balanced/LowResource strategies
   - Configurable verification thresholds

4. **Resource Efficiency**
   - Reused HTTP connections via IHttpClientFactory
   - JSON serialization options reuse (~20% faster)
   - Configurable cache sizes and timeouts

5. **Observability**
   - OpenTelemetry metrics integration
   - Distributed tracing support
   - Performance profiling capabilities

### Optimization Tips

1. **Use Cost-Optimized Profile** for quick prototyping
2. **Use Quality-Optimized Profile** for critical research
3. **Adjust max iterations** based on time constraints
4. **Cache results** to avoid redundant searches
5. **Monitor state service metrics** for optimization

### Benchmarks

- Configuration test suite: < 2 seconds
- Single workflow execution: 30-120 seconds (depending on research depth)
- Typical report generation: 2-5 minutes

## 🧪 Testing

### Run All Tests
```bash
dotnet test
```

### Run Specific Tests
```bash
# Model configuration tests
dotnet test --filter "WorkflowModel"

# Workflow tests
dotnet test --filter "Workflow"

# With details
dotnet test --filter "WorkflowModel" --logger "console;verbosity=detailed"
```

### Test Structure
- **Unit Tests** - Individual component testing
- **Integration Tests** - Component interaction testing
- **Scenario Tests** - Real-world usage scenarios

## 🎨 Configuration

### APO (Automatic Performance Optimization) Configuration

Configure Agent-Lightning APO in `appsettings.apo.json`:

```json
{
  "Lightning": {
    "ServerUrl": "http://localhost:8090",
    "APO": {
      "Enabled": true,
      "OptimizationStrategy": "Balanced",
      "ResourceLimits": {
        "MaxCpuPercent": 80,
        "MaxMemoryMb": 2048,
        "MaxConcurrentTasks": 10,
        "TaskTimeoutSeconds": 300,
        "CacheSizeMb": 512
      },
      "PerformanceMetrics": {
        "TrackingEnabled": true,
        "MetricsIntervalSeconds": 10,
        "EnableTracing": true,
        "EnableProfiling": false
      },
      "AutoScaling": {
        "Enabled": false,
        "MinInstances": 1,
        "MaxInstances": 5,
        "ScaleUpThresholdPercent": 70,
        "ScaleDownThresholdPercent": 30
      },
      "CircuitBreaker": {
        "Enabled": true,
        "FailureThreshold": 5,
        "FailureRateThreshold": 50,
        "BreakDurationSeconds": 60,
        "EnableFallback": true
      }
    }
  }
}
```

#### Optimization Strategies

| Strategy | Use Case | Characteristics |
|----------|----------|-----------------|
| **HighPerformance** | Low-latency requirements | Max throughput, skip VERL verification, priority=10 |
| **Balanced** | Production workloads | Balance speed & quality, enable VERL, priority=5 |
| **LowResource** | Resource-constrained | Min CPU/memory, more retries, priority=3 |
| **CostOptimized** | Budget-conscious | Optimize cost/performance ratio, priority=4 |

#### APO Runtime Overrides

Override APO behavior at function call level:

```csharp
// Use HighPerformance strategy for this specific call
var apoOptions = new ApoExecutionOptions
{
    StrategyOverride = OptimizationStrategy.HighPerformance,
    Priority = 10,
    ForceVerification = false
};

var facts = await researcherWorkflow.ResearchAsync(
    "quantum computing",
    apoOptions: apoOptions);

// Disable APO for specific execution
var apoDisabled = new ApoExecutionOptions { DisableApo = true };
var result = await lightningService.SubmitTaskAsync(
    "agent-1", 
    task, 
    apoDisabled);
```

### Model Configuration

```csharp
// Default (optimized for balance)
var config = new WorkflowModelConfiguration();

// Cost-optimized (fast/cheap)
var config = new WorkflowModelConfiguration
{
    SupervisorBrainModel = "mistral:7b",
    SupervisorToolsModel = "mistral:latest",
    QualityEvaluatorModel = "mistral:7b",
    RedTeamModel = "mistral:7b",
    ContextPrunerModel = "orca-mini:latest"
};

// Quality-optimized (best results)
var config = new WorkflowModelConfiguration
{
    SupervisorBrainModel = "neural-chat:13b",
    SupervisorToolsModel = "neural-chat:7b",
    QualityEvaluatorModel = "neural-chat:13b",
    RedTeamModel = "neural-chat:13b",
    ContextPrunerModel = "neural-chat:7b"
};
```

### Available Models

| Function | Model | Purpose |
|----------|-------|---------|
| Supervisor Brain | `gpt-oss:20b` | Complex reasoning |
| Supervisor Tools | `mistral:latest` | Fast coordination |
| Quality Evaluator | `gpt-oss:20b` | Detailed analysis |
| Red Team | `gpt-oss:20b` | Critical thinking |
| Context Pruner | `mistral:latest` | Quick extraction |

## 🔧 Development

### Project Structure

```
DeepResearchAgent/
├── Models/                    # Data models
├── Services/                  # Core services
│   ├── OllamaService.cs      # LLM integration
│   ├── SearCrawl4AIService.cs # Web search
│   └── StateManagement/      # State services
├── Workflows/                 # Research workflows
│   ├── MasterWorkflow.cs      # Main orchestrator
│   ├── SupervisorWorkflow.cs  # Refinement loop
│   └── ResearcherWorkflow.cs  # Research tasks
├── Configuration/             # Configuration classes
└── Prompts/                   # LLM prompts

DeepResearchAgent.Tests/
├── Configuration/             # Configuration tests
├── Workflows/                 # Workflow tests
├── Integration/               # Integration tests
└── Examples/                  # Usage examples
```

### Building

```bash
# Debug build
dotnet build

# Release build
dotnet build --configuration Release

# Run specific project
dotnet run --project DeepResearchAgent

# Run tests
dotnet test

# Build documentation
# See BuildDoc/ folder for markdown documentation
```

## 📋 Requirements

- **.NET 8** or higher
- **Ollama** for local LLM (or API endpoint)
- **Internet connection** for web search functionality
- **Docker Desktop** (optional, for monitoring stack)

## 📊 Monitoring & Observability

### Grafana Dashboard Setup

The Deep Research Agent includes a complete monitoring stack with Grafana, Prometheus, and Alertmanager for APO performance metrics.

**Quick Start:**
```bash
cd monitoring
# Windows
.\setup.ps1

# Linux/macOS
./setup.sh
```

**Access:**
- Grafana: http://localhost:3000 (admin/admin)
- Prometheus: http://localhost:9091
- Alertmanager: http://localhost:9093

**Features:**
- 📈 Real-time APO performance dashboards
- ⚠️  16 pre-configured alerts for critical issues
- 📧 Email/Slack/Teams notifications
- 📊 30-day metric retention
- 🔍 Query builder for custom metrics

See `monitoring/README.md` for detailed documentation.

## 🔗 External Services

### Ollama
- **Purpose:** Local LLM inference
- **Models:** gpt-oss:20b, mistral:latest, neural-chat:13b, etc.
- **Setup:** Download from https://ollama.ai

### Crawl4AI
- **Purpose:** Web scraping and content extraction
- **Features:** JavaScript rendering, markdown conversion

## 📈 Performance

### Optimization Tips

1. **Use Cost-Optimized Profile** for quick prototyping
2. **Use Quality-Optimized Profile** for critical research
3. **Adjust max iterations** based on time constraints
4. **Cache results** to avoid redundant searches
5. **Monitor state service metrics** for optimization

### Benchmarks

- Configuration test suite: < 2 seconds
- Single workflow execution: 30-120 seconds (depending on research depth)
- Typical report generation: 2-5 minutes

## 🐛 Troubleshooting

### Common Issues

**"Cannot connect to Ollama"**
- Ensure Ollama is running: `ollama serve`
- Check endpoint: Default is `http://localhost:11434`

**"Model not found"**
- Pull the model: `ollama pull gpt-oss:20b`
- Verify model name in configuration

**"Tests failing"**
- Clean build: `dotent clean && dotnet build`
- Run single test: `dotnet test --filter "TestName"`

## 📝 License

See `LICENSE.txt` for license information.

## 🤝 Contributing

Contributions are welcome! Please:

1. Fork the repository
2. Create a feature branch
3. Make your changes
4. Run tests: `dotnet test`
5. Submit a pull request

## 📞 Support

For issues, questions, or suggestions:
- Open an issue on GitHub
- Check `BuildDoc/` folder for documentation
- Review test examples for usage patterns

## 📚 Learning Resources

### Quick Start Guides
- **BuildDoc/START_HERE.md** - Start here
- **BuildDoc/QUICK_REFERENCE.md** - Quick reference
- **BuildDoc/README_INDEX.md** - Documentation index

### Deep Dives
- **BuildDoc/IMPLEMENTATION_COMPLETE.md** - Full implementation
- **BuildDoc/WORKFLOW_INTEGRATION_COMPLETE.md** - Integration patterns
- **BuildDoc/TESTING_COMPLETE.md** - Testing strategies

### Examples
- Review test files in `DeepResearchAgent.Tests/`
- Check usage examples in `ModelConfigurationUsageExamples.cs`
- See configuration examples in `WorkflowModelConfigurationTests.cs`

## 📖 Citation

This project is inspired by and based on the research and concepts presented in:

**Khan, Fareed.** "Building a Human-Level Deep Research Agentic System using the Time Test Diffusion Algorithm: ReAct Loops, Denoising, Parallel Research and more." *Level Up Coding* (Medium), 2024.  
🔗 [Read the article](https://levelup.gitconnected.com/building-a-human-level-deep-research-agentic-system-using-the-time-test-diffusion-algorithm-587ed3c225a0)  
👤 [Author Profile](https://medium.com/@fareedkhandev)

## 📚 References

The implementation incorporates techniques and insights from the following publications:

### LLM Optimization & Reasoning

Note: It's always nice to see others are working on similar problems! Validates the work you have been doing.

**LinkedIn AI & HuggingFace.** "GPT-OSS: Open-Source Models for Agentic Reinforcement Learning."  
🔗 [Read the article](https://huggingface.co/blog/LinkedIn/gpt-oss-agentic-rl)  
*Presents GPT-OSS models optimized for agentic workflows and reinforcement learning applications*

**Microsoft Research.** "OptiMind: A Small Language Model with Optimization Expertise."  
🔗 [Read the article](https://www.microsoft.com/en-us/research/blog/optimind-a-small-language-model-with-optimization-expertise/)  
*Explores optimization techniques for efficient small language models*

**Microsoft Research.** "PromptWizard: The Future of Prompt Optimization Through Feedback-Driven Self-Evolving Prompts."  
🔗 [Read the article](https://www.microsoft.com/en-us/research/blog/promptwizard-the-future-of-prompt-optimization-through-feedback-driven-self-evolving-prompts/)  
*Demonstrates prompt optimization and self-improvement mechanisms*

**Microsoft Research.** "New Methods Boost Reasoning in Small and Large Language Models."  
🔗 [Read the article](https://www.microsoft.com/en-us/research/blog/new-methods-boost-reasoning-in-small-and-large-language-models/)  
*Presents techniques for enhancing reasoning capabilities across model sizes*

---

**Made with ❤️ for researchers and developers**

For complete documentation, see the `BuildDoc` folder.


Copyright (c) 2026 Anthony Adame

Statement of clarity: The software provided is covered under the MIT License. Everything in the folder "BuildDoc" is my journal, filled with trial, tribulations, missteps and misdirection's but most importantly it reflects the discovery process required, hence the copyright. 

Everything else in the repository is MIT License.
[![License: MIT](https://img.shields.io/badge/License-MIT-yellow.svg)](https://opensource.org/licenses/MIT) ![.NET](


