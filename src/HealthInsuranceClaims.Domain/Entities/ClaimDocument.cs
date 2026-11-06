using HealthInsuranceClaims.Domain.Enums;

namespace HealthInsuranceClaims.Domain.Entities;

public class ClaimDocument
{
    public int DocumentId { get; set; }
    public int ClaimId { get; set; }
    public DocumentType DocumentType { get; set; } = DocumentType.OtherSupportingDocument;
    public string OriginalFileName { get; set; } = string.Empty;
    public string StoredFileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string UploadedBy { get; set; } = string.Empty;
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;

    // Navigation property
    public virtual Claim? Claim { get; set; }
}
