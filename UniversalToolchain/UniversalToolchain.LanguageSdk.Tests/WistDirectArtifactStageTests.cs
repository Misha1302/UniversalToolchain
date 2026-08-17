using BasicCore.Contracts;
using BasicCore.Core;
using BasicCore.ParserWrapper;
using BasicTypesExtensions;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.Wist.LanguagePack;

namespace UniversalToolchain.LanguageSdk.Tests;

[TestFixture]
public sealed class WistDirectArtifactStageTests
{
    private static readonly BackendId Interpreter = new("interpreter");

    [Test]
    public void CanonicalRuntime_ExecutesArithmeticThroughNativeSemanticPath()
    {
        var package = new WistLanguageFeaturePackage();
        var plan = CreatePlan(package);
        using var runtime = LanguageRuntime.Create(plan, new ILanguageRouteComponentSource[] { package });

        var result = runtime.Run(new LanguageExecutionRequest("2 + 3 * 4", Interpreter));

        Assert.That(result.Value, Is.EqualTo(14d));
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
    }

    [Test]
    public void SemanticNormalization_SnapshotsMeaningWithoutRetainingMutableAst()
    {
        var source = new AstNode(ExtensibleEnum<AstNodeTag>.CreateOrGet("Addition"), null, []);
        var program = WistSemanticNormalizer.Normalize(source);

        source.NodeType = ExtensibleEnum<AstNodeTag>.CreateOrGet("Subtraction");

        var operation = program.Root as WistSemanticOperationNode;
        Assert.Multiple(() =>
        {
            Assert.That(operation, Is.Not.Null);
            Assert.That(operation!.Operation, Is.EqualTo(WistSemanticOperations.Add));
            Assert.That(
                typeof(WistSemanticNode).Assembly
                    .GetTypes()
                    .Where(static type => type.Namespace == typeof(WistSemanticNode).Namespace)
                    .Where(static type => typeof(WistSemanticNode).IsAssignableFrom(type))
                    .SelectMany(static type => type.GetProperties())
                    .Any(static property => typeof(AstNode).IsAssignableFrom(property.PropertyType)),
                Is.False,
                "Semantic nodes must not expose live mutable AST nodes.");
        });
    }

    [Test]
    public void SemanticProgramGraph_DoesNotRetainSyntaxBindingOrLegacyLoweringObjects()
    {
        var semanticTypes = typeof(WistSemanticProgram).Assembly
            .GetTypes()
            .Where(static type => type == typeof(WistSemanticProgram)
                                  || typeof(WistSemanticNode).IsAssignableFrom(type))
            .ToArray();

        var violations = semanticTypes
            .SelectMany(static type => type
                .GetFields(System.Reflection.BindingFlags.Instance
                           | System.Reflection.BindingFlags.Public
                           | System.Reflection.BindingFlags.NonPublic)
                .Select(field => $"{type.Name}.{field.Name}: {field.FieldType.FullName}"))
            .Where(static description =>
                description.Contains("AstNode", StringComparison.Ordinal)
                || description.Contains("BasicCore.Binding", StringComparison.Ordinal)
                || description.Contains("IAstVisitor", StringComparison.Ordinal)
                || description.Contains("IFrontendCoreModule", StringComparison.Ordinal)
                || description.Contains("IAstToBytecodeTranslator", StringComparison.Ordinal))
            .ToArray();

        Assert.That(
            violations,
            Is.Empty,
            "The semantic artifact must be a data-only ownership boundary; forbidden retained members:" +
            Environment.NewLine + string.Join(Environment.NewLine, violations));
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
            Assert.That(
                forbidden.Any(type => type.IsAssignableFrom(property.PropertyType)),
                Is.False,
                $"{artifact.Name}.{property.Name} exposes an executable component.");
            if (property.PropertyType.IsGenericType)
            {
                Assert.That(
                    property.PropertyType.GetGenericArguments()
                        .Any(argument => forbidden.Any(type => type.IsAssignableFrom(argument))),
                    Is.False,
                    $"{artifact.Name}.{property.Name} exposes an executable component collection.");
            }
        }
    }

    [Test]
    public void SemanticArtifact_ContainsSemanticProgramAndNoAstPayload()
    {
        var properties = typeof(WistSemanticArtifact).GetProperties();

        Assert.Multiple(() =>
        {
            Assert.That(properties.Any(static property => property.PropertyType == typeof(WistSemanticProgram)), Is.True);
            Assert.That(properties.Any(static property => typeof(AstNode).IsAssignableFrom(property.PropertyType)), Is.False);
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

    private static LanguagePlan CreatePlan(WistLanguageFeaturePackage package) =>
        new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(WistLanguageDefinitions.Create(WistLanguageDefinitions.MinimalArithmeticId))
            .GetRequiredPlan();
}
