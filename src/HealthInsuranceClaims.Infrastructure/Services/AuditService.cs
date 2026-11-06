using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Infrastructure.Data;

namespace HealthInsuranceClaims.Infrastructure.Services;

public class AuditService : IAuditService
{
    private readonly ApplicationDbContext _context;

    public AuditService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task LogAsync(string? userId, string userName, string action, string entityType, string? entityId, string? details, string? ipAddress = null)
    {
        var log = new AuditLog
        {
            UserId = userId,
            UserName = string.IsNullOrWhiteSpace(userName) ? "System" : userName,
            Action = action,
            EntityType = entityType,
            EntityId = entityId,
            Details = details,
            IpAddress = ipAddress,
            Timestamp = DateTime.UtcNow
        };

        _context.AuditLogs.Add(log);
        await _context.SaveChangesAsync();
    }
}
