using System.Globalization;
using BasicCore;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.TranslatorWrapper;
using BasicTypesExtensions;

namespace NativeMathModule;

[AutoRegisterService]
public class NativeTypesModuleImpl : IFrontendCoreModule
{
    private static readonly HashSet<string> _nativeTypeKeywords = new()
    {
        "int", "long", "float", "double", "decimal"
    };

    private static readonly Dictionary<string, Type> _typeMap = new()
    {
        ["int"] = typeof(int),
        ["long"] = typeof(long),
        ["float"] = typeof(float),
        ["double"] = typeof(double),
        ["decimal"] = typeof(decimal)
    };

    private static readonly Dictionary<string, string> _suffixToType = new()
    {
        ["L"] = "long",
        ["F"] = "float",
        ["D"] = "double",
        ["M"] = "decimal"
    };

    public void InitLexer(ILexer lexer)
    {
        // Ключевые слова для явного приведения типов
        foreach (var type in _nativeTypeKeywords)
        {
            lexer.Configuration.TryAddPattern(
                new LexemePattern(
                    $@"{type}",
                    ExtensibleEnum<LexemeTag>.CreateOrGet($"NativeType_{type}")
                )
            );
        }


        lexer.Configuration.TryAddPattern(
            new LexemePattern(
                @"[+-]?\d+(?:_?\d+)*(?:\.\d+(?:_?\d+)*)?[eE][+-]?\d+(?:_?\d+)*[FDMfdm]?",
                ExtensibleEnum<LexemeTag>.CreateOrGet("NativeScientific")
            )
        );

        lexer.Configuration.TryAddPattern(
            new LexemePattern(
                @"[+-]?\d+(?:_?\d+)*\.\d+(?:_?\d+)*[FDMfdm]?",
                ExtensibleEnum<LexemeTag>.CreateOrGet("NativeFloat")
            )
        );


        lexer.Configuration.TryAddPattern(
            new LexemePattern(
                @"[+-]?\d+(?:_?\d+)*[LFDlfd]?",
                ExtensibleEnum<LexemeTag>.CreateOrGet("NativeInteger")
            )
        );

        // Арифметические операции (используем существующие, но переопределяем поведение)
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\+", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeAddition")), priority: -10f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\-", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeSubtraction")), priority: -10f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\*", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeMultiplication")), priority: -10f);
        lexer.Configuration.TryAddPattern(new LexemePattern(@"\/", ExtensibleEnum<LexemeTag>.CreateOrGet("NativeDivision")), priority: -10f);
    }

    public void InitParser(IParser parser)
    {
        // Сначала обрабатываем явные приведения типов
        parser.Configuration.NodeCreators.Add(-1000, new TypeCastNodeCreator());

        // Затем операции
        parser.Configuration.NodeCreators.Add(-1, new NativeMultiplicationOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(-1, new NativeDivisionOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(0, new NativeAdditionOperationNodeCreator());
        parser.Configuration.NodeCreators.Add(0, new NativeSubtractionOperationNodeCreator());
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
        translator.Configuration.Visitors.Add(new NativeNumberAstVisitor());
        translator.Configuration.Visitors.Add(new NativeArithmeticAstVisitor());
    }

    public static object ParseNumber(string text)
    {
        var cleanText = text.Replace("_", "");

        // Определяем тип по суффиксу
        if (text.EndsWith('M') || text.EndsWith('m'))
        {
            return decimal.Parse(cleanText.TrimEnd('M', 'm'), CultureInfo.InvariantCulture);
        }
        if (text.EndsWith('F') || text.EndsWith('f'))
        {
            return float.Parse(cleanText.TrimEnd('F', 'f'), CultureInfo.InvariantCulture);
        }
        if (text.EndsWith('D') || text.EndsWith('d'))
        {
            return double.Parse(cleanText.TrimEnd('D', 'd'), CultureInfo.InvariantCulture);
        }
        if (text.EndsWith('L') || text.EndsWith('l'))
        {
            return long.Parse(cleanText.TrimEnd('L', 'l'));
        }

        // Если нет суффикса
        if (cleanText.Contains('.') || cleanText.Contains('e') || cleanText.Contains('E'))
        {
            return double.Parse(cleanText, CultureInfo.InvariantCulture);
        }

        return int.Parse(cleanText);
    }
}