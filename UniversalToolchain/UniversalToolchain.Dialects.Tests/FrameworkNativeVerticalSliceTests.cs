using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.Compilation;
using BasicCore.Core;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using CommonExceptions;
using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class FrameworkNativeVerticalSliceTests
{
    [Test]
    public void Compile_FullValidDialect_ProducesExpectedSlice()
    {
        var compiler = DialectDslTestComposition.CreateCompiler();

        var result = compiler.Compile(
            """

            dialect Tiny

            use Arithmetic,Variables,Scopes
            exclude Legacy
            requires Arithmetic,Variables,Scopes
            before Arithmetic,Variables
            after Variables,Scopes
            backend interpreter,cil
            allow add_i32
            forbid unsafe_reflect
            enable Ssa
            disable Fold
            security restricted
            capability sandbox,safeInterop

            """);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Tiny"));
            Assert.That(result.UseModules, Is.EqualTo(new[] { "Arithmetic", "Variables", "Scopes" }));
            Assert.That(result.ExcludeModules, Is.EqualTo(new[] { "Legacy" }));
            Assert.That(result.OrderDirectives.Select(x => (x.Directive, x.SourceModule, x.TargetModule)), Is.EquivalentTo(new[]
            {
                ("requires", "Arithmetic", "Variables"),
                ("requires", "Variables", "Scopes"),
                ("before", "Arithmetic", "Variables"),
                ("after", "Variables", "Scopes")
            }));
            Assert.That(result.BackendDirectives.Select(x => (x.Backend, x.Enabled)), Is.EqualTo(new[]
            {
                (TestBackendIds.Interpreter, true),
                (TestBackendIds.Cil, true)
            }));
            Assert.That(result.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)), Is.EqualTo(new[]
            {
                ("add_i32", true, TestBackendIds.Any),
                ("unsafe_reflect", false, TestBackendIds.Any)
            }));
            Assert.That(result.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)), Is.EqualTo(new[]
            {
                ("Ssa", true, TestBackendIds.Any),
                ("Fold", false, TestBackendIds.Any)
            }));
            Assert.That(result.SecurityProfile, Is.EqualTo(DialectSecurityProfile.Restricted));
            Assert.That(result.CapabilityDirectives.Select(x => (x.Name, x.Value)), Is.EqualTo(new[]
            {
                ("sandbox", true),
                ("safeInterop", true)
            }));
        });
    }

    [Test]
    public void Compile_IsDeterministic_ForSameInput()
    {
        var compiler = DialectDslTestComposition.CreateCompiler();
        const string source =
            """
            dialect Tiny
            use Variables,Scopes
            requires Variables,Scopes
            backend interpreter
            allow add_i32
            enable Ssa
            security trusted
            capability sandbox
            """;

        var first = compiler.Compile(source);
        var second = compiler.Compile(source);

        Assert.Multiple(() =>
        {
            Assert.That(first.Name, Is.EqualTo(second.Name));
            Assert.That(first.UseModules, Is.EqualTo(second.UseModules));
            Assert.That(first.OrderDirectives.Select(x => (x.Directive, x.SourceModule, x.TargetModule)),
                Is.EqualTo(second.OrderDirectives.Select(x => (x.Directive, x.SourceModule, x.TargetModule))));
            Assert.That(first.BackendDirectives.Select(x => (x.Backend, x.Enabled)),
                Is.EqualTo(second.BackendDirectives.Select(x => (x.Backend, x.Enabled))));
            Assert.That(first.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)),
                Is.EqualTo(second.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target))));
            Assert.That(first.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)),
                Is.EqualTo(second.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target))));
            Assert.That(first.SecurityProfile, Is.EqualTo(second.SecurityProfile));
            Assert.That(first.CapabilityDirectives.Select(x => (x.Name, x.Value)),
                Is.EqualTo(second.CapabilityDirectives.Select(x => (x.Name, x.Value))));
        });
    }

    [Test]
    public void Compile_MissingDialectDeclaration_ThrowsParserException()
    {
        var compiler = DialectDslTestComposition.CreateCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("use A\n"));

        Assert.That(ex!.Message, Does.Contain("dialect"));
    }

    [Test]
    public void Compile_MalformedIdentifierList_ThrowsParserException()
    {
        var compiler = DialectDslTestComposition.CreateCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("dialect Tiny\nuse A,,B\n"));

        Assert.That(ex!.Message, Does.Contain("invalid identifier list item"));
    }

    [Test]
    public void Compile_UnknownDirective_ThrowsParserException()
    {
        var compiler = DialectDslTestComposition.CreateCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("dialect Tiny\nwat unknown\n"));

        Assert.That(ex!.Message, Does.Contain("Unknown dialect directive 'wat'"));
    }

    [Test]
    public void Compile_DuplicateSecurityDirective_ThrowsParserException()
    {
        var compiler = DialectDslTestComposition.CreateCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("dialect Tiny\nsecurity trusted\nsecurity restricted\n"));

        Assert.That(ex!.Message, Does.Contain("only be declared once"));
    }

    [Test]
    public void Compile_ConflictingIntrinsicPolicies_ThrowParserException()
    {
        var compiler = DialectDslTestComposition.CreateCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("dialect Tiny\nallow add_i32\nforbid add_i32\n"));

        Assert.That(ex!.Message, Does.Contain("cannot be both allowed and forbidden"));
    }

    [Test]
    public void SliceCompiler_RejectsMissingDialectNameAnnotation()
    {
        var compiler = new DialectDefinitionSliceCompiler();
        var ir = new AbstractIR();
        ir.AppendInstructions([new Instruction(UOpCode.Annotate, metadata: [new UseModulesAirAnnotation(new[] { "Arithmetic" })])]);

        var ex = Assert.Throws<InvalidOperationException>(() => compiler.Compile(ir, new CompilationInput { SourceText = "ignored" }));

        Assert.That(ex!.Message, Does.Contain("missing a DialectNameAirAnnotation"));
    }

    [Test]
    public void SliceCompiler_RejectsDuplicateSingletonAnnotation()
    {
        var compiler = new DialectDefinitionSliceCompiler();
        var ir = new AbstractIR();
        ir.AppendInstructions([
            new Instruction(UOpCode.Annotate, metadata:
            [
                new DialectNameAirAnnotation("Tiny"),
                new SecurityAirAnnotation(DialectSecurityProfile.Trusted),
                new SecurityAirAnnotation(DialectSecurityProfile.Restricted)
            ])
        ]);

        var ex = Assert.Throws<InvalidOperationException>(() => compiler.Compile(ir, new CompilationInput { SourceText = "ignored" }));

        Assert.That(ex!.Message, Does.Contain("duplicate singleton annotation"));
    }

    [Test]
    public void SliceCompiler_RejectsDuplicateDialectNameAnnotation()
    {
        var compiler = new DialectDefinitionSliceCompiler();
        var ir = new AbstractIR();
        ir.AppendInstructions([
            new Instruction(UOpCode.Annotate, metadata:
            [
                new DialectNameAirAnnotation("Tiny"),
                new DialectNameAirAnnotation("Other")
            ])
        ]);

        var ex = Assert.Throws<InvalidOperationException>(() => compiler.Compile(ir, new CompilationInput { SourceText = "ignored" }));

        Assert.That(ex!.Message, Does.Contain("duplicate DialectNameAirAnnotation"));
    }

    [Test]
    public void Compile_RequiresFrontendModulePipeline_WithoutModuleLexingFails()
    {
        var coreWithoutDialectModule = new BasicCoreImpl<DialectDefinitionSlice>(
            () => new BasicLexerImpl(new LexerConfiguration([])),
            () => new BasicParserImpl(new ParserConfiguration([])),
            () => new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([])),
            DialectDslTestSupport.CreateAbstractMethodsTranslator,
            () => new DialectDefinitionSliceCompiler(),
            () => new DialectDefinitionSliceExecutor(),
            [],
            [],
            []);

        Assert.Throws<LexerException>(() => coreWithoutDialectModule.GetExecutable("dialect Tiny\n"));
    }
}
