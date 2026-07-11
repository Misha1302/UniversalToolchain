namespace BasicCore.Semantics;

public readonly record struct AstSemanticTagId
{
    public AstSemanticTagId(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            Thrower.Argument(nameof(value), "AST semantic tag id must not be empty.");

        Value = value;
    }

    public string Value { get; }

    public override string ToString() => Value;
}
