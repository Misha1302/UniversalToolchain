using System.Reflection.Emit;
using BasicCore.ExecutorWrapper;
using GrEmit;

namespace BasicCilCompiler;

public class BasicCilCompilerImpl : IExecutor
{
    public object Execute(List<(GroboIL, DynamicMethod)> methods)
    {
        var main = CompileToOneMethod(methods);
        return ExecuteInternal(main);
    }

    private object ExecuteInternal(DynamicMethod main)
    {
        return main.Invoke(null, [])!;
    }

    private DynamicMethod CompileToOneMethod(List<(GroboIL, DynamicMethod)> methods)
    {
        var method = new DynamicMethod("main", typeof(void), []);
        var il = new GroboIL(method);
        var labels =
        (
            from m in methods
            select m.Item2.Name
            into name
            where name.Contains("Label_!Intrinsic")
            select name[(name.LastIndexOf('_') + 1)..]
        ).ToDictionary(
            labelName => labelName,
            labelName => il.DefineLabel(labelName)
        );

        foreach (var m in methods)
        {
            var name = m.Item2.Name;
            if (!name.Contains("!Intrinsic"))
            {
                il.Call(m.Item2);
            }
            else if (name.Contains("Goto_!Intrinsic"))
            {
                var labelName = name[(name.LastIndexOf('_') + 1)..];
                il.Br(labels[labelName]);
            }
            else if (name.Contains("Label_!Intrinsic"))
            {
                var labelName = name[(name.LastIndexOf('_') + 1)..];
                il.MarkLabel(labels[labelName]);
            }
        }

        il.Ret();
        return method;
    }
}