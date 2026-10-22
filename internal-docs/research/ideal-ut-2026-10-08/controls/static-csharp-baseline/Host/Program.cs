using Shared; using Shape; using Range; using Consumer;
var checkCount=0;
void Check(string caseId, bool observed, bool expected) { if (observed!=expected) throw new Exception(caseId+" got "+observed+" expected "+expected); Console.WriteLine(caseId+" = "+observed);checkCount++; }
Query good = new("example",1,2,5,"sha-A",true,true);
bool Decide(Query q,params IFactProvider[] p) => new BoundsOptimization(new SimpleSafetyQuery(p)).CanElideCheck(q);
var shape = new ShapeProvider();var range = new RangeProvider();
Check("shape-only-retains",Decide(good,shape),false);
Check("independent-range-enables",Decide(good,shape,range),true);
Check("higher-precision-range-substitution",Decide(good,shape,new PreciseRangeProvider()),true);
Check("baseline-index-zero-conservative",Decide(good with { Index=0 },shape,range),false);
Check("precise-index-zero-enabled",Decide(good with { Index=0 },shape,new PreciseRangeProvider()),true);
Check("invalid-index-retains",Decide(good with { Index=5 },shape,range),false);
Check("negative-index-retains",Decide(good with { Index=-1 },shape,range),false);
Check("conflict-retains",Decide(good,shape,range,new ConflictingRangeProvider()),false);
Check("mutation-retains",Decide(good with { LengthStable=false },shape,range),false);
Check("aliasing-retains",Decide(good with { AliasingSafe=false },shape,range),false);
Check("stale-revision-retains",Decide(good,shape,new StaleProvider(range)),false);
Check("changed-implementation-retains",Decide(good,shape,new WrongHashProvider(range)),false);
Console.WriteLine("PASS: "+checkCount+" decision assertions; no UT runtime, LLVM, MLIR or machine-code optimizer invoked.");
sealed class StaleProvider(IFactProvider inner):IFactProvider {public string Domain=>inner.Domain;public Observation Query(Query q)=>inner.Query(q) with {Revision=q.Revision-1};}
sealed class WrongHashProvider(IFactProvider inner):IFactProvider {public string Domain=>inner.Domain;public Observation Query(Query q)=>inner.Query(q) with {ImplementationHash="sha-B"};}
