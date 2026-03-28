using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;

namespace UniversalToolchain.Dialects.Frontend;

public interface IDialectDirectiveFeature
{
    string Id { get; }

    string Keyword { get; }

    string LexemeTag { get; }

    DialectDirectiveParserOrder ParserOrder { get; }

    bool IsSingleton { get; }

    string SingletonViolationMessage { get; }

    DialectDirectiveAstNode ParseDirective(AstNode lineNode);

    void Accumulate(IReadOnlyList<LexemeValue> line, DialectDirectiveAccumulation accumulation);

    void ValidateSemantic(DialectDirectiveAstNode directive, DialectDirectiveValidationContext context);

    IReadOnlyList<IDialectDefinitionSliceAnnotation> Lower(DialectDirectiveAstNode directive);
}

public interface IDialectDocumentValidationRule
{
    int Order { get; }

    void Validate(DialectDocumentAstNode document, DialectDirectiveValidationContext context);
}

public interface IDialectDslFeatureProvider
{
    int Order { get; }

    void Register(DialectDslRegistryBuilder builder);
}