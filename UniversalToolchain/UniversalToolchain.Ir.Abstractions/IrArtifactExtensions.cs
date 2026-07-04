namespace UniversalToolchain.Ir.Abstractions;

public static class IrArtifactExtensions
{
    public static TArtifact As<TArtifact>(this IIrArtifact artifact)
        where TArtifact : class, IIrArtifact
    {
        artifact = artifact ?? throw new ArgumentNullException(nameof(artifact));

        if (artifact is TArtifact typedArtifact)
            return typedArtifact;

        throw new InvalidOperationException(
            $"IR artifact kind '{artifact.Kind}' cannot be used as '{typeof(TArtifact).Name}'.");
    }
}
