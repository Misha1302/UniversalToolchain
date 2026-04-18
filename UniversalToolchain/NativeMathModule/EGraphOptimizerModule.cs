using BasicCore.Builtins;
using BasicCore.Capabilities;
using UniversalToolchain.Dialects.Integration;

namespace NativeMathModule;

[DialectOptimizerAlias("EGraphOptimization")]
[DialectRuntimeExport("Optimizer", "EGraphOptimization")]
[AutoRegisterService]
[IntrinsicDescriptorProvider(typeof(ArithmeticIntrinsicDescriptorProvider))]
[ArithmeticModeCompatibility(ArithmeticMode.Native)]
public class EGraphOptimizerModule : IIRProcessingModule
{
    private static readonly IReadOnlyList<Type> _supportedArithmeticTypes =
    [
        typeof(int), typeof(long), typeof(float), typeof(double)
    ];

    private IOptimizerIntrinsicCapabilityContext? _capabilityContext;

    public void InitIntrinsicCapabilityContext(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        capabilityContext = capabilityContext.ArgNotNull();

        _capabilityContext = capabilityContext;
    }

    public IAbstractIR ProcessIr<TCompilationOutput>(IAbstractIR current, IAbstractIrCompiler<TCompilationOutput> compiler)
    {
        if (_capabilityContext == null)
            Thrower.InvalidOpEx("E-graph optimizer requires intrinsic capability context initialization.");

        var capabilityContext = _capabilityContext;

        if (!HasRequiredCapabilities(capabilityContext))
            return current;

        var source = current.Instructions.ToList();
        var optimized = new List<Instruction>();

        var blockStart = 0;
        for (var i = 0; i < source.Count; i++)
        {
            if (source[i].UOpCode == UOpCode.Label)
            {
                AppendOptimizedStraightLineBlock(source, blockStart, i, optimized);
                optimized.Add(source[i]);
                blockStart = i + 1;
                continue;
            }

            if (IsControlFlowTerminator(source[i]))
            {
                AppendOptimizedStraightLineBlock(source, blockStart, i, optimized);
                optimized.Add(source[i]);
                blockStart = i + 1;
            }
        }

        AppendOptimizedStraightLineBlock(source, blockStart, source.Count, optimized);

        var result = new AbstractIR();
        result.AppendInstructions(optimized);
        return result;
    }

    private static bool HasRequiredCapabilities(IOptimizerIntrinsicCapabilityContext capabilityContext)
    {
        var requirements = _supportedArithmeticTypes.SelectMany(type => new (IntrinsicSymbol Symbol, Type[] TypeArguments)[]
        {
            (BuiltinIntrinsicSymbols.Arithmetic.Add, [type]),
            (BuiltinIntrinsicSymbols.Arithmetic.Subtract, [type]),
            (BuiltinIntrinsicSymbols.Arithmetic.Multiply, [type]),
            (BuiltinIntrinsicSymbols.Arithmetic.Divide, [type]),
            (BuiltinIntrinsicSymbols.Storage.LoadLocal, [type])
        });

        return OptimizerCapabilityGuards.SupportsAll(capabilityContext, requirements);
    }

    private static bool IsControlFlowTerminator(Instruction instruction) =>
        instruction.UOpCode is UOpCode.Jmp or UOpCode.JmpIf or UOpCode.JmpIfNot;

    private static void AppendOptimizedStraightLineBlock(
        IReadOnlyList<Instruction> source,
        int start,
        int endExclusive,
        ICollection<Instruction> target)
    {
        if (start >= endExclusive)
            return;

        var block = source.Skip(start).Take(endExclusive - start).ToList();
        var optimized = TryOptimizeBlock(block);
        foreach (var instruction in optimized)
            target.Add(instruction);
    }

