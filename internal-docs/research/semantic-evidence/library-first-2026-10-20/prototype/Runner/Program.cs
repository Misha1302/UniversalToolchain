using System.Reflection;
using ConsumerQ;
using ProducerP1;
using SemanticPrototype;

static class Program
{
    static readonly World World0 = new("RESEARCH-DEMO-NOT-UT-PLANHASH", "cil-int32-checked", "host-policy-v1", "trusted-rules-v1");
    static readonly Scope Sc = new("main", "entry");
    static readonly Subject X = new("main", "%x");
    static readonly Subject I = new("main", "%i");
    static readonly Subject Y = new("main", "%y");
    static int _checks;

    static void Check(string name,Action action)
    {
        action();
        _checks++;
        Console.WriteLine("PASS " + name);
    }
    static void Require(bool condition,string message="assertion failed")
    {
        if (!condition) throw new InvalidOperationException(message);
    }
    static EvidenceSession Fresh(Type? p2Type=null) => new(World0,"host-ssa-owner",Sc,
        p2Type is null
            ? [new ProducerBinding("p1-range", typeof(RangeProducer))]
            : [new ProducerBinding("p1-range",typeof(RangeProducer)),new ProducerBinding("p2-affine",p2Type)]);
    static IRootProducer P2(string path,Subject source,Subject result,int offset)
    {
        var assembly=Assembly.LoadFrom(Path.GetFullPath(path));
        var type=assembly.GetType("ProducerP2.RelationProducer",throwOnError:true)!;
        return (IRootProducer)Activator.CreateInstance(type,source,result,offset)!;
    }
    static (EvidenceSession session, FactHandle range, FactHandle relation) Seed(string p2path,int lo=0,int hi=2,Subject? source=null, int shift=5)
    {
        var p2=P2(p2path,source??X,I,shift);
        var session=Fresh(p2.GetType());
        var range=session.Import(new RangeProducer(X,lo,hi)).Single();
        var relation=session.Import(p2).Single();
        return (session,range,relation);
    }
    // Honest typed-C# baseline has the same arithmetic reasoning and trust assumptions;
    // DAG's possible extra value is provenance, dependency invalidation and shared vocabulary.
    static bool TypedBaselineAllows(RangeRoot r,AffineRoot a,Subject target,int bound)
    {
        if(r.Scope!=a.Scope || r.Subject!=a.Source || a.Result!=target) return false;
        long upper=(long)r.Value.Upper + a.Offset;
        long lower=(long)r.Value.Lower + a.Offset;
        return r.Value.IsValid && lower>=int.MinValue && upper<=int.MaxValue && upper<bound;
    }
    static int Original(int x)
    {
        var i=checked(x+5);
        if(i>=8) throw new ArgumentOutOfRangeException(nameof(x));
        return i;
    }
    static int WithoutCheck(int x) => checked(x+5);

    sealed class SpoofedProducer : IRootProducer
    {
        public string Id=>"p1-range";
        public IReadOnlyList<RootDeclaration> Propose(ProgramView v) => [new RangeRoot(v.Scope,X,new(0,2))];
    }
    sealed class InvalidBatchProducer : IRootProducer
    {
        public string Id=>"invalid-batch";
        public IReadOnlyList<RootDeclaration> Propose(ProgramView v) =>
            [new RangeRoot(v.Scope,X,new(0,2)),new RangeRoot(new Scope("another","entry"),X,new(0,2))];
    }

