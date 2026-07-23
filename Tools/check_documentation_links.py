#!/usr/bin/env python3
from __future__ import annotations

import re
import sys
from collections import defaultdict
from pathlib import Path
from urllib.parse import unquote

ROOT = Path(__file__).resolve().parents[1]
DOCS = ROOT / "docs"
INTERNAL = ROOT / "internal-docs"
LINK_RE = re.compile(r"!?\[[^\]]*\]\(([^)]+)\)")
SIDEBAR_LINK_RE = re.compile(r"\blink:\s*['\"]([^'\"]+)['\"]")
HEADING_RE = re.compile(r"^#{1,6}\s+(.+?)\s*$")
FENCE_RE = re.compile(r"^```")
INLINE_CODE_RE = re.compile(r"`([^`\n]+)`")
FRONT_MATTER_RE = re.compile(r"\A---\n(.*?)\n---\n", re.DOTALL)
REPOSITORY_PATH_PREFIXES = (
    "docs/",
    "internal-docs/",
    "Tools/",
    "UniversalToolchain/",
    "samples/",
    "eng/",
    ".github/",
    "readme.md",
    "VERIFICATION.md",
    "AGENTS.md",
    "build.sh",
    "build.ps1",
    "package.json",
)
INTERNAL_CURRENT_FILES = (
    INTERNAL / "policies-and-reports" / "DOCUMENTATION_INDEX.md",
    INTERNAL / "policies-and-reports" / "PROJECT_RULES.md",
    INTERNAL / "policies-and-reports" / "ARCHITECTURE_RULES.md",
    INTERNAL / "policies-and-reports" / "DOCUMENTATION_RULES.md",
    INTERNAL / "policies-and-reports" / "SYNTAX_OWNERSHIP_RULES.md",
    INTERNAL / "policies-and-reports" / "project-positioning.md",
    INTERNAL / "policies-and-reports" / "public-claim-ledger.md",
    INTERNAL / "policies-and-reports" / "technical-debt.md",
)
INTERNAL_CURRENT_ROOTS = (INTERNAL / "maintainers",)
INTERNAL_PUBLIC_NAMES = {
    "archive",
    "reviews",
    "proposals",
    "talks",
    "maintainers",
    "contracts",
    "vision",
}


def markdown_files(base: Path) -> list[Path]:
    if not base.exists():
        return []
    return sorted(path for path in base.rglob("*.md") if ".vitepress" not in path.parts)


def split_target(raw: str) -> tuple[str, str]:
    target = raw.strip()
    if target.startswith("<") and target.endswith(">"):
        target = target[1:-1]
    target = re.split(r"\s+['\"]", target, maxsplit=1)[0]
    target = unquote(target)
    if "#" in target:
        path, anchor = target.split("#", 1)
        return path, anchor
    return target, ""


def slugify(text: str) -> str:
    text = re.sub(r"<[^>]+>", "", text)
    text = re.sub(r"[`*_~]", "", text).strip().lower()
    text = re.sub(r"[^\w\-\s]", "", text, flags=re.UNICODE)
    return re.sub(r"[\s_]+", "-", text).strip("-")


def anchors(path: Path) -> set[str]:
    result: set[str] = set()
    in_fence = False
    counts: dict[str, int] = {}
    for line in path.read_text(encoding="utf-8").splitlines():
        if FENCE_RE.match(line.strip()):
            in_fence = not in_fence
            continue
        if in_fence:
            continue
        match = HEADING_RE.match(line)
        if not match:
            continue
        base = slugify(match.group(1))
        if not base:
            continue
        count = counts.get(base, 0)
        counts[base] = count + 1
        result.add(base if count == 0 else f"{base}-{count}")
    return result


def front_matter(path: Path) -> dict[str, str]:
    text = path.read_text(encoding="utf-8")
    match = FRONT_MATTER_RE.match(text)
    if not match:
        return {}
    result: dict[str, str] = {}
    for raw in match.group(1).splitlines():
        if ":" not in raw:
            continue
        key, value = raw.split(":", 1)
        result[key.strip()] = value.strip().strip("'\"")
    return result


def resolve_page(root: Path, path_text: str) -> Path:
    if path_text in {"", "/"}:
        return root / "index.md"
    rel = path_text.lstrip("/")
    candidate = root / rel
    if candidate.is_dir():
        return candidate / "index.md"
    if candidate.exists():
        return candidate
    if candidate.suffix.lower() in {".md", ".html", ".png", ".jpg", ".jpeg", ".svg", ".txt", ".json"}:
        return candidate
    for possible in (Path(str(candidate) + ".md"), candidate / "index.md"):
        if possible.exists():
            return possible
    return Path(str(candidate) + ".md")


def resolve_target(source: Path, target_path: str) -> Path | None:
    if not target_path:
        return source
    if target_path.startswith(("http://", "https://", "mailto:", "tel:", "data:")):
        return None
    if target_path.startswith("/"):
        page = resolve_page(DOCS, target_path)
        if page.exists():
            return page
        return DOCS / "public" / target_path.lstrip("/")
    return (source.parent / target_path).resolve()


