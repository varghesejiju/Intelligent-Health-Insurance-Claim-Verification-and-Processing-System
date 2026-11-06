using HealthInsuranceClaims.Application.ViewModels.Dashboard;

namespace HealthInsuranceClaims.Application.Interfaces;

public interface IDashboardService
{
    Task<CustomerDashboardViewModel> GetCustomerDashboardAsync(string customerId);
    Task<OfficerDashboardViewModel> GetOfficerDashboardAsync();
    Task<AdminDashboardViewModel> GetAdminDashboardAsync();
    Task<HospitalDeskDashboardViewModel> GetHospitalDeskDashboardAsync(int hospitalId);
}
