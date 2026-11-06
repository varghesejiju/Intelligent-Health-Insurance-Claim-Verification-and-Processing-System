using HealthInsuranceClaims.Application.ViewModels.Hospitals;
using HealthInsuranceClaims.Domain.Entities;

namespace HealthInsuranceClaims.Application.Interfaces;

public interface IHospitalService
{
    Task<List<HospitalViewModel>> GetAllHospitalsAsync();
    Task<List<Hospital>> GetActiveHospitalsAsync();
    Task<Hospital?> GetHospitalByIdAsync(int hospitalId);
    Task<int> CreateHospitalAsync(HospitalViewModel model);
    Task<bool> UpdateHospitalAsync(HospitalViewModel model);
    Task<bool> ToggleHospitalStatusAsync(int hospitalId);
}
