using BasicCore.Core;
using BytecodeDynamicMethodsCompiler.Compilers;

namespace UniversalToolchain.LanguageSdk.Generic.Tests;

[TestFixture]
public sealed class CilIntrinsicRegistryInternalTests
{
    [Test]
    public void CilRegistryDefinesDedicatedHandlersForAllCapabilityDescriptors()
    {
        var names = CilIntrinsicRegistry.CapabilityIds
            .Select(capabilityId => CilIntrinsicRegistry.GetRequired(capabilityId).Name)
            .ToArray();

        Assert.That(names, Has.All.Not.Empty);
        Assert.That(names.Distinct(StringComparer.Ordinal).Count(), Is.EqualTo(names.Length));
    }

    [Test]
    public void CilRegistryTypeHandlersMatchSharedIntrinsicTypeProcessor()
    {
        foreach (var capabilityId in CilIntrinsicRegistry.CapabilityIds)
        {
            var descriptor = CilIntrinsicRegistry.GetRequired(capabilityId);
            var instruction = IntrinsicInstructionFactory.CreateForCapability(capabilityId);
            var expected = new List<Type>();
            var actual = new List<Type>();

            Exception? expectedError = null;
            Exception? actualError = null;
            try
            {
                IntrinsicTypeProcessor.ProcessTypes(instruction, expected);
            }
            catch (Exception exception)
            {
                expectedError = exception;
            }

            try
            {
                descriptor.ProcessTypes(instruction, actual);
            }
            catch (Exception exception)
            {
                actualError = exception;
            }

            Assert.Multiple(() =>
            {
                Assert.That(actualError?.GetType(), Is.EqualTo(expectedError?.GetType()), capabilityId);
                Assert.That(actual, Is.EqualTo(expected), capabilityId);
            });
        }
    }
}
