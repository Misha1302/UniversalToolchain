using System.Text.Json;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;

namespace UniversalToolchain.TensorRules;

internal static class Program
{
    private static readonly BackendId Interpreter = new("tensor-interpreter");
    private static readonly LanguageArtifactKind<TensorSyntax> Syntax = new("tensorrules.syntax");
    private static readonly LanguageArtifactKind<TensorPlan> Plan = new("tensorrules.plan");
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    public static int Main(string[] args)
    {
        var output = Path.GetFullPath(args.Length == 0 ? "artifacts/cgo27-tensorrules" : args[0]);
        Directory.CreateDirectory(output);

        var package = BuildPackage();
        var registry = new LanguagePackageRegistry().AddPackage(package);
        var definition = LanguageDefinitionBuilder.Create("TensorRules", "1.0.0")
            .UseFeature("tensorrules.core")
            .EnableBackend(Interpreter)
            .UseRuntimeProvider("tensorrules.runtime", "1.0.0")
            .WithRuntimePolicy(new LanguageRuntimePolicy(RequireDeterminism: true, MaximumSourceLength: 256))
            .Build();
        var compilation = new LanguageCompiler(registry).Compile(definition);
        if (!compilation.IsSuccess)
        {
            foreach (var diagnostic in compilation.Diagnostics)
                Console.Error.WriteLine($"{diagnostic.Code}: {diagnostic.Message}");
            return 2;
        }

        using var runtime = LanguageRuntime.Create(
            compilation.GetRequiredPlan(),
            new ILanguageRouteComponentSource[] { package });
        var cases = BuildCases();
        var observations = new List<Observation>();
        foreach (var @case in cases)
        {
            foreach (var policy in Enum.GetValues<TensorPolicy>())
                observations.Add(Run(runtime, @case, policy));
        }

        Validate(cases, observations);
        var result = new StudyResult(
            ValidExamples: cases.Count(static item => item.Role == CaseRole.Valid),
            InvalidExamples: cases.Count(static item => item.Role == CaseRole.Invalid),
            FaultCases: cases.Count(static item => item.Role == CaseRole.Fault),
            Observations: observations.Count,
            SelectiveAlwaysParity: cases.Count,
            PublicSdkBoundary: true,
            WistReferences: 0,
            Cases: cases,
            Results: observations);
        File.WriteAllText(Path.Combine(output, "results.json"), JsonSerializer.Serialize(result, JsonOptions) + "\n");
        File.WriteAllText(Path.Combine(output, "summary.json"), JsonSerializer.Serialize(new
        {
            schemaVersion = 2,
            status = "VALIDATED",
            languageId = "TensorRules",
            validExamples = result.ValidExamples,
            invalidExamples = result.InvalidExamples,
            faultCases = result.FaultCases,
            observations = result.Observations,
            selectiveAlwaysParity = result.SelectiveAlwaysParity,
            historicalV1Cases = cases.Count(static item => item.StudySet == "historical-v1"),
            demandV2Cases = cases.Count(static item => item.StudySet == "demand-v2"),
            demandBaseline = new
            {
                queried = observations.Single(static item => item.CaseId == "demand-query" && item.Policy == TensorPolicy.P1D_DEMAND_RECOMPUTATION).Classification,
                unqueried = observations.Single(static item => item.CaseId == "demand-no-query" && item.Policy == TensorPolicy.P1D_DEMAND_RECOMPUTATION).Classification
            },
            publicSdkBoundary = result.PublicSdkBoundary,
            independentlyAuthored = false,
            label = "second-language-package"
        }, JsonOptions) + "\n");
        Console.WriteLine($"TENSORRULES_SUMMARY={JsonSerializer.Serialize(new { result.ValidExamples, result.InvalidExamples, result.FaultCases, result.Observations, result.SelectiveAlwaysParity })}");
        return 0;
    }

