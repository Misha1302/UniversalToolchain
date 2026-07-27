using System.Security.Cryptography;
using System.Text;

namespace LabelsModule.Core;

public class LabelsSharedData
{
    private readonly Dictionary<string, Guid> _nameToId = [];

    public Guid GetGuidByName(string name) => GetOrCreateIdByName(name);

    public Guid GetIdByName(string name) => GetOrCreateIdByName(name);

    private Guid GetOrCreateIdByName(string name)
    {
        if (_nameToId.TryGetValue(name, out var existingId))
            return existingId;

        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"wist-label:{name}"));
        var id = new Guid(hash.AsSpan(0, 16));
        _nameToId[name] = id;
        return id;
    }
}
