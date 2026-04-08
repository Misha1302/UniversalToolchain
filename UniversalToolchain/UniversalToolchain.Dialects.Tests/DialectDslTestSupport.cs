using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using CommonExceptions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using AstNodeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.ParserWrapper.AstNodeTag>;

namespace UniversalToolchain.Dialects.Tests;

internal static class DialectDslTestSupport
{
    public static readonly string[] ExpectedBuiltInKeywords =
    [
        "use", "exclude", "requires", "before", "after", "backend", "allow", "forbid", "enable", "disable", "security", "capability"
    ];

    public static readonly string[] RepresentativeSources =
    [
        "dialect Demo\n",
        "dialect Demo\nuse Arithmetic,Variables\nexclude Legacy\nrequires Core\nbefore Parsing\nafter Lowering\n",
        "dialect Demo\nbackend cil,interpreter\nallow add_i32\nforbid sub_i32\nenable Ssa\ndisable Inlining\nsecurity trusted\ncapability sandbox\n",
        "dialect Demo\nuse Arithmetic\nbackend cil\nallow add_i32\nenable Ssa\ncapability sandbox\n"
    ];

    public static DialectDefinitionSlice CompileWithFrontendModule(DialectDslFrontendModule module, string source)
    {
        var parser = new BasicParserImpl(new ParserConfiguration([]));
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        var translator = new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([]));

        module.InitLexer(lexer);
        module.InitParser(parser);
        module.InitAstTranslator(translator);

        var ast = parser.Parse(lexer.Lexemize(source));
        var bytecode = translator.Translate(module.ProcessAst(ast));
        using var provider = CreateFrontendCompilerServices(module);
        var ir = provider.GetRequiredService<Func<IAbstractMethodsTranslator>>()().Translate(bytecode);
        return DialectDefinitionSliceAirReader.Read(ir);
    }

    public static AstNode ParseAstWithFrontendModule(DialectDslFrontendModule module, string source)
    {
        var parser = new BasicParserImpl(new ParserConfiguration([]));
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        module.InitLexer(lexer);
        module.InitParser(parser);
        return parser.Parse(lexer.Lexemize(source));
    }

    public static List<LexemeValue> Lex(DialectDslRegistry registry, string source)
    {
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        lexer.AddLexemes(DialectDslLexemeRegistry.CreateRegistrations(registry));
        return lexer.Lexemize(source);
    }

    public static void AssertSlicesEquivalent(DialectDefinitionSlice expected, DialectDefinitionSlice actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.UseModules, Is.EqualTo(expected.UseModules));
            Assert.That(actual.ExcludeModules, Is.EqualTo(expected.ExcludeModules));
            Assert.That(actual.OrderDirectives.Select(x => (x.Kind, x.SourceModule, x.TargetModule)),
                Is.EqualTo(expected.OrderDirectives.Select(x => (x.Kind, x.SourceModule, x.TargetModule))));
            Assert.That(actual.BackendDirectives.Select(x => (x.Backend, x.Enabled)),
                Is.EqualTo(expected.BackendDirectives.Select(x => (x.Backend, x.Enabled))));
            Assert.That(actual.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)),
                Is.EqualTo(expected.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target))));
            Assert.That(actual.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)),
                Is.EqualTo(expected.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target))));
            Assert.That(actual.SecurityProfile, Is.EqualTo(expected.SecurityProfile));
            Assert.That(actual.CapabilityDirectives.Select(x => (x.Name, x.Value)),
                Is.EqualTo(expected.CapabilityDirectives.Select(x => (x.Name, x.Value))));
        });
    }

    public static void AssertParserExceptionContains(ParserException exception, params string[] fragments)
    {
        Assert.That(exception, Is.Not.Null);
        Assert.Multiple(() =>
        {
            foreach (var fragment in fragments)
                Assert.That(exception!.Message, Does.Contain(fragment), $"Expected parser exception to contain '{fragment}'.");
        });
    }

    public static DialectDefinition BuildDefinition(DialectDefinitionSlice slice, out List<DialectDiagnostic> diagnostics)
    {
        diagnostics = [];
        return DialectDefinitionSemanticBinder.Bind(slice, diagnostics);
    }

    public static ServiceProvider CreateFrontendCompilerServices(DialectDslFrontendModule module)
    {
        var services = new ServiceCollection();
        services.AddDialectDslFrontendCompilerServices(module);
        return services.BuildServiceProvider();
    }

    public static IAbstractMethodsTranslator CreateAbstractMethodsTranslator()
    {
        return CreateAbstractMethodsTranslator(DialectDslTestComposition.CreateFrontendModule());
    }

    public static IAbstractMethodsTranslator CreateAbstractMethodsTranslator(DialectDslFrontendModule module)
    {
        if (module == null)
            throw new ArgumentNullException(nameof(module));

        using var provider = CreateFrontendCompilerServices(module);
        return provider.GetRequiredService<Func<IAbstractMethodsTranslator>>()();
    }
}

