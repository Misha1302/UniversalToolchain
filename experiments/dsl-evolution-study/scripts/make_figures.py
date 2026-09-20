#!/usr/bin/env python3
import html
import json
from pathlib import Path

study = Path(__file__).resolve().parents[1]
data = json.loads((study / "results" / "summary.json").read_text())["metrics"]
out = study / "results" / "figures"
out.mkdir(parents=True, exist_ok=True)


def write_bar_chart(path, title, ylabel, labels, values, ceiling=None):
    width, height = 760, 420
    left, right, top, bottom = 90, 30, 60, 70
    chart_width = width - left - right
    chart_height = height - top - bottom
    maximum = ceiling or max(values) or 1
    bar_width = chart_width / (len(values) * 1.8)
    step = chart_width / len(values)
    parts = [
        f'<svg xmlns="http://www.w3.org/2000/svg" width="{width}" height="{height}">',
        '<rect width="100%" height="100%" fill="white"/>',
        f'<text x="{width / 2}" y="30" text-anchor="middle" font-family="sans-serif" font-size="20">{html.escape(title)}</text>',
        f'<text x="18" y="{height / 2}" transform="rotate(-90 18 {height / 2})" text-anchor="middle" font-family="sans-serif" font-size="13">{html.escape(ylabel)}</text>',
        f'<line x1="{left}" y1="{top}" x2="{left}" y2="{height - bottom}" stroke="black"/>',
        f'<line x1="{left}" y1="{height - bottom}" x2="{width - right}" y2="{height - bottom}" stroke="black"/>',
    ]
    for index, (label, value) in enumerate(zip(labels, values)):
        x = left + step * index + (step - bar_width) / 2
        bar_height = chart_height * value / maximum
        y = height - bottom - bar_height
        display = f"{value:.4f}" if isinstance(value, float) else str(value)
        parts.extend([
            f'<rect x="{x}" y="{y}" width="{bar_width}" height="{bar_height}" fill="#777"/>',
            f'<text x="{x + bar_width / 2}" y="{y - 8}" text-anchor="middle" font-family="sans-serif" font-size="14">{display}</text>',
            f'<text x="{x + bar_width / 2}" y="{height - bottom + 25}" text-anchor="middle" font-family="sans-serif" font-size="13">{html.escape(label)}</text>',
        ])
    parts.append("</svg>")
    path.write_text("\n".join(parts) + "\n")


labels = ["Control", "Clone-own", "UT"]
keys = ["control", "clone-own", "universal-toolchain"]
write_bar_chart(
    out / "implementation-footprint.svg",
    "Implementation footprint",
    "Handwritten production SLOC",
    labels,
    [data[key]["production_sloc"] for key in keys],
)
write_bar_chart(
    out / "duplication-ratio.svg",
    "Deterministic clone approximation",
    "Repeated normalized-line ratio",
    labels,
    [data[key]["duplication_ratio"] for key in keys],
    1.0,
)