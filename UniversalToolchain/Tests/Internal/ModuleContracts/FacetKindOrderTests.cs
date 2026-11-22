using UniversalToolchain.Diagnostics.Abstractions;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

/// <summary>
///     Pins <c>UT-CONTRACT-009</c>, which was unreachable by construction.
/// </summary>
/// <remarks>
///     The old <c>ValidateComplete()</c> enumerated <see cref="Enum.GetValues{TEnum}" /> and reported values missing
///     from the order table. Every declared value is in the table, so it returned empty for every possible input and
///     the code could never be emitted. The check now takes the facets that are actually present.
///     <para>
///         These tests use a facet whose <see cref="ContractFacetKind" /> is outside the enum, because that is the
///         only input that can carry an unorderable kind. They also pin the ordering behaviour, which failed in two
///         different ways depending on how many such facets happened to be present: two threw a raw
///         <see cref="InvalidOperationException" /> from inside the sort, and one was accepted silently because a
///         one-element sort never invokes the key selector.
///     </para>
/// </remarks>
[TestFixture]
public sealed class FacetKindOrderTests
{
    private static readonly ModuleId Module = new("test.facet-order");

    private static readonly ContractFacetKind Unknown = (ContractFacetKind)99;

    [Test]
    public void Build_WhenSingleFacetHasUnknownKind_ReportsMissingFacetKindOrder()
    {
        // One facet is the case that used to pass silently: the sort never invoked the key selector.
        var table = new ModuleContractTableBuilder()
            .AddFacet(new SyntheticFacet(Module, Unknown))
            .Build();

        var diagnostics = table.Diagnostics
            .Where(static x => x.Code == ModuleContractDiagnosticCodes.MissingFacetKindOrder)
            .ToArray();

        Assert.That(diagnostics, Has.Length.EqualTo(1));
        Assert.That(diagnostics[0].Severity, Is.EqualTo(ToolchainDiagnosticSeverity.Error));
        Assert.That(diagnostics[0].Message, Does.Contain($"Module '{Module.Value}'"));
        Assert.That(diagnostics[0].Message, Does.Contain($"'{Unknown}'"));
    }

    [Test]
    public void Build_WhenTwoFacetsHaveUnknownKinds_ReportsOneDiagnosticPerFacetInsteadOfThrowing()
    {
        // Two facets used to throw a raw InvalidOperationException from inside the sort.
        var second = new ModuleId("test.facet-order.second");

        var table = new ModuleContractTableBuilder()
            .AddFacet(new SyntheticFacet(Module, Unknown))
            .AddFacet(new SyntheticFacet(second, Unknown))
            .Build();

        var diagnostics = table.Diagnostics
            .Where(static x => x.Code == ModuleContractDiagnosticCodes.MissingFacetKindOrder)
            .ToArray();

        Assert.That(diagnostics, Has.Length.EqualTo(2));
        // Compare whole quoted module ids: one id is a prefix of the other, so a substring check would pass twice.
        Assert.That(
            diagnostics.Select(static x => x.Message).OrderBy(static x => x, StringComparer.Ordinal),
            Is.EqualTo(new[]
            {
                $"Module '{Module.Value}' declares facet kind '{Unknown}', which has no deterministic order.",
                $"Module '{second.Value}' declares facet kind '{Unknown}', which has no deterministic order."
            }.OrderBy(static x => x, StringComparer.Ordinal)));
    }

    [Test]
    public void Build_WhenFacetHasKnownKind_ReportsNoMissingFacetKindOrder()
    {
        var table = new ModuleContractTableBuilder()
            .AddFacet(new SyntaxContractFacet(Module, [], []))
            .Build();

        Assert.That(
            table.Diagnostics.Select(static x => x.Code),
            Does.Not.Contain(ModuleContractDiagnosticCodes.MissingFacetKindOrder));
    }

    [Test]
    public void GetSortKeyOrLast_WhenKindIsUnknown_SortsAfterEveryKnownKind()
    {
        // Ordering must stay total: an unknown kind has to land somewhere deterministic rather than abort the sort.
        var everyKnownKey = Enum.GetValues<ContractFacetKind>()
            .Select(ContractFacetKindOrder.GetSortKeyOrLast)
            .ToArray();

        Assert.Multiple(() =>
        {
            Assert.That(ContractFacetKindOrder.GetSortKeyOrLast(Unknown),
                Is.EqualTo(ContractFacetKindOrder.UnknownKindSortKey));
            Assert.That(everyKnownKey, Has.None.EqualTo(ContractFacetKindOrder.UnknownKindSortKey));
        });
    }

    [Test]
    public void EveryDeclaredFacetKind_HasADeterministicOrder()
    {
        // This is the guard the tautological ValidateComplete() pretended to be. It lives in a test on purpose: it
        // is a statement about the source, not about a runtime input, so it has to fail when a developer adds an enum
        // member without an order entry - which no runtime diagnostic can observe, because the enum cannot contain a
        // value the table never declared.
        var unordered = Enum.GetValues<ContractFacetKind>()
            .Where(static kind => !ContractFacetKindOrder.IsKnown(kind))
            .ToArray();

        Assert.That(unordered, Is.Empty,
            "Every ContractFacetKind needs an entry in ContractFacetKindOrder.");
    }

    [Test]
    public void Validate_WhenFacetsAreEmpty_ReportsNothing()
    {
        Assert.That(ContractFacetKindOrder.Validate([]), Is.Empty);
    }

    private sealed record SyntheticFacet(ModuleId ModuleId, ContractFacetKind Kind) : IModuleContractFacet
    {
        public ContractSchemaVersion SchemaVersion { get; init; } = ModuleContractSchemaVersions.Current;
    }
}
