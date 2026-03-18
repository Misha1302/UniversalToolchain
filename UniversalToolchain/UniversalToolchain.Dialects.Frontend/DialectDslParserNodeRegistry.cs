using BasicCore.Registration;

namespace UniversalToolchain.Dialects.Frontend;

public static class DialectDslParserNodeRegistry
{
    public static IReadOnlyList<NodeCreatorRegistration> Registrations { get; } =
    [
        new(0f, new DialectLineNodeCreator()),
        new(10f, new DialectDeclarationNodeCreator()),
        new(11f, new UseModulesDirectiveNodeCreator()),
        new(12f, new ExcludeModulesDirectiveNodeCreator()),
        new(13f, new RequiresModulesDirectiveNodeCreator()),
        new(14f, new BeforeModulesDirectiveNodeCreator()),
        new(15f, new AfterModulesDirectiveNodeCreator()),
        new(16f, new BackendDirectiveNodeCreator()),
        new(17f, new AllowIntrinsicDirectiveNodeCreator()),
        new(18f, new ForbidIntrinsicDirectiveNodeCreator()),
        new(19f, new EnableIntrinsicDirectiveNodeCreator()),
        new(20f, new DisableIntrinsicDirectiveNodeCreator()),
        new(21f, new SecurityDirectiveNodeCreator()),
        new(22f, new CapabilityDirectiveNodeCreator()),
        new(100f, new DialectDocumentNodeCreator())
    ];
}