    private static IReadOnlyList<Instruction> TryOptimizeBlock(IReadOnlyList<Instruction> block)
    {
        var expressionStack = new Stack<Expr>();

        foreach (var instruction in block)
        {
            switch (instruction.UOpCode)
            {
                case UOpCode.Push:
                {
                    if (instruction.Operands.Count != 1 || !TryCreateConstExpr(instruction.Operands[0], out var constExpr))
                        return block;
                    expressionStack.Push(constExpr);
                    break;
                }
                case UOpCode.Intrinsic:
                {
                    if (!TryProcessIntrinsic(instruction, expressionStack))
                        return block;
                    break;
                }
                case UOpCode.Drop:
                {
                    if (expressionStack.Count == 0)
                        return block;
                    expressionStack.Pop();
                    break;
                }
                default:
                    return block;
            }
        }

        var rewrittenStack = expressionStack.Reverse().Select(ExtractBest).ToList();
        var rebuilt = new List<Instruction>();
        foreach (var expr in rewrittenStack)
            EmitExpression(expr, rebuilt);

        var originalCost = block.Count;
        var newCost = rewrittenStack.Sum(GetCost);
        return newCost <= originalCost ? rebuilt : block;
    }

    private static bool TryProcessIntrinsic(Instruction instruction, Stack<Expr> stack)
    {
        if (!BuiltinIntrinsicInstruction.TryGetInvocation(instruction, out var invocation))
            return false;

        if (invocation.Symbol == BuiltinIntrinsicSymbols.Storage.LoadLocal)
        {
            if (invocation.DataOperands.Count < 2 ||
                invocation.DataOperands[0] is not string name ||
                invocation.DataOperands[1] is not Type type ||
                !IsSupportedType(type))
                return false;

            stack.Push(Expr.Local(name, type));
            return true;
        }

        if (!TryGetBinaryOp(invocation, out var operation, out var resultType))
            return false;

        if (stack.Count < 2)
            return false;

        var right = stack.Pop();
        var left = stack.Pop();

        if (left.Type != resultType || right.Type != resultType)
            return false;

        stack.Push(Simplify(new Expr(operation, resultType, left, right, null, null)));
        return true;
    }

    private static bool TryCreateConstExpr(object value, out Expr expression)
    {
        var type = value.GetType();
        if (!IsSupportedType(type))
        {
            expression = null!;
            return false;
        }

        expression = Expr.Const(value, type);
        return true;
    }

    private static bool IsSupportedType(Type type) =>
        type == typeof(int) || type == typeof(long) || type == typeof(float) || type == typeof(double);

    private static bool TryGetBinaryOp(IntrinsicInvocation invocation, out ExprOp operation, [NotNullWhen(true)] out Type? type)
    {
        ExprOp? parsedOperation = invocation.Symbol.Name switch
        {
            "Add" => ExprOp.Add,
            "Subtract" => ExprOp.Sub,
            "Multiply" => ExprOp.Mul,
            "Divide" => ExprOp.Div,
            _ => null
        };

        type = invocation.TypeArguments.Count == 1 ? invocation.TypeArguments[0].RuntimeType : null;

        if (invocation.Symbol.Namespace != BuiltinIntrinsicSymbols.Arithmetic.Add.Namespace ||
            parsedOperation is null ||
            type is null ||
            !IsSupportedType(type))
        {
            operation = ExprOp.Const;
            return false;
        }

        operation = parsedOperation.Value;
        return true;
    }

    private static Expr ExtractBest(Expr expr)
    {
        var simplified = Simplify(expr);
        return GetCost(simplified) <= GetCost(expr) ? simplified : expr;
    }

    private static Expr Simplify(Expr expr)
    {
        if (expr.Operation is ExprOp.Const or ExprOp.Local)
            return expr;

        var left = Simplify(expr.Left!);
        var right = Simplify(expr.Right!);

        if (TryFoldConstants(expr.Operation, left, right, expr.Type, out var folded))
            return folded;

        return expr.Operation switch
        {
            ExprOp.Add => SimplifyAdd(left, right, expr.Type),
            ExprOp.Sub => SimplifySub(left, right, expr.Type),
            ExprOp.Mul => SimplifyMul(left, right, expr.Type),
            ExprOp.Div => SimplifyDiv(left, right, expr.Type),
            _ => expr
        };
    }

