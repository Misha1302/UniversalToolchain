namespace UniversalToolchain.Dialects.Frontend;

public sealed class DialectDirectiveDescriptor
{
    public required string Id { get; init; }

    public required string Keyword { get; init; }

    public required DialectDirectiveParserOrder ParserOrder { get; init; }

    public bool IsSingleton { get; init; }

    public required string SingletonViolationMessage { get; init; }
}

public static class DialectDirectiveDescriptors
{
    public static IReadOnlyList<DialectDirectiveDescriptor> CreateOrdered(DialectDslRegistry registry)
    {
        if (registry == null)
            Thrower.ArgumentNull(nameof(registry));

        return registry.DirectiveFeatures
            .Select(x => new DialectDirectiveDescriptor
            {
                Id = x.Id,
                Keyword = x.Keyword,
                ParserOrder = x.ParserOrder,
                IsSingleton = x.IsSingleton,
                SingletonViolationMessage = x.SingletonViolationMessage
            })
            .ToList();
    }
}