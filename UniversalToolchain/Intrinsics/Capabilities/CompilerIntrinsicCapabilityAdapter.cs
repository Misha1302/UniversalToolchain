namespace BasicCore.Capabilities;

public sealed class CompilerIntrinsicCapabilityAdapter<TCompilationOutput> : IIntrinsicCapabilitySet
{
    private readonly IAbstractIrCompiler<TCompilationOutput> _compiler;

    public CompilerIntrinsicCapabilityAdapter(IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        compiler = compiler.ArgNotNull();

        _compiler = compiler;
    }

    public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<IntrinsicTypeArgument> typeArguments) =>
        LegacyCapabilityNameEncoder.TryEncode(symbol, typeArguments, out var capabilityName)
        && _compiler.SupportedIntrinsics.Contains(capabilityName);
}