    private static Expr SimplifyAdd(Expr left, Expr right, Type type)
    {
        if (IsIntegral(type))
        {
            if (IsZero(left)) return right;
            if (IsZero(right)) return left;
        }

        if (!IsIntegral(type))
            return new Expr(ExprOp.Add, type, left, right, null, null);

        var terms = new List<Expr>();
        CollectTerms(ExprOp.Add, left, type, terms);
        CollectTerms(ExprOp.Add, right, type, terms);

        var constValue = ZeroOf(type);
        var nonConst = new List<Expr>();
        foreach (var term in terms)
        {
            if (term.Operation == ExprOp.Const)
                constValue = FoldBinaryRaw(ExprOp.Add, constValue, term.Value!, type);
            else
                nonConst.Add(term);
        }

        if (!IsZeroValue(constValue))
            nonConst.Add(Expr.Const(constValue, type));

        return BuildCanonical(ExprOp.Add, nonConst, type, ZeroOf(type));
    }

    private static Expr SimplifyMul(Expr left, Expr right, Type type)
    {
        if (IsIntegral(type))
        {
            if (IsZero(left) || IsZero(right))
                return Expr.Const(ZeroOf(type), type);
            if (IsOne(left)) return right;
            if (IsOne(right)) return left;
        }

        if (!IsIntegral(type))
            return new Expr(ExprOp.Mul, type, left, right, null, null);

        var terms = new List<Expr>();
        CollectTerms(ExprOp.Mul, left, type, terms);
        CollectTerms(ExprOp.Mul, right, type, terms);

        var constValue = OneOf(type);
        var nonConst = new List<Expr>();
        foreach (var term in terms)
        {
            if (term.Operation == ExprOp.Const)
                constValue = FoldBinaryRaw(ExprOp.Mul, constValue, term.Value!, type);
            else
                nonConst.Add(term);
        }

        if (IsZeroValue(constValue))
            return Expr.Const(ZeroOf(type), type);

        if (!IsOneValue(constValue))
            nonConst.Add(Expr.Const(constValue, type));

        return BuildCanonical(ExprOp.Mul, nonConst, type, OneOf(type));
    }

    private static Expr SimplifySub(Expr left, Expr right, Type type)
    {
        if (IsZero(right))
            return left;

        return new Expr(ExprOp.Sub, type, left, right, null, null);
    }

    private static Expr SimplifyDiv(Expr left, Expr right, Type type)
    {
        if (IsOne(right))
            return left;

        return new Expr(ExprOp.Div, type, left, right, null, null);
    }

    private static Expr BuildCanonical(ExprOp operation, List<Expr> operands, Type type, object identity)
    {
        if (operands.Count == 0)
            return Expr.Const(identity, type);

        var ordered = operands
            .OrderBy(GetOrderingKey)
            .ToList();

        var current = ordered[0];
        for (var i = 1; i < ordered.Count; i++)
            current = new Expr(operation, type, current, ordered[i], null, null);

        return current;
    }

    private static string GetOrderingKey(Expr expr) => expr.Operation switch
    {
        ExprOp.Const => $"0:{expr.Value}",
        ExprOp.Local => $"1:{expr.LocalName}",
        _ => $"2:{expr.Operation}:{GetOrderingKey(expr.Left!)}:{GetOrderingKey(expr.Right!)}"
    };

    private static void CollectTerms(ExprOp operation, Expr expr, Type type, ICollection<Expr> destination)
    {
        if (expr.Operation == operation && expr.Type == type)
        {
            CollectTerms(operation, expr.Left!, type, destination);
            CollectTerms(operation, expr.Right!, type, destination);
            return;
        }

        destination.Add(expr);
    }

    private static bool TryFoldConstants(ExprOp operation, Expr left, Expr right, Type type, out Expr folded)
    {
        folded = null!;
        if (left.Operation != ExprOp.Const || right.Operation != ExprOp.Const)
            return false;

        if (operation == ExprOp.Div && IsZero(right))
            return false;

        var value = FoldBinaryRaw(operation, left.Value!, right.Value!, type);
        folded = Expr.Const(value, type);
        return true;
    }

    private static object FoldBinaryRaw(ExprOp operation, object left, object right, Type type)
    {
        if (type == typeof(int))
        {
            var l = (int)left;
            var r = (int)right;
            return operation switch
            {
                ExprOp.Add => unchecked (l + r),
                ExprOp.Sub => unchecked (l - r),
                ExprOp.Mul => unchecked (l * r),
                ExprOp.Div => l / r,
                _ => left
            };
        }

