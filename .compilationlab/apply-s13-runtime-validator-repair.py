#!/usr/bin/env python3
from __future__ import annotations

import hashlib
import json
import sys
from pathlib import Path

EXPECTED_BEFORE = {
    "Tools/test-build-topology-runtime.py": "1fc70f905de7e9aacff38e6a1962db44203cdf8af68172acd38de006455a3a14",
    "eng/retired-surface.json": "c510b9c0f97d64d4b2b64a4f671e7323c7cdcbd7b7a47f02f951a927527a118b",
    "Tools/test-retired-surface-mutants.py": "15273336c56570a2d276ae0a6067f23506aca534dd3378428aa822e1929c6ad8",
}
EXPECTED_AFTER = {
    "Tools/test-build-topology-runtime.py": "ce7ebfb1b5a9463428c57c6fa72a42b3de373a70a4d8da19247ea2db8d91142c",
    "eng/retired-surface.json": "f727bc0554a688590cfff29c6a77b14ca8e1ad0a1b6e7704d53abe088c716b0b",
    "Tools/test-retired-surface-mutants.py": "021ed7584eec5ad894e6f98578ae2f8c46a9a353d83e4c9c31ecbf25326dad1d",
}


def digest(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def replace_once(text: str, old: str, new: str, label: str) -> str:
    if text.count(old) != 1:
        raise RuntimeError(f"expected exactly one repair anchor for {label}, found {text.count(old)}")
    return text.replace(old, new, 1)


def main() -> int:
    root = Path(sys.argv[1] if len(sys.argv) > 1 else ".").resolve()
    for relative, expected in EXPECTED_BEFORE.items():
        actual = digest(root / relative)
        if actual != expected:
            raise RuntimeError(f"unexpected preimage for {relative}: {actual}")

    runtime_path = root / "Tools/test-build-topology-runtime.py"
    runtime = runtime_path.read_text(encoding="utf-8")
    runtime = replace_once(runtime, "FRESH_PROCESS_PROJECTS = (", "RETIRED_DIALECT_FRESH_PROCESS_PROJECTS = (", "fresh-process constant")
    runtime = replace_once(
        runtime,
        '''def require_absent_output(\n    search_roots: tuple[Path, ...],\n    pattern: str,\n    case_name: str,\n) -> None:\n    matches = matching_outputs(search_roots, pattern)\n    if matches:\n        raise RuntimeTopologyError(\n            f"unexpected {pattern} for {case_name}; LanguagePack must not rebuild/copy the facade: "\n            + ", ".join(str(path) for path in matches)\n        )\n''',
        '''def require_absent_output(\n    search_roots: tuple[Path, ...],\n    pattern: str,\n    case_name: str,\n    *,\n    configuration: str | None = None,\n) -> None:\n    matches = matching_outputs(search_roots, pattern, configuration=configuration)\n    if matches:\n        raise RuntimeTopologyError(\n            f"unexpected {pattern} for {case_name}: "\n            + ", ".join(str(path) for path in matches)\n        )\n''',
        "absent-output helper",
    )
    runtime = replace_once(
        runtime,
        '''        dialect_directory = (root / DIALECT_TESTS).parent\n        remove_configuration_outputs(dialect_directory, configuration)\n        for relative in FRESH_PROCESS_PROJECTS:\n            remove_configuration_outputs(root / relative, configuration)\n        run_build(args.dotnet, root, DIALECT_TESTS, configuration, build_project_references=False)\n        require_output(\n            (root / FRESH_PROCESS_PROJECTS[-1],),\n            "UniversalToolchain.Dialects.FreshProcessHost.dll",\n            "dialect fresh-process host",\n            configuration=configuration,\n        )\n\n''',
        '''        dialect_directory = (root / DIALECT_TESTS).parent\n        returned_fresh_process_projects = [\n            relative.as_posix()\n            for relative in RETIRED_DIALECT_FRESH_PROCESS_PROJECTS\n            if (root / relative).exists()\n        ]\n        if returned_fresh_process_projects:\n            raise RuntimeTopologyError(\n                "retired dialect fresh-process topology returned: "\n                + ", ".join(returned_fresh_process_projects)\n            )\n        remove_configuration_outputs(dialect_directory, configuration)\n        run_build(args.dotnet, root, DIALECT_TESTS, configuration, build_project_references=False)\n        require_output(\n            (dialect_directory,),\n            "UniversalToolchain.Dialects.Tests.dll",\n            "dialect tests without project-reference rebuilds",\n            configuration=configuration,\n        )\n        require_absent_output(\n            (dialect_directory,),\n            "UniversalToolchain.Dialects.FreshProcessHost.dll",\n            "retired dialect fresh-process host",\n            configuration=configuration,\n        )\n\n''',
        "dialect runtime topology oracle",
    )
    runtime_path.write_text(runtime, encoding="utf-8")

    registry_path = root / "eng/retired-surface.json"
    registry = json.loads(registry_path.read_text(encoding="utf-8"))
    retired_paths = [
        "UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/HostOnlyContractFixture",
        "UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/HostileRuntimeFixture",
        "UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/CanonicalRuntimeFixture",
        "UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/UnregisteredDependencyRuntimeFixture",
        "UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/RuntimeSharedAssemblyFreshProcessHost",
    ]
    for relative in retired_paths:
        if relative in registry["paths"]:
            raise RuntimeError(f"retired path already present before repair: {relative}")
        registry["paths"].append(relative)
    registry_path.write_text(json.dumps(registry, indent=2) + "\n", encoding="utf-8")

    mutants_path = root / "Tools/test-retired-surface-mutants.py"
    mutants = mutants_path.read_text(encoding="utf-8")
    mutants = replace_once(
        mutants,
        '''            "UniversalToolchain/UniversalToolchain.Dialects.Integration",\n            "UniversalToolchain/UniversalToolchain.Dialects.Wist",\n        ]):''',
        '''            "UniversalToolchain/UniversalToolchain.Dialects.Integration",\n            "UniversalToolchain/UniversalToolchain.Dialects.Wist",\n            "UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/RuntimeSharedAssemblyFreshProcessHost",\n        ]):''',
        "retired fresh-process mutant",
    )
    mutants_path.write_text(mutants, encoding="utf-8")

    for relative, expected in EXPECTED_AFTER.items():
        actual = digest(root / relative)
        if actual != expected:
            raise RuntimeError(f"unexpected postimage for {relative}: {actual}; expected {expected}")
        print(f"REPAIR_SHA256={actual} {relative}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
