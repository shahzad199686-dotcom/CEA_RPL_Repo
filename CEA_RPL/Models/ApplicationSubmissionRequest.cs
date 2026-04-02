using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace CEA_RPL.Models;

public class ApplicationSubmissionRequest
{
    // Applicant Details
    public string full_name { get; set; } = string.Empty;
    public string parent_relation { get; set; } = string.Empty;
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
    public List<string>? ref2_name { get; set; } // Added Ref name
    public List<string>? ref2_phone { get; set; }
    public List<IFormFile>? exp_proof { get; set; }
    
    // Project Experience Cat 1
    public List<string>? project_name_cat1 { get; set; }
    public List<string>? project_client_cat1 { get; set; }
    public List<string>? project_location_cat1 { get; set; }
    public List<int>? project_year_cat1 { get; set; }
    public List<string>? project_role_cat1 { get; set; }

    // Section 6: Audit Reports
    public List<IFormFile>? audit_report { get; set; }

    // Section 8: Training
    public List<string>? training_name { get; set; }
    public List<string>? training_from { get; set; }
    public List<string>? training_duration { get; set; }
    public List<int>? training_year { get; set; }
    public List<IFormFile>? training_proof { get; set; }

    // Section 9: Memberships
    public List<string>? membership_name { get; set; }
    public List<string>? membership_from { get; set; }
    public List<int>? membership_year { get; set; }
    public List<string>? membership_duration { get; set; }
    public List<IFormFile>? membership_proof { get; set; }

    // Section 10: Papers
    public List<string>? paper_name { get; set; }
    public List<string>? paper_place { get; set; }
    public List<int>? paper_year { get; set; }
    public List<string>? paper_role { get; set; }
    public List<IFormFile>? paper_proof { get; set; }

    // Section 11: Awards
    public List<string>? award_name { get; set; }
    public List<string>? award_from { get; set; }
    public List<int>? award_year { get; set; }
    public List<IFormFile>? award_proof { get; set; }

    // Section 12: Software Skills
    public List<string>? software_skill { get; set; }
    public List<string>? proficiency_level { get; set; }

    // Section 13: Other Enclosure
    public IFormFile? other_enclosure { get; set; }

    // Section 13a: Payment Details
    public decimal payment_amount { get; set; }
    public string payment_date { get; set; } = string.Empty;
    public string payment_utr { get; set; } = string.Empty;
    public IFormFile payment_receipt { get; set; } = null!;

    // Section 14: Declaration & Signature
    public string decl_name { get; set; } = string.Empty;
    public string decl_date { get; set; } = string.Empty;
    public string? decl_place { get; set; }
    public List<IFormFile>? Any_other_doc { get; set; } // This is signature
}
