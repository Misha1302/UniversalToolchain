namespace UniversalToolchain.ModuleContracts;

public sealed class BytecodeDriftReporter
{
    public BytecodeDriftReport CreateReport(
        SelectedModuleContractTable contractTable,
        IReadOnlyList<ObservedBytecodeEmission> observedEmissions)
    {
        contractTable = contractTable.ArgNotNull();
        observedEmissions = observedEmissions.ArgNotNull();

        var declaredByModule = contractTable.BytecodeFacets
            .GroupBy(static x => x.ModuleId)
            .ToDictionary(
                static x => x.Key,
                static x => FlattenDeclared(x.SelectMany(static facet => facet.BytecodeEmissions)));
        var observedByModule = observedEmissions
            .GroupBy(static x => x.ProducerModule)
            .ToDictionary(
                static x => x.Key,
                static x => FlattenObserved(x));

        var moduleIds = declaredByModule.Keys
            .Concat(observedByModule.Keys)
            .Distinct()
            .OrderBy(static x => x.Value, StringComparer.Ordinal)
            .ToArray();

        var modules = moduleIds
            .Select(moduleId =>
            {
                declaredByModule.TryGetValue(moduleId, out var declared);
                observedByModule.TryGetValue(moduleId, out var observed);
                declared ??= FlattenDeclared([]);
                observed ??= FlattenObserved([]);

                return new ModuleBytecodeDrift(
                    moduleId,
                    Difference(observed.Tags, declared.Tags),
                    Difference(observed.Patterns, declared.Patterns),
                    Difference(declared.Tags, observed.Tags),
                    Difference(declared.Patterns, observed.Patterns));
            })
            .ToArray();

        return new BytecodeDriftReport(modules);
    }

    private static BytecodeIdSet FlattenDeclared(IEnumerable<BytecodeEmissionContract> declarations) =>
        new(
            declarations
                .SelectMany(static x => x.MayEmitTags)
                .OrderBy(static x => x.Value, StringComparer.Ordinal)
                .Distinct()
                .ToArray(),
            declarations
                .SelectMany(static x => x.MayEmitPatterns)
                .OrderBy(static x => x.Value, StringComparer.Ordinal)
                .Distinct()
                .ToArray());

    private static BytecodeIdSet FlattenObserved(IEnumerable<ObservedBytecodeEmission> emissions) =>
        new(
            emissions
                .SelectMany(static x => x.Tags)
                .OrderBy(static x => x.Value, StringComparer.Ordinal)
                .Distinct()
                .ToArray(),
            emissions
                .SelectMany(static x => x.Patterns)
                .OrderBy(static x => x.Value, StringComparer.Ordinal)
                .Distinct()
                .ToArray());

    private static IReadOnlyList<TId> Difference<TId>(
        IReadOnlyList<TId> left,
        IReadOnlyList<TId> right)
        where TId : notnull =>
        left.Where(item => !right.Contains(item)).ToArray();

    private sealed record BytecodeIdSet(
        IReadOnlyList<BytecodeTagId> Tags,
        IReadOnlyList<BytecodePatternId> Patterns);
}
