using System.Text.Json;
using UniversalToolchain.ModuleContracts;

namespace UniversalToolchain.ContractExperiments;

internal static partial class Cgo27Program
{
    private sealed record DemandBaselineSummary(
        string Status,
        int InvalidatedFacts,
        int NoDemandVerifierInvocations,
        int ExplicitDemandVerifierInvocations,
        int SelectiveVerifierInvocations,
        bool UndemandedInvalidArtifactEscaped,
        bool DemandedInvalidArtifactDetected,
        bool SelectiveInvalidArtifactDetected,
        bool DemandedMatchedControlAccepted);

    private static DemandBaselineSummary RunDemandDrivenBaseline()
    {
        var requests = new[]
        {
            new ReverificationRequest(
                KnownCoreVerifierRules.AirContract,
                [KnownCoreCompilerFacts.AirVerified])
        };
        var routes = AvailableRoutes(CompilerPipelineStage.Air);

        var noDemand = VerificationPolicyScheduler.ScheduleDemandDriven(routes, requests, []);
        var demanded = VerificationPolicyScheduler.ScheduleDemandDriven(
            routes,
            requests,
            [KnownCoreVerifierRules.AirContract]);
        var selective = VerificationPolicyScheduler.Schedule(
            ExperimentPolicy.P2_SELECTIVE,
            routes,
            requests);

        var demandedFaultDetected = demanded.Any(invocation => !ExecuteSemanticReverification(
            invocation.RuleId,
            "demand-baseline-fault",
            PipelineMutation.InvalidateAirVerified,
            invalidArtifact: true));
        var selectiveFaultDetected = selective.Any(invocation => !ExecuteSemanticReverification(
            invocation.RuleId,
            "selective-baseline-fault",
            PipelineMutation.InvalidateAirVerified,
            invalidArtifact: true));
        var demandedControlAccepted = demanded.All(invocation => ExecuteSemanticReverification(
            invocation.RuleId,
            "demand-baseline-control",
            PipelineMutation.InvalidateAirVerified,
            invalidArtifact: false));

        var result = new DemandBaselineSummary(
            Status: "VALIDATED",
            InvalidatedFacts: 1,
            NoDemandVerifierInvocations: noDemand.Count,
            ExplicitDemandVerifierInvocations: demanded.Count,
            SelectiveVerifierInvocations: selective.Count,
            UndemandedInvalidArtifactEscaped: noDemand.Count == 0,
            DemandedInvalidArtifactDetected: demandedFaultDetected,
            SelectiveInvalidArtifactDetected: selectiveFaultDetected,
            DemandedMatchedControlAccepted: demandedControlAccepted);

        if (result.NoDemandVerifierInvocations != 0 ||
            result.ExplicitDemandVerifierInvocations != 1 ||
            result.SelectiveVerifierInvocations != 1 ||
            !result.UndemandedInvalidArtifactEscaped ||
            !result.DemandedInvalidArtifactDetected ||
            !result.SelectiveInvalidArtifactDetected ||
            !result.DemandedMatchedControlAccepted)
        {
            throw new InvalidOperationException(
                "Demand-driven baseline witness no longer distinguishes consumer demand from boundary obligation discharge: " +
                JsonSerializer.Serialize(result));
        }

        return result;
    }
}
