#!/usr/bin/env python3
from __future__ import annotations

import argparse
import json
import re
from pathlib import Path


def fail(message: str) -> None:
    raise SystemExit(message)


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    args = parser.parse_args()
    root = args.root.resolve()
    owner_path = root / "eng/ci-required-workflows.json"
    data = json.loads(owner_path.read_text(encoding="utf-8"))

    if data.get("schemaVersion") != 1:
        fail("ci-required-workflows.json: unsupported schemaVersion")
    if data.get("allowedConclusions") != ["success"]:
        fail("CI aggregate must be fail-closed: only conclusion 'success' is allowed")

    required = data.get("requiredForCodeAcceptance")
    if not isinstance(required, list) or not required:
        fail("CI contract has no required workflows")

    names: set[str] = set()
    paths: set[str] = set()
    for item in required:
        name = item.get("name")
        relative = item.get("workflow")
        if not isinstance(name, str) or not name.strip() or not isinstance(relative, str):
            fail(f"invalid required workflow entry: {item!r}")
        if name in names or relative in paths:
            fail(f"duplicate required workflow identity: {name!r} / {relative!r}")
        names.add(name)
        paths.add(relative)

        workflow_path = root / relative
        if not workflow_path.is_file():
            fail(f"required workflow is missing: {relative}")
        text = workflow_path.read_text(encoding="utf-8")
        match = re.search(r"(?m)^name:\s*(.+?)\s*$", text)
        if match is None or match.group(1).strip(" '\"") != name:
            fail(f"required workflow name drift in {relative}: expected {name!r}")
        if not re.search(r"(?m)^\s*push:\s*$", text):
            fail(f"required workflow does not run on push: {relative}")
        if "master" not in text and '"**"' not in text and "'**'" not in text:
            fail(f"required workflow does not cover master pushes: {relative}")

    aggregate = (root / ".github/workflows/ci-aggregate.yml").read_text(encoding="utf-8")
    if "eng/ci-required-workflows.json" not in aggregate:
        fail("ci-aggregate.yml does not consume the canonical CI owner")
    if "allowedConclusions" not in aggregate:
        fail("ci-aggregate.yml does not consume canonical allowed conclusions")
    for forbidden in ("'skipped'", '"skipped"', "'neutral'", '"neutral"'):
        if forbidden in aggregate:
            fail(f"ci-aggregate.yml contains fail-open conclusion {forbidden}")

    non_blocking = data.get("nonBlockingWorkflows", [])
    for item in non_blocking:
        if item.get("name") in names:
            fail(f"non-blocking workflow is also required: {item.get('name')!r}")

    pages_wiring = {
        ".github/workflows/docs-check.yml": (
            "npm run docs:pages-selftest",
            "npm run docs:pages",
        ),
        ".github/workflows/deploy-docs.yml": (
            "npm run docs:pages",
        ),
    }
    for relative, required_commands in pages_wiring.items():
        workflow = root / relative
        if not workflow.is_file():
            fail(f"GitHub Pages invariant workflow is missing: {relative}")
        text = workflow.read_text(encoding="utf-8")
        for command in required_commands:
            if command not in text:
                fail(f"{relative}: GitHub Pages invariant gate is not wired: missing `{command}`")

    print(f"CI contract OK: {len(required)} fail-closed required workflows")


if __name__ == "__main__":
    main()
