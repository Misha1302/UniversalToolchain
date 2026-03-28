using UniversalToolchain.Dialects.Wist;

var services = new ServiceCollection();
services.AddWistDialectServices();
services.AddWistCilBackend();
services.AddWistInterpreterBackend();

using var provider = services.BuildServiceProvider();
var workflow = provider.GetRequiredService<WistDialectExecutionWorkflow>();

var dialect = workflow.ComposeFile("./Dialects/examples/wist/full-default/dialect.wistdialect");
if (!dialect.IsSuccess)
{
    Console.WriteLine(dialect.ToDeterministicText());
    return;
}

using var host = workflow.CreateHost(dialect);
var result = host.Run("(2 + 2) * 3", "compiler");
Console.WriteLine(result);