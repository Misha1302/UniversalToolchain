// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicCodeTranslator;
using BasicInterpreter;
using BasicLexer;
using BasicParser;
using StandardParserNodeCreators;
using LexemeType = BasicTypesExtensions.ExtensibleEnum<BasicLexer.LexemeTag>;

var patters = (List<LexemePattern>)
[
    new LexemePattern(" ", LexemeType.CreateOrGet("Space")),
    new LexemePattern(@"\n", LexemeType.CreateOrGet("NewLine")),
    new LexemePattern(@"\(", LexemeType.CreateOrGet("OpenPar")),
    new LexemePattern(@"\)", LexemeType.CreateOrGet("ClosePar")),
    new LexemePattern(@"\d+", LexemeType.CreateOrGet("Number"))
];
var lexemesToIgnore = (List<LexemeType>)[LexemeType.Get("NewLine"), LexemeType.Get("Space")];
var lexerConfiguration = new LexerConfiguration(patters, lexemesToIgnore);
var lexer = new BasicLexerImpl(lexerConfiguration);

const string code =
    """
    6 (9  
    (42 (2 6)) 5) (777) (()
    0 9 4)
    """;

var lexemes = lexer.Lexemize(code);
var creators = new SortedDictionary<float, IAstNodeCreator>
{
    { -1000f, new ScopesCreator() }
};
var parserConfiguration = new ParserConfiguration(creators);
var root = new BasicParserImpl(parserConfiguration).Parse(lexemes);

var visitors = (List<IAstVisitor>)[new ScopeAstVisitor(), new NumberAstVisitor()];
var translatorConfiguration = new BytecodeTranslatorConfiguration(visitors);
var bytecode = new BasicBytecodeTranslatorImpl(translatorConfiguration).Translate(root);

var interpreterConfiguration = new InterpreterConfiguration(bytecode);
var ans = new BasicInterpreterImpl(interpreterConfiguration).Interpret();

Console.WriteLine(string.Join("\n", lexemes.Select(x => x.ToString())));
Console.WriteLine(root);
Console.WriteLine(bytecode);
Console.WriteLine(ans);