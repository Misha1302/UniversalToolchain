#!/usr/bin/env python3
import csv, json, re, subprocess
from pathlib import Path

ROOT = Path(__file__).resolve().parents[3]
STUDY = ROOT / "experiments" / "dsl-evolution-study"
FILES = {
    "control": STUDY / "control" / "ControlEvaluator.cs",
    "clone-own": STUDY / "clone-own" / "CloneEvaluators.cs",
    "universal-toolchain": STUDY / "universal-toolchain" / "UtComposition.cs",
}

def sloc(lines):
    return sum(1 for x in lines if x.strip() and not x.lstrip().startswith("//"))

def block_lines(lines, marker):
    start = next(i for i,x in enumerate(lines) if marker in x)
    brace = 0; seen = False
    out=[]
    for x in lines[start:]:
        out.append(x); brace += x.count("{") - x.count("}")
        if "{" in x: seen=True
        if seen and brace == 0: break
    return out

def normalize(line):
    s=line.strip()
    if not s or s.startswith("using ") or s.startswith("namespace ") or s in {"{","}"}: return None
    s=re.sub(r'"(?:\\.|[^"\\])*"', '"STR"', s)
    s=re.sub(r'\b(?:discount|surcharge)\b', 'FEATURE', s, flags=re.I)
    s=re.sub(r'\b\d+(?:\.\d+)?m?\b', 'NUM', s)
    s=re.sub(r'\s+', ' ', s)
    return s

def duplication(lines, window=3):
    norm=[normalize(x) for x in lines]
    seq=[(i,x) for i,x in enumerate(norm) if x]
    windows={}
    for j in range(len(seq)-window+1):
        idxs=tuple(i for i,_ in seq[j:j+window]); key=tuple(x for _,x in seq[j:j+window])
        windows.setdefault(key,[]).append(idxs)
    duplicated=set()
    for occurrences in windows.values():
        if len(occurrences)>1:
            for occ in occurrences: duplicated.update(occ)
    eligible=sum(1 for x in norm if x)
    return len(duplicated), eligible, (len(duplicated)/eligible if eligible else 0.0)

def e3_churn(path_fragment):
    sha=subprocess.check_output(["git","log","--format=%H","--grep=^Apply frozen E3 percent-range propagation change$","-1"],cwd=ROOT,text=True).strip()
    out=subprocess.check_output(["git","show","--numstat","--format=",sha,"--",path_fragment],cwd=ROOT,text=True).strip()
    if not out: return {"commit":sha,"added":0,"deleted":0,"changed_existing_loc":0,"touched_existing_files":0,"propagation_sites":0}
    a,d,_=out.split("\t",2)
    return {"commit":sha,"added":int(a),"deleted":int(d),"changed_existing_loc":int(a)+int(d),"touched_existing_files":1,"propagation_sites":1}

rows=[]; summary={"schema_version":1,"metrics":{},"method":{"duplication":"Repeated normalized 3-line windows within each treatment source; string literals, numeric literals, and discount/surcharge names normalized; using/namespace/brace-only lines excluded.","loc":"Nonblank non-comment handwritten C# lines.","e3":"Git numstat churn at the frozen E3 commit, per treatment file. PF is number of treatment-local logical rule sites edited."}}
for name,path in FILES.items():
    lines=path.read_text().splitlines(); dup,eligible,ratio=duplication(lines)
    metric={"production_sloc":sloc(lines),"duplicated_lines":dup,"duplication_eligible_lines":eligible,"duplication_ratio":round(ratio,4),"e3":e3_churn(str(path.relative_to(ROOT)))}
    if name=="control": metric["first_case_loc"]=sloc(block_lines(lines,"internal static class ControlEvaluator"))
    if name=="clone-own":
        metric["e1_feature_loc"]=sloc(block_lines(lines,"internal static class DiscountVariant"))
        metric["e2_feature_loc"]=sloc(block_lines(lines,"internal static class SurchargeVariant"))+sloc(block_lines(lines,"internal static class CombinedVariant"))
    if name=="universal-toolchain":
        ad=block_lines(lines,"private static PricingProgram ApplyDiscount")
        au=block_lines(lines,"private static PricingProgram ApplySurcharge")
        rule=block_lines(lines,"internal static class UtPercentRule")
        semantic=sloc(ad)+sloc(au)+sloc(rule)
        metric["semantic_loc"]=semantic
        metric["ceremony_platform_loc"]=metric["production_sloc"]-semantic
        # Registration slice + semantic method, bounded by adjacent fluent calls.
        text='\n'.join(lines)
        disc=text[text.index('.AddFeature("pricing.discount"'):text.index('.AddFeature("pricing.surcharge"')].splitlines()
        sur=text[text.index('.AddFeature("pricing.surcharge"'):text.index('.UseRouteRuntime')].splitlines()
        metric["e1_feature_loc"]=sloc(disc)+sloc(ad)
        metric["e2_feature_loc"]=sloc(sur)+sloc(au)
    summary["metrics"][name]=metric
    rows.append({"treatment":name,**{k:v for k,v in metric.items() if not isinstance(v,dict)},**{f"e3_{k}":v for k,v in metric["e3"].items() if k!="commit"}})

outdir=STUDY/"results"; outdir.mkdir(exist_ok=True)
(outdir/"summary.json").write_text(json.dumps(summary,indent=2)+"\n")
with (outdir/"raw.csv").open("w",newline="") as f:
    w=csv.DictWriter(f, fieldnames=sorted({k for r in rows for k in r}), lineterminator="\n"); w.writeheader(); w.writerows(rows)
(outdir/"raw.json").write_text(json.dumps(rows,indent=2)+"\n")
print(json.dumps(summary,indent=2))
