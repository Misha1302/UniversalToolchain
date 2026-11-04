using Shared;
namespace Range;
public sealed class RangeProvider : IFactProvider {
 public string Domain => "range";
 // Conservatively cannot establish safety for index 0, even when it is valid.
 public Observation Query(Query q) => new(Domain,q.Revision,q.ImplementationHash,q.Index>0 && q.Index<q.Length);
}
public sealed class PreciseRangeProvider : IFactProvider {
 public string Domain => "range";
 public Observation Query(Query q) => new(Domain,q.Revision,q.ImplementationHash,q.Index>=0 && q.Index<q.Length);
}
public sealed class ConflictingRangeProvider : IFactProvider {
 public string Domain => "range";
 public Observation Query(Query q) => new(Domain,q.Revision,q.ImplementationHash,false);
}
