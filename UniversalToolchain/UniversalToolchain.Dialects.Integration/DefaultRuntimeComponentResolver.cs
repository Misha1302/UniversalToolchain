using System.Reflection;
using ExceptionsManager;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Dialects.Integration;

public sealed class DefaultRuntimeComponentResolver : IRuntimeComponentResolver
{
    private readonly IRuntimeAssemblyTypeLoader _assemblyTypeLoader;

    public DefaultRuntimeComponentResolver(IRuntimeAssemblyLoadStrategy assemblyLoadStrategy)
        : this(new DefaultRuntimeAssemblyTypeLoader(assemblyLoadStrategy))
    {
    }

    public DefaultRuntimeComponentResolver(IRuntimeAssemblyTypeLoader assemblyTypeLoader)
    {
        _assemblyTypeLoader = assemblyTypeLoader.ArgNotNull();
    }

    public RuntimeComponentDescriptor Resolve(RuntimeComponentManifestEntry entry)
    {
        entry = entry.ArgNotNull();
        var activation = entry.Activation ?? Thrower.InvalidOpEx<RuntimeComponentActivationInfo>(
            $"Runtime component '{entry.ComponentId}' must declare exact activation metadata.");
        var type = _assemblyTypeLoader.LoadType(
            activation.ActivationType.AssemblySimpleName,
            activation.ActivationType.TypeFullName);
        var export = type.GetCustomAttribute<DialectRuntimeExportAttribute>(false);
        if (export == null)
            Thrower.InvalidOpEx(
                $"Runtime activation type '{GetTypeName(type)}' for manifest entry '{entry.ComponentId}' does not declare DialectRuntimeExportAttribute.");

        var descriptor = CreateExportDescriptor(type, export);
        ValidateResolvedComponent(entry, descriptor);
        ValidateAliases(entry, type);
        return new RuntimeComponentDescriptor(
            entry.ComponentId,
            entry.Kind,
            entry.CanonicalAlias,
            entry.Aliases,
            descriptor.ActivationType);
    }

    private static RuntimeComponentExportDescriptor CreateExportDescriptor(Type type, DialectRuntimeExportAttribute export)
    {
        var kind = RuntimeComponentKindCodec.Parse(export.ComponentKind, type.AssemblyQualifiedName ?? type.Name);
        return new RuntimeComponentExportDescriptor(
            RuntimeComponentIdFactory.Create(kind, export.CanonicalAlias),
            kind,
            export.CanonicalAlias,
            type);
    }

    private static void ValidateResolvedComponent(RuntimeComponentManifestEntry entry, RuntimeComponentExportDescriptor descriptor)
    {
        if (descriptor.Id != entry.ComponentId)
            Thrower.InvalidOpEx(
                $"Runtime activation type '{GetTypeName(descriptor.ActivationType)}' exports component id '{descriptor.Id}', not manifest id '{entry.ComponentId}'.");
        if (descriptor.Kind != entry.Kind)
            Thrower.InvalidOpEx(
                $"Runtime manifest entry '{entry.ComponentId}' resolves to type '{GetTypeName(descriptor.ActivationType)}', but the exported component kind is '{RuntimeComponentKindCodec.Format(descriptor.Kind)}' instead of '{RuntimeComponentKindCodec.Format(entry.Kind)}'.");
        if (!string.Equals(descriptor.CanonicalAlias, entry.CanonicalAlias, StringComparison.Ordinal))
            Thrower.InvalidOpEx(
                $"Runtime manifest entry '{entry.ComponentId}' resolves to type '{GetTypeName(descriptor.ActivationType)}', but the exported canonical alias is '{descriptor.CanonicalAlias}' instead of '{entry.CanonicalAlias}'.");
    }

    private static void ValidateAliases(RuntimeComponentManifestEntry entry, Type activationType)
    {
        var declaredAliases = activationType
            .GetCustomAttributes<DialectRuntimeAliasAttribute>(false)
            .Select(static attribute => attribute.Alias?.Trim())
            .Where(static alias => !string.IsNullOrWhiteSpace(alias))
            .Select(static alias => alias!)
            .Where(alias => !string.Equals(alias, entry.CanonicalAlias, StringComparison.Ordinal))
            .Distinct(StringComparer.Ordinal)
            .OrderBy(static alias => alias, StringComparer.Ordinal)
            .ToArray();

        if (!declaredAliases.SequenceEqual(entry.Aliases, StringComparer.Ordinal))
        {
            Thrower.InvalidOpEx(
                $"Runtime manifest aliases for component '{entry.ComponentId}' do not match aliases declared by activation type '{GetTypeName(activationType)}'. " +
                $"Manifest: [{string.Join(", ", entry.Aliases)}]; assembly: [{string.Join(", ", declaredAliases)}].");
        }
    }

    private static string GetTypeName(Type type) => type.FullName ?? type.Name;

    private sealed record RuntimeComponentExportDescriptor(
        RuntimeComponentId Id,
        RuntimeComponentKind Kind,
        string CanonicalAlias,
        Type ActivationType);
}
