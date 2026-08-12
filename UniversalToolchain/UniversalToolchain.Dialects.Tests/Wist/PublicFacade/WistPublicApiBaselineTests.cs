using System.Reflection;
using UniversalToolchain.Wist;

namespace UniversalToolchain.Dialects.Tests.Wist.PublicFacade;

public sealed class WistPublicApiBaselineTests
{
    [Test]
    public void ExportedFacadeApi_MatchesReviewedAlphaBaseline()
    {
        var baselinePath = FindRepositoryFile(
            "UniversalToolchain",
            "UniversalToolchain.Wist",
            "PublicAPI.Shipped.txt");
        var expected = File.ReadAllLines(baselinePath)
            .Where(static line => !string.IsNullOrWhiteSpace(line))
            .ToArray();
        var actual = Snapshot(typeof(WistEngine).Assembly).ToArray();

        Assert.That(actual, Is.EqualTo(expected),
            "The supported Wist facade changed. Review the compatibility impact and update PublicAPI.Shipped.txt intentionally.");
    }

    private static IReadOnlyList<string> Snapshot(Assembly assembly)
    {
        var lines = new List<string>();
        foreach (var type in assembly.GetExportedTypes().OrderBy(static type => type.FullName, StringComparer.Ordinal))
        {
            var typeName = FormatType(type);
            lines.Add($"type {Kind(type)} {typeName}");
            const BindingFlags flags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;

            foreach (var constructor in type.GetConstructors(flags).OrderBy(Signature, StringComparer.Ordinal))
                lines.Add($"ctor {typeName}({Parameters(constructor.GetParameters())})");

            foreach (var field in type.GetFields(flags).OrderBy(static field => field.Name, StringComparer.Ordinal))
            {
                var suffix = field.IsLiteral ? $" = {field.GetRawConstantValue() ?? "null"}" : string.Empty;
                lines.Add($"field {FormatType(field.FieldType)} {typeName}.{field.Name}{suffix}");
            }

            foreach (var property in type.GetProperties(flags).OrderBy(static property => property.Name, StringComparer.Ordinal))
            {
                var accessors = new List<string>();
                if (property.GetMethod?.IsPublic == true)
                    accessors.Add("get");
                if (property.SetMethod?.IsPublic == true)
                    accessors.Add("set");
                var index = property.GetIndexParameters();
                var name = index.Length == 0
                    ? property.Name
                    : $"{property.Name}[{Parameters(index)}]";
                lines.Add($"property {FormatType(property.PropertyType)} {typeName}.{name} {{ {string.Join("; ", accessors)}; }}");
            }

            foreach (var eventInfo in type.GetEvents(flags).OrderBy(static eventInfo => eventInfo.Name, StringComparer.Ordinal))
                lines.Add($"event {FormatType(eventInfo.EventHandlerType!)} {typeName}.{eventInfo.Name}");

            foreach (var method in type.GetMethods(flags)
                         .Where(static method => !method.IsSpecialName)
                         .OrderBy(Signature, StringComparer.Ordinal))
            {
                var generic = method.IsGenericMethodDefinition ? $"``{method.GetGenericArguments().Length}" : string.Empty;
                lines.Add($"method {FormatType(method.ReturnType)} {typeName}.{method.Name}{generic}({Parameters(method.GetParameters())})");
            }
        }

        return lines;
    }

    private static string Signature(MethodBase method) =>
        $"{method.Name}({Parameters(method.GetParameters())})";

    private static string Parameters(IEnumerable<ParameterInfo> parameters) =>
        string.Join(", ", parameters.Select(static parameter => $"{FormatType(parameter.ParameterType)} {parameter.Name}"));

    private static string Kind(Type type) =>
        type.IsEnum ? "enum" :
        type.IsInterface ? "interface" :
        type.IsValueType ? "struct" :
        type.IsAbstract && type.IsSealed ? "static-class" :
        type.IsAbstract ? "abstract-class" :
        "class";

    private static string FormatType(Type type)
    {
        if (type.IsByRef)
            return FormatType(type.GetElementType()!) + "&";
        if (type.IsPointer)
            return FormatType(type.GetElementType()!) + "*";
        if (type.IsArray)
            return FormatType(type.GetElementType()!) + "[]";
        if (type.IsGenericParameter)
            return type.Name;
        if (!type.IsGenericType)
            return type.FullName ?? type.Name;

        var definition = type.GetGenericTypeDefinition();
        var name = (definition.FullName ?? definition.Name).Split('`')[0];
        return $"{name}<{string.Join(", ", type.GetGenericArguments().Select(FormatType))}>";
    }

    private static string FindRepositoryFile(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
        while (directory != null)
        {
            var candidate = Path.Combine(new[] { directory.FullName }.Concat(relativeParts).ToArray());
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }

        throw new FileNotFoundException("Could not locate repository file.", Path.Combine(relativeParts));
    }
}
