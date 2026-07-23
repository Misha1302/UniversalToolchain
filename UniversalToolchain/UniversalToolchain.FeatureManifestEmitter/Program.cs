using System.Reflection;
using System.Runtime.Loader;
using UniversalToolchain.FeatureSdk;

if (args.Length != 2)
{
    Console.Error.WriteLine("Usage: UniversalToolchain.FeatureManifestEmitter <assembly> <output>");
    return 2;
}

var assemblyPath = Path.GetFullPath(args[0]);
var outputPath = Path.GetFullPath(args[1]);
var resolver = new AssemblyDependencyResolver(assemblyPath);
var context = new AssemblyLoadContext("toolchain-feature-emitter", isCollectible: true);
context.Resolving += (_, name) =>
{
    var path = resolver.ResolveAssemblyToPath(name);
    return path == null ? null : context.LoadFromAssemblyPath(path);
};

try
{
    var assembly = context.LoadFromAssemblyPath(assemblyPath);
    var contractName = typeof(ILanguageFeaturePackage).FullName;
    var candidates = assembly.GetTypes()
        .Where(static type => type is { IsAbstract: false, IsInterface: false })
        .Where(type => type.GetInterfaces().Any(i => i.FullName == contractName))
        .Where(static type => type.GetConstructor(Type.EmptyTypes) != null)
        .ToArray();
    if (candidates.Length != 1)
        throw new InvalidOperationException($"Expected exactly one public parameterless feature package in '{assemblyPath}', found {candidates.Length}.");

    var instance = Activator.CreateInstance(candidates[0])!;
    var descriptor = candidates[0].GetProperty(nameof(ILanguageFeaturePackage.Descriptor))?.GetValue(instance)
                     ?? throw new InvalidOperationException("Feature package descriptor is unavailable.");
    // Avoid type identity mismatch across isolated contexts by invoking the serializer from the loaded FeatureSdk assembly.
    var featureSdkAssembly = candidates[0].GetInterfaces().Single(i => i.FullName == contractName).Assembly;
    var serializerType = featureSdkAssembly.GetType(typeof(LanguageFeatureManifestSerializer).FullName!)!;
    var serialize = serializerType.GetMethod(nameof(LanguageFeatureManifestSerializer.Serialize), BindingFlags.Public | BindingFlags.Static)!;
    var json = (string)serialize.Invoke(null, [descriptor])!;
    Directory.CreateDirectory(Path.GetDirectoryName(outputPath)!);
    File.WriteAllText(outputPath, json);
    Console.WriteLine(outputPath);
    return 0;
}
finally
{
    context.Unload();
}
