namespace UniversalToolchain.ModuleContracts;

public sealed class OptimizerAirValidationHook(IAirVerifier verifier) : IOptimizerAirValidationHook
{
    private readonly IAirVerifier _verifier = verifier.ArgNotNull();

    public AirVerificationResult Validate(OptimizerAirValidationRequest request)
    {
        request = request.ArgNotNull();

        if (string.IsNullOrWhiteSpace(request.OptimizerId))
            Thrower.Argument(nameof(request), "Optimizer validation request must include an optimizer id.");

        return _verifier.Verify(new AirVerificationRequest(
            request.OptimizedAir,
            request.ContractTable,
            request.BackendSelection,
            request.Profile));
    }
}
