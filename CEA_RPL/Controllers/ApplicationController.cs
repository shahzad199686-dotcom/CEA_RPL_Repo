using System.Security.Claims;
using CEA_RPL.Models;
using CEA_RPL.Application.Interfaces;
using CEA_RPL.Domain.Entities;
using CEA_RPL.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CEA_RPL.Controllers;

public class ApplicationController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly IFileService _fileService;

    public ApplicationController(ApplicationDbContext context, IFileService fileService)
    {
        _context = context;
        _fileService = fileService;
    }

    [Authorize]
    public async Task<IActionResult> Index()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (int.TryParse(userIdString, out var userId))
        {
            var user = await _context.Users
                .Include(u => u.Applicant)
                .FirstOrDefaultAsync(u => u.Id == userId);

            if (user != null)
            {
                ViewBag.UserName = user.Applicant?.FullName ?? user.Email;
                ViewBag.ApplicationStatus = user.Applicant?.Status;
                ViewBag.AdminFeedback = user.Applicant?.AdminFeedback;
                ViewBag.LastSavedAt = user.Applicant?.LastSavedAt?.ToString("f");
                ViewBag.UserEmail = user.Email;
                ViewBag.UserMobile = user.Mobile;
            }
        }
        
        return View();
    }

    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> Submit([FromForm] ApplicationSubmissionRequest req)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();
            
        // Enforce OTP verification
        var user = await _context.Users.FindAsync(userId);
        if (user == null || !user.IsEmailVerified || !user.IsMobileVerified)
            return BadRequest(new { message = "You must verify your email and mobile OTPs before submitting." });

        // Check if user already submitted an application (Drafts are allowed)
        var existingApplicant = await _context.Applicants
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (existingApplicant != null && existingApplicant.Status != "Draft")
        {
            return BadRequest(new { message = "You have already submitted an application." });
        }

        // --- FILE VALIDATION ---
        string fileError;
        if (req.applicant_photo == null) return BadRequest(new { message = "Applicant photograph is required." });
        if (!IsFileValid(req.applicant_photo, out fileError)) return BadRequest(new { message = fileError });

        if (req.gov_id_upload == null) return BadRequest(new { message = "Government ID proof is required." });
        if (!IsFileValid(req.gov_id_upload, out fileError)) return BadRequest(new { message = fileError });
        if (req.other_enclosure != null && !IsFileValid(req.other_enclosure, out fileError)) return BadRequest(new { message = fileError });
        if (req.payment_receipt != null && !IsFileValid(req.payment_receipt, out fileError)) return BadRequest(new { message = fileError });
        if (req.Any_other_doc != null && req.Any_other_doc.Count > 0 && req.Any_other_doc[0] != null && !IsFileValid(req.Any_other_doc[0], out fileError)) return BadRequest(new { message = fileError });

        if (req.edu_cert != null) foreach (var f in req.edu_cert) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.exp_proof != null) foreach (var f in req.exp_proof) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.audit_report != null) foreach (var f in req.audit_report) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.training_proof != null) foreach (var f in req.training_proof) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.membership_proof != null) foreach (var f in req.membership_proof) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.paper_proof != null) foreach (var f in req.paper_proof) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        // --- END FILE VALIDATION ---


        string? report1 = null;
        string? report2 = null;
        if (req.audit_report != null)
        {
            if (req.audit_report.Count > 0 && req.audit_report[0] != null)
                report1 = await _fileService.SaveFileAsync(req.audit_report[0].OpenReadStream(), req.audit_report[0].FileName);
            if (req.audit_report.Count > 1 && req.audit_report[1] != null)
                report2 = await _fileService.SaveFileAsync(req.audit_report[1].OpenReadStream(), req.audit_report[1].FileName);
        }

        string? otherEnc = null;
        if (req.other_enclosure != null)
            otherEnc = await _fileService.SaveFileAsync(req.other_enclosure.OpenReadStream(), req.other_enclosure.FileName);

        string? payReceipt = null;
        if (req.payment_receipt != null)
            payReceipt = await _fileService.SaveFileAsync(req.payment_receipt.OpenReadStream(), req.payment_receipt.FileName);

        string? signature = null;
        if (req.Any_other_doc != null && req.Any_other_doc.Count > 0 && req.Any_other_doc[0] != null)
            signature = await _fileService.SaveFileAsync(req.Any_other_doc[0].OpenReadStream(), req.Any_other_doc[0].FileName);

        var photoPath = await _fileService.SaveFileAsync(req.applicant_photo!.OpenReadStream(), req.applicant_photo.FileName);
        var govIdPath = await _fileService.SaveFileAsync(req.gov_id_upload!.OpenReadStream(), req.gov_id_upload.FileName);

        // --- MAPPING LOGIC (Symmetric for Submit & Draft) ---
        var applicant = existingApplicant ?? new Applicant { UserId = userId };
        if (existingApplicant == null) _context.Applicants.Add(applicant);

        applicant.Status = "Submitted";
        applicant.CurrentStep = 15;
        
        applicant.FullName = req.full_name ?? "";
        applicant.ParentRelation = req.parent_relation;
        applicant.ParentName = req.parent_name;
        applicant.DateOfBirth = DateTime.TryParse(req.dob, out var dob) ? dob : DateTime.Now;
        applicant.Gender = string.IsNullOrWhiteSpace(req.gender) ? "Other" : req.gender;
        applicant.Citizenship = string.IsNullOrWhiteSpace(req.citizenship) ? "Indian" : req.citizenship;
        applicant.PermanentAddress = req.address_perm ?? "";
        applicant.CorrespondenceAddress = req.address_corr;
        applicant.Email = req.email ?? "";
        applicant.Mobile = req.mobile ?? "";
        applicant.AlternateMobile = req.alt_mobile;
        applicant.PhotoPath = photoPath;
        applicant.GovIdType = req.gov_id_type ?? "";
        applicant.GovIdNumber = req.gov_id_number ?? "";
        applicant.GovIdPath = govIdPath;
        applicant.Categories = req.cert_category != null ? string.Join(", ", req.cert_category) : string.Empty;

        // Section 6: Audit Reports
        if (report1 != null) applicant.UploadReports.Add(new UploadReport { FilePath = report1, FileName = req.audit_report?[0].FileName ?? "Report1" });
        if (report2 != null) applicant.UploadReports.Add(new UploadReport { FilePath = report2, FileName = req.audit_report?[1].FileName ?? "Report2" });

        // Section 12: Other Enclosures
        if (otherEnc != null) applicant.OtherEnclosures.Add(new OtherEnclosure { FilePath = otherEnc, FileName = req.other_enclosure?.FileName ?? "AdditionalDoc" });

        // --- PAYMENT & UTR VERIFICATION ---
        // 1. Check for Duplicate UTR
        if (!string.IsNullOrEmpty(req.payment_utr))
        {
            var existingPayment = await _context.PaymentDetails
                .AnyAsync(p => p.UtrNumber == req.payment_utr);
            if (existingPayment)
            {
                return BadRequest(new { message = "This UTR Number has already been used for another application. Duplicate payments are not allowed." });
            }
        }

        // 2. Verify Amount matches categories (₹5,000 per category)
        int categoryCount = req.cert_category?.Count ?? 0;
        decimal expectedAmount = categoryCount * 5000;
        if (req.payment_amount < expectedAmount)
        {
            return BadRequest(new { message = $"Insufficient payment. Total fee for {categoryCount} categories is ₹{expectedAmount}. Please pay the full amount." });
        }

        // Section 13: Payment Details
        applicant.PaymentDetails.Add(new PaymentDetail
        {
            Amount = req.payment_amount,
            PaymentDate = DateTime.TryParse(req.payment_date, out var pd) ? pd : DateTime.Now,
            UtrNumber = req.payment_utr ?? "",
            ReceiptPath = payReceipt
        });

        // Section 14: Declaration & Signature
        applicant.Declaration = new Declaration
        {
            Name = req.decl_name ?? "",
            Date = DateTime.TryParse(req.decl_date, out var dd) ? dd : DateTime.Now,
            Place = req.decl_place,
            SignaturePath = signature
        };

        // 1) Educations
        if (req.edu_degree != null && req.edu_cert != null)
        {
            for (int i = 0; i < req.edu_degree.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.edu_degree[i])) continue;
                
                string? certPath = null;
                if (i < req.edu_cert.Count && req.edu_cert[i] != null)
                    certPath = await _fileService.SaveFileAsync(req.edu_cert[i].OpenReadStream(), req.edu_cert[i].FileName);

                applicant.Educations.Add(new Education
                {
                    Degree = req.edu_degree[i] ?? "",
                    Discipline = (req.edu_discipline != null && req.edu_discipline.Count > i ? req.edu_discipline[i] : "") ?? "",
                    Institution = (req.edu_institution != null && req.edu_institution.Count > i ? req.edu_institution[i] : "") ?? "",
                    Year = (req.edu_year != null && req.edu_year.Count > i ? req.edu_year[i] : 0),
                    CertificatePath = certPath
                });
            }
        }

        // 2) Professional Experience
        if (req.exp_org != null && req.exp_proof != null)
        {
            for (int i = 0; i < req.exp_org.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.exp_org[i])) continue;
                
                string? proofPath = null;
                if (i < req.exp_proof.Count && req.exp_proof[i] != null)
                    proofPath = await _fileService.SaveFileAsync(req.exp_proof[i].OpenReadStream(), req.exp_proof[i].FileName);

                applicant.ProfessionalExperiences.Add(new ProfessionalExperience
                {
                    Organization = req.exp_org[i] ?? "",
                    Designation = (req.exp_designation != null && req.exp_designation.Count > i ? req.exp_designation[i] : "") ?? "",
                    Duration = (req.exp_duration != null && req.exp_duration.Count > i ? req.exp_duration[i] : "") ?? "",
                    NatureOfWork = (req.exp_nature != null && req.exp_nature.Count > i ? req.exp_nature[i] : "") ?? "",
                    ReferencePhone = (req.ref2_phone != null && req.ref2_phone.Count > i ? req.ref2_phone[i] : "") ?? "",
                    ProofPath = proofPath
                });
            }
        }

        // 3) Projects Category 1
        if (req.project_name_cat1 != null)
        {
            for (int i = 0; i < req.project_name_cat1.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.project_name_cat1[i])) continue;
                applicant.ProjectExperiences.Add(new ProjectExperience
                {
                    Category = "Category 1",
                    Name = req.project_name_cat1[i] ?? "",
                    Client = (req.project_client_cat1 != null && req.project_client_cat1.Count > i ? req.project_client_cat1[i] : "") ?? "",
                    Location = (req.project_location_cat1 != null && req.project_location_cat1.Count > i ? req.project_location_cat1[i] : "") ?? "",
                    Year = (req.project_year_cat1 != null && req.project_year_cat1.Count > i ? req.project_year_cat1[i] : 0),
                    Role = (req.project_role_cat1 != null && req.project_role_cat1.Count > i ? req.project_role_cat1[i] : "") ?? ""
                });
            }
        }

        // 4) Training
        if (req.training_name != null)
        {
            for (int i = 0; i < req.training_name.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.training_name[i])) continue;
                string? proof = null;
                if (req.training_proof != null && i < req.training_proof.Count && req.training_proof[i] != null)
                    proof = await _fileService.SaveFileAsync(req.training_proof[i].OpenReadStream(), req.training_proof[i].FileName);

                applicant.CertificationTrainings.Add(new CertificationTraining
                {
                    Name = req.training_name[i] ?? "",
                    ObtainedFrom = (req.training_from != null && req.training_from.Count > i ? req.training_from[i] : "") ?? "",
                    Duration = (req.training_duration != null && req.training_duration.Count > i ? req.training_duration[i] : "") ?? "",
                    Year = (req.training_year != null && req.training_year.Count > i ? req.training_year[i] : 0),
                    ProofPath = proof
                });
            }
        }

        // 5) Memberships
        if (req.membership_name != null)
        {
            for (int i = 0; i < req.membership_name.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.membership_name[i])) continue;
                string? proof = null;
                if (req.membership_proof != null && i < req.membership_proof.Count && req.membership_proof[i] != null)
                    proof = await _fileService.SaveFileAsync(req.membership_proof[i].OpenReadStream(), req.membership_proof[i].FileName);

                applicant.Memberships.Add(new Membership
                {
                    Name = req.membership_name[i] ?? "",
                    ObtainedFrom = (req.membership_from != null && req.membership_from.Count > i ? req.membership_from[i] : "") ?? "",
                    Year = (req.membership_year != null && req.membership_year.Count > i ? req.membership_year[i] : 0),
                    Duration = (req.membership_duration != null && req.membership_duration.Count > i ? req.membership_duration[i] : "") ?? "",
                    ProofPath = proof
                });
            }
        }

        // 6) Publications
        if (req.paper_name != null)
        {
            for (int i = 0; i < (req.paper_name?.Count ?? 0); i++)
            {
                if (string.IsNullOrWhiteSpace(req.paper_name?[i])) continue;
                string? proof = null;
                if (req.paper_proof != null && i < req.paper_proof.Count && req.paper_proof[i] != null)
                    proof = await _fileService.SaveFileAsync(req.paper_proof[i].OpenReadStream(), req.paper_proof[i].FileName);

                applicant.PaperPublisheds.Add(new PaperPublished
                {
                    Name = req.paper_name[i] ?? "",
                    Place = (req.paper_place != null && req.paper_place.Count > i ? req.paper_place[i] : "") ?? "",
                    Year = (req.paper_year != null && req.paper_year.Count > i ? req.paper_year[i] : 0),
                    Role = (req.paper_role != null && req.paper_role.Count > i ? req.paper_role[i] : "") ?? "",
                    ProofPath = proof
                });
            }
        }

        // 7) Awards
        if (req.award_name != null)
        {
            for (int i = 0; i < req.award_name.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.award_name[i])) continue;
                string? proof = null;
                if (req.award_proof != null && i < req.award_proof.Count && req.award_proof[i] != null)
                    proof = await _fileService.SaveFileAsync(req.award_proof[i].OpenReadStream(), req.award_proof[i].FileName);

                applicant.Awards.Add(new Award
                {
                    Name = req.award_name[i] ?? "",
                    ReceivedFrom = (req.award_from != null && req.award_from.Count > i ? req.award_from[i] : "") ?? "",
                    Year = (req.award_year != null && req.award_year.Count > i ? req.award_year[i] : 0),
                    ProofPath = proof
                });
            }
        }

        // 8) Software Skills
        if (req.software_skill != null)
        {
            for (int i = 0; i < req.software_skill.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.software_skill[i])) continue;
                applicant.SoftwareSkills.Add(new SoftwareSkill
                {
                    SoftwareName = req.software_skill[i] ?? "",
                    ProficiencyLevel = (req.proficiency_level != null && req.proficiency_level.Count > i ? req.proficiency_level[i] : "") ?? ""
                });
            }
        }
        
        await _context.SaveChangesAsync();
        return Ok(new { message = "Draft saved successfully", savedAt = DateTime.Now.ToString("h:mm tt") });
    }

    [HttpPost("save-draft")]
    [Authorize]
    public async Task<IActionResult> SaveDraft([FromForm] ApplicationSubmissionRequest req)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();

        var applicant = await _context.Applicants
            .Include(a => a.Educations)
            .Include(a => a.SoftwareSkills)
            .Include(a => a.ProfessionalExperiences)
            .Include(a => a.ProjectExperiences)
            .Include(a => a.Awards)
            .Include(a => a.CertificationTrainings)
            .Include(a => a.Memberships)
            .Include(a => a.PaperPublisheds)
            .Include(a => a.UploadReports)
            .Include(a => a.OtherEnclosures)
            .Include(a => a.PaymentDetails)
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (applicant != null && applicant.Status != "Draft")
            return BadRequest(new { message = "You have already submitted an application." });

        // --- FILE VALIDATION ---
        string fileError;
        if (req.applicant_photo != null && !IsFileValid(req.applicant_photo, out fileError)) return BadRequest(new { message = fileError });
        if (req.gov_id_upload != null && !IsFileValid(req.gov_id_upload, out fileError)) return BadRequest(new { message = fileError });
        if (req.other_enclosure != null && !IsFileValid(req.other_enclosure, out fileError)) return BadRequest(new { message = fileError });
        if (req.payment_receipt != null && !IsFileValid(req.payment_receipt, out fileError)) return BadRequest(new { message = fileError });
        if (req.Any_other_doc != null && req.Any_other_doc.Count > 0 && req.Any_other_doc[0] != null && !IsFileValid(req.Any_other_doc[0], out fileError)) return BadRequest(new { message = fileError });

        if (req.edu_cert != null) foreach (var f in req.edu_cert) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.exp_proof != null) foreach (var f in req.exp_proof) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.audit_report != null) foreach (var f in req.audit_report) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.training_proof != null) foreach (var f in req.training_proof) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.membership_proof != null) foreach (var f in req.membership_proof) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        if (req.paper_proof != null) foreach (var f in req.paper_proof) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
        // --- END FILE VALIDATION ---

        if (applicant == null)
        {
            applicant = new Applicant { UserId = userId };
            _context.Applicants.Add(applicant);
        }

        applicant.Status = "Draft";
        applicant.CurrentStep = req.current_step ?? 1;
        applicant.LastSavedAt = DateTime.Now;

        // Map Basic Info
        applicant.FullName = req.full_name ?? "";
        applicant.ParentRelation = req.parent_relation;
        applicant.ParentName = req.parent_name;
        applicant.DateOfBirth = DateTime.TryParse(req.dob, out var dob) ? dob : DateTime.Now;
        applicant.Gender = req.gender ?? "";
        applicant.Citizenship = req.citizenship ?? "Indian";
        applicant.PermanentAddress = req.address_perm ?? "";
        applicant.CorrespondenceAddress = req.address_corr;
        applicant.Email = req.email ?? "";
        applicant.Mobile = req.mobile ?? "";
        applicant.AlternateMobile = req.alt_mobile;
        applicant.GovIdType = req.gov_id_type ?? "";
        applicant.GovIdNumber = req.gov_id_number ?? "";
        applicant.Categories = req.cert_category != null ? string.Join(", ", req.cert_category) : string.Empty;

        // Map Files (Optional in Draft)
        if (req.applicant_photo != null)
            applicant.PhotoPath = await _fileService.SaveFileAsync(req.applicant_photo.OpenReadStream(), req.applicant_photo.FileName);
        if (req.gov_id_upload != null)
            applicant.GovIdPath = await _fileService.SaveFileAsync(req.gov_id_upload.OpenReadStream(), req.gov_id_upload.FileName);

        // Map Collections (Education)
        if (req.edu_degree != null)
        {
            foreach (var e in applicant.Educations.ToList()) _context.Educations.Remove(e);
            for (int i = 0; i < req.edu_degree.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.edu_degree[i])) continue;
                applicant.Educations.Add(new Education { 
                    Degree = req.edu_degree[i] ?? "", 
                    Discipline = (req.edu_discipline != null && req.edu_discipline.Count > i ? req.edu_discipline[i] : "") ?? "", 
                    Institution = (req.edu_institution != null && req.edu_institution.Count > i ? req.edu_institution[i] : "") ?? "", 
                    Year = (req.edu_year != null && req.edu_year.Count > i ? req.edu_year[i] : 0) 
                });
            }
        }

        // Map Collections (Experience)
        if (req.exp_org != null)
        {
            foreach (var e in applicant.ProfessionalExperiences.ToList()) _context.ProfessionalExperiences.Remove(e);
            for (int i = 0; i < req.exp_org.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.exp_org[i])) continue;
                applicant.ProfessionalExperiences.Add(new ProfessionalExperience { 
                    Organization = req.exp_org[i] ?? "", 
                    Designation = (req.exp_designation != null && req.exp_designation.Count > i ? req.exp_designation[i] : "") ?? "", 
                    Duration = (req.exp_duration != null && req.exp_duration.Count > i ? req.exp_duration[i] : "") ?? "", 
                    NatureOfWork = (req.exp_nature != null && req.exp_nature.Count > i ? req.exp_nature[i] : "") ?? "" 
                });
            }
        }

        // Map Collections (Projects)
        if (req.project_name_cat1 != null)
        {
            foreach (var p in applicant.ProjectExperiences.ToList()) _context.ProjectExperiences.Remove(p);
            for (int i = 0; i < req.project_name_cat1.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.project_name_cat1[i])) continue;
                applicant.ProjectExperiences.Add(new ProjectExperience { 
                    Category = "Category 1",
                    Name = req.project_name_cat1[i] ?? "", 
                    Client = (req.project_client_cat1 != null && req.project_client_cat1.Count > i ? req.project_client_cat1[i] : "") ?? "", 
                    Location = (req.project_location_cat1 != null && req.project_location_cat1.Count > i ? req.project_location_cat1[i] : "") ?? "", 
                    Year = (req.project_year_cat1 != null && req.project_year_cat1.Count > i ? req.project_year_cat1[i] : 0), 
                    Role = (req.project_role_cat1 != null && req.project_role_cat1.Count > i ? req.project_role_cat1[i] : "") ?? "" 
                });
            }
        }

        // Map Collections (Training)
        if (req.training_name != null)
        {
            foreach (var t in applicant.CertificationTrainings.ToList()) _context.CertificationTrainings.Remove(t);
            for (int i = 0; i < req.training_name.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.training_name[i])) continue;
                applicant.CertificationTrainings.Add(new CertificationTraining {
                    Name = req.training_name[i] ?? "",
                    ObtainedFrom = (req.training_from != null && req.training_from.Count > i ? req.training_from[i] : "") ?? "",
                    Duration = (req.training_duration != null && req.training_duration.Count > i ? req.training_duration[i] : "") ?? "",
                    Year = (req.training_year != null && req.training_year.Count > i ? req.training_year[i] : 0)
                });
            }
        }

        // Map Collections (Membership)
        if (req.membership_name != null)
        {
            foreach (var m in applicant.Memberships.ToList()) _context.Memberships.Remove(m);
            for (int i = 0; i < req.membership_name.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.membership_name[i])) continue;
                applicant.Memberships.Add(new Membership {
                    Name = req.membership_name[i] ?? "",
                    ObtainedFrom = (req.membership_from != null && req.membership_from.Count > i ? req.membership_from[i] : "") ?? "",
                    Year = (req.membership_year != null && req.membership_year.Count > i ? req.membership_year[i] : 0),
                    Duration = (req.membership_duration != null && req.membership_duration.Count > i ? req.membership_duration[i] : "") ?? ""
                });
            }
        }

        // Map Collections (Papers)
        if (req.paper_name != null)
        {
            foreach (var p in applicant.PaperPublisheds.ToList()) _context.PaperPublisheds.Remove(p);
            for (int i = 0; i < req.paper_name.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.paper_name[i])) continue;
                applicant.PaperPublisheds.Add(new PaperPublished {
                    Name = req.paper_name[i] ?? "",
                    Place = (req.paper_place != null && req.paper_place.Count > i ? req.paper_place[i] : "") ?? "",
                    Year = (req.paper_year != null && req.paper_year.Count > i ? req.paper_year[i] : 0),
                    Role = (req.paper_role != null && req.paper_role.Count > i ? req.paper_role[i] : "") ?? ""
                });
            }
        }

        // Map Collections (Awards)
        if (req.award_name != null)
        {
            foreach (var a in applicant.Awards.ToList()) _context.Awards.Remove(a);
            for (int i = 0; i < req.award_name.Count; i++)
            {
                if (string.IsNullOrWhiteSpace(req.award_name[i])) continue;
                applicant.Awards.Add(new Award {
                    Name = req.award_name[i] ?? "",
                    ReceivedFrom = (req.award_from != null && req.award_from.Count > i ? req.award_from[i] : "") ?? "",
                    Year = (req.award_year != null && req.award_year.Count > i ? req.award_year[i] : 0)
                });
            }
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "Draft saved successfully", savedAt = DateTime.Now.ToString("h:mm tt") });
    }

    [HttpGet("get-draft")]
    [Authorize]
    public async Task<IActionResult> GetDraft()
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();

        var applicant = await _context.Applicants
            .Include(a => a.Educations)
            .Include(a => a.SoftwareSkills)
            .Include(a => a.ProfessionalExperiences)
            .Include(a => a.ProjectExperiences)
            .Include(a => a.Awards)
            .Include(a => a.CertificationTrainings)
            .Include(a => a.Memberships)
            .Include(a => a.PaperPublisheds)
            .Include(a => a.PaymentDetails)
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (applicant == null) return NotFound();

        return Ok(new
        {
            current_step = applicant.CurrentStep,
            last_saved_at = applicant.LastSavedAt?.ToString("dd MMM yyyy, h:mm tt") ?? "Unknown",
            full_name = applicant.FullName,
            parent_relation = applicant.ParentRelation,
            parent_name = applicant.ParentName,
            email = applicant.Email,
            mobile = applicant.Mobile,
            alt_mobile = applicant.AlternateMobile,
            gender = applicant.Gender,
            dob = applicant.DateOfBirth.ToString("yyyy-MM-dd"),
            citizenship = applicant.Citizenship,
            address_perm = applicant.PermanentAddress,
            address_corr = applicant.CorrespondenceAddress,
            gov_id_type = applicant.GovIdType,
            gov_id_number = applicant.GovIdNumber,
            categories = applicant.Categories.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList(),
            educations = applicant.Educations.Select(e => new { 
                degree = e.Degree, 
                discipline = e.Discipline, 
                institution = e.Institution,
                year = e.Year 
            }).ToList(),
            experiences = applicant.ProfessionalExperiences.Select(ex => new {
                org = ex.Organization,
                designation = ex.Designation,
                duration = ex.Duration,
                nature = ex.NatureOfWork
            }).ToList(),
            projects = applicant.ProjectExperiences.Select(p => new {
                name = p.Name,
                client = p.Client,
                location = p.Location,
                year = p.Year,
                role = p.Role
            }).ToList(),
            trainings = applicant.CertificationTrainings.Select(t => new {
                name = t.Name,
                from = t.ObtainedFrom,
                duration = t.Duration,
                year = t.Year
            }).ToList(),
            memberships = applicant.Memberships.Select(m => new {
                name = m.Name,
                from = m.ObtainedFrom,
                year = m.Year,
                duration = m.Duration
            }).ToList(),
            papers = applicant.PaperPublisheds.Select(p => new {
                name = p.Name,
                place = p.Place,
                year = p.Year,
                role = p.Role
            }).ToList(),
            awards = applicant.Awards.Select(a => new {
                name = a.Name,
                from = a.ReceivedFrom,
                year = a.Year
            }).ToList(),
            software_skills = applicant.SoftwareSkills.Select(s => new {
                skill = s.SoftwareName,
                level = s.ProficiencyLevel
            }).ToList(),
            payment = applicant.PaymentDetails.OrderByDescending(p => p.Id).Select(p => new {
                amount = p.Amount,
                date = p.PaymentDate.ToString("yyyy-MM-dd"),
                utr = p.UtrNumber
            }).FirstOrDefault()
        });
    }

    private bool IsFileValid(IFormFile file, out string errorMessage)
    {
        errorMessage = "";
        if (file == null || file.Length == 0) return true;

        var fileName = file.FileName.ToLower();
        var isImage = fileName.EndsWith(".jpg") || fileName.EndsWith(".jpeg") || fileName.EndsWith(".png");
        var isPdf = fileName.EndsWith(".pdf");

        if (isImage)
        {
            if (file.Length > 2 * 1024 * 1024)
            {
                errorMessage = $"Image '{file.FileName}' exceeds 2MB limit.";
                return false;
            }
        }
        else if (isPdf)
        {
            if (file.Length > 10 * 1024 * 1024)
            {
                errorMessage = $"PDF '{file.FileName}' exceeds 10MB limit.";
                return false;
            }
        }
        else
        {
            // Default 10MB for other allowed types (like ZIP in other_enclosure)
            if (file.Length > 10 * 1024 * 1024)
            {
                errorMessage = $"File '{file.FileName}' exceeds 10MB limit.";
                return false;
            }
        }

        return true;
    }
}
