using HealthInsuranceClaims.Application.ViewModels.Claims;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.AspNetCore.Http;

namespace HealthInsuranceClaims.Application.Interfaces;

public interface IClaimService
{
    Task<int> CreateDraftClaimAsync(CreateClaimViewModel model, string customerId, string customerName);
    Task<bool> AddExpenseAsync(AddExpenseViewModel model, string userId, int? hospitalId = null);
    Task<bool> RemoveExpenseAsync(int expenseId, string userId, int? hospitalId = null);
    Task<(bool Success, string Message)> UploadDocumentAsync(int claimId, DocumentType documentType, IFormFile file, string userId, string userName, int? hospitalId = null);
    Task<bool> DeleteDocumentAsync(int documentId, string userId, int? hospitalId = null);
    Task<ClaimDetailsViewModel?> GetClaimDetailsAsync(int claimId, string? userId = null, bool isStaff = false, int? hospitalId = null);
    Task<List<ClaimListViewModel>> GetCustomerClaimsAsync(string customerId);
    Task<List<ClaimListViewModel>> GetHospitalClaimsAsync(int hospitalId);
    Task<List<ClaimListViewModel>> GetAllClaimsAsync(string? search = null, ClaimStatus? status = null, DateTime? fromDate = null, DateTime? toDate = null);
    Task<List<ClaimListViewModel>> GetPendingOfficerClaimsAsync();
    Task<List<ClaimListViewModel>> GetDecidedClaimsAsync(DecisionType? decisionType = null, string? search = null);
    Task<string> GenerateClaimReferenceNumberAsync();
}
