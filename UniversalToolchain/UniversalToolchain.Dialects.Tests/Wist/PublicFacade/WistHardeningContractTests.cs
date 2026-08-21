using CommonExceptions;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

[TestFixture]
public sealed class WistHardeningContractTests
{
    [Test]
    public void TryCompile_InvalidFormula_SafeModeIsUserInputWithoutExceptionObject()
    {
        using var engine = WistEngine.Create(new WistEngineOptions
        {
            DiagnosticExposure = WistDiagnosticExposure.Safe
        });

        var result = engine.TryCompile<Func<double, double>>("price *", "price");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(WistFailureKind.UserInput));
            Assert.That(result.Exception, Is.Null);
            Assert.That(result.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void TryCompile_InvalidFormula_PreservesParserDiagnosticStage()
    {
        using var engine = WistEngine.CreateRestrictedArithmetic();

        var result = engine.TryCompile<Func<double, double>>("price *", "price");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(WistFailureKind.UserInput));
            Assert.That(result.Diagnostics, Has.Count.EqualTo(1));
            Assert.That(result.Diagnostics[0].Code, Is.EqualTo(WistDiagnosticCodes.ParserFailure));
            Assert.That(result.Diagnostics[0].Stage, Is.EqualTo("Parser"));
        });
    }

    [Test]
    public void TryCompile_DeveloperDiagnostics_ExposeExpectedException()
    {
        using var engine = WistEngine.Create(new WistEngineOptions
        {
            DiagnosticExposure = WistDiagnosticExposure.Developer
        });

        var result = engine.TryCompile<Func<double, double>>("price *", "price");

        Assert.Multiple(() =>
        {
            Assert.That(result.FailureKind, Is.EqualTo(WistFailureKind.UserInput));
            Assert.That(result.Exception, Is.Not.Null);
        });
    }

    [Test]
    public void TryCompile_UnknownIdentifier_IsTypedUserInputWithoutChangingLegacyExceptionFamily()
    {
        using var engine = WistEngine.CreateRestrictedArithmetic();

        var result = engine.TryCompile<Func<double>>("missing + 1.0");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(WistFailureKind.UserInput));
            Assert.That(result.Exception, Is.TypeOf<InvalidOperationException>());
            Assert.That(result.Exception!.InnerException, Is.TypeOf<BindingException>());
            Assert.That(result.Diagnostics, Is.Not.Empty);
        });
    }

    [Test]
    public void Validate_ResourceLimit_SafeModeIsPolicyFailure()
    {
        using var engine = WistEngine.Create(new WistEngineOptions
        {
            DiagnosticExposure = WistDiagnosticExposure.Safe,
            ResourceLimits = new WistResourceLimits { MaxSourceLength = 3 }
        });

        var result = engine.Validate("1 + 2");

        Assert.Multiple(() =>
        {
            Assert.That(result.IsValid, Is.False);
            Assert.That(result.FailureKind, Is.EqualTo(WistFailureKind.Policy));
            Assert.That(result.Exception, Is.Null);
        });
    }

    [Test]
    public void ArbitraryInvariantFailure_IsInternalAndCannotBecomeValidationFailure()
    {
        Exception[] untypedFrameworkFaults =
        [
            new InvalidOperationException("artificial invariant failure"),
            new ArgumentException("artificial framework argument failure")
        ];

        Assert.Multiple(() =>
        {
            foreach (var exception in untypedFrameworkFaults)
            {
                var kind = WistFailureClassifier.Classify(exception);
                Assert.That(kind, Is.EqualTo(WistFailureKind.Internal), exception.GetType().Name);
                Assert.That(WistFailureClassifier.IsStructuredResultFailure(kind), Is.False, exception.GetType().Name);
            }

            Assert.That(
                WistFailureClassifier.Classify(new WistUserInputException("typed facade input failure")),
                Is.EqualTo(WistFailureKind.UserInput));
            Assert.That(
                WistFailureClassifier.Classify(
                    new InvalidOperationException("legacy binder family", new BindingException("typed binding failure"))),
                Is.EqualTo(WistFailureKind.UserInput));
        });
    }

    [Test]
    public void SourceRetention_HashAndIdentity_DropsRawSourceButKeepsIdentity()
    {
        const string source = "price * 2.0";
        using var engine = WistEngine.Create(new WistEngineOptions
        {
            SourceRetention = WistSourceRetentionPolicy.HashAndIdentity
        });

        var program = engine.Compile<Func<double, double>>(source, "price");

        Assert.Multiple(() =>
        {
            Assert.That(program.Metadata.SourceRetention, Is.EqualTo(WistSourceRetentionPolicy.HashAndIdentity));
            Assert.That(program.Metadata.SourceText, Is.Null);
            Assert.That(program.Metadata.SourceSha256, Has.Length.EqualTo(64));
            Assert.That(program.Metadata.SourceLength, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void SourceRetention_None_DropsRawSourceAndHash()
    {
        const string source = "price * 2.0";
        using var engine = WistEngine.Create(new WistEngineOptions
        {
            SourceRetention = WistSourceRetentionPolicy.None
        });

        var program = engine.Compile<Func<double, double>>(source, "price");

        Assert.Multiple(() =>
        {
            Assert.That(program.Metadata.SourceRetention, Is.EqualTo(WistSourceRetentionPolicy.None));
            Assert.That(program.Metadata.SourceText, Is.Null);
            Assert.That(program.Metadata.SourceSha256, Is.Null);
            Assert.That(program.Metadata.SourceLength, Is.EqualTo(source.Length));
        });
    }

    [Test]
    public void SameEngine_OverlappingOperations_AreExplicitlyRejected()
    {
        var gate = new WistOperationConcurrencyGate();
        using var first = gate.Enter();

        var exception = Assert.Throws<InvalidOperationException>(() => gate.Enter());

        Assert.That(exception!.Message, Does.Contain("Concurrent operations on one WistEngine instance are not supported"));
    }

    [Test]
    public void SeparateEngines_AreTheSafeConcurrencyUnit()
    {
        using var first = WistEngine.CreateRestrictedArithmetic();
        using var second = WistEngine.CreateRestrictedArithmetic();

        var results = Task.WhenAll(
            Task.Run(() => first.Evaluate<double>("1 + 2")),
            Task.Run(() => second.Evaluate<double>("2 + 3")))
            .GetAwaiter()
            .GetResult();

        Assert.That(results, Is.EqualTo(new[] { 3.0d, 5.0d }));
    }
}
