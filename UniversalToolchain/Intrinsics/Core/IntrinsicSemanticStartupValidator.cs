using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public sealed class IntrinsicSemanticStartupValidator
{
    public void Validate(IEnumerable<IIntrinsicDescriptorProvider> providers)
    {
        if (providers == null)
            Thrower.ArgumentNull(nameof(providers));

        _ = new IntrinsicCatalogBuilder().Build(providers);
    }
}
