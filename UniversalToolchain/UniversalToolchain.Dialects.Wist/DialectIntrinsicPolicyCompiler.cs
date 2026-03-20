namespace UniversalToolchain.Dialects.Wist;

internal sealed class DialectIntrinsicPolicyCompiler<TCompilationOutput> : IAbstractIrCompiler<TCompilationOutput>
{
    private readonly HashSet<string> _allowedIntrinsics;
    private readonly HashSet<string> _forbiddenIntrinsics;
    private readonly bool _hasExplicitAllowList;
    private readonly IAbstractIrCompiler<TCompilationOutput> _inner;

    public DialectIntrinsicPolicyCompiler(
        IAbstractIrCompiler<TCompilationOutput> inner,
        IEnumerable<string> allowedIntrinsics,
        IEnumerable<string> forbiddenIntrinsics,
        bool hasExplicitAllowList = false)
    {
        if (inner == null)
            Thrower.ArgumentNull(nameof(inner));

        _inner = inner;
        _allowedIntrinsics = CreateSet(allowedIntrinsics, nameof(allowedIntrinsics));
        _forbiddenIntrinsics = CreateSet(forbiddenIntrinsics, nameof(forbiddenIntrinsics));
        _hasExplicitAllowList = hasExplicitAllowList;
        SupportedIntrinsics = _inner.SupportedIntrinsics
            .Where(x => !_forbiddenIntrinsics.Contains(x))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToList();
    }

    public IReadOnlyList<string> SupportedIntrinsics { get; }

    public TCompilationOutput Compile(IAbstractIR air, CompilationInput input)
    {
        if (air == null)
            Thrower.ArgumentNull(nameof(air));

        if (input == null)
            Thrower.ArgumentNull(nameof(input));

        ValidateIntrinsics(air);
        return _inner.Compile(air, input);
    }

    private void ValidateIntrinsics(IAbstractIR air)
    {
        foreach (var instruction in air.Instructions)
        {
            if (instruction.UOpCode != UOpCode.Intrinsic)
                continue;

            if (instruction.Operands.Count == 0 || instruction.Operands[0] is not string intrinsicName)
                continue;

            if (_forbiddenIntrinsics.Contains(intrinsicName))
                Thrower.InvalidOpEx($"Intrinsic '{intrinsicName}' is forbidden by the selected dialect.");

            if (_hasExplicitAllowList && !_allowedIntrinsics.Contains(intrinsicName))
                Thrower.InvalidOpEx($"Intrinsic '{intrinsicName}' is not allowed by the selected dialect.");
        }
    }

    private static HashSet<string> CreateSet(IEnumerable<string> values, string paramName)
    {
        if (values == null)
            Thrower.ArgumentNull(paramName);

        return values
            .Select(x => x.NotNull(paramName))
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .ToHashSet(StringComparer.Ordinal);
    }
}