    private static AuthoredLanguagePackage BuildPackage() =>
        LanguagePackageBuilder.Create("TensorRules", "1.0.0")
            .AddFeature("tensorrules.core", feature => feature
                .AddTransformer(
                    "tensorrules.parse",
                    LanguageSlots.FrontendParser,
                    StandardLanguageArtifactKinds.SourceText,
                    Syntax,
                    static (source, _) => TensorSyntax.Parse(source),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1)
                .AddTransformer(
                    "tensorrules.shape-types",
                    LanguageSlots.SemanticsTypes,
                    Syntax,
                    Plan,
                    static (syntax, _) => TensorVerifier.VerifyInitial(syntax),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop,
                    cost: 1)
                .AddPass(
                    "tensorrules.contract-pass",
                    LanguageSlots.Optimizers,
                    Plan,
                    static (plan, context) => TensorPolicyPass.Apply(plan, context),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop)
                .AddBackend(
                    Interpreter,
                    new LanguageContributionId("tensorrules.interpreter"),
                    Plan,
                    static (plan, _) => new TensorExecutionResult(plan.Output, plan.VerifierInvocations),
                    LanguageRuntimeComponentTraits.DeterministicNoHostInterop))
            .UseRouteRuntime("tensorrules.runtime", "1.0.0")
            .Build();

    private static Observation Run(LanguageRuntime runtime, TensorCase @case, TensorPolicy policy)
    {
        var arguments = new Dictionary<string, object?>(StringComparer.Ordinal)
        {
            ["policy"] = policy.ToString(),
            ["fault"] = @case.FaultId,
            ["demand"] = @case.ExplicitDemand
        };
        try
        {
            var result = runtime.Run(new LanguageExecutionRequest(@case.Source, Interpreter, arguments));
            var execution = result.Value as TensorExecutionResult
                ?? throw new InvalidOperationException("TensorRules backend returned an unexpected value type.");
            return new Observation(
                @case.Id,
                @case.StudySet,
                policy,
                @case.ExplicitDemand,
                execution.Output == @case.ExpectedOutput ? "accepted" : "wrong-result",
                execution.Output,
                null,
                execution.VerifierInvocations);
        }
        catch (Exception exception)
        {
            var failure = FindFailure(exception);
            return new Observation(
                @case.Id,
                @case.StudySet,
                policy,
                @case.ExplicitDemand,
                "rejected",
                null,
                failure.Code,
                failure.VerifierInvocations);
        }
    }

    private static TensorRulesException FindFailure(Exception exception)
    {
        for (Exception? current = exception; current != null; current = current.InnerException)
        {
            if (current is TensorRulesException typed)
                return typed;
        }
        return new TensorRulesException(exception.GetType().Name, exception.Message, 0);
    }

