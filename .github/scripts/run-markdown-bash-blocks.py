#!/usr/bin/env python3
from __future__ import annotations

import shlex
import os
import re
import shutil
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path

DEFAULT_BASH_BLOCK_TIMEOUT = "60s"
MARKDOWN_DISCOVERY_EXCLUDED_DIRECTORIES = {
    ".git",
    ".hg",
    ".svn",
    ".cache",
    ".idea",
    ".vs",
    ".vscode",
    "artifacts",
    "bin",
    "dist",
    "node_modules",
    "obj",
    "packages",
}


@dataclass(frozen=True)
class BashBlock:
    file_path: Path
    start_line: int
    attributes: dict[str, str]
    content: str


@dataclass(frozen=True)
class BashCommand:
    command: str
    start_line: int
    allowed_exit_codes: set[int]


def GetRepositoryRoot() -> Path:
    return Path(__file__).resolve().parents[2]


def TryGetTrackedMarkdownFiles(repositoryRoot: Path) -> list[Path] | None:
    result = subprocess.run(
        ["git", "ls-files"],
        cwd=repositoryRoot,
        capture_output=True,
        text=True,
    )

    if result.returncode != 0:
        return None

    markdownFiles = [
        repositoryRoot / line
        for line in result.stdout.splitlines()
        if line.endswith(".md")
    ]

    return sorted(markdownFiles)


def IsMarkdownDiscoveryExcluded(markdownFilePath: Path, repositoryRoot: Path) -> bool:
    relativeParts = markdownFilePath.relative_to(repositoryRoot).parts
    return any(part in MARKDOWN_DISCOVERY_EXCLUDED_DIRECTORIES for part in relativeParts)


def GetMarkdownFiles(repositoryRoot: Path) -> list[Path]:
    trackedMarkdownFiles = TryGetTrackedMarkdownFiles(repositoryRoot)
    if trackedMarkdownFiles is not None:
        return trackedMarkdownFiles

    markdownFiles = [
        markdownFilePath
        for markdownFilePath in repositoryRoot.rglob("*.md")
        if not IsMarkdownDiscoveryExcluded(markdownFilePath, repositoryRoot)
    ]

    return sorted(markdownFiles)


def ParseFenceAttributes(rawAttributes: str) -> dict[str, str]:
    if rawAttributes == "":
        return {}

    parsed: dict[str, str] = {}
    allowedKeys = {
        "ci-timeout",
        "ci-allowed-exit-codes",
        "ci-run",
    }

    for token in shlex.split(rawAttributes):
        if "=" not in token:
            raise ValueError(f"Unsupported bash fence attribute token: {token}")

        key, value = token.split("=", 1)
        if key not in allowedKeys:
            raise ValueError(f"Unsupported bash fence attribute: {key}")

        parsed[key] = value

    return parsed


def ExtractBashBlocks(markdownFilePath: Path) -> list[BashBlock]:
    lines = markdownFilePath.read_text(encoding="utf-8").splitlines()

    blocks: list[BashBlock] = []
    currentLines: list[str] = []
    currentAttributes: dict[str, str] | None = None
    currentStartLine: int | None = None

    for index, line in enumerate(lines, start=1):
        strippedLine = line.strip()

        if currentAttributes is None:
            if strippedLine.startswith("```bash"):
                rawAttributes = strippedLine[len("```bash"):].strip()
                currentAttributes = ParseFenceAttributes(rawAttributes)
                currentStartLine = index
                currentLines = []

            continue

        if strippedLine == "```":
            content = "\n".join(currentLines).strip()
            if content != "":
                blocks.append(
                    BashBlock(
                        file_path=markdownFilePath,
                        start_line=currentStartLine or index,
                        attributes=currentAttributes,
                        content=content,
                    )
                )

            currentAttributes = None
            currentStartLine = None
            currentLines = []
            continue

        currentLines.append(line)

    if currentAttributes is not None:
        raise ValueError(f"Unterminated bash fence in {markdownFilePath}")

    return blocks


