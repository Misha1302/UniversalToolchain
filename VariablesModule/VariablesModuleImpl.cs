// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com
using System.Reflection;
using AssemblyFinder;
using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;
using EqualityModule;
using ExceptionsManager;
using IlCodeGeneratorFactory;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicCore.LexerWrapper.LexemeTag>;

namespace VariablesModule;

public class VariablesModuleImpl : ICoreModule
{
    // TODO: split to module of variables and set/get values
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(":", LexemeType.CreateOrGet("Colon"))
        );

        lexer.Configuration.TryAddPattern(
            new LexemePattern("=", LexemeType.CreateOrGet("Equality"))
        );
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-1.5f, new VariablesNodeCreator());
        parser.Configuration.NodeCreators.Add(10f, new ValuesSetNodeCreator());
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new VariablesVisitor());
        translator.Configuration.Visitors.Add(new EqualityAstVisitor());
    }
}

public class VariablesVisitor : IAstVisitor
{
    private readonly Dictionary<string, Type> _variableTypes = [];

    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Variable"))
            return;

        var varName = data.Node.Text;

        if (data.Node.AllTags.Contains("VariableDefinition"))
            _variableTypes[varName] = TypesFinder.GetType(data.Node.Children[^1].Text);

        if (data.Node.AllTags.Contains("ExpectingSettableReference"))
        {
            var method = new DynamicMethodConvertableWrapperImpl();
            method.Make(
                $"LoadReferenceToLocalVar_{varName}",
                typeof(VariableReference<>).MakeGenericType(_variableTypes[varName]),
                [],
                (il, _) =>
                {
                    il.Ldstr(varName);
                    var variablesContainer = typeof(VariablesContainer<>).MakeGenericType(_variableTypes[varName]);
                    var getRefMethod = variablesContainer.GetMethod("GetRef");
                    il.Call(getRefMethod);
                    il.Ret();
                }
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
        else
        {
            var method = new DynamicMethodConvertableWrapperImpl();
            method.Make($"LoadValueOfLocalVar_{varName}", _variableTypes[varName], [],
                (il, _) =>
                {
                    il.Ldstr(varName);
                    il.Call(typeof(VariablesContainer<>).MakeGenericType(_variableTypes[varName]).GetMethod("Get"));
                    il.Ret();
                }
            );
            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }
    }
}

public static class VariablesContainer<T>
{
    private static readonly Dictionary<string, T> _variables = [];

    public static void Set(string key, T value)
    {
        _variables[key] = value;
    }

    public static T Get(string key)
    {
        return _variables[key];
    }

    public static VariableReference<T> GetRef(string key)
    {
        return new VariableReference<T>(value => _variables[key] = value);
    }
}

public class VariableReference<T>(Action<T> set) : ISettable<T>
{
    public void SetValue(T value)
    {
        set(value);
    }
}

public class EqualityAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("Equality"))
            return;

        data.BytecodeTranslator.Translate(data.Node.Children[0]);
        data.BytecodeTranslator.Translate(data.Node.Children[1]);

        var method = new DynamicMethodConvertableWrapperImpl();
        method.Make(
            $"Set_{data.Node.Children[0].LexemeValue?.Text}={data.Node.Children[1].LexemeValue?.Text}",
            typeof(void),
            [null, null],
            (il, args) =>
            {
                il.LdArgsAndCall(
                    args[0].GetMethod("SetValue", BindingFlags.Instance | BindingFlags.Public).NotNull(),
                    i =>
                    {
                        il.Ldarg(i);
                        return args[i];
                    });
                il.Ret();
            }
        );
        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}