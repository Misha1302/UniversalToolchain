using IntermediateRepresentationAbstractions;
using UniversalToolchain.Dialects.Wist;

var services = new ServiceCollection();
services.AddWistDialectServices();
services.AddWistCilBackend();
services.AddWistInterpreterBackend();

using var provider = services.BuildServiceProvider();
var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

var dialectFile = Path.Combine(AppContext.BaseDirectory, "Dialects", "examples", "wist", "full-default-native", "dialect.wistdialect");
var dialect = workflow.ComposeFile(dialectFile);
if (!dialect.IsSuccess)
{
    Console.WriteLine(dialect.ToDeterministicText());
    return;
}

using var host = workflow.CreateHost(dialect);

const string formula = "price * 0.9 + fee";
var declaredBindings = new OrderedDictionary<string, Type>
{
    ["price"] = typeof(double),
    ["fee"] = typeof(double)
};

var compiler = host.GetArtifactCompiler<DynamicMethod>("compiler");
var compiledArtifact = compiler.Compile(formula, declaredBindings);
var calculateCompiled = compiledArtifact.AsFunc<double, double, double>();
var compiledResult = calculateCompiled(100.0, 5.0);

var interpreter = host.GetArtifactCompiler<IAbstractIR>("interpreter");
var interpretedArtifact = interpreter.Compile(formula, declaredBindings);
var interpretedSession = interpretedArtifact.CreateSession();
interpretedSession.SetArgument("price", 100.0);
interpretedSession.SetArgument("fee", 5.0);
var interpretedResult = (double)interpretedSession.Run().NotNull();

var fastNativeInvoker = new DynamicMethodInvoker<double, double, double>(compiledArtifact.CompilationOutput);
var nativeInvokedResult = fastNativeInvoker.Invoke(100.0, 5.0);

Console.WriteLine($"Formula: {formula}");
Console.WriteLine($"Compiler result: {compiledResult}");
Console.WriteLine($"Interpreter result: {interpretedResult}");
Console.WriteLine($"Fast invoked result: {nativeInvokedResult}");
Console.WriteLine("Expected result: 95");