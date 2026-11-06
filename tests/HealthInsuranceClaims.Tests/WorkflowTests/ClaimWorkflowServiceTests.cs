using HealthInsuranceClaims.Application.Interfaces;
using HealthInsuranceClaims.Application.Services;
using HealthInsuranceClaims.Domain.Entities;
using HealthInsuranceClaims.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace HealthInsuranceClaims.Tests.WorkflowTests;

public class ClaimWorkflowServiceTests
{
    private readonly Mock<IClaimVerificationService> _verificationServiceMock;
    private readonly Mock<IAuditService> _auditServiceMock;
    private readonly Mock<ILogger<ClaimWorkflowService>> _loggerMock;

    public ClaimWorkflowServiceTests()
    {
        _verificationServiceMock = new Mock<IClaimVerificationService>();
        _auditServiceMock = new Mock<IAuditService>();
        _loggerMock = new Mock<ILogger<ClaimWorkflowService>>();
    }

    [Theory]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Submitted, true)]
    [InlineData(ClaimStatus.Submitted, ClaimStatus.UnderReview, true)]
    [InlineData(ClaimStatus.UnderReview, ClaimStatus.InformationRequested, true)]
    [InlineData(ClaimStatus.InformationRequested, ClaimStatus.Resubmitted, true)]
    [InlineData(ClaimStatus.Resubmitted, ClaimStatus.UnderReview, true)]
    [InlineData(ClaimStatus.UnderReview, ClaimStatus.Approved, true)]
    [InlineData(ClaimStatus.UnderReview, ClaimStatus.PartiallyApproved, true)]
    [InlineData(ClaimStatus.UnderReview, ClaimStatus.Rejected, true)]
    [InlineData(ClaimStatus.Approved, ClaimStatus.Closed, true)]
    [InlineData(ClaimStatus.Draft, ClaimStatus.Approved, false)] // Illegal jump
    [InlineData(ClaimStatus.Draft, ClaimStatus.UnderReview, false)] // Illegal jump
    [InlineData(ClaimStatus.Rejected, ClaimStatus.UnderReview, false)] // Illegal reopening
    [InlineData(ClaimStatus.Closed, ClaimStatus.Draft, false)] // Illegal reset
    public void CanTransition_ValidatesPermittedStatusTransitions(ClaimStatus current, ClaimStatus next, bool expected)
    {
        // Arrange
        var workflowService = new ClaimWorkflowService(null!, _verificationServiceMock.Object, _auditServiceMock.Object, _loggerMock.Object);

        // Act
        var result = workflowService.CanTransition(current, next);

        // Assert
        Assert.Equal(expected, result);
    }
}
