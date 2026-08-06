#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import os
import re
import shutil
import subprocess
import tempfile
from pathlib import Path
from xml.sax.saxutils import escape

DEFAULT_STATE = Path("eng/documentation-release-state.json")
SOURCE_MARKER_BEGIN = "<!-- wist-source-candidate:begin -->"
SOURCE_MARKER_END = "<!-- wist-source-candidate:end -->"
CSHARP_FENCE = re.compile(r"```csharp[^\n]*\n(.*?)\n```", re.DOTALL)
LOCAL_ARTIFACT_FEED = re.compile(r"(?:--source\s+)?\.?/?artifacts/packages", re.IGNORECASE)
VALIDATION_MESSAGE_ACCESS = re.compile(r"\b(?:validation|rejected)\.Message\b")
RUNTIME_ASSEMBLY_COUNT = re.compile(
    r"\b(?:The\s+)?(?P<count>\d+)\s+assemblies\s+under\s+`lib/net10\.0`",
    re.IGNORECASE,
)


class FirstContactError(ValueError):
    pass


def require_string(data: dict[str, object], key: str) -> str:
    value = data.get(key)
    if not isinstance(value, str) or not value.strip():
        raise FirstContactError(f"release state field {key!r} must be a non-empty string")
    return value.strip()


def require_string_list(data: dict[str, object], key: str) -> list[str]:
    value = data.get(key)
    if not isinstance(value, list) or not value or not all(
        isinstance(item, str) and item.strip() for item in value
    ):
        raise FirstContactError(f"release state field {key!r} must be a non-empty string list")
    return [item.strip() for item in value]


def load_state(path: Path) -> dict[str, object]:
    try:
        data = json.loads(path.read_text(encoding="utf-8"))
    except (OSError, json.JSONDecodeError) as exc:
        raise FirstContactError(f"cannot read release state {path}: {exc}") from exc
    if not isinstance(data, dict) or data.get("schemaVersion") != 1:
        raise FirstContactError("unsupported documentation release-state schema")
    return data


def marked_block(text: str, begin: str, end: str, document: Path) -> str:
    if text.count(begin) != 1 or text.count(end) != 1:
        raise FirstContactError(
            f"{document}: expected exactly one marker pair {begin!r} / {end!r}"
        )
    return text.split(begin, 1)[1].split(end, 1)[0]


def validate_package_readmes(root: Path, state: dict[str, object]) -> list[Path]:
    package_id = require_string(state, "packageId")
    source_version = require_string(state, "sourceVersion")
    package_readmes = require_string_list(state, "packageReadmeDocuments")
    source_documents = set(require_string_list(state, "sourceCandidateDocuments"))

    missing_from_source_contract = sorted(set(package_readmes) - source_documents)
    if missing_from_source_contract:
        raise FirstContactError(
            "package README documents are not release-state source candidates: "
            + ", ".join(missing_from_source_contract)
        )

    paths: list[Path] = []
    install = re.compile(
        rf"dotnet\s+add\s+package\s+{re.escape(package_id)}\b"
        rf"(?:(?!dotnet\s+add\s+package).)*?--version\s+{re.escape(source_version)}\b",
        re.DOTALL,
    )
    for relative in package_readmes:
        document = root / relative
        if not document.is_file():
            raise FirstContactError(f"package README does not exist: {relative}")
        text = document.read_text(encoding="utf-8")
        candidate = marked_block(text, SOURCE_MARKER_BEGIN, SOURCE_MARKER_END, document)
        if source_version not in candidate:
            raise FirstContactError(
                f"{relative}: source-candidate block does not identify {source_version}"
            )
        if not any(
            phrase in candidate.lower()
            for phrase in ("not published", "unpublished", "not on nuget.org")
        ):
            raise FirstContactError(
                f"{relative}: source candidate is not explicitly labeled unpublished"
            )
        if not install.search(text):
            raise FirstContactError(
                f"{relative}: package-facing install command does not pin {package_id} {source_version}"
            )
        if LOCAL_ARTIFACT_FEED.search(text):
            raise FirstContactError(
                f"{relative}: package README must not assume a repository-local artifacts/packages feed"
            )
        paths.append(document)
    return paths


def validate_first_contact_text(root: Path, package_readmes: list[Path]) -> None:
    documents = [root / "readme.md", *package_readmes]
    surface_path = root / "eng/wist-package-surface.json"
    try:
        surface = json.loads(surface_path.read_text(encoding="utf-8"))
        expected_runtime_count = len(surface["runtimeAssemblies"])
    except (OSError, json.JSONDecodeError, KeyError, TypeError) as exc:
        raise FirstContactError(f"cannot read runtime package surface: {exc}") from exc

    for document in documents:
        text = document.read_text(encoding="utf-8")
        bad_member = VALIDATION_MESSAGE_ACCESS.search(text)
        if bad_member:
            raise FirstContactError(
                f"{document.relative_to(root)}: validation result has no aggregate Message property: "
                f"{bad_member.group(0)}"
            )
        for match in RUNTIME_ASSEMBLY_COUNT.finditer(text):
            actual = int(match.group("count"))
            if actual != expected_runtime_count:
                raise FirstContactError(
                    f"{document.relative_to(root)}: stale runtime assembly count {actual}; "
                    f"expected {expected_runtime_count} from eng/wist-package-surface.json"
                )


