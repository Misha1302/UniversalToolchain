using System.Collections.ObjectModel;

namespace SemanticPrototype;

// Model-only API: source IDs and ranges are host-approved assumptions, not proven truths.
public readonly record struct World(string SelectedPlanHash, string Target, string SemanticPolicy, string RuleSetVersion);
public readonly record struct Scope(string FunctionId, string BlockId);
public readonly record struct Subject(string FunctionId, string SsaValueId);
public readonly record struct Interval(int Lower, int Upper)
{
    public bool IsValid => Lower <= Upper;
}
public sealed record ProgramView(World World, string Owner, long Revision, Scope Scope);

public abstract record RootDeclaration(Scope Scope);
public sealed record RangeRoot(Scope Scope, Subject Subject, Interval Value) : RootDeclaration(Scope);
public sealed record AffineRoot(Scope Scope, Subject Result, Subject Source, int Offset) : RootDeclaration(Scope);
public sealed record ProducerBinding(string Id, Type ApprovedImplementation);
public interface IRootProducer
{
    string Id { get; }
    IReadOnlyList<RootDeclaration> Propose(ProgramView view);
}

public sealed class FactHandle
{
    internal FactHandle(Guid sessionId, long nodeId) { SessionId = sessionId; NodeId = nodeId; }
    internal Guid SessionId { get; }
    internal long NodeId { get; }
}
public sealed class RewritePermit
{
    internal RewritePermit(Guid sessionId, long generation, long revision, World world, Scope scope, Subject subject, long factId)
    { SessionId=sessionId; Generation=generation; Revision=revision; World=world; Scope=scope; Subject=subject; FactId=factId; }
    internal Guid SessionId { get; }
    internal long Generation { get; }
    internal long Revision { get; }
    internal World World { get; }
    internal Scope Scope { get; }
    internal Subject Subject { get; }
    internal long FactId { get; }
}

public sealed class EvidenceSession
{
    private sealed record FactNode(long Id, long Revision, Scope Scope, Subject Subject,
        Interval? Interval, AffineRoot? Affine, string? Source, string RuleId, IReadOnlyList<long> Parents);
    private readonly object _gate = new();
    private readonly Guid _sessionId = Guid.NewGuid();
    private readonly Dictionary<long, FactNode> _nodes = new();
    private readonly Dictionary<string,Type> _allowed;
    private readonly HashSet<string> _revoked = new();
    private long _nextId, _generation;
    private World _world;
    private long _revision;
    private bool _rewriteApplied;
    public string Owner { get; }
    public Scope Scope { get; }
    public EvidenceSession(World world, string owner, Scope scope, IEnumerable<ProducerBinding> approvedSources)
    {
        _world = world; Owner=owner; Scope=scope;
        _allowed=approvedSources.ToDictionary(x => x.Id,x => x.ApprovedImplementation,StringComparer.Ordinal);
    }
    public ProgramView View { get { lock(_gate) return new(_world,Owner,_revision,Scope); } }
    public bool RewriteApplied { get { lock(_gate) return _rewriteApplied; } }
    public int NodeCount { get { lock(_gate) return _nodes.Count; } }

    // Trust boundary in this model: only host code calls Import, after selecting an approved producer.
    // Executing arbitrary untrusted CLR code in-process is explicitly out of scope.
    public IReadOnlyList<FactHandle> Import(IRootProducer producer)
    {
        ArgumentNullException.ThrowIfNull(producer);
        var view=View;
        var declarations=producer.Propose(view).ToArray();
        lock(_gate)
        {
            if (!_allowed.TryGetValue(producer.Id,out var selectedType) || producer.GetType()!=selectedType || _revoked.Contains(producer.Id))
                throw new InvalidOperationException("Unapproved or revoked root producer");
            if (_world != view.World || _revision != view.Revision)
                throw new InvalidOperationException("Snapshot changed while producer ran");
            // Validate the entire batch before mutating session state (all-or-nothing import).
            foreach(var d in declarations)
            {
                if(d.Scope != Scope || d is RangeRoot r && (!r.Value.IsValid || r.Subject.FunctionId!=Scope.FunctionId) ||
                    d is AffineRoot a && (a.Result.FunctionId!=Scope.FunctionId || a.Source.FunctionId!=Scope.FunctionId) ||
                    d is not (RangeRoot or AffineRoot))
                    throw new InvalidOperationException("Invalid or unsupported root/scope");
            }
            var results=new List<FactHandle>();
            foreach(var d in declarations)
            {
                if(d is RangeRoot r) results.Add(Add(r.Scope,r.Subject,r.Value,null,producer.Id,"trusted-root-v1",[]));
                else if(d is AffineRoot a) results.Add(Add(a.Scope,a.Result,null,a,producer.Id,"trusted-root-v1",[]));
            }
            return new ReadOnlyCollection<FactHandle>(results);
        }
    }

