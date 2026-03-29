namespace CEA_RPL.Domain.Entities;

public class User : BaseEntity
{
    public string Email { get; set; } = string.Empty;
    public string Mobile { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public bool IsEmailVerified { get; set; }
    public bool IsMobileVerified { get; set; }
    
    // One to one mapping with Applicant application
    public virtual Applicant? Applicant { get; set; }
}
