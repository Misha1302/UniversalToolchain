namespace UniversalToolchain.ModuleContracts;

public interface IBytecodeVerifier
{
    BytecodeVerificationResult Verify(BytecodeVerificationRequest request);
}
