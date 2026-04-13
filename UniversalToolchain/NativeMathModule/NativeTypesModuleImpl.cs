namespace NativeMathModule;

[DialectModuleAlias("NativeTypes")]
[DialectRuntimeExport("FrontendModule", "NativeTypes")]
[AutoRegisterService]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
public class NativeTypesModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"\d+(?:_?\d+)*(?:\.\d+(?:_?\d+)*)?(?:[eE][+-]?\d+(?:_?\d+)*)?[fdmlFDML]?",
                ExtensibleEnum<LexemeTag>.CreateOrGet("NativeNumber")),
            priority: -20f
        );

        // Register arithmetic tokens that map to native operation nodes.
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\+", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeAddition")), priority: -10f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\-", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeSubtraction")), priority: -10f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\*", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeMultiplication")), priority: -10f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\/", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeDivision")), priority: -10f);
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-40f, new NativeUnaryMinusOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-31, new NativeMultiplicationOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-31, new NativeDivisionOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-30, new NativeAdditionOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-30, new NativeSubtractionOperationNodeCreator());
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new NativeNumberAstVisitor());
        translator.Configuration.Visitors.Add(new NativeUnaryMinusAstVisitor());
        translator.Configuration.Visitors.Add(new NativeArithmeticAstVisitor());
    }

    public static object ParseNumber(string text)
    {
        var cleanedText = text.Replace("_", "");
        var suffix = char.ToLower(cleanedText[^1]);

        // Resolve numeric type from suffix.
        if (suffix == 'm')
            return decimal.Parse(cleanedText[..^1], NumberStyles.Any);
        if (suffix == 'f')
            return float.Parse(cleanedText[..^1], NumberStyles.Any);
        if (suffix == 'd')
            return double.Parse(cleanedText[..^1], NumberStyles.Any);
        if (suffix == 'l')
            return long.Parse(cleanedText[..^1], NumberStyles.Any);

        // Without suffix, infer from literal form.
        if (cleanedText.Contains('.') || cleanedText.Contains('e') || cleanedText.Contains('E'))
            return double.Parse(cleanedText, NumberStyles.Any);

        return int.Parse(cleanedText);
    }
}
