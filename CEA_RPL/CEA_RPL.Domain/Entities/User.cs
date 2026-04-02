namespace CEA_RPL.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string? MiddleName { get; set; }
    public string? LastName { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsMobileVerified { get; set; }
    public string Role { get; set; } = "Candidate";
    
    // One to one mapping with Applicant application
    public virtual Applicant? Applicant { get; set; }
}
