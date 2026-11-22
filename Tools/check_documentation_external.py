#!/usr/bin/env python3
"""Bounded live first-contact checks. Exit 0=PASS, 1=FAIL, 2=INCONCLUSIVE.

Offline docs checks remain deterministic. This command reads public endpoints only;
it never changes the pinned installation version or certifies package compatibility.
"""
from __future__ import annotations

import argparse
from datetime import datetime, timezone
import json
from pathlib import Path
import re
import time
import urllib.error
import urllib.request
from urllib.parse import urlsplit
import xml.etree.ElementTree as ET

from check_documentation_release_state import (
    ReleaseStateError, available_versions, child_text, load_state, validate,
)


def http_outcome(status: int | None) -> str:
    if status is not None and 200 <= status < 300:
        return "PASS"
    if status is None or status in {401, 403, 408, 429} or status >= 500:
        return "INCONCLUSIVE"
    return "FAIL"


class RedirectRecorder(urllib.request.HTTPRedirectHandler):
    def __init__(self) -> None:
        super().__init__()
        self.redirects: list[dict[str, object]] = []

    def redirect_request(self, req, fp, code, msg, headers, newurl):
        self.redirects.append({"status": code, "from": req.full_url, "to": newurl})
        return super().redirect_request(req, fp, code, msg, headers, newurl)


def probe(url: str, timeout: float = 8.0) -> tuple[dict[str, object], bytes]:
    """Retry transient failures once, with a bounded response size and timeout."""
    for attempt in (1, 2):
        redirects = RedirectRecorder()
        result: dict[str, object] = {"url": url, "attempt": attempt}
        try:
            request = urllib.request.Request(url, headers={"User-Agent": "UT-docs-first-contact/1"})
            with urllib.request.build_opener(redirects).open(request, timeout=timeout) as response:
                body = response.read(1024 * 1024 + 1)
                result.update(status=response.status, finalUrl=response.geturl(), redirects=redirects.redirects)
                if len(body) > 1024 * 1024:
                    result.update(outcome="INCONCLUSIVE", error="response exceeds 1 MiB limit")
                    return result, b""
                result["outcome"] = http_outcome(response.status)
                return result, body
        except urllib.error.HTTPError as exc:
            result.update(status=exc.code, finalUrl=exc.geturl(), redirects=redirects.redirects,
                          outcome=http_outcome(exc.code), error=str(exc))
            exc.close()
        except (urllib.error.URLError, OSError, TimeoutError) as exc:
            result.update(status=None, finalUrl=url, redirects=redirects.redirects,
                          outcome="INCONCLUSIVE", error=str(exc))
        if result["outcome"] == "FAIL" or attempt == 2:
            return result, b""
        time.sleep(0.5)
    raise AssertionError("unreachable")


def registry_check(body: bytes, state: dict[str, object]) -> dict[str, object]:
    try:
        payload = json.loads(body)
        versions = payload["versions"]
        if not isinstance(versions, list) or not versions or not all(isinstance(v, str) for v in versions):
            raise ValueError("versions must be a non-empty string array")
    except (ValueError, KeyError, TypeError) as exc:
        return {"outcome": "INCONCLUSIVE", "error": f"invalid registry response: {exc}"}
    expected = available_versions(state)
    missing = sorted(set(expected) - set(versions))
    return {
        "outcome": "FAIL" if missing else "PASS",
        "versions": versions,
        "missingSnapshotVersions": missing,
        "newVersionsRequiringReview": sorted(set(versions) - set(expected)),
        "pinnedInstallSmokeVersion": state["publishedVersion"],
    }


def check(root: Path, fetch=probe) -> dict[str, object]:
    state_path = root / "eng/documentation-release-state.json"
    report: dict[str, object] = {
        "checkedAtUtc": datetime.now(timezone.utc).isoformat(timespec="seconds"),
        "scope": "NuGet availability and current-source first-contact URLs; not embedded published READMEs or compatibility",
    }
    try:
        validate(root, state_path)
        state = load_state(state_path)
        project = ET.parse(root / str(state["project"])).getroot()
        site = child_text(project, "PackageProjectUrl") or ""
        if urlsplit(site).scheme != "https" or not urlsplit(site).netloc.endswith(".github.io"):
            raise ValueError("expected an HTTPS GitHub Pages package project URL")
        urls = {site}
        for relative in state["packageReadmeDocuments"]:
            text = (root / relative).read_text(encoding="utf-8")
            urls.update(url for url in re.findall(r"https?://[^\s<>()[\]`]+", text)
                        if urlsplit(url).netloc.endswith(".github.io"))
        if len(urls) > 16:
            raise ValueError("first-contact URL budget exceeded (16)")
    except (OSError, ValueError, TypeError, KeyError, ET.ParseError) as exc:
        report.update(outcome="FAIL", error=f"local contract: {exc}")
        return report

    index, body = fetch(state["nugetAvailability"]["indexUrl"])
    report["index"] = index
    outcomes = [index["outcome"]]
    if index["outcome"] == "PASS":
        registry = registry_check(body, state)
        report["registry"] = registry
        outcomes.append(registry["outcome"])
    links = []
    for url in sorted(urls):
        result, _ = fetch(url)
        links.append(result)
        outcomes.append(result["outcome"])
    report["links"] = links
    report["outcome"] = ("FAIL" if "FAIL" in outcomes else
                         "INCONCLUSIVE" if "INCONCLUSIVE" in outcomes else "PASS")
    return report


def main() -> int:
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--report", type=Path, help="Optional JSON evidence output (outside tracked files).")
    args = parser.parse_args()
    report = check(args.root.resolve())
    text = json.dumps(report, indent=2) + "\n"
    print(text, end="")
    if args.report:
        args.report.write_text(text, encoding="utf-8")
    return {"PASS": 0, "FAIL": 1, "INCONCLUSIVE": 2}[report["outcome"]]


if __name__ == "__main__":
    raise SystemExit(main())
