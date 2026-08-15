using System.Collections.Concurrent;
using AbstractIrConverters;
using ArithmeticModule.Module;
using ArithmeticModule.Visitors;
using BasicCodeTranslator;
using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicLexer.Core;
using BasicParser.Core;
using BasicTypesExtensions;
using IntermediateRepresentationAbstractions;
using Microsoft.Extensions.DependencyInjection;
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
    public void DirectStages_UseExplicitSemanticBoundary_AndProduceAir()
    {
        const string source = "2 + 3 * 4";
        var plan = CreatePlan();
        var factory = CreateDirectFactory(CreateCanonicalModuleFactories(), plan);
        var context = CreateContext(plan, source);

        var syntax = factory.CreateFrontend().Transform(source, context);
        var semantic = factory.CreateSemanticBinding().Transform(syntax, context);
        var bytecode = factory.CreateBytecodeLowering().Transform(semantic, context);
        var air = factory.CreateAirLowering().Transform(bytecode, context);

        Assert.Multiple(() =>
        {
            Assert.That(semantic.Program.Root, Is.Not.Null);
            Assert.That(bytecode.Bytecode.Instructions, Is.Not.Empty);
            Assert.That(air.Air.Instructions, Is.Not.Empty);
            Assert.That(WistDirectArtifactKinds.Semantic.Contract, Is.EqualTo(WistArtifactKinds.SemanticProgramContract));
        });
    }

    [Test]
    public void SymbolicAndTextualAddition_NormalizeToSameCanonicalSemanticAdd()
    {
        var symbolic = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Addition"), null, []);
        var textual = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("TextualAddition"), null, []);

        var symbolicProgram = WistSemanticNormalizer.Normalize(symbolic);
        var textualProgram = WistSemanticNormalizer.Normalize(textual);
        var symbolicOperation = symbolicProgram.Root as WistSemanticOperationNode;
        var textualOperation = textualProgram.Root as WistSemanticOperationNode;

        Assert.Multiple(() =>
        {
            Assert.That(symbolicOperation, Is.Not.Null);
            Assert.That(textualOperation, Is.Not.Null);
            Assert.That(symbolicOperation!.Operation, Is.EqualTo(WistSemanticOperations.Add));
            Assert.That(textualOperation!.Operation, Is.EqualTo(WistSemanticOperations.Add));
            Assert.That(symbolicOperation.Operation, Is.EqualTo(textualOperation.Operation));
        });

        var projectedSymbolic = WistSemanticNormalizer.ProjectForLegacyLowering(symbolicProgram);
        var projectedTextual = WistSemanticNormalizer.ProjectForLegacyLowering(textualProgram);
        Assert.Multiple(() =>
        {
            Assert.That(projectedSymbolic.NodeType, Is.EqualTo(ArithmeticSemanticLowering.AddNodeType));
            Assert.That(projectedTextual.NodeType, Is.EqualTo(ArithmeticSemanticLowering.AddNodeType));
            Assert.That(projectedSymbolic.LexemeValue, Is.Null);
            Assert.That(projectedTextual.LexemeValue, Is.Null);
        });
    }

    [Test]
    public void DirectArtifacts_AreDataOnlyAcrossPhaseBoundaries()
    {
        var forbidden = new[] { typeof(IFrontendCoreModule), typeof(IAirOptimizer) };
        var artifacts = new[]
        {
            typeof(WistSyntaxArtifact),
            typeof(WistSemanticArtifact),
            typeof(WistBytecodeArtifact),
            typeof(WistAirArtifact)
        };

        foreach (var artifact in artifacts)
        foreach (var property in artifact.GetProperties())
        {
            Assert.That(forbidden.Any(type => type.IsAssignableFrom(property.PropertyType)), Is.False,
                $"{artifact.Name}.{property.Name} exposes an executable component.");
            if (property.PropertyType.IsGenericType)
            {
                Assert.That(property.PropertyType.GetGenericArguments().Any(argument => forbidden.Any(type => type.IsAssignableFrom(argument))), Is.False,
                    $"{artifact.Name}.{property.Name} exposes an executable component collection.");
            }
        }
    }

    [Test]
    public void DirectFrontend_ConcurrentTransformsOwnIndependentStageLocalModuleInstances()
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
            .Select(index => Task.Run(() => frontend.Transform($"{index} + 1", CreateContext(plan, $"{index} + 1"))))
            .ToArray();
        Task.WaitAll(tasks);

        Assert.Multiple(() =>
        {
            Assert.That(instances, Has.Count.EqualTo(24));
            Assert.That(instances.Distinct().Count(), Is.EqualTo(24));
            Assert.That(tasks.All(static task => task.Result.Root != null), Is.True);
        });
    }

    [Test]
    public void DirectArtifactContracts_AreDistinctAndOrderedByPhase()
    {
        Assert.Multiple(() =>
        {
            Assert.That(WistDirectArtifactKinds.Syntax.Contract, Is.EqualTo(WistArtifactKinds.SyntaxTreeContract));
            Assert.That(WistDirectArtifactKinds.Semantic.Contract, Is.EqualTo(WistArtifactKinds.SemanticProgramContract));
            Assert.That(WistDirectArtifactKinds.Bytecode.Contract, Is.EqualTo(WistArtifactKinds.BytecodeContract));
            Assert.That(WistDirectArtifactKinds.Air.Contract, Is.EqualTo(WistArtifactKinds.AirContract));
            Assert.That(new[]
            {
                WistDirectArtifactKinds.Syntax.Contract,
                WistDirectArtifactKinds.Semantic.Contract,
                WistDirectArtifactKinds.Bytecode.Contract,
                WistDirectArtifactKinds.Air.Contract
            }.Distinct().Count(), Is.EqualTo(4));
        });
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
        new(plan, new LanguageExecutionRequest(source, Interpreter), new LanguageRuntimeOptions());

    private static IReadOnlyList<Func<IFrontendCoreModule>> CreateCanonicalModuleFactories(Func<IFrontendCoreModule>? extra = null)
    {
        var factories = new List<Func<IFrontendCoreModule>>
        {
            static () => new ArithmeticModuleImpl(),
            static () => new NumbersModuleImpl(),
            static () => new WhitespaceModuleImpl()
        };
        if (extra != null)
            factories.Add(extra);
        return factories;
    }

    private sealed class SessionMarkerModule : IFrontendCoreModule;
}
