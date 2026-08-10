using System.Collections.Concurrent;
using AbstractIrConverters;
using ArithmeticModule.Module;
using BasicCodeTranslator;
using BasicCore.Binding;
using BasicCore.Compilation;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using CommonExceptions;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
using NumbersModule.Core;
using NumbersModule.Module;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;
using WhitespacesModule;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistDirectArtifactStageTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public void DirectStages_MatchCanonicalCoreBytecodeAndAir_ForRepresentativeArithmetic()
    {
        const string source = "2 + 3 * 4";
        var canonical = RunCanonicalCoreStages(source, CreateCanonicalModules());
        var direct = RunDirect(source, CreateCanonicalModuleFactories());

        Assert.Multiple(() =>
        {
            Assert.That(direct.Bytecode.Bytecode.ToString(), Is.EqualTo(canonical.Bytecode.ToString()));
            Assert.That(AirSignature(direct.Air.Air), Is.EqualTo(AirSignature(canonical.Air)));
            Assert.That(direct.Air.Air.Instructions, Is.Not.Empty);
        });
    }

    [Test]
    public void DirectFrontend_PreservesCanonicalSourceSpans_ForLfCrLfAndCr()
    {
        foreach (var newline in new[] { "\n", "\r\n", "\r" })
        {
            var source = $"1 + 2{newline}+ 3";
            var canonicalCapture = new AstSpanCapture();
            var directCapture = new AstSpanCapture();

            _ = RunCanonicalCoreStages(source, CreateCanonicalModules(new AstSpanCaptureModule(canonicalCapture)));
            _ = RunDirect(source, CreateCanonicalModuleFactories(() => new AstSpanCaptureModule(directCapture)));

            Assert.Multiple(() =>
            {
                Assert.That(directCapture.Signature, Is.EqualTo(canonicalCapture.Signature), $"newline={Escape(newline)}");
                Assert.That(directCapture.Signature, Does.Contain("3@2:2"), $"newline={Escape(newline)}");
            });
        }
    }

    [Test]
    public void DirectFrontend_InvalidTokenMatchesCanonicalStructuredDiagnostic()
    {
        const string source = "1 + @\n2";
        var plan = CreatePlan();
        var directFactory = CreateDirectFactory(CreateCanonicalModuleFactories(), plan);
        var frontend = directFactory.CreateFrontend();
        var context = CreateContext(plan, source);

        var direct = Assert.Throws<LexerException>(() => frontend.Transform(source, context));
        var canonical = Assert.Throws<LexerException>(() =>
            RunCanonicalCoreStages(source, CreateCanonicalModules()));

        Assert.Multiple(() =>
        {
            Assert.That(direct!.Stage, Is.EqualTo(canonical!.Stage));
            Assert.That(direct.Location?.Line, Is.EqualTo(canonical.Location?.Line));
            Assert.That(direct.Location?.Column, Is.EqualTo(canonical.Location?.Column));
            Assert.That(direct.Location?.Line, Is.EqualTo(1));
            Assert.That(direct.Location?.Column, Is.EqualTo(4));
        });
    }

    [Test]
    public void DirectStages_KeepCanonicalArtifactContractsAndExplicitHostBindings()
    {
        var plan = CreatePlan();
        var input = new WistHostBindingAdapter(plan).CreateRuntimeInput(
            "price + fee",
            new Dictionary<string, object?>
            {
                ["price"] = 100.0,
                ["fee"] = 5
            });

        Assert.Multiple(() =>
        {
            Assert.That(WistDirectArtifactKinds.Syntax.Contract, Is.EqualTo(WistArtifactKinds.SyntaxTreeContract));
            Assert.That(WistDirectArtifactKinds.Bytecode.Contract, Is.EqualTo(WistArtifactKinds.BytecodeContract));
            Assert.That(WistDirectArtifactKinds.Air.Contract, Is.EqualTo(WistArtifactKinds.AirContract));
            Assert.That(WistDirectArtifactKinds.Syntax.Contract, Is.Not.EqualTo(WistDirectArtifactKinds.Bytecode.Contract));
            Assert.That(WistDirectArtifactKinds.Bytecode.Contract, Is.Not.EqualTo(WistDirectArtifactKinds.Air.Contract));
            Assert.That(input.ExternalBindings.Select(static binding => binding.Name), Is.EqualTo(new[] { "price", "fee" }));
            Assert.That(input.ExternalBindings.Select(static binding => binding.Type), Is.EqualTo(new[] { typeof(RealNumberImpl), typeof(RealNumberImpl) }));
        });
    }

    [Test]
    public void DirectFrontend_ConcurrentTransformsOwnIndependentModuleInstances()
    {
        var instances = new ConcurrentBag<SessionMarkerModule>();
        var factories = CreateCanonicalModuleFactories(() =>
        {
            var module = new SessionMarkerModule();
            instances.Add(module);
            return module;
        });
        var plan = CreatePlan();
        var frontend = CreateDirectFactory(factories, plan).CreateFrontend();

        var tasks = Enumerable.Range(0, 24)
            .Select(index => Task.Run(() =>
            {
                var source = $"{index} + 1";
                return frontend.Transform(source, CreateContext(plan, source));
            }))
            .ToArray();
        Task.WaitAll(tasks);

        var artifacts = tasks.Select(static task => task.Result).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(instances, Has.Count.EqualTo(24));
            Assert.That(instances.Distinct().Count(), Is.EqualTo(24));
            Assert.That(artifacts.SelectMany(static artifact => artifact.Modules.OfType<SessionMarkerModule>()).Count(), Is.EqualTo(24));
            Assert.That(artifacts.All(static artifact => artifact.Root != null), Is.True);
        });
    }

    [Test]
    public void DirectComponents_AreInternalAndOwnedByWistLanguagePack()
    {
        var assembly = typeof(WistLanguageFeaturePackage).Assembly;
        var types = new[]
        {
            typeof(WistDirectFrontendTransformer),
            typeof(WistDirectBytecodeTransformer),
            typeof(WistDirectAirTransformer),
            typeof(WistHostBindingAdapter),
            typeof(WistSyntaxArtifact),
            typeof(WistBytecodeArtifact),
            typeof(WistAirArtifact)
        };

        Assert.Multiple(() =>
        {
            Assert.That(types.Select(static type => type.Assembly).Distinct(), Is.EqualTo(new[] { assembly }));
            Assert.That(types.All(static type => !type.IsPublic), Is.True);
            Assert.That(types.All(type => type.Namespace == typeof(WistLanguageFeaturePackage).Namespace), Is.True);
            Assert.That(assembly, Is.Not.EqualTo(typeof(CompilationInputNormalizer).Assembly));
        });
    }

    private static DirectSnapshot RunDirect(
        string source,
        IReadOnlyList<Func<IFrontendCoreModule>> moduleFactories)
    {
        var plan = CreatePlan();
        var factory = CreateDirectFactory(moduleFactories, plan);
        var context = CreateContext(plan, source);
        var syntax = factory.CreateFrontend().Transform(source, context);
        var bytecode = factory.CreateBytecodeLowering().Transform(syntax, context);
        var air = factory.CreateAirLowering().Transform(bytecode, context);
        return new DirectSnapshot(syntax, bytecode, air);
    }

    private static CanonicalCoreSnapshot RunCanonicalCoreStages(
        string source,
        IReadOnlyList<IFrontendCoreModule> modules)
    {
        var input = new CompilationInputNormalizer().NormalizeRuntimeInput(source);
        var lexer = new BasicLexerImpl();
        var parser = new BasicParserImpl();
        var astTranslator = new BasicAstToBytecodeTranslatorImpl();
        var methodsTranslator = CreateMethodsTranslator();

        var targetCode = modules.Aggregate(input.SourceText, static (current, module) => module.ProcessText(current));
        foreach (var module in modules)
            module.InitLexer(lexer);
        var lexemes = lexer.Lexemize(targetCode);
        var targetLexemes = modules.Aggregate(lexemes, static (current, module) => module.ProcessLexemes(current));
        foreach (var module in modules)
            module.InitParser(parser);
        var astRoot = parser.Parse(targetLexemes);
        var targetRoot = modules.Aggregate(astRoot, static (current, module) => module.ProcessAst(current));
        var bindingRules = modules.SelectMany(static module => module.GetAstBindingRules()).ToArray();
        var boundRoot = new Binder(input.ExternalBindings, bindingRules).Bind(targetRoot);

        foreach (var module in modules)
            module.InitAstTranslator(astTranslator, modules);
        var bytecode = modules.Aggregate(
            astTranslator.Translate(boundRoot),
            static (current, module) => module.ProcessBytecode(current));
        var air = methodsTranslator.Translate(bytecode);
        return new CanonicalCoreSnapshot(bytecode, air);
    }

    private static WistDirectArtifactStageFactory CreateDirectFactory(
        IReadOnlyList<Func<IFrontendCoreModule>> moduleFactories,
        LanguagePlan plan) => new(
        static () => new BasicLexerImpl(),
        static () => new BasicParserImpl(),
        static () => new BasicAstToBytecodeTranslatorImpl(),
        CreateMethodsTranslator,
        moduleFactories,
        new WistHostBindingAdapter(plan));

    private static IAbstractMethodsTranslator CreateMethodsTranslator()
    {
        var services = new ServiceCollection();
        services.AddCoreIntrinsicServices();
        var provider = services.BuildServiceProvider();
        return new BytecodeToAbstractIrConverterImpl(
            provider.GetRequiredService<IInstructionIntrinsicReader>(),
            provider.GetRequiredService<IIntrinsicTypeStackProcessor>());
    }

    private static LanguagePlan CreatePlan() =>
        new LanguageCompiler(new LanguagePackageRegistry().AddPackage(new WistLanguageFeaturePackage()))
            .Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.MinimalArithmeticId))
            .GetRequiredPlan();

    private static LanguageArtifactTransformationContext CreateContext(LanguagePlan plan, string source) =>
        new(
            plan,
            new LanguageExecutionRequest(source, Interpreter),
            new LanguageRuntimeOptions());

    private static IReadOnlyList<IFrontendCoreModule> CreateCanonicalModules(IFrontendCoreModule? extra = null)
    {
        var modules = new List<IFrontendCoreModule>
        {
            new WistProgramStructureFrontendModule(),
            new ArithmeticModuleImpl(),
            new NumbersModuleImpl(),
            new WhitespaceModuleImpl()
        };
        if (extra != null)
            modules.Add(extra);
        return modules;
    }

    private static IReadOnlyList<Func<IFrontendCoreModule>> CreateCanonicalModuleFactories(
        Func<IFrontendCoreModule>? extra = null)
    {
        var factories = new List<Func<IFrontendCoreModule>>
        {
            static () => new WistProgramStructureFrontendModule(),
            static () => new ArithmeticModuleImpl(),
            static () => new NumbersModuleImpl(),
            static () => new WhitespaceModuleImpl()
        };
        if (extra != null)
            factories.Add(extra);
        return factories;
    }

    private static string AirSignature(IAbstractIR air) =>
        string.Join("\n", air.Instructions.Select(static instruction => instruction.ToString()));

    private static string Escape(string value) => value
        .Replace("\r", "\\r", StringComparison.Ordinal)
        .Replace("\n", "\\n", StringComparison.Ordinal);

    private sealed record DirectSnapshot(
        WistSyntaxArtifact Syntax,
        WistBytecodeArtifact Bytecode,
        WistAirArtifact Air);

    private sealed record CanonicalCoreSnapshot(Bytecode Bytecode, IAbstractIR Air);

    private sealed class AstSpanCapture
    {
        public string Signature { get; set; } = string.Empty;
    }

    private sealed class AstSpanCaptureModule(AstSpanCapture capture) : IFrontendCoreModule
    {
        public AstNode ProcessAst(AstNode astRoot)
        {
            capture.Signature = string.Join(
                "|",
                Flatten(astRoot)
                    .Select(static node => node.LexemeValue)
                    .Where(static lexeme => lexeme != null)
                    .Select(static lexeme => $"{lexeme!.Text}@{lexeme.LineNumber}:{lexeme.CharNumber}"));
            return astRoot;
        }

        private static IEnumerable<AstNode> Flatten(AstNode root)
        {
            yield return root;
            foreach (var child in root.Children)
            foreach (var nested in Flatten(child))
                yield return nested;
        }
    }

    private sealed class SessionMarkerModule : IFrontendCoreModule;
}
