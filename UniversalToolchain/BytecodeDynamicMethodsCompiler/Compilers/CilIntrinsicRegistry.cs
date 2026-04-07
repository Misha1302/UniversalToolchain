namespace BytecodeDynamicMethodsCompiler.Compilers;

internal sealed class CilIntrinsicRegistry
{
    private readonly IReadOnlyDictionary<string, CilIntrinsicDescriptor> _descriptors;

    public CilIntrinsicRegistry(IEnumerable<CilIntrinsicDescriptor> descriptors)
    {
        var orderedDescriptors = descriptors
            .OrderBy(descriptor => descriptor.Name, StringComparer.Ordinal)
            .ToArray();

        var registeredDescriptors = new Dictionary<string, CilIntrinsicDescriptor>(StringComparer.Ordinal);
        foreach (var descriptor in orderedDescriptors)
        {
            if (!registeredDescriptors.TryAdd(descriptor.Name, descriptor))
                Thrower.InvalidOpEx($"Duplicate CIL intrinsic registration: {descriptor.Name}");
        }

        _descriptors = registeredDescriptors;
        SupportedIntrinsics = orderedDescriptors.Select(descriptor => descriptor.Name).ToArray();
    }

    public IReadOnlyList<string> SupportedIntrinsics { get; }

    public bool TryGet(string name, out CilIntrinsicDescriptor descriptor)
        => _descriptors.TryGetValue(name, out descriptor!);

    public CilIntrinsicDescriptor GetRequired(string name)
        => TryGet(name, out var descriptor)
            ? descriptor
            : Thrower.InvalidOpEx<CilIntrinsicDescriptor>($"Unsupported intrinsic: {name}");
}
