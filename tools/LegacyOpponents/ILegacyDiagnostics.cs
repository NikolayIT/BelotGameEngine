namespace BelotLegacy
{
    /// <summary>Counts compatibility interventions instead of silently hiding them.</summary>
    public interface ILegacyDiagnostics
    {
        long BidDecisions { get; }

        long CardDecisions { get; }

        long RejectedBids { get; }

        long CardFallbacks { get; }

        long LegalSetDifferences { get; }
    }
}
