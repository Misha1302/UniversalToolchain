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
        using var host = ComposeAndCreateHost("dialect Demo\nuse Arithmetic,Numbers\nbackend compiler,interpreter");

        AssertThrowsWithMessageFragment<InvalidOperationException>(
            () => host.GetCore("unknown-mode"),
            "Supported backends:");
    }

    [Test]
    public void GetArtifactCompiler_WithWrongCompilationOutputType_ThrowsInvalidOperationException()
    {
        using var host = ComposeAndCreateHost("dialect Demo\nuse Arithmetic,Numbers\nbackend interpreter");

        AssertThrowsWithMessageFragment<InvalidOperationException>(
            () => host.GetArtifactCompiler<DynamicMethod>("interpreter"),
            "compatible artifact compiler");
    }

    [Test]
    public void GetCore_WithDisabledBackend_ThrowsInvalidOperationException()
    {
        using var host = ComposeAndCreateHost("dialect Demo\nuse Arithmetic,Numbers\nbackend interpreter");

        AssertThrowsWithMessageFragment<InvalidOperationException>(
            () => host.GetCore("compiler"),
            "Unknown backend 'compiler'");
    }

    [Test]
    public void Run_WithUnknownMode_ThrowsInvalidOperationException()
    {
        using var host = ComposeAndCreateHost("dialect Demo\nuse Arithmetic,Numbers\nbackend interpreter");

        AssertThrowsWithMessageFragment<InvalidOperationException>(
            () => host.Run("2 + 2", "unknown-mode"),
            "Unknown backend 'unknown-mode'");
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