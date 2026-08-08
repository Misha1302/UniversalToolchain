using AbstractIrConverters;
using UniversalToolchain.Wist;

namespace Tests.Backends;

[TestFixture]
public sealed class InterpreterBackendOptimizerIntrinsicSurfaceTests
{
    [Test]
    public void InterpreterBackendStub_SupportedIntrinsics_ShouldContainOnlyUniversalCallIntrinsics()
    {
        var supported = AbstractIrToAbstractIrStub.SupportedIntrinsicIds;

        Assert.That(supported, Does.Contain("call C#"));
        Assert.That(supported, Does.Contain("call C# ctor"));
        Assert.That(supported, Does.Not.Contain("load_external"));
        Assert.That(supported, Does.Not.Contain("store_external"));
    }

    [Test]
    public void InterpreterBackend_WithOptimizersEnabled_ExecutesThroughCanonicalRuntime()
    {
        const string dialect = """
            dialect Tiny
            use NativeTypes, BooleanConditions, ComparisonConditions, Conditions, Identifier, Numbers, Scopes, Variables, Whitespaces
            backend interpreter
            enable ArithmeticOptimization
            enable BooleanOptimization
            enable ComparisonIntrinsicOptimization
            enable NativeCilOptimization
            enable EGraphOptimization
            security restricted
            """;
        var options = WistEngineOptions.FromDialectText(dialect);
        options.BackendId = "interpreter";

        using var engine = WistEngine.Create(options);

        Assert.That(engine.Evaluate<bool>("(1 + 2) > 0 and true"), Is.True);
    }
}
