namespace UniversalToolchain.Testing.Infrastructure;

public static class DialectTestHostInfrastructure
{
    public static object? RunInBothBackends(string dialectText, string code)
    {
        var (compilerResult, interpreterResult) = BackendParityInfrastructure.RunBoth(dialectText, code);
        BackendParityInfrastructure.AssertSemanticParity(compilerResult, interpreterResult);
        return compilerResult.Value;
    }
}
