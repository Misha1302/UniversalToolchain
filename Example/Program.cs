using BasicLexer;

var patters = (List<LexemePattern>)
[
    new LexemePattern(@"\n", LexemeType.CreateNewUnique("NewLine")),
    new LexemePattern(@"\d+", LexemeType.CreateNewUnique("Number"))
];
var lexemesToIgnore = (List<LexemeType>)[LexemeType.Get("NewLine")];
var configuration = new LexerConfiguration(patters, lexemesToIgnore);
var lexer = new DefaultLexer(configuration);

const string code =
    """
    6 9 
    42
    """;

var lexemes = lexer.Lexemize(code);

Console.WriteLine(string.Join("\n", lexemes.Select(x => x.ToString())));