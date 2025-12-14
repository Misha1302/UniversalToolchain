# Main ideas (what is need todo)

## 1. Fix parser read and dump configuration

## 2. Make an application for configuration changing

1. Read a configuration that was dumped by plugin (like Logs Viewer)
2. Change order for parser configuration by hand

## 3. Make the same for bytecode operations

## 4. Make attributes and DI for modules (which are implement ICoreModule)

For example:

```C#
[ParserNodePriority("equalityParserPriority", 10f)]
[LexerLexemePriority("equalityLexerPriority", 100f)]
public class EqualityModuleImpl : ICoreModule
{
    private int _equalityParserPriority;
    private int _equalityLexerPriority;
    
    public void InitializeParserPriorities(int equalityParserPriority, int equalityLexerPriority)
    {
        _equalityParserPriority = equalityParserPriority;
        _equalityLexerPriority = equalityLexerPriority;
    }
    
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\=", LexemeType.CreateOrGet("Equality")), 
            priority: _equalityLexerPriority
        );
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(
            _equalityParserPriority, 
            new ValuesSetNodeCreator()
        );
    }

    public void InitTranslator(IBytecodeTranslator translator)
    {
        ...
    }
}
```