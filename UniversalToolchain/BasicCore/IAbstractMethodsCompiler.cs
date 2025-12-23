using BasicCore.TranslatorWrapper;

namespace BasicCore;

public interface IAbstractMethodsCompiler<out TCompilationOutput>
{
    public TCompilationOutput Compile(Bytecode bytecode);
}