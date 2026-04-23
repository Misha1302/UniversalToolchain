#!/usr/bin/env python3
from __future__ import annotations

import argparse
import csv
import html
import math
import re
import statistics
import sys
from collections import defaultdict
from dataclasses import dataclass
from pathlib import Path
from typing import Iterable


TIME_UNIT_TO_NS = {
    "ns": 1.0,
    "μs": 1_000.0,
    "us": 1_000.0,
    "ms": 1_000_000.0,
    "s": 1_000_000_000.0,
}

MEMORY_UNIT_TO_BYTES = {
    "B": 1.0,
    "KB": 1024.0,
    "MB": 1024.0 * 1024.0,
    "GB": 1024.0 * 1024.0 * 1024.0,
}

METHOD_CLASS_MAP = {
    "Wist": "method-wist",
    "CSharp": "method-csharp",
    "NCalc": "method-ncalc",
}


@dataclass(frozen=True)
class BenchmarkRow:
    benchmark_name: str
    method: str
    mean_raw: str
    mean_ns: float
    error_raw: str
    error_ns: float | None
    stddev_raw: str
    stddev_ns: float | None
    ratio: float | None
    ratio_sd: float | None
    rank: int | None
    allocated_raw: str
    allocated_bytes: float | None
    alloc_ratio: str
    runtime: str
    job: str
    source_file: str


@dataclass(frozen=True)
class MethodSummary:
    method: str
    benchmarks_count: int
    wins: int
    average_mean_ns: float
    median_mean_ns: float
    geometric_mean_relative_to_best: float
    average_relative_to_best: float


@dataclass(frozen=True)
class ParsedPathResult:
    rows: list[BenchmarkRow]
    source_files: list[Path]


TIME_RE = re.compile(r"^\s*([0-9]+(?:[.,][0-9]+)?)\s*(ns|μs|us|ms|s)\s*$")
MEMORY_RE = re.compile(r"^\s*([0-9]+(?:[.,][0-9]+)?)\s*(B|KB|MB|GB)\s*$", re.IGNORECASE)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(
        description=(
            "Build a pretty standalone HTML report from BenchmarkDotNet CSV files. "
            "You can pass a directory, a CSV file, or multiple paths."
        )
    )
    parser.add_argument(
        "paths",
        nargs="*",
        help="Directory and/or CSV files with BenchmarkDotNet results.",
    )
    parser.add_argument(
        "-o",
        "--output",
        default="benchmark-report.html",
        help="Path to output HTML file. Default: benchmark-report.html",
    )
    parser.add_argument(
        "--title",
        default="Benchmark Report",
        help="Title displayed in the generated HTML page.",
    )
    return parser.parse_args()


def prompt_for_paths_if_needed(paths: list[str]) -> list[str]:
    if paths:
        return paths

    entered = input(
        "Enter a path to a BenchmarkDotNet results folder or CSV file "
        "(you can also paste multiple paths separated by ';'): "
    ).strip()

    if not entered:
        raise SystemExit("No input path was provided.")

    return [part.strip() for part in entered.split(";") if part.strip()]


def discover_csv_files(paths: Iterable[str]) -> list[Path]:
    discovered: list[Path] = []
    seen: set[Path] = set()

    for raw_path in paths:
        path = Path(raw_path).expanduser().resolve()

        if not path.exists():
            print(f"[warning] Path does not exist: {path}", file=sys.stderr)
            continue

        if path.is_dir():
            candidates = sorted(path.rglob("*.csv"))
            candidates = [candidate for candidate in candidates if candidate.name.endswith("-report.csv")]
        elif path.is_file() and path.suffix.lower() == ".csv":
            candidates = [path]
        else:
            print(f"[warning] Unsupported path skipped: {path}", file=sys.stderr)
            continue

        for candidate in candidates:
            candidate = candidate.resolve()
            if candidate not in seen:
                seen.add(candidate)
                discovered.append(candidate)

    discovered.sort()
    return discovered


def parse_time_to_ns(raw: str) -> float | None:
    if raw is None:
        return None

    text = raw.strip()
    if not text or text.upper() == "NA" or text == "-":
        return None

    match = TIME_RE.match(text)
    if not match:
        return None

    value = float(match.group(1).replace(",", "."))
    unit = match.group(2)
    return value * TIME_UNIT_TO_NS[unit]


