using CommonExceptions;
using UniversalToolchain.Dialects.Abstractions;

namespace CommentsModule;

[DialectModuleAlias("Comments")]
[DialectRuntimeExport("FrontendModule", "Comments")]
[AutoRegisterService]
public class CommentsModuleImpl : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@"//[^\n]*", "SingleLineComment", true, -100f),
        new(@"/\*[\s\S]*?\*/", "MultiLineComment", true, -100f)
    ];

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public string ProcessText(string curCode)
    {
        ValidateNoUnterminatedBlockComment(curCode);
        return curCode;
    }

    public void InitAstTranslator(IAstToBytecodeTranslator translator)
    {
    }

    private static void ValidateNoUnterminatedBlockComment(string code)
    {
        var state = CommentScanState.Normal;
        var blockCommentStartIndex = -1;

        for (var index = 0; index < code.Length; index++)
        {
            var currentChar = code[index];
            var nextChar = index + 1 < code.Length ? code[index + 1] : '\0';

            switch (state)
            {
                case CommentScanState.Normal:
                    if (currentChar == '/' && nextChar == '/')
                    {
                        state = CommentScanState.SingleLineComment;
                        index++;
                    }
                    else if (currentChar == '/' && nextChar == '*')
                    {
                        state = CommentScanState.BlockComment;
                        blockCommentStartIndex = index;
                        index++;
                    }
                    break;

                case CommentScanState.SingleLineComment:
                    if (currentChar is '\n' or '\r')
                        state = CommentScanState.Normal;
                    break;

                case CommentScanState.BlockComment:
                    if (currentChar == '*' && nextChar == '/')
                    {
                        state = CommentScanState.Normal;
                        blockCommentStartIndex = -1;
                        index++;
                    }
                    break;
            }
        }

        if (state == CommentScanState.BlockComment)
        {
            var location = new LexemeValue("/*", null, blockCommentStartIndex, code);
            WistThrower.Lexer(
                "Unterminated block comment (comment).",
                new SourceLocation { Line = location.LineNumber, Column = location.CharNumber }
            );
        }
    }

    private enum CommentScanState
    {
        Normal,
        SingleLineComment,
        BlockComment
    }
}