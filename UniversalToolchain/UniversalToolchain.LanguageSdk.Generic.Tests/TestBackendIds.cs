using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Tests;

internal static class TestBackendIds
{
    public static DialectBackendId Interpreter { get; } = new("interpreter");
    public static DialectBackendId Cil { get; } = new("cil");
    public static DialectBackendSelector Any { get; } = DialectBackendSelector.Any;
    public static DialectBackendSelector InterpreterSelector { get; } = DialectBackendSelector.For(Interpreter);
    public static DialectBackendSelector CilSelector { get; } = DialectBackendSelector.For(Cil);
}
