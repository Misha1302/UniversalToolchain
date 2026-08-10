#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import subprocess
import tempfile
from pathlib import Path


def run(checker: Path, root: Path, registry: Path) -> int:
    return subprocess.run(["python3", str(checker), "--root", str(root), "--registry", str(registry)], check=False, stdout=subprocess.PIPE, stderr=subprocess.STDOUT, text=True).returncode


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    source_root = args.root.resolve()
    checker = source_root / "Tools/check-retired-surface.py"
    registry = source_root / "eng/retired-surface.json"
    data = json.loads(registry.read_text(encoding="utf-8"))
    with tempfile.TemporaryDirectory(prefix="retired-surface-mutants-") as tmp_name:
        tmp = Path(tmp_name)
        temp_registry = tmp / "retired-surface.json"
        temp_registry.write_text(json.dumps(data), encoding="utf-8")
        for index, relative in enumerate([
            "UniversalToolchain/LocalVariablesOptimizerModule/LocalVariablesOptimizerModule.csproj",
            "UniversalToolchain/UniversalToolchain.Dialects.Wist/WistDialectExecutionConfiguration.cs",
            "UniversalToolchain/BasicCore/Core/BasicCoreImpl.cs",
            "UniversalToolchain/BasicCore/Core/PreparedExecutionBuilder.cs",
            "UniversalToolchain/UniversalToolchain.Dialects.Integration",
            "UniversalToolchain/UniversalToolchain.Dialects.Wist",
            "UniversalToolchain/UniversalToolchain.Dialects.Tests/FreshProcess/RuntimeSharedAssemblyFreshProcessHost",
            "UniversalToolchain/UniversalToolchain.Dialects.ManifestEmitter",
            "UniversalToolchain/UniversalToolchain.Dialects.Abstractions/DialectRuntimeExportAttribute.cs",
        ]):
            mutant = tmp / f"path-{index}"
            target = mutant / relative
            target.parent.mkdir(parents=True, exist_ok=True)
            target.write_text("retired", encoding="utf-8")
            if run(checker, mutant, temp_registry) == 0:
                raise RuntimeError(f"retired path mutant survived: {relative}")
            print(f"SURVIVOR=0 mutant=retired-path-{index}")
        for index, symbol in enumerate([
            "ToolchainRuntimeHost",
            "DialectRuntimeExportAttribute",
            "DialectModuleAliasAttribute",
            "DialectBackendDeclaration",
        ]):
            mutant = tmp / f"symbol-{index}"
            source = mutant / "UniversalToolchain/Example/Returned.cs"
            source.parent.mkdir(parents=True, exist_ok=True)
            source.write_text(f"internal sealed class {symbol} {{}}", encoding="utf-8")
            if run(checker, mutant, temp_registry) == 0:
                raise RuntimeError(f"retired symbol mutant survived: {symbol}")
            print(f"SURVIVOR=0 mutant=retired-symbol-{index}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
