using CommonExceptions;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistDslTranslationTests
{
    [Test]
    public void InlineDialect_GroupAlias_ExpandsThroughCanonicalWistGroupInventory()
    {
        const string dialect = """
            dialect GroupedArithmetic
            use ArithmeticCore
            use Scopes
            backend interpreter
            security restricted
            """;
        var options = WistEngineOptions.FromDialectText(dialect);
        options.BackendId = "interpreter";

        using var wist = WistEngine.Create(options);

        Assert.That(wist.Evaluate<int>("2 + 3"), Is.EqualTo(5));
    }

    [Test]
    public void InlineDialect_UnsafeInteropCapability_MapsToTypedRuntimePolicy()
    {
        const string dialect = """
            dialect TrustedInterop
            use BooleanConditions,Comments,ComparisonConditions,Conditions,CSharpInterop,Equality,Identifier,Labels,Loops,NativeTypes,Scopes,SemicolonAsNewLine,Variables,Whitespaces
            backend cil,interpreter
            enable ArithmeticOptimization
            enable BooleanOptimization
            enable ComparisonIntrinsicOptimization
            enable EGraphOptimization
            enable NativeCilOptimization
            enable NativeTypesOptimization
            security trusted
            capability unsafe-interop
            """;
        var options = WistEngineOptions.FromDialectText(dialect);
        options.AllowedAssemblies = [typeof(Math).Assembly];

        using var wist = WistEngine.Create(options);
        var program = wist.Compile<Func<double>>("System.Math.Sqrt(16.0)");

        Assert.That(program.CompiledDelegate(), Is.EqualTo(4.0d).Within(1e-9));
    }

    [Test]
    public void InlineDialect_CompositionRestrictedCapability_MapsToTypedFeature()
    {
        const string dialect = """
            dialect CompositionRestricted
            use Arithmetic,BooleanConditions,Comments,ComparisonConditions,Conditions,Equality,Numbers,Scopes,Whitespaces
            exclude CSharpInterop,Identifier,InternalPreprocessorLexemes,Labels,Loops,NativeTypes,ParametersSetter,SemicolonAsNewLine,Variables
            backend interpreter
            security restricted
            capability composition-restricted
            """;
        var options = WistEngineOptions.FromDialectText(dialect);
        options.BackendId = "interpreter";

        using var wist = WistEngine.Create(options);

        Assert.That(wist.Evaluate<double>("2 + 3"), Is.EqualTo(5.0d).Within(1e-9));
    }

    [Test]
    public void InlineDialect_UnknownCapability_FailsClosedWithSourceLocation()
    {
        const string dialect = """
            dialect UnknownCapability
            use Arithmetic,Numbers,Scopes,Whitespaces
            backend interpreter
            security restricted
            capability magic-runtime-switch
            """;
        var options = WistEngineOptions.FromDialectText(dialect);
        options.BackendId = "interpreter";

        var exception = Assert.Throws<ParserException>(() => WistEngine.Create(options));

        Assert.Multiple(() =>
        {
            Assert.That(exception!.Message, Does.Contain("magic-runtime-switch"));
            Assert.That(exception.Stage, Is.EqualTo("Parser"));
            Assert.That(exception.Location, Is.Not.Null);
            Assert.That(exception.Location!.Value.Line, Is.EqualTo(5));
            Assert.That(exception.Location.Value.Column, Is.EqualTo(0));
        });
    }

    [Test]
    public void InlineDialect_ForbiddenOptimizerIntrinsic_UsesSemanticFallback()
    {
        const string dialect = """
            dialect NoIntegerAddIntrinsic
            use NativeTypes,Numbers,Scopes,Whitespaces
            backend cil
            enable ArithmeticOptimization
            enable EGraphOptimization
            enable NativeCilOptimization
            enable NativeTypesOptimization
            forbid add_i32
            security restricted
            """;
        var options = WistEngineOptions.FromDialectText(dialect);
        options.BackendId = "cil";

        using var wist = WistEngine.Create(options);
        var evaluated = wist.Evaluate<int>("2 + 3");
        var compiled = wist.Compile<Func<int>>("2 + 3");

        Assert.Multiple(() =>
        {
            Assert.That(evaluated, Is.EqualTo(5));
            Assert.That(compiled.CompiledDelegate(), Is.EqualTo(5));
        });
    }
}
