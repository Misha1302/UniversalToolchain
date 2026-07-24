namespace UniversalToolchain.PlanFuzz.Cli;

public static class PlanFuzzExitCodes
{
    public const int Success = 0;
    public const int Usage = 1;
    public const int InvalidCase = 2;
    public const int InfrastructureFailure = 3;
    public const int Finding = 4;
    public const int UnhandledFailure = 5;
    public const int Inconclusive = 6;
    public const int Flaky = 7;
    public const int Timeout = 124;
    public const int ProcessCrash = 125;
}
