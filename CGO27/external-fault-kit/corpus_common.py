from __future__ import annotations
import json
import re
from pathlib import Path
from typing import Any

CASE_ID = re.compile(r"^[A-Za-z0-9][A-Za-z0-9._-]{2,63}$")


def _load(directory: Path, role: str) -> list[dict[str, Any]]:
    path = directory / role
    if not path.is_dir():
        raise ValueError(f"missing directory: {role}")
    result = []
    for file in sorted(path.glob("*.json")):
        value = json.loads(file.read_text(encoding="utf-8"))
        if not isinstance(value, dict):
            raise ValueError(f"{file}: case must be an object")
        if value.get("schemaVersion") != 1:
            raise ValueError(f"{file}: schemaVersion must be 1")
        case_id = value.get("caseId")
        if not isinstance(case_id, str) or CASE_ID.fullmatch(case_id) is None:
            raise ValueError(f"{file}: invalid caseId")
        if value.get("blindAttestation") is not True:
            raise ValueError(f"{file}: blindAttestation must be true")
        value = dict(value)
        value["caseRole"] = "fault" if role == "faults" else "control"
        result.append(value)
    return result


def collect_cases(directory: Path) -> tuple[list[dict[str, Any]], list[dict[str, Any]]]:
    faults = _load(directory, "faults")
    controls = _load(directory, "controls")
    ids = [case["caseId"] for case in faults + controls]
    if len(ids) != len(set(ids)):
        raise ValueError("caseId values must be unique across faults and controls")
    return faults, controls


def validate_accounting(faults: list[dict[str, Any]], controls: list[dict[str, Any]]) -> None:
    if not 15 <= len(faults) <= 30:
        raise ValueError("fault count must be between 15 and 30")
    families = {case.get("family") for case in faults}
    if None in families or len(families) < 6:
        raise ValueError("fault corpus must contain at least 6 non-empty families")
    if len(controls) < 2 * len(faults):
        raise ValueError("control count must be at least twice the fault count")
    for case in faults:
        if case.get("expectedNoProtocolSymptom") not in {"wrong-result", "late-failure", "silent-invalid-acceptance"}:
            raise ValueError(f"{case['caseId']}: invalid expectedNoProtocolSymptom")
        if not case.get("expectedDetectionBoundary"):
            raise ValueError(f"{case['caseId']}: missing expectedDetectionBoundary")