def ParseAllowedExitCodes(attributes: dict[str, str]) -> set[int]:
    rawValue = attributes.get("ci-allowed-exit-codes")
    if rawValue is None:
        return {0}

    allowedCodes = {
        int(value.strip())
        for value in rawValue.split(",")
        if value.strip() != ""
    }

    if len(allowedCodes) == 0:
        raise ValueError("ci-allowed-exit-codes must contain at least one exit code")

    return allowedCodes


def ParseBooleanAttribute(rawValue: str, attributeName: str) -> bool:
    normalizedValue = rawValue.strip().lower()
    if normalizedValue in {"1", "true", "yes"}:
        return True

    if normalizedValue in {"0", "false", "no"}:
        return False

    raise ValueError(f"{attributeName} must be one of: true, false, 1, 0, yes, no")


def ShouldRunBlock(block: BashBlock) -> bool:
    rawValue = block.attributes.get("ci-run")
    if rawValue is None:
        return True

    return ParseBooleanAttribute(rawValue, "ci-run")


def BuildCommand(block: BashBlock, scriptPath: Path) -> list[str]:
    timeoutValue = block.attributes.get("ci-timeout", DEFAULT_BASH_BLOCK_TIMEOUT)
    return ["timeout", timeoutValue, "bash", str(scriptPath)]


def BuildProcessEnvironment(repositoryRoot: Path) -> dict[str, str]:
    environment = os.environ.copy()
    environment.pop("PLATFORM", None)
    environment.setdefault("DOTNET_CLI_HOME", str(repositoryRoot / ".dotnet-home"))
    environment.setdefault("DOTNET_SKIP_FIRST_TIME_EXPERIENCE", "1")
    environment.setdefault("DOTNET_NOLOGO", "1")
    environment.setdefault("NuGetAudit", "false")

    candidateNuGetPackagePaths = [
        repositoryRoot / "UniversalToolchain" / "packages",
        Path("/workspace/nuget_wist2_minimal_0705_unpack/packages"),
    ]
    if "NUGET_PACKAGES" not in environment:
        for candidateNuGetPackagePath in candidateNuGetPackagePaths:
            if candidateNuGetPackagePath.exists() and any(candidateNuGetPackagePath.iterdir()):
                environment["NUGET_PACKAGES"] = str(candidateNuGetPackagePath)
                break

    if shutil.which("dotnet", path=environment.get("PATH")) is not None:
        return environment

    candidateDotnetPaths = [
        repositoryRoot / ".dotnet" / "dotnet",
        repositoryRoot.parent / ".dotnet" / "dotnet",
        Path("/workspace/.dotnet/dotnet"),
    ]
    for candidateDotnetPath in candidateDotnetPaths:
        if not candidateDotnetPath.exists():
            continue

        dotnetDirectory = str(candidateDotnetPath.parent)
        currentPath = environment.get("PATH", "")
        environment["PATH"] = dotnetDirectory if currentPath == "" else f"{dotnetDirectory}{os.pathsep}{currentPath}"
        return environment

    return environment


def PrintStream(name: str, value: str) -> None:
    if value == "":
        return

    print(f"[{name}]")
    print(value, end="" if value.endswith("\n") else "\n")


DOTNET_RUN_PROJECT_PATTERN = re.compile(r"(^|\s)dotnet\s+run\s+--project\s+([^\s\\]+)")


def FindRunnableDotnetProjects(blocks: list[BashBlock]) -> list[str]:
    projects: set[str] = set()
    for block in blocks:
        if not ShouldRunBlock(block):
            continue

        for match in DOTNET_RUN_PROJECT_PATTERN.finditer(block.content):
            projects.add(match.group(2))

    return sorted(projects)