def extract_snippets(root: Path, state: dict[str, object]) -> list[tuple[str, str]]:
    raw_specs = state.get("firstContactCsharpSnippets")
    if not isinstance(raw_specs, list) or not raw_specs:
        raise FirstContactError("firstContactCsharpSnippets must be a non-empty list")

    snippets: list[tuple[str, str]] = []
    for index, raw in enumerate(raw_specs):
        if not isinstance(raw, dict):
            raise FirstContactError(f"snippet specification {index} must be an object")
        relative = raw.get("document")
        marker = raw.get("marker")
        if not isinstance(relative, str) or not relative.strip():
            raise FirstContactError(f"snippet specification {index} has no document")
        if not isinstance(marker, str) or not marker.strip():
            raise FirstContactError(f"snippet specification {index} has no marker")
        document = root / relative
        if not document.is_file():
            raise FirstContactError(f"snippet document does not exist: {relative}")
        begin = f"<!-- {marker}:begin -->"
        end = f"<!-- {marker}:end -->"
        block = marked_block(document.read_text(encoding="utf-8"), begin, end, document)
        matches = CSHARP_FENCE.findall(block)
        if len(matches) != 1:
            raise FirstContactError(
                f"{relative}: marker {marker!r} must contain exactly one C# fence"
            )
        snippets.append((f"{relative}:{marker}", matches[0].strip() + "\n"))
    return snippets


def split_usings(code: str) -> tuple[list[str], str]:
    lines = code.splitlines()
    usings: list[str] = []
    body_start = 0
    for index, line in enumerate(lines):
        stripped = line.strip()
        if not stripped:
            body_start = index + 1
            continue
        if stripped.startswith("using ") and stripped.endswith(";") and "=" not in stripped:
            usings.append(stripped)
            body_start = index + 1
            continue
        break
    return usings, "\n".join(lines[body_start:]).strip()


def compile_snippets(
    root: Path,
    state: dict[str, object],
    snippets: list[tuple[str, str]],
    dotnet: str,
) -> None:
    project = (root / require_string(state, "project")).resolve()
    if not project.is_file():
        raise FirstContactError(f"Wist project does not exist: {project}")
    executable = shutil.which(dotnet)
    if executable is None:
        raise FirstContactError(f"dotnet executable not found: {dotnet}")

    with tempfile.TemporaryDirectory(prefix="wist-doc-snippets-") as temp_name:
        temp = Path(temp_name)
        csproj = temp / "DocumentationSnippets.csproj"
        csproj.write_text(
            "<Project Sdk=\"Microsoft.NET.Sdk\">\n"
            "  <PropertyGroup>\n"
            "    <TargetFramework>net10.0</TargetFramework>\n"
            "    <ImplicitUsings>enable</ImplicitUsings>\n"
            "    <Nullable>enable</Nullable>\n"
            "    <LangVersion>14</LangVersion>\n"
            "  </PropertyGroup>\n"
            "  <ItemGroup>\n"
            f"    <ProjectReference Include=\"{escape(str(project))}\" />\n"
            "  </ItemGroup>\n"
            "</Project>\n",
            encoding="utf-8",
        )

        for index, (label, code) in enumerate(snippets):
            usings, body = split_usings(code)
            if not body:
                raise FirstContactError(f"{label}: C# snippet has no executable body")
            indented = "\n".join("        " + line if line else "" for line in body.splitlines())
            source = "\n".join(usings)
            if source:
                source += "\n\n"
            source += (
                "namespace WistDocumentationSnippets;\n\n"
                f"internal static class Snippet{index}\n"
                "{\n"
                "    internal static void CompileOnly()\n"
                "    {\n"
                f"{indented}\n"
                "    }\n"
                "}\n"
            )
            (temp / f"Snippet{index}.cs").write_text(source, encoding="utf-8")

        env = os.environ.copy()
        env.update(
            {
                "DOTNET_CLI_TELEMETRY_OPTOUT": "1",
                "DOTNET_NOLOGO": "1",
                "NuGetAudit": "false",
            }
        )
        completed = subprocess.run(
            [
                executable,
                "build",
                str(csproj),
                "-c",
                "Release",
                "--nologo",
                "-v:minimal",
                "-p:NuGetAudit=false",
            ],
            cwd=root,
            env=env,
            text=True,
            stdout=subprocess.PIPE,
            stderr=subprocess.STDOUT,
            check=False,
        )
        if completed.returncode != 0:
            labels = ", ".join(label for label, _ in snippets)
            raise FirstContactError(
                f"first-contact C# snippets failed to compile ({labels}):\n{completed.stdout}"
            )


def validate(root: Path, state_path: Path, *, skip_compile: bool, dotnet: str) -> dict[str, int]:
    state = load_state(state_path)
    package_readmes = validate_package_readmes(root, state)
    validate_first_contact_text(root, package_readmes)
    snippets = extract_snippets(root, state)
    if not skip_compile:
        compile_snippets(root, state, snippets, dotnet)
    return {"packageReadmes": len(package_readmes), "snippets": len(snippets)}


def main() -> int:
    parser = argparse.ArgumentParser(
        description="Validate Wist package README and compile first-contact documentation snippets."
    )
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--state", type=Path, default=DEFAULT_STATE)
    parser.add_argument("--skip-compile", action="store_true")
    parser.add_argument("--dotnet", default=os.environ.get("DOTNET", "dotnet"))
    args = parser.parse_args()

    root = args.root.resolve()
    state_path = args.state.resolve() if args.state.is_absolute() else (root / args.state).resolve()
    try:
        result = validate(root, state_path, skip_compile=args.skip_compile, dotnet=args.dotnet)
    except (OSError, FirstContactError) as exc:
        print(f"wist-documentation-first-contact: ERROR: {exc}", file=os.sys.stderr)
        return 1

    suffix = "static-only" if args.skip_compile else "compiled"
    print(
        "wist-documentation-first-contact: PASS "
        f"package-readmes={result['packageReadmes']} snippets={result['snippets']} mode={suffix}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