def check_markdown_links(files: list[Path], public: bool) -> tuple[list[str], dict[Path, set[Path]]]:
    errors: list[str] = []
    incoming: dict[Path, set[Path]] = defaultdict(set)
    anchor_cache: dict[Path, set[str]] = {}
    for source in files:
        text = source.read_text(encoding="utf-8")
        for match in LINK_RE.finditer(text):
            raw = match.group(1)
            target_path, anchor = split_target(raw)
            if target_path.startswith(("http://", "https://", "mailto:", "tel:", "data:")):
                continue
            target = resolve_target(source, target_path)
            if target is None:
                continue
            try:
                target.relative_to(ROOT)
            except ValueError:
                errors.append(f"{source.relative_to(ROOT)}: link escapes repository: {raw}")
                continue
            if public:
                try:
                    target.relative_to(INTERNAL)
                except ValueError:
                    pass
                else:
                    errors.append(f"{source.relative_to(ROOT)}: public Markdown link points to repository-only documentation: {raw}")
                    continue
            if not target.exists():
                errors.append(f"{source.relative_to(ROOT)}: missing local target: {raw} -> {target.relative_to(ROOT)}")
                continue
            incoming[target.resolve()].add(source.resolve())
            if anchor and target.suffix.lower() == ".md":
                available = anchor_cache.setdefault(target, anchors(target))
                if anchor not in available:
                    errors.append(f"{source.relative_to(ROOT)}: missing anchor '#{anchor}' in {target.relative_to(ROOT)}")
    return errors, incoming


def normalize_inline_repository_path(raw: str) -> str | None:
    value = raw.strip().strip(".,;:()[]{}")
    if not value.startswith(REPOSITORY_PATH_PREFIXES):
        return None
    if any(token in value for token in ("<", ">", "*", "$", "{", "}", "|", " -> ", " ")):
        return None
    value = value.split("#", 1)[0]
    value = re.sub(r":\d+(?:-\d+)?$", "", value)
    return value.rstrip("/") or None


def check_inline_repository_paths(files: list[Path]) -> list[str]:
    errors: list[str] = []
    for source in files:
        text = source.read_text(encoding="utf-8")
        in_fence = False
        for line_number, line in enumerate(text.splitlines(), start=1):
            if FENCE_RE.match(line.strip()):
                in_fence = not in_fence
                continue
            if in_fence:
                continue
            for raw in INLINE_CODE_RE.findall(line):
                relative = normalize_inline_repository_path(raw)
                if relative is None:
                    continue
                target = ROOT / relative
                if not target.exists():
                    errors.append(
                        f"{source.relative_to(ROOT)}:{line_number}: missing repository path in inline code: `{relative}`"
                    )
    return errors


def check_sidebar() -> tuple[list[str], set[Path]]:
    errors: list[str] = []
    targets: set[Path] = set()
    config = DOCS / ".vitepress" / "config.mts"
    text = config.read_text(encoding="utf-8")
    for link in SIDEBAR_LINK_RE.findall(text):
        if link.startswith(("http://", "https://")):
            continue
        target = resolve_page(DOCS, link)
        if not target.exists():
            errors.append(f"docs/.vitepress/config.mts: sidebar/nav target missing: {link} -> {target.relative_to(ROOT)}")
        else:
            targets.add(target.resolve())
    required_prefixes = (
        "'/start/'",
        "'/language-authoring/'",
        "'/build-dsls/'",
        "'/write-modules/'",
        "'/architecture/'",
        "'/reference/'",
        "'/evidence/'",
    )
    for prefix in required_prefixes:
        if prefix not in text:
            errors.append(f"docs/.vitepress/config.mts: missing path-specific sidebar prefix {prefix}")
    return errors, targets


def check_orphans(public_files: list[Path], incoming: dict[Path, set[Path]], sidebar_targets: set[Path]) -> list[str]:
    errors: list[str] = []
    for path in public_files:
        resolved = path.resolve()
        if resolved in sidebar_targets or incoming.get(resolved):
            continue
        metadata = front_matter(path)
        if metadata.get("navigation") == "hidden":
            continue
        errors.append(
            f"{path.relative_to(ROOT)}: public page is not in navigation and has no incoming public link; "
            "link it or add front matter `navigation: hidden` with a status reason"
        )
    return errors


def main() -> int:
    errors: list[str] = []
    for name in sorted(INTERNAL_PUBLIC_NAMES):
        path = DOCS / name
        if path.exists():
            errors.append(f"docs/{name}: internal documentation must live under internal-docs/")

    public_markdown = markdown_files(DOCS)
    current_internal = sorted(
        [path for path in INTERNAL_CURRENT_FILES if path.exists()]
        + [
            path
            for base in INTERNAL_CURRENT_ROOTS
            if base.exists()
            for path in base.rglob("*.md")
        ]
    )

    link_errors, incoming = check_markdown_links(public_markdown, public=True)
    errors.extend(link_errors)
    internal_link_errors, _ = check_markdown_links(current_internal, public=False)
    errors.extend(internal_link_errors)
    errors.extend(check_inline_repository_paths(public_markdown + current_internal))

    sidebar_errors, sidebar_targets = check_sidebar()
    errors.extend(sidebar_errors)
    errors.extend(check_orphans(public_markdown, incoming, sidebar_targets))

    public_static_markdown = sorted((DOCS / "public").glob("*.md")) if (DOCS / "public").exists() else []
    for path in public_static_markdown:
        errors.append(f"{path.relative_to(ROOT)}: Markdown must not be stored in VitePress public/ static directory")

    if errors:
        print("Documentation link/split/navigation check failed:", file=sys.stderr)
        for error in errors:
            print(f"- {error}", file=sys.stderr)
        return 1

    print(
        f"Documentation link/split/navigation check passed: {len(public_markdown)} public Markdown files, "
        f"{len(markdown_files(INTERNAL))} internal Markdown files, {len(sidebar_targets)} navigated targets."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
