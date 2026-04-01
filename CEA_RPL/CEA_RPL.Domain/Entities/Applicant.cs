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
    
    // Navigation Properties
    public virtual Declaration? Declaration { get; set; } = null!;
    public virtual ICollection<PaymentDetail> PaymentDetails { get; set; } = new List<PaymentDetail>();
    public virtual ICollection<UploadReport> UploadReports { get; set; } = new List<UploadReport>();
    public virtual ICollection<OtherEnclosure> OtherEnclosures { get; set; } = new List<OtherEnclosure>();
    
    // Navigation Collections
    public virtual ICollection<Education> Educations { get; set; } = new List<Education>();
    public virtual ICollection<ProfessionalExperience> ProfessionalExperiences { get; set; } = new List<ProfessionalExperience>();
    public virtual ICollection<ProjectExperience> ProjectExperiences { get; set; } = new List<ProjectExperience>();
    public virtual ICollection<Award> Awards { get; set; } = new List<Award>();
    public virtual ICollection<CertificationTraining> CertificationTrainings { get; set; } = new List<CertificationTraining>();
    public virtual ICollection<Membership> Memberships { get; set; } = new List<Membership>();
    public virtual ICollection<PaperPublished> PaperPublisheds { get; set; } = new List<PaperPublished>();
    public virtual ICollection<SoftwareSkill> SoftwareSkills { get; set; } = new List<SoftwareSkill>();
}
