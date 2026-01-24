# Vector Database Integration

This document describes the vector database support in Deep Research Agent.

## Overview

The Deep Research Agent now supports integration with vector databases for semantic search and similarity-based fact retrieval. This enables the system to:

1. **Store embeddings** of research findings for fast semantic search
2. **Find similar facts** without redundant research
3. **Improve research quality** by detecting related findings
4. **Support multiple vector database backends** through a pluggable architecture

## Supported Vector Databases

### Qdrant (Recommended)
- **Status**: Fully implemented
- **Description**: Open-source, production-grade vector similarity search engine
- **Website**: https://qdrant.tech/
- **Deployment**: Docker, Kubernetes, Cloud-hosted
- **Default Config**: Localhost on port 6333

### Future Implementations
The architecture supports adding more vector databases:
- Pinecone (cloud-native vector database)
- Milvus (open-source vector database)
- Weaviate (vector database platform)
- Redis with RedisSearch (in-memory vector store)
- Elasticsearch with vector search
- FAISS (Facebook AI Similarity Search)

## Architecture

### Key Components

#### `IVectorDatabaseService`
Main interface for vector database operations:
```csharp
public interface IVectorDatabaseService
{
    Task<string> UpsertAsync(string id, string content, float[] embedding, Dictionary<string, object>? metadata = null);
    Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] embedding, int topK = 5, double? scoreThreshold = null);
    Task<IReadOnlyList<VectorSearchResult>> SearchByContentAsync(string content, int topK = 5, double? scoreThreshold = null);
    Task DeleteAsync(string id);
    Task ClearAsync();
    Task<VectorDatabaseStats> GetStatsAsync();
    Task<bool> HealthCheckAsync();
}
```

#### `IEmbeddingService`
Generates vector embeddings from text:
```csharp
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text);
    Task<IReadOnlyList<float[]>> EmbedBatchAsync(IEnumerable<string> texts);
    int GetEmbeddingDimension();
}
```

#### `IVectorDatabaseFactory`
Factory pattern for managing multiple database implementations:
```csharp
public interface IVectorDatabaseFactory
{
    IVectorDatabaseService GetVectorDatabase(string name);
    IVectorDatabaseService GetDefaultVectorDatabase();
    void RegisterVectorDatabase(string name, IVectorDatabaseService service);
    IReadOnlyList<string> GetAvailableDatabases();
}
```

## Configuration

### Via appsettings.json

```json
{
  "VectorDatabase": {
    "Enabled": true,
    "DefaultDatabase": "qdrant",
    "EmbeddingModel": "nomic-embed-text",
    "EmbeddingApiUrl": "http://localhost:11434",
    "Qdrant": {
      "BaseUrl": "http://localhost:6333",
      "CollectionName": "research",
      "VectorDimension": 384,
      "ApiKey": null
    }
  }
}
```

### Via Environment Variables

```bash
# Enable/disable vector database support
export VectorDatabase__Enabled=true

# Qdrant configuration
export VectorDatabase__Qdrant__BaseUrl=http://localhost:6333
export VectorDatabase__Qdrant__CollectionName=research
export VectorDatabase__Qdrant__VectorDimension=384
export VectorDatabase__Qdrant__ApiKey=your-api-key

# Embedding service configuration
export VectorDatabase__EmbeddingModel=nomic-embed-text
export VectorDatabase__EmbeddingApiUrl=http://localhost:11434
```

## Usage in ResearcherWorkflow

### Automatic Indexing

Facts extracted from research are automatically indexed to the vector database:

```csharp
// In ResearcherWorkflow.ResearchAsync()
var facts = ExtractFactsFromFindings(compressedFindings);
if (facts.Count > 0)
{
    await _store.SaveFactsAsync(facts.AsEnumerable(), cancellationToken);
    
    // Automatically index to vector database
    if (_vectorDb != null && _embeddingService != null)
    {
        await IndexFactsToVectorDatabaseAsync(facts, cancellationToken);
    }
}
```

### Semantic Search

Search for similar facts using semantic similarity:

```csharp
var similarFacts = await researchWorkflow.SearchSimilarFactsAsync(
    query: "machine learning applications",
    topK: 5,
    cancellationToken: cancellationToken
);
```

