using System.Reflection;
using UniversalToolchain.Dialects.Wist;

namespace UniversalToolchain.Dialects.Tests;

public class WistRuntimeManifestMetadataValidationTests
{
    [Test]
    public void ManifestEntries_ResolveTypes_AndMatchExpectedContracts()
    {
        var manifest = new WistRuntimeManifest();
        var entries = manifest.Modules.Concat(manifest.Optimizers).Concat(manifest.Backends).ToList();

        foreach (var entry in entries)
        {
            var entryPath = Path.Combine(AppContext.BaseDirectory, entry.AssemblySimpleName + ".dll");
            Assert.That(File.Exists(entryPath), Is.True, $"Assembly is missing: {entryPath}");

            var runtimeAssemblies = Directory.GetFiles(Path.GetDirectoryName(typeof(object).Assembly.Location)!, "*.dll");
            var resolverPaths = runtimeAssemblies.Concat([entryPath, typeof(WistRuntimeManifest).Assembly.Location, typeof(BasicCore.Contracts.IFrontendCoreModule).Assembly.Location, typeof(UniversalToolchain.Dialects.Abstractions.DialectBackendDeclaration).Assembly.Location]).Distinct(StringComparer.Ordinal).ToArray();
            var resolver = new PathAssemblyResolver(resolverPaths);
            using var context = new MetadataLoadContext(resolver);
            var assembly = context.LoadFromAssemblyPath(entryPath);
            var type = assembly.GetType(entry.TypeFullName, throwOnError: false, ignoreCase: false);
            Assert.That(type, Is.Not.Null, $"Type not found: {entry.TypeFullName}");

            var frontendContract = context.LoadFromAssemblyName(typeof(BasicCore.Contracts.IFrontendCoreModule).Assembly.GetName().Name!).GetType("BasicCore.Contracts.IFrontendCoreModule")!;
            var irContract = context.LoadFromAssemblyName(typeof(BasicCore.Contracts.IIRProcessingModule).Assembly.GetName().Name!).GetType("BasicCore.Contracts.IIRProcessingModule")!;
            var backendContract = context.LoadFromAssemblyName(typeof(UniversalToolchain.Dialects.Abstractions.DialectBackendDeclaration).Assembly.GetName().Name!).GetType("UniversalToolchain.Dialects.Abstractions.DialectBackendDeclaration")!;

            Assert.That(IsValidForKind(entry.Kind, type!, frontendContract, irContract, backendContract), Is.True, $"Unexpected contract mismatch for {entry.CanonicalAlias}");
        }
    }

    private static bool IsValidForKind(
        RuntimeComponentKind kind,
        Type type,
        Type frontendContract,
        Type irContract,
        Type backendContract)
    {
        return kind switch
        {
            RuntimeComponentKind.FrontendModule => frontendContract.IsAssignableFrom(type) || irContract.IsAssignableFrom(type),
            RuntimeComponentKind.Optimizer => irContract.IsAssignableFrom(type),
            RuntimeComponentKind.Backend => backendContract.IsAssignableFrom(type),
            _ => false
        };
    }
}
