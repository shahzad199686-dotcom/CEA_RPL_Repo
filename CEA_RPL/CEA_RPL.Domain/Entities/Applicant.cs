namespace CEA_RPL.Domain.Entities;

public class Applicant : BaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? ParentName { get; set; }
    public DateTime DateOfBirth { get; set; }
    public string Gender { get; set; } = string.Empty;
    public string Citizenship { get; set; } = "Indian";
    public string PermanentAddress { get; set; } = string.Empty;
    public string? CorrespondenceAddress { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string? AlternateMobile { get; set; }
    // Relationships
    public int UserId { get; set; }
    public virtual User User { get; set; } = null!;
    
    // Status
    public string Status { get; set; } = "Draft"; // Draft, Submitted, UnderReview, Approved, Rejected
    
    // Photo & IDs
    public string? PhotoPath { get; set; }
    public string GovIdType { get; set; } = string.Empty;
    public string GovIdNumber { get; set; } = string.Empty;
    public string? GovIdPath { get; set; }
    
    // Selected Categories
    public string Categories { get; set; } = string.Empty; // Comma separated Category 1, Category 2 etc.
    
    // Additional Documents
    public string? ReportPath1 { get; set; }
    public string? ReportPath2 { get; set; }
    public string? OtherEnclosurePath { get; set; }
    
    // Payment Details
    public decimal PaymentAmount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string? PaymentUtr { get; set; }
    public string? PaymentReceiptPath { get; set; }
    
    // Declaration
    public string? DeclarationName { get; set; }
    public DateTime DeclarationDate { get; set; }
    public string? DeclarationPlace { get; set; }
    public string? SignaturePath { get; set; }
    
    // Navigation Collections
    public virtual ICollection<Education> Educations { get; set; } = new List<Education>();
    public virtual ICollection<ProfessionalExperience> ProfessionalExperiences { get; set; } = new List<ProfessionalExperience>();
    public virtual ICollection<ProjectExperience> ProjectExperiences { get; set; } = new List<ProjectExperience>();
    public virtual ICollection<Award> Awards { get; set; } = new List<Award>();
    public virtual ICollection<CertificationTraining> CertificationTrainings { get; set; } = new List<CertificationTraining>();
    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public virtual ICollection<Publication> Publications { get; set; } = new List<Publication>();
    public virtual ICollection<SoftwareSkill> SoftwareSkills { get; set; } = new List<SoftwareSkill>();
}
