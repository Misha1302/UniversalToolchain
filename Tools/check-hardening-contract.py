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
    user_input = (root / "UniversalToolchain/UniversalToolchain.Wist/WistUserInputException.cs").read_text(encoding="utf-8-sig")
    binding = (root / "UniversalToolchain/VariablesModule/VariablesBindingRule.cs").read_text(encoding="utf-8-sig")
    common_failures = (root / "UniversalToolchain/UniversalToolchain.Exceptions/StageExceptions.cs").read_text(encoding="utf-8-sig")
    wist_exception = (root / "UniversalToolchain/CommonExceptions/WistException.cs").read_text(encoding="utf-8-sig")
    diagnostics = (root / "UniversalToolchain/UniversalToolchain.Wist/WistDiagnosticFactory.cs").read_text(encoding="utf-8-sig")

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
    if engine.count(expected_guard) != 3:
        raise AssertionError("both Validate overloads and TryCompile must fail fast for infrastructure/internal faults")
    require(classifier, "_ => WistFailureKind.Internal", "unclassified framework exceptions must fail closed as Internal")
    require(classifier, "WistUserInputException => WistFailureKind.UserInput", "only facade-owned argument validation may be classified as user input")
    require(classifier, "if (Contains<BindingException>(exception))", "binding user-input classification must require a typed marker in the exception chain")
    forbid(classifier, "exception is InvalidOperationException", "arbitrary InvalidOperationException must not be classified as user input")
    forbid(classifier, "ArgumentException => WistFailureKind.UserInput", "arbitrary ArgumentException must not be classified as user input")
    forbid(classifier, "ArgumentException or", "arbitrary ArgumentException must not participate in a user-input pattern")
    require(classifier, "kind is WistFailureKind.UserInput or WistFailureKind.Policy or WistFailureKind.Unsupported", "only expected failure classes may become structured results")
    require(user_input, "internal sealed class WistUserInputException : ArgumentException", "facade user-input failure must stay internal and preserve the ArgumentException family")

    require(common_failures, "public sealed class BindingException : ToolchainException", "binding failures need the generic typed toolchain marker")
    require(wist_exception, "public class WistException : ToolchainException", "Wist facade exceptions must share the generic toolchain diagnostic base")
    forbid(wist_exception, "public new string? Stage", "WistException must not hide ToolchainException.Stage")
    forbid(wist_exception, "public new SourceLocation? Location", "WistException must not hide ToolchainException.Location")
    require(diagnostics, "var toolchainException = exception as ToolchainException;", "Wist diagnostics must project stage/location from generic toolchain exceptions")
    require(binding, "private static InvalidOperationException BindingFailure(string message)", "low-level binding failures must preserve the reviewed InvalidOperationException family")
    require(binding, "new(message, new BindingException(message))", "low-level binding failures must carry the typed marker for facade classification")
    forbid(binding, "throw new TypeSystemException", "VariablesModule must not change the reviewed top-level binding exception family")

    print("HARDENING_CONTRACT=PASS")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except (AssertionError, OSError) as error:
        print(f"HARDENING_CONTRACT=FAIL: {error}")
        raise SystemExit(1)
