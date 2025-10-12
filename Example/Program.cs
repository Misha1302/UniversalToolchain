// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicLexer;
using BasicParser;
using BasicTypesExtensions;
using StandardParserNodeCreators;

var patters = (List<LexemePattern>)
[
    new LexemePattern(" ", ExtensibleEnum<LexemeTag>.CreateOrGet("Space")),
    new LexemePattern(@"\n", ExtensibleEnum<LexemeTag>.CreateOrGet("NewLine")),
    new LexemePattern(@"\(", ExtensibleEnum<LexemeTag>.CreateOrGet("OpenPar")),
    new LexemePattern(@"\)", ExtensibleEnum<LexemeTag>.CreateOrGet("ClosePar")),
    new LexemePattern(@"\d+", ExtensibleEnum<LexemeTag>.CreateOrGet("Number"))
];
var lexemesToIgnore = (List<ExtensibleEnum<LexemeTag>>)
    [ExtensibleEnum<LexemeTag>.Get("NewLine"), ExtensibleEnum<LexemeTag>.Get("Space")];
var configuration = new LexerConfiguration(patters, lexemesToIgnore);
var lexer = new DefaultLexer(configuration);

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
var root = new BasicParser.BasicParser(new ParserConfiguration(creators)).Parse(lexemes);

Console.WriteLine(string.Join("\n", lexemes.Select(x => x.ToString())));
Console.WriteLine(root);