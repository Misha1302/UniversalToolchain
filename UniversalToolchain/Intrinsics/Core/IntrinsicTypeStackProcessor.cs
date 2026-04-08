using UniversalToolchain.Intrinsics.Contracts;

namespace UniversalToolchain.Intrinsics.Core;

public sealed class IntrinsicTypeStackProcessor : IIntrinsicTypeStackProcessor
{
    private readonly IIntrinsicCatalog _catalog;
    private readonly IIntrinsicTypeResolutionContext _context;

    public IntrinsicTypeStackProcessor(
        IIntrinsicCatalog catalog,
        IIntrinsicTypeResolutionContext context)
    {
        if (catalog == null)
            Thrower.ArgumentNull(nameof(catalog));

        if (context == null)
            Thrower.ArgumentNull(nameof(context));

        _catalog = catalog;
        _context = context;
    }

    public void Process(IntrinsicInvocation invocation, List<Type> stack)
    {
        if (invocation == null)
            Thrower.ArgumentNull(nameof(invocation));

        if (stack == null)
            Thrower.ArgumentNull(nameof(stack));

        var descriptor = _catalog.Resolve(invocation.Symbol);
        descriptor.ValidationRule.Validate(invocation, _context);
        descriptor.StackRule.Apply(invocation, stack, _context);
    }
}
