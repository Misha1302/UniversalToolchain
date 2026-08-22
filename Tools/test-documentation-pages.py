#!/usr/bin/env python3
from __future__ import annotations

import importlib.util
import tempfile
from pathlib import Path

SCRIPT = Path(__file__).with_name("check_documentation_pages.py")
spec = importlib.util.spec_from_file_location("check_documentation_pages", SCRIPT)
assert spec and spec.loader
pages = importlib.util.module_from_spec(spec)
spec.loader.exec_module(pages)

REPOSITORY = "Misha1302/UniversalToolchain"


def write_fixture(root: Path) -> None:
    (root / "docs" / ".vitepress" / "dist" / "assets").mkdir(parents=True)
    (root / "docs" / ".vitepress" / "dist" / "start").mkdir(parents=True)
    (root / "docs" / ".vitepress" / "dist" / "evidence").mkdir(parents=True)
    (root / "docs" / ".vitepress").mkdir(parents=True, exist_ok=True)
    (root / "docs" / ".vitepress" / "config.mts").write_text(
        "export default defineConfig({ base: '/UniversalToolchain/' })\n", encoding="utf-8"
    )
    (root / "docs" / "index.md").write_text(
        '# Home\n\n<a href="./start/">Start</a>\n', encoding="utf-8"
    )
    (root / "docs" / ".vitepress" / "dist" / "index.html").write_text(
        '<link rel="stylesheet" href="/UniversalToolchain/assets/app.css">\n'
        '<a href="/UniversalToolchain/start/">Start</a>\n'
        '<a href="/UniversalToolchain/evidence/wist-stability-v0.1.0-alpha.7">Stability</a>\n',
        encoding="utf-8",
    )
    (root / "docs" / ".vitepress" / "dist" / "assets" / "app.css").write_text("body{}\n", encoding="utf-8")
    (root / "docs" / ".vitepress" / "dist" / "start" / "index.html").write_text("<p>start</p>\n", encoding="utf-8")
    (root / "docs" / ".vitepress" / "dist" / "evidence" / "wist-stability-v0.1.0-alpha.7.html").write_text(
        "<p>stability</p>\n", encoding="utf-8"
    )


def expect_failure(root: Path, needle: str) -> None:
    errors = pages.validate(root, REPOSITORY, check_built=True)
    if not any(needle in error for error in errors):
        raise AssertionError(f"expected failure containing {needle!r}, got: {errors}")


def main() -> int:
    with tempfile.TemporaryDirectory() as temp:
        root = Path(temp)
        write_fixture(root)
        errors = pages.validate(root, REPOSITORY, check_built=True)
        if errors:
            raise AssertionError(f"valid fixture rejected: {errors}")

        config = root / "docs" / ".vitepress" / "config.mts"
        config.write_text("export default defineConfig({ base: '/Wist2/' })\n", encoding="utf-8")
        expect_failure(root, "expected '/UniversalToolchain/'")
        config.write_text("export default defineConfig({ base: '/UniversalToolchain/' })\n", encoding="utf-8")

        source = root / "docs" / "index.md"
        source.write_text('<a href="/UniversalToolchain/start/">Start</a>\n', encoding="utf-8")
        expect_failure(root, "bypasses VitePress base")
        source.write_text('<a href="./start/">Start</a>\n', encoding="utf-8")

        built = root / "docs" / ".vitepress" / "dist" / "index.html"
        built.write_text('<link rel="stylesheet" href="/Wist2/assets/app.css">\n', encoding="utf-8")
        expect_failure(root, "escapes expected Pages base")

        built.write_text('<link rel="stylesheet" href="/UniversalToolchain/assets/missing.css">\n', encoding="utf-8")
        expect_failure(root, "missing built target")

    print(
        "GitHub Pages invariant self-test passed: valid fixture including dotted clean URLs + "
        "base/source/built/missing-target mutants rejected."
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
