using System.Reflection;
using UniversalToolchain.Dialects.Integration;

namespace UniversalToolchain.Dialects.Tests;

public class DialectCompositionExplainabilityArchitectureGuardrailTests
{
    [Test]
    public void ExplainabilitySurface_DoesNotDependOnWistSpecificConcepts()
    {
        var explainabilityTypes = new[]
        {
            typeof(IDialectResolvedRuntimeSelection),
            typeof(DialectCompositionExplanation),
            typeof(DialectBuildPlanExplanation),
            typeof(DialectRuntimeSelectionExplanation),
            typeof(DialectCompositionExplanationProjector),
            typeof(DialectCompositionExplanationFormatter)
        };

        var dependencyTypes = explainabilityTypes
            .SelectMany(GetReferencedTypes)
            .Where(static x => x.Namespace != null)
            .ToArray();

        var wistDependencies = dependencyTypes
            .Where(static x => x.Namespace!.StartsWith("UniversalToolchain.Dialects.Wist", StringComparison.Ordinal))
            .Select(static x => x.FullName ?? x.Name)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        Assert.That(wistDependencies, Is.Empty);
    }

    private static IEnumerable<Type> GetReferencedTypes(Type type)
    {
        foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static))
            yield return property.PropertyType;

        foreach (var method in type.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly))
        {
            yield return method.ReturnType;
            foreach (var parameter in method.GetParameters())
                yield return parameter.ParameterType;
        }
    }
}
