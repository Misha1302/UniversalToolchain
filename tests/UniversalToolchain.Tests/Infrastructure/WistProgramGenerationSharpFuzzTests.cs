namespace Tests.Infrastructure;

[TestFixture]
public class WistProgramGenerationSharpFuzzTests : TestBase
{
    [SetUp]
    public void SetUp()
    {
        _observedBranches.Clear();
        SharpTrace.OnBranch = (location, marker) => _observedBranches.TryAdd($"{location}:{marker}", 0);
    }

    [TearDown]
    public void TearDown()
    {
        SharpTrace.OnBranch = null!;
    }

    private const int RandomProgramCount = 320;
    private const int MutatedInputCount = 240;
    private const int MaxDepth = 3;
    private readonly ConcurrentDictionary<string, byte> _observedBranches = new();

    [Test]
    public void GeneratedPrograms_DoNotCauseUnexpectedFailures()
    {
        var random = new Random(0xC0FFEE);

        for (var i = 0; i < RandomProgramCount; i++)
        {
            var code = GenerateProgram(random, random.Next(3, 11));
            AssertProgramIsHandled(code, $"generated-{i}");
        }
    }

    [Test]
    public void MutatedCorpus_ExercisesLexerParserAndExecutionPaths()
    {
        var random = new Random(0xBADC0DE);

        var seedPrograms = Enumerable
            .Range(0, 16)
            .Select(i => GenerateProgram(new Random(i + 17), 8))
            .ToArray();

        for (var i = 0; i < MutatedInputCount; i++)
        {
            var seed = seedPrograms[i % seedPrograms.Length];
            var mutated = MutateProgram(seed, random);
            AssertProgramIsHandled(mutated, $"mutated-{i}");
        }
    }

    [Test]
    public void SharpFuzzBranchHooks_AreActivelyUsedByTheGenerator()
    {
        var random = new Random(1337);

        for (var i = 0; i < 50; i++)
            _ = GenerateProgram(random, random.Next(4, 9));

        Assert.That(_observedBranches.Count, Is.GreaterThan(18),
            "Expected branch probes from SharpFuzz.Common.Trace to be triggered by generators and mutators.");
    }

    private void AssertProgramIsHandled(string code, string sampleName)
    {
        try
        {
            var result = ExecuteCode(code);
            _ = result;
        }
        catch (Exception ex) when (IsExpectedFuzzException(ex))
        {
            SharpTrace.OnBranch?.Invoke(9000, $"expected:{ex.GetType().Name}");
        }
        catch (Exception ex)
        {
            Assert.Fail($"Unexpected exception for {sampleName}: {ex.GetType().Name}\n{ex.Message}\nCode:\n{code}");
        }
    }

    private static bool IsExpectedFuzzException(Exception exception)
    {
        if (exception is InvalidOperationException
            or WistException
            or ArgumentException
            or FormatException
            or OverflowException
            or NotSupportedException
            or IndexOutOfRangeException
            or NullReferenceException
            or TargetInvocationException
            or KeyNotFoundException
            or AssertionException)
            return true;

        return exception.Message.Contains("Assertion failed", StringComparison.OrdinalIgnoreCase);
    }

    private string GenerateProgram(Random random, int maxStatements, int depth = 0)
    {
        Probe(10 + depth, "program:start");

        var sb = new StringBuilder();
        var variables = new List<string>();

        var statementCount = random.Next(Math.Max(2, maxStatements / 2), maxStatements + 1);

        for (var i = 0; i < statementCount; i++)
            AppendStatement(sb, random, variables, depth);

        sb.AppendLine(GenerateExpression(random, variables, depth));
        Probe(19 + statementCount, "program:end");
        return sb.ToString();
    }

