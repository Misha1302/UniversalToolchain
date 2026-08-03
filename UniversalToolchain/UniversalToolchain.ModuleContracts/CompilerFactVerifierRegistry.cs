namespace UniversalToolchain.ModuleContracts;

public interface ICompilerFactVerifierRuleProvider
{
    IReadOnlyDictionary<CompilerFactId, VerifierRuleId> GetRules();
}

public sealed record CompilerFactVerifierRouteDescriptor(
    VerifierRuleId RuleId,
    string CanonicalOwner);

public interface ICompilerFactVerifierRouteProvider
{
    IReadOnlyDictionary<CompilerFactId, CompilerFactVerifierRouteDescriptor> GetRoutes();
}

public sealed class CoreCompilerFactVerifierRuleProvider :
    ICompilerFactVerifierRuleProvider,
    ICompilerFactVerifierRouteProvider
{
    public IReadOnlyDictionary<CompilerFactId, VerifierRuleId> GetRules() =>
        CompilerFactVerifierRegistry.CreateCoreRules();

    public IReadOnlyDictionary<CompilerFactId, CompilerFactVerifierRouteDescriptor> GetRoutes() =>
        CompilerFactVerifierRegistry.CreateCoreRoutes();
}

public sealed class CompilerFactVerifierRegistry
{
    private readonly IReadOnlyDictionary<CompilerFactId, CompilerFactVerifierRouteDescriptor> _routesByFact;

    public CompilerFactVerifierRegistry(IReadOnlyDictionary<CompilerFactId, VerifierRuleId> rulesByFact)
        : this(rulesByFact.ArgNotNull().ToDictionary(
            static pair => pair.Key,
            static pair => new CompilerFactVerifierRouteDescriptor(pair.Value, pair.Value.Value)))
    {
    }

    public CompilerFactVerifierRegistry(
        IReadOnlyDictionary<CompilerFactId, CompilerFactVerifierRouteDescriptor> routesByFact)
    {
        routesByFact = routesByFact.ArgNotNull();
        _routesByFact = routesByFact.ToDictionary(
            static pair => pair.Key,
            static pair => ValidateDescriptor(pair.Key, pair.Value));
    }

    public CompilerFactVerifierRegistry(IEnumerable<ICompilerFactVerifierRuleProvider> providers)
        : this(BuildRoutes(providers))
    {
    }

    public static CompilerFactVerifierRegistry Core { get; } = new(CreateCoreRoutes());

    public IReadOnlySet<CompilerFactId> KnownFacts => _routesByFact.Keys.ToHashSet();

    internal static IReadOnlyDictionary<CompilerFactId, VerifierRuleId> CreateCoreRules() =>
        CreateCoreRoutes().ToDictionary(static pair => pair.Key, static pair => pair.Value.RuleId);

    internal static IReadOnlyDictionary<CompilerFactId, CompilerFactVerifierRouteDescriptor> CreateCoreRoutes() =>
        new Dictionary<CompilerFactId, CompilerFactVerifierRouteDescriptor>
        {
            [KnownCoreCompilerFacts.BytecodeVerified] = new(KnownCoreVerifierRules.BytecodeContract, "core.bytecode"),
            [KnownCoreCompilerFacts.AirSchemaValid] = new(KnownCoreVerifierRules.AirContract, "core.air"),
            [KnownCoreCompilerFacts.AirBranchTargetsValid] = new(KnownCoreVerifierRules.AirContract, "core.air"),
            [KnownCoreCompilerFacts.AirStackBalanced] = new(KnownCoreVerifierRules.AirContract, "core.air"),
            [KnownCoreCompilerFacts.AirBranchStackCompatible] = new(KnownCoreVerifierRules.AirContract, "core.air"),
            [KnownCoreCompilerFacts.AirIntrinsicsSupported] = new(KnownCoreVerifierRules.AirContract, "core.air"),
            [KnownCoreCompilerFacts.AirVerified] = new(KnownCoreVerifierRules.AirContract, "core.air"),
            [KnownCoreCompilerFacts.BackendInputVerified] = new(KnownCoreVerifierRules.BackendInputContract, "core.backend-input")
        };

    public bool TryGetVerifier(CompilerFactId factId, out VerifierRuleId ruleId)
    {
        if (_routesByFact.TryGetValue(factId, out var route))
        {
            ruleId = route.RuleId;
            return true;
        }

        ruleId = default;
        return false;
    }

    public bool TryGetRoute(
        CompilerFactId factId,
        out CompilerFactVerifierRouteDescriptor route) =>
        _routesByFact.TryGetValue(factId, out route!);

    private static IReadOnlyDictionary<CompilerFactId, CompilerFactVerifierRouteDescriptor> BuildRoutes(
        IEnumerable<ICompilerFactVerifierRuleProvider> providers)
    {
        providers = providers.ArgNotNull();
        var normalizedProviders = providers
            .Select(static provider => provider.ArgNotNull())
            .OrderBy(static provider => provider.GetType().FullName, StringComparer.Ordinal)
            .ToArray();
        var routes = new Dictionary<CompilerFactId, CompilerFactVerifierRouteDescriptor>();
        foreach (var provider in normalizedProviders)
        {
            var providerRoutes = provider is ICompilerFactVerifierRouteProvider routeProvider
                ? routeProvider.GetRoutes().ArgNotNull()
                : provider.GetRules().ArgNotNull().ToDictionary(
                    static pair => pair.Key,
                    static pair => new CompilerFactVerifierRouteDescriptor(pair.Value, pair.Value.Value));
            foreach (var (fact, candidate) in providerRoutes
                         .OrderBy(static pair => pair.Key.Value, StringComparer.Ordinal))
            {
                var route = ValidateDescriptor(fact, candidate);
                if (routes.TryGetValue(fact, out var existing) && existing != route)
                {
                    throw new InvalidOperationException(
                        $"Compiler fact '{fact}' is routed to conflicting verifier routes " +
                        $"'{existing.RuleId}'/'{existing.CanonicalOwner}' and " +
                        $"'{route.RuleId}'/'{route.CanonicalOwner}'.");
                }

                routes[fact] = route;
            }
        }

        return routes;
    }

    private static CompilerFactVerifierRouteDescriptor ValidateDescriptor(
        CompilerFactId fact,
        CompilerFactVerifierRouteDescriptor descriptor)
    {
        descriptor = descriptor.ArgNotNull();
        if (string.IsNullOrWhiteSpace(descriptor.CanonicalOwner))
            throw new InvalidOperationException($"Compiler fact '{fact}' has no canonical verifier owner.");
        return descriptor;
    }
}