def parse_memory_to_bytes(raw: str) -> float | None:
    if raw is None:
        return None

    text = raw.strip()
    if not text or text.upper() == "NA" or text == "-":
        return None

    match = MEMORY_RE.match(text)
    if not match:
        return None

    value = float(match.group(1).replace(",", "."))
    unit = match.group(2).upper()
    return value * MEMORY_UNIT_TO_BYTES[unit]


def parse_float(raw: str) -> float | None:
    if raw is None:
        return None

    text = raw.strip()
    if not text or text.upper() == "NA" or text == "-":
        return None

    try:
        return float(text.replace(",", "."))
    except ValueError:
        return None


def parse_int(raw: str) -> int | None:
    number = parse_float(raw)
    if number is None:
        return None
    return int(number)


def benchmark_name_from_file(path: Path) -> str:
    name = path.stem
    suffix = "-report"
    if name.endswith(suffix):
        name = name[: -len(suffix)]

    name = re.sub(r"^UniversalToolchain\.Benchmarks\.", "", name)
    return name


def read_csv_file(path: Path) -> list[BenchmarkRow]:
    rows: list[BenchmarkRow] = []
    benchmark_name = benchmark_name_from_file(path)

    with path.open("r", encoding="utf-8-sig", newline="") as file:
        reader = csv.DictReader(file)

        for csv_row in reader:
            method = (csv_row.get("Method") or "").strip()
            mean_raw = (csv_row.get("Mean") or "").strip()

            if not method or not mean_raw:
                continue

            rows.append(
                BenchmarkRow(
                    benchmark_name=benchmark_name,
                    method=method,
                    mean_raw=mean_raw,
                    mean_ns=parse_time_to_ns(mean_raw) or math.nan,
                    error_raw=(csv_row.get("Error") or "").strip(),
                    error_ns=parse_time_to_ns(csv_row.get("Error") or ""),
                    stddev_raw=(csv_row.get("StdDev") or "").strip(),
                    stddev_ns=parse_time_to_ns(csv_row.get("StdDev") or ""),
                    ratio=parse_float(csv_row.get("Ratio") or ""),
                    ratio_sd=parse_float(csv_row.get("RatioSD") or ""),
                    rank=parse_int(csv_row.get("Rank") or ""),
                    allocated_raw=(csv_row.get("Allocated") or "").strip(),
                    allocated_bytes=parse_memory_to_bytes(csv_row.get("Allocated") or ""),
                    alloc_ratio=(csv_row.get("Alloc Ratio") or "").strip(),
                    runtime=(csv_row.get("Runtime") or "").strip(),
                    job=(csv_row.get("Job") or "").strip(),
                    source_file=str(path),
                )
            )

    return rows


def parse_inputs(paths: list[str]) -> ParsedPathResult:
    files = discover_csv_files(paths)
    if not files:
        raise SystemExit("No BenchmarkDotNet CSV files were found.")

    all_rows: list[BenchmarkRow] = []
    for path in files:
        all_rows.extend(read_csv_file(path))

    if not all_rows:
        raise SystemExit("CSV files were found, but no benchmark rows could be parsed.")

    all_rows.sort(key=lambda row: (row.benchmark_name.lower(), row.mean_ns, row.method.lower()))
    return ParsedPathResult(rows=all_rows, source_files=files)


def group_rows_by_benchmark(rows: Iterable[BenchmarkRow]) -> dict[str, list[BenchmarkRow]]:
    grouped: dict[str, list[BenchmarkRow]] = defaultdict(list)
    for row in rows:
        grouped[row.benchmark_name].append(row)

    for benchmark_rows in grouped.values():
        benchmark_rows.sort(key=lambda row: (row.mean_ns, row.method.lower()))

    return dict(sorted(grouped.items(), key=lambda pair: pair[0].lower()))


