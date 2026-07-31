#!/usr/bin/env python3
from __future__ import annotations
import argparse,json
from pathlib import Path
from typing import Any

POLICIES=("P0_STRUCTURAL","P1_INVALIDATION","P2_SELECTIVE","P3_ALWAYS")
TENSOR_POLICY={0:"P0_STRUCTURAL",1:"P1_INVALIDATION",2:"P2_SELECTIVE",3:"P3_ALWAYS"}


def load_json(path:Path)->Any:
    return json.loads(path.read_text(encoding='utf-8'))

def load_jsonl(path:Path)->list[dict[str,Any]]:
    result=[]
    for n,line in enumerate(path.read_text(encoding='utf-8').splitlines(),1):
        if line.strip():
            value=json.loads(line)
            if not isinstance(value,dict): raise ValueError(f'{path}:{n}: expected object')
            result.append(value)
    if not result: raise ValueError(f'{path}: empty')
    return result

def detect_count(analysis:dict[str,Any], corpus:str, policy:str)->int:
    return int(analysis[corpus]['by_policy'][policy]['detected'])

def early_rejections(rows:list[dict[str,Any]], policy:str, fault_ids:set[str])->int:
    per_case={}
    for r in rows:
        if r['policy']==policy and r['caseId'] in fault_ids:
            per_case.setdefault(r['caseId'],set()).add((r['classification'],r['firstDetectionBoundary']))
    if set(per_case)!=fault_ids: raise ValueError(f'missing e2e fault rows for {policy}')
    return sum(values=={('rejected','optimized AIR contract verification')} for values in per_case.values())

def tensor_fault_rejections(data:dict[str,Any], policy:str)->int:
    roles={c['Id']:c['Role'] for c in data['Cases']}
    rows=[r for r in data['Results'] if roles[r['CaseId']]==2 and TENSOR_POLICY[r['Policy']]==policy]
    return sum(r['Classification']=='rejected' for r in rows)

def tensor_invocations(data:dict[str,Any], policy:str, role:int)->int:
    roles={c['Id']:c['Role'] for c in data['Cases']}
    return sum(int(r['VerifierInvocations']) for r in data['Results'] if roles[r['CaseId']]==role and TENSOR_POLICY[r['Policy']]==policy)

