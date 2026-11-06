using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Application.ViewModels.Dashboard;

public class CustomerDashboardViewModel
{
    public string CustomerName { get; set; } = string.Empty;
    public int ActivePoliciesCount { get; set; }
    public int TotalClaims { get; set; }
    public int DraftClaims { get; set; }
    public int SubmittedClaims { get; set; }
    public int UnderReviewClaims { get; set; }
    public int ApprovedClaims { get; set; }
    public int PartiallyApprovedClaims { get; set; }
    public int RejectedClaims { get; set; }

    public decimal TotalClaimedAmount { get; set; }
    public decimal TotalApprovedAmount { get; set; }

    public List<ClaimListViewModel> RecentClaims { get; set; } = new();
}

public class OfficerDashboardViewModel
{
    public string OfficerName { get; set; } = string.Empty;
    public int TotalClaims { get; set; }
    public int PendingReviews { get; set; }
    public int InfoRequestedClaims { get; set; }
    public int ApprovedClaims { get; set; }
    public int PartiallyApprovedClaims { get; set; }
    public int RejectedClaims { get; set; }

    public decimal TotalPendingClaimValue { get; set; }
    public decimal TotalApprovedAmount { get; set; }

    public List<ClaimListViewModel> PendingReviewQueue { get; set; } = new();
    public List<ClaimListViewModel> DecidedClaims { get; set; } = new();
}

public class AdminDashboardViewModel
{
    public int TotalCustomers { get; set; }
    public int TotalOfficers { get; set; }
    public int TotalPolicies { get; set; }
    public int ActivePolicies { get; set; }
    public int TotalHospitals { get; set; }
    public int TotalClaims { get; set; }

    public decimal AggregateClaimedAmount { get; set; }
    public decimal AggregateApprovedAmount { get; set; }

    public Dictionary<ClaimStatus, int> ClaimsByStatus { get; set; } = new();
    public List<ClaimListViewModel> RecentClaims { get; set; } = new();
    public List<AuditLog> RecentAuditLogs { get; set; } = new();
}

public class HospitalDeskDashboardViewModel
{
    public int HospitalId { get; set; }
    public string HospitalName { get; set; } = string.Empty;
    public int TotalClaims { get; set; }
    public int DraftClaims { get; set; }
    public int PendingReviewClaims { get; set; }
    public int InfoRequestedClaims { get; set; }
    public int ApprovedClaims { get; set; }
    public int PartiallyApprovedClaims { get; set; }
    public int RejectedClaims { get; set; }

    public decimal TotalClaimedAmount { get; set; }
    public decimal TotalApprovedAmount { get; set; }

    public List<ClaimListViewModel> RecentClaims { get; set; } = new();
}
