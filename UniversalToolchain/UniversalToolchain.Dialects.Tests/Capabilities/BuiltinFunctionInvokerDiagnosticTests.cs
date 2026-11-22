using System.Reflection;
using UniversalToolchain.Capabilities.Abstractions;
using UniversalToolchain.Capabilities.Core;
using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.Functions.Abstractions;

namespace UniversalToolchain.Dialects.Tests.Capabilities;

/// <summary>
///     Pins the argument-validation diagnostics of <see cref="BuiltinFunctionInvoker" />.
/// </summary>
/// <remarks>
///     <para>
///         These two codes were the last unobserved ones in the emitted corpus, and the honest reason they were
///         overlooked is worth recording: the invoker has <b>no production caller today</b>, so a reachability
///         argument would have concluded "dead code, ignore it". That argument is wrong here.
///     </para>
///     <para>
///         <see cref="BuiltinFunctionInvoker" /> is a <b>public type with a public surface</b>: it is part of the
///         contract a host consumer can call directly. Its guard clauses are therefore reachable by definition -
///         by the consumer, not by the repository - and its diagnostics are what that consumer receives.
///         Reserving them as "not emitted" would have recorded a false claim, which
///         <c>check-diagnostic-codes.py</c> correctly rejected when it was tried.
///     </para>
///     <para>
///         A third trigger exists for wrong argument type - a null argument for a value-type parameter - and is
///         asserted separately, because it takes a different branch than an incompatible runtime type.
///     </para>
/// </remarks>
[TestFixture]
public sealed class BuiltinFunctionInvokerDiagnosticTests
{
    [Test]
    public void WrongArgumentCount_IsReportedBeforeAnyArgumentIsInspected()
    {
        var result = Invoke(nameof(SingleIntValue), []);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Value, Is.Null);
            Assert.That(
                result.Diagnostics.Select(static diagnostic => diagnostic.Code),
                Does.Contain(ToolchainDiagnosticCodes.WrongFunctionArgumentCount));
        });
    }

    [Test]
    public void ArgumentOfIncompatibleRuntimeType_IsReportedAsWrongType()
    {
        var result = Invoke(nameof(SingleIntValue), ["not an int"]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            var reported = result.Diagnostics.Single(
                static diagnostic => diagnostic.Code == ToolchainDiagnosticCodes.WrongFunctionArgumentType);
            // Both branches of UTC-FUNC-005 carry the same code, so the code alone cannot tell a
            // type-mismatch from a nullability defect. The messages differ because the remedies differ:
            // one needs a different value, the other needs a non-null value. Asserting the message is what
            // keeps the two branches distinguishable.
            Assert.That(reported.Message, Does.Contain("must have runtime type"));
            Assert.That(reported.Message, Does.Contain("Actual runtime type"));
        });
    }

    [Test]
    public void NullArgumentForAValueTypeParameter_IsReportedAsWrongType()
    {
        // Distinct from the incompatible-type branch above: null is checked first, because
        // IsInstanceOfType(null) is false for every parameter type and would blur a nullability defect into
        // a type-mismatch one. Collapsing this branch into the type-mismatch one is therefore a real
        // regression, and only a message assertion catches it - the code is identical either way.
        var result = Invoke(nameof(SingleIntValue), [null]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            var reported = result.Diagnostics.Single(
                static diagnostic => diagnostic.Code == ToolchainDiagnosticCodes.WrongFunctionArgumentType);
            Assert.That(reported.Message, Does.Contain("must not be null"));
            Assert.That(reported.Message, Does.Not.Contain("Actual runtime type"));
        });
    }

    [Test]
    public void CorrectArguments_InvokeTheMethod()
    {
        // The positive control. Without it the three tests above could all pass on an invoker that rejects
        // everything, which is not the contract.
        var result = Invoke(nameof(SingleIntValue), [41]);

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
            Assert.That(result.Diagnostics, Is.Empty);
        });
    }

    public static int SingleIntValue(int value) => value + 1;

    private static BuiltinFunctionInvocationResult Invoke(string methodName, IReadOnlyList<object?> arguments)
    {
        var method = typeof(BuiltinFunctionInvokerDiagnosticTests)
            .GetMethod(methodName, BindingFlags.Public | BindingFlags.Static)!;
        var signature = new BuiltinFunctionSignature(
            methodName,
            method.GetParameters().Select(static parameter => new FunctionTypeDescriptor(parameter.ParameterType.Name)).ToArray());
        var binding = new BuiltinFunctionRuntimeBinding(
            signature,
            new FunctionTypeDescriptor(method.ReturnType.Name),
            new LanguageFeatureId("capability.invoker.test.feature"),
            method,
            []);

        return new BuiltinFunctionInvoker().Invoke(
            new BuiltinFunctionResolution(true, null, binding, null, []),
            arguments);
    }
}
