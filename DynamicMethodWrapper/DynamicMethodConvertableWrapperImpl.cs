// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;
using ExceptionsManager;
using GrEmit;

namespace DynamicMethodWrapper;

public class DynamicMethodConvertableWrapperImpl : IDynamicMethodConvertable
{
    private List<Type?> _argAbstractTypes = null!;
    private Action<GroboIL> _bodyGenerator = null!;
    private bool _isInitialized;
    private Type? _returnType;
    public string Name { get; private set; } = null!;

    public DynamicMethod ToDynamicMethod(Type? returnType, IList<Type> args)
    {
        if (_returnType != null) returnType = _returnType;

        Thrower.AssertAlways(_isInitialized, "DynamicMethod Wrapper was not initialized");
        Thrower.AssertAlways(
            args.Select((x, i) => _argAbstractTypes[i] == null || x.IsAssignableTo(_argAbstractTypes[i])).All(x => x),
            MakeArgsInconsistencyErrorMessage(args)
        );
        Thrower.AssertAlways(returnType != null, "Cannot find return type");


        var m = new DynamicMethod(Name, returnType, args as Type[] ?? args.ToArray(), true);
        using var il = new GroboIL(m);
        _bodyGenerator.Invoke(il);

        return m;
    }

    private string MakeArgsInconsistencyErrorMessage(IList<Type> args)
    {
        return
            $"Inconsistency of arg types and param types: ({string.Join(", ", args.Select(x => x.Name))}) " +
            $"and ({string.Join(", ", _argAbstractTypes.Select(x => x?.Name ?? "null"))})";
    }

    public void Make(string name, Type? returnType, List<Type?> argAbstractTypes, Action<GroboIL> bodyGenerator)
    {
        Name = name;
        _isInitialized = true;
        _argAbstractTypes = argAbstractTypes;
        _bodyGenerator = bodyGenerator;
        _returnType = returnType;

        Thrower.AssertAlways(
            returnType != null && (returnType.IsClass || returnType.IsValueType),
            "return type is not a concrete type"
        );
    }
}