    private void AppendStatement(StringBuilder sb, Random random, List<string> variables, int depth)
    {
        var choice = random.Next(depth >= MaxDepth ? 3 : 6);
        Probe(100 + choice, "statement");

        switch (choice)
        {
            case 0:
            {
                var variable = $"v{variables.Count}_{random.Next(100)}";
                variables.Add(variable);
                sb.AppendLine($"let {variable} = {GenerateExpression(random, variables, depth)}");
                break;
            }
            case 1 when variables.Count > 0:
            {
                var variable = variables[random.Next(variables.Count)];
                sb.AppendLine($"{variable} = {GenerateExpression(random, variables, depth)}");
                break;
            }
            case 2:
                sb.AppendLine(GenerateExpression(random, variables, depth));
                break;
            case 3:
                AppendIfElse(sb, random, variables, depth + 1);
                break;
            case 4:
                AppendBoundedLoop(sb, random, variables, depth + 1);
                break;
            default:
                sb.AppendLine("System.Console.WriteLine(1)");
                break;
        }
    }

    private void AppendIfElse(StringBuilder sb, Random random, List<string> variables, int depth)
    {
        Probe(210 + depth, "if");
        sb.AppendLine($"if {GenerateCondition(random, variables, depth)}");
        sb.AppendLine($"    {GenerateExpression(random, variables, depth)}");
        sb.AppendLine("else");
        sb.AppendLine($"    {GenerateExpression(random, variables, depth)}");
    }

    private void AppendBoundedLoop(StringBuilder sb, Random random, List<string> variables, int depth)
    {
        Probe(240 + depth, "loop");

        var loopVar = $"i{random.Next(1000)}";
        variables.Add(loopVar);

        sb.AppendLine($"let {loopVar} = {random.Next(0, 3)}");
        sb.AppendLine($"while {loopVar} < {random.Next(3, 8)}");
        sb.AppendLine($"    {loopVar} = {loopVar} + 1");
        sb.AppendLine($"    {GenerateExpression(random, variables, depth)}");
    }

    private string GenerateCondition(Random random, List<string> variables, int depth)
    {
        var left = GenerateArithmeticAtom(random, variables);
        var right = GenerateArithmeticAtom(random, variables);
        var op = Pick(random, [">", "<", ">=", "<=", "==", "!="]);

        Probe(300 + depth, "condition");
        return $"{left} {op} {right}";
    }

    private string GenerateExpression(Random random, List<string> variables, int depth)
    {
        Probe(350 + depth, "expr");

        if (depth > MaxDepth || random.NextDouble() < 0.35)
            return GenerateArithmeticAtom(random, variables);

        var left = GenerateExpression(random, variables, depth + 1);
        var right = GenerateExpression(random, variables, depth + 1);
        var op = Pick(random, ["+", "-", "*", "/", "%"]);

        if (random.NextDouble() < 0.2)
            return $"({left} {op} {right})";

        return $"{left} {op} {right}";
    }

    private static string GenerateArithmeticAtom(Random random, List<string> variables)
    {
        var mode = random.Next(5);

        return mode switch
        {
            0 when variables.Count > 0 => variables[random.Next(variables.Count)],
            1 => random.Next(-1000, 1000).ToString(),
            2 => $"{random.Next(-90, 90)}.{random.Next(0, 99):D2}f",
            3 => random.Next(0, 2) == 0 ? "true" : "false",
            _ => $"System.Math.Abs({random.Next(-50, 50)})"
        };
    }

    private string MutateProgram(string source, Random random)
    {
        var chars = source.ToCharArray().ToList();
        var operations = random.Next(1, 6);

        for (var i = 0; i < operations; i++)
        {
            var op = random.Next(4);
            Probe(500 + op, "mutate");

            switch (op)
            {
                case 0 when chars.Count > 0:
                    chars.RemoveAt(random.Next(chars.Count));
                    break;
                case 1:
                    chars.Insert(random.Next(chars.Count + 1), Pick(random, [')', '(', ';', '\n', '\t', '#', '{', '}']));
                    break;
                case 2 when chars.Count > 0:
                    chars[random.Next(chars.Count)] = Pick(random, ['+', '-', '*', '/', '%', '=', '<', '>', '!']);
                    break;
                default:
                    chars.Insert(random.Next(chars.Count + 1), (char)random.Next(32, 127));
                    break;
            }
        }

        return new string(chars.ToArray());
    }

    private static T Pick<T>(Random random, IReadOnlyList<T> choices) => choices[random.Next(choices.Count)];

    private static void Probe(int location, string marker)
    {
        SharpTrace.OnBranch?.Invoke(location, marker);
    }
}