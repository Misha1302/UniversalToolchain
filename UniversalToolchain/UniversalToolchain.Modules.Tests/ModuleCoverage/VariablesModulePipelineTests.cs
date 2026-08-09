using AssemblyFinder;
using BasicCore.Binding;
using BasicCore.Binding.Symbols;
using BasicCore.Compilation;
using BasicCore.Core;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Semantics;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using IntermediateRepresentationAbstractions;
using NumbersModule.Core;
using UniversalToolchain.Wist;
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
        h.AssertFailsContaining(
            "unknownVariable",
            _modules,
            "Unknown identifier 'unknownVariable'");
    }

    [TestCase("let value: System.DateTime = 1\nvalue", "Unknown declared type 'System.DateTime'")]
    public void Variables_UnapprovedDeclaredType_FailsClosed(string source, string expectedMessage)
    {
        using var h = new ModulePipelineTestHelper();

        h.AssertFailsContaining(source, _modules, expectedMessage);
    }

    [Test]
    public void Variables_AssemblyQualifiedDeclaredType_IsRejectedByBinderPolicy()
    {
        var variableNode = CreateVariableNode("value");
        variableNode.AddTag("VariableDefinition");
        variableNode.AddTag("VariableDefinitionWithType");
        variableNode.Children.Add(new AstNode(
            ExtensibleEnum<AstNodeTag>.CreateOrGet("Type"),
            new LexemeValue("System.Int32, System.Private.CoreLib", null, -1, null),
            []));

        var ruleType = typeof(VariablesVisitor).Assembly.GetType(
            "VariablesModule.VariablesBindingRule",
            throwOnError: true)!;
        var rule = Activator.CreateInstance(ruleType, nonPublic: true)!;
        var bind = ruleType.GetMethod("Bind")!;
        var invocation = Assert.Throws<System.Reflection.TargetInvocationException>(() =>
            bind.Invoke(rule, [variableNode, new BindingContext([]), (Func<AstNode, AstNode>)(static node => node)]));

        Assert.That(invocation!.InnerException, Is.TypeOf<InvalidOperationException>());
        Assert.That(invocation.InnerException!.Message, Does.Contain("Assembly-qualified declared type"));
    }

    [Test]
    public void Variables_IndependentRuns_DoNotLeakStateBetweenCalls()
    {
        using var h = new ModulePipelineTestHelper();
        var dialect = h.BuildDialectText("VariableIsolation", _modules, backends: ["interpreter"]);
        var options = WistEngineOptions.FromDialectText(dialect, "variable-isolation-test");
        options.BackendId = "interpreter";
        options.AllowedAssemblies = [typeof(int).Assembly, typeof(VariablesModulePipelineTests).Assembly];
        using var engine = WistEngine.Create(options);

        var first = engine.Evaluate<object?>("let x = 7\nx");
        var secondRunException = Assert.Catch<Exception>(() => engine.Evaluate<object?>("x"));

        Assert.That(ModulePipelineTestHelper.AsNumber(first), Is.EqualTo(7));
        Assert.That(secondRunException, Is.Not.Null);
    }

    [Test]
    public void Variables_ExternalVariableExecutesAndBindingContextKeepsConstantIdentityDistinct()
    {
        using var h = new ModulePipelineTestHelper();
        var dialect = h.BuildDialectText("ExternalVariables", _modules, backends: ["interpreter"]);
        var options = WistEngineOptions.FromDialectText(dialect, "external-variable-test");
        options.BackendId = "interpreter";
        options.AllowedAssemblies = [typeof(int).Assembly, typeof(VariablesModulePipelineTests).Assembly];
        using var engine = WistEngine.Create(options);

        var result = engine.Evaluate<object?>("external + immutable", new Dictionary<string, object?>
        {
            ["external"] = 10.0,
            ["immutable"] = 5.0
        });

        var bindings = new ExternalBinding[]
        {
            new() { Name = "external", Type = typeof(RealNumberImpl), Kind = ExternalBindingKind.Variable },
            new() { Name = "immutable", Type = typeof(RealNumberImpl), Value = RealNumberImpl.Create(5), Kind = ExternalBindingKind.Constant }
        };
        var bindingContext = new BindingContext(bindings);

        Assert.Multiple(() =>
        {
            Assert.That(ModulePipelineTestHelper.AsNumber(result), Is.EqualTo(15));
            Assert.That(bindingContext.TryGetExternal("external", out var variable), Is.True);
            Assert.That(variable, Is.TypeOf<ExternalVariableSymbol>());
            Assert.That(bindingContext.TryGetExternal("immutable", out var constant), Is.True);
            Assert.That(constant, Is.TypeOf<ExternalConstantSymbol>());
        });
    }

    [Test]
    public void Variables_WriteTargetTypeInference_Path_IsUsed_ForAssignment()
    {
        var variableSource = CreateVariableNode("x");
        variableSource.AddSemanticTag(AssignmentSemanticContractIds.WriteTarget);

        var visitor = new VariablesVisitor(CreateTypeCatalog());
        var bytecode = new Bytecode([]);
        visitor.TryVisit(new BytecodeVisitorData(new PassthroughAstTranslator(), bytecode, variableSource));

        var referenceOp = GetSingleOp(bytecode, 0);
        Assert.That(referenceOp.Name, Is.EqualTo("InferWriteTypeOfLocalVar_x"));
    }

    [Test]
    public void Variables_PreprocessorDefineLexeme_RegistersTypeDeterministically()
    {
        var visitor = new VariablesVisitor(CreateTypeCatalog());
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

        Assert.That(ir.Instructions.Any(static instruction =>
            CSharpCallIntrinsicReader.TryGetCallMethod(instruction, out var method) &&
            (method.ReturnType == typeof(int) || method.GetGenericArguments().Contains(typeof(int)))), Is.True);
    }

    [Test]
    public void Variables_ObjectBinding_IsRefinedToConcreteType_OnFirstValidContext()
    {
        var visitor = new VariablesVisitor(CreateTypeCatalog());
        var bytecode = new Bytecode([]);

        var source = CreateVariableNode("ext");
        source.AddSemanticTag(AssignmentSemanticContractIds.WriteTarget);
        var boundSettable = new BoundExternalReference(source, new ExternalVariableSymbol("ext", typeof(object), 0));

        visitor.TryVisit(new BytecodeVisitorData(new PassthroughAstTranslator(), bytecode, boundSettable));
        var refOp = GetSingleOp(bytecode, 0);
        _ = refOp.GetAbstractIR(new IAbstractMethodConvertable.Context([typeof(int)]));

        var readSource = CreateVariableNode("ext");
        var boundRead = new BoundExternalReference(readSource, new ExternalVariableSymbol("ext", typeof(object), 0));
        visitor.TryVisit(new BytecodeVisitorData(new PassthroughAstTranslator(), bytecode, boundRead));

        var loadOp = GetSingleOp(bytecode, 1);
        var ir = loadOp.GetAbstractIR(new IAbstractMethodConvertable.Context([]));

        Assert.That(ir.Instructions.Any(static instruction =>
            CSharpCallIntrinsicReader.TryGetCallMethod(instruction, out var method) &&
            (method.ReturnType == typeof(int) || method.GetGenericArguments().Contains(typeof(int)))), Is.True);
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

    private static ITypeCatalog CreateTypeCatalog() =>
        TypeCatalogFactory.Create([typeof(int).Assembly, typeof(VariablesModulePipelineTests).Assembly]);

    private sealed class PassthroughAstTranslator : IAstToBytecodeTranslator
    {
        public BytecodeTranslatorConfiguration Configuration { get; } = new([]);

        public Bytecode Translate(AstNode root) => new([]);
    }
}
