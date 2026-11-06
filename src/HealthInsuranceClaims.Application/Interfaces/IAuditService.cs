namespace HealthInsuranceClaims.Application.Interfaces;

public interface IAuditService
{
    Task LogAsync(string? userId, string userName, string action, string entityType, string? entityId, string? details, string? ipAddress = null);
}
