using SemanticPrototype;
namespace ProducerP2;
public sealed class RelationProducer(Subject source,Subject target,int offset) : IRootProducer
{
    public string Id => "p2-affine";
    public IReadOnlyList<RootDeclaration> Propose(ProgramView view) =>
        [new AffineRoot(view.Scope,target,source,offset)];
}
