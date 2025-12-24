namespace LabelsModule;

public class LabelsSharedData
{
    private readonly Dictionary<Guid, string> _idToName = [];
    private readonly Dictionary<string, Guid> _nameToId = [];

    public Guid GetGuidByName(string name)
    {
        if (_nameToId.TryGetValue(name, out var existsingId)) return existsingId;

        var id = Guid.NewGuid();
        _idToName[id] = name;
        _nameToId[name] = id;
        return id;
    }

    public Guid GetIdByName(string name)
    {
        if (!_nameToId.ContainsKey(name)) GetGuidByName(name);
        return _nameToId[name];
    }
}