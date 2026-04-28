namespace BasicCore.Execution;

public sealed class ExternalRuntimeCallProvider
{
    private readonly IExecutionEnvironment _environment;

    public ExternalRuntimeCallProvider(IExecutionEnvironment environment)
    {
        _environment = environment.ArgNotNull();
    }

    public IExecutionEnvironment LoadEnvironment()
    {
        return _environment;
    }
}
