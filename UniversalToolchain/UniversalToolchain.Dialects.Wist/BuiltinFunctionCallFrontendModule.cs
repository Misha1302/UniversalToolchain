using BasicCore.Contracts;
using BasicCore.LexerWrapper;
using BasicCore.ParserWrapper;
using BasicCore.Registration;
using BasicCore.TranslatorWrapper;
using CommonExceptions;
using DynamicMethodWrapper;
using UniversalToolchain.Dialects.Wist.Functions;

namespace UniversalToolchain.Dialects.Wist;

internal sealed class BuiltinFunctionCallFrontendModule : IFrontendCoreModule
{
    private static readonly IReadOnlyList<LexemeRegistration> _lexemeRegistrations =
    [
        new(@",", "Comma")
    ];

    private static readonly IReadOnlyList<NodeCreatorRegistration> _nodeCreatorRegistrations =
    [
        new(-1_100f, new BuiltinFunctionCallNodeCreator())
    ];

    private readonly WistBuiltinFunctionBindingResolver _bindingResolver;

    public BuiltinFunctionCallFrontendModule(WistBuiltinFunctionBindingResolver bindingResolver)
    {
        _bindingResolver = bindingResolver;
    }

    public void InitLexer(ILexer lexer) => lexer.AddLexemes(_lexemeRegistrations);

    public void InitParser(IParser parser) => parser.AddNodeCreators(_nodeCreatorRegistrations);

    public void InitAstTranslator(IAstToBytecodeTranslator translator) => translator.AddVisitors(new BuiltinFunctionCallAstVisitor(_bindingResolver));

    private sealed class BuiltinFunctionCallNodeCreator : IAstNodeCreator
    {
        private static readonly HashSet<string> _supportedNames =
        [
            "abs",
            "clamp",
            "max",
            "min"
        ];

        public AstNodeType AstNodeType { get; } = AstNodeType.CreateOrGet("BuiltinFunctionCall");

        public bool TryCreateNode(AstNode scope, int childIndex)
        {
            var identifier = scope.SafeGet(childIndex);
            var arguments = scope.SafeGet(childIndex + 1);

            if (identifier?.NodeType != AstNodeType.CreateOrGet("Identifier") ||
                arguments?.NodeType != AstNodeType.CreateOrGet("Scope"))
            {
                return false;
            }

            if (!_supportedNames.Contains(identifier.Text) || identifier.Text.Contains('.', StringComparison.Ordinal))
            {
                return false;
            }

            identifier.NodeType = AstNodeType;
            identifier.Children.Add(arguments);
            scope.Children.RemoveAt(childIndex + 1);
            return true;
        }
    }

    private sealed class BuiltinFunctionCallAstVisitor : IAstVisitor
    {
        private readonly WistBuiltinFunctionBindingResolver _bindingResolver;

        public BuiltinFunctionCallAstVisitor(WistBuiltinFunctionBindingResolver bindingResolver)
        {
            _bindingResolver = bindingResolver;
        }

        public void TryVisit(BytecodeVisitorData data)
        {
            if (data.Node.NodeType != ExtensibleEnum<AstNodeTag>.CreateOrGet("BuiltinFunctionCall"))
            {
                return;
            }

            var functionName = data.Node.Text;
            var arguments = GetArguments(data.Node);

            foreach (var argument in arguments)
            {
                data.AstToBytecodeTranslator.Translate(argument);
            }

            var method = new AbstractMethodImpl(
                $"BuiltinFunction_{functionName}",
                (il, context) =>
                {
                    var argumentTypes = context.Stack.TakeLast(arguments.Count).ToArray();
                    il.CallCSharp(_bindingResolver.Resolve(functionName, argumentTypes));
                });

            data.Bytecode.Instructions.Add(new BytecodeInstruction(method));
        }

        private static IReadOnlyList<AstNode> GetArguments(AstNode node)
        {
            if (node.Children.Count != 1 || node.Children[0].NodeType != AstNodeType.CreateOrGet("Scope"))
            {
                WistThrower.Parser("Builtin function call must contain one argument scope.");
            }

            var scope = node.Children[0];
            if (scope.Children.Count == 0)
            {
                return [];
            }

            var arguments = new List<AstNode>();
            AstNode? pendingArgument = null;

            foreach (var child in scope.Children)
            {
                if (child.NodeType == AstNodeType.CreateOrGet("Comma"))
                {
                    if (pendingArgument == null)
                    {
                        WistThrower.Parser($"Function '{node.Text}' contains an empty argument.");
                    }

                    arguments.Add(pendingArgument);
                    pendingArgument = null;
                    continue;
                }

                if (pendingArgument != null)
                {
                    WistThrower.Parser($"Function '{node.Text}' argument syntax is invalid.");
                }

                pendingArgument = child;
            }

            if (pendingArgument == null)
            {
                WistThrower.Parser($"Function '{node.Text}' contains an empty trailing argument.");
            }

            arguments.Add(pendingArgument);
            return arguments;
        }
    }
}
