namespace UniversalToolchain.ExpressionTyping.Abstractions;

public interface IExpressionTypeRule
{
    bool TryInfer(
        ExpressionTypeResolutionContext context,
        object node,
        IExpressionTypeResolver resolver,
        out ExpressionTypeDescriptor? type);
}
