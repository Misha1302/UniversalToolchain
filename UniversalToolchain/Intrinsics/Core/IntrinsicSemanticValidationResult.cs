namespace UniversalToolchain.Intrinsics.Core;

public sealed class IntrinsicSemanticValidationResult
{
    public IntrinsicSemanticValidationResult(IEnumerable<string> errors)
    {
        if (errors == null)
            Thrower.ArgumentNull(nameof(errors));

        Errors = errors
            .Select(x => x.NotNull(nameof(errors)))
            .ToList();
        IsSuccess = Errors.Count == 0;
    }

    public bool IsSuccess { get; }

    public IReadOnlyList<string> Errors { get; }
}
