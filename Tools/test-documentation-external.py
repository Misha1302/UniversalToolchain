#!/usr/bin/env python3
"""Offline positive/negative controls for the optional external docs check."""
from __future__ import annotations

import copy
import json
from pathlib import Path
import unittest
from unittest.mock import patch
import urllib.error

import check_documentation_external as external
from check_documentation_release_state import ReleaseStateError, available_versions, validate_source_publication

ROOT = Path(__file__).resolve().parents[1]
STATE = json.loads((ROOT / "eng/documentation-release-state.json").read_text())


class ExternalDocumentationTests(unittest.TestCase):
    def test_status_contract(self):
        for status, expected in [(200, "PASS"), (404, "FAIL"), (410, "FAIL"),
                                 (None, "INCONCLUSIVE"), (401, "INCONCLUSIVE"),
                                 (403, "INCONCLUSIVE"), (429, "INCONCLUSIVE"),
                                 (503, "INCONCLUSIVE")]:
            with self.subTest(status=status):
                self.assertEqual(external.http_outcome(status), expected)

    def test_observed_versions_pass_without_promoting_preview(self):
        result = external.registry_check(json.dumps({"versions": available_versions(STATE)}).encode(), STATE)
        self.assertEqual(result["outcome"], "PASS")
        self.assertEqual(result["pinnedInstallSmokeVersion"], STATE["publishedVersion"])

    def test_missing_recorded_version_fails(self):
        versions = [v for v in available_versions(STATE) if v != STATE["sourceVersion"]]
        result = external.registry_check(json.dumps({"versions": versions}).encode(), STATE)
        self.assertEqual(result["outcome"], "FAIL")
        self.assertIn(STATE["sourceVersion"], result["missingSnapshotVersions"])

    def test_new_version_is_reported_not_recommended(self):
        versions = available_versions(STATE) + ["99.0.0-preview.1"]
        result = external.registry_check(json.dumps({"versions": versions}).encode(), STATE)
        self.assertEqual(result["outcome"], "PASS")
        self.assertEqual(result["newVersionsRequiringReview"], ["99.0.0-preview.1"])
        self.assertEqual(result["pinnedInstallSmokeVersion"], STATE["publishedVersion"])

    def test_gateway_html_and_malformed_schema_are_inconclusive(self):
        for body in [b"<html>gateway</html>", b"{}", b'{"versions": []}', b'{"versions": "1"}']:
            self.assertEqual(external.registry_check(body, STATE)["outcome"], "INCONCLUSIVE")

    def test_unknown_publication_is_not_inferred_from_different_versions(self):
        validate_source_publication(Path("fixture.md"), "Publication is not verified", "next", available_versions(STATE))
        with self.assertRaises(ReleaseStateError):
            validate_source_publication(Path("fixture.md"), "Not published", "next", available_versions(STATE))

    def test_false_unpublished_and_missing_provenance_are_rejected(self):
        for block in ["not published on NuGet.org", "available on NuGet.org"]:
            with self.assertRaises(ReleaseStateError):
                validate_source_publication(Path("fixture.md"), block, STATE["sourceVersion"], available_versions(STATE))
        validate_source_publication(Path("fixture.md"), "Available on NuGet.org; binary provenance not verified",
                                    STATE["sourceVersion"], available_versions(STATE))

    def test_invalid_snapshot_fails_closed(self):
        for field, value in [("checkedAtUtc", "yesterday"), ("indexUrl", "https://example.com"), ("versions", [])]:
            state = copy.deepcopy(STATE)
            state["nugetAvailability"][field] = value
            with self.assertRaises(ReleaseStateError):
                available_versions(state)

    @patch("check_documentation_external.time.sleep")
    @patch("check_documentation_external.urllib.request.build_opener")
    def test_network_error_retries_once_and_is_not_pass(self, build, sleep):
        build.return_value.open.side_effect = urllib.error.URLError("fixture DNS failure")
        result, body = external.probe("https://fixture.invalid/")
        self.assertEqual(result["outcome"], "INCONCLUSIVE")
        self.assertEqual(build.return_value.open.call_count, 2)
        self.assertEqual(body, b"")

    @patch("check_documentation_external.urllib.request.build_opener")
    def test_404_is_confirmed_not_retried(self, build):
        build.return_value.open.side_effect = urllib.error.HTTPError("https://fixture.invalid/", 404, "missing", {}, None)
        result, _ = external.probe("https://fixture.invalid/")
        self.assertEqual(result["outcome"], "FAIL")
        self.assertEqual(build.return_value.open.call_count, 1)

    def test_injected_live_success_and_failure_aggregation(self):
        index = STATE["nugetAvailability"]["indexUrl"]
        def fetch(url):
            body = json.dumps({"versions": available_versions(STATE)}).encode() if url == index else b"site"
            return {"url": url, "outcome": "PASS", "status": 200, "finalUrl": url, "redirects": []}, body
        self.assertEqual(external.check(ROOT, fetch)["outcome"], "PASS")
        def outage(url):
            if url == index:
                return {"url": url, "outcome": "INCONCLUSIVE", "status": None}, b""
            return fetch(url)
        self.assertEqual(external.check(ROOT, outage)["outcome"], "INCONCLUSIVE")
        def broken(url):
            if url == index:
                return outage(url)
            return {"url": url, "outcome": "FAIL", "status": 404}, b""
        self.assertEqual(external.check(ROOT, broken)["outcome"], "FAIL")


if __name__ == "__main__":
    unittest.main(verbosity=2)
