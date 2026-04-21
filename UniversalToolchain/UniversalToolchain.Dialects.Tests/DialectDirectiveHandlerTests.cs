using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Core.Binding.Handlers;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDirectiveHandlerTests
{
    [Test]
    public void Registry_AppliesHandlersInDeterministicOrder()
    {
        var applied = new List<string>();
        var registry = new DialectDirectiveHandlerRegistry(
        [
            new RecordingDirectiveHandlerB(applied),
            new RecordingDirectiveHandlerLate(applied),
            new RecordingDirectiveHandlerA(applied)
        ]);

        registry.Apply(CreateContext(new TestBindingSource(), new DialectDefinitionBuilder(), []));

        Assert.Multiple(() =>
        {
            Assert.That(
                registry.Handlers.Select(x => x.GetType().Name),
                Is.EqualTo(new[] { nameof(RecordingDirectiveHandlerA), nameof(RecordingDirectiveHandlerB), nameof(RecordingDirectiveHandlerLate) }));
            Assert.That(applied, Is.EqualTo(new[] { "A", "B", "Late" }));
        });
    }

    [Test]
    public void Registry_RejectsNullHandler()
    {
        Assert.That(
            () => new DialectDirectiveHandlerRegistry([new IntrinsicDirectiveHandler(), null!]),
            Throws.TypeOf<ArgumentNullException>());
    }

    [Test]
    public void ModuleHandler_NormalizesAndSetsPolicy()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var builder = CreateBuilderWithPolicyDefaults();
        var source = new TestBindingSource
        {
            UseModules = ["Variables", "Arithmetic", "Arithmetic", "UnsafeInterop"],
            ExcludeModules = ["UnsafeInterop"]
        };

        new ModuleDirectiveHandler().Apply(CreateContext(source, builder, diagnostics));
        var definition = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(definition.ModulePolicy.IncludedModules, Is.EqualTo(new[] { "Variables", "Arithmetic" }));
            Assert.That(definition.ModulePolicy.ExcludedModules, Is.EqualTo(new[] { "UnsafeInterop" }));
            Assert.That(diagnostics.Select(x => x.Code), Is.EqualTo(new[] { "S001" }));
        });
    }

    [Test]
    public void BackendHandler_NormalizesAndSetsPolicy()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var builder = CreateBuilderWithPolicyDefaults();
        var source = new TestBindingSource
        {
            InputKind = DialectBindingInputKind.Compiled,
            BackendDirectives =
            [
                new BackendBindingDirectiveRecord(TestBackendIds.Interpreter, true),
                new BackendBindingDirectiveRecord(TestBackendIds.Cil, false),
                new BackendBindingDirectiveRecord(TestBackendIds.Interpreter, true),
                new BackendBindingDirectiveRecord(TestBackendIds.Interpreter, false)
            ]
        };

        new BackendDirectiveHandler().Apply(CreateContext(source, builder, diagnostics));
        var definition = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(definition.BackendPolicy.EnabledBackends, Is.EqualTo(new[] { TestBackendIds.Interpreter }));
            Assert.That(definition.BackendPolicy.DisabledBackends, Is.EqualTo(new[] { TestBackendIds.Cil }));
            Assert.That(diagnostics.Select(x => x.Code), Is.EqualTo(new[] { "S102" }));
        });
    }

    [Test]
    public void IntrinsicHandler_NormalizesAndSetsPolicy()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var builder = CreateBuilder();
        builder.SetOptimizerPolicy(new OptimizerPolicy());
        builder.SetSecurityPolicy(null);
        builder.SetCapabilityPolicy(new CapabilityPolicy());
        var source = new TestBindingSource
        {
            IntrinsicDirectives =
            [
                new IntrinsicBindingDirectiveRecord("add_i32", TestBackendIds.Any, true),
                new IntrinsicBindingDirectiveRecord("unsafe_reflect", TestBackendIds.CilSelector, false),
                new IntrinsicBindingDirectiveRecord("add_i32", TestBackendIds.Any, true),
                new IntrinsicBindingDirectiveRecord("add_i32", TestBackendIds.Any, false)
            ]
        };

        new IntrinsicDirectiveHandler().Apply(CreateContext(source, builder, diagnostics));
        var definition = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(definition.IntrinsicPolicy.AllowedIntrinsics, Is.EqualTo(new[] { "add_i32" }));
            Assert.That(definition.IntrinsicPolicy.ForbiddenIntrinsics, Is.EqualTo(new[] { "unsafe_reflect@cil" }));
            Assert.That(diagnostics.Select(x => x.Code), Is.EqualTo(new[] { "S004" }));
        });
    }

    [Test]
    public void OptimizerHandler_NormalizesAndSetsPolicy()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var builder = CreateBuilder();
        builder.SetIntrinsicPolicy(new IntrinsicPolicy());
        builder.SetSecurityPolicy(null);
        builder.SetCapabilityPolicy(new CapabilityPolicy());
        var source = new TestBindingSource
        {
            InputKind = DialectBindingInputKind.Compiled,
            OptimizerDirectives =
            [
                new OptimizerBindingDirectiveRecord("const_fold", TestBackendIds.Any, true),
                new OptimizerBindingDirectiveRecord("aggressive_inline", TestBackendIds.InterpreterSelector, false),
                new OptimizerBindingDirectiveRecord("const_fold", TestBackendIds.Any, false)
            ]
        };

        new OptimizerDirectiveHandler().Apply(CreateContext(source, builder, diagnostics));
        var definition = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(definition.OptimizerPolicy.EnabledOptimizers, Is.EqualTo(new[] { "const_fold" }));
            Assert.That(definition.OptimizerPolicy.DisabledOptimizers, Is.EqualTo(new[] { "aggressive_inline@interpreter" }));
            Assert.That(diagnostics.Select(x => x.Code), Is.EqualTo(new[] { "S104" }));
        });
    }

    [Test]
    public void SecurityHandler_SetsPresentAndMissingPolicies()
    {
        var presentBuilder = CreateBuilderWithPolicyDefaults();
        var missingBuilder = CreateBuilderWithPolicyDefaults();

        new SecurityDirectiveHandler().Apply(CreateContext(
            new TestBindingSource { SecurityProfile = SecurityProfile.Restricted },
            presentBuilder,
            []));
        new SecurityDirectiveHandler().Apply(CreateContext(new TestBindingSource(), missingBuilder, []));

        Assert.Multiple(() =>
        {
            Assert.That(presentBuilder.Build().SecurityPolicy?.Profile, Is.EqualTo(SecurityProfile.Restricted));
            Assert.That(missingBuilder.Build().SecurityPolicy, Is.Null);
        });
    }

    [Test]
    public void CapabilityHandler_SortsCapabilitiesDeterministically()
    {
        var builder = CreateBuilder();
        builder.SetIntrinsicPolicy(new IntrinsicPolicy());
        builder.SetOptimizerPolicy(new OptimizerPolicy());
        builder.SetSecurityPolicy(null);
        var source = new TestBindingSource
        {
            Capabilities =
            [
                new KeyValuePair<string, bool>("z-cap", true),
                new KeyValuePair<string, bool>("a-cap", false)
            ]
        };

        new CapabilityDirectiveHandler().Apply(CreateContext(source, builder, []));
        var definition = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(definition.CapabilityPolicy.Capabilities.Keys, Is.EqualTo(new[] { "a-cap", "z-cap" }));
            Assert.That(definition.CapabilityPolicy.Capabilities["a-cap"], Is.False);
            Assert.That(definition.CapabilityPolicy.Capabilities["z-cap"], Is.True);
        });
    }

    [Test]
    public void HandlerRegistry_BuildsLegacyEquivalentPolicies()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var builder = CreateBuilder();
        var source = CreateFullSource();
        var registry = new DialectDirectiveHandlerRegistry(
        [
            new BackendDirectiveHandler(),
            new ModuleDirectiveHandler(),
            new SecurityDirectiveHandler(),
            new CapabilityDirectiveHandler(),
            new OptimizerDirectiveHandler(),
            new IntrinsicDirectiveHandler()
        ]);

        registry.Apply(CreateContext(source, builder, diagnostics));
        var definition = builder.Build();

        Assert.Multiple(() =>
        {
            Assert.That(definition.ModulePolicy.IncludedModules, Is.EqualTo(new[] { "Variables", "Arithmetic" }));
            Assert.That(definition.ModulePolicy.ExcludedModules, Is.EqualTo(new[] { "UnsafeInterop" }));
            Assert.That(definition.BackendPolicy.EnabledBackends, Is.EqualTo(new[] { TestBackendIds.Interpreter }));
            Assert.That(definition.BackendPolicy.DisabledBackends, Is.EqualTo(new[] { TestBackendIds.Cil }));
        });
        AssertPolicyShape(definition);
        Assert.That(diagnostics, Is.Empty);
    }

    [Test]
    public void BindCore_FullPipelineAppliesCoreRulesAndDirectiveHandlers()
    {
        var diagnostics = new List<DialectDiagnostic>();

        var definition = DialectDefinitionSemanticBinder.BindCore(CreateFullSource(), diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(definition.ModulePolicy.IncludedModules, Is.EqualTo(new[] { "Variables", "Arithmetic" }));
            Assert.That(definition.ModulePolicy.ExcludedModules, Is.EqualTo(new[] { "UnsafeInterop" }));
            Assert.That(definition.BackendPolicy.EnabledBackends, Is.EqualTo(new[] { TestBackendIds.Interpreter }));
            Assert.That(definition.BackendPolicy.DisabledBackends, Is.EqualTo(new[] { TestBackendIds.Cil }));
            Assert.That(definition.OrderRules.Select(x => (x.Kind, x.ModuleName, x.RelatedModuleName)), Is.EqualTo(new[] { (OrderRuleKind.Before, "Arithmetic", "Variables") }));
            Assert.That(diagnostics, Is.Empty);
        });
        AssertPolicyShape(definition);
    }

    private static DialectDefinitionBuilder CreateBuilder()
    {
        var builder = new DialectDefinitionBuilder();
        builder.SetIdentity("dialect", "1.0", "base");
        builder.SetModulePolicy(new ModulePolicy(["Arithmetic"]));
        builder.SetBackendPolicy(new BackendPolicy([TestBackendIds.Interpreter]));
        builder.SetOrderRules([]);
        return builder;
    }

    private static DialectBindingExecutionContext CreateContext(IDialectBindingSource source, DialectDefinitionBuilder builder, List<DialectDiagnostic> diagnostics) => new(source, builder, diagnostics);

    private static DialectDefinitionBuilder CreateBuilderWithPolicyDefaults()
    {
        var builder = CreateBuilder();
        builder.SetIntrinsicPolicy(new IntrinsicPolicy());
        builder.SetOptimizerPolicy(new OptimizerPolicy());
        builder.SetSecurityPolicy(null);
        builder.SetCapabilityPolicy(new CapabilityPolicy());
        return builder;
    }

    private static TestBindingSource CreateFullSource() =>
        new()
        {
            Name = "dialect",
            Version = "1.0",
            BaseDialectName = "base",
            UseModules = ["Variables", "Arithmetic", "Arithmetic"],
            ExcludeModules = ["UnsafeInterop"],
            OrderRules = [new OrderBindingDirectiveRecord(OrderRuleKind.Before, "Arithmetic", "Variables")],
            BackendDirectives =
            [
                new BackendBindingDirectiveRecord(TestBackendIds.Interpreter, true),
                new BackendBindingDirectiveRecord(TestBackendIds.Cil, false)
            ],
            IntrinsicDirectives =
            [
                new IntrinsicBindingDirectiveRecord("add_i32", TestBackendIds.Any, true),
                new IntrinsicBindingDirectiveRecord("unsafe_reflect", TestBackendIds.CilSelector, false)
            ],
            OptimizerDirectives =
            [
                new OptimizerBindingDirectiveRecord("const_fold", TestBackendIds.Any, true),
                new OptimizerBindingDirectiveRecord("aggressive_inline", TestBackendIds.InterpreterSelector, false)
            ],
            SecurityProfile = SecurityProfile.Restricted,
            Capabilities =
            [
                new KeyValuePair<string, bool>("supports-floats", true),
                new KeyValuePair<string, bool>("safe-interop", false)
            ]
        };

    private static void AssertPolicyShape(DialectDefinition definition)
    {
        Assert.Multiple(() =>
        {
            Assert.That(definition.IntrinsicPolicy.AllowedIntrinsics, Is.EqualTo(new[] { "add_i32" }));
            Assert.That(definition.IntrinsicPolicy.ForbiddenIntrinsics, Is.EqualTo(new[] { "unsafe_reflect@cil" }));
            Assert.That(definition.OptimizerPolicy.EnabledOptimizers, Is.EqualTo(new[] { "const_fold" }));
            Assert.That(definition.OptimizerPolicy.DisabledOptimizers, Is.EqualTo(new[] { "aggressive_inline@interpreter" }));
            Assert.That(definition.SecurityPolicy?.Profile, Is.EqualTo(SecurityProfile.Restricted));
            Assert.That(definition.CapabilityPolicy.Capabilities.Keys, Is.EqualTo(new[] { "safe-interop", "supports-floats" }));
            Assert.That(definition.CapabilityPolicy.Capabilities["safe-interop"], Is.False);
            Assert.That(definition.CapabilityPolicy.Capabilities["supports-floats"], Is.True);
        });
    }

    private sealed class TestBindingSource : IDialectBindingSource
    {
        public DialectBindingInputKind InputKind { get; init; } = DialectBindingInputKind.Syntax;

        public string Name { get; init; } = "dialect";

        public string? Version { get; init; }

        public string? BaseDialectName { get; init; }

        public IReadOnlyList<string> UseModules { get; init; } = [];

        public IReadOnlyList<string> ExcludeModules { get; init; } = [];

        public IReadOnlyList<OrderBindingDirectiveRecord> OrderRules { get; init; } = [];

        public IReadOnlyList<BackendBindingDirectiveRecord> BackendDirectives { get; init; } = [];

        public IReadOnlyList<IntrinsicBindingDirectiveRecord> IntrinsicDirectives { get; init; } = [];

        public IReadOnlyList<OptimizerBindingDirectiveRecord> OptimizerDirectives { get; init; } = [];

        public SecurityProfile? SecurityProfile { get; init; }

        public IReadOnlyList<KeyValuePair<string, bool>> Capabilities { get; init; } = [];
    }

    private sealed class RecordingDirectiveHandlerA(List<string> applied) : IDialectDirectiveHandler
    {
        public int Order => 0;

        public string Name => "A";

        public void Apply(DialectBindingExecutionContext context)
        {
            applied.Add("A");
        }
    }

    private sealed class RecordingDirectiveHandlerB(List<string> applied) : IDialectDirectiveHandler
    {
        public int Order => 0;

        public string Name => "B";

        public void Apply(DialectBindingExecutionContext context)
        {
            applied.Add("B");
        }
    }

    private sealed class RecordingDirectiveHandlerLate(List<string> applied) : IDialectDirectiveHandler
    {
        public int Order => 10;

        public string Name => "Late";

        public void Apply(DialectBindingExecutionContext context)
        {
            applied.Add("Late");
        }
    }
}
