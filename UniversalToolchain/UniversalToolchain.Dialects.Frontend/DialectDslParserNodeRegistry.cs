using BasicCore.Registration;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslParserNodeRegistry
{
    public static IReadOnlyList<NodeCreatorRegistration> Registrations { get; } =
    [
        new(0f, new DialectLineNodeCreator())
    ];
}
