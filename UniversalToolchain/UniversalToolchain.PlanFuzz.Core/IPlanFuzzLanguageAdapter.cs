namespace UniversalToolchain.PlanFuzz;

/// <summary>
/// Defines the language-owned generation and execution boundary consumed by the generic PlanFuzz coordinator.
/// </summary>
public interface IPlanFuzzLanguageAdapter
{
    PlanFuzzAdapterDescriptor Descriptor { get; }

    PlanFuzzTestCase GenerateCase(
        ulong campaignSeed,
        long caseIndex,
        PlanFuzzCaseGenerationOptions options);

    PlanFuzzObservation Execute(
        PlanFuzzTestCase testCase,
        PlanFuzzPlanVariant variant);
}
