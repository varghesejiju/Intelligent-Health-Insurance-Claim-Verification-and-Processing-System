using HealthInsuranceClaims.Application.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace HealthInsuranceClaims.Infrastructure.Services;

public class PhysicalFileStorageService : IFileStorageService
{
    private readonly string _uploadDirectory;
    private readonly long _maxFileSizeInBytes;
    private readonly string[] _allowedExtensions;
    private readonly ILogger<PhysicalFileStorageService> _logger;

    public PhysicalFileStorageService(
        IWebHostEnvironment environment,
        IConfiguration configuration,
        ILogger<PhysicalFileStorageService> logger)
    {
        _logger = logger;

        var relativePath = configuration.GetValue<string>("FileStorage:UploadDirectory") ?? "App_Data/SecureUploads";
        _uploadDirectory = Path.IsPathRooted(relativePath)
            ? relativePath
            : Path.Combine(environment.ContentRootPath, relativePath);

        _maxFileSizeInBytes = configuration.GetValue<long>("FileStorage:MaxFileSizeInBytes", 10485760); // 10MB default
        _allowedExtensions = configuration.GetSection("FileStorage:AllowedExtensions").Get<string[]>() 
            ?? new[] { ".pdf", ".jpg", ".jpeg", ".png" };

        if (!Directory.Exists(_uploadDirectory))
        {
            Directory.CreateDirectory(_uploadDirectory);
        }
    }

    public async Task<(bool Success, string StoredFileName, string FilePath, string ErrorMessage)> SaveFileAsync(IFormFile file)
    {
        if (file == null || file.Length == 0)
        {
            return (false, string.Empty, string.Empty, "File is empty or not provided.");
        }

        if (file.Length > _maxFileSizeInBytes)
        {
            return (false, string.Empty, string.Empty, $"File exceeds the maximum permissible size of {_maxFileSizeInBytes / (1024 * 1024)} MB.");
        }

        var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (string.IsNullOrEmpty(extension) || !_allowedExtensions.Contains(extension))
        {
            return (false, string.Empty, string.Empty, $"File format '{extension}' is not permitted. Allowed formats: {string.Join(", ", _allowedExtensions)}.");
        }

        // Generate cryptographically random stored filename (GUID) to prevent directory traversal or collision
        var storedFileName = $"{Guid.NewGuid():N}{extension}";
        var fullPath = Path.Combine(_uploadDirectory, storedFileName);

        try
        {
            using (var stream = new FileStream(fullPath, FileMode.Create, FileAccess.Write))
            {
                await file.CopyToAsync(stream);
            }

            return (true, storedFileName, fullPath, string.Empty);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to persist document to disk.");
            return (false, string.Empty, string.Empty, "An internal error occurred while saving the file.");
        }
    }

    public async Task<(byte[] Bytes, string ContentType, string DownloadName)?> GetFileAsync(string storedFileName, string originalFileName, string contentType)
    {
        var fullPath = Path.Combine(_uploadDirectory, storedFileName);
        if (!File.Exists(fullPath))
        {
            _logger.LogWarning("Requested document file not found at: {Path}", fullPath);
            return null;
        }

        var bytes = await File.ReadAllBytesAsync(fullPath);
        return (bytes, contentType, originalFileName);
    }

    public bool DeleteFile(string storedFileName)
    {
        try
        {
            var fullPath = Path.Combine(_uploadDirectory, storedFileName);
            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return true;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to delete file: {FileName}", storedFileName);
        }
        return false;
    }
}
