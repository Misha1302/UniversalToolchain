using System.Diagnostics;
using UniversalToolchain.Dialects.Wist;

namespace Tests.Infrastructure;

internal sealed class TempDirectory : IDisposable
{
    public TempDirectory()
    {
        Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), $"wist-tests-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (Directory.Exists(Path))
            Directory.Delete(Path, true);
    }
}

internal sealed class StaticManifestLocator(IReadOnlyList<string> paths) : IRuntimeManifestFileLocator
{
    public IReadOnlyList<string> GetManifestFilePaths() => paths;
}

internal static class TestContractsInfrastructure
{
    public static string WriteManifest(string root, string fileName, string assemblySimpleName, IReadOnlyList<FileDialectRuntimeComponentEntry> components)
    {
        var serializer = new RuntimeManifestJsonSerializer();
        var path = Path.Combine(root, fileName);
        var document = new FileDialectRuntimeManifestDocument(assemblySimpleName, components);
        File.WriteAllText(path, serializer.Serialize(document));
        return path;
    }

    public static ServiceProvider CreateWorkflowProvider(bool addCompiler = true, bool addInterpreter = true, string? searchRoot = null)
    {
        var services = new ServiceCollection();
        if (!string.IsNullOrWhiteSpace(searchRoot))
            services.AddSingleton(new RuntimeArtifactLocatorOptions { SearchRoots = [searchRoot], IncludeAppContextBaseDirectory = true });

        services.AddWistDialectServices();
        if (addCompiler)
            services.AddWistCilBackend();

        if (addInterpreter)
            services.AddWistInterpreterBackend();

        return services.BuildServiceProvider();
    }

    public static string BuildSelectionSignature(DialectFrameworkCompositionResult composition)
    {
        var selection = composition.RuntimeSelection as SelectedRuntimePlan;
        if (selection == null)
            return "<no-selection>";

        return string.Join("|", selection.OrderedModules.Select(static x => x.CanonicalAlias))
               + "::"
               + string.Join("|", selection.EnabledOptimizers.Select(static x => x.CanonicalAlias))
               + "::"
               + string.Join("|", selection.EnabledBackends.Select(static x => x.CanonicalAlias));
    }

    public static string BuildHostSignature(WistDialectExecutionHost host)
    {
        return string.Join("|", host.Configuration.FrontendModules.Select(static x => x.FullName))
               + "::"
               + string.Join("|", host.Configuration.IrModules.Select(static x => x.FullName))
               + "::"
               + string.Join("|", host.Configuration.Optimizers.Select(static x => x.FullName))
               + "::"
               + string.Join("|", host.Configuration.BackendConfigurations.Select(static x => x.BackendDescriptor.CanonicalId));
    }

    public static CliResult RunProcess(string fileName, string arguments, string workingDirectory, int timeoutMs)
    {
        var startInfo = new ProcessStartInfo(ResolveProcessFileName(fileName), arguments)
        {
            RedirectStandardError = true,
            RedirectStandardOutput = true,
            UseShellExecute = false,
            WorkingDirectory = workingDirectory
        };
        startInfo.Environment.TryAdd("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1");
        startInfo.Environment.TryAdd("DOTNET_CLI_HOME", ResolveWritableDotnetHome());

        using var process = Process.Start(startInfo)!;
        var stdOutTask = process.StandardOutput.ReadToEndAsync();
        var stdErrTask = process.StandardError.ReadToEndAsync();

        var timedOut = !process.WaitForExit(timeoutMs);
        if (timedOut)
        {
            process.Kill(true);
            process.WaitForExit(5000);
        }
        else
        {
            process.WaitForExit();
        }

        var streamsCompleted = Task.WaitAll([stdOutTask, stdErrTask], TimeSpan.FromSeconds(10));
        var stdOut = streamsCompleted && stdOutTask.IsCompletedSuccessfully ? stdOutTask.Result : string.Empty;
        var stdErr = streamsCompleted && stdErrTask.IsCompletedSuccessfully ? stdErrTask.Result : string.Empty;

        return new CliResult(process.ExitCode, stdOut, stdErr, timedOut);
    }

    private static string ResolveProcessFileName(string fileName)
    {
        if (!string.Equals(fileName, "dotnet", StringComparison.OrdinalIgnoreCase))
            return fileName;

        if (!string.IsNullOrWhiteSpace(Environment.ProcessPath) &&
            string.Equals(Path.GetFileName(Environment.ProcessPath), "dotnet", StringComparison.OrdinalIgnoreCase))
        {
            return Environment.ProcessPath;
        }

        return fileName;
    }

    private static string ResolveWritableDotnetHome()
    {
        var existing = Environment.GetEnvironmentVariable("DOTNET_CLI_HOME");
        if (!string.IsNullOrWhiteSpace(existing))
            return existing;

        var home = Path.Combine(Path.GetTempPath(), "wist-dotnet-home");
        Directory.CreateDirectory(home);
        return home;
    }
}

internal sealed record CliResult(int ExitCode, string StdOut, string StdErr, bool TimedOut);
