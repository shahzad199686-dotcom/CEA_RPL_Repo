namespace CEA_RPL.Domain.Entities;

public class ProjectExperience : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;
    
    public string Category { get; set; } = string.Empty; // Category 1, Category 2, etc.
    public string Name { get; set; } = string.Empty;
    public string Client { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Role { get; set; } = string.Empty;
}
