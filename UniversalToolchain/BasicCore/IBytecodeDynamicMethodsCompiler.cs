// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;
using BasicCore.TranslatorWrapper;
using GrEmit;

namespace BasicCore;

public interface IBytecodeDynamicMethodsCompiler
{
    public List<(GroboIL, DynamicMethod)> Compile(Bytecode bytecode);
}