def build_method_summaries(grouped_rows: dict[str, list[BenchmarkRow]]) -> list[MethodSummary]:
    mean_values: dict[str, list[float]] = defaultdict(list)
    relative_values: dict[str, list[float]] = defaultdict(list)
    wins: dict[str, int] = defaultdict(int)

    for benchmark_rows in grouped_rows.values():
        if not benchmark_rows:
            continue

        best_mean = min(row.mean_ns for row in benchmark_rows)
        best_methods = {row.method for row in benchmark_rows if row.mean_ns == best_mean}

        for method in best_methods:
            wins[method] += 1

        for row in benchmark_rows:
            mean_values[row.method].append(row.mean_ns)
            relative_values[row.method].append(row.mean_ns / best_mean)

    summaries: list[MethodSummary] = []
    all_methods = sorted(mean_values.keys(), key=str.lower)

    for method in all_methods:
        relative_list = relative_values[method]
        geometric_mean = math.exp(sum(math.log(value) for value in relative_list) / len(relative_list))
        summaries.append(
            MethodSummary(
                method=method,
                benchmarks_count=len(mean_values[method]),
                wins=wins[method],
                average_mean_ns=statistics.mean(mean_values[method]),
                median_mean_ns=statistics.median(mean_values[method]),
                geometric_mean_relative_to_best=geometric_mean,
                average_relative_to_best=statistics.mean(relative_list),
            )
        )

    summaries.sort(
        key=lambda item: (
            item.geometric_mean_relative_to_best,
            item.average_relative_to_best,
            item.average_mean_ns,
            item.method.lower(),
        )
    )
    return summaries


def method_css_class(method: str) -> str:
    for prefix, css_class in METHOD_CLASS_MAP.items():
        if method.startswith(prefix):
            return css_class
    return "method-generic"


def escape(value: object) -> str:
    return html.escape(str(value))


def format_ratio(value: float | None) -> str:
    if value is None:
        return "—"
    return f"{value:.2f}×"


def format_relative_percent(value: float) -> str:
    delta_percent = (value - 1.0) * 100.0
    if abs(delta_percent) < 0.05:
        return "~ equal"
    if delta_percent < 0:
        return f"{abs(delta_percent):.1f}% faster"
    return f"{delta_percent:.1f}% slower"


def format_ns(value: float) -> str:
    if value >= 1_000_000_000.0:
        return f"{value / 1_000_000_000.0:.3f} s"
    if value >= 1_000_000.0:
        return f"{value / 1_000_000.0:.3f} ms"
    if value >= 1_000.0:
        return f"{value / 1_000.0:.3f} μs"
    return f"{value:.1f} ns"


def format_bytes(value: float | None, raw: str) -> str:
    if value is None:
        return raw or "—"
    if value >= 1024.0 * 1024.0 * 1024.0:
        return f"{value / (1024.0 * 1024.0 * 1024.0):.2f} GB"
    if value >= 1024.0 * 1024.0:
        return f"{value / (1024.0 * 1024.0):.2f} MB"
    if value >= 1024.0:
        return f"{value / 1024.0:.2f} KB"
    return f"{value:.0f} B"


def build_overview_cards(
    source_files: list[Path],
    grouped_rows: dict[str, list[BenchmarkRow]],
    method_summaries: list[MethodSummary],
) -> str:
    fastest_method = method_summaries[0] if method_summaries else None
    method_count = len(method_summaries)
    benchmark_count = len(grouped_rows)

    runtime_values = sorted(
        {
            row.runtime
            for benchmark_rows in grouped_rows.values()
            for row in benchmark_rows
            if row.runtime
        }
    )

    runtime_text = ", ".join(runtime_values) if runtime_values else "Unknown"

    cards = [
        ("CSV files", str(len(source_files))),
        ("Benchmarks", str(benchmark_count)),
        ("Methods", str(method_count)),
        ("Runtime", runtime_text),
    ]

    if fastest_method is not None:
        cards.append(
            (
                "Overall leader",
                f"{fastest_method.method} · {format_ratio(fastest_method.geometric_mean_relative_to_best)} vs best",
            )
        )

    return "\n".join(
        f"<div class='card'><div class='label'>{escape(label)}</div><div class='value'>{escape(value)}</div></div>"
        for label, value in cards
    )


