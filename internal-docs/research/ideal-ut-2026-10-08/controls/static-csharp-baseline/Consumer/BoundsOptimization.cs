using Shared;
namespace Consumer;
public sealed class BoundsOptimization(ISafeIndexQuery queries) {
  // A decision test, not machine-code check elimination.
  public bool CanElideCheck(Query q) => queries.IsProvenSafe(q);
}
