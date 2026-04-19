#!/usr/bin/env python3
from __future__ import annotations

import shlex
import subprocess
import sys
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


@dataclass(frozen=True)
class BashBlock
{
    file_path: Path
    start_line: int
    attributes: dict[str, str]
    content: str
}


def GetRepositoryRoot() -> Path:
    return Path(__file__).resolve().parents[2]


def GetTrackedMarkdownFiles(repositoryRoot: Path) -> list[Path]:
    result = subprocess.run(
        ["git", "ls-files"],
        cwd=repositoryRoot,
        check=True,
        capture_output=True,
        text=True,
    )

    markdownFiles = [
        repositoryRoot / line
        for line in result.stdout.splitlines()
        if line.endswith(".md")
    ]

    return sorted(markdownFiles)


def ParseFenceAttributes(rawAttributes: str) -> dict[str, str]:
    if rawAttributes == "":
        return {}

    parsed: dict[str, str] = {}
    allowedKeys = {
        "ci-timeout",
        "ci-allowed-exit-codes",
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


def BuildCommand(block: BashBlock, scriptPath: Path) -> list[str]:
    timeoutValue = block.attributes.get("ci-timeout")
    if timeoutValue is None:
        return ["bash", str(scriptPath)]

    return ["timeout", timeoutValue, "bash", str(scriptPath)]


def PrintStream(name: str, value: str) -> None:
    if value == "":
        return

    print(f"[{name}]")
    print(value, end="" if value.endswith("\n") else "\n")


def RunBlock(repositoryRoot: Path, block: BashBlock, blockIndex: int, totalBlocks: int) -> None:
    relativePath = block.file_path.relative_to(repositoryRoot)
    groupName = f"Markdown bash {blockIndex}/{totalBlocks}: {relativePath}:{block.start_line}"

    print(f"::group::{groupName}")
    print(block.content)

    with tempfile.NamedTemporaryFile("w", suffix=".sh", delete=False, encoding="utf-8") as tempScript:
        tempScript.write("set -euo pipefail\n")
        tempScript.write(block.content)
        tempScript.write("\n")
        tempScriptPath = Path(tempScript.name)

    try:
        command = BuildCommand(block, tempScriptPath)
        result = subprocess.run(
            command,
            cwd=repositoryRoot,
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
        print("::endgroup::")


def main() -> int:
    repositoryRoot = GetRepositoryRoot()
    markdownFiles = GetTrackedMarkdownFiles(repositoryRoot)

    allBlocks: list[BashBlock] = []
    for markdownFile in markdownFiles:
        allBlocks.extend(ExtractBashBlocks(markdownFile))

    if len(allBlocks) == 0:
        raise RuntimeError("No bash fenced code blocks were found in tracked markdown files.")

    print(f"Found {len(allBlocks)} bash fenced code blocks across tracked markdown files.")

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
