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
}
