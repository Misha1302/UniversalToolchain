using BasicCodeTranslator;
using BasicCore.Builtins;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using CommonExceptions;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Frontend.Composition;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDslArchitectureRefactorTests
{
    [Test]
    public void AddDialectDsl_RegistersBuiltInCompositionThroughDi()
    {
        using var provider = DialectDslTestComposition.CreateProvider();

        var registry = provider.GetRequiredService<DialectDslRegistry>();
        var module = provider.GetRequiredService<DialectDslFrontendModule>();
        var compiler = provider.GetRequiredService<DialectDslCompiler>();
        var frontendModules = provider.GetServices<IFrontendCoreModule>().ToList();

        Assert.Multiple(() =>
        {
            Assert.That(registry.DirectiveFeatures.Select(x => x.Keyword), Does.Contain("security"));
            Assert.That(module.Registry, Is.SameAs(registry));
            Assert.That(compiler.Compile("dialect Demo\nuse Arithmetic\n").UseModules, Is.EqualTo(new[] { "Arithmetic" }));
            Assert.That(frontendModules, Has.Member(module));
        });
    }

    [Test]
    public void AddDialectDslDefaultComposition_RegistersSharedBuiltInComposition()
    {
        var services = new ServiceCollection();
        services.AddDialectDslDefaultComposition();

        using var provider = services.BuildServiceProvider();
        var registry = provider.GetRequiredService<DialectDslRegistry>();
        var compiler = provider.GetRequiredService<DialectDslCompiler>();

        Assert.Multiple(() =>
        {
            Assert.That(registry.DirectiveFeatures.Select(x => x.Keyword), Does.Contain("capability"));
            Assert.That(compiler.Compile("dialect Demo\nuse Arithmetic\n").UseModules, Is.EqualTo(new[] { "Arithmetic" }));
        });
    }

    [Test]
    public void Compiler_DefaultConstructor_PreservesBuiltInStandaloneUsage()
    {
        using var compiler = new DialectDslCompiler();

        var slice = compiler.Compile("dialect Demo\nuse Arithmetic\n");

        Assert.That(slice.UseModules, Is.EqualTo(new[] { "Arithmetic" }));
    }

    [Test]
    public void Compiler_DefaultConstructor_UsesSameBuiltInCompositionAsDiCompiler()
    {
        const string source =
            """
            dialect Demo
            use Arithmetic,Variables
            exclude Experimental
            requires Core
            before Parsing
            after Lowering
            backend cil
            allow add_i32
            forbid sub_i32
            enable Ssa
            disable Inlining
            security trusted
            capability sandbox
            """;

        using var standaloneCompiler = new DialectDslCompiler();
        using var diCompiler = DialectDslTestComposition.CreateCompiler();
        var standaloneSlice = standaloneCompiler.Compile(source);
        var diSlice = diCompiler.Compile(source);

        Assert.Multiple(() =>
        {
            Assert.That(standaloneSlice.Name, Is.EqualTo(diSlice.Name));
            Assert.That(standaloneSlice.UseModules, Is.EqualTo(diSlice.UseModules));
            Assert.That(standaloneSlice.ExcludeModules, Is.EqualTo(diSlice.ExcludeModules));
            Assert.That(
                standaloneSlice.OrderDirectives.Select(x => (x.Kind, x.SourceModule, x.TargetModule)),
                Is.EqualTo(diSlice.OrderDirectives.Select(x => (x.Kind, x.SourceModule, x.TargetModule))));
            Assert.That(
                standaloneSlice.BackendDirectives.Select(x => (x.Backend, x.Enabled)),
                Is.EqualTo(diSlice.BackendDirectives.Select(x => (x.Backend, x.Enabled))));
            Assert.That(
                standaloneSlice.IntrinsicDirectives.Select(x => (x.Name, x.Allowed)),
                Is.EqualTo(diSlice.IntrinsicDirectives.Select(x => (x.Name, x.Allowed))));
            Assert.That(
                standaloneSlice.OptimizerDirectives.Select(x => (x.Name, x.Enabled)),
                Is.EqualTo(diSlice.OptimizerDirectives.Select(x => (x.Name, x.Enabled))));
            Assert.That(standaloneSlice.SecurityProfile, Is.EqualTo(diSlice.SecurityProfile));
            Assert.That(
                standaloneSlice.CapabilityDirectives.Select(x => x.Name),
                Is.EqualTo(diSlice.CapabilityDirectives.Select(x => x.Name)));
        });
    }

    [Test]
    public void Compiler_DefaultConstructor_PreservesBuiltInDocumentValidationRules()
    {
        using var compiler = new DialectDslCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("dialect Demo\nuse Arithmetic\nexclude Arithmetic\n"));

        Assert.That(ex!.Message, Does.Contain("use").And.Contain("exclude"));
    }

    [Test]
    public void ProviderCompositionOrder_IsDeterministic_WhenProvidersShareOrder()
    {
        using var provider = DialectDslTestComposition.CreateProvider(services =>
        {
            services.AddSingleton<ProviderExecutionRecorder>();
            services.AddDialectDirectiveFeatureProvider<ZedTrackingProvider>();
            services.AddDialectDirectiveFeatureProvider<AlphaTrackingProvider>();
        });

        _ = provider.GetRequiredService<DialectDslRegistry>();
        var recorder = provider.GetRequiredService<ProviderExecutionRecorder>();

        Assert.That(recorder.Executions, Is.EqualTo(new[] { nameof(AlphaTrackingProvider), nameof(ZedTrackingProvider) }));
    }

    [Test]
    public void ParserOrdering_IsDeterministic_WithoutFloatEncoding()
    {
        var registry = DialectDslTestComposition.CreateRegistry();
        var registrations = DialectDslParserNodeRegistry.CreateRegistrations(registry);
        var priorities = registrations.Select(x => x.Priority).ToArray();
        var directiveCreators = registrations.Select(x => x.Creator.GetType().Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(DialectParserOrders.LineSplitter.CompareTo(DialectParserOrders.Declaration), Is.LessThan(0));
            Assert.That(DialectParserOrder.Directive(new DialectDirectiveParserOrder(DialectDirectiveSlot.Security, 0)).CompareTo(DialectParserOrders.Document), Is.LessThan(0));
            Assert.That(priorities, Is.EqualTo(Enumerable.Range(0, registrations.Count).Select(x => (float)x).ToArray()));
            Assert.That(directiveCreators, Does.Contain(nameof(FeatureDialectDirectiveNodeCreator)));
        });
    }

    [Test]
    public void OrderingCollisions_AreRejectedWithMeaningfulException()
    {
        var services = new ServiceCollection();
        services.AddDialectDsl();
        services.AddDialectDirectiveFeature<CollisionFeatureA>();
        services.AddDialectDirectiveFeature<CollisionFeatureB>();

        using var provider = services.BuildServiceProvider();
        var ex = Assert.Throws<InvalidOperationException>(() => provider.GetRequiredService<DialectDslRegistry>());

        Assert.That(ex!.Message, Does.Contain("collision").And.Contain("tests.collision.a").And.Contain("tests.collision.b"));
    }

    [Test]
    public void SingletonEnforcement_IsCentralized_WhenFeatureDeclaresSingleton()
    {
        var compiler = DialectDslTestComposition.CreateCompiler(services => services.AddDialectDirectiveFeature<SingletonNoteDirectiveFeature>());

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("dialect Demo\nnote first\nnote second\n"));

        Assert.That(ex!.Message, Does.Contain("note directive can only be declared once"));
    }

    [Test]
    public void ValidationState_UsesTypedKeys_ForExtensionSafeStorage()
    {
        var context = new DialectDirectiveValidationContext();
        var setKey = new DialectSetStateKey<string>("tests.validation.set", StringComparer.Ordinal);
        var stateKey = new DialectValueStateKey<List<string>>("tests.validation.state");

        context.AddValue(setKey, "alpha", "duplicate", null);
        var values = context.GetValues(setKey);
        var state = context.GetOrAddState(stateKey, static () => []);
        state.Add("beta");

        Assert.Multiple(() =>
        {
            Assert.That(values, Is.EquivalentTo(new[] { "alpha" }));
            Assert.That(context.GetOrAddState(stateKey, static () => throw new InvalidOperationException()), Is.SameAs(state));
        });
    }

    [Test]
    public void AccumulationState_UsesTypedKeys_ForExtensionSafeStorage()
    {
        var accumulation = new DialectDirectiveAccumulation();
        var listKey = new DialectListStateKey<string>("tests.accumulation.list");
        var valueKey = new DialectValueStateKey<int?>("tests.accumulation.value");

        accumulation.GetOrCreateList(listKey).Add("alpha");
        accumulation.SetValue(valueKey, 42);

        Assert.Multiple(() =>
        {
            Assert.That(accumulation.GetOrCreateList(listKey), Is.EqualTo(new[] { "alpha" }));
            Assert.That(accumulation.GetValue(valueKey), Is.EqualTo(42));
        });
    }

    [Test]
    public void KeywordRegexEscaping_SupportsDirectiveKeywordsWithRegexMetacharacters()
    {
        var registry = DialectDslTestComposition.CreateRegistry(services => services.AddDialectDirectiveFeature<RegexKeywordDirectiveFeature>());
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        lexer.AddLexemes(DialectDslLexemeRegistry.CreateRegistrations(registry));

        var lexemes = lexer.Lexemize("dialect Demo\nmeta+ value\n");
        var metaKeyword = lexemes.Single(x => x.Text == "meta+");

        Assert.That(metaKeyword.LexemePattern!.LexemeType.GetName(), Is.EqualTo("DialectDirectiveKeyword.meta+"));
    }

    [Test]
    public void BuiltInPolicies_PreserveIntrinsicOptimizerAndSecuritySemantics()
    {
        using var compiler = DialectDslTestComposition.CreateCompiler();
        var slice = compiler.Compile(
            """
            dialect Demo
            allow add_i32
            enable Ssa
            """);

        Assert.Multiple(() =>
        {
            Assert.That(slice.IntrinsicDirectives.Select(x => (x.Name, x.Allowed)), Is.EqualTo(new[] { ("add_i32", true) }));
            Assert.That(slice.OptimizerDirectives.Select(x => (x.Name, x.Enabled)), Is.EqualTo(new[] { ("Ssa", true) }));
            Assert.That(slice.SecurityProfile, Is.Null);
        });
    }

    [Test]
    public void DefaultCompilerAndDefaultFrontendModule_RemainCompositionallyEquivalent()
    {
        const string source =
            """
            dialect Demo
            use Arithmetic
            capability sandbox
            """;

        using var compiler = new DialectDslCompiler();
        var standaloneSlice = compiler.Compile(source);
        var frontendModuleSlice = ParseWithFrontendModule(DialectDslTestComposition.CreateFrontendModule(), source);

        Assert.Multiple(() =>
        {
            Assert.That(standaloneSlice.Name, Is.EqualTo(frontendModuleSlice.Name));
            Assert.That(standaloneSlice.UseModules, Is.EqualTo(frontendModuleSlice.UseModules));
            Assert.That(
                standaloneSlice.CapabilityDirectives.Select(x => x.Name),
                Is.EqualTo(frontendModuleSlice.CapabilityDirectives.Select(x => x.Name)));
        });
    }

    private static DialectDefinitionSlice ParseWithFrontendModule(DialectDslFrontendModule module, string source)
    {
        var parser = new BasicParserImpl(new ParserConfiguration([]));
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        var translator = new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([]));

        module.InitLexer(lexer);
        module.InitParser(parser);
        module.InitAstTranslator(translator);

        var ast = parser.Parse(lexer.Lexemize(source));
        var bytecode = translator.Translate(module.ProcessAst(ast));
        using var provider = DialectDslTestSupport.CreateFrontendCompilerServices(module);
        var ir = provider.GetRequiredService<Func<IAbstractMethodsTranslator>>()().Translate(bytecode);
        return DialectDefinitionSliceAirReader.Read(ir);
    }

    [Test]
    public void FrontendCompilerServices_ShouldResolveIntrinsicCatalogThroughSharedDiComposition()
    {
        var module = DialectDslTestComposition.CreateFrontendModule();
        using var provider = DialectDslTestSupport.CreateFrontendCompilerServices(module);
        using var compiler = new DialectDslCompiler(module);

        var providerTypes = provider.GetServices<IIntrinsicDescriptorProvider>()
            .Select(static x => x.GetType())
            .ToArray();
        var slice = compiler.Compile("dialect Demo\nuse Arithmetic\n");

        Assert.Multiple(() =>
        {
            Assert.That(provider.GetRequiredService<IIntrinsicCatalog>(), Is.Not.Null);
            Assert.That(providerTypes, Is.EqualTo(new[]
            {
                typeof(CoreIntrinsicDescriptorProvider)
            }));
            Assert.That(slice.UseModules, Is.EqualTo(new[] { "Arithmetic" }));
        });
    }

    private sealed class ProviderExecutionRecorder
    {
        public List<string> Executions { get; } = [];
    }

    private sealed class AlphaTrackingProvider(ProviderExecutionRecorder recorder) : IDialectDslFeatureProvider
    {
        public int Order => 10;

        public void Register(DialectDslRegistryBuilder builder)
        {
            recorder.Executions.Add(nameof(AlphaTrackingProvider));
        }
    }

    private sealed class ZedTrackingProvider(ProviderExecutionRecorder recorder) : IDialectDslFeatureProvider
    {
        public int Order => 10;

        public void Register(DialectDslRegistryBuilder builder)
        {
            recorder.Executions.Add(nameof(ZedTrackingProvider));
        }
    }

    private sealed class CollisionFeatureA : SimpleIdentifierDirectiveFeatureBase
    {
        public override string Id => "tests.collision.a";
        public override string Keyword => "collision-a";
        public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, 12);
    }

    private sealed class CollisionFeatureB : SimpleIdentifierDirectiveFeatureBase
    {
        public override string Id => "tests.collision.b";
        public override string Keyword => "collision-b";
        public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, 12);
    }

    private sealed class SingletonNoteDirectiveFeature : SimpleIdentifierDirectiveFeatureBase
    {
        public override string Id => "tests.singleton.note";
        public override string Keyword => "note";
        public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, 20);
        public override bool IsSingleton => true;
        public override string SingletonViolationMessage => "note directive can only be declared once";
    }

    private sealed class RegexKeywordDirectiveFeature : SimpleIdentifierDirectiveFeatureBase
    {
        public override string Id => "tests.regex.keyword";
        public override string Keyword => "meta+";
        public override string LexemeTag => "DialectDirectiveKeyword.meta+";
        public override DialectDirectiveParserOrder ParserOrder => new(DialectDirectiveSlot.Extension, 30);
    }

    private abstract class SimpleIdentifierDirectiveFeatureBase : IDialectDirectiveFeature
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
        }

        public void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context)
        {
        }

        public IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive) =>
            [];
    }
}