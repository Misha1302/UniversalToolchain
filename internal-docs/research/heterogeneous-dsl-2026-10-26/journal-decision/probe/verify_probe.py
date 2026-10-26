#!/usr/bin/env python3
"""Verify the recorded four-case mechanism diagnostic and its source identity."""
import argparse
import hashlib
import json
from pathlib import Path

HERE = Path(__file__).resolve().parent
ROOT = HERE.parents[4]


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def verify_result(data):
    assert data["schemaVersion"] == 1
    rows = data["rows"]
    matched = data["matchedSchedulerRows"]
    counts = data["genericCounts"]
    assert (len(rows), len(matched), len(counts)) == (8, 12, 2)
    names = {"clean-control", "declared-inner-corruption", "undeclared-inner-corruption", "unverified-output-corruption"}
    assert {r["Name"] for r in rows} == names
    assert len({(r["Name"], r["policy"]) for r in rows}) == 8
    assert len({(r["Name"], r["treatment"]) for r in matched}) == 12
    by_case = {(r["Name"], r["policy"]): r for r in rows}
    observed = {
        "clean-control": ("accepted", "accepted", True),
        "declared-inner-corruption": ("rejected", "rejected", False),
        "undeclared-inner-corruption": ("accepted", "rejected", False),
        "unverified-output-corruption": ("accepted", "accepted", False),
    }
    for name, (selective, always, valid) in observed.items():
        assert by_case[name, "P2_SELECTIVE"]["status"] == selective
        assert by_case[name, "P3_ALWAYS"]["status"] == always
        assert all(by_case[name, p]["independentValid"] == valid for p in ("P2_SELECTIVE", "P3_ALWAYS"))
        arms = {r["treatment"]: r for r in matched if r["Name"] == name}
        for key in ("status", "invocations", "independentValid", "diagnostic"):
            assert arms["production-selective"][key] == arms["typed-manual-selective"][key]
    assert {r["policy"]: r["scheduled"] for r in counts} == {"P2Selective": 0, "P3Always": 1}
    return data


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--replay", type=Path)
    args = parser.parse_args()
    receipt = json.loads((HERE / "receipt.json").read_text())
    for name, expected in receipt["artifact_sha256"].items():
        assert digest(HERE / name) == expected, name
    for name, expected in receipt["linked_production_source_sha256"].items():
        assert digest(ROOT / name) == expected, name
    recorded = verify_result(json.loads((HERE / "results.json").read_text()))
    if args.replay:
        assert verify_result(json.loads(args.replay.read_text())) == recorded
    print("Verified: source identities, four diagnostic cases, 8+12+2 observations, bounded manual parity" + (", exact replay" if args.replay else ""))


if __name__ == "__main__":
    main()
