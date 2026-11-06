using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Infrastructure.Services;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthInsuranceClaims.Tests.SecurityTests;

public class SecurityAndFileValidationTests
{
    private readonly Mock<IWebHostEnvironment> _envMock;
    private readonly Mock<ILogger<PhysicalFileStorageService>> _loggerMock;
    private readonly IConfiguration _config;

    public SecurityAndFileValidationTests()
    {
        _envMock = new Mock<IWebHostEnvironment>();
        _envMock.Setup(e => e.ContentRootPath).Returns(AppContext.BaseDirectory);

        _loggerMock = new Mock<ILogger<PhysicalFileStorageService>>();

        var inMemorySettings = new Dictionary<string, string?>
        {
            {"FileStorage:UploadDirectory", "TempTestUploads"},
            {"FileStorage:MaxFileSizeInBytes", "10485760"}, // 10MB
            {"FileStorage:AllowedExtensions:0", ".pdf"},
            {"FileStorage:AllowedExtensions:1", ".jpg"},
            {"FileStorage:AllowedExtensions:2", ".jpeg"},
            {"FileStorage:AllowedExtensions:3", ".png"}
        };

        _config = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();
    }

    [Theory]
    [InlineData("script.exe")]
    [InlineData("payload.bat")]
    [InlineData("virus.js")]
    [InlineData("data.zip")]
    [InlineData("document.docx")]
    public async Task SaveFileAsync_WhenExtensionNotWhitelisted_RejectsUpload(string fileName)
    {
        // Arrange
        var service = new PhysicalFileStorageService(_envMock.Object, _config, _loggerMock.Object);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns(fileName);
        fileMock.Setup(f => f.Length).Returns(1024); // 1KB

        // Act
        var result = await service.SaveFileAsync(fileMock.Object);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("not permitted", result.ErrorMessage);
    }

    [Fact]
    public async Task SaveFileAsync_WhenFileExceedsMaxPermissibleSize_RejectsUpload()
    {
        // Arrange
        var service = new PhysicalFileStorageService(_envMock.Object, _config, _loggerMock.Object);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("large_scan.pdf");
        fileMock.Setup(f => f.Length).Returns(15 * 1024 * 1024); // 15MB (> 10MB limit)

        // Act
        var result = await service.SaveFileAsync(fileMock.Object);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("exceeds the maximum permissible size", result.ErrorMessage);
    }

    [Fact]
    public async Task SaveFileAsync_WhenFileEmpty_RejectsUpload()
    {
        // Arrange
        var service = new PhysicalFileStorageService(_envMock.Object, _config, _loggerMock.Object);
        var fileMock = new Mock<IFormFile>();
        fileMock.Setup(f => f.FileName).Returns("empty.pdf");
        fileMock.Setup(f => f.Length).Returns(0);

        // Act
        var result = await service.SaveFileAsync(fileMock.Object);

        // Assert
        Assert.False(result.Success);
        Assert.Equal("File is empty or not provided.", result.ErrorMessage);
    }
}
