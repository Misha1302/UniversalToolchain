using BasicCore.Compilation;
using ExceptionsManager;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Rules.Abstractions;

namespace UniversalToolchain.Dialects.Wist.Rules;

public interface IWistRuleSetCompiler
{
    RuleSetCompileResult Compile(string source, string mode);
}

public sealed class WistRuleSetCompiler : IWistRuleSetCompiler
{
    private readonly Func<string, OrderedDictionary<string, Type>, string, ICompiledArtifact> _artifactCompiler;
    private readonly IWistRuleArgumentBinder _argumentBinder;
    private readonly WistRuleBodyValidator _validator;
    private readonly WistRuleDeclarationExtractor _extractor;
    private readonly WistRuleRuntimeTypeResolver _typeResolver;

    public WistRuleSetCompiler(
        Func<string, OrderedDictionary<string, Type>, string, ICompiledArtifact> artifactCompiler,
        IReadOnlyList<RuleRuntimeTypeBinding> ruleRuntimeTypeBindings)
    {
        _artifactCompiler = artifactCompiler.ArgNotNull();
        ruleRuntimeTypeBindings = ruleRuntimeTypeBindings.ArgNotNull();

        _extractor = new WistRuleDeclarationExtractor();
        _validator = new WistRuleBodyValidator();

        var typeResolver = new WistRuleRuntimeTypeResolver(ruleRuntimeTypeBindings);
        var valueAdapter = new WistRuleRuntimeValueAdapter(typeResolver);

        _typeResolver = typeResolver;
        _argumentBinder = new WistRuleArgumentBinder(valueAdapter);
    }

    public RuleSetCompileResult Compile(string source, string mode)
    {
        var extraction = _extractor.Extract(source);
        if (!extraction.IsSuccess)
            return new RuleSetCompileResult(false, null, extraction.Diagnostics);

        var validationDiagnostics = _validator.Validate(extraction.Rules);
        if (validationDiagnostics.Count > 0)
            return RuleSetCompileResult.Failure(validationDiagnostics);

        var diagnostics = new List<ToolchainDiagnostic>();
        var compiledRules = new List<ICompiledRule>();

        foreach (var rule in extraction.Rules.OrderBy(static x => x.Name, StringComparer.Ordinal))
        {
            var declaredBindings = CreateDeclaredBindings(rule.Parameters);
            try
            {
                var artifact = _artifactCompiler(rule.Body.SourceText, declaredBindings, mode);
                compiledRules.Add(new CompiledWistRule(CreateDescriptor(rule), artifact, _argumentBinder));
            }
            catch (Exception ex)
            {
                diagnostics.Add(new ToolchainDiagnostic(
                    ToolchainDiagnosticCodes.RuleInvalidBody,
                    ToolchainDiagnosticSeverity.Error,
                    $"Rule '{rule.Name}' could not be compiled: {ex.Message}",
                    null,
                    []));
            }
        }

        return diagnostics.Count == 0
            ? RuleSetCompileResult.Success(new CompiledWistRuleSet(compiledRules))
            : RuleSetCompileResult.Failure(diagnostics);
    }

    private OrderedDictionary<string, Type> CreateDeclaredBindings(IReadOnlyList<RuleParameterModel> parameters)
    {
        var declaredBindings = new OrderedDictionary<string, Type>();

        foreach (var parameter in parameters)
        {
            if (_typeResolver.TryResolve(parameter.Type, out var runtimeType))
                declaredBindings[parameter.Name] = runtimeType;
            else
                _ = Thrower.NotSupported<Type>($"Unsupported rule type '{parameter.Type.Name}'.");
        }

        return declaredBindings;
    }

    private static CompiledRuleDescriptor CreateDescriptor(RuleDeclarationModel rule)
    {
        return new CompiledRuleDescriptor(
            rule.Name,
            rule.Parameters.Select(static x => new RuleParameterDescriptor(x.Name, x.Type)).ToList(),
            rule.ReturnType);
    }
}
