using IntermediateRepresentationAbstractions;
using UniversalIntermediateRepresentation;
using UniversalToolchain.Dialects.Abstractions;
using UniversalToolchain.Dialects.Core;
using UniversalToolchain.Dialects.Core.Binding;
using UniversalToolchain.Dialects.Frontend;

namespace UniversalToolchain.Dialects.Tests;

public class DialectDefinitionSliceAirReaderMetadataTests
{
    [Test]
    public void AirReader_Read_PreservesVersionAnnotation()
    {
        var air = CreateAir(
            new DialectNameAirAnnotation("dialect"),
            new DialectVersionAirAnnotation("1.2.3"));

        var slice = DialectDefinitionSliceAirReader.Read(air);

        Assert.That(slice.Version, Is.EqualTo("1.2.3"));
    }

    [Test]
    public void AirReader_Read_PreservesBaseDialectAnnotation()
    {
        var air = CreateAir(
            new DialectNameAirAnnotation("dialect"),
            new BaseDialectAirAnnotation("base"));

        var slice = DialectDefinitionSliceAirReader.Read(air);

        Assert.That(slice.BaseDialectName, Is.EqualTo("base"));
    }

    [Test]
    public void AirReader_Read_ThrowsOnDuplicateVersionAnnotation()
    {
        var air = CreateAir(
            new DialectNameAirAnnotation("dialect"),
            new DialectVersionAirAnnotation("1.0"),
            new DialectVersionAirAnnotation("2.0"));

        Assert.Throws<InvalidOperationException>(() => DialectDefinitionSliceAirReader.Read(air));
    }

    [Test]
    public void AirReader_Read_ThrowsOnDuplicateBaseDialectAnnotation()
    {
        var air = CreateAir(
            new DialectNameAirAnnotation("dialect"),
            new BaseDialectAirAnnotation("base"),
            new BaseDialectAirAnnotation("other-base"));

        Assert.Throws<InvalidOperationException>(() => DialectDefinitionSliceAirReader.Read(air));
    }

    [Test]
    public void CompiledAirPath_BindCore_PreservesVersionAndBaseDialectName()
    {
        var diagnostics = new List<DialectDiagnostic>();
        var air = CreateAir(
            new DialectNameAirAnnotation("dialect"),
            new DialectVersionAirAnnotation("2.0"),
            new BaseDialectAirAnnotation("base"));
        var slice = DialectDefinitionSliceAirReader.Read(air);

        var definition = DialectDefinitionSemanticBinder.BindCore(new CompiledDialectBindingSource(slice), diagnostics);

        Assert.Multiple(() =>
        {
            Assert.That(definition.Version, Is.EqualTo("2.0"));
            Assert.That(definition.BaseDialectName, Is.EqualTo("base"));
            Assert.That(diagnostics, Is.Empty);
        });
    }

    private static IAbstractIR CreateAir(params object[] annotations)
    {
        var air = new AbstractIR();
        air.AppendInstructions([
            new Instruction(UOpCode.Annotate, metadata: annotations.ToList())
        ]);

        return air;
    }
}