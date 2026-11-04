using Shared;
namespace Shape;
public sealed class ShapeProvider : IFactProvider {
 public string Domain => "shape";
 public Observation Query(Query q) => new(Domain,q.Revision,q.ImplementationHash,q.Length >= 0);
}
