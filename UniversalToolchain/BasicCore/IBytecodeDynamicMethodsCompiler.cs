using System.Reflection.Emit;
using BasicCore.TranslatorWrapper;
using GrEmit;

namespace BasicCore;

public interface IBytecodeDynamicMethodsCompiler
{
    public List<(GroboIL, DynamicMethod)> Compile(Bytecode bytecode);
}