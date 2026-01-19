using Xunit;
using Moq;
using DeepResearchAgent.Services.VectorDatabase;
using Microsoft.Extensions.Logging;

namespace DeepResearchAgent.Tests.Services.VectorDatabase;

/// <summary>
/// Unit tests for VectorDatabaseFactory.
/// Tests factory registration, retrieval, and error handling.
/// </summary>
public class VectorDatabaseFactoryTests
{
    private readonly Mock<ILogger> _mockLogger;

    public VectorDatabaseFactoryTests()
    {
        _mockLogger = new Mock<ILogger>();
    }

    #region Registration Tests

    [Fact]
    public void RegisterVectorDatabase_WithValidService_Succeeds()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        var mockService = CreateMockVectorDatabaseService("test-db");

        // Act
        factory.RegisterVectorDatabase("test-db", mockService);

        // Assert
        var registered = factory.GetAvailableDatabases();
        Assert.Contains("test-db", registered);
    }

    [Fact]
    public void RegisterVectorDatabase_FirstRegistration_SetAsDefault()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        var mockService = CreateMockVectorDatabaseService("first-db");

        // Act
        factory.RegisterVectorDatabase("first-db", mockService);
        var defaultDb = factory.GetDefaultVectorDatabase();

        // Assert
        Assert.Equal(mockService, defaultDb);
    }

    [Fact]
    public void RegisterVectorDatabase_MultipleRegistrations_OnlyFirstIsDefault()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        var mockService1 = CreateMockVectorDatabaseService("first-db");
        var mockService2 = CreateMockVectorDatabaseService("second-db");

        // Act
        factory.RegisterVectorDatabase("first-db", mockService1);
        factory.RegisterVectorDatabase("second-db", mockService2);
        var defaultDb = factory.GetDefaultVectorDatabase();

        // Assert
        Assert.Equal(mockService1, defaultDb);
    }

    [Fact]
    public void RegisterVectorDatabase_WithNullService_ThrowsException()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => factory.RegisterVectorDatabase("test", null!));
    }

    [Fact]
    public void RegisterVectorDatabase_OverwriteExistingService()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        var mockService1 = CreateMockVectorDatabaseService("test-db");
        var mockService2 = CreateMockVectorDatabaseService("test-db");

        // Act
        factory.RegisterVectorDatabase("test-db", mockService1);
        factory.RegisterVectorDatabase("test-db", mockService2);
        var retrieved = factory.GetVectorDatabase("test-db");

        // Assert
        Assert.Equal(mockService2, retrieved);
    }

    #endregion

    #region Retrieval Tests

    [Fact]
    public void GetVectorDatabase_WithValidName_ReturnsService()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        var mockService = CreateMockVectorDatabaseService("test-db");
        factory.RegisterVectorDatabase("test-db", mockService);

        // Act
        var retrieved = factory.GetVectorDatabase("test-db");

        // Assert
        Assert.Equal(mockService, retrieved);
    }

    [Fact]
    public void GetVectorDatabase_WithInvalidName_ThrowsException()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(
            () => factory.GetVectorDatabase("nonexistent"));
    }

    [Fact]
    public void GetVectorDatabase_CaseSensitive()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        var mockService = CreateMockVectorDatabaseService("test-db");
        factory.RegisterVectorDatabase("test-db", mockService);

        // Act & Assert
        Assert.Throws<KeyNotFoundException>(
            () => factory.GetVectorDatabase("TEST-DB"));
    }

    [Fact]
    public void GetDefaultVectorDatabase_WithNoRegistration_ThrowsException()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);

        // Act & Assert
        Assert.Throws<InvalidOperationException>(
            () => factory.GetDefaultVectorDatabase());
    }

    [Fact]
    public void GetDefaultVectorDatabase_WithMultipleRegistrations_ReturnsFirst()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        var mockService1 = CreateMockVectorDatabaseService("qdrant");
        var mockService2 = CreateMockVectorDatabaseService("pinecone");

        factory.RegisterVectorDatabase("qdrant", mockService1);
        factory.RegisterVectorDatabase("pinecone", mockService2);

        // Act
        var defaultDb = factory.GetDefaultVectorDatabase();

        // Assert
        Assert.Equal(mockService1, defaultDb);
    }

    #endregion

    #region Available Databases Tests

    [Fact]
    public void GetAvailableDatabases_WithNoRegistrations_ReturnsEmpty()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);

        // Act
        var databases = factory.GetAvailableDatabases();

        // Assert
        Assert.NotNull(databases);
        Assert.Empty(databases);
    }

    [Fact]
    public void GetAvailableDatabases_WithRegistrations_ReturnsAllNames()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        factory.RegisterVectorDatabase("qdrant", CreateMockVectorDatabaseService("qdrant"));
        factory.RegisterVectorDatabase("pinecone", CreateMockVectorDatabaseService("pinecone"));
        factory.RegisterVectorDatabase("milvus", CreateMockVectorDatabaseService("milvus"));

        // Act
        var databases = factory.GetAvailableDatabases();

        // Assert
        Assert.NotNull(databases);
        Assert.Equal(3, databases.Count);
        Assert.Contains("qdrant", databases);
        Assert.Contains("pinecone", databases);
        Assert.Contains("milvus", databases);
    }

    [Fact]
    public void GetAvailableDatabases_ReturnsReadOnlyCollection()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        factory.RegisterVectorDatabase("qdrant", CreateMockVectorDatabaseService("qdrant"));

        // Act
        var databases = factory.GetAvailableDatabases();

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<string>>(databases);
    }

    #endregion

    #region Factory Lifecycle Tests

    [Fact]
    public void Factory_SupportsMultipleDatabaseTypes()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        
        // Act
        factory.RegisterVectorDatabase("qdrant", CreateMockVectorDatabaseService("qdrant"));
        factory.RegisterVectorDatabase("pinecone", CreateMockVectorDatabaseService("pinecone"));
        factory.RegisterVectorDatabase("milvus", CreateMockVectorDatabaseService("milvus"));

        // Assert
        Assert.Equal(3, factory.GetAvailableDatabases().Count);
        Assert.NotNull(factory.GetVectorDatabase("qdrant"));
        Assert.NotNull(factory.GetVectorDatabase("pinecone"));
        Assert.NotNull(factory.GetVectorDatabase("milvus"));
    }

    [Fact]
    public void Factory_WithoutLogger_Initializes()
    {
        // Arrange & Act
        var factory = new VectorDatabaseFactory(null);

        // Assert
        Assert.NotNull(factory);
        factory.RegisterVectorDatabase("test", CreateMockVectorDatabaseService("test"));
        Assert.NotNull(factory.GetDefaultVectorDatabase());
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void Factory_PluggableArchitecture_AllowsCustomImplementations()
    {
        // Arrange
        var factory = new VectorDatabaseFactory(_mockLogger.Object);
        var customService = new CustomTestVectorDatabase();

        // Act
        factory.RegisterVectorDatabase("custom", customService);
        var retrieved = factory.GetVectorDatabase("custom");

        // Assert
        Assert.Equal(customService, retrieved);
        Assert.IsType<CustomTestVectorDatabase>(retrieved);
    }

    #endregion

    #region Helper Methods

    private static IVectorDatabaseService CreateMockVectorDatabaseService(string name)
    {
        var mockService = new Mock<IVectorDatabaseService>();
        mockService.Setup(s => s.Name).Returns(name);
        return mockService.Object;
    }

    /// <summary>
    /// Custom test implementation for pluggable architecture testing.
    /// </summary>
    private class CustomTestVectorDatabase : IVectorDatabaseService
    {
        public string Name => "CustomTest";

        public Task<string> UpsertAsync(string id, string content, float[] embedding, Dictionary<string, object>? metadata = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(id);
        }

        public Task<IReadOnlyList<VectorSearchResult>> SearchAsync(float[] embedding, int topK = 5, double? scoreThreshold = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>(new List<VectorSearchResult>());
        }

        public Task<IReadOnlyList<VectorSearchResult>> SearchByContentAsync(string content, int topK = 5, double? scoreThreshold = null, CancellationToken cancellationToken = default)
        {
            return Task.FromResult<IReadOnlyList<VectorSearchResult>>(new List<VectorSearchResult>());
        }

        public Task DeleteAsync(string id, CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task ClearAsync(CancellationToken cancellationToken = default)
        {
            return Task.CompletedTask;
        }

        public Task<VectorDatabaseStats> GetStatsAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(new VectorDatabaseStats { IsHealthy = true });
        }

        public Task<bool> HealthCheckAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }
    }

    #endregion
}
