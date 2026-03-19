using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Tests;

public class BuildPlanCollaboratorsTests
{
    [Test]
    public void OrderConstraintMapper_MapsSyntaxKindsToSemanticKinds()
    {
        var constraints = DialectOrderConstraintMapper.FromSyntaxRules([
            new OrderRule(OrderRuleKind.Requires, "A", "B"),
            new OrderRule(OrderRuleKind.Before, "C", "D"),
            new OrderRule(OrderRuleKind.After, "E", "F")
        ]);

        Assert.That(
            constraints.Select(x => (x.Kind, x.SourceModule, x.TargetModule)),
            Is.EqualTo(new[]
            {
                (DialectOrderConstraintKind.Requires, "A", "B"),
                (DialectOrderConstraintKind.Before, "C", "D"),
                (DialectOrderConstraintKind.After, "E", "F")
            }));
    }


    [Test]
    public void OrderConstraintMapper_MapsCompiledDirectiveKindsToSemanticKinds()
    {
        var constraints = DialectOrderConstraintMapper.FromCompiledDirectives([
            new DialectOrderDirective(DialectOrderDirectiveKind.Requires, "A", "B"),
            new DialectOrderDirective(DialectOrderDirectiveKind.Before, "C", "D"),
            new DialectOrderDirective(DialectOrderDirectiveKind.After, "E", "F")
        ]);

        Assert.That(
            constraints.Select(x => (x.Kind, x.SourceModule, x.TargetModule)),
            Is.EqualTo(new[]
            {
                (DialectOrderConstraintKind.Requires, "A", "B"),
                (DialectOrderConstraintKind.Before, "C", "D"),
                (DialectOrderConstraintKind.After, "E", "F")
            }));
    }

    [Test]
    public void SecurityCapabilityPolicyValidator_RestrictedUnsafeInterop_AddsDiagnostic()
    {
        var diagnostics = new List<DialectDiagnostic>();

        DialectSecurityCapabilityPolicyValidator.Validate(
            SecurityProfile.Restricted,
            new Dictionary<string, bool>(StringComparer.Ordinal)
            {
                ["unsafe-interop"] = true
            },
            diagnostics);

        Assert.That(diagnostics.Any(x => x.Code == "S006"), Is.True);
    }


    [Test]
    public void SecurityCapabilityPolicyValidator_CustomRule_IsAppliedThroughRulePipeline()
    {
        var diagnostics = new List<DialectDiagnostic>();

        DialectSecurityCapabilityPolicyValidator.Validate(
            SecurityProfile.Trusted,
            new Dictionary<string, bool>(StringComparer.Ordinal),
            diagnostics,
            [new RequireCapabilityRule("sandbox")]);

        Assert.That(diagnostics.Any(x => x.Code == "S900"), Is.True);
    }


    [Test]
    public void DialectPolicyValidator_ReturnsBuildPlanValidationDiagnostics()
    {
        var validator = new DialectPolicyValidator(new DialectBuildPlanBuilder());
        var document = new DialectSyntaxDocument(
            "dialect",
            null,
            ["A"],
            [],
            [],
            [],
            [],
            [],
            SecurityProfile.Restricted,
            [new KeyValuePair<string, bool>("unsafe-interop", true)]);

        var result = validator.Validate(document);

        Assert.That(result.Diagnostics.Any(x => x.Code == "S006"), Is.True);
    }

    [Test]
    public void SyntaxSemanticNormalizer_NormalizesModulesAndBackendDirectives()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var document = new DialectSyntaxDocument(
            "dialect",
            null,
            ["B", "A", "A"],
            ["Z"],
            [new OrderRule(OrderRuleKind.Before, "A", "B")],
            [
                new BackendDirectiveSyntax(TestBackendIds.Cil, false),
                new BackendDirectiveSyntax(TestBackendIds.Interpreter, true)
            ],
            [new IntrinsicDirectiveSyntax("i1", true, TestBackendIds.Any)],
            [new OptimizerDirectiveSyntax("o1", true, TestBackendIds.Interpreter)],
            null,
            []);

        var normalized = DialectSyntaxSemanticNormalizer.Normalize(document, diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(diagnostics, Is.Empty);
            Assert.That(normalized.ActiveModules, Is.EqualTo(new[] { "A", "B" }));
            Assert.That(normalized.BackendMap[TestBackendIds.Interpreter], Is.True);
            Assert.That(normalized.BackendMap[TestBackendIds.Cil], Is.False);
            Assert.That(normalized.OrderConstraints.Select(x => x.Kind), Is.EqualTo(new[] { DialectOrderConstraintKind.Before }));
            Assert.That(normalized.IntrinsicDirectives, Has.Count.EqualTo(1));
            Assert.That(normalized.OptimizerDirectives, Has.Count.EqualTo(1));
        });
    }

    private sealed class RequireCapabilityRule : IDialectPolicyValidationRule
    {
        private readonly string _capability;

        public RequireCapabilityRule(string capability)
        {
            _capability = capability;
        }

        public void Validate(
            SecurityProfile? securityProfile,
            IReadOnlyDictionary<string, bool> capabilities,
            List<DialectDiagnostic> diagnostics)
        {
            if (capabilities.ContainsKey(_capability))
                return;

            diagnostics.Add(new DialectDiagnostic(
                "S900",
                $"Capability '{_capability}' must be explicitly declared.",
                DialectDiagnosticSeverity.Error));
        }
    }
}