internal class RecordingProvider(int order, string providerName, Action<DialectDslRegistryBuilder> registration, List<string> executionLog) : IDialectDslFeatureProvider
{
    public int Order => order;

    public void Register(DialectDslRegistryBuilder builder)
    {
        executionLog.Add(providerName);
        registration(builder);
    }
}

internal class RecordingProviderAlpha(Action<DialectDslRegistryBuilder> registration, List<string> executionLog) : RecordingProvider(25, nameof(RecordingProviderAlpha), registration, executionLog);

internal class RecordingProviderOmega(Action<DialectDslRegistryBuilder> registration, List<string> executionLog) : RecordingProvider(25, nameof(RecordingProviderOmega), registration, executionLog);

internal sealed class AliasDirectiveFeatureProvider : IDialectDslFeatureProvider
{
    public int Order => 100;

    public void Register(DialectDslRegistryBuilder builder)
    {
        builder.RegisterFeature(new AliasDirectiveFeature("alias", "tests.alias", "alias:", 10));
    }
}

internal sealed class AliasDirectiveFeatureProvider2 : IDialectDslFeatureProvider
{
    public int Order => 100;

    public void Register(DialectDslRegistryBuilder builder)
    {
        builder.RegisterFeature(new AliasDirectiveFeature("alias-2", "tests.alias-2", "alias-2:", 11));
    }
}

internal sealed class DirectAliasDirectiveFeature : AliasDirectiveFeature
{
    public DirectAliasDirectiveFeature() : base("direct-alias", "tests.direct-alias", "direct-alias:", 12)
    {
    }
}

internal class AliasDirectiveFeature(string keyword, string id, string capabilityPrefix, int sequence) : IDialectDirectiveFeature
{
    private static readonly DialectListStateKey<string> AccumulationKey = new("AliasMappings");
    private static readonly DialectSetStateKey<string> ValidationKey = new("AliasMappings", StringComparer.Ordinal);

    public string Id => id;

    public string Keyword => keyword;

    public string LexemeTag => $"DialectDirectiveKeyword.{keyword}";

    public DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, sequence);

    public bool IsSingleton => false;

    public string SingletonViolationMessage => $"Directive '{keyword}' can only be declared once.";

    public DialectDirectiveAstNode ParseDirective(AstNode lineNode)
    {
        if (lineNode.Children.Count != 3)
            DialectDefinitionSliceParseErrors.Fail($"Directive '{keyword}' expects exactly two identifiers.", lineNode.Children[0].LexemeValue);

        var source = lineNode.Children[1];
        var target = lineNode.Children[2];
        if (!DialectLexemeTags.IsTag(source.LexemeValue, DialectLexemeTags.Identifier) || !DialectLexemeTags.IsTag(target.LexemeValue, DialectLexemeTags.Identifier))
            DialectDefinitionSliceParseErrors.Fail($"Directive '{keyword}' expects identifier arguments.", source.LexemeValue ?? target.LexemeValue);

        return new DialectDirectiveAstNode(this, lineNode.Children[0].LexemeValue,
        [
            new AliasDirectivePayloadAstNode(new IdentifierValueAstNode(source.LexemeValue!), new IdentifierValueAstNode(target.LexemeValue!))
        ]);
    }

    public void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        if (line.Count != 3)
            DialectDefinitionSliceParseErrors.Fail($"Directive '{keyword}' expects exactly two identifiers.", line[0]);

        accumulation.GetOrCreateList(AccumulationKey).Add($"{line[1].Text}->{line[2].Text}");
    }

    public void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        var payload = GetPayload(directive);
        if (string.Equals(payload.Source.Identifier, payload.Target.Identifier, StringComparison.Ordinal))
            DialectDefinitionSliceParseErrors.Fail("Alias source and target must differ.", directive.LexemeValue);

        context.AddValue(ValidationKey, $"{payload.Source.Identifier}->{payload.Target.Identifier}", "Duplicate alias directive is not allowed.", directive.LexemeValue);
    }

    public IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        var payload = GetPayload(directive);
        return [new CapabilityAirAnnotation([$"{capabilityPrefix}{payload.Source.Identifier}->{payload.Target.Identifier}"])];
    }

    private static AliasDirectivePayloadAstNode GetPayload(DialectDirectiveAstNode directive) =>
        directive.Payload as AliasDirectivePayloadAstNode
        ?? throw new ArgumentException("Alias directive payload is invalid.", nameof(directive));
}

