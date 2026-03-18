using System.Reflection;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Integration;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Tests;

public class FrameworkNativeExtensionSeamsTests
{
    [Test]
    public void DirectiveFeatures_AreDiscoveredDeterministically_AndDriveParserRegistrations()
    {
        var first = DialectDslFeatureCatalog.Features.Select(x => (x.Kind, x.Keyword, x.ParserPriority, x.GetType().Name)).ToArray();
        var second = DialectDslFeatureCatalog.Features.Select(x => (x.Kind, x.Keyword, x.ParserPriority, x.GetType().Name)).ToArray();
        var parserCreatorTypes = DialectDslParserNodeRegistry.Registrations.Select(x => x.Creator.GetType().Name).ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first.Select(x => x.Keyword), Is.EqualTo(new[]
            {
                "use", "exclude", "requires", "before", "after", "backend", "allow", "forbid", "enable", "disable", "security", "capability"
            }));
            Assert.That(parserCreatorTypes, Does.Contain(nameof(IdentifierListDialectDirectiveNodeCreator)));
            Assert.That(parserCreatorTypes, Does.Contain(nameof(SingleIdentifierDialectDirectiveNodeCreator)));
        });
    }

    [Test]
    public void SemanticBinder_BindsCompiledSliceIntoNormalizedDialectDefinition()
    {
        var slice = new DialectDslCompiler().Compile(
            """
            dialect Tiny
            use B,A
            exclude Legacy
            before A,B
            backend interpreter,cil
            allow add_i32
            forbid sub_i32
            enable Ssa
            disable Fold
            capability sandbox
            """);

        var diagnostics = new List<DialectDiagnostic>();
        var definition = DialectDefinitionSemanticBinder.Bind(slice, diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(definition.Name, Is.EqualTo("Tiny"));
            Assert.That(definition.ModulePolicy.IncludedModules, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(definition.ModulePolicy.ExcludedModules, Is.EqualTo(new[] { "Legacy" }));
            Assert.That(definition.BackendPolicy.EnabledBackends, Is.EqualTo(new[] { DialectBackendTarget.Interpreter, DialectBackendTarget.Cil }));
            Assert.That(definition.IntrinsicPolicy.AllowedIntrinsics, Is.EqualTo(new[] { "add_i32" }));
            Assert.That(definition.IntrinsicPolicy.ForbiddenIntrinsics, Is.EqualTo(new[] { "sub_i32" }));
            Assert.That(definition.OptimizerPolicy.EnabledOptimizers, Is.EqualTo(new[] { "Ssa" }));
            Assert.That(definition.OptimizerPolicy.DisabledOptimizers, Is.EqualTo(new[] { "Fold" }));
            Assert.That(definition.OrderRules.Select(x => (x.Kind, x.ModuleName, x.RelatedModuleName)), Is.EqualTo(new[]
            {
                (OrderRuleKind.Before, "A", "B")
            }));
        });
    }

    [Test]
    public void RuntimeDescriptorFactory_BuildsDeterministically_FromProviders()
    {
        var first = DialectRuntimeDescriptorRegistryFactory.BuildFromAssemblies(Assembly.GetExecutingAssembly());
        var second = DialectRuntimeDescriptorRegistryFactory.BuildFromAssemblies(Assembly.GetExecutingAssembly());

        Assert.Multiple(() =>
        {
            Assert.That(first.Modules.Keys, Is.EqualTo(second.Modules.Keys));
            Assert.That(first.Backends.Keys, Is.EqualTo(second.Backends.Keys));
            Assert.That(first.Optimizers.Keys, Is.EqualTo(second.Optimizers.Keys));
            Assert.That(first.Intrinsics.Keys, Is.EqualTo(second.Intrinsics.Keys));
            Assert.That(first.Modules.Keys, Does.Contain("DemoFrontend"));
            Assert.That(first.Optimizers.Keys, Does.Contain("DemoOptimizer"));
            Assert.That(first.Intrinsics.Keys, Does.Contain(("add_i32", DialectBackendTarget.Any)));
            Assert.That(first.Backends.Keys, Does.Contain(DialectBackendTarget.Interpreter));
        });
    }

    [Test]
    public void BuildPlanBuilder_ProjectsParserSemanticModel_WithScopedTargets()
    {
        var document = new DialectSyntaxDocument(
            "Scoped",
            null,
            ["A"],
            [],
            [],
            [new BackendDirectiveSyntax(DialectBackendTarget.Interpreter, true)],
            [new IntrinsicDirectiveSyntax("add_i32", true, DialectBackendTarget.Interpreter)],
            [new OptimizerDirectiveSyntax("Ssa", true, DialectBackendTarget.Any)],
            null,
            []);

        var plan = new DialectBuildPlanBuilder().Build(document);

        Assert.Multiple(() =>
        {
            Assert.That(plan.CanBuild, Is.True);
            Assert.That(plan.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)), Is.EqualTo(new[]
            {
                ("add_i32", true, DialectBackendTarget.Interpreter)
            }));
            Assert.That(plan.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)), Is.EqualTo(new[]
            {
                ("Ssa", true, DialectBackendTarget.Any)
            }));
        });
    }

    private sealed class DemoRuntimeDescriptorProvider : IDialectRuntimeDescriptorProvider
    {
        public int Order => 10;

        public void Register(DialectRuntimeDescriptorRegistryBuilder builder)
        {
            builder
                .RegisterModule(new RuntimeModuleDescriptor("DemoFrontend", typeof(FakeFrontendModule)))
                .RegisterBackend(new RuntimeBackendDescriptor(DialectBackendTarget.Interpreter, "InterpreterBackend"))
                .RegisterOptimizer(new RuntimeOptimizerDescriptor("DemoOptimizer", typeof(FakeOptimizerModule)))
                .RegisterIntrinsic(new RuntimeIntrinsicDescriptor("add_i32", DialectBackendTarget.Any));
        }
    }

    private sealed class FakeFrontendModule : BasicCore.Contracts.IFrontendCoreModule
    {
        public void InitLexer(BasicCore.LexerWrapper.ILexer lexer)
        {
        }

        public void InitParser(BasicCore.ParserWrapper.IParser parser)
        {
        }

        public BasicCore.ParserWrapper.AstNode ProcessAst(BasicCore.ParserWrapper.AstNode astRoot)
        {
            return astRoot;
        }

        public void InitAstTranslator(BasicCore.TranslatorWrapper.IAstToBytecodeTranslator translator)
        {
        }
    }

    private sealed class FakeOptimizerModule : BasicCore.Contracts.IIRProcessingModule
    {
        public IntermediateRepresentationAbstractions.IAbstractIR Process(IntermediateRepresentationAbstractions.IAbstractIR air)
        {
            return air;
        }
    }
}