def EnsureDotnetRunProjectsAreBuilt(repositoryRoot: Path, blocks: list[BashBlock]) -> None:
    projects = FindRunnableDotnetProjects(blocks)
    if len(projects) == 0:
        return

    environment = BuildProcessEnvironment(repositoryRoot)
    for project in projects:
        if not project.endswith(".csproj"):
            continue

        projectPath = repositoryRoot / project
        if not projectPath.exists():
            continue

        print(f"::group::Markdown prebuild: {project}")
        try:
            result = subprocess.run(
                [
                    "dotnet",
                    "build",
                    project,
                    "-c",
                    "Debug",
                    "-p:NuGetAudit=false",
                ],
                cwd=repositoryRoot,
                env=environment,
                capture_output=True,
                text=True,
            )
            PrintStream("stdout", result.stdout)
            PrintStream("stderr", result.stderr)
            if result.returncode != 0:
                raise RuntimeError(
                    f"Markdown prebuild failed for {project} with exit code {result.returncode}."
                )
        finally:
            print("::endgroup::")


def RewriteDotnetRunLineForCi(line: str) -> str:
    strippedLine = line.lstrip()
    indentation = line[: len(line) - len(strippedLine)]
    if strippedLine.startswith("dotnet run --project "):
        return indentation + strippedLine.replace(
            "dotnet run --project ",
            "dotnet run --no-restore --no-build --project ",
            1,
        )

    return line


def RewriteScriptContentForCi(content: str) -> str:
    return "\n".join(RewriteDotnetRunLineForCi(line) for line in content.splitlines())


def HasCommandDirectives(block: BashBlock) -> bool:
    return any(line.strip().startswith("# ci:") for line in block.content.splitlines())


def ParseDirectiveExitCodes(rawValue: str) -> set[int]:
    allowedCodes = {
        int(value.strip())
        for value in rawValue.split(",")
        if value.strip() != ""
    }

    if len(allowedCodes) == 0:
        raise ValueError("expect-exit directive must contain at least one exit code")

    return allowedCodes


def ParseCommandDirectives(block: BashBlock) -> list[BashCommand]:
    if "ci-allowed-exit-codes" in block.attributes:
        raise ValueError(
            "bash fence cannot mix ci-allowed-exit-codes with line-level '# ci:' directives"
        )

    commands: list[BashCommand] = []
    pendingAllowedExitCodes = {0}
    pendingDirectiveLine: int | None = None

    for offset, line in enumerate(block.content.splitlines(), start=1):
        lineNumber = block.start_line + offset
        strippedLine = line.strip()

        if strippedLine == "":
            continue

        if strippedLine.startswith("# ci:"):
            directiveText = strippedLine[len("# ci:"):].strip()
            if directiveText == "":
                raise ValueError(
                    f"Empty CI directive in {block.file_path}:{lineNumber}"
                )

            directiveParts = shlex.split(directiveText)
            currentAllowedExitCodes: set[int] | None = None

            for token in directiveParts:
                if "=" not in token:
                    raise ValueError(
                        f"Unsupported CI directive token '{token}' in {block.file_path}:{lineNumber}"
                    )

                key, value = token.split("=", 1)
                if key != "expect-exit":
                    raise ValueError(
                        f"Unsupported CI directive '{key}' in {block.file_path}:{lineNumber}"
                    )

                currentAllowedExitCodes = ParseDirectiveExitCodes(value)

            if currentAllowedExitCodes is None:
                raise ValueError(
                    f"Directive in {block.file_path}:{lineNumber} did not define any expectation"
                )

            pendingAllowedExitCodes = currentAllowedExitCodes
            pendingDirectiveLine = lineNumber
            continue

        if strippedLine.startswith("#"):
            continue

        if strippedLine.endswith("\\"):
            raise ValueError(
                f"Line-level CI directives only support single-line commands, but {block.file_path}:{lineNumber} uses a line continuation."
            )

        commands.append(
            BashCommand(
                command=line,
                start_line=lineNumber,
                allowed_exit_codes=pendingAllowedExitCodes,
            )
        )
        pendingAllowedExitCodes = {0}
        pendingDirectiveLine = None

    if pendingDirectiveLine is not None:
        raise ValueError(
            f"Dangling CI directive without a following command in {block.file_path}:{pendingDirectiveLine}"
        )

    if len(commands) == 0:
        raise ValueError(
            f"No executable commands were found in directive-driven block {block.file_path}:{block.start_line}"
        )

    return commands


