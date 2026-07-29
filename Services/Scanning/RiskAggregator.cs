using OpenIPRadar.Core.Abstractions;
using OpenIPRadar.Core.Enums;
using OpenIPRadar.Core.Models;

namespace OpenIPRadar.Services.Scanning;

/// <summary>
/// Computes the normalized 0–100 threat score and <see cref="RiskLevel"/> for an address by
/// combining the strongest signals across providers: AbuseIPDB abuse confidence and VirusTotal
/// malicious/suspicious detections. The score is the maximum of the individual provider signals
/// so a single high-confidence source is not diluted by quieter ones.
/// </summary>
public sealed class RiskAggregator : IRiskAggregator
{
    private const int MaliciousThreshold = 70;
    private const int SuspiciousThreshold = 30;

    /// <inheritdoc />
    public (int Score, RiskLevel Level) Evaluate(IReadOnlyList<ProviderResult> results)
    {
        var hasData = results.Any(r => r.Status == ProviderResultStatus.Success);
        if (!hasData)
        {
            return (0, RiskLevel.Unknown);
        }

        var score = 0;

        foreach (var result in results)
        {
            if (result.AbuseConfidenceScore is { } abuse)
            {
                score = Math.Max(score, Math.Clamp(abuse, 0, 100));
            }

            if (result.VtMalicious is { } malicious)
            {
                var suspicious = result.VtSuspicious ?? 0;
                var vtSignal = Math.Clamp(malicious * 12 + suspicious * 4, 0, 100);
                score = Math.Max(score, vtSignal);
            }
        }

        var level = score >= MaliciousThreshold ? RiskLevel.Malicious
            : score >= SuspiciousThreshold ? RiskLevel.Suspicious
            : RiskLevel.Clean;

        return (score, level);
    }
}
