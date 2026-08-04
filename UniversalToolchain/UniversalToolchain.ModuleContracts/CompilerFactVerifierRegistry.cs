namespace UniversalToolchain.ModuleContracts;

public interface ICompilerFactVerifierRuleProvider
{
    IReadOnlyDictionary<CompilerFactId, VerifierRuleId> GetRules();
}

public sealed record CompilerFactVerifierRouteDescriptor(
    VerifierRuleId RuleId,
    string CanonicalOwner,
    CompilerPipelineStage EarliestExecutableBoundary = CompilerPipelineStage.Bytecode);

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
            [KnownCoreCompilerFacts.BytecodeVerified] = new(
                KnownCoreVerifierRules.BytecodeContract,
                "core.bytecode",
                CompilerPipelineStage.Bytecode),
            [KnownCoreCompilerFacts.AirSchemaValid] = new(
                KnownCoreVerifierRules.AirContract,
                "core.air",
                CompilerPipelineStage.Air),
            [KnownCoreCompilerFacts.AirBranchTargetsValid] = new(
                KnownCoreVerifierRules.AirContract,
                "core.air",
                CompilerPipelineStage.Air),
            [KnownCoreCompilerFacts.AirStackBalanced] = new(
                KnownCoreVerifierRules.AirContract,
                "core.air",
                CompilerPipelineStage.Air),
            [KnownCoreCompilerFacts.AirBranchStackCompatible] = new(
                KnownCoreVerifierRules.AirContract,
                "core.air",
                CompilerPipelineStage.Air),
            [KnownCoreCompilerFacts.AirIntrinsicsSupported] = new(
                KnownCoreVerifierRules.AirContract,
                "core.air",
                CompilerPipelineStage.Air),
            [KnownCoreCompilerFacts.AirVerified] = new(
                KnownCoreVerifierRules.AirContract,
                "core.air",
                CompilerPipelineStage.Air),
            [KnownCoreCompilerFacts.BackendInputVerified] = new(
                KnownCoreVerifierRules.BackendInputContract,
                "core.backend-input",
                CompilerPipelineStage.BackendInput)
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

    public IReadOnlyList<CompilerFactId> GetFactsForRoute(
        VerifierRuleId ruleId,
        CompilerPipelineStage boundary) =>
        _routesByFact
            .Where(pair =>
                pair.Value.RuleId == ruleId &&
                pair.Value.EarliestExecutableBoundary <= boundary)
            .Select(static pair => pair.Key)
            .OrderBy(static fact => fact.Value, StringComparer.Ordinal)
            .ToArray();

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
                        $"'{existing.RuleId}'/'{existing.CanonicalOwner}'@'{existing.EarliestExecutableBoundary}' and " +
                        $"'{route.RuleId}'/'{route.CanonicalOwner}'@'{route.EarliestExecutableBoundary}'.");
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
        if (!Enum.IsDefined(descriptor.EarliestExecutableBoundary))
        {
            throw new InvalidOperationException(
                $"Compiler fact '{fact}' has unknown earliest executable boundary " +
                $"'{descriptor.EarliestExecutableBoundary}'.");
        }
        return descriptor;
    }
}
