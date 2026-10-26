"""Check captured prompt/evaluation identities and judge evidence, not truth of scores."""

import hashlib
import json
from pathlib import Path


def sha256(path):
    return hashlib.sha256(path.read_bytes()).hexdigest()


def main():
    root = Path(__file__).resolve().parent
    plan = json.loads((root / "frozen-plan.json").read_text(encoding="utf-8"))
    rubric = json.loads((root / "rubric.json").read_text(encoding="utf-8"))
    judge = json.loads((root / "judge.json").read_text(encoding="utf-8"))
    captured = json.loads((root / "responses/raw-responses.json").read_text(encoding="utf-8"))
    originals = {r["response_id"]: r for r in captured["responses"]}
    errors = []

    def check(condition, message):
        if not condition:
            errors.append(message)

    for name, path in (
        ("candidate", plan["candidate_path"]),
        ("baseline", plan["baseline_path"]),
        ("fixtures", "fixtures.json"),
        ("rubric", "rubric.json"),
    ):
        check(sha256(root / path) == plan[name + "_sha256"], name + " hash changed")

    expected_ids = {run["id"] for run in plan["runs"]}
    received_ids = [run["response_id"] for run in judge["runs"]]
    check(len(received_ids) == len(expected_ids), "wrong judge run count")
    check(set(received_ids) == expected_ids, "wrong or duplicate response IDs")
    check(set(originals) == expected_ids and len(captured["responses"]) == len(expected_ids), "wrong raw response IDs")
    check(judge["variant_assignment_received"] is False, "judge assignment disclosure")
    cases = {case["id"]: case for case in rubric["cases"]}
    judgments = {run["response_id"]: run for run in judge["runs"]}
    summary = []

    for run in plan["runs"]:
        run_id = run["id"]
        case_id = run["case_id"]
        check(sha256(root / "inputs" / (case_id + ".json")) == run["case_sha256"], run_id + " case changed")
        check(sha256(root / run["task_path"]) == run["task_sha256"], run_id + " task changed")
        check(run["prompt_sha256"] == plan[run["variant"] + "_sha256"], run_id + " prompt mismatch")
        response_path = root / run["response_path"]
        response = response_path.read_text(encoding="utf-8")
        original = originals[run_id]
        raw = original["original_text"].encode("utf-8")
        check(hashlib.sha256(raw).hexdigest() == original["original_utf8_sha256"], run_id + " raw response hash mismatch")
        check(sha256(response_path) == original["text_view_sha256"], run_id + " text view hash mismatch")
        expected_view = raw if original["text_view_normalization"] == "none" else raw.rstrip(b"\n") + b"\n"
        check(response_path.read_bytes() == expected_view, run_id + " unexpected response edit")
        word_count = len(response.split())
        check(word_count <= plan["output_budget_words"], run_id + " word budget exceeded")
        judgment = judgments.get(run_id)
        if judgment is None:
            continue
        check(judgment["case_id"] == case_id, run_id + " judge case mismatch")
        check(judgment["word_count_whitespace"] == word_count, run_id + " word count mismatch")

        for field, expected in (
            ("criteria", cases[case_id]["criteria"]),
            ("global_checks", rubric["global_checks"]),
        ):
            rows = judgment[field]
            check(len(rows) == len(expected), run_id + " wrong " + field + " count")
            check({r["id"] for r in rows} == {r["id"] for r in expected}, run_id + " wrong " + field + " IDs")
            for row in rows:
                check(row["result"] in rubric["scoring"]["criterion_result"], run_id + " invalid result")
                check(type(row["critical_error_present"]) is bool, run_id + " invalid critical flag")
                for quote in row["evidence_quotes"]:
                    check(bool(quote) and quote in response, run_id + "/" + row["id"] + " inexact evidence quote")

        met = sum(row["result"] == "met" for row in judgment["criteria"])
        critical = sum(row["critical_error_present"] for row in judgment["criteria"] + judgment["global_checks"])
        check(judgment["met_count"] == met, run_id + " met total mismatch")
        check(judgment["critical_error_count"] == critical, run_id + " critical total mismatch")
        summary.append({
            "response_id": run_id, "case_id": case_id, "variant": run["variant"],
            "words_whitespace": word_count, "response_sha256": sha256(response_path),
            "original_response_sha256": original["original_utf8_sha256"],
            "met": met, "criteria": len(judgment["criteria"]), "critical_errors": critical,
            "global_met": sum(row["result"] == "met" for row in judgment["global_checks"]),
        })

    print(json.dumps({"integrity_pass": not errors, "errors": errors, "runs": summary,
                      "scope": "Mechanical hashes, completeness, word counts and exact quotations only; no semantic-score validation."},
                     ensure_ascii=False, indent=2))
    return 1 if errors else 0


if __name__ == "__main__":
    raise SystemExit(main())
