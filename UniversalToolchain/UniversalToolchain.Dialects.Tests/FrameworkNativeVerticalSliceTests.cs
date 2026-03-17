using AbstractIrConverters;
using BasicCodeTranslator;
using BasicCore.Core;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using BasicLexer.Core;
using BasicParser.Core;
using CommonExceptions;
using UniversalIntermediateRepresentation;
using BasicCore.Compilation;
using UniversalToolchain.Dialects.Frontend;

using UniversalToolchain.Dialects.Abstractions;
namespace UniversalToolchain.Dialects.Tests;

public class FrameworkNativeVerticalSliceTests
{
    [Test]
    public void Compile_ValidDialectHeader_ProducesDialectName()
    {
        var compiler = new DialectDslCompiler();

        var result = compiler.Compile("dialect Tiny\n");

        Assert.That(result.Name, Is.EqualTo("Tiny"));
    }

    [Test]
    public void Compile_OrderingAndBackendDirectives_AreCaptured()
    {
        var compiler = new DialectDslCompiler();

        var result = compiler.Compile(
            """
            dialect Tiny
            use Arithmetic
            exclude Loops
            requires Arithmetic -> Variables
            before Variables -> Scopes
            after Labels -> Loops
            backend interpreter enable
            backend cil disable
            """);

        Assert.Multiple(() =>
        {
            Assert.That(result.OrderDirectives.Count, Is.EqualTo(3));
            Assert.That(result.OrderDirectives[0].Directive, Is.EqualTo("requires"));
            Assert.That(result.OrderDirectives[1].Directive, Is.EqualTo("before"));
            Assert.That(result.OrderDirectives[2].Directive, Is.EqualTo("after"));
            Assert.That(result.BackendDirectives.Select(x => (x.Backend, x.Enabled)).ToArray(), Is.EqualTo(new[]
            {
                (DialectBackendTarget.Interpreter, true),
                (DialectBackendTarget.Cil, false)
            }));
        });
    }

    [Test]
    public void Compile_IntrinsicOptimizerSecurityCapability_AreCaptured()
    {
        var compiler = new DialectDslCompiler();

        var result = compiler.Compile(
            """
            dialect Tiny
            allow intrinsic "add_i32" for any
            forbid intrinsic "unsafe" for cil
            enable optimizer Ssa for interpreter
            disable optimizer Fold for any
            security restricted
            capability sandbox = true
            capability unsafeInterop = false
            """);

        Assert.Multiple(() =>
        {
            Assert.That(result.IntrinsicDirectives.Select(x => (x.Name, x.Allowed, x.Target)).ToArray(), Is.EqualTo(new[]
            {
                ("add_i32", true, DialectBackendTarget.Any),
                ("unsafe", false, DialectBackendTarget.Cil)
            }));
            Assert.That(result.OptimizerDirectives.Select(x => (x.Name, x.Enabled, x.Target)).ToArray(), Is.EqualTo(new[]
            {
                ("Ssa", true, DialectBackendTarget.Interpreter),
                ("Fold", false, DialectBackendTarget.Any)
            }));
            Assert.That(result.SecurityProfile, Is.EqualTo(DialectSecurityProfile.Restricted));
            Assert.That(result.CapabilityDirectives.Select(x => (x.Name, x.Value)).ToArray(), Is.EqualTo(new[]
            {
                ("sandbox", true),
                ("unsafeInterop", false)
            }));
        });
    }

    [Test]
    public void Compile_InvalidSyntax_ThrowsParserExceptionWithMessage()
    {
        var compiler = new DialectDslCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile("dialect\nuse A\n"));

        Assert.That(ex!.Message, Does.Contain("dialect <Name>"));
    }

    [Test]
    public void Compile_InvalidNewDirectiveSyntax_ThrowsParserException()
    {
        var compiler = new DialectDslCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile(
            """
            dialect Tiny
            allow intrinsic add_i32 for any
            """));

        Assert.That(ex!.Message, Does.Contain("allow|forbid intrinsic \"name\""));
    }

    [Test]
    public void Compile_DuplicateSecurityDirective_ThrowsParserException()
    {
        var compiler = new DialectDslCompiler();

        var ex = Assert.Throws<ParserException>(() => compiler.Compile(
            """
            dialect Tiny
            security trusted
            security restricted
            """));

        Assert.That(ex!.Message, Does.Contain("only once"));
    }

    [Test]
    public void Compile_IsDeterministic_ForSameInput()
    {
        var compiler = new DialectDslCompiler();
        const string source =
            """
            dialect Tiny
            use Variables
            requires Variables -> Scopes
            backend interpreter enable
            allow intrinsic "add_i32" for any
            enable optimizer Ssa for interpreter
            security trusted
            capability sandbox = true
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
    public void SliceCompiler_UsesPipelineTokenContext_WithoutFallbackRelexing()
    {
        var compiler = new DialectDefinitionSliceCompiler();
        var lexer = new BasicLexerImpl(new LexerConfiguration([]));
        lexer.AddLexemes(DialectDslLexemeRegistry.Registrations);
        var tokens = lexer.Lexemize("dialect FromContext\nuse Arithmetic\n");

        DialectCompilationTokenContext.Set(tokens);

        var result = compiler.Compile(
            new AbstractIR(),
            new CompilationInput
            {
                SourceText = "%%% this is not valid dialect text %%%"
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("FromContext"));
            Assert.That(result.UseModules, Is.EqualTo(new[] { "Arithmetic" }));
        });
    }

    [Test]
    public void SliceCompiler_BuildsOutputFromCompilationInputWithoutSharedContext()
    {
        var compiler = new DialectDefinitionSliceCompiler();
        var input = new CompilationInput
        {
            SourceText =
                """
                dialect Tiny
                use Arithmetic
                backend interpreter enable
                """
        };

        var result = compiler.Compile(new AbstractIR(), input);

        Assert.Multiple(() =>
        {
            Assert.That(result.Name, Is.EqualTo("Tiny"));
            Assert.That(result.UseModules, Is.EqualTo(new[] { "Arithmetic" }));
            Assert.That(result.BackendDirectives.Select(x => (x.Backend, x.Enabled)),
                Is.EqualTo(new[] { (DialectBackendTarget.Interpreter, true) }));
        });
    }


    [Test]
    public void Compile_RequiresFrontendModulePipeline_WithoutModuleLexingFails()
    {
        var coreWithoutDialectModule = new BasicCoreImpl<DialectDefinitionSlice>(
            () => new BasicLexerImpl(new LexerConfiguration([])),
            () => new BasicParserImpl(new ParserConfiguration([])),
            () => new BasicAstToBytecodeTranslatorImpl(new BytecodeTranslatorConfiguration([])),
            () => new BytecodeToAbstractIrConverterImpl(),
            () => new DialectDefinitionSliceCompiler(),
            () => new DialectDefinitionSliceExecutor(),
            [],
            [],
            []);

        Assert.Throws<LexerException>(() => coreWithoutDialectModule.GetExecutable("dialect Tiny\n"));
    }
}
