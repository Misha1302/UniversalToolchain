#!/usr/bin/env python3
from __future__ import annotations

from pathlib import Path
import re

ROOT = Path(__file__).resolve().parents[1]


def replace_markdown_prose(path: Path, direct: dict[str, str]) -> None:
    text = path.read_text(encoding="utf-8")
    for old, new in direct.items():
        text = text.replace(old, new)

    output: list[str] = []
    in_fence = False
    for line in text.splitlines(keepends=True):
        if line.lstrip().startswith("```"):
            in_fence = not in_fence
            output.append(line)
            continue
        if in_fence:
            output.append(line)
            continue

        parts = line.split("`")
        for index in range(0, len(parts), 2):
            value = parts[index]
            for old, new in direct.items():
                value = value.replace(old, new)
            value = re.sub(r"\bPreview\b", "Alpha", value)
            value = re.sub(r"\bpreview\b", "alpha", value)
            parts[index] = value
        output.append("`".join(parts))

    path.write_text("".join(output), encoding="utf-8")


def replace_in_file(relative: str, replacements: list[tuple[str, str]]) -> None:
    path = ROOT / relative
    text = path.read_text(encoding="utf-8")
    for old, new in replacements:
        text = text.replace(old, new)
    path.write_text(text, encoding="utf-8")


def main() -> None:
    old_status = ROOT / "docs/public/what-is-stable-in-preview.md"
    new_status = ROOT / "docs/public/what-is-stable-in-alpha.md"
    if not old_status.exists():
        raise RuntimeError(f"Missing expected status file: {old_status}")
    old_status.rename(new_status)

    direct = {
        "docs/public/what-is-stable-in-preview.md": "docs/public/what-is-stable-in-alpha.md",
        "/public/what-is-stable-in-preview": "/public/what-is-stable-in-alpha",
        "what-is-stable-in-preview": "what-is-stable-in-alpha",
        "What is stable in preview?": "What is stable in alpha?",
        "Preview stability": "Alpha stability",
        "one-off previews": "one-off trial runs",
        "admin previews": "admin trial runs",
        "Offline artifact preview": "Offline artifact inspection",
        "offline artifact preview": "offline artifact inspection",
        "Preview.5 v9": "Legacy cycle 5 / v9",
        "Preview.4 v8": "Legacy cycle 4 / v8",
        "Preview.3 boundary": "Legacy cycle 3 boundary",
        "Preview.3 host controls": "Alpha host controls",
        "preview.3": "legacy cycle 3",
        "preview.2": "earlier facade release",
    }

    for path in ROOT.rglob("*.md"):
        if ".git" not in path.parts:
            replace_markdown_prose(path, direct)

    code_edits = {
        "UniversalToolchain/UniversalToolchain.Wist/WistOptimizationOptions.cs": [
            ("preview releases", "alpha releases")
        ],
        "UniversalToolchain/UniversalToolchain.Wist/WistValidationResult.cs": [
            ("compatibility with the preview.2 facade", "compatibility with earlier facade releases")
        ],
        "UniversalToolchain/UniversalToolchain.Ssa.Abstractions/SsaDescriptors.cs": [
            ('"preview ', '"alpha ')
        ],
        "UniversalToolchain/UniversalToolchain.Ssa.Optimization/SsaRouteProfile.cs": [
            ("Core preview type descriptors", "Core alpha-stage type descriptors")
        ],
        "UniversalToolchain/UniversalToolchain.Ssa.Optimization/SsaRuntimeExecution.cs": [
            ("preview SSA optimizer module", "alpha SSA optimizer module")
        ],
        "UniversalToolchain/UniversalToolchain.Ssa.Optimization/SsaSparseConditionalConstantPropagationPass.cs": [
            ("for preview SSA", "for alpha SSA")
        ],
    }
    for relative, replacements in code_edits.items():
        replace_in_file(relative, replacements)

    test_paths = [
        "UniversalToolchain/Tests/Ssa/AirToSsaConverterTests.cs",
        "UniversalToolchain/Tests/Ssa/SsaOptimizationTests.cs",
        "UniversalToolchain/Tests/Ssa/SsaRouteIntegrationRegressionTests.cs",
        "UniversalToolchain/Tests/Ssa/SsaToAirConverterTests.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Tests/Wist/PublicFacade/WistEngineSmokeTests.cs",
        "UniversalToolchain/UniversalToolchain.Dialects.Tests/Wist/PublicFacade/WistPublicApiBaselineTests.cs",
    ]
    for relative in test_paths:
        path = ROOT / relative
        text = path.read_text(encoding="utf-8")
        path.write_text(text.replace("Preview", "Alpha").replace("preview", "alpha"), encoding="utf-8")

    checker = ROOT / "Tools/check_documentation_status.py"
    text = checker.read_text(encoding="utf-8")
    marker = "        ROOT / 'docs/CURRENT_ARCHITECTURE_STATUS.md',\n"
    required = "        ROOT / 'docs/public/what-is-stable-in-alpha.md',\n"
    if required not in text:
        if marker not in text:
            raise RuntimeError("Could not locate required-files insertion point")
        text = text.replace(marker, marker + required)

    pattern = re.compile(
        r"    prepublish_phrases = \[.*?(?=    docs_index = ROOT / 'docs/index\.md')",
        re.DOTALL,
    )
    replacement = """    forbidden_legacy_stage_phrases = [
        'what-is-stable-in-preview',
        'public preview',
        'current preview',
        'preview package',
        'Wist facade preview',
        'Preview status',
    ]
    for path in markdown_files:
        text = path.read_text(encoding='utf-8')
        for phrase in forbidden_legacy_stage_phrases:
            if phrase in text:
                errors.append(f'{rel(path)}: legacy preview-stage wording remains: {phrase}')

"""
    text, count = pattern.subn(replacement, text, count=1)
    if count != 1:
        raise RuntimeError(f"Expected one documentation checker block, replaced {count}")
    checker.write_text(text, encoding="utf-8")


if __name__ == "__main__":
    main()
