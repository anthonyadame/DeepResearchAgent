namespace DeepResearchAgent.Models;

/// <summary>
/// An atomic unit of knowledge, extracted from raw research notes and stored in our structured knowledge base.
/// </summary>
public record FactState
{
    /// <summary>
    /// The core factual statement itself, extracted from a source.
    /// </summary>
    public required string Content { get; init; }

    /// <summary>
    /// We must track the provenance of every fact for traceability and citations in the final report.
    /// </summary>
    public required string SourceUrl { get; init; }

    /// <summary>
    /// A confidence score allows the system to weigh more credible sources higher during synthesis.
    /// Range: 1-100 based on source credibility.
    /// </summary>
    public required int ConfidenceScore { get; init; }

    /// <summary>
    /// This flag allows our Red Team to mark facts that are contradicted by other evidence,
    /// a key part of our self-correction loop.
    /// </summary>
    public bool IsDisputed { get; init; } = false;
}
