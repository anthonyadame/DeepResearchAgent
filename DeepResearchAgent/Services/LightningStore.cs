using System.Text.Json;
using DeepResearchAgent.Models;

namespace DeepResearchAgent.Services;

/// <summary>
/// Simple file-backed knowledge store for persisting facts.
/// Stores data as JSON on disk for easy inspection and portability.
/// </summary>
public interface ILightningStore
{
    Task SaveFactAsync(FactState fact, CancellationToken cancellationToken = default);
    Task SaveFactsAsync(IEnumerable<FactState> facts, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FactState>> GetAllFactsAsync(CancellationToken cancellationToken = default);
    Task<IReadOnlyList<FactState>> SearchAsync(string query, CancellationToken cancellationToken = default);
    Task ClearAsync(CancellationToken cancellationToken = default);
}

/// <summary>
/// Configuration options for LightningStore.
/// </summary>
public class LightningStoreOptions
{
    public string DataDirectory { get; set; } = "data";
    public string FileName { get; set; } = "lightningstore.json";

    public string FilePath => Path.Combine(DataDirectory, FileName);
}

/// <summary>
/// File-based implementation of ILightningStore.
/// </summary>
public class LightningStore : ILightningStore
{
    private readonly LightningStoreOptions _options;
    private readonly SemaphoreSlim _mutex = new(1, 1);
    private readonly JsonSerializerOptions _serializerOptions = new()
    {
        WriteIndented = true
    };

    public LightningStore(LightningStoreOptions? options = null)
    {
        _options = options ?? new LightningStoreOptions();
        Directory.CreateDirectory(_options.DataDirectory);
    }

    public async Task SaveFactAsync(FactState fact, CancellationToken cancellationToken = default)
    {
        await SaveFactsAsync(new[] { fact }, cancellationToken);
    }

    public async Task SaveFactsAsync(IEnumerable<FactState> facts, CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var allFacts = await LoadFactsLockedAsync(cancellationToken);
            allFacts.AddRange(facts);
            await PersistFactsLockedAsync(allFacts, cancellationToken);
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<FactState>> GetAllFactsAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            var facts = await LoadFactsLockedAsync(cancellationToken);
            return facts.ToList();
        }
        finally
        {
            _mutex.Release();
        }
    }

    public async Task<IReadOnlyList<FactState>> SearchAsync(string query, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return Array.Empty<FactState>();
        }

        var allFacts = await GetAllFactsAsync(cancellationToken);
        return allFacts
            .Where(f => f.Content.Contains(query, StringComparison.OrdinalIgnoreCase) ||
                        f.SourceUrl.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();
    }

    public async Task ClearAsync(CancellationToken cancellationToken = default)
    {
        await _mutex.WaitAsync(cancellationToken);
        try
        {
            if (File.Exists(_options.FilePath))
            {
                File.Delete(_options.FilePath);
            }
        }
        finally
        {
            _mutex.Release();
        }
    }

    private async Task<List<FactState>> LoadFactsLockedAsync(CancellationToken cancellationToken)
    {
        if (!File.Exists(_options.FilePath))
        {
            return new List<FactState>();
        }

        await using var stream = File.Open(_options.FilePath, FileMode.Open, FileAccess.Read, FileShare.Read);
        var facts = await JsonSerializer.DeserializeAsync<List<FactState>>(stream, cancellationToken: cancellationToken);
        return facts ?? new List<FactState>();
    }

    private async Task PersistFactsLockedAsync(List<FactState> facts, CancellationToken cancellationToken)
    {
        await using var stream = File.Open(_options.FilePath, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, facts, _serializerOptions, cancellationToken);
    }
}