internal sealed class DuplicateAliasDirectiveFeature : AliasDirectiveFeature
{
    public DuplicateAliasDirectiveFeature() : base("alias", "tests.alias.duplicate", "dup-alias:", 13)
    {
    }
}

internal sealed class SingletonNoteDirectiveFeature : SimpleIdentifierDirectiveFeatureBase
{
    public override string Id => "tests.singleton.note";
    public override string Keyword => "note";
    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, 50);
    public override bool IsSingleton => true;
    public override string SingletonViolationMessage => "note directive can only be declared once";
}

internal sealed class CollisionFeatureA : SimpleIdentifierDirectiveFeatureBase
{
    public override string Id => "tests.collision.a";
    public override string Keyword => "collision-a";
    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, 12);
}

internal sealed class CollisionFeatureB : SimpleIdentifierDirectiveFeatureBase
{
    public override string Id => "tests.collision.b";
    public override string Keyword => "collision-b";
    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, 12);
}

internal sealed class RegexKeywordDirectiveFeature(string keyword, int sequence) : SimpleIdentifierDirectiveFeatureBase
{
    public override string Id => $"tests.regex.{keyword}";
    public override string Keyword => keyword;
    public override string LexemeTag => $"DialectDirectiveKeyword.{keyword}";
    public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, sequence);
}

internal abstract class SimpleIdentifierDirectiveFeatureBase : IDialectDirectiveFeature
{
    public virtual string LexemeTag => $"DialectDirectiveKeyword.{Keyword}";
    public virtual bool IsSingleton => false;
    public virtual string SingletonViolationMessage => $"Directive '{Keyword}' can only be declared once.";
    public abstract string Id { get; }
    public abstract string Keyword { get; }
    public abstract DialectDirectiveParserOrder ParserOrder { get; }

    public DialectDirectiveAstNode ParseDirective(AstNode lineNode)
    {
        if (lineNode.Children.Count != 2 || !DialectLexemeTags.IsTag(lineNode.Children[1].LexemeValue, DialectLexemeTags.Identifier))
            DialectDefinitionSliceParseErrors.Fail($"Directive '{Keyword}' expects a single identifier.", lineNode.Children.ElementAtOrDefault(1)?.LexemeValue ?? lineNode.Children[0].LexemeValue);

        return new DialectDirectiveAstNode(this, lineNode.Children[0].LexemeValue, [new IdentifierValueAstNode(lineNode.Children[1].LexemeValue!)]);
    }

    public void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation)
    {
        if (line.Count != 2)
            DialectDefinitionSliceParseErrors.Fail($"Directive '{Keyword}' expects a single identifier.", line[0]);

        accumulation.GetOrCreateList(new DialectListStateKey<string>($"accumulation.{Keyword}")).Add(line[1].Text);
    }

    public virtual void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
    {
        var payload = directive.Payload as IdentifierValueAstNode
                      ?? throw new ArgumentException("Directive payload is invalid.", nameof(directive));

        if (string.IsNullOrWhiteSpace(payload.Identifier))
            DialectDefinitionSliceParseErrors.Fail($"Directive '{Keyword}' identifier must not be empty.", directive.LexemeValue);
    }

    public virtual IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive)
    {
        var payload = directive.Payload as IdentifierValueAstNode
                      ?? throw new ArgumentException("Directive payload is invalid.", nameof(directive));
        return [new CapabilityAirAnnotation([$"{Keyword}:{payload.Identifier}"])];
    }
}

internal sealed class AliasDirectivePayloadAstNode : DialectAstNode
{
    private static readonly AstNodeType PayloadNodeType = AstNodeType.CreateOrGet("AliasDirectivePayload");

    public AliasDirectivePayloadAstNode(IdentifierValueAstNode source, IdentifierValueAstNode target) : base(PayloadNodeType, null, [source, target])
    {
    }

    public IdentifierValueAstNode Source => (IdentifierValueAstNode)Children[0];

    public IdentifierValueAstNode Target => (IdentifierValueAstNode)Children[1];
}