## Quick Start with Docker

### 1. Start Qdrant

```bash
docker run -p 6333:6333 qdrant/qdrant:latest
```

### 2. Verify Qdrant is Running

```bash
curl http://localhost:6333/health
```

### 3. Configure in appsettings.json

```json
{
  "VectorDatabase": {
    "Enabled": true,
    "Qdrant": {
      "BaseUrl": "http://localhost:6333",
      "CollectionName": "research",
      "VectorDimension": 384
    }
  }
}
```

### 4. Run Deep Research Agent

The vector database will be automatically initialized and used.

## Adding a New Vector Database

### 1. Implement IVectorDatabaseService

```csharp
public class YourVectorDatabaseService : IVectorDatabaseService
{
    private readonly HttpClient _httpClient;
    private readonly YourDatabaseConfig _config;
    private readonly IEmbeddingService _embeddingService;
    private readonly ILogger? _logger;

    public string Name => "YourDatabase";

    // Implement all interface methods
    public async Task<string> UpsertAsync(...) { ... }
    public async Task<IReadOnlyList<VectorSearchResult>> SearchAsync(...) { ... }
    // ... etc
}
```

### 2. Register in Program.cs

```csharp
services.AddSingleton<IVectorDatabaseService>(sp => new YourVectorDatabaseService(
    sp.GetRequiredService<HttpClient>(),
    config,
    sp.GetRequiredService<IEmbeddingService>(),
    logger: sp.GetService<ILogger<YourVectorDatabaseService>>()
));

// Register with factory
services.AddSingleton<IVectorDatabaseFactory>(sp =>
{
    var factory = new VectorDatabaseFactory(sp.GetService<ILogger<VectorDatabaseFactory>>());
    factory.RegisterVectorDatabase("your-db", sp.GetRequiredService<IVectorDatabaseService>());
    return factory;
});
```

### 3. Update Configuration

Add configuration support for your database in appsettings.json.

## Performance Considerations

- **Embedding Generation**: Models like `nomic-embed-text` are fast (~50-100 ms per fact)
- **Batch Operations**: Use `EmbedBatchAsync` for multiple texts to leverage parallelism
- **Vector Dimension**: Larger dimensions (768, 1024) provide better quality but slower search. Default (384) balances quality/performance
- **Search Threshold**: Adjust `scoreThreshold` in search calls to filter low-confidence results

## Troubleshooting

### Qdrant Connection Error

```
Failed to search Qdrant: System.Net.Http.HttpRequestException: 'Connection refused'
```

**Solution**: Ensure Qdrant is running:
```bash
docker run -p 6333:6333 qdrant/qdrant:latest
```

### Collection Not Found

```
Failed to upsert document: 404 Not Found
```

**Solution**: Collection will be created automatically on first upsert. If using a different collection name, update `appsettings.json`.

### Embedding Dimension Mismatch

```
Embedding dimension mismatch. Expected 384, got 768
```

**Solution**: Ensure `EmbeddingModel` and `VectorDimension` match the embedding service output.

## Performance Metrics

Typical performance with Qdrant and Ollama embeddings:

- **Index Fact**: 50-100 ms (includes embedding generation)
- **Search (topK=5)**: 10-20 ms
- **Batch Index (10 facts)**: 600-800 ms
- **Health Check**: 5-10 ms

## Best Practices

1. **Enable Vector Database in Production**: Significantly improves research quality
2. **Use Semantic Search**: Check `SearchSimilarFactsAsync` before starting new research
3. **Monitor Vector DB Health**: Call `GetStatsAsync()` periodically
4. **Batch Operations**: Process multiple facts together for efficiency
5. **Metadata**: Include relevant metadata (source URL, confidence) for better filtering
6. **Dimension Matching**: Ensure embedding model output matches configured dimension

## Future Enhancements

- [ ] Hybrid search (BM25 + semantic)
- [ ] Temporal decay (older facts ranked lower)
- [ ] Metadata-based filtering in search
- [ ] Vector clustering for topic organization
- [ ] Automatic collection management
- [ ] Vector DB cluster support
