using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.ViewModels.Hospitals;
using HealthInsuranceClaims.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace HealthInsuranceClaims.Application.Services;

public class HospitalService : IHospitalService
{
    private readonly IApplicationDbContext _context;

    public HospitalService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<List<HospitalViewModel>> GetAllHospitalsAsync()
    {
        return await _context.Hospitals
            .Select(h => new HospitalViewModel
            {
                HospitalId = h.HospitalId,
                HospitalName = h.HospitalName,
                Address = h.Address,
                ContactNumber = h.ContactNumber,
                Email = h.Email,
                IsActive = h.IsActive,
                TotalClaimsTreated = h.Claims.Count
            })
            .OrderBy(h => h.HospitalName)
            .ToListAsync();
    }

    public async Task<List<Hospital>> GetActiveHospitalsAsync()
    {
        return await _context.Hospitals
            .Where(h => h.IsActive)
            .OrderBy(h => h.HospitalName)
            .ToListAsync();
    }

    public async Task<Hospital?> GetHospitalByIdAsync(int hospitalId)
    {
        return await _context.Hospitals.FindAsync(hospitalId);
    }

    public async Task<int> CreateHospitalAsync(HospitalViewModel model)
    {
        var hospital = new Hospital
        {
            HospitalName = model.HospitalName.Trim(),
            Address = model.Address?.Trim(),
            ContactNumber = model.ContactNumber?.Trim(),
            Email = model.Email?.Trim(),
            IsActive = model.IsActive,
            CreatedDate = DateTime.UtcNow
        };

        _context.Hospitals.Add(hospital);
        await _context.SaveChangesAsync();
        return hospital.HospitalId;
    }

    public async Task<bool> UpdateHospitalAsync(HospitalViewModel model)
    {
        var hospital = await _context.Hospitals.FindAsync(model.HospitalId);
        if (hospital == null) return false;

        hospital.HospitalName = model.HospitalName.Trim();
        hospital.Address = model.Address?.Trim();
        hospital.ContactNumber = model.ContactNumber?.Trim();
        hospital.Email = model.Email?.Trim();
        hospital.IsActive = model.IsActive;

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> ToggleHospitalStatusAsync(int hospitalId)
    {
        var hospital = await _context.Hospitals.FindAsync(hospitalId);
        if (hospital == null) return false;

        hospital.IsActive = !hospital.IsActive;
        await _context.SaveChangesAsync();
        return true;
    }
}
