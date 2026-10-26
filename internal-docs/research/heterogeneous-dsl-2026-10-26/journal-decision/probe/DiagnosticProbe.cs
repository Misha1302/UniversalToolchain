using System.Reflection;
using System.Text.Json;
using UniversalToolchain.FeatureSdk;
using UniversalToolchain.Language.Abstractions;
using UniversalToolchain.LanguageAuthoring;
using UniversalToolchain.LanguageSdk;
using UniversalToolchain.Runtime;
using UniversalToolchain.ModuleContracts;
using UniversalToolchain.TensorRules;

internal static class DiagnosticProbe
{
    public static int Main(string[] args)
    {
        var outPath = args.Length > 0 ? args[0] : "diagnostic-probe.json";
        var package = (AuthoredLanguagePackage) typeof(UniversalToolchain.TensorRules.Program)
            .GetMethod("BuildPackage", BindingFlags.NonPublic | BindingFlags.Static)!.Invoke(null, null)!;
        var backend = new BackendId("tensor-interpreter");
        var definition = LanguageDefinitionBuilder.Create("TensorRules", "1.0.0")
            .UseFeature("tensorrules.core").EnableBackend(backend)
            .UseRuntimeProvider("tensorrules.runtime", "1.0.0")
            .WithRuntimePolicy(new LanguageRuntimePolicy(RequireDeterminism: true, MaximumSourceLength: 256)).Build();
        var languagePlan = new LanguageCompiler(new LanguagePackageRegistry().AddPackage(package))
            .Compile(definition).GetRequiredPlan();
        var original = TensorVerifier.VerifyInitial(TensorSyntax.Parse("matmul 2x3 3x4 row"));
        var rows = new List<object>();
        var matchedSchedulerRows = new List<object>();
        var cases = new[]
        {
            (Name: "clean-control", Input: original, Fault: "none", IndependentValid: true),
            (Name: "declared-inner-corruption", Input: original, Fault: "inner", IndependentValid: false),
            (Name: "undeclared-inner-corruption", Input: original with { RightRows = 4 }, Fault: "none", IndependentValid: false),
            (Name: "unverified-output-corruption", Input: original with { Output = "unrelated-output" }, Fault: "none", IndependentValid: false)
        };
        foreach (var item in cases)
        {
            foreach (var policy in new[] { TensorPolicy.P2_SELECTIVE, TensorPolicy.P3_ALWAYS })
            {
                var context = new LanguageArtifactTransformationContext(languagePlan,
                    new LanguageExecutionRequest("matmul 2x3 3x4 row", backend,
                        new Dictionary<string,object?> { ["policy"] = policy.ToString(), ["fault"] = item.Fault }),
                    new LanguageRuntimeOptions());
                try
                {
                    var result = TensorPolicyPass.Apply(item.Input, context);
                    // Independent local specification: relational shape checks + canonical output projection.
                    // This is a deterministic mechanism probe, not an external/blind fault corpus.
                    var independentValid = result.Inner == result.RightRows
                        && result.OutputRows == result.LeftRows && result.OutputColumns == result.RightColumns
                        && result.OutputRows > 0 && result.OutputColumns > 0
                        && result.Output == $"{result.LeftRows}x{result.RightColumns}:{result.Layout}";
                    rows.Add(new { item.Name, policy = policy.ToString(), status = "accepted", result.Output,
                        result.VerifierInvocations, independentValid, diagnostic = (string?)null });
                }
                catch (TensorRulesException e)
                {
                    rows.Add(new { item.Name, policy = policy.ToString(), status = "rejected", Output = (string?)null,
                        e.VerifierInvocations, independentValid = item.IndependentValid, diagnostic = e.Code });
                }
            }
        }
        foreach (var item in cases)
        {
            var changed = (TensorPlan) typeof(TensorPolicyPass).GetMethod("ApplyFault", BindingFlags.NonPublic | BindingFlags.Static)!
                .Invoke(null, new object[] { item.Input, item.Fault })!;
            bool declaredInvalidation = item.Fault != "none";
            var shapeRule = new VerifierRuleId("probe.shape");
            var routes = new[] { new ModuleContractVerifierRoute(shapeRule, "probe.owner") };
            IReadOnlyList<ReverificationRequest> requests = declaredInvalidation
                ? new[] { new ReverificationRequest(shapeRule, new[] { new CompilerFactId("probe.shape.valid") }) }
                : Array.Empty<ReverificationRequest>();
            foreach (var treatment in new[] { "production-selective", "production-always", "typed-manual-selective" })
            {
                var invocations = 0;
                var independentValid = changed.Inner == changed.RightRows
                    && changed.OutputRows == changed.LeftRows && changed.OutputColumns == changed.RightColumns
                    && changed.OutputRows > 0 && changed.OutputColumns > 0
                    && changed.Output == $"{changed.LeftRows}x{changed.RightColumns}:{changed.Layout}";
                try
                {
                    // Identical checker and explicit invalidation information in every arm.
                    // The manual arm is a direct typed conditional for this one-rule scope.
                    var count = treatment == "typed-manual-selective"
                        ? (declaredInvalidation ? 1 : 0)
                        : ModuleContractVerificationScheduler.Schedule(
                            treatment == "production-selective" ? ModuleContractVerificationPolicy.P2Selective : ModuleContractVerificationPolicy.P3Always,
                            routes, requests).Count;
                    for (int i = 0; i < count; i++)
                    {
                        invocations++;
                        TensorVerifier.VerifySemantic(changed, invocations);
                    }
                    matchedSchedulerRows.Add(new { item.Name, treatment, declaredInvalidation,
                        status = "accepted", invocations, independentValid, diagnostic = (string?)null });
                }
                catch (TensorRulesException e)
                {
                    matchedSchedulerRows.Add(new { item.Name, treatment, declaredInvalidation,
                        status = "rejected", invocations, independentValid, diagnostic = e.Code });
                }
            }
        }
        var route = new ModuleContractVerifierRoute(new VerifierRuleId("probe.shape"), "probe.owner");
        var genericCounts = new[] { ModuleContractVerificationPolicy.P2Selective, ModuleContractVerificationPolicy.P3Always }
            .Select(policy => new { policy = policy.ToString(), requests = 0,
                scheduled = ModuleContractVerificationScheduler.Schedule(policy, new[] { route }, Array.Empty<ReverificationRequest>()).Count }).ToArray();
        var resultText = JsonSerializer.Serialize(new { schemaVersion = 1,
            scope = "Model-designed deterministic mechanism probe; linked unmodified TensorRules source and production generic scheduler; not external blind evidence or numerical execution",
            rows, matchedSchedulerRows, genericCounts }, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(outPath, resultText + "\n");
        Console.WriteLine(resultText);
        return 0;
    }
}
