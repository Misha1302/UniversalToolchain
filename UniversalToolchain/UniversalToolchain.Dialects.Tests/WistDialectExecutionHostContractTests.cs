using System.Reflection.Emit;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistDialectExecutionHostContractTests
{
    [Test]
    public void ComposeFile_WithEmptyPath_ThrowsArgumentException()
    {
        using var provider = CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        AssertThrowsWithMessageFragment<ArgumentException>(
            () => workflow.ComposeFile(string.Empty),
            "Dialect file path must not be empty");
    }

    [Test]
    public void ComposeFile_WithMissingFile_ThrowsFileNotFoundException()
    {
        using var provider = CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var missingPath = Path.Combine(Path.GetTempPath(), $"wist-missing-{Guid.NewGuid():N}.wistdialect");

        AssertThrowsWithMessageFragment<FileNotFoundException>(
            () => workflow.ComposeFile(missingPath),
            missingPath);
    }

    [Test]
    public void ComposeText_WithNullSourceText_ThrowsArgumentNullException()
    {
        using var provider = CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        AssertThrowsWithMessageFragment<ArgumentNullException>(
            () => workflow.ComposeText(null!, "inline"),
            "sourceText");
    }

    [Test]
    public void ComposeText_WithEmptySourceName_ThrowsArgumentException()
    {
        using var provider = CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

        AssertThrowsWithMessageFragment<ArgumentException>(
            () => workflow.ComposeText("dialect Demo\nuse Arithmetic\nbackend interpreter", string.Empty),
            "Source name must not be empty");
    }

    [Test]
    public void CreateHost_WithFailedComposition_ThrowsArgumentException()
    {
        using var provider = CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText("dialect Broken\nuse MissingModule\nbackend interpreter", "broken-inline");

        Assert.That(composition.IsSuccess, Is.False);

        AssertThrowsWithMessageFragment<ArgumentException>(
            () => workflow.CreateHost(composition),
            "must be successful");
    }

    [Test]
    public void GetCore_WithUnknownMode_ThrowsInvalidOperationException_WithSupportedModesList()
    {
        var exception = CaptureGetCoreFailure(
            "dialect Demo\nuse Arithmetic,Numbers\nbackend cil,interpreter",
            "unknown-mode");

        Assert.That(exception.Message, Does.Contain("Supported backends:"));
    }

    [Test]
    public void GetBackendSpecificArtifactCompiler_WithWrongCompilationOutputType_ThrowsInvalidOperationException()
    {
        var exception = CaptureBackendSpecificCompilerFailure<DynamicMethod>(
            "dialect Demo\nuse Arithmetic,Numbers\nbackend interpreter",
            "interpreter");

        Assert.That(exception.Message, Does.Contain("compatible artifact compiler"));
    }

    [Test]
    public void GetCore_WithDisabledBackend_ThrowsInvalidOperationException()
    {
        var exception = CaptureGetCoreFailure(
            "dialect Demo\nuse Arithmetic,Numbers\nbackend interpreter",
            "cil");

        Assert.That(exception.Message, Does.Contain("Unknown backend 'cil'"));
    }

    [Test]
    public void Run_WithUnknownMode_ThrowsInvalidOperationException()
    {
        var exception = CaptureRunFailure(
            "dialect Demo\nuse Arithmetic,Numbers\nbackend interpreter",
            "unknown-mode");

        Assert.That(exception.Message, Does.Contain("Unknown backend 'unknown-mode'"));
    }

    private static Exception CaptureGetCoreFailure(string dialect, string backend)
    {
        using var host = ComposeAndCreateHost(dialect);
        try
        {
            _ = host.GetCore(backend);
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new AssertionException("Expected GetCore to fail.");
    }

    private static Exception CaptureBackendSpecificCompilerFailure<TCompilationOutput>(string dialect, string backend)
    {
        using var host = ComposeAndCreateHost(dialect);
        try
        {
            _ = host.GetBackendSpecificArtifactCompiler<TCompilationOutput>(backend);
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new AssertionException("Expected backend-specific compiler resolution to fail.");
    }

    private static Exception CaptureRunFailure(string dialect, string backend)
    {
        using var host = ComposeAndCreateHost(dialect);
        try
        {
            _ = host.Run("2 + 2", backend);
        }
        catch (Exception exception)
        {
            return exception;
        }

        throw new AssertionException("Expected Run to fail.");
    }

    private static WistDialectExecutionHost ComposeAndCreateHost(string dialect)
    {
        using var provider = CreateWorkflowProvider();
        var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
        var composition = workflow.ComposeText(dialect, "inline");
        if (!composition.IsSuccess)
            throw new InvalidOperationException(DialectCompositionExplanationFormatter.FormatDeterministic(DialectCompositionExplanationProjector.Project(composition)));

        return workflow.CreateHost(composition);
    }

    private static ServiceProvider CreateWorkflowProvider()
    {
        var services = new ServiceCollection();
        services.AddWistDialectServices();
        services.AddWistCilBackend();
        services.AddWistInterpreterBackend();
        return services.BuildServiceProvider();
    }

    private static void AssertThrowsWithMessageFragment<TException>(TestDelegate action, string expectedMessageFragment)
        where TException : Exception
    {
        var ex = Assert.Throws<TException>(action);

        Assert.That(ex!.Message, Does.Contain(expectedMessageFragment));
    }
}