    private FactHandle Add(Scope scope,Subject subject,Interval? interval,AffineRoot? affine,string? source,string ruleId,long[] parents)
    {
        var id=++_nextId;
        _nodes.Add(id,new FactNode(id,_revision,scope,subject,interval,affine,source,ruleId,parents));
        return new FactHandle(_sessionId,id);
    }
    private FactNode? Lookup(FactHandle h)
        => h.SessionId==_sessionId && _nodes.TryGetValue(h.NodeId,out var n) ? n : null;
    private bool Valid(FactNode? node,HashSet<long> visiting,HashSet<long> validated)
    {
        if(node is null || node.Revision!=_revision || node.Scope!=Scope) return false;
        if(validated.Contains(node.Id)) return true; // DAG reconvergence is not a cycle.
        if(!visiting.Add(node.Id)) return false;
        if(node.Source is not null && (!_allowed.ContainsKey(node.Source)||_revoked.Contains(node.Source))) return false;
        foreach(var p in node.Parents)
        {
            if(!_nodes.TryGetValue(p,out var parent) || !Valid(parent,visiting,validated)) return false;
        }
        visiting.Remove(node.Id);
        validated.Add(node.Id);
        return true;
    }
    private bool Valid(FactNode? node) => Valid(node,new HashSet<long>(),new HashSet<long>());

    // Trusted host-owned inference rule. No general proof checker and no plugin executable rule.
    public FactHandle? DeriveAffineRange(FactHandle inputRange,FactHandle inputRelation)
    {
        lock(_gate)
        {
            var left=Lookup(inputRange);var right=Lookup(inputRelation);
            if(!Valid(left) || !Valid(right) || left!.Interval is null || right!.Affine is null) return null;
            var rel=right.Affine;
            if(left.Subject!=rel.Source || left.Scope!=right.Scope || left.Subject==rel.Result) return null;
            var low=(long)left.Interval.Value.Lower+rel.Offset;
            var high=(long)left.Interval.Value.Upper+rel.Offset;
            if(low<int.MinValue || high>int.MaxValue) return null;
            return Add(left.Scope,rel.Result,new Interval((int)low,(int)high),null,null,"affine-range-checked-v1",[left.Id,right.Id]);
        }
    }

    // Second trusted rule: intersection is permitted ONLY for the exact same subject.
    public FactHandle? DeriveIntersection(FactHandle first,FactHandle second)
    {
        lock(_gate)
        {
            var a=Lookup(first);var b=Lookup(second);
            if(!Valid(a) || !Valid(b) || a!.Interval is null || b!.Interval is null ||
                a.Subject!=b.Subject || a.Scope!=b.Scope) return null;
            int low=Math.Max(a.Interval.Value.Lower,b.Interval.Value.Lower);
            int high=Math.Min(a.Interval.Value.Upper,b.Interval.Value.Upper);
            if(low>high) return null; // Contradiction is not an arbitrary replacement fact.
            return Add(a.Scope,a.Subject,new Interval(low,high),null,null,"intersection-same-subject-v1",[a.Id,b.Id]);
        }
    }

    public RewritePermit? PermitUpperCheckElimination(FactHandle rangeFact,Subject target,int exclusiveUpper)
    {
        lock(_gate)
        {
            var node=Lookup(rangeFact);
            if(!Valid(node) || node!.Subject!=target || node.Interval is null || node.Interval.Value.Upper>=exclusiveUpper)
                return null;
            return new RewritePermit(_sessionId,_generation,_revision,_world,Scope,target,node.Id);
        }
    }
    // Only session-private mutable publication is updated; no pre-commit IR mutation is exposed.
    public bool TryCommit(RewritePermit permit)
    {
        lock(_gate)
        {
            if(permit.SessionId!=_sessionId || permit.Generation!=_generation || permit.Revision!=_revision ||
               permit.World!=_world || permit.Scope!=Scope || _rewriteApplied ||
               !_nodes.TryGetValue(permit.FactId,out var n) || !Valid(n) || n.Subject!=permit.Subject)
                return false;
            _rewriteApplied=true;
            _revision++;
            _generation++;
            return true;
        }
    }
    public void AdvanceRevision() { lock(_gate) { _revision++;_generation++; } }
    public void ChangeWorld(World newWorld) { lock(_gate) { _world=newWorld;_revision++;_generation++; } }
    public void Revoke(string sourceId) { lock(_gate) { _revoked.Add(sourceId);_generation++; } }
    public string? DerivationKind(FactHandle handle)
    {
        lock(_gate) return Lookup(handle)?.RuleId;
    }
    public IReadOnlyList<long> Dependencies(FactHandle handle)
    {
        lock(_gate) return Lookup(handle)?.Parents.ToArray() ?? [];
    }
}
