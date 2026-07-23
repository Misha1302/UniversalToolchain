namespace UniversalToolchain.Language.Abstractions;

/// <summary>
/// Stable language-artifact identity paired with the CLR value contract carried by that artifact.
/// The type identity intentionally excludes assembly version so package patch releases do not
/// invalidate otherwise compatible language plans.
/// </summary>
public readonly record struct LanguageArtifactContract
{
    public LanguageArtifactContract(LanguageArtifactKindId kind, string? valueTypeIdentity = null)
    {
        Kind = kind;
        ValueTypeIdentity = string.IsNullOrWhiteSpace(valueTypeIdentity) ? null : valueTypeIdentity.Trim();
    }

    public LanguageArtifactKindId Kind { get; }
    public string? ValueTypeIdentity { get; }
    public bool IsTyped => ValueTypeIdentity != null;

    public static LanguageArtifactContract Untyped(LanguageArtifactKindId kind) => new(kind);

    public override string ToString() => ValueTypeIdentity == null
        ? Kind.Value
        : $"{Kind.Value}<{ValueTypeIdentity}>";
}

public interface ILanguageArtifactKind
{
    LanguageArtifactKindId Id { get; }
    Type ValueType { get; }
    LanguageArtifactContract Contract { get; }
}

/// <summary>
/// A strongly typed artifact kind. Use the same instance in package descriptors and runtime
/// implementations to make the value contract impossible to duplicate accidentally.
/// </summary>
public sealed class LanguageArtifactKind<T> : ILanguageArtifactKind, IEquatable<LanguageArtifactKind<T>>
{
    public LanguageArtifactKind(string id)
        : this(new LanguageArtifactKindId(id), LanguageTypeIdentity.For<T>())
    {
    }

    public LanguageArtifactKind(string id, string contractIdentity)
        : this(new LanguageArtifactKindId(id), contractIdentity)
    {
    }

    public LanguageArtifactKind(LanguageArtifactKindId id)
        : this(id, LanguageTypeIdentity.For<T>())
    {
    }

    public LanguageArtifactKind(LanguageArtifactKindId id, string contractIdentity)
    {
        if (string.IsNullOrWhiteSpace(contractIdentity))
            throw new ArgumentException("Artifact contract identity must not be empty.", nameof(contractIdentity));
        Id = id;
        Contract = new LanguageArtifactContract(id, contractIdentity);
    }

    public LanguageArtifactKindId Id { get; }
    public Type ValueType => typeof(T);
    public LanguageArtifactContract Contract { get; }

    public bool Equals(LanguageArtifactKind<T>? other) => other != null && Contract == other.Contract;
    public override bool Equals(object? obj) => obj is LanguageArtifactKind<T> other && Equals(other);
    public override int GetHashCode() => Contract.GetHashCode();
    public override string ToString() => Contract.ToString();
}

public static class LanguageTypeIdentity
{
    public static string For<T>() => For(typeof(T));

    public static string For(Type type)
    {
        ArgumentNullException.ThrowIfNull(type);
        return BuildStableIdentity(type);
    }

    private static string BuildStableIdentity(Type type)
    {
        if (type.IsArray)
            return $"{BuildStableIdentity(type.GetElementType()!)}[{new string(',', type.GetArrayRank() - 1)}]";
        if (type.IsByRef)
            return BuildStableIdentity(type.GetElementType()!) + "&";
        if (type.IsPointer)
            return BuildStableIdentity(type.GetElementType()!) + "*";
        if (type.IsGenericParameter)
            return "`" + type.GenericParameterPosition;

        var assemblyName = type.Assembly.GetName().Name
            ?? throw new InvalidOperationException($"Type '{type}' has no assembly name.");
        if (!type.IsGenericType)
            return $"{type.FullName ?? type.Name}, {assemblyName}";

        var definition = type.GetGenericTypeDefinition();
        var definitionName = definition.FullName ?? definition.Name;
        var arguments = string.Join(",", type.GetGenericArguments().Select(BuildStableIdentity));
        return $"{definitionName}<{arguments}>, {assemblyName}";
    }
}

public static class StandardLanguageArtifactKinds
{
    public static LanguageArtifactKind<string> SourceText { get; } = new(LanguageArtifacts.SourceText);
}
