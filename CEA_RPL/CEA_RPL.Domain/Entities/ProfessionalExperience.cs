namespace CEA_RPL.Domain.Entities;

public class ProfessionalExperience : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;
    
    public string Organization { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Duration { get; set; } = string.Empty; // e.g., "01/2020 - 05/2025"
    public string NatureOfWork { get; set; } = string.Empty;
    public string ReferencePhone { get; set; } = string.Empty;
    
    public string? ProofPath { get; set; }
}
