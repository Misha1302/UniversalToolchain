// This is an independent project of an individual developer. Dear PVS-Studio, please check it.
// PVS-Studio Static Code Analyzer for C, C++, C#, and Java: https://pvs-studio.com

using BasicLexer;
using BasicTypesExtensions;

var patters = (List<LexemePattern>)
[
    new LexemePattern(" ", ExtensibleEnum<LexemeTag>.CreateNewUnique("Space")),
    new LexemePattern(@"\n", ExtensibleEnum<LexemeTag>.CreateNewUnique("NewLine")),
    new LexemePattern(@"\d+", ExtensibleEnum<LexemeTag>.CreateNewUnique("Number"))
];
var lexemesToIgnore = (List<ExtensibleEnum<LexemeTag>>)
    [ExtensibleEnum<LexemeTag>.Get("NewLine"), ExtensibleEnum<LexemeTag>.Get("Space")];
var configuration = new LexerConfiguration(patters, lexemesToIgnore);
var lexer = new DefaultLexer(configuration);

const string code =
    """
    6 9  
    42 5 777
    """;

var lexemes = lexer.Lexemize(code);

Console.WriteLine(string.Join("\n", lexemes.Select(x => x.ToString())));