    private static void Validate(IReadOnlyList<TensorCase> cases, IReadOnlyList<Observation> observations)
    {
        if (cases.Count != 14 || observations.Count != 70)
            throw new InvalidOperationException("TensorRules study cardinality changed.");

        if (cases.Count(static item => item.StudySet == "historical-v1") != 12 ||
            cases.Count(static item => item.StudySet == "demand-v2") != 2)
            throw new InvalidOperationException("TensorRules versioned study-set cardinality changed.");

        foreach (var @case in cases)
        {
            var rows = observations.Where(item => item.CaseId == @case.Id).ToArray();
            if (rows.Length != 5)
                throw new InvalidOperationException($"{@case.Id}: expected five policies.");
            var selective = rows.Single(item => item.Policy == TensorPolicy.P2_SELECTIVE);
            var always = rows.Single(item => item.Policy == TensorPolicy.P3_ALWAYS);
            if ((selective.Classification, selective.Output, selective.DiagnosticCode) !=
                (always.Classification, always.Output, always.DiagnosticCode))
                throw new InvalidOperationException($"{@case.Id}: P2/P3 parity mismatch.");

            foreach (var row in rows)
            {
                switch (@case.Role)
                {
                    case CaseRole.Valid:
                        if (row.Classification != "accepted" || row.Output != @case.ExpectedOutput)
                            throw new InvalidOperationException($"{@case.Id}/{row.Policy}: valid case failed.");
                        break;
                    case CaseRole.Invalid:
                        if (row.Classification != "rejected" || row.DiagnosticCode != @case.ExpectedDiagnostic)
                            throw new InvalidOperationException($"{@case.Id}/{row.Policy}: invalid case classification changed.");
                        break;
                    case CaseRole.Fault when row.Policy is TensorPolicy.P0_STRUCTURAL or TensorPolicy.P1_INVALIDATION:
                        if (row.Classification != "wrong-result" || row.Output == @case.ExpectedOutput)
                            throw new InvalidOperationException($"{@case.Id}/{row.Policy}: no-protocol fault symptom missing.");
                        break;
                    case CaseRole.Fault when row.Policy == TensorPolicy.P1D_DEMAND_RECOMPUTATION && !@case.ExplicitDemand:
                        if (row.Classification != "wrong-result" || row.Output == @case.ExpectedOutput)
                            throw new InvalidOperationException($"{@case.Id}/{row.Policy}: unqueried demand baseline unexpectedly verified.");
                        break;
                    case CaseRole.Fault:
                        if (row.Classification != "rejected" || row.DiagnosticCode != @case.ExpectedDiagnostic)
                            throw new InvalidOperationException($"{@case.Id}/{row.Policy}: protocol did not reject the fault.");
                        break;
                }
            }
        }

        foreach (var valid in cases.Where(static item => item.Role == CaseRole.Valid))
        {
            var selective = observations.Single(item => item.CaseId == valid.Id && item.Policy == TensorPolicy.P2_SELECTIVE);
            var always = observations.Single(item => item.CaseId == valid.Id && item.Policy == TensorPolicy.P3_ALWAYS);
            if (selective.VerifierInvocations != 0 || always.VerifierInvocations != 1)
                throw new InvalidOperationException($"{valid.Id}: clean-boundary policy scheduling is not distinct.");
        }


        var queried = observations.Single(static item =>
            item.CaseId == "demand-query" && item.Policy == TensorPolicy.P1D_DEMAND_RECOMPUTATION);
        var unqueried = observations.Single(static item =>
            item.CaseId == "demand-no-query" && item.Policy == TensorPolicy.P1D_DEMAND_RECOMPUTATION);
        if (queried.Classification != "rejected" || unqueried.Classification != "wrong-result")
            throw new InvalidOperationException("TensorRules demand-baseline counterexample changed.");
    }

    private static IReadOnlyList<TensorCase> BuildCases() =>
    [
        new("valid-1", "historical-v1", CaseRole.Valid, "matmul 2x3 3x4 row", "2x4:row", "none", null, false),
        new("valid-2", "historical-v1", CaseRole.Valid, "matmul 1x8 8x2 column", "1x2:column", "none", null, false),
        new("invalid-shape", "historical-v1", CaseRole.Invalid, "matmul 2x3 4x5 row", null, "none", "TR-SHAPE-001", false),
        new("invalid-extent", "historical-v1", CaseRole.Invalid, "matmul 0x3 3x4 row", null, "none", "TR-SHAPE-002", false),
        new("fault-inner", "historical-v1", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "inner", "TR-SHAPE-001", false),
        new("fault-output", "historical-v1", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "output", "TR-SHAPE-002", false),
        new("fault-layout", "historical-v1", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "layout", "TR-LAYOUT-001", false),
        new("fault-dynamic", "historical-v1", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "dynamic", "TR-SHAPE-002", false),
        new("fault-backend", "historical-v1", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "backend", "TR-BACKEND-001", false),
        new("fault-broadcast", "historical-v1", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "broadcast", "TR-SHAPE-001", false),
        new("fault-stale", "historical-v1", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "stale", "TR-SHAPE-002", false),
        new("fault-owner", "historical-v1", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "owner", "TR-OWNER-001", false),
        new("demand-query", "demand-v2", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "stale", "TR-SHAPE-002", true),
        new("demand-no-query", "demand-v2", CaseRole.Fault, "matmul 2x3 3x4 row", "2x4:row", "stale", "TR-SHAPE-002", false)
    ];
}

