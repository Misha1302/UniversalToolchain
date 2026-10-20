using SemanticPrototype;
namespace ConsumerQ;
// This compiled assembly is intentionally not changed when ProviderP2 appears.
public static class BoundsConsumer
{
    public static RewritePermit? Propose(EvidenceSession session,FactHandle xRange,FactHandle relation,Subject i,int exclusiveBound)
    {
        var derived=session.DeriveAffineRange(xRange,relation);
        return derived is null ? null : session.PermitUpperCheckElimination(derived,i,exclusiveBound);
    }
}
