namespace UniversalToolchain.PlanFuzz.Cli;

internal sealed record PlanFuzzCampaignSummary(
    ulong CampaignSeed,
    int RequestedCases,
    int CompletedCases,
    int CleanCases,
    int ConfirmedFindings,
    int DistinctFindingClasses,
    int FlakyCases,
    int InconclusiveCases,
    int InfrastructureFailures,
    string AdapterId,
    string? SeededFaultId,
    bool IncludedRegressionCorpus);
