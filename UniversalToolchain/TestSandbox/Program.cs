using ExceptionsManager;
using Microsoft.Extensions.DependencyInjection;
using UniversalToolchain.Dialects.Wist;

var services = new ServiceCollection();

services.AddWistDialectServices();
services.AddWistCilBackend();
services.AddWistInterpreterBackend();

using var provider = services.BuildServiceProvider();
var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();
var dialect = workflow.ComposeText(
    """
    dialect DeclaredBindingsDialect
    use Arithmetic,Identifier,Numbers,Scopes,Variables,Whitespaces
    backend compiler,interpreter
    """,
    "DeclaredBindingsDialect"
);

if (!dialect.IsSuccess)
    Thrower.InvalidOpEx(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationFormatter.FormatDeterministic(UniversalToolchain.Dialects.Integration.DialectCompositionExplanationProjector.Project(dialect)));

var host = workflow.CreateHost(dialect);
var result = host.Run("2 + 3", "compiler");
Console.WriteLine(result ?? "null");