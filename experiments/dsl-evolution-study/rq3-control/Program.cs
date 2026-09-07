using System.Globalization;
using System.Text.Json;
using DslEvolutionStudy.Rq3;

if (args.Length != 2 || args[0] != "--treatment")
{
    Console.Error.WriteLine("usage: --treatment shared-pipeline|universal-toolchain");
    return 2;
}

var spec = Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "spec", "cases.json");
if (!File.Exists(spec))
    spec = Path.Combine(Directory.GetCurrentDirectory(), "experiments", "dsl-evolution-study", "rq3-control", "spec", "cases.json");
var cases = JsonSerializer.Deserialize<List<Rq3Case>>(File.ReadAllText(spec), new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
foreach (var @case in cases)
{
    var result = args[1] switch
    {
        "shared-pipeline" => SharedPipelineTreatment.Evaluate(@case.Source, @case.Features),
        "universal-toolchain" => UtDownstreamTreatment.Evaluate(@case.Source, @case.Features),
        _ => throw new ArgumentException($"Unknown treatment: {args[1]}")
    };
    Console.WriteLine(JsonSerializer.Serialize(new Observation(
        @case.Id, result.Value.ToString("0.################", CultureInfo.InvariantCulture), result.RemainingOperations)));
}

internal sealed record Rq3Case(string Id, string Source, string[] Features, string Expected);
internal sealed record Observation(string Id, string Value, int RemainingOperations);