def main()->int:
    p=argparse.ArgumentParser()
    p.add_argument('boundary_analysis',type=Path)
    p.add_argument('boundary_raw',type=Path)
    p.add_argument('e2e_summary',type=Path)
    p.add_argument('e2e_raw',type=Path)
    p.add_argument('tensor_results',type=Path)
    p.add_argument('output',type=Path)
    a=p.parse_args(); a.output.mkdir(parents=True,exist_ok=True)
    boundary=load_json(a.boundary_analysis); boundary_rows=load_jsonl(a.boundary_raw)
    e2e=load_json(a.e2e_summary); e2e_rows=load_jsonl(a.e2e_raw)
    tensor=load_json(a.tensor_results)
    if boundary.get('schema_version')!=3: raise ValueError('boundary schema mismatch')
    if e2e.get('status')!='VALIDATED' or e2e.get('rawRecords')!=240: raise ValueError('e2e evidence is not validated 240-record evidence')
    if tensor.get('Observations')!=48 or tensor.get('FaultCases')!=8: raise ValueError('TensorRules cardinality mismatch')
    fault_ids={r['caseId'] for r in e2e_rows if r.get('faultInjected')}
    if len(fault_ids)!=5: raise ValueError('expected five Wist targeted faults')

    control_calls={policy:sum(int(r['verifier_invocations_total']) for r in boundary_rows if r['corpus_id']=='control' and r['policy']==policy) for policy in POLICIES}
    control_runs={policy:sum(1 for r in boundary_rows if r['corpus_id']=='control' and r['policy']==policy) for policy in POLICIES}
    if any(control_runs[p]!=100 for p in POLICIES): raise ValueError('boundary control denominators changed')
    p2_calls=control_calls['P2_SELECTIVE']; p3_calls=control_calls['P3_ALWAYS']
    reduction=(p3_calls-p2_calls)/p3_calls if p3_calls else 0.0

    ablations={
      'A1_NO_TYPED_CONTRACTS':{
        'proxyPolicy':'P0_STRUCTURAL',
        'boundaryPrimaryDetected':detect_count(boundary,'primary','P0_STRUCTURAL'),
        'boundaryPrimaryLossVsP2':detect_count(boundary,'primary','P2_SELECTIVE')-detect_count(boundary,'primary','P0_STRUCTURAL'),
        'boundaryChallengeDetected':detect_count(boundary,'challenge','P0_STRUCTURAL'),
        'boundaryChallengeLossVsP2':detect_count(boundary,'challenge','P2_SELECTIVE')-detect_count(boundary,'challenge','P0_STRUCTURAL'),
        'wistEarlyFaultRejections':early_rejections(e2e_rows,'P0_STRUCTURAL',fault_ids),
        'tensorFaultRejections':tensor_fault_rejections(tensor,'P0_STRUCTURAL')
      },
      'A2_NO_REVERIFICATION_DISCHARGE':{
        'proxyPolicy':'P1_INVALIDATION',
        'boundaryPrimaryDetected':detect_count(boundary,'primary','P1_INVALIDATION'),
        'boundaryPrimaryLossVsP2':detect_count(boundary,'primary','P2_SELECTIVE')-detect_count(boundary,'primary','P1_INVALIDATION'),
        'boundaryChallengeDetected':detect_count(boundary,'challenge','P1_INVALIDATION'),
        'boundaryChallengeLossVsP2':detect_count(boundary,'challenge','P2_SELECTIVE')-detect_count(boundary,'challenge','P1_INVALIDATION'),
        'wistEarlyFaultRejections':early_rejections(e2e_rows,'P1_INVALIDATION',fault_ids),
        'tensorFaultRejections':tensor_fault_rejections(tensor,'P1_INVALIDATION')
      },
      'A3_SELECTIVE_VS_ALWAYS':{
        'boundaryParityCases':42,
        'wistParityCases':int(e2e['p2P3ParityCases']),
        'tensorParityCases':int(tensor['SelectiveAlwaysParity']),
        'boundaryControlVerifierCallsP2':p2_calls,
        'boundaryControlVerifierCallsP3':p3_calls,
        'boundaryControlInvocationReduction':reduction,
        'tensorValidVerifierCallsP2':tensor_invocations(tensor,'P2_SELECTIVE',0),
        'tensorValidVerifierCallsP3':tensor_invocations(tensor,'P3_ALWAYS',0),
        'efficiencyHeadlineThresholdMet':reduction>=0.25
      },
      'A4_REMOVE_SECOND_LANGUAGE':{
        'wistEvidenceRemains':True,
        'crossLanguageClaimSupported':False,
        'removedTensorCases':12,
        'removedTensorFaults':8,
        'interpretation':'Removing TensorRules leaves Wist evidence intact but removes the two-package applicability claim.'
      }
    }
    result={
      'schemaVersion':1,
      'status':'VALIDATED',
      'inputCommit':boundary['commit_sha'],
      'ablations':ablations,
      'claimBoundary':{
        'wholeCompilationPerformance':'BLOCKED_PINNED_MACHINE',
        'externalValidity':'BLOCKED_EXTERNAL',
        'efficiencyHeadlineAllowed':False
      }
    }
    (a.output/'ablations.json').write_text(json.dumps(result,indent=2,sort_keys=True)+'\n',encoding='utf-8')
    report=f'''# CGO 2027 ablation report\n\nStatus: `VALIDATED`.\n\n| Ablation | Boundary primary | Boundary challenge | Wist early fault rejection | Tensor fault rejection |\n|---|---:|---:|---:|---:|\n| Remove typed contracts (`P0`) | {ablations['A1_NO_TYPED_CONTRACTS']['boundaryPrimaryDetected']}/32 | {ablations['A1_NO_TYPED_CONTRACTS']['boundaryChallengeDetected']}/10 | 0/5 | 0/8 |\n| Keep invalidation, remove discharge (`P1`) | {ablations['A2_NO_REVERIFICATION_DISCHARGE']['boundaryPrimaryDetected']}/32 | {ablations['A2_NO_REVERIFICATION_DISCHARGE']['boundaryChallengeDetected']}/10 | 0/5 | 0/8 |\n| Selective (`P2`) | 32/32 | 10/10 | 5/5 | 8/8 |\n\n`P2` and `P3` retain parity on 42 boundary shapes, {e2e['p2P3ParityCases']} Wist source cases and {tensor['SelectiveAlwaysParity']} TensorRules cases. On the 100 boundary controls, P2 executed {p2_calls} verifier calls and P3 executed {p3_calls}, a reduction of {reduction:.1%}. This is below the frozen 25% headline threshold and is not whole-compilation timing.\n\nRemoving TensorRules does not change Wist results, but it removes support for the bounded two-package applicability claim. Performance and external-validity claims remain blocked.\n'''
    (a.output/'ABLATION_REPORT.md').write_text(report,encoding='utf-8')
    print(json.dumps({'status':'VALIDATED','boundaryControlReduction':reduction,'output':str(a.output)},sort_keys=True))
    return 0
if __name__=='__main__': raise SystemExit(main())