def build_method_summary_table(method_summaries: list[MethodSummary]) -> str:
    rows_html: list[str] = []

    for index, summary in enumerate(method_summaries, start=1):
        rows_html.append(
            """
            <tr>
                <td>{place}</td>
                <td><span class='method-pill {method_class}'>{method}</span></td>
                <td>{wins}</td>
                <td>{benchmarks_count}</td>
                <td>{avg_mean}</td>
                <td>{median_mean}</td>
                <td>{geo_ratio}</td>
                <td>{avg_ratio}</td>
                <td>{speed_note}</td>
            </tr>
            """.format(
                place=index,
                method_class=method_css_class(summary.method),
                method=escape(summary.method),
                wins=summary.wins,
                benchmarks_count=summary.benchmarks_count,
                avg_mean=escape(format_ns(summary.average_mean_ns)),
                median_mean=escape(format_ns(summary.median_mean_ns)),
                geo_ratio=escape(format_ratio(summary.geometric_mean_relative_to_best)),
                avg_ratio=escape(format_ratio(summary.average_relative_to_best)),
                speed_note=escape(format_relative_percent(summary.geometric_mean_relative_to_best)),
            )
        )

    return "\n".join(rows_html)


def build_benchmark_sections(grouped_rows: dict[str, list[BenchmarkRow]]) -> tuple[str, str]:
    nav_items: list[str] = []
    sections: list[str] = []

    for benchmark_name, rows in grouped_rows.items():
        anchor = make_anchor(benchmark_name)
        nav_items.append(f"<a href='#{anchor}'>{escape(benchmark_name)}</a>")

        best_mean = min(row.mean_ns for row in rows)
        worst_mean = max(row.mean_ns for row in rows)
        span = worst_mean - best_mean
        scale_max = worst_mean if worst_mean > 0 else 1.0

        table_rows: list[str] = []
        for row in rows:
            relative_to_best = row.mean_ns / best_mean
            width_percent = max(8.0, (row.mean_ns / scale_max) * 100.0)
            delta_note = format_relative_percent(relative_to_best)
            speed_badge = "winner-badge" if abs(relative_to_best - 1.0) < 1e-12 else "normal-badge"

            table_rows.append(
                """
                <tr>
                    <td><span class='method-pill {method_class}'>{method}</span></td>
                    <td>{mean}</td>
                    <td>{error}</td>
                    <td>{stddev}</td>
                    <td>{ratio}</td>
                    <td>{rank}</td>
                    <td>{allocated}</td>
                    <td>
                        <div class='bar-cell'>
                            <div class='bar-track'>
                                <div class='bar-fill {method_class}' style='width:{width_percent:.2f}%'></div>
                            </div>
                            <div class='bar-text'><span class='{badge_class}'>{delta_note}</span></div>
                        </div>
                    </td>
                </tr>
                """.format(
                    method_class=method_css_class(row.method),
                    method=escape(row.method),
                    mean=escape(row.mean_raw),
                    error=escape(row.error_raw or "—"),
                    stddev=escape(row.stddev_raw or "—"),
                    ratio=escape(format_ratio(row.ratio if row.ratio is not None else relative_to_best)),
                    rank=escape(row.rank if row.rank is not None else "—"),
                    allocated=escape(format_bytes(row.allocated_bytes, row.allocated_raw)),
                    width_percent=width_percent,
                    delta_note=escape(delta_note),
                    badge_class=speed_badge,
                )
            )

        sections.append(
            """
            <section class='benchmark-section' id='{anchor}'>
                <div class='section-header'>
                    <div>
                        <h2>{benchmark_name}</h2>
                        <div class='section-meta'>
                            Best: {best_mean} · Spread: {spread} · Source: {source_file}
                        </div>
                    </div>
                    <a class='back-link' href='#top'>↑ top</a>
                </div>
                <div class='table-wrap'>
                    <table class='benchmark-table'>
                        <thead>
                            <tr>
                                <th>Method</th>
                                <th>Mean</th>
                                <th>Error</th>
                                <th>StdDev</th>
                                <th>Ratio</th>
                                <th>Rank</th>
                                <th>Allocated</th>
                                <th>Relative speed</th>
                            </tr>
                        </thead>
                        <tbody>
                            {table_rows}
                        </tbody>
                    </table>
                </div>
            </section>
            """.format(
                anchor=escape(anchor),
                benchmark_name=escape(benchmark_name),
                best_mean=escape(format_ns(best_mean)),
                spread=escape(format_ns(span)),
                source_file=escape(Path(rows[0].source_file).name),
                table_rows="\n".join(table_rows),
            )
        )

    return "\n".join(nav_items), "\n".join(sections)


def make_anchor(text: str) -> str:
    anchor = re.sub(r"[^a-zA-Z0-9]+", "-", text.strip()).strip("-").lower()
    return anchor or "section"


