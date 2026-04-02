using System.Reflection.Emit;
using DynamicMethodCalling;
using DynamicMethodCalling.Core;

namespace Tests.Core;

[TestFixture]
public class DynamicMethodInvokerTests
{
    [Test]
    public void AsFuncAndInvoke_ShouldReturnExpectedValues_ForArity0To4()
    {
        var invoker0 = new NativeDelegateInvoker(CreateConstantMethod(41));
        var invoker1 = new NativeDelegateInvoker(CreateIncrementMethod());
        var invoker2 = new NativeDelegateInvoker(CreateSumMethod2());
        var invoker3 = new NativeDelegateInvoker(CreateSumMethod3());
        var invoker4 = new NativeDelegateInvoker(CreateSumMethod4());

        Assert.Multiple(() =>
        {
            Assert.That(invoker0.AsFunc<int>()(), Is.EqualTo(41));
            Assert.That(invoker0.Invoke<int>(), Is.EqualTo(41));

            Assert.That(invoker1.AsFunc<int, int>()(9), Is.EqualTo(10));
            Assert.That(invoker1.Invoke<int, int>(9), Is.EqualTo(10));

            Assert.That(invoker2.AsFunc<int, int, int>()(2, 3), Is.EqualTo(5));
            Assert.That(invoker2.Invoke<int, int, int>(2, 3), Is.EqualTo(5));

            Assert.That(invoker3.AsFunc<int, int, int, int>()(2, 3, 4), Is.EqualTo(9));
            Assert.That(invoker3.Invoke<int, int, int, int>(2, 3, 4), Is.EqualTo(9));

            Assert.That(invoker4.AsFunc<int, int, int, int, int>()(1, 2, 3, 4), Is.EqualTo(10));
            Assert.That(invoker4.Invoke<int, int, int, int, int>(1, 2, 3, 4), Is.EqualTo(10));
        });
    }

    [Test]
    public void ExtensionMethods_ShouldThrowArgumentNullException_WhenInvokerIsNull()
    {
        INativeDelegateInvoker? invoker = null;

        Assert.Multiple(() =>
        {
            Assert.Throws<ArgumentNullException>(() => invoker!.AsFunc<int>());
            Assert.Throws<ArgumentNullException>(() => invoker!.AsFunc<int, int>());
            Assert.Throws<ArgumentNullException>(() => invoker!.AsFunc<int, int, int>());
            Assert.Throws<ArgumentNullException>(() => invoker!.AsFunc<int, int, int, int>());
            Assert.Throws<ArgumentNullException>(() => invoker!.AsFunc<int, int, int, int, int>());

            Assert.Throws<ArgumentNullException>(() => invoker!.Invoke<int>());
            Assert.Throws<ArgumentNullException>(() => invoker!.Invoke<int, int>(1));
            Assert.Throws<ArgumentNullException>(() => invoker!.Invoke<int, int, int>(1, 2));
            Assert.Throws<ArgumentNullException>(() => invoker!.Invoke<int, int, int, int>(1, 2, 3));
            Assert.Throws<ArgumentNullException>(() => invoker!.Invoke<int, int, int, int, int>(1, 2, 3, 4));
        });
    }

    [Test]
    public void DynamicMethodInvokerBase_ShouldThrow_WhenReturnTypeDoesNotMatch()
    {
        var method = CreateConstantMethod(123);

        var exception = Assert.Throws<InvalidOperationException>(() => _ = new DynamicMethodInvoker<string>(method));

        Assert.That(exception!.Message, Does.Contain("Return type must be"));
    }

    [Test]
    public void TypedDynamicMethodInvokers_ShouldInvokeCompiledDynamicMethods_ForArity0To4()
    {
        var typedInvoker0 = new DynamicMethodInvoker<int>(CreateConstantMethod(77));
        var typedInvoker1 = new DynamicMethodInvoker<int, int>(CreateIncrementMethod());
        var typedInvoker2 = new DynamicMethodInvoker<int, int, int>(CreateSumMethod2());
        var typedInvoker3 = new DynamicMethodInvoker<int, int, int, int>(CreateSumMethod3());
        var typedInvoker4 = new DynamicMethodInvoker<int, int, int, int, int>(CreateSumMethod4());

        Assert.Multiple(() =>
        {
            Assert.That(typedInvoker0.Invoke(), Is.EqualTo(77));
            Assert.That(typedInvoker1.Invoke(10), Is.EqualTo(11));
            Assert.That(typedInvoker2.Invoke(5, 6), Is.EqualTo(11));
            Assert.That(typedInvoker3.Invoke(1, 2, 3), Is.EqualTo(6));
            Assert.That(typedInvoker4.Invoke(1, 2, 3, 4), Is.EqualTo(10));
        });
    }

    private static DynamicMethod CreateConstantMethod(int value)
    {
        var method = new DynamicMethod("Const", typeof(int), Type.EmptyTypes);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldc_I4, value);
        il.Emit(OpCodes.Ret);
        return method;
    }

    private static DynamicMethod CreateIncrementMethod()
    {
        var method = new DynamicMethod("Inc", typeof(int), [typeof(int)]);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldc_I4_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);
        return method;
    }

    private static DynamicMethod CreateSumMethod2()
    {
        var method = new DynamicMethod("Sum2", typeof(int), [typeof(int), typeof(int)]);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);
        return method;
    }

    private static DynamicMethod CreateSumMethod3()
    {
        var method = new DynamicMethod("Sum3", typeof(int), [typeof(int), typeof(int), typeof(int)]);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);
        return method;
    }

    private static DynamicMethod CreateSumMethod4()
    {
        var method = new DynamicMethod("Sum4", typeof(int), [typeof(int), typeof(int), typeof(int), typeof(int)]);
        var il = method.GetILGenerator();
        il.Emit(OpCodes.Ldarg_0);
        il.Emit(OpCodes.Ldarg_1);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarg_2);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ldarg_3);
        il.Emit(OpCodes.Add);
        il.Emit(OpCodes.Ret);
        return method;
    }
}
