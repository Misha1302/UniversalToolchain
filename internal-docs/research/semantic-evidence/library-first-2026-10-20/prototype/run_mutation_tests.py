#!/usr/bin/env python3
"""Three strictly local adversarial source mutations; success means each mutant was killed."""
import os, pathlib, shutil, subprocess, tempfile
p = pathlib.Path(__file__).resolve().parent
actual = p/'Contracts'/'SemanticCore.cs'
base = actual.read_text()
mutants = {
    'cross_subject_intersection': ('a.Subject!=b.Subject || a.Scope!=b.Scope', 'false || a.Scope!=b.Scope', 'intersection forbids cross-subject'),
    'cross_subject_affine': ('left.Subject!=rel.Source || left.Scope!=right.Scope', 'false || left.Scope!=right.Scope', 'cross-subject inference is rejected'),
    'spoofed_producer_id': ('producer.GetType()!=selectedType || _revoked', 'false || _revoked', 'spoofed producer ID'),
}
runner_dll = next((p/'Runner'/'bin').rglob('Runner.dll'))
p2_dll = next((p/'ProducerP2'/'bin').rglob('ProducerP2.dll'))
dotnet=os.environ.get('DOTNET','dotnet')
for name,(needle,replacement,expected_label) in mutants.items():
    assert base.count(needle)==1, (name,'mutant location changed')
    with tempfile.TemporaryDirectory(prefix='ut-l3-mutant-') as tmp:
        temp=pathlib.Path(tmp)
        for folder in ('Contracts','ProducerP1','ProducerP2','ConsumerQ','Runner'):
            shutil.copytree(p/folder,temp/folder,ignore=shutil.ignore_patterns('bin','obj'))
        dst=temp/'Contracts'/'SemanticCore.cs'
        dst.write_text(base.replace(needle,replacement))
        build=subprocess.run([dotnet,'build',str(temp/'Runner'/'Runner.csproj'),'-c','Release','--ignore-failed-sources'],capture_output=True,text=True)
        if build.returncode: raise RuntimeError(f'mutant {name} failed to compile: {build.stderr[-900:]} {build.stdout[-900:]}')
        mutant_p2=subprocess.run([dotnet,'build',str(temp/'ProducerP2'/'ProducerP2.csproj'),'-c','Release','--ignore-failed-sources'],capture_output=True,text=True)
        if mutant_p2.returncode: raise RuntimeError(f'P2 build {name} failed')
        run=next((temp/'Runner'/'bin').rglob('Runner.dll'))
        p2=next((temp/'ProducerP2'/'bin').rglob('ProducerP2.dll'))
        result=subprocess.run([dotnet,str(run),str(p2)],capture_output=True,text=True)
        log=p/'logs'/f'mutant_{name}.log'
        log.write_text(f'exit={result.returncode}\nSTDOUT:\n{result.stdout}\nSTDERR:\n{result.stderr}\n')
        assert result.returncode != 0, f'mutant SURVIVED: {name}'
        print(f'KILLED {name}: exit={result.returncode}')
assert actual.read_text()==base
print('MUTANT_CAMPAIGN=3_KILLED; originals_unchanged=YES')
