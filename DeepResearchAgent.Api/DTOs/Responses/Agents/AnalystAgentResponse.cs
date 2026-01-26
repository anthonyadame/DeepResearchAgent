namespace DeepResearchAgent.Api.DTOs.Responses.Agents;

using DeepResearchAgent.Api.DTOs.Common;

/// <summary>
/// Response from AnalystAgent - Analysis of research findings.
/// </summary>
public class AnalystAgentResponse
{
    /// <summary>
    /// Unique analysis ID.
    /// </summary>
    public string AnalysisId { get; set; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Themes identified in findings.
    /// </summary>
    public List<ThemeDto> ThemesIdentified { get; set; } = new();

    /// <summary>
    /// Contradictions detected in findings.
    /// </summary>
    public List<ContradictionDto> ContradictionsDetected { get; set; } = new();

    /// <summary>
    /// Importance scores for each finding.
    /// </summary>
    public Dictionary<string, double> FactImportanceScores { get; set; } = new();

    /// <summary>
    /// Synthesized key insights.
    /// </summary>
    public List<InsightDto> KeyInsights { get; set; } = new();

    /// <summary>
    /// Pattern analysis results.
    /// </summary>
    public List<PatternDto> PatternsIdentified { get; set; } = new();

    /// <summary>
    /// Confidence metrics for analysis.
    /// </summary>
    public AnalysisConfidenceDto? ConfidenceMetrics { get; set; }

    /// <summary>
    /// Recommended research areas for expansion.
    /// </summary>
    public List<string> RecommendedExpansionAreas { get; set; } = new();

    /// <summary>
    /// Identified knowledge gaps.
    /// </summary>
    public List<string> KnowledgeGaps { get; set; } = new();

    /// <summary>
    /// Overall quality score (0.0-1.0).
    /// </summary>
    public double OverallQualityScore { get; set; }

    /// <summary>
    /// Status: Success, Error.
    /// </summary>
    public string Status { get; set; } = "Success";

    /// <summary>
    /// Duration in milliseconds.
    /// </summary>
    public long DurationMilliseconds { get; set; }

    /// <summary>
    /// Metadata about the operation.
    /// </summary>
    public ApiMetadata? Metadata { get; set; }
}

/// <summary>
/// Identified theme from analysis.
/// </summary>
public class ThemeDto
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public List<string> RelatedFacts { get; set; } = new();
    public double Relevance { get; set; }
}

/// <summary>
/// Detected contradiction in findings.
/// </summary>
public class ContradictionDto
{
    public string Statement1 { get; set; } = string.Empty;
    public string Statement2 { get; set; } = string.Empty;
    public string? Source1 { get; set; }
    public string? Source2 { get; set; }
    public string? Resolution { get; set; }
    public double Severity { get; set; }
}

/// <summary>
/// Synthesized insight from analysis.
/// </summary>
public class InsightDto
{
    public string InsightStatement { get; set; } = string.Empty;
    public List<string> SupportingFacts { get; set; } = new();
    public double ConfidenceScore { get; set; }
    public string? ImplicationsSummary { get; set; }
}

/// <summary>
/// Identified pattern in findings.
/// </summary>
public class PatternDto
{
    public string PatternDescription { get; set; } = string.Empty;
    public List<string> ExamplesInFindings { get; set; } = new();
    public double Consistency { get; set; }
    public string? SignificanceLevel { get; set; }
}

/// <summary>
/// Confidence metrics for analysis.
/// </summary>
public class AnalysisConfidenceDto
{
    public double ThemeIdentificationConfidence { get; set; }
    public double ContradictionDetectionConfidence { get; set; }
    public double InsightSynthesisConfidence { get; set; }
    public double OverallAnalysisConfidence { get; set; }
}