internal enum TensorPolicy { P0_STRUCTURAL, P1_INVALIDATION, P1D_DEMAND_RECOMPUTATION, P2_SELECTIVE, P3_ALWAYS }
internal enum CaseRole { Valid, Invalid, Fault }
internal sealed record TensorCase(string Id, string StudySet, CaseRole Role, string Source, string? ExpectedOutput, string FaultId, string? ExpectedDiagnostic, bool ExplicitDemand);
internal sealed record Observation(string CaseId, string StudySet, TensorPolicy Policy, bool DemandQuery, string Classification, string? Output, string? DiagnosticCode, int VerifierInvocations);
internal sealed record StudyResult(int ValidExamples, int InvalidExamples, int FaultCases, int Observations, int SelectiveAlwaysParity, bool PublicSdkBoundary, int WistReferences, IReadOnlyList<TensorCase> Cases, IReadOnlyList<Observation> Results);
internal sealed record TensorSyntax(int LeftRows, int Inner, int RightRows, int RightColumns, string Layout)
{
    public static TensorSyntax Parse(string source)
    {
        var parts = source.Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length != 4 || parts[0] != "matmul")
            throw new TensorRulesException("TR-SYNTAX-001", "Expected: matmul <rows>x<inner> <inner>x<cols> <layout>.", 0);
        var left = ParseExtent(parts[1]);
        var right = ParseExtent(parts[2]);
        return new TensorSyntax(left.Rows, left.Columns, right.Rows, right.Columns, parts[3]);
    }

    private static (int Rows, int Columns) ParseExtent(string value)
    {
        var parts = value.Split('x');
        if (parts.Length != 2 || !int.TryParse(parts[0], out var rows) || !int.TryParse(parts[1], out var columns))
            throw new TensorRulesException("TR-SYNTAX-001", $"Invalid extent '{value}'.", 0);
        return (rows, columns);
    }
}
internal sealed record TensorPlan(int LeftRows, int Inner, int RightRows, int RightColumns, int OutputRows, int OutputColumns, string Layout, string RequiredBackend, string Output, int VerifierInvocations);
internal sealed record TensorExecutionResult(string Output, int VerifierInvocations);

internal static class TensorVerifier
{
    public static TensorPlan VerifyInitial(TensorSyntax syntax)
    {
        if (syntax.LeftRows <= 0 || syntax.Inner <= 0 || syntax.RightRows <= 0 || syntax.RightColumns <= 0)
            throw new TensorRulesException("TR-SHAPE-002", "Tensor extents must be positive.", 1);
        if (syntax.Inner != syntax.RightRows)
            throw new TensorRulesException("TR-SHAPE-001", "Matmul inner extents do not match.", 1);
        if (syntax.Layout is not ("row" or "column"))
            throw new TensorRulesException("TR-LAYOUT-001", "Unknown tensor layout.", 1);
        return new TensorPlan(
            syntax.LeftRows,
            syntax.Inner,
            syntax.RightRows,
            syntax.RightColumns,
            syntax.LeftRows,
            syntax.RightColumns,
            syntax.Layout,
            "tensor-interpreter",
            $"{syntax.LeftRows}x{syntax.RightColumns}:{syntax.Layout}",
            0);
    }

