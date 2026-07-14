using UniversalToolchain.Ir.Abstractions;
using UniversalToolchain.Semantics.Abstractions;
using UniversalToolchain.Ssa.Abstractions;
using UniversalToolchain.Ssa.Core;

namespace UniversalToolchain.Ssa.Optimization;

public sealed class SsaOptimizerPipeline
{
    private readonly IReadOnlyList<IIrOptimizationPass> _passes;
    private readonly SsaDescriptorSet _descriptors;
    private readonly SemanticDescriptorSet _semanticDescriptors;

    public SsaOptimizerPipeline(
        IEnumerable<IIrOptimizationPass> passes,
        SsaDescriptorSet descriptors,
        SemanticDescriptorSet? semanticDescriptors = null)
    {
        _passes = passes?.ToArray() ?? throw new ArgumentNullException(nameof(passes));
        _descriptors = descriptors ?? throw new ArgumentNullException(nameof(descriptors));
        _semanticDescriptors = semanticDescriptors ?? SemanticDescriptorSet.Empty;

        var duplicatePass = _passes
            .GroupBy(static pass => pass.Id)
            .FirstOrDefault(static group => group.Count() > 1);
        if (duplicatePass is not null)
            throw new ArgumentException($"Duplicate SSA optimizer pass id '{duplicatePass.Key}'.", nameof(passes));

        foreach (var pass in _passes)
        {
            if (pass.InputKind != SsaIrKinds.Ssa || pass.OutputKind != SsaIrKinds.Ssa)
                throw new ArgumentException($"SSA optimizer pass '{pass.Id}' must preserve SSA IR kind.", nameof(passes));
        }
    }

    public IReadOnlyList<IrStageId> PassIds => _passes.Select(static pass => pass.Id).ToArray();

    public IrStageResult Run(SsaArtifact artifact, IrPipelineContext context) =>
        Run(artifact, context, passCompleted: null);

    internal IrStageResult Run(
        SsaArtifact artifact,
        IrPipelineContext context,
        Action<IrStageId>? passCompleted)
    {
        ArgumentNullException.ThrowIfNull(artifact);
        ArgumentNullException.ThrowIfNull(context);

        VerifyOrThrow(artifact, context, "ssa.optimization.input.invalid", "Input SSA artifact is invalid before optimization");

        IIrArtifact current = artifact;
        var facts = AddFact(context.Facts, SsaFacts.StructuralVerification);
        foreach (var pass in _passes)
        {
            ValidateContract(pass, context.Capabilities, facts);

            var result = pass.Run(current, new IrPipelineContext(context.Capabilities, facts));
            current = result.Artifact.As<SsaArtifact>();
            facts = ApplyFactEffects(pass.Contract, facts);
            VerifyOrThrow(current.As<SsaArtifact>(), context, "ssa.optimization.output.invalid", $"SSA artifact is invalid after optimization pass '{pass.Id}'");
            facts = AddFact(facts, SsaFacts.StructuralVerification);
            passCompleted?.Invoke(pass.Id);
        }

        return new IrStageResult(current, facts);
    }

    private static void ValidateContract(IIrOptimizationPass pass, CapabilitySet capabilities, IrFactSet facts)
    {
        var diagnostics = new List<IrDiagnostic>();
        diagnostics.AddRange(pass.Contract.RequiresFacts.Values
            .Where(required => !facts.Contains(required))
            .Select(required => new IrDiagnostic(
                IrDiagnosticSeverity.Error,
                "ssa.optimization.fact.missing",
                $"Optimization pass '{pass.Id}' requires missing fact '{required}'.")));

        diagnostics.AddRange(pass.Contract.RequiresCapabilities.Values
            .Where(required => !capabilities.Supports(required))
            .Select(required => new IrDiagnostic(
                IrDiagnosticSeverity.Error,
                "ssa.optimization.capability.missing",
                $"Optimization pass '{pass.Id}' requires missing capability '{required}'.")));

        if (diagnostics.Count != 0)
            throw new SsaOptimizationException($"SSA optimization pass '{pass.Id}' cannot run", diagnostics);
    }

    private static IrFactSet AddFact(IrFactSet facts, FactId fact) =>
        facts.Contains(fact) ? facts : new IrFactSet(facts.Values.Concat([fact]));

    private static IrFactSet ApplyFactEffects(IrStageContract contract, IrFactSet inputFacts)
    {
        var facts = new HashSet<FactId>();

        foreach (var preserved in contract.PreservesFacts.Values)
        {
            if (inputFacts.Contains(preserved))
                facts.Add(preserved);
        }

        foreach (var produced in contract.ProducesFacts.Values)
            facts.Add(produced);

        foreach (var invalidated in contract.InvalidatesFacts.Values)
            facts.Remove(invalidated);

        return new IrFactSet(facts);
    }

    private void VerifyOrThrow(SsaArtifact artifact, IrPipelineContext context, string code, string message)
    {
        var managedDescriptorDiagnostics = new List<IrDiagnostic>();
        var verifier = CreateVerifier(artifact, managedDescriptorDiagnostics);
        if (managedDescriptorDiagnostics.Count != 0)
        {
            throw new SsaOptimizationException(
                message,
                new[] { new IrDiagnostic(IrDiagnosticSeverity.Error, code, message) }.Concat(managedDescriptorDiagnostics));
        }

        var result = verifier.Verify(artifact, context);
        if (result.IsSuccess)
            return;

        var diagnostics = new[]
        {
            new IrDiagnostic(IrDiagnosticSeverity.Error, code, message)
        }.Concat(result.Diagnostics);

        throw new SsaOptimizationException(message, diagnostics);
    }

    private StructuralSsaVerifier CreateVerifier(SsaArtifact artifact, List<IrDiagnostic> diagnostics)
    {
        var managedDescriptors = CollectManagedCallableDescriptors(artifact, diagnostics);
        if (managedDescriptors.Count == 0)
            return new StructuralSsaVerifier(_descriptors, _semanticDescriptors);

        return new StructuralSsaVerifier(
            _descriptors,
            MergeSemanticDescriptors(_semanticDescriptors, managedDescriptors));
    }

    private static IReadOnlyList<CallableDescriptor> CollectManagedCallableDescriptors(
        SsaArtifact artifact,
        List<IrDiagnostic> diagnostics)
    {
        var descriptors = artifact.ManagedCallableBindings.Values.ToDictionary(
            static binding => binding.Callable,
            static binding => binding.Descriptor);

        foreach (var call in artifact.Module.Functions.SelectMany(static function => function.Blocks).SelectMany(static block => block.Calls))
        {
            if (!SsaManagedCallables.IsManagedCallable(call.Callee) || descriptors.ContainsKey(call.Callee))
                continue;

            diagnostics.Add(new IrDiagnostic(
                IrDiagnosticSeverity.Error,
                "ssa.optimization.managed-call.binding.missing",
                $"SSA managed call '{call.Id}' to '{call.Callee}' has no execution-scoped managed member binding."));
        }

        return descriptors.Values.ToArray();
    }

    private static SemanticDescriptorSet MergeSemanticDescriptors(
        SemanticDescriptorSet baseDescriptors,
        IReadOnlyList<CallableDescriptor> additionalCallables)
    {
        var types = baseDescriptors.Types
            .GroupBy(static x => x.Id)
            .Select(static x => x.First())
            .ToArray();
        var callables = baseDescriptors.Callables
            .Concat(additionalCallables)
            .GroupBy(static x => x.Id)
            .Select(static x => x.First())
            .ToArray();

        return new SemanticDescriptorSet(types, callables);
    }
}
