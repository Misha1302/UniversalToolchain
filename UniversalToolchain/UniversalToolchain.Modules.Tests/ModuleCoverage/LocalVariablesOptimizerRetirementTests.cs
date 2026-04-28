using LocalVariablesOptimizerModule;
using UniversalToolchain.Dialects.Abstractions;

namespace UniversalToolchain.Modules.Tests.ModuleCoverage;

[TestFixture]
public sealed class LocalVariablesOptimizerRetirementTests
{
    [Test]
    public void LocalVariablesOptimizer_IsNotExportedAsDialectOptimizer()
    {
        var optimizerType = typeof(LocalVariablesOptimizer);

        Assert.That(optimizerType.GetCustomAttributes(typeof(DialectOptimizerAliasAttribute), inherit: false), Is.Empty);
        Assert.That(optimizerType.GetCustomAttributes(typeof(DialectRuntimeExportAttribute), inherit: false), Is.Empty);
    }
}
