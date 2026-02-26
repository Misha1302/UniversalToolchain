using System.Globalization;
using BasicCore.Attributes;
using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace NativeMathModule;

[AutoRegisterService]
public class NativeTypesModuleImpl : IFrontendCoreModule
{
    public void InitLexer(ILexer lexer)
    {
        lexer.Configuration.TryAddPattern(
            new LexemePattern(@"[+-]?\d+(?:_?\d+)*(?:\.\d+(?:_?\d+)*)?(?:[eE][+-]?\d+(?:_?\d+)*)?[fdmFDM]?",
                ExtensibleEnum<LexemeTag>.CreateOrGet("NativeNumber")),
            priority: -20f
        );

        // Арифметические операции (используем существующие, но переопределяем поведение)
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\+", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeAddition")), priority: -10f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\-", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeSubtraction")), priority: -10f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\*", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeMultiplication")), priority: -10f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\/", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeDivision")), priority: -10f);
    }

    public void InitParser(IParser parser)
    {
        parser.Configuration.NodeCreators.Add(-31, new NativeMultiplicationOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-31, new NativeDivisionOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-30, new NativeAdditionOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-30, new NativeSubtractionOperationNodeCreator());
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new NativeNumberAstVisitor());
        translator.Configuration.Visitors.Add(new NativeArithmeticAstVisitor());
    }

    public static object ParseNumber(string text)
    {
        var cleanedText = text.Replace("_", "");
        var suffix = char.ToLower(cleanedText[^1]);

        // Определяем тип по суффиксу
        if (suffix == 'm')
            return decimal.Parse(cleanedText[..^1], NumberStyles.Any);
        if (suffix == 'f')
            return float.Parse(cleanedText[..^1], NumberStyles.Any);
        if (suffix == 'd')
            return double.Parse(cleanedText[..^1], NumberStyles.Any);
        if (suffix == 'l')
            return long.Parse(cleanedText[..^1], NumberStyles.Any);

        // Если нет суффикса
        if (cleanedText.Contains('.') || cleanedText.Contains('e') || cleanedText.Contains('E'))
            return double.Parse(cleanedText, NumberStyles.Any);

        return int.Parse(cleanedText);
    }
}