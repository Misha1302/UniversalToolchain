using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistEngineSmokeTests
{
    [Test]
    public void PublicDiagnostics_UseFacadeOwnedContractTypes()
    {
        var properties = typeof(WistDiagnostic).GetProperties().ToDictionary(static property => property.Name);

        Assert.Multiple(() =>
        {
            Assert.That(properties[nameof(WistDiagnostic.Severity)].PropertyType, Is.EqualTo(typeof(WistDiagnosticSeverity)));
            Assert.That(properties[nameof(WistDiagnostic.Span)].PropertyType, Is.EqualTo(typeof(WistSourceSpan)));
            Assert.That(properties[nameof(WistDiagnostic.Hints)].PropertyType, Is.EqualTo(typeof(IReadOnlyList<WistDiagnosticHint>)));
            Assert.That(typeof(WistDiagnosticSeverity).Assembly, Is.EqualTo(typeof(WistEngine).Assembly));
            Assert.That(typeof(WistSourceSpan).Assembly, Is.EqualTo(typeof(WistEngine).Assembly));
            Assert.That(typeof(WistDiagnosticHint).Assembly, Is.EqualTo(typeof(WistEngine).Assembly));
            Assert.That(typeof(WistOptimizationReport).Assembly, Is.EqualTo(typeof(WistEngine).Assembly));
            Assert.That(typeof(WistSsaOptimizationReport).Assembly, Is.EqualTo(typeof(WistEngine).Assembly));
        });
    }

    [Test]
    public void Evaluate_WithAnonymousArguments_ReturnsExpectedDouble()
    {
        using var wist = WistEngine.CreateRestrictedArithmetic();

        var result = wist.Evaluate<double>(
            "price * 0.9 + fee",
            new
            {
                price = 100.0d,
                fee = 5.0d
            });

        Assert.That(result, Is.EqualTo(95.0d).Within(1e-9));
    }

    [Test]
    public void Evaluate_WithExplicitPresetFactories_ReturnsExpectedDouble()
    {
        using var restrictedArithmetic = WistEngine.CreateRestrictedArithmetic();
        using var fullNativePreview = WistEngine.CreateFullNative();

        var restrictedResult = restrictedArithmetic.Evaluate<double>(
            "price * 0.9 + fee",
            new { price = 100.0d, fee = 5.0d });
        var fullResult = fullNativePreview.Evaluate<double>(
            "price * 0.9 + fee",
            new { price = 100.0d, fee = 5.0d });

        Assert.Multiple(() =>
        {
            Assert.That(restrictedResult, Is.EqualTo(95.0d).Within(1e-9));
            Assert.That(fullResult, Is.EqualTo(95.0d).Within(1e-9));
        });
    }

    [Test]
    public void FullNative_ClrInteropWithoutHostAllowlist_FailsClearly()
    {
        using var wist = WistEngine.CreateFullNative();

        var result = wist.TryCompile<Func<double>>("System.Math.Sqrt(16.0)");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void FullNative_ClrInteropWithExplicitHostAllowlist_Succeeds()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.FullNative,
            AllowedAssemblies = [typeof(Math).Assembly]
        });

        var program = wist.Compile<Func<double>>("System.Math.Sqrt(16.0)");

        Assert.That(program.CompiledDelegate(), Is.EqualTo(4.0d).Within(1e-9));
    }

    [Test]
    public void Compile_Delegate_ReturnsExpectedResultAndMetadata()
    {
        using var wist = WistEngine.CreateRestrictedArithmetic();

        var program = wist.Compile<Func<double, double, double>>(
            "price * 0.9 + fee",
            "price",
            "fee");

        var result = program.CompiledDelegate(100.0d, 5.0d);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.EqualTo(95.0d).Within(1e-9));
            Assert.That(program.Metadata.Backend, Is.EqualTo("compiler"));
            Assert.That(program.Metadata.ParameterNames, Is.EqualTo(new[] { "price", "fee" }));
            Assert.That(program.Metadata.ParameterTypes, Is.EqualTo(new[] { typeof(double), typeof(double) }));
            Assert.That(program.Metadata.ReturnType, Is.EqualTo(typeof(double)));
        });
    }

    [Test]
    public void Compile_Delegate_InvokeRepeatedly_ReturnsStableResults()
    {
        using var wist = WistEngine.CreateRestrictedArithmetic();

        var program = wist.Compile<Func<double, double, double>>(
            "price * 0.9 + fee",
            "price",
            "fee");

        var first = program.CompiledDelegate(100.0d, 5.0d);
        var second = program.CompiledDelegate(200.0d, 10.0d);

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(95.0d).Within(1e-9));
            Assert.That(second, Is.EqualTo(190.0d).Within(1e-9));
        });
    }

    [Test]
    public void TryCompile_Delegate_WhenFormulaInvalid_ReturnsFailureWithoutThrowing()
    {
        using var wist = WistEngine.CreateRestrictedArithmetic();

        var result = wist.TryCompile<Func<double, double>>(
            "price *",
            "price");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Program, Is.Null);
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.Message, Is.Not.Empty);
        });
    }

    [Test]
    public void TryCompile_Delegate_WhenDelegateReturnsVoid_ReturnsFailureWithoutThrowing()
    {
        using var wist = WistEngine.CreateRestrictedArithmetic();

        var result = wist.TryCompile<Action<double>>(
            "price * 0.9",
            "price");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Program, Is.Null);
            Assert.That(result.Exception, Is.Not.Null);
            Assert.That(result.Message, Does.Contain("return a value"));
        });
    }

    [Test]
    public void Compile_Delegate_WhenParameterNamesAreDuplicated_FailsClearly()
    {
        using var wist = WistEngine.CreateRestrictedArithmetic();

        var exception = Assert.Catch(
            () => wist.Compile<Func<double, double, double>>(
                "price * 0.9 + price",
                "price",
                "price"));

        Assert.Multiple(() =>
        {
            Assert.That(exception, Is.Not.Null);
            Assert.That(exception!.Message, Does.Contain("Duplicate parameter name"));
        });
    }

    [Test]
    public void Validate_ValidFormulaWithSampleArguments_ReturnsSuccess()
    {
        using var wist = WistEngine.CreateRestrictedArithmetic();

        var result = wist.Validate(
            "price * 0.9 + fee",
            new
            {
                price = 100.0d,
                fee = 5.0d
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.True);
            Assert.That(result.Message, Is.Null);
            Assert.That(result.Exception, Is.Null);
        });
    }

    [Test]
    public void Validate_InvalidFormula_ReturnsFailureWithoutThrowing()
    {
        using var wist = WistEngine.CreateRestrictedArithmetic();

        var result = wist.Validate(
            """
            let discount = 0.9
            price * discount + fee
            """,
            new
            {
                price = 100.0d,
                fee = 5.0d
            });

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Message, Is.Not.Empty);
            Assert.That(result.Exception, Is.Not.Null);
        });
    }

    [Test]
    public void Validate_WhenSourceLimitIsExceeded_ReturnsStableStructuredDiagnostic()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            ResourceLimits = new WistResourceLimits
            {
                MaxSourceLength = 8,
                MaxParameterCount = 4
            }
        });

        var result = wist.Validate("123456789");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(WistDiagnosticCodes.SourceLimitExceeded));
            Assert.That(result.Diagnostics[0].Stage, Is.EqualTo("Policy"));
            Assert.That(result.Exception, Is.TypeOf<WistResourceLimitException>());
        });
    }

    [Test]
    public void TryCompile_WhenParameterLimitIsExceeded_ReturnsStableStructuredDiagnostic()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            ResourceLimits = new WistResourceLimits
            {
                MaxSourceLength = 128,
                MaxParameterCount = 1
            }
        });

        var result = wist.TryCompile<Func<double, double, double>>("left + right", "left", "right");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(WistDiagnosticCodes.ParameterLimitExceeded));
            Assert.That(result.Diagnostics[0].Stage, Is.EqualTo("Policy"));
        });
    }

    [Test]
    public void Create_SnapshotsMutableHostOptions()
    {
        var limits = new WistResourceLimits
        {
            MaxSourceLength = 16,
            MaxParameterCount = 2
        };
        var options = new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            ResourceLimits = limits
        };

        using var wist = WistEngine.Create(options);
        limits.MaxSourceLength = 1;

        var result = wist.Evaluate<double>("1 + 2");

        Assert.That(result, Is.EqualTo(3.0d).Within(1e-9));
    }


    [Test]
    public void Compile_WithSsaPrefer_UsesRouteForAlreadyOptimizedInt32Arithmetic()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            Optimization = new WistOptimizationOptions
            {
                Ssa = new WistSsaOptions
                {
                    Policy = WistSsaPolicy.Prefer,
                    DiagnosticLevel = WistSsaDiagnosticLevel.Detailed
                }
            }
        });

        var program = wist.Compile<Func<int>>("2 + 3");
        var report = program.Metadata.OptimizationReport.Ssa;

        Assert.Multiple(() =>
        {
            Assert.That(program.CompiledDelegate(), Is.EqualTo(5));
            Assert.That(report.UsedSsa, Is.True);
            Assert.That(report.FellBackToAir, Is.False);
            Assert.That(report.ExecutedPasses, Is.Not.Empty);
            Assert.That(report.InputAirInstructionCount, Is.GreaterThan(0));
            Assert.That(report.OutputAirInstructionCount, Is.GreaterThan(0));
            Assert.That(report.Trace.Select(static entry => entry.Stage), Does.Contain("optimization"));
            Assert.That(report.Trace.Select(static entry => entry.Stage), Does.Contain("emission"));
        });
    }

    [Test]
    public void Compile_WithSsaPrefer_ParameterExpressionUsesExternalSlotWithoutFallback()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            Optimization = new WistOptimizationOptions
            {
                Ssa = new WistSsaOptions
                {
                    Policy = WistSsaPolicy.Prefer,
                    DiagnosticLevel = WistSsaDiagnosticLevel.Detailed
                }
            }
        });

        var program = wist.Compile<Func<int, int>>("value + 3", "value");
        var report = program.Metadata.OptimizationReport.Ssa;

        Assert.Multiple(() =>
        {
            Assert.That(program.CompiledDelegate(39), Is.EqualTo(42));
            Assert.That(report.UsedSsa, Is.True);
            Assert.That(report.FellBackToAir, Is.False);
            Assert.That(report.Diagnostics, Is.Empty);
            Assert.That(report.ExecutedPasses, Is.Not.Empty);
        });
    }

    [Test]
    public void Compile_WithSsaDebug_PreservesManagedDivisionBindingWithoutFallback()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            Optimization = new WistOptimizationOptions
            {
                Ssa = new WistSsaOptions
                {
                    Policy = WistSsaPolicy.Debug,
                    DiagnosticLevel = WistSsaDiagnosticLevel.Detailed
                }
            }
        });

        var program = wist.Compile<Func<int>>("8 / 2");
        var report = program.Metadata.OptimizationReport.Ssa;

        Assert.Multiple(() =>
        {
            Assert.That(program.CompiledDelegate(), Is.EqualTo(4));
            Assert.That(report.UsedSsa, Is.True);
            Assert.That(report.FellBackToAir, Is.False);
            Assert.That(report.Trace.Select(static entry => entry.Stage), Does.Contain("emission"));
        });
    }

    [Test]
    public void Compile_WithRepeatedManagedDivision_ReusesEquivalentExecutionBinding()
    {
        using var wist = WistEngine.Create(new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            Optimization = new WistOptimizationOptions
            {
                Ssa = new WistSsaOptions
                {
                    Policy = WistSsaPolicy.Debug,
                    DiagnosticLevel = WistSsaDiagnosticLevel.Detailed
                }
            }
        });

        var program = wist.Compile<Func<int>>("8 / 2 + 9 / 3");
        var report = program.Metadata.OptimizationReport.Ssa;

        Assert.Multiple(() =>
        {
            Assert.That(program.CompiledDelegate(), Is.EqualTo(7));
            Assert.That(report.UsedSsa, Is.True);
            Assert.That(report.FellBackToAir, Is.False);
            Assert.That(report.Diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Create_FromInlineDialectText_ComposesWithoutTemporaryFile()
    {
        const string dialect = """
            dialect InlineRestricted
            use NativeTypes, Numbers, Scopes, Whitespaces
            backend cil
            security restricted
            """;
        using var wist = WistEngine.Create(WistEngineOptions.FromDialectText(dialect));

        Assert.That(wist.Evaluate<int>("2 + 3"), Is.EqualTo(5));
    }

    [Test]
    public void Create_SnapshotsMutableOptimizationOptions()
    {
        var ssa = new WistSsaOptions { Policy = WistSsaPolicy.Prefer };
        var options = new WistEngineOptions
        {
            Preset = WistPreset.RestrictedArithmetic,
            Optimization = new WistOptimizationOptions { Ssa = ssa }
        };

        using var wist = WistEngine.Create(options);
        ssa.Policy = WistSsaPolicy.Disabled;

        var program = wist.Compile<Func<int>>("2 + 3");
        Assert.That(program.Metadata.OptimizationReport.Ssa.RequestedPolicy, Is.EqualTo(WistSsaPolicy.Prefer));
    }

    [Test]
    public void PublicOperations_AfterDispose_FailClearly()
    {
        var wist = WistEngine.CreateRestrictedArithmetic();
        wist.Dispose();

        Assert.Throws<ObjectDisposedException>(() => wist.Evaluate<double>("1 + 2"));
    }
}

#pragma warning restore CS0618
