namespace BasicCore.Core;

public sealed class IntrinsicTypeStackProcessor : IIntrinsicTypeStackProcessor
{
    private readonly IIntrinsicCatalog _catalog;
    private readonly IIntrinsicTypeResolutionContext _context;

    public IntrinsicTypeStackProcessor(
        IIntrinsicCatalog catalog,
        IIntrinsicTypeResolutionContext context)
    {
        catalog = catalog.ArgNotNull();

        context = context.ArgNotNull();

        _catalog = catalog;
        _context = context;
    }

    public void Process(IntrinsicInvocation invocation, List<Type> stack)
    {
        invocation = invocation.ArgNotNull();

        stack = stack.ArgNotNull();

        var descriptor = _catalog.Resolve(invocation.Symbol);
        descriptor.ValidationRule.Validate(invocation, _context);
        descriptor.StackRule.Apply(invocation, stack, _context);
    }
}