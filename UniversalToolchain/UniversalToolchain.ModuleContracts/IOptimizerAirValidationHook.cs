namespace UniversalToolchain.ModuleContracts;

public interface IOptimizerAirValidationHook
{
    AirVerificationResult Validate(OptimizerAirValidationRequest request);
}
