namespace LabelsModule;

public class LabelsSharedData
{
    private readonly Dictionary<string, Guid> _nameToId = [];

    public Guid GetGuidByName(string name)
    {
        if (_nameToId.TryGetValue(name, out var existingId)) return existingId;

        var id = Guid.NewGuid();
        _nameToId[name] = id;
        return id;
    }

    public Guid GetIdByName(string name)
    {
        if (!_nameToId.ContainsKey(name)) GetGuidByName(name);
        return _nameToId[name];
    }
}