#!/usr/bin/env python3
"""Offline SDK-version and contributor-path regression controls."""
from pathlib import Path
import tempfile
import unittest
from unittest.mock import patch

import check_documentation_links as links
import check_documentation_onboarding as onboarding

ROOT = Path(__file__).resolve().parents[1]


class OnboardingDocumentationTests(unittest.TestCase):
    def setUp(self):
        self.versions = onboarding.package_versions(ROOT)

    def test_current_quickstart_template_and_generated_project_agree(self):
        self.assertEqual(onboarding.check(ROOT), [])

    def test_mixed_package_versions_are_valid(self):
        text = "\n".join(f'<PackageReference Include="{name}" Version="{version}" />'
                         for name, version in self.versions.items())
        self.assertEqual(onboarding.check_references(text, self.versions, "fixture"), [])

    def test_stale_template_version_is_rejected(self):
        self.assertTrue(onboarding.check_references(
            "dotnet new install UniversalToolchain.Templates.0.0.0-mutant.nupkg", self.versions, "fixture"))

    def test_stale_reference_version_is_rejected(self):
        self.assertTrue(onboarding.check_references(
            '<PackageReference Include="UniversalToolchain.LanguageSdk" Version="0.0.0-mutant" />',
            self.versions, "fixture"))

    def test_reference_attribute_order_and_child_version_are_supported(self):
        name = "UniversalToolchain.Runtime"
        version = self.versions[name]
        for text in [f'<PackageReference Version="{version}" Include="{name}" />',
                     f'<PackageReference Include="{name}"><Version>{version}</Version></PackageReference>']:
            self.assertEqual(onboarding.check_references(text, self.versions, "fixture"), [])

    def test_unknown_package_and_missing_version_are_rejected(self):
        for text in ['<PackageReference Include="UniversalToolchain.DoesNotExist" Version="1" />',
                     '<PackageReference Include="UniversalToolchain.Runtime" />']:
            self.assertTrue(onboarding.check_references(text, self.versions, "fixture"))

    def test_real_agent_policy_targets_exist(self):
        self.assertEqual(links.check_inline_repository_paths([ROOT / "AGENTS.md"]), [])

    def test_missing_agent_policy_path_is_rejected_then_existing_path_passes(self):
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            agents = root / "AGENTS.md"
            agents.write_text('Read `docs/SYNTAX_OWNERSHIP_RULES.md`.\n')
            with patch.object(links, "ROOT", root):
                self.assertTrue(links.check_inline_repository_paths([agents]))
                policy = root / "internal-docs/policies-and-reports/SYNTAX_OWNERSHIP_RULES.md"
                policy.parent.mkdir(parents=True)
                policy.write_text('# Policy\n')
                agents.write_text('Read `internal-docs/policies-and-reports/SYNTAX_OWNERSHIP_RULES.md`.\n')
                self.assertEqual(links.check_inline_repository_paths([agents]), [])


if __name__ == "__main__":
    unittest.main(verbosity=2)
