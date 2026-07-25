namespace UniversalToolchain.PlanFuzz.Adapter.Acme;

internal static class AcmePricingProgramReducer
{
    public static long GetComplexity(AcmePricingProgramModel model)
    {
        model = model.ArgNotNull();
        return checked(
            NumericComplexity(model.UnitPrice) +
            NumericComplexity(model.Quantity) +
            NumericComplexity(model.Discount));
    }

    public static IReadOnlyList<PlanFuzzProgramReductionCandidate> CreateCandidates(
        PlanFuzzTestCase testCase,
        AcmePricingProgramModel model)
    {
        testCase = testCase.ArgNotNull();
        model = model.ArgNotNull();
        var currentComplexity = GetComplexity(model);
        var candidates = new List<PlanFuzzProgramReductionCandidate>();
        var seenPayloads = new HashSet<string>(StringComparer.Ordinal);

        void Add(string id, string summary, AcmePricingProgramModel candidate)
        {
            var complexity = GetComplexity(candidate);
            if (complexity >= currentComplexity)
                return;
            var program = CreateProgram(testCase, candidate);
            if (!seenPayloads.Add(program.Model.CanonicalJson))
                return;
            candidates.Add(new PlanFuzzProgramReductionCandidate(id, summary, complexity, program));
        }

        try
        {
            var product = checked(model.UnitPrice * model.Quantity);
            if (model.UnitPrice != 1m)
            {
                Add(
                    "factor-unit-price-to-one",
                    "Replace the unit price with one while preserving the exact product.",
                    model with { UnitPrice = 1m, Quantity = product });
            }
            if (model.Quantity != 1m)
            {
                Add(
                    "factor-quantity-to-one",
                    "Replace the quantity with one while preserving the exact product.",
                    model with { UnitPrice = product, Quantity = 1m });
            }
        }
        catch (OverflowException)
        {
            // The original generated model is valid; an unavailable factorization is simply not a candidate.
        }

        AddNumericCandidates("unit-price", model.UnitPrice, value => model with { UnitPrice = value }, Add);
        AddNumericCandidates("quantity", model.Quantity, value => model with { Quantity = value }, Add);
        AddNumericCandidates("discount", model.Discount, value => model with { Discount = value }, Add);

        return new ReadOnlyCollection<PlanFuzzProgramReductionCandidate>(candidates
            .OrderBy(static candidate => candidate.Complexity)
            .ThenBy(static candidate => candidate.CandidateId, StringComparer.Ordinal)
            .ToArray());
    }

    private static void AddNumericCandidates(
        string field,
        decimal current,
        Func<decimal, AcmePricingProgramModel> create,
        Action<string, string, AcmePricingProgramModel> add)
    {
        foreach (var target in NumericTargets(current))
        {
            add(
                $"{field}-to-{TargetId(target)}",
                $"Reduce {field} from {Format(current)} to {Format(target)}.",
                create(target));
        }
    }

    private static IEnumerable<decimal> NumericTargets(decimal value)
    {
        var targets = new List<decimal>();
        void Add(decimal target)
        {
            if (target != value && !targets.Contains(target))
                targets.Add(target);
        }

        Add(0m);
        Add(1m);
        Add(-1m);
        Add(decimal.Truncate(value));
        Add(value / 2m);
        return targets;
    }

    private static PlanFuzzProgram CreateProgram(
        PlanFuzzTestCase testCase,
        AcmePricingProgramModel model) =>
        new(
            testCase.Program.ModelKind,
            testCase.Program.ModelSchemaVersion,
            model.ToPayload(),
            model.RenderSource(),
            testCase.Program.ProgramClass);

    private static long NumericComplexity(decimal value)
    {
        if (value == 0m)
            return 0;
        if (value == 1m)
            return 1;
        if (value == -1m)
            return 2;

        var text = Format(value);
        var punctuation = text.Count(static character => character is '-' or '.');
        return checked(10L + text.Length * 2L + punctuation);
    }

    private static string TargetId(decimal value) => value switch
    {
        0m => "zero",
        1m => "one",
        -1m => "minus-one",
        _ => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(Format(value))))[..12].ToLowerInvariant()
    };

    private static string Format(decimal value) => value.ToString("G29", CultureInfo.InvariantCulture);
}