    public static void VerifySemantic(TensorPlan plan, int invocations)
    {
        if (plan.Inner != plan.RightRows)
            throw new TensorRulesException("TR-SHAPE-001", "Matmul inner extents do not match after optimization.", invocations);
        if (plan.OutputRows <= 0 || plan.OutputColumns <= 0 || plan.OutputRows != plan.LeftRows || plan.OutputColumns != plan.RightColumns)
            throw new TensorRulesException("TR-SHAPE-002", "Output tensor extent is invalid or stale.", invocations);
        if (plan.Layout is not ("row" or "column"))
            throw new TensorRulesException("TR-LAYOUT-001", "Optimized tensor layout is invalid.", invocations);
        if (plan.RequiredBackend != "tensor-interpreter")
            throw new TensorRulesException("TR-BACKEND-001", "Tensor plan requires an unavailable backend capability.", invocations);
        if (plan.Output.StartsWith("owner-conflict", StringComparison.Ordinal))
            throw new TensorRulesException("TR-OWNER-001", "Tensor fact has multiple canonical owners.", invocations);
    }
}

internal static class TensorPolicyPass
{
    public static TensorPlan Apply(TensorPlan input, LanguageArtifactTransformationContext context)
    {
        var policy = ParsePolicy(context.Request.Arguments);
        var fault = ReadString(context.Request.Arguments, "fault", "none");
        var demand = ReadBool(context.Request.Arguments, "demand");
        var mutated = ApplyFault(input, fault);
        var invalidated = fault != "none";
        var shouldVerify = policy == TensorPolicy.P3_ALWAYS ||
                           policy == TensorPolicy.P2_SELECTIVE && invalidated ||
                           policy == TensorPolicy.P1D_DEMAND_RECOMPUTATION && invalidated && demand;
        if (!shouldVerify)
            return mutated with { VerifierInvocations = 0 };
        TensorVerifier.VerifySemantic(mutated, 1);
        return mutated with { VerifierInvocations = 1 };
    }

    private static TensorPolicy ParsePolicy(IReadOnlyDictionary<string, object?> arguments)
    {
        var value = ReadString(arguments, "policy", TensorPolicy.P3_ALWAYS.ToString());
        if (!Enum.TryParse<TensorPolicy>(value, out var policy) || !Enum.IsDefined(policy))
            throw new TensorRulesException("TR-POLICY-001", $"Unknown TensorRules policy '{value}'.", 0);
        return policy;
    }

    private static string ReadString(IReadOnlyDictionary<string, object?> arguments, string key, string fallback) =>
        arguments.TryGetValue(key, out var value) && value is string text ? text : fallback;

    private static bool ReadBool(IReadOnlyDictionary<string, object?> arguments, string key) =>
        arguments.TryGetValue(key, out var value) && value is true;

    private static TensorPlan ApplyFault(TensorPlan plan, string fault) => fault switch
    {
        "none" => plan,
        "inner" => plan with { RightRows = plan.Inner + 1, Output = "inner-mismatch" },
        "output" => plan with { OutputRows = 0, Output = "zero-output" },
        "layout" => plan with { Layout = "strided-unknown", Output = "layout-mismatch" },
        "dynamic" => plan with { OutputColumns = -1, Output = "dynamic-extent" },
        "backend" => plan with { RequiredBackend = "tensor-gpu", Output = "backend-mismatch" },
        "broadcast" => plan with { Inner = plan.Inner + 2, Output = "broadcast-mismatch" },
        "stale" => plan with { OutputRows = plan.LeftRows + 1, Output = "stale-shape" },
        "owner" => plan with { Output = "owner-conflict" },
        _ => throw new TensorRulesException("TR-FAULT-001", $"Unknown fault '{fault}'.", 0)
    };
}

internal sealed class TensorRulesException : InvalidOperationException
{
    public TensorRulesException(string code, string message, int verifierInvocations) : base(message)
    {
        Code = code;
        VerifierInvocations = verifierInvocations;
    }
    public string Code { get; }
    public int VerifierInvocations { get; }
}
