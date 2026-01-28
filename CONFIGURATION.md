# Configuration Guide

## API Key Management

All API keys and configuration have been consolidated into `config.yml` for easier management.

### Configuration Files

1. **config.yml** - Main configuration file with all settings
2. **appsettings.json** - Legacy JSON configuration (still loaded)
3. **.env.example** - Environment variable template
4. **config.local.yml.example** - Local override template

### API Keys

The following API keys are supported:

- **Tavily** (`ApiKeys:Tavily`) - Web search API
- **Qdrant** (`ApiKeys:Qdrant`) - Vector database authentication

### Configuration Priority

Configuration sources are loaded in this order (later sources override earlier ones):

1. appsettings.json
2. appsettings.websearch.json
3. appsettings.apo.json
4. **config.yml** ← New consolidated config
5. Environment variables ← Highest priority

### Setting API Keys

#### Option 1: Direct in config.yml (Development only)
```yaml
ApiKeys:
  Tavily: "tvly-xxxxxxxxxxxxxxxx"
  Qdrant: "your-qdrant-key-here"
```

#### Option 2: Environment Variables (Recommended for Production)
```bash
export APIKEYS__TAVILY="tvly-xxxxxxxxxxxxxxxx"
export APIKEYS__QDRANT="your-qdrant-key"
```

#### Option 3: .NET User Secrets (Development)
```bash
dotnet user-secrets set "ApiKeys:Tavily" "tvly-xxxxxxxxxxxxxxxx"
dotnet user-secrets set "ApiKeys:Qdrant" "your-qdrant-key"
```

### Qdrant Configuration

Qdrant vector database now supports API key authentication:

```yaml
VectorDatabase:
  Enabled: true
  Qdrant:
    BaseUrl: "http://localhost:6333"
    ApiKey: ""  # Leave empty for no auth, or set to your API key
    CollectionName: "research"
    VectorDimension: 384
    TimeoutMs: 30000
```

The API key can be:
- Set directly in `VectorDatabase:Qdrant:ApiKey`
- Referenced from `ApiKeys:Qdrant` (automatically falls back)
- Provided via environment: `VECTORDATABASE__QDRANT__APIKEY`

### Security Best Practices

1. **Never commit API keys** to source control
2. Add `config.local.yml` to `.gitignore`
3. Use environment variables in production
4. Use User Secrets for local development
5. Rotate keys regularly

### Example Usage

```bash
# Set environment variables
export APIKEYS__QDRANT="my-secret-key"
export VECTORDATABASE__ENABLED=true

# Run the application
dotnet run --project DeepResearchAgent.Api
```

### NuGet Package Required

The YAML configuration requires:
```xml
<PackageReference Include="NetEscapades.Configuration.Yaml" Version="3.1.0" />
```

This is already included in `DeepResearchAgent.Api.csproj`.
