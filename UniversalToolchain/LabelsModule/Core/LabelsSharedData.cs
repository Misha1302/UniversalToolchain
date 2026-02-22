namespace LabelsModule;

public class LabelsSharedData
{
    private readonly Dictionary<string, Guid> _nameToId = [];

    public Guid GetGuidByName(string name) => GetOrCreateIdByName(name);

    public Guid GetIdByName(string name) => GetOrCreateIdByName(name);

    private Guid GetOrCreateIdByName(string name)
    {
        if (_nameToId.TryGetValue(name, out var existingId)) return existingId;

        var id = Guid.NewGuid();
        _nameToId[name] = id;
        return id;
    }
}