def render_html(title: str, parsed: ParsedPathResult) -> str:
    grouped_rows = group_rows_by_benchmark(parsed.rows)
    method_summaries = build_method_summaries(grouped_rows)
    nav_html, sections_html = build_benchmark_sections(grouped_rows)
    overview_cards_html = build_overview_cards(parsed.source_files, grouped_rows, method_summaries)
    summary_rows_html = build_method_summary_table(method_summaries)

    return f"""<!DOCTYPE html>
<html lang="en">
<head>
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>{escape(title)}</title>
    <style>
        :root {{
            --bg: #0b1020;
            --panel: rgba(16, 24, 48, 0.78);
            --panel-2: rgba(22, 31, 61, 0.88);
            --line: rgba(255, 255, 255, 0.10);
            --text: #e8ecf8;
            --muted: #9aa8c7;
            --accent: #7dd3fc;
            --accent-2: #a78bfa;
            --good: #34d399;
            --warn: #f59e0b;
            --bad: #f87171;
            --shadow: 0 18px 50px rgba(0, 0, 0, 0.35);
        }}

        * {{ box-sizing: border-box; }}

        html {{ scroll-behavior: smooth; }}

        body {{
            margin: 0;
            font-family: Inter, ui-sans-serif, system-ui, -apple-system, BlinkMacSystemFont, "Segoe UI", sans-serif;
            background:
                radial-gradient(circle at top left, rgba(125, 211, 252, 0.15), transparent 28%),
                radial-gradient(circle at top right, rgba(167, 139, 250, 0.14), transparent 24%),
                linear-gradient(180deg, #0a0f1e 0%, #0b1020 100%);
            color: var(--text);
            min-height: 100vh;
        }}

        .container {{
            width: min(1680px, calc(100vw - 24px));
            margin: 0 auto;
            padding: 18px 0 40px;
        }}

        .hero {{
            padding: 28px;
            border: 1px solid var(--line);
            border-radius: 24px;
            background: linear-gradient(180deg, rgba(18, 28, 55, 0.92), rgba(13, 20, 40, 0.88));
            box-shadow: var(--shadow);
            backdrop-filter: blur(12px);
        }}

        h1, h2, h3 {{ margin: 0; }}

        .hero h1 {{
            font-size: clamp(28px, 4vw, 42px);
            line-height: 1.05;
            letter-spacing: -0.03em;
        }}

        .hero p {{
            margin: 12px 0 0;
            color: var(--muted);
            font-size: 16px;
            line-height: 1.6;
            max-width: 900px;
        }}

        .cards {{
            display: grid;
            grid-template-columns: repeat(auto-fit, minmax(180px, 1fr));
            gap: 14px;
            margin-top: 18px;
        }}

        .card {{
            border: 1px solid var(--line);
            border-radius: 18px;
            padding: 16px;
            background: rgba(255, 255, 255, 0.03);
        }}

        .card .label {{
            color: var(--muted);
            font-size: 12px;
            text-transform: uppercase;
            letter-spacing: 0.08em;
            margin-bottom: 8px;
        }}

        .card .value {{
            font-size: 18px;
            line-height: 1.35;
            font-weight: 700;
        }}

        .content-grid {{
            display: grid;
            grid-template-columns: 300px minmax(0, 1fr);
            gap: 20px;
            margin-top: 22px;
            align-items: start;
        }}

        .sidebar, .panel {{
            border: 1px solid var(--line);
            border-radius: 24px;
            background: var(--panel);
            box-shadow: var(--shadow);
            backdrop-filter: blur(12px);
        }}

        .sidebar {{
            position: sticky;
            top: 16px;
            padding: 20px;
        }}

        .sidebar h3 {{
            font-size: 15px;
            margin-bottom: 12px;
            color: var(--muted);
            text-transform: uppercase;
            letter-spacing: 0.08em;
        }}

        .sidebar nav {{
            display: flex;
            flex-direction: column;
            gap: 8px;
            max-height: calc(100vh - 180px);
            overflow: auto;
            padding-right: 4px;
        }}

        .sidebar a {{
            color: var(--text);
            text-decoration: none;
            border: 1px solid transparent;
            border-radius: 12px;
            padding: 10px 12px;
            background: rgba(255, 255, 255, 0.02);
        }}

        .sidebar a:hover {{
            border-color: var(--line);
            background: rgba(255, 255, 255, 0.05);
        }}

        .panel {{
            padding: 20px;
        }}

        .panel + .panel {{
            margin-top: 18px;
        }}

        .table-wrap {{
            width: 100%;
            overflow-x: hidden;
            margin-top: 14px;
        }}

        table {{
            width: 100%;
            border-collapse: collapse;
            table-layout: fixed;
        }}

        th, td {{
            padding: 9px 10px;
            border-bottom: 1px solid rgba(255, 255, 255, 0.08);
            text-align: left;
            vertical-align: middle;
            font-size: 13px;
            line-height: 1.35;
        }}

        th {{
            color: var(--muted);
            font-size: 11px;
            letter-spacing: 0.06em;
            text-transform: uppercase;
            background: rgba(255, 255, 255, 0.03);
            position: sticky;
            top: 0;
            white-space: normal;
            word-break: break-word;
        }}

        td {{
            overflow-wrap: anywhere;
        }}

        tbody tr:hover {{
            background: rgba(255, 255, 255, 0.03);
        }}

        .method-pill {{
            display: inline-flex;
            align-items: center;
            gap: 6px;
            border-radius: 999px;
            padding: 6px 10px;
            border: 1px solid rgba(255, 255, 255, 0.12);
            font-weight: 700;
            letter-spacing: 0.01em;
            white-space: normal;
            max-width: 100%;
            font-size: 12px;
        }}

        .method-wist {{ background: rgba(125, 211, 252, 0.14); }}
        .method-csharp {{ background: rgba(52, 211, 153, 0.14); }}
        .method-ncalc {{ background: rgba(248, 113, 113, 0.14); }}
        .method-generic {{ background: rgba(255, 255, 255, 0.08); }}

        .benchmark-section + .benchmark-section {{
            margin-top: 22px;
        }}

        .section-header {{
            display: flex;
            justify-content: space-between;
            align-items: flex-start;
            gap: 12px;
        }}

        .section-header h2 {{
            font-size: 24px;
            letter-spacing: -0.02em;
        }}

        .section-meta {{
            margin-top: 8px;
            color: var(--muted);
            line-height: 1.55;
        }}

        .back-link {{
            color: var(--accent);
            text-decoration: none;
            white-space: nowrap;
            padding-top: 4px;
        }}

        .bar-cell {{
            display: grid;
            grid-template-columns: minmax(110px, 1fr) auto;
            gap: 8px;
            align-items: center;
        }}

        .bar-track {{
            width: 100%;
            height: 10px;
            border-radius: 999px;
            background: rgba(255, 255, 255, 0.08);
            overflow: hidden;
        }}

        .bar-fill {{
            height: 100%;
            border-radius: inherit;
            box-shadow: inset 0 0 0 1px rgba(255, 255, 255, 0.16);
        }}

        .winner-badge, .normal-badge {{
            display: inline-flex;
            align-items: center;
            border-radius: 999px;
            padding: 3px 8px;
            font-size: 11px;
            font-weight: 700;
            white-space: nowrap;
        }}

        .winner-badge {{
            background: rgba(52, 211, 153, 0.18);
            color: #c4ffeb;
        }}

        .normal-badge {{
            background: rgba(255, 255, 255, 0.08);
            color: var(--text);
        }}

        .footer-note {{
            color: var(--muted);
            margin-top: 18px;
            line-height: 1.65;
        }}


        .summary-table th:nth-child(1), .summary-table td:nth-child(1) {{ width: 5%; }}
        .summary-table th:nth-child(2), .summary-table td:nth-child(2) {{ width: 18%; }}
        .summary-table th:nth-child(3), .summary-table td:nth-child(3) {{ width: 7%; }}
        .summary-table th:nth-child(4), .summary-table td:nth-child(4) {{ width: 8%; }}
        .summary-table th:nth-child(5), .summary-table td:nth-child(5) {{ width: 11%; }}
        .summary-table th:nth-child(6), .summary-table td:nth-child(6) {{ width: 11%; }}
        .summary-table th:nth-child(7), .summary-table td:nth-child(7) {{ width: 12%; }}
        .summary-table th:nth-child(8), .summary-table td:nth-child(8) {{ width: 11%; }}
        .summary-table th:nth-child(9), .summary-table td:nth-child(9) {{ width: 17%; }}

        .benchmark-table th:nth-child(1), .benchmark-table td:nth-child(1) {{ width: 17%; }}
        .benchmark-table th:nth-child(2), .benchmark-table td:nth-child(2) {{ width: 10%; }}
        .benchmark-table th:nth-child(3), .benchmark-table td:nth-child(3) {{ width: 10%; }}
        .benchmark-table th:nth-child(4), .benchmark-table td:nth-child(4) {{ width: 10%; }}
        .benchmark-table th:nth-child(5), .benchmark-table td:nth-child(5) {{ width: 8%; }}
        .benchmark-table th:nth-child(6), .benchmark-table td:nth-child(6) {{ width: 6%; }}
        .benchmark-table th:nth-child(7), .benchmark-table td:nth-child(7) {{ width: 9%; }}
        .benchmark-table th:nth-child(8), .benchmark-table td:nth-child(8) {{ width: 30%; }}

        @media (max-width: 1100px) {{
            .content-grid {{
                grid-template-columns: 1fr;
            }}

            .sidebar {{
                position: static;
            }}
        }}

        @media (max-width: 900px) {{
            table {{
                table-layout: auto;
            }}

            th, td {{
                padding: 8px 8px;
                font-size: 12px;
            }}

            .summary-table th:nth-child(9), .summary-table td:nth-child(9),
            .benchmark-table th:nth-child(3), .benchmark-table td:nth-child(3),
            .benchmark-table th:nth-child(4), .benchmark-table td:nth-child(4) {{
                display: none;
            }}
        }}

        @media (max-width: 720px) {{
            .container {{ width: min(100vw - 12px, 1680px); }}
            .hero, .panel, .sidebar {{ border-radius: 18px; }}
            .hero {{ padding: 20px; }}
            .panel, .sidebar {{ padding: 16px; }}
            .bar-cell {{ grid-template-columns: 1fr; }}
            .method-pill {{ padding: 5px 8px; }}
        }}
    </style>
</head>
<body>
    <div class="container" id="top">
        <section class="hero">
            <h1>{escape(title)}</h1>
            <p>
                Standalone HTML report generated from BenchmarkDotNet CSV files. It aggregates all found benchmark tables,
                highlights the overall leader, and shows relative performance inside each benchmark.
            </p>
            <div class="cards">
                {overview_cards_html}
            </div>
        </section>

        <div class="content-grid">
            <aside class="sidebar">
                <h3>Benchmarks</h3>
                <nav>
                    {nav_html}
                </nav>
            </aside>

            <main>
                <section class="panel">
                    <h2>Overall method summary</h2>
                    <div class="table-wrap">
                        <table class='summary-table'>
                            <thead>
                                <tr>
                                    <th>#</th>
                                    <th>Method</th>
                                    <th>Wins</th>
                                    <th>Bench-<wbr>marks</th>
                                    <th>Avg mean</th>
                                    <th>Median mean</th>
                                    <th>Geo mean vs best</th>
                                    <th>Avg vs best</th>
                                    <th>Interpretation</th>
                                </tr>
                            </thead>
                            <tbody>
                                {summary_rows_html}
                            </tbody>
                        </table>
                    </div>
                    <div class="footer-note">
                        <strong>How to read this:</strong> “Geo mean vs best” normalizes each benchmark to its fastest method,
                        then aggregates results across all benchmarks. A value of 1.00× means the method was effectively on the benchmark frontier.
                    </div>
                </section>

                <section class="panel">
                    <h2>Per-benchmark details</h2>
                    <div class="footer-note">
                        The bar in each row shows relative runtime inside the same benchmark. Shorter is better.
                    </div>
                    {sections_html}
                </section>
            </main>
        </div>
    </div>
</body>
</html>
"""


def main() -> None:
    args = parse_args()
    input_paths = prompt_for_paths_if_needed(args.paths)
    parsed = parse_inputs(input_paths)
    output_path = Path(args.output).expanduser().resolve()
    html_text = render_html(args.title, parsed)
    output_path.write_text(html_text, encoding="utf-8")

    print(f"Generated HTML report: {output_path}")
    print(f"CSV files processed: {len(parsed.source_files)}")
    print(f"Benchmark rows processed: {len(parsed.rows)}")


if __name__ == "__main__":
    main()
