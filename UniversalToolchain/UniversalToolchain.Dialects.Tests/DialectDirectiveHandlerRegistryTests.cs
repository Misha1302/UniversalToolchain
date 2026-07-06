using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Core.Binding.Handlers;
using UniversalToolchain.Dialects.Core.Groups;
using UniversalToolchain.Dialects.Parsing;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDirectiveHandlerRegistryTests
{
    [Test]
    public void Registry_OrdersHandlersByOrderThenTypeName()
    {
        var applied = new List<string>();
        var registry = new DialectDirectiveHandlerRegistry(
        [
            new RegistryOrderingHandlerC(applied),
            new RegistryOrderingHandlerB(applied),
            new RegistryOrderingHandlerA(applied)
        ]);

        registry.Apply(new DialectDirectiveBindingContext(new TestBindingSource(), new DialectDefinitionBuilder(), []));

        Assert.Multiple(() =>
        {
            Assert.That(
                registry.Handlers.Select(static x => x.GetType().Name),
                Is.EqualTo(new[]
                {
                    nameof(RegistryOrderingHandlerA),
                    nameof(RegistryOrderingHandlerB),
                    nameof(RegistryOrderingHandlerC)
                }));
            Assert.That(applied, Is.EqualTo(new[] { "A", "B", "C" }));
        });
    }

    [Test]
    public void Registry_AppliesHandlersThroughExecutionContext()
    {
        var handler = new RegistryContextCapturingHandler();
        var registry = new DialectDirectiveHandlerRegistry([handler]);
        var source = new TestBindingSource { InputKind = DialectBindingInputKind.Compiled };
        var builder = new DialectDefinitionBuilder();
        var diagnostics = new List<DialectDiagnostic>();
        var context = new DialectDirectiveBindingContext(source, builder, diagnostics);

        registry.Apply(context);

        Assert.Multiple(() =>
        {
            Assert.That(handler.Context, Is.SameAs(context));
            Assert.That(handler.Source, Is.SameAs(source));
            Assert.That(handler.Builder, Is.SameAs(builder));
            Assert.That(handler.Diagnostics, Is.SameAs(diagnostics));
            Assert.That(handler.DirectiveContext, Is.Not.Null);
            Assert.That(handler.DirectiveContext!.BackendContradictionCode, Is.EqualTo("S102"));
            Assert.That(diagnostics.Select(static x => x.Code), Is.EqualTo(new[] { "T001" }));
        });
    }

    [Test]
    public void Registry_RejectsDuplicateSemanticHandlerFamilies()
    {
        Assert.That(
            () => new DialectDirectiveHandlerRegistry(
            [
                new DuplicateFamilyHandlerA(),
                new DuplicateFamilyHandlerB()
            ]),
            Throws.TypeOf<InvalidOperationException>().With.Message.Contain("DuplicateFamily"));
    }

    [Test]
    public void BuildPlanBuilder_CanUseInjectedSemanticHandlerWithoutEditingCentralBinder()
    {
        var document = new DialectSyntaxDocument(
            "Injected",
            null,
            [],
            [],
            [],
            [],
            [],
            [],
            null,
            [],
            null);
        var registry = new DialectDirectiveHandlerRegistry(
        [
            ..DialectDefinitionSemanticBinder.CreateDefaultDirectiveHandlerRegistry().Handlers,
            new InjectedWarningHandler()
        ]);
        var builder = new DialectBuildPlanBuilder(new DialectGroupExpander(new EmptyDialectGroupCatalog()), registry);

        var plan = builder.Build(document);

        Assert.That(plan.ValidationResult.Diagnostics.Select(static x => x.Code), Does.Contain("T900"));
    }

    private sealed class RegistryOrderingHandlerA(List<string> applied) : IDialectDirectiveHandler
    {
        public int Order => 0;

        public string Name => "A";

        public void Apply(DialectDirectiveBindingContext context)
        {
            applied.Add("A");
        }
    }

    private sealed class RegistryOrderingHandlerB(List<string> applied) : IDialectDirectiveHandler
    {
        public int Order => 0;

        public string Name => "B";

        public void Apply(DialectDirectiveBindingContext context)
        {
            applied.Add("B");
        }
    }

    private sealed class RegistryOrderingHandlerC(List<string> applied) : IDialectDirectiveHandler
    {
        public int Order => 10;

        public string Name => "C";

        public void Apply(DialectDirectiveBindingContext context)
        {
            applied.Add("C");
        }
    }

    private sealed class RegistryContextCapturingHandler : IDialectDirectiveHandler
    {
        public DialectDirectiveBindingContext? Context { get; private set; }

        public IDialectBindingSource? Source { get; private set; }

        public DialectDefinitionBuilder? Builder { get; private set; }

        public List<DialectDiagnostic>? Diagnostics { get; private set; }

        public DialectDirectiveHandlerContext? DirectiveContext { get; private set; }
        public int Order => 0;

        public string Name => "Capture";

        public void Apply(DialectDirectiveBindingContext context)
        {
            Context = context;
            Source = context.Source;
            Builder = context.Builder;
            Diagnostics = context.DiagnosticsList;
            DirectiveContext = context.DirectiveContext;
            context.AddDiagnostic(new DialectDiagnostic("T001", "Test diagnostic.", DialectDiagnosticSeverity.Info));
        }
    }

    private sealed class DuplicateFamilyHandlerA : IDialectDirectiveHandler
    {
        public int Order => 0;

        public string Name => "DuplicateFamily";

        public void Apply(DialectDirectiveBindingContext context)
        {
        }
    }

    private sealed class DuplicateFamilyHandlerB : IDialectDirectiveHandler
    {
        public int Order => 1;

        public string Name => "DuplicateFamily";

        public void Apply(DialectDirectiveBindingContext context)
        {
        }
    }

    private sealed class InjectedWarningHandler : IDialectDirectiveHandler
    {
        public int Order => 100;

        public string Name => "InjectedWarning";

        public void Apply(DialectDirectiveBindingContext context)
        {
            context.AddDiagnostic(new DialectDiagnostic("T900", "Injected semantic handler was applied.", DialectDiagnosticSeverity.Warning));
        }
    }

    private sealed class TestBindingSource : IDialectBindingSource
    {
        public DialectBindingInputKind InputKind { get; init; } = DialectBindingInputKind.Syntax;

        public string Name { get; } = "dialect";

        public string? Version => null;

        public string? BaseDialectName => null;

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
