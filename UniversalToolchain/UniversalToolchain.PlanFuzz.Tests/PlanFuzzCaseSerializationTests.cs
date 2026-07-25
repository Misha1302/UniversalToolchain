using UniversalToolchain.PlanFuzz.Adapter.Acme;

namespace UniversalToolchain.PlanFuzz.Tests;

[TestFixture]
public sealed class PlanFuzzCaseSerializationTests
{
    [Test]
    public void SameSeedAndIndexProduceIdenticalCanonicalTestcase()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var first = adapter.GenerateCase(77, 19, new PlanFuzzCaseGenerationOptions());
        var second = adapter.GenerateCase(77, 19, new PlanFuzzCaseGenerationOptions());

        Assert.That(PlanFuzzTestCaseSerializer.Serialize(first), Is.EqualTo(PlanFuzzTestCaseSerializer.Serialize(second)));
        Assert.That(first.CaseId, Is.EqualTo(second.CaseId));
    }

    [Test]
    public void TestcaseRoundtripPreservesCaseIdentity()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var original = adapter.GenerateCase(123, 7, new PlanFuzzCaseGenerationOptions());
        var serialized = PlanFuzzTestCaseSerializer.Serialize(original);
        var roundtripped = PlanFuzzTestCaseSerializer.Deserialize(serialized);

        Assert.Multiple(() =>
        {
            Assert.That(roundtripped.CaseId, Is.EqualTo(original.CaseId));
            Assert.That(roundtripped.Program.SourceText, Is.EqualTo(original.Program.SourceText));
            Assert.That(roundtripped.Variants.Select(static item => item.VariantId),
                Is.EqualTo(original.Variants.Select(static item => item.VariantId)));
        });
    }

    [Test]
    public void CaseIdentityDetectsRecordedContentTampering()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var original = adapter.GenerateCase(123, 7, new PlanFuzzCaseGenerationOptions());
        var serialized = PlanFuzzTestCaseSerializer.Serialize(original);
        var tampered = serialized.Replace(original.Program.SourceText, "1 * 1 - 1", StringComparison.Ordinal);

        Assert.That(
            () => PlanFuzzTestCaseSerializer.Deserialize(tampered),
            Throws.TypeOf<InvalidOperationException>());
    }
    [Test]
    public void ContractReductionPrunesUnreferencedVariantsWithoutChangingProvenance()
    {
        var adapter = new AcmePlanFuzzAdapter();
        var original = adapter.GenerateCase(
            123,
            0,
            new PlanFuzzCaseGenerationOptions(AcmePlanFuzzConstants.WrongArithmeticFault));
        var violationContract = original.OracleContracts.Single(static contract =>
            contract.ContractId == "backend-parity.seeded-wrong-arithmetic");

        var reduced = PlanFuzzTestCaseTransform.WithContractsAndReferencedVariants(
            original,
            [violationContract]);

        Assert.Multiple(() =>
        {
            Assert.That(reduced.OracleContracts.Select(static contract => contract.ContractId),
                Is.EqualTo(new[] { "backend-parity.seeded-wrong-arithmetic" }));
            Assert.That(reduced.Variants.Select(static variant => variant.VariantId),
                Is.EqualTo(new[] { "baseline.interpreter", "seeded-wrong-arithmetic.compiled" }));
            Assert.That(reduced.CampaignSeed, Is.EqualTo(original.CampaignSeed));
            Assert.That(reduced.CaseIndex, Is.EqualTo(original.CaseIndex));
            Assert.That(reduced.CaseSeed, Is.EqualTo(original.CaseSeed));
            Assert.That(reduced.CaseId, Is.Not.EqualTo(original.CaseId));
        });
    }

}
