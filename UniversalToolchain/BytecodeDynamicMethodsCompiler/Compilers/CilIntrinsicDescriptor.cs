namespace BytecodeDynamicMethodsCompiler.Compilers;

internal sealed record CilIntrinsicDescriptor(
    string Name,
    Action<CompilationContext, Instruction, List<Type>> Compile,
    Action<Instruction, List<Type>> ProcessTypes
);