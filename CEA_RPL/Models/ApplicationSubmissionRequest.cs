using Microsoft.AspNetCore.Http;

namespace CEA_RPL.Models;

public class ApplicationSubmissionRequest
{
    // Applicant Details
    public string full_name { get; set; } = string.Empty;
    public string? parent_name { get; set; }
    public string dob { get; set; } = string.Empty;
    public string gender { get; set; } = string.Empty;
    public string citizenship { get; set; } = string.Empty;
    public IFormFile applicant_photo { get; set; } = null!;
    public string address_perm { get; set; } = string.Empty;
    public string? address_corr { get; set; }
    public string email { get; set; } = string.Empty;
    public string mobile { get; set; } = string.Empty;
    public string? alt_mobile { get; set; }
    
    // IDs
    public string gov_id_type { get; set; } = string.Empty;
    public string gov_id_number { get; set; } = string.Empty;
    public IFormFile gov_id_upload { get; set; } = null!;
    
    // Selected Categories
    public List<string>? cert_category { get; set; }
    
    // Education Arrays
    public List<string>? edu_degree { get; set; }
    public List<string>? edu_discipline { get; set; }
    public List<string>? edu_institution { get; set; }
    public List<int>? edu_year { get; set; }
    public List<IFormFile>? edu_cert { get; set; }

    // Professional Experience
    public string total_experience { get; set; } = string.Empty;
    public List<string>? exp_org { get; set; }
    public List<string>? exp_designation { get; set; }
    public List<string>? exp_duration { get; set; }
    public List<string>? exp_nature { get; set; }
    public List<string>? ref2_phone { get; set; }
    public List<IFormFile>? exp_proof { get; set; }
    
    // Project Experience Cat 1
    public List<string>? project_name_cat1 { get; set; }
    public List<string>? project_client_cat1 { get; set; }
    public List<string>? project_location_cat1 { get; set; }
    public List<int>? project_year_cat1 { get; set; }
    public List<string>? project_role_cat1 { get; set; }
}
