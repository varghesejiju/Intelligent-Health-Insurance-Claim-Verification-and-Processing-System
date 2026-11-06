namespace HealthInsuranceClaims.Domain.Enums;

public enum ClaimStatus
{
    Draft = 1,
    Submitted = 2,
    UnderReview = 3,
    InformationRequested = 4,
    Resubmitted = 5,
    Approved = 6,
    PartiallyApproved = 7,
    Rejected = 8,
    Closed = 9
}
