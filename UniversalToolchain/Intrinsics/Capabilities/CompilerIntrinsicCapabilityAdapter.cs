using BasicCore.Contracts;
using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Capabilities;

public sealed class CompilerIntrinsicCapabilityAdapter<TCompilationOutput> : IIntrinsicCapabilitySet
{
    private readonly IAbstractIrCompiler<TCompilationOutput> _compiler;

    public CompilerIntrinsicCapabilityAdapter(IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        _compiler = compiler ?? throw new ArgumentNullException(nameof(compiler));
    }

    public bool Supports(IntrinsicSymbol symbol, IReadOnlyList<IntrinsicTypeArgument> typeArguments)
    {
        return LegacyCapabilityNameEncoder.TryEncode(symbol, typeArguments, out var capabilityName)
               && _compiler.SupportedIntrinsics.Contains(capabilityName);
    }
}
