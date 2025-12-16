// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using System.Reflection.Emit;
using ExceptionsManager;
using GrEmit;

namespace DynamicMethodWrapper;

public class DynamicMethodConvertableWrapperImpl : IDynamicMethodConvertable
{
    private Action<GroboIL, IDynamicMethodConvertable.Context> _bodyGenerator = null!;
    private bool _isInitialized;
    private Func<IDynamicMethodConvertable.Context, Type> _returnType = null!;
    public int ParamsCount { get; private set; } = -1;

    public string Name { get; private set; } = null!;

    public (GroboIL, DynamicMethod) ToDynamicMethod(IDynamicMethodConvertable.Context context)
    {
        Thrower.AssertAlways(context.Args.Count == ParamsCount);
        Thrower.AssertAlways(_isInitialized, "DynamicMethod Wrapper was not initialized");

        var retType = _returnType(context).NotNull();
        var argsArray = context.Args as Type[] ?? context.Args.ToArray();
        var m = new DynamicMethod(Name, retType, argsArray, true);
        var il = new GroboIL(m);
        _bodyGenerator.Invoke(il, context);

        return (il, m);
    }

    public void Make(
        string name,
        int argsCount,
        Action<GroboIL, IDynamicMethodConvertable.Context> bodyGenerator,
        Func<IDynamicMethodConvertable.Context, Type> returnType
    )
    {
        Name = name;
        _isInitialized = true;
        ParamsCount = argsCount;
        _bodyGenerator = bodyGenerator;
        _returnType = returnType;
    }

    public override string ToString()
    {
        return Name;
    }
}