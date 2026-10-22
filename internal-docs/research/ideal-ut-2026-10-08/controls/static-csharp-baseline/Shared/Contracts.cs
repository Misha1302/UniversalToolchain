namespace Shared;
public sealed record Query(string ProgramId,int Revision,int Index,int Length,string ImplementationHash,bool LengthStable,bool AliasingSafe);
public sealed record Observation(string Domain,int Revision,string ImplementationHash,bool? Value);
public interface IFactProvider { string Domain {get;} Observation Query(Query q); }
public interface ISafeIndexQuery { bool IsProvenSafe(Query q); }
public sealed class SimpleSafetyQuery(IEnumerable<IFactProvider> providers) : ISafeIndexQuery {
    public bool IsProvenSafe(Query q) {
        // Limited invariant: all facts are exact-snapshot, provider identity matches and no contradiction.
        if (!q.LengthStable || !q.AliasingSafe) return false;
        var facts = providers.Select(p => p.Query(q)).ToArray();
        if (facts.Any(f => f.Revision != q.Revision || f.ImplementationHash != q.ImplementationHash)) return false;
        foreach(var domain in new[] {"shape","range"}) {
            var matching = facts.Where(f => f.Domain == domain).ToArray();
            if (matching.Length == 0 || matching.Any(f => f.Value != true)) return false;
        }
        return true;
    }
}
