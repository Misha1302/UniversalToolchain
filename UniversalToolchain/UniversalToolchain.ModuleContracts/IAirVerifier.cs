namespace UniversalToolchain.ModuleContracts;

public interface IAirVerifier
{
    AirVerificationResult Verify(AirVerificationRequest request);
}
