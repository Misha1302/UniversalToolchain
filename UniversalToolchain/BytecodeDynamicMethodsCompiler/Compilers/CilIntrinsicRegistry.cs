using UniversalToolchain.Intrinsics.Core;

namespace BytecodeDynamicMethodsCompiler.Compilers;

internal sealed class CilIntrinsicRegistry
{
    private readonly IReadOnlyList<CilIntrinsicDescriptor> _descriptors;
    private readonly IReadOnlyDictionary<string, CilIntrinsicDescriptor> _descriptorsByName;

    public CilIntrinsicRegistry()
    {
        var descriptors = new List<CilIntrinsicDescriptor>();

        var carriedRegister = (
            string name,
            Action<CompilationContext, Instruction, List<Type>> compile
        ) => Register(descriptors, name, compile, IntrinsicTypeProcessor.ProcessTypes);

        carriedRegister(
            "call C#",
            AbstractMethodsIntrinsicCompiler.CompileCallCSharp
        );
        carriedRegister(
            "call C# ctor",
            AbstractMethodsIntrinsicCompiler.CompileCallCSharpCtor
        );
        carriedRegister(
            "store_local",
            AbstractMethodsIntrinsicCompiler.CompileStoreLocal
        );
        carriedRegister(
            "load_local",
            AbstractMethodsIntrinsicCompiler.CompileLoadLocal
        );
        carriedRegister(
            "load_local_ref",
            AbstractMethodsIntrinsicCompiler.CompileLoadLocalRef
        );
        carriedRegister(
            "load_external",
            AbstractMethodsIntrinsicCompiler.CompileLoadExternal
        );
        carriedRegister(
            "store_external",
            AbstractMethodsIntrinsicCompiler.CompileStoreExternal
        );
        carriedRegister(
            "load_bool",
            AbstractMethodsIntrinsicCompiler.CompileLoadBool
        );
        Register(
            descriptors,
            "boolean_and",
            AbstractMethodsIntrinsicCompiler.CompileBooleanAnd,
            IntrinsicTypeProcessor.ProcessTypes
        );
        carriedRegister(
            "boolean_or",
            AbstractMethodsIntrinsicCompiler.CompileBooleanOr
        );
        carriedRegister(
            "boolean_not",
            AbstractMethodsIntrinsicCompiler.CompileBooleanNot
        );
        carriedRegister(
            "load_i32",
            AbstractMethodsIntrinsicCompiler.LoadNativeNumber
        );
        carriedRegister(
            "load_i64",
            AbstractMethodsIntrinsicCompiler.LoadNativeNumber
        );
        carriedRegister(
            "load_f32",
            AbstractMethodsIntrinsicCompiler.LoadNativeNumber
        );
        carriedRegister(
            "load_f64",
            AbstractMethodsIntrinsicCompiler.LoadNativeNumber
        );
        carriedRegister(
            "load_decimal",
            AbstractMethodsIntrinsicCompiler.LoadNativeNumber
        );

        RegisterArithmeticFamily(descriptors, "i32");
        RegisterArithmeticFamily(descriptors, "i64");
        RegisterArithmeticFamily(descriptors, "f32");
        RegisterArithmeticFamily(descriptors, "f64");
        RegisterArithmeticFamily(descriptors, "decimal");

        RegisterComparisonFamily(descriptors, "i32");
        RegisterComparisonFamily(descriptors, "i64");
        RegisterComparisonFamily(descriptors, "f32");
        RegisterComparisonFamily(descriptors, "f64");

        var descriptorsByName = new OrderedDictionary<string, CilIntrinsicDescriptor>(descriptors.Count, StringComparer.Ordinal);
        foreach (var descriptor in descriptors)
        {
            if (!descriptorsByName.TryAdd(descriptor.Name, descriptor))
                Thrower.InvalidOpEx($"Duplicate CIL intrinsic registration: {descriptor.Name}");
        }

        _descriptors = descriptors.AsReadOnly();
        _descriptorsByName = descriptorsByName;
        SupportedIntrinsics = _descriptors.Select(x => x.Name).ToArray();
    }

    public IReadOnlyList<string> SupportedIntrinsics { get; }

    public bool TryGet(string name, out CilIntrinsicDescriptor descriptor)
        => _descriptorsByName.TryGetValue(name, out descriptor!);

    public CilIntrinsicDescriptor GetRequired(string name)
        => TryGet(name, out var descriptor)
            ? descriptor
            : Thrower.InvalidOpEx<CilIntrinsicDescriptor>($"Unsupported intrinsic: {name}");

    private static void Register(
        ICollection<CilIntrinsicDescriptor> descriptors,
        string name,
        Action<CompilationContext, Instruction, List<Type>> compile,
        Action<Instruction, List<Type>> processTypes)
    {
        descriptors.Add(new CilIntrinsicDescriptor(name, compile, processTypes));
    }

    private static void RegisterArithmeticFamily(ICollection<CilIntrinsicDescriptor> descriptors, string typeName)
    {
        Register(
            descriptors,
            $"add_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileArithmeticIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
        Register(
            descriptors,
            $"sub_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileArithmeticIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
        Register(
            descriptors,
            $"mul_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileArithmeticIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
        Register(
            descriptors,
            $"div_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileArithmeticIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
    }

    private static void RegisterComparisonFamily(ICollection<CilIntrinsicDescriptor> descriptors, string typeName)
    {
        Register(
            descriptors,
            $"cmp_eq_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileComparisonIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
        Register(
            descriptors,
            $"cmp_ne_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileComparisonIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
        Register(
            descriptors,
            $"cmp_gt_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileComparisonIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
        Register(
            descriptors,
            $"cmp_ge_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileComparisonIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
        Register(
            descriptors,
            $"cmp_lt_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileComparisonIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
        Register(
            descriptors,
            $"cmp_le_{typeName}",
            AbstractMethodsIntrinsicCompiler.CompileComparisonIntrinsic,
            IntrinsicTypeProcessor.ProcessTypes
        );
    }
}