        if (type == typeof(long))
        {
            var l = (long)left;
            var r = (long)right;
            return operation switch
            {
                ExprOp.Add => unchecked (l + r),
                ExprOp.Sub => unchecked (l - r),
                ExprOp.Mul => unchecked (l * r),
                ExprOp.Div => l / r,
                _ => left
            };
        }

        if (type == typeof(float))
        {
            var l = (float)left;
            var r = (float)right;
            return operation switch
            {
                ExprOp.Add => l + r,
                ExprOp.Sub => l - r,
                ExprOp.Mul => l * r,
                ExprOp.Div => l / r,
                _ => left
            };
        }

        var ld = (double)left;
        var rd = (double)right;
        return operation switch
        {
            ExprOp.Add => ld + rd,
            ExprOp.Sub => ld - rd,
            ExprOp.Mul => ld * rd,
            ExprOp.Div => ld / rd,
            _ => left
        };
    }

    private static int GetCost(Expr expr) => expr.Operation switch
    {
        ExprOp.Const => 1,
        ExprOp.Local => 1,
        _ => 2 + GetCost(expr.Left!) + GetCost(expr.Right!)
    };


    private static object ZeroOf(Type type)
    {
        if (type == typeof(int))
            return 0;
        return 0L;
    }

    private static object OneOf(Type type)
    {
        if (type == typeof(int))
            return 1;
        return 1L;
    }

    private static bool IsIntegral(Type type) => type == typeof(int) || type == typeof(long);

    private static bool IsZero(Expr expr) => expr.Operation == ExprOp.Const && IsZeroValue(expr.Value!);

    private static bool IsOne(Expr expr) => expr.Operation == ExprOp.Const && IsOneValue(expr.Value!);

    private static bool IsZeroValue(object value) => value switch
    {
        int v => v == 0,
        long v => v == 0L,
        float v => v == 0f,
        double v => v == 0d,
        _ => false
    };

    private static bool IsOneValue(object value) => value switch
    {
        int v => v == 1,
        long v => v == 1L,
        float v => Math.Abs(v - 1f) < 1e-9,
        double v => Math.Abs(v - 1d) < 1e-9,
        _ => false
    };

    private static void EmitExpression(Expr expr, ICollection<Instruction> output)
    {
        switch (expr.Operation)
        {
            case ExprOp.Const:
                output.Add(new Instruction(UOpCode.Push, [expr.Value!]));
                return;
            case ExprOp.Local:
                output.Add(BuiltinIntrinsicInstruction.Create(
                    BuiltinIntrinsicSymbols.Storage.LoadLocal,
                    IntrinsicTypeArgument.From(expr.Type),
                    expr.LocalName!,
                    expr.Type));
                return;
            case ExprOp.Add:
            case ExprOp.Sub:
            case ExprOp.Mul:
            case ExprOp.Div:
                EmitExpression(expr.Left!, output);
                EmitExpression(expr.Right!, output);
                output.Add(BuiltinIntrinsicInstruction.Create(GetIntrinsicSymbol(expr.Operation), expr.Type));
                return;
        }
    }

    private static IntrinsicSymbol GetIntrinsicSymbol(ExprOp operation)
    {
        return operation switch
        {
            ExprOp.Add => BuiltinIntrinsicSymbols.Arithmetic.Add,
            ExprOp.Sub => BuiltinIntrinsicSymbols.Arithmetic.Subtract,
            ExprOp.Mul => BuiltinIntrinsicSymbols.Arithmetic.Multiply,
            ExprOp.Div => BuiltinIntrinsicSymbols.Arithmetic.Divide,
            _ => Thrower.InvalidOpEx<IntrinsicSymbol>("Unsupported expression operation")
        };
    }

    private enum ExprOp
    {
        Const,
        Local,
        Add,
        Sub,
        Mul,
        Div
    }

    private sealed record Expr(ExprOp Operation, Type Type, Expr? Left, Expr? Right, object? Value, string? LocalName)
    {
        public static Expr Const(object value, Type type) => new(ExprOp.Const, type, null, null, value, null);
        public static Expr Local(string name, Type type) => new(ExprOp.Local, type, null, null, null, name);
    }
}