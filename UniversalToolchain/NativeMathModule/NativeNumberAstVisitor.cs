using BasicCore;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;
using DynamicMethodWrapper;

namespace NativeMathModule;

[AutoRegisterService]
public class NativeNumberAstVisitor : IAstVisitor
{
    public void TryVisit(BytecodeVisitorData data)
    {
        var nodeType = data.Node.NodeType;

        if (nodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("NativeNumber"))
            return;

        var numText = data.Node.LexemeValue.Text;
        var value = NativeTypesModuleImpl.ParseNumber(numText);
        var valueType = value.GetType();

        var method = new AbstractMethodImpl(
            $"PushNative_{valueType.Name}_{value}",
            (il, _) =>
            {
                // Просто пушим значение на стек
                il.Push(value);

                // Для числовых типов не нужно дополнительных действий
                // (в отличие от RealNumberImpl, который требует вызов конструктора)
            }
        );

        data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
    }
}