using NumbersModule.Contracts;
using UniversalToolchain.ModuleContracts;

namespace Tests.Internal.ModuleContracts;

[TestFixture]
public sealed class BytecodeDriftReporterTests
{
    [Test]
    public void CreateReport_WhenNumbersObservedEmissionMatchesDeclaration_ReturnsNoDrift()
    {
        var table = CreateNumbersTable();
        var observed = new ObservedBytecodeEmission(
            NumbersContractIds.Module,
            NumbersContractIds.NumberNode,
            [],
            [NumbersContractIds.PushRealNumber]);

        var report = new BytecodeDriftReporter().CreateReport(table, [observed]);

        Assert.That(report.HasDrift, Is.False);
        Assert.That(report.Modules.Single().ModuleId, Is.EqualTo(NumbersContractIds.Module));
    }

    [Test]
    public void CreateReport_WhenNumbersObservedEmissionIsNotDeclared_ReportsObservedPatternDrift()
    {
        var table = CreateNumbersTable();
        var extraPattern = new BytecodePatternId("wist.numbers.bytecode.extra");
        var observed = new ObservedBytecodeEmission(
            NumbersContractIds.Module,
            NumbersContractIds.NumberNode,
            [],
            [NumbersContractIds.PushRealNumber, extraPattern]);

        var report = new BytecodeDriftReporter().CreateReport(table, [observed]);

        Assert.That(report.HasDrift, Is.True);
        Assert.That(report.Modules.Single().ObservedUndeclaredPatterns, Is.EqualTo(new[] { extraPattern }));
    }

    private static SelectedModuleContractTable CreateNumbersTable() =>
        new ModuleContractTableBuilder()
            .AddFacets(new NumbersModuleContractDescriptorProvider().GetFacets())
            .Build();
}
