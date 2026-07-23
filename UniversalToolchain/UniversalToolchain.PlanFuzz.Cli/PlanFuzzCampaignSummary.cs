namespace UniversalToolchain.PlanFuzz.Cli;

internal sealed record PlanFuzzCampaignSummary(
    ulong CampaignSeed,
    int RequestedCases,
    int CompletedCases,
    int CleanCases,
    int ConfirmedFindings,
    int FlakyCases,
    int InfrastructureFailures,
    string AdapterId,
    string? SeededFaultId);
