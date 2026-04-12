using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Frontend;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDefinitionBindingTests
{
    [Test]
    public void Bind_SyntaxPath_MatchesBindCoreResult()
    {
        var document = CreateSyntaxDocument("dialect", version: "1.0");
        var publicDiagnostics = new List<DialectDiagnostic>();
        var coreDiagnostics = new List<DialectDiagnostic>();

        var publicResult = DialectDefinitionSemanticBinder.Bind(document, publicDiagnostics);
        var coreResult = DialectDefinitionSemanticBinder.BindCore(new SyntaxDialectBindingSource(document), coreDiagnostics);

        AssertDefinitionsEqual(publicResult, coreResult);
        Assert.That(coreDiagnostics.Select(x => x.Code), Is.EqualTo(publicDiagnostics.Select(x => x.Code)));
    }

    [Test]
    public void Bind_SyntaxAndCompiledPaths_AreEquivalentForSameContent()
    {
        var syntax = CreateSyntaxDocument("dialect");
        var compiled = CreateCompiledSlice("dialect");
        var syntaxDiagnostics = new List<DialectDiagnostic>();
        var compiledDiagnostics = new List<DialectDiagnostic>();

        var syntaxResult = DialectDefinitionSemanticBinder.Bind(syntax, syntaxDiagnostics);
        var compiledResult = DialectDefinitionSemanticBinder.Bind(compiled, compiledDiagnostics);

        AssertDefinitionsEqual(syntaxResult, compiledResult);
        Assert.Multiple(() =>
        {
            Assert.That(syntaxDiagnostics, Is.Empty);
            Assert.That(compiledDiagnostics, Is.Empty);
        });
    }

    [Test]
    public void BindCore_PreservesSourceVersionAndBaseDialectName()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var source = new TestBindingSource("dialect", "2.0", "base");

        var definition = DialectDefinitionSemanticBinder.BindCore(source, diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Version, Is.EqualTo("2.0"));
            Assert.That(definition.BaseDialectName, Is.EqualTo("base"));
            Assert.That(diagnostics, Is.Empty);
        });
    }

    [Test]
    public void Builder_Build_RejectsMissingRequiredFields()
    {
        var builder = new DialectDefinitionBuilder();
        builder.SetIdentity("dialect", null, null);
        builder.SetModulePolicy(new ModulePolicy());
        builder.SetBackendPolicy(new BackendPolicy());
        builder.SetIntrinsicPolicy(new IntrinsicPolicy());
        builder.SetOptimizerPolicy(new OptimizerPolicy());
        builder.SetSecurityPolicy(null);

        Assert.That(
            () => builder.Build(),
            Throws.TypeOf<InvalidOperationException>());
    }

    [Test]
    public void Builder_Build_CreatesDefinitionWhenAllRequiredFieldsAreSet()
    {
        var builder = new DialectDefinitionBuilder();
        builder.SetIdentity("dialect", "1.0", "base");
        builder.SetModulePolicy(new ModulePolicy(["A"]));
        builder.SetBackendPolicy(new BackendPolicy([TestBackendIds.Cil]));
        builder.SetIntrinsicPolicy(new IntrinsicPolicy(["i"]));
        builder.SetOptimizerPolicy(new OptimizerPolicy(["o"]));
        builder.SetSecurityPolicy(null);
        builder.SetCapabilityPolicy(new CapabilityPolicy([new KeyValuePair<string, bool>("cap", true)]));
        builder.SetOrderRules([new OrderRule(OrderRuleKind.Requires, "A", "B")]);

        var definition = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(definition.Name, Is.EqualTo("dialect"));
            Assert.That(definition.Version, Is.EqualTo("1.0"));
            Assert.That(definition.BaseDialectName, Is.EqualTo("base"));
            Assert.That(definition.ModulePolicy.IncludedModules, Is.EqualTo(new[] { "A" }));
            Assert.That(definition.CapabilityPolicy.Capabilities["cap"], Is.True);
        });
    }

    private static DialectSyntaxDocument CreateSyntaxDocument(string name, string? version = null)
    {
        return new DialectSyntaxDocument(
            name,
            version,
            ["B", "A", "A"],
            ["Z", "Z"],
            [
                new OrderRule(OrderRuleKind.Requires, "A", "B"),
                new OrderRule(OrderRuleKind.Before, "B", "C"),
                new OrderRule(OrderRuleKind.After, "D", "C")
            ],
            [
                new BackendDirectiveSyntax(TestBackendIds.Interpreter, true),
                new BackendDirectiveSyntax(TestBackendIds.Cil, false)
            ],
            [
                new IntrinsicDirectiveSyntax("add_i32", true, TestBackendIds.Any),
                new IntrinsicDirectiveSyntax("unsafe_reflect", false, TestBackendIds.CilSelector)
            ],
            [
                new OptimizerDirectiveSyntax("const_fold", true, TestBackendIds.Any),
                new OptimizerDirectiveSyntax("aggressive_inline", false, TestBackendIds.InterpreterSelector)
            ],
            SecurityProfile.Restricted,
            [
                new KeyValuePair<string, bool>("supports-floats", true),
                new KeyValuePair<string, bool>("safe-interop", false)
            ]);
    }

    private static DialectDefinitionSlice CreateCompiledSlice(string name)
    {
        return new DialectDefinitionSlice(
            name,
            ["B", "A", "A"],
            ["Z", "Z"],
            [
                new DialectOrderDirective(DialectOrderDirectiveKind.Requires, "A", "B"),
                new DialectOrderDirective(DialectOrderDirectiveKind.Before, "B", "C"),
                new DialectOrderDirective(DialectOrderDirectiveKind.After, "D", "C")
            ],
            [
                new DialectBackendDirective(TestBackendIds.Interpreter, true),
                new DialectBackendDirective(TestBackendIds.Cil, false)
            ],
            [
                new DialectIntrinsicDirective("add_i32", true, TestBackendIds.Any),
                new DialectIntrinsicDirective("unsafe_reflect", false, TestBackendIds.CilSelector)
            ],
            [
                new DialectOptimizerDirective("const_fold", true, TestBackendIds.Any),
                new DialectOptimizerDirective("aggressive_inline", false, TestBackendIds.InterpreterSelector)
            ],
            DialectSecurityProfile.Restricted,
            [
                new DialectCapabilityDirective("supports-floats", true),
                new DialectCapabilityDirective("safe-interop", false)
            ]);
    }

    private static void AssertDefinitionsEqual(DialectDefinition expected, DialectDefinition actual)
    {
        Assert.Multiple(() =>
        {
            Assert.That(actual.Name, Is.EqualTo(expected.Name));
            Assert.That(actual.Version, Is.EqualTo(expected.Version));
            Assert.That(actual.BaseDialectName, Is.EqualTo(expected.BaseDialectName));
            Assert.That(actual.ModulePolicy.IncludedModules, Is.EqualTo(expected.ModulePolicy.IncludedModules));
            Assert.That(actual.ModulePolicy.ExcludedModules, Is.EqualTo(expected.ModulePolicy.ExcludedModules));
            Assert.That(actual.BackendPolicy.EnabledBackends, Is.EqualTo(expected.BackendPolicy.EnabledBackends));
            Assert.That(actual.BackendPolicy.DisabledBackends, Is.EqualTo(expected.BackendPolicy.DisabledBackends));
            Assert.That(actual.IntrinsicPolicy.AllowedIntrinsics, Is.EqualTo(expected.IntrinsicPolicy.AllowedIntrinsics));
            Assert.That(actual.IntrinsicPolicy.ForbiddenIntrinsics, Is.EqualTo(expected.IntrinsicPolicy.ForbiddenIntrinsics));
            Assert.That(actual.OptimizerPolicy.EnabledOptimizers, Is.EqualTo(expected.OptimizerPolicy.EnabledOptimizers));
            Assert.That(actual.OptimizerPolicy.DisabledOptimizers, Is.EqualTo(expected.OptimizerPolicy.DisabledOptimizers));
            Assert.That(actual.SecurityPolicy?.Profile, Is.EqualTo(expected.SecurityPolicy?.Profile));
            Assert.That(actual.CapabilityPolicy.Capabilities, Is.EqualTo(expected.CapabilityPolicy.Capabilities));
            Assert.That(
                actual.OrderRules.Select(x => (x.Kind, x.ModuleName, x.RelatedModuleName)),
                Is.EqualTo(expected.OrderRules.Select(x => (x.Kind, x.ModuleName, x.RelatedModuleName))));
        });
    }

    private sealed class TestBindingSource : IDialectBindingSource
    {
        public TestBindingSource(string name, string? version, string? baseDialectName)
        {
            Name = name;
            Version = version;
            BaseDialectName = baseDialectName;
        }

        public DialectBindingInputKind InputKind => DialectBindingInputKind.Syntax;

        public string Name { get; }

        public string? Version { get; }

        public string? BaseDialectName { get; }

        public IReadOnlyList<string> UseModules { get; } = [];

        public IReadOnlyList<string> ExcludeModules { get; } = [];

        public IReadOnlyList<OrderBindingDirectiveRecord> OrderRules { get; } = [];

        public IReadOnlyList<BackendBindingDirectiveRecord> BackendDirectives { get; } = [];

        public IReadOnlyList<IntrinsicBindingDirectiveRecord> IntrinsicDirectives { get; } = [];

        public IReadOnlyList<OptimizerBindingDirectiveRecord> OptimizerDirectives { get; } = [];

        public SecurityProfile? SecurityProfile => null;

        public IReadOnlyList<KeyValuePair<string, bool>> Capabilities { get; } = [];
    }
}
