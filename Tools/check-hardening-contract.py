#!/usr/bin/env python3
from __future__ import annotations

import argparse
from pathlib import Path


def require(text: str, fragment: str, message: str) -> None:
    if fragment not in text:
        raise AssertionError(message)


def forbid(text: str, fragment: str, message: str) -> None:
    if fragment in text:
        raise AssertionError(message)


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path.cwd())
    args = parser.parse_args()
    root = args.root.resolve()

    runtime = (root / "UniversalToolchain/UniversalToolchain.Runtime/LanguageRuntime.cs").read_text(encoding="utf-8-sig")
    build_runtime = (root / "UniversalToolchain/UniversalToolchain.Runtime/LanguageBuildRuntime.cs").read_text(encoding="utf-8-sig")
    failure = (root / "UniversalToolchain/UniversalToolchain.Runtime/RuntimeConstructionFailure.cs").read_text(encoding="utf-8-sig")
    engine = (root / "UniversalToolchain/UniversalToolchain.Wist/WistEngine.cs").read_text(encoding="utf-8-sig")
    classifier = (root / "UniversalToolchain/UniversalToolchain.Wist/WistFailureClassifier.cs").read_text(encoding="utf-8-sig")

    forbid(runtime, "public LanguageArtifactBuildResult Build(", "execution-only LanguageRuntime must not expose Build")
    forbid(runtime, "public LanguageExecutionResult ExecuteBuilt(", "execution-only LanguageRuntime must not expose ExecuteBuilt")
    forbid(runtime, "public T GetBuiltArtifactValue<", "execution-only LanguageRuntime must not expose built artifact extraction")
    require(runtime, "public static LanguageBuildRuntime Create(", "component-source factory must return typed LanguageBuildRuntime")
    require(runtime, "RuntimeConstructionFailure.Rethrow(", "build-runtime construction rollback must preserve primary failure")
    require(build_runtime, "public LanguageArtifactBuildResult Build(", "typed LanguageBuildRuntime must own build capability")

    require(failure, "ExceptionDispatchInfo.Capture(primaryException).Throw();", "primary-only construction failure must preserve stack")
    require(failure, "primaryException", "construction failure aggregate must retain primary exception")
    require(failure, "combined.AddRange(cleanupExceptions);", "cleanup failures must be retained separately after primary")

    expected_guard = "if (!WistFailureClassifier.IsStructuredResultFailure(kind))\n                throw;"
    if engine.count(expected_guard) != 2:
        raise AssertionError("Validate and TryCompile must both fail fast for infrastructure/internal faults")
    require(classifier, "_ => WistFailureKind.Internal", "unclassified framework exceptions must fail closed as Internal")
    require(classifier, "kind is WistFailureKind.UserInput or WistFailureKind.Policy or WistFailureKind.Unsupported", "only expected failure classes may become structured results")

    print("HARDENING_CONTRACT=PASS")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, OSError) as error:
        print(f"HARDENING_CONTRACT=FAIL: {error}")
        raise SystemExit(1)