def RunCommand(repositoryRoot: Path, block: BashBlock, command: BashCommand) -> None:
    relativePath = block.file_path.relative_to(repositoryRoot)
    print(command.command)

    with tempfile.NamedTemporaryFile("w", suffix=".sh", delete=False, encoding="utf-8") as tempScript:
        tempScript.write("set -euo pipefail\n")
        tempScript.write(RewriteScriptContentForCi(command.command))
        tempScript.write("\n")
        tempScriptPath = Path(tempScript.name)

    try:
        result = subprocess.run(
            BuildCommand(block, tempScriptPath),
            cwd=repositoryRoot,
            env=BuildProcessEnvironment(repositoryRoot),
            capture_output=True,
            text=True,
        )

        PrintStream("stdout", result.stdout)
        PrintStream("stderr", result.stderr)

        if result.returncode not in command.allowed_exit_codes:
            allowedCodesText = ", ".join(str(code) for code in sorted(command.allowed_exit_codes))
            raise RuntimeError(
                f"Unexpected exit code for {relativePath}:{command.start_line}. "
                f"Expected one of [{allowedCodesText}], got {result.returncode}."
            )
    finally:
        tempScriptPath.unlink(missing_ok=True)


def RunWholeBlock(repositoryRoot: Path, block: BashBlock) -> None:
    relativePath = block.file_path.relative_to(repositoryRoot)
    print(block.content)

    with tempfile.NamedTemporaryFile("w", suffix=".sh", delete=False, encoding="utf-8") as tempScript:
        tempScript.write("set -euo pipefail\n")
        tempScript.write(RewriteScriptContentForCi(block.content))
        tempScript.write("\n")
        tempScriptPath = Path(tempScript.name)

    try:
        command = BuildCommand(block, tempScriptPath)
        result = subprocess.run(
            command,
            cwd=repositoryRoot,
            env=BuildProcessEnvironment(repositoryRoot),
            capture_output=True,
            text=True,
        )

        PrintStream("stdout", result.stdout)
        PrintStream("stderr", result.stderr)

        allowedExitCodes = ParseAllowedExitCodes(block.attributes)
        if result.returncode not in allowedExitCodes:
            allowedCodesText = ", ".join(str(code) for code in sorted(allowedExitCodes))
            raise RuntimeError(
                f"Unexpected exit code for {relativePath}:{block.start_line}. "
                f"Expected one of [{allowedCodesText}], got {result.returncode}."
            )
    finally:
        tempScriptPath.unlink(missing_ok=True)


def RunBlock(repositoryRoot: Path, block: BashBlock, blockIndex: int, totalBlocks: int) -> None:
    relativePath = block.file_path.relative_to(repositoryRoot)
    groupName = f"Markdown bash {blockIndex}/{totalBlocks}: {relativePath}:{block.start_line}"

    print(f"::group::{groupName}")

    try:
        if not ShouldRunBlock(block):
            print("[skipped] ci-run=false")
            print(block.content)
            return

        if HasCommandDirectives(block):
            for command in ParseCommandDirectives(block):
                RunCommand(repositoryRoot, block, command)
        else:
            RunWholeBlock(repositoryRoot, block)
    finally:
        print("::endgroup::")


def main() -> int:
    repositoryRoot = GetRepositoryRoot()
    markdownFiles = GetMarkdownFiles(repositoryRoot)

    allBlocks: list[BashBlock] = []
    for markdownFile in markdownFiles:
        allBlocks.extend(ExtractBashBlocks(markdownFile))

    if len(allBlocks) == 0:
        raise RuntimeError("No bash fenced code blocks were found in markdown files.")

    EnsureDotnetRunProjectsAreBuilt(repositoryRoot, allBlocks)

    print(f"Found {len(allBlocks)} bash fenced code blocks across markdown files.")

    for index, block in enumerate(allBlocks, start=1):
        RunBlock(repositoryRoot, block, index, len(allBlocks))

    print("All markdown bash blocks completed successfully.")
    return 0


if __name__ == "__main__":
    try:
        raise SystemExit(main())
    except Exception as exception:
        print(str(exception), file=sys.stderr)
        raise
