using HealthInsuranceClaims.Domain.Entities;

namespace HealthInsuranceClaims.Application.Interfaces;

public interface IClaimVerificationService
{
    Task<VerificationRun> VerifyClaimAsync(int claimId);
    VerificationRun CalculateVerification(Claim claim, InsurancePolicy policy);
}
