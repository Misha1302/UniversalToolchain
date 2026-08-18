#!/usr/bin/env python3
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parents[1]


def parse_number(value: str) -> int:
    return int(value.replace(',', ''))


def main() -> int:
    errors: list[str] = []

    test_counts_path = ROOT / 'eng' / 'test-counts.json'
    release_state_path = ROOT / 'eng' / 'documentation-release-state.json'
    recipe_path = ROOT / 'docs' / 'start' / 'use-case-recipes.md'
    recipe_test_path = (
        ROOT
        / 'UniversalToolchain'
        / 'UniversalToolchain.Dialects.Tests'
        / 'Wist'
        / 'PublicFacade'
        / 'WistUseCaseRecipeTests.cs'
    )

    test_counts = json.loads(test_counts_path.read_text(encoding='utf-8'))
    expected_total = int(test_counts['totalPassed'])
    release_state = json.loads(release_state_path.read_text(encoding='utf-8'))

    stability_path = ROOT / release_state['stabilityDocument']
    stability_text = stability_path.read_text(encoding='utf-8')
    stability_match = re.search(
        r'(?i)exact repository test manifest of\s+([0-9][0-9,]*)\s+tests\b',
        stability_text,
    )
    if stability_match is None:
        errors.append(
            f'{stability_path.relative_to(ROOT).as_posix()}: '
            'missing exact repository test-manifest claim'
        )
    else:
        claimed_total = parse_number(stability_match.group(1))
        if claimed_total != expected_total:
            errors.append(
                f'{stability_path.relative_to(ROOT).as_posix()}: '
                f'stale test total {claimed_total}; expected {expected_total} from eng/test-counts.json'
            )

    recipe_text = recipe_path.read_text(encoding='utf-8')
    formula_marker = re.search(
        r'<!--\s*wist-rollout-formula-contract:\s*(.*?)\s*-->',
        recipe_text,
    )
    score_marker = re.search(
        r'<!--\s*wist-rollout-expected-score:\s*([0-9]+(?:\.[0-9]+)?)\s*-->',
        recipe_text,
    )
    test_text = recipe_test_path.read_text(encoding='utf-8')
    test_formula = re.search(
        r'DocumentedRolloutFormula\s*=\s*"([^"]+)"\s*;',
        test_text,
        re.MULTILINE,
    )
    test_score = re.search(
        r'DocumentedRolloutExpectedScore\s*=\s*([0-9]+(?:\.[0-9]+)?)\s*;',
        test_text,
    )

    if formula_marker is None:
        errors.append('docs/start/use-case-recipes.md: rollout formula contract marker is missing')
    if score_marker is None:
        errors.append('docs/start/use-case-recipes.md: rollout expected-score contract marker is missing')
    if test_formula is None:
        errors.append('WistUseCaseRecipeTests.cs: documented rollout formula constant is missing')
    if test_score is None:
        errors.append('WistUseCaseRecipeTests.cs: documented rollout expected-score constant is missing')

    if formula_marker is not None and test_formula is not None:
        documented_formula = formula_marker.group(1).strip()
        regression_formula = test_formula.group(1)
        if documented_formula != regression_formula:
            errors.append(
                'rollout recipe drift: documentation formula and public-facade regression differ'
            )
        if f'"{documented_formula}"' not in recipe_text:
            errors.append(
                'docs/start/use-case-recipes.md: rollout contract marker is not used by the copy-ready C# snippet'
            )

    if score_marker is not None and test_score is not None:
        documented_score = float(score_marker.group(1))
        regression_score = float(test_score.group(1))
        if documented_score != regression_score:
            errors.append(
                'rollout recipe drift: documentation expected score and regression expected score differ'
            )

    if errors:
        print('Wist release-readiness documentation check failed:', file=sys.stderr)
        for error in errors:
            print(f'- {error}', file=sys.stderr)
        return 1

    print(
        'Wist release-readiness documentation check passed: '
        f'test total {expected_total}, rollout recipe synchronized.'
    )
    return 0


if __name__ == '__main__':
    raise SystemExit(main())
