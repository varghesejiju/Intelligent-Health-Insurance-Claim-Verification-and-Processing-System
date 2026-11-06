using Microsoft.AspNetCore.Http;

namespace HealthInsuranceClaims.Application.Interfaces;

public interface IFileStorageService
{
    Task<(bool Success, string StoredFileName, string FilePath, string ErrorMessage)> SaveFileAsync(IFormFile file);
    Task<(byte[] Bytes, string ContentType, string DownloadName)?> GetFileAsync(string storedFileName, string originalFileName, string contentType);
    bool DeleteFile(string storedFileName);
}
