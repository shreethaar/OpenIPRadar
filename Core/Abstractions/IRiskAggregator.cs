using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Core.Abstractions;

/// <summary>
/// Computes a normalized threat score (0–100) and <see cref="RiskLevel"/> from the merged
/// provider results for a single address.
/// </summary>
public interface IRiskAggregator
{
    /// <summary>Calculates the threat score and risk level for the given provider results.</summary>
    /// <param name="results">The per-provider results for a single address.</param>
    /// <returns>A tuple of the 0–100 score and the derived risk level.</returns>
    (int Score, RiskLevel Level) Evaluate(IReadOnlyList<ProviderResult> results);
}
