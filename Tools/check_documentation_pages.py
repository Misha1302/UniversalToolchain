#!/usr/bin/env python3
from __future__ import annotations

import argparse
import os
import re
import subprocess
import sys
from html.parser import HTMLParser
from pathlib import Path
from urllib.parse import unquote, urlsplit

BASE_RE = re.compile(r"\bbase\s*:\s*['\"]([^'\"]+)['\"]")
RAW_ROOT_URL_RE = re.compile(r"\b(?:href|src)\s*=\s*['\"](/(?!/)[^'\"]*)['\"]", re.IGNORECASE)
INLINE_CODE_RE = re.compile(r"`[^`\n]*`")
FENCE_RE = re.compile(r"^\s*```")


class LocalUrlParser(HTMLParser):
    def __init__(self) -> None:
        super().__init__(convert_charrefs=True)
        self.urls: list[tuple[str, str, str]] = []

    def handle_starttag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        self._collect(tag, attrs)

    def handle_startendtag(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        self._collect(tag, attrs)

    def _collect(self, tag: str, attrs: list[tuple[str, str | None]]) -> None:
        for name, value in attrs:
            if name.lower() in {"href", "src"} and value:
                self.urls.append((tag.lower(), name.lower(), value.strip()))


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Validate GitHub Pages/VitePress deployment invariants.")
    parser.add_argument("--root", type=Path, default=Path(__file__).resolve().parents[1])
    parser.add_argument("--repository", help="GitHub repository in owner/name form. Defaults to GITHUB_REPOSITORY or origin.")
    parser.add_argument("--skip-built", action="store_true", help="Validate source/config invariants without requiring dist/.")
    return parser.parse_args()


def repository_identity(root: Path, explicit: str | None) -> str:
    if explicit:
        value = explicit
    elif os.environ.get("GITHUB_REPOSITORY"):
        value = os.environ["GITHUB_REPOSITORY"]
    else:
        try:
            remote = subprocess.run(
                ["git", "-C", str(root), "config", "--get", "remote.origin.url"],
                check=True,
                capture_output=True,
                text=True,
            ).stdout.strip()
        except (OSError, subprocess.CalledProcessError) as exc:
            raise ValueError("cannot determine repository identity; pass --repository or set GITHUB_REPOSITORY") from exc
        match = re.search(r"github\.com[/:]([^/]+)/([^/]+?)(?:\.git)?$", remote)
        if not match:
            raise ValueError(f"cannot parse GitHub repository from origin URL: {remote!r}")
        value = f"{match.group(1)}/{match.group(2)}"

    parts = value.strip().strip("/").split("/")
    if len(parts) != 2 or not all(parts):
        raise ValueError(f"repository identity must be owner/name, got {value!r}")
    return f"{parts[0]}/{parts[1]}"


def expected_pages_base(repository: str) -> str:
    repo_name = repository.rsplit("/", 1)[1]
    if repo_name.lower().endswith(".github.io"):
        return "/"
    return f"/{repo_name}/"


def vitepress_base(root: Path) -> tuple[str | None, list[str]]:
    config = root / "docs" / ".vitepress" / "config.mts"
    if not config.exists():
        return None, [f"missing VitePress config: {config.relative_to(root)}"]
    matches = BASE_RE.findall(config.read_text(encoding="utf-8"))
    if len(matches) != 1:
        return None, [
            f"docs/.vitepress/config.mts: expected exactly one static `base: '...'` declaration, found {len(matches)}"
        ]
    return matches[0], []


def source_raw_root_urls(root: Path) -> list[str]:
    errors: list[str] = []
    docs = root / "docs"
    for path in sorted(docs.rglob("*.md")):
        if ".vitepress" in path.parts:
            continue
        in_fence = False
        for line_number, line in enumerate(path.read_text(encoding="utf-8").splitlines(), start=1):
            if FENCE_RE.match(line):
                in_fence = not in_fence
                continue
            if in_fence:
                continue
            visible = INLINE_CODE_RE.sub("", line)
            for match in RAW_ROOT_URL_RE.finditer(visible):
                value = match.group(1)
                errors.append(
                    f"{path.relative_to(root)}:{line_number}: raw HTML root-relative URL {value!r} bypasses VitePress base; "
                    "use a Markdown/VitePress link or a relative HTML URL"
                )
    return errors


def _candidate_targets(base: Path, path_text: str, *, allow_clean_url: bool) -> list[Path]:
    target = base / unquote(path_text)
    candidates = [target]
    if path_text.endswith("/"):
        candidates.append(target / "index.html")
    elif allow_clean_url:
        candidates.extend([Path(str(target) + ".html"), target / "index.html"])
    return candidates


def _exists_within_dist(dist: Path, candidates: list[Path]) -> bool:
    dist_resolved = dist.resolve()
    for candidate in candidates:
        try:
            resolved = candidate.resolve()
            resolved.relative_to(dist_resolved)
        except (OSError, ValueError):
            continue
        if resolved.is_file():
            return True
    return False


def built_site_errors(root: Path, expected_base: str) -> list[str]:
    dist = root / "docs" / ".vitepress" / "dist"
    if not dist.is_dir():
        return ["docs/.vitepress/dist is missing; run `npm run docs:build` before the Pages invariant check"]

    errors: list[str] = []
    for html_path in sorted(dist.rglob("*.html")):
        parser = LocalUrlParser()
        parser.feed(html_path.read_text(encoding="utf-8"))
        for tag, attr, raw in parser.urls:
            parsed = urlsplit(raw)
            if parsed.scheme or parsed.netloc or raw.startswith("//"):
                continue
            path_text = parsed.path
            if not path_text:
                continue

            allow_clean_url = tag == "a" and attr == "href"
            if path_text.startswith("/"):
                if expected_base == "/":
                    relative = path_text.lstrip("/")
                elif path_text == expected_base.rstrip("/"):
                    relative = ""
                elif path_text.startswith(expected_base):
                    relative = path_text[len(expected_base):]
                else:
                    errors.append(
                        f"{html_path.relative_to(root)}: <{tag} {attr}={raw!r}> escapes expected Pages base {expected_base!r}"
                    )
                    continue
                candidates = _candidate_targets(dist, relative, allow_clean_url=allow_clean_url)
            else:
                candidates = _candidate_targets(html_path.parent, path_text, allow_clean_url=allow_clean_url)

            if not _exists_within_dist(dist, candidates):
                errors.append(
                    f"{html_path.relative_to(root)}: <{tag} {attr}={raw!r}> points to a missing built target"
                )
    return errors


def validate(root: Path, repository: str, *, check_built: bool) -> list[str]:
    root = root.resolve()
    expected_base = expected_pages_base(repository)
    errors: list[str] = []

    actual_base, base_errors = vitepress_base(root)
    errors.extend(base_errors)
    if actual_base is not None and actual_base != expected_base:
        errors.append(
            f"docs/.vitepress/config.mts: VitePress base is {actual_base!r}, expected {expected_base!r} from repository {repository!r}"
        )

    errors.extend(source_raw_root_urls(root))
    if check_built:
        errors.extend(built_site_errors(root, expected_base))
    return errors


def main() -> int:
    args = parse_args()
    root = args.root.resolve()
    try:
        repository = repository_identity(root, args.repository)
    except ValueError as exc:
        print(f"GitHub Pages invariant check failed: {exc}", file=sys.stderr)
        return 2

    expected_base = expected_pages_base(repository)
    errors = validate(root, repository, check_built=not args.skip_built)
    if errors:
        print("GitHub Pages invariant check failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    phase = "source/config" if args.skip_built else "source/config + built site"
    print(f"GitHub Pages invariant check passed ({phase}): repository={repository}, base={expected_base}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
