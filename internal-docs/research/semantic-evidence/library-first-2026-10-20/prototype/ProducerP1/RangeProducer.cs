using SemanticPrototype;
namespace ProducerP1;
public sealed class RangeProducer(Subject subject,int lower,int upper) : IRootProducer
{
    public string Id => "p1-range";
    public IReadOnlyList<RootDeclaration> Propose(ProgramView view) =>
        [new RangeRoot(view.Scope,subject,new Interval(lower,upper))];
}