    public static int Main(string[] args)
    {
        Console.WriteLine("MODEL_ONLY=YES; UT_NATIVE_GATE=NOT_IMPLEMENTED; TRUSTED_ROOTS_ARE_AXIOMS");
        Check("P1 only: Q cannot infer i range without P2", () => {
            var session=Fresh();
            var range=session.Import(new RangeProducer(X,0,2)).Single();
            Require(session.NodeCount==1);
            Require(!session.RewriteApplied);
            Require(TypedBaselineAllows(new RangeRoot(Sc,X,new(0,2)),new AffineRoot(Sc,I,X,5),I,8));
            // A typed baseline can implement the same rule, once given the affine relation.
        });
        if(args.Length==0) { Console.WriteLine($"P2_NOT_LOADED checks={_checks}");return 0; }
        var dll=args[0];
        Check("P2 + unchanged Q derives a safe i range and rewrites", () => {
            var (s,r,a)=Seed(dll);
            var permit=BoundsConsumer.Propose(s,r,a,I,8);
            Require(permit is not null);
            var derivation=s.DeriveAffineRange(r,a);
            Require(derivation is not null && s.Dependencies(derivation).Count==2);
            Require(s.DerivationKind(derivation!)=="affine-range-checked-v1");
            Require(s.TryCommit(permit!));
            foreach(var x in Enumerable.Range(0,3)) Require(Original(x)==WithoutCheck(x),"independent Int32 oracle mismatch");
        });
        Check("unsafe x=[0,7] must retain the check", () => {
            var (s,r,a)=Seed(dll,0,7);
            Require(BoundsConsumer.Propose(s,r,a,I,8) is null);
            Require(!s.RewriteApplied);
            var unsafeInput=7;
            Require(WithoutCheck(unsafeInput)==12);
            try { _=Original(unsafeInput);throw new InvalidOperationException("Oracle should have thrown"); }
            catch(ArgumentOutOfRangeException) { }
            Require(!TypedBaselineAllows(new RangeRoot(Sc,X,new(0,7)),new AffineRoot(Sc,I,X,5),I,8));
        });
        Check("cross-subject inference is rejected", () => {
            var (s,r,a)=Seed(dll,0,2,source:Y);
            Require(s.DeriveAffineRange(r,a) is null);
            Require(s.DeriveIntersection(r,a) is null);
        });
        Check("intersection forbids cross-subject and accepts shared DAG ancestors", () => {
            var s=Fresh();
            var x=s.Import(new RangeProducer(X,0,10)).Single();
            var y=s.Import(new RangeProducer(Y,4,8)).Single();
            Require(s.DeriveIntersection(x,y) is null);
            var a=s.DeriveIntersection(x,x);
            Require(a is not null);
            var diamond=s.DeriveIntersection(a!,a!);
            Require(diamond is not null,"DAG reconvergence is not a cycle");
        });
        Check("checked arithmetic refuses overflow", () => {
            var (s,r,a)=Seed(dll,int.MaxValue-1,int.MaxValue);
            Require(s.DeriveAffineRange(r,a) is null);
        });
        Check("unapproved roots do not enter session", () => {
            var s=Fresh();
            try {s.Import(P2(dll,X,I,5));throw new InvalidOperationException("unapproved root imported");}
            catch(InvalidOperationException e) when(e.Message.Contains("Unapproved")) { }
            Require(s.NodeCount==0);
        });
        Check("spoofed producer ID is not trusted without selected implementation identity", () => {
            var s=Fresh();
            try {s.Import(new SpoofedProducer());throw new InvalidOperationException("spoofed root imported");}
            catch(InvalidOperationException e) when(e.Message.Contains("Unapproved")) { }
            Require(s.NodeCount==0);
        });
        Check("invalid batch import is atomic", () => {
            var s=new EvidenceSession(World0,"host-ssa-owner",Sc,
                [new ProducerBinding("invalid-batch",typeof(InvalidBatchProducer))]);
            try {s.Import(new InvalidBatchProducer());throw new InvalidOperationException("invalid batch imported");}
            catch(InvalidOperationException e) when(e.Message.Contains("Invalid or unsupported")) { }
            Require(s.NodeCount==0);
        });
        Check("stale revision blocks use and commit", () => {
            var (s,r,a)=Seed(dll);
            var permit=BoundsConsumer.Propose(s,r,a,I,8)!;
            s.AdvanceRevision();
            Require(s.DeriveAffineRange(r,a) is null);
            Require(!s.TryCommit(permit) && !s.RewriteApplied);
        });
        Check("changed world/policy invalidates old permits", () => {
            var (s,r,a)=Seed(dll);
            var permit=BoundsConsumer.Propose(s,r,a,I,8)!;
            s.ChangeWorld(World0 with {SemanticPolicy="host-policy-v2"});
            Require(!s.TryCommit(permit) && !s.RewriteApplied);
        });
        Check("revocation transitively invalidates descendants", () => {
            var (s,r,a)=Seed(dll);
            var derived=s.DeriveAffineRange(r,a)!;
            var permit=s.PermitUpperCheckElimination(derived,I,8)!;
            s.Revoke("p2-affine");
            Require(s.PermitUpperCheckElimination(derived,I,8) is null);
            Require(!s.TryCommit(permit));
        });
        Check("foreign-session permit is not reusable", () => {
            var (s,r,a)=Seed(dll);
            var permit=BoundsConsumer.Propose(s,r,a,I,8)!;
            var target=Fresh();
            Require(!target.TryCommit(permit));
        });
        Check("concurrent permits yield exactly one commit", () => {
            var (s,r,a)=Seed(dll);
            var permit=BoundsConsumer.Propose(s,r,a,I,8)!;
            var winners=0;
            Parallel.For(0,32,_=>{if(s.TryCommit(permit))Interlocked.Increment(ref winners);});
            Require(winners==1 && s.RewriteApplied);
        });
        Check("signed overflow and bounds match baseline for preregistered safe subset", () => {
            for(int lower=-20;lower<=20;lower++)
            for(int upper=lower;upper<=20;upper++)
            for(int shift=-10;shift<=10;shift++)
            {
                // A trusted affine relation can be a different signed offset, P2 creates it dynamically.
                var (s,r,a)=Seed(dll,lower,upper,shift:shift);
                var inference=s.DeriveAffineRange(r,a);
                bool actual=inference is not null && s.PermitUpperCheckElimination(inference,I,8) is not null;
                bool expected=TypedBaselineAllows(new RangeRoot(Sc,X,new(lower,upper)),new AffineRoot(Sc,I,X,shift),I,8);
                Require(actual==expected,$"Mismatch: [{lower},{upper}] shift={shift}");
            }
        });
        Console.WriteLine($"P2_LOADED=YES checks={_checks} false_accepts=0 current_ut_commit=NOT_TESTED_BY_THIS_MODEL");
        return 0;
    }
}
