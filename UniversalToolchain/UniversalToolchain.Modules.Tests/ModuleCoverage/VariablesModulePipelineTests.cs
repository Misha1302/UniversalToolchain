using BasicCore.Binding;
using BasicCore.Binding.Symbols;
using BasicCore.Compilation;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;
using NumbersModule.Core;
using VariablesModule;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public class VariablesModulePipelineTests
{
    private static readonly string[] _modules = ModulePipelineTestHelper.FullUniversalModules;

    [Test]
    public void Variables_LocalAssignRead_MultipleAssignments_AndArithmetic_AreDeterministic()
    {
        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth(
            """
            let x = 1
            x = x + 2
            x = x * 3
            x + 4
            """,
            _modules);

        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(13));
    }

    [Test]
    public void Variables_UnknownVariable_FailsDeterministically()
    {
        using var h = new ModulePipelineTestHelper();
        h.AssertFailsContaining("unknownVariable", _modules, string.Empty);
    }

    [Test]
    public void Variables_IndependentRuns_DoNotLeakStateBetweenCalls()
    {
        using var h = new ModulePipelineTestHelper();
        using var host = h.CreateHost(_modules, backends: ["interpreter"]);
        var core = host.GetCore("interpreter");

        var first = core.Run("let x = 7\nx");
        var secondRunException = Assert.Catch<Exception>(() => core.Run("x"));

        Assert.That(ModulePipelineTestHelper.AsNumber(first), Is.EqualTo(7));
        Assert.That(secondRunException, Is.Not.Null);
    }

    [Test]
    public void Variables_BoundExternalVariableAndConstant_WorkViaDeclaredBindingsAndSession()
    {
        using var h = new ModulePipelineTestHelper();
        using var host = h.CreateHost(_modules, backends: ["interpreter"]);
        var interpreterCompiler = host.GetArtifactCompiler<IAbstractIR>("interpreter");

        var artifact = interpreterCompiler.Compile(new CompilationInput
        {
            SourceText = "external + immutable",
            ExternalBindings =
            [
                new ExternalBinding { Name = "external", Type = typeof(RealNumberImpl), Kind = ExternalBindingKind.Variable },
                new ExternalBinding { Name = "immutable", Type = typeof(RealNumberImpl), Value = RealNumberImpl.Create(5), Kind = ExternalBindingKind.Constant }
            ]
        });

        var session = artifact.CreateSession();
        session.SetArgument("external", RealNumberImpl.Create(10));
        var result = session.Run();

        Assert.That(ModulePipelineTestHelper.AsNumber(result), Is.EqualTo(15));
        Assert.Throws<InvalidOperationException>(() => session.SetArgument("immutable", RealNumberImpl.Create(99)));
    }

    [Test]
    public void Variables_ExpectingWriteTypeInference_Path_IsUsed_ForAssignment()
    {
        var variableSource = CreateVariableNode("x");
        variableSource.AddTag("ExpectingWriteTypeInference");

        var visitor = new VariablesVisitor();
        var bytecode = new Bytecode([]);
        visitor.TryVisit(new BytecodeVisitorData(new PassthroughAstTranslator(), bytecode, variableSource));

        var referenceOp = GetSingleOp(bytecode, 0);
        Assert.That(referenceOp.Name, Is.EqualTo("InferWriteTypeOfLocalVar_x"));

        using var h = new ModulePipelineTestHelper();
        var result = h.ExecuteBoth("let x = 3\nx = x + 2\nx", _modules);
        ModulePipelineTestHelper.AssertParity(result.Compiler, result.Interpreter);
        Assert.That(ModulePipelineTestHelper.AsNumber(result.Compiler), Is.EqualTo(5));
    }

    [Test]
    public void Variables_PreprocessorDefineLexeme_RegistersTypeDeterministically()
    {
        var visitor = new VariablesVisitor();
        var bytecode = new Bytecode([]);

        visitor.TryVisit(new BytecodeVisitorData(
            new PassthroughAstTranslator(),
            bytecode,
            CreatePreprocessorNode("#![define value as System.Int32]")));

        var defineOp = GetSingleOp(bytecode, 0);
        Assert.That(defineOp.Name, Is.EqualTo("DefineArgument_value_System.Int32"));

        visitor.TryVisit(new BytecodeVisitorData(
            new PassthroughAstTranslator(),
            bytecode,
            CreateVariableNode("value")));

        var loadOp = GetSingleOp(bytecode, 1);
        var ir = loadOp.GetAbstractIR(new IAbstractMethodConvertable.Context([]));

        Assert.That(ir.Instructions.SelectMany(static i => i.Operands).Any(static o => o?.ToString()?.Contains("Int32", StringComparison.Ordinal) == true), Is.True);
    }

    [Test]
    public void Variables_ObjectBinding_IsRefinedToConcreteType_OnFirstValidContext()
    {
        var visitor = new VariablesVisitor();
        var bytecode = new Bytecode([]);

        var source = CreateVariableNode("ext");
        source.AddTag("ExpectingWriteTypeInference");
        var boundSettable = new BoundExternalReference(source, new ExternalVariableSymbol("ext", typeof(object), 0));

        visitor.TryVisit(new BytecodeVisitorData(new PassthroughAstTranslator(), bytecode, boundSettable));
        var refOp = GetSingleOp(bytecode, 0);
        _ = refOp.GetAbstractIR(new IAbstractMethodConvertable.Context([typeof(int)]));

        var readSource = CreateVariableNode("ext");
        var boundRead = new BoundExternalReference(readSource, new ExternalVariableSymbol("ext", typeof(object), 0));
        visitor.TryVisit(new BytecodeVisitorData(new PassthroughAstTranslator(), bytecode, boundRead));

        var loadOp = GetSingleOp(bytecode, 1);
        var ir = loadOp.GetAbstractIR(new IAbstractMethodConvertable.Context([]));

        Assert.That(ir.Instructions.SelectMany(static i => i.Operands).Any(static o => o?.ToString()?.Contains("Int32", StringComparison.Ordinal) == true), Is.True);
    }

    private static AstNode CreateVariableNode(string name)
        => new(ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable"), new LexemeValue(name, null, -1, null), []);

    private static AstNode CreatePreprocessorNode(string text)
        => new(
            ExtensibleEnum<AstNodeTag>.CreateOrGet("Preprocessor lexeme"),
            new LexemeValue(text, new LexemePattern("", ExtensibleEnum<LexemeTag>.CreateOrGet("Preprocessor lexeme")), -1, null),
            []);

    private static IAbstractMethodConvertable GetSingleOp(Bytecode bytecode, int instructionIndex)
        => bytecode.Instructions[instructionIndex].Ops.Single().Value.Single();

    private sealed class PassthroughAstTranslator : IAstToBytecodeTranslator
    {
        public BytecodeTranslatorConfiguration Configuration { get; } = new([]);

        public Bytecode Translate(AstNode root) => new([]);
    }
}