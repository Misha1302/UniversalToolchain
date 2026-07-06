namespace FunctionCallsModule;

public sealed class FunctionCallPlanningResult
{
    private FunctionCallPlanningResult(FunctionCallPlan? plan, string? diagnosticCode, string? diagnosticMessage)
    {
        Plan = plan;
        DiagnosticCode = diagnosticCode;
        DiagnosticMessage = diagnosticMessage;
    }

    public FunctionCallPlan? Plan { get; }

    public string? DiagnosticCode { get; }

    public string? DiagnosticMessage { get; }

    public bool IsSuccess => Plan != null;

    public static FunctionCallPlanningResult Success(FunctionCallPlan plan)
    {
        plan = plan.ArgNotNull();
        return new FunctionCallPlanningResult(plan, null, null);
    }

    public static FunctionCallPlanningResult Failure(string diagnosticCode, string diagnosticMessage)
    {
        if (string.IsNullOrWhiteSpace(diagnosticCode))
            Thrower.Argument(nameof(diagnosticCode), "Diagnostic code must not be empty.");

        if (string.IsNullOrWhiteSpace(diagnosticMessage))
            Thrower.Argument(nameof(diagnosticMessage), "Diagnostic message must not be empty.");

        return new FunctionCallPlanningResult(null, diagnosticCode, diagnosticMessage);
    }
}
