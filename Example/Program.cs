// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

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
var configuration = new LexerConfiguration(patters, lexemesToIgnore);
var lexer = new BasicLexerImpl(configuration);

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
var root = new BasicParserImpl(new ParserConfiguration(creators)).Parse(lexemes);

Console.WriteLine(string.Join("\n", lexemes.Select(x => x.ToString())));
Console.WriteLine(root);