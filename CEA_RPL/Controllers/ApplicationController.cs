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
    private readonly IWebHostEnvironment _env;

    public ApplicationController(ApplicationDbContext context, IFileService fileService, IWebHostEnvironment env)
    {
        _context = context;
        _fileService = fileService;
        _env = env;
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

                if (user.Applicant != null)
                {
                    ViewBag.SubmittedAt = user.Applicant.SubmittedAt?.ToString("dd MMM yyyy, h:mm tt");
                    ViewBag.AppId = $"CEA/RPL/2026/{user.Applicant.Id:D5}";
                    ViewBag.AppliedCategories = user.Applicant.Categories;
                    
                    // Simple count of main documents for dashboard display
                    int docCount = 0;
                    if (user.Applicant.PhotoPath != null) docCount++;
                    if (user.Applicant.GovIdPath != null) docCount++;
                    
                    var app = await _context.Applicants
                        .Include(a => a.UploadReports)
                        .Include(a => a.OtherEnclosures)
                        .Include(a => a.Educations)
                        .Include(a => a.ProfessionalExperiences)
                        .Include(a => a.CertificationTrainings)
                        .Include(a => a.Memberships)
                        .Include(a => a.PaperPublisheds)
                        .Include(a => a.Awards)
                        .Include(a => a.PaymentDetails)
                        .FirstOrDefaultAsync(a => a.Id == user.Applicant.Id);
                    
                    if (app != null)
                    {
                        docCount += app.UploadReports.Count;
                        docCount += app.OtherEnclosures.Count;
                        docCount += app.Educations.Count(e => !string.IsNullOrEmpty(e.CertificatePath));
                        docCount += app.ProfessionalExperiences.Count(e => !string.IsNullOrEmpty(e.ProofPath));
                        docCount += app.CertificationTrainings.Count(e => !string.IsNullOrEmpty(e.ProofPath));
                        docCount += app.Memberships.Count(e => !string.IsNullOrEmpty(e.ProofPath));
                        docCount += app.PaperPublisheds.Count(e => !string.IsNullOrEmpty(e.ProofPath));
                        docCount += app.Awards.Count(e => !string.IsNullOrEmpty(e.ProofPath));
                        
                        ViewBag.DocCount = docCount;
                        ViewBag.TotalAmount = app.PaymentDetails.Sum(p => p.Amount);
                    }
                }
            }
        }
        
        return View();
    }

    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> Submit([FromForm] ApplicationSubmissionRequest req)
    {
        return await ProcessApplication(req, isFinalSubmit: true);
    }

    [HttpPost("save-draft")]
    [Authorize]
    public async Task<IActionResult> SaveDraft([FromForm] ApplicationSubmissionRequest req)
    {
        return await ProcessApplication(req, isFinalSubmit: false);
    }

    private async Task<IActionResult> ProcessApplication(ApplicationSubmissionRequest req, bool isFinalSubmit)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var user = await _context.Users.FindAsync(userId);
            if (user == null) return Unauthorized();

            bool skipOtpCheck = _env.IsDevelopment();
            if (isFinalSubmit && !skipOtpCheck && (!user.IsEmailVerified || !user.IsMobileVerified))
            {
                return BadRequest(new { message = "You must verify your email and mobile OTPs before submitting." });
            }

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
                .Include(a => a.Declaration)
                .FirstOrDefaultAsync(a => a.UserId == userId);

            if (applicant != null && applicant.Status != "Draft")
                return BadRequest(new { message = "Your application has already been submitted." });

            if (applicant == null)
            {
                applicant = new Applicant { UserId = userId };
                _context.Applicants.Add(applicant);
            }

            // --- FILE VALIDATION ---
            string fileError;
            if (isFinalSubmit)
            {
                if (applicant.PhotoPath == null && req.applicant_photo == null) return BadRequest(new { message = "Applicant photograph is required." });
                if (applicant.GovIdPath == null && req.gov_id_upload == null) return BadRequest(new { message = "Government ID proof is required." });
                if (applicant.PaymentDetails.Count == 0 && req.payment_receipt == null) return BadRequest(new { message = "Payment receipt is required." });
                if (applicant.Declaration?.SignaturePath == null && req.signature_file == null) return BadRequest(new { message = "Signature is required." });
            }

            if (req.applicant_photo != null && !IsFileValid(req.applicant_photo, out fileError)) return BadRequest(new { message = fileError });
            if (req.gov_id_upload != null && !IsFileValid(req.gov_id_upload, out fileError)) return BadRequest(new { message = fileError });
            if (req.other_enclosure != null && !IsFileValid(req.other_enclosure, out fileError)) return BadRequest(new { message = fileError });
            if (req.payment_receipt != null && !IsFileValid(req.payment_receipt, out fileError)) return BadRequest(new { message = fileError });
            if (req.signature_file != null && !IsFileValid(req.signature_file, out fileError)) return BadRequest(new { message = fileError });

            // Batch validate collections
            var collectionsWithFiles = new[] { req.edu_cert, req.exp_proof, req.audit_report, req.training_proof, req.membership_proof, req.paper_proof, req.award_proof };
            foreach (var list in collectionsWithFiles)
            {
                if (list != null) foreach (var f in list) if (f != null && !IsFileValid(f, out fileError)) return BadRequest(new { message = fileError });
            }

            // --- DATA MAPPING ---
            applicant.Title = req.title ?? "";
            applicant.FullName = req.full_name ?? "";
            applicant.ParentRelation = req.parent_relation;
            applicant.ParentName = req.parent_name;
            applicant.DateOfBirth = DateTime.TryParse(req.dob, out var dob) ? dob : DateTime.Now;
            applicant.Gender = req.gender ?? "Other";
            applicant.Citizenship = req.citizenship ?? "Indian";
            applicant.PermanentAddress = req.address_perm ?? "";
            applicant.CorrespondenceAddress = req.address_corr;
            applicant.Email = req.email ?? "";
            applicant.Mobile = req.mobile ?? "";
            applicant.AlternateMobile = req.alt_mobile;
            applicant.GovIdType = req.gov_id_type ?? "";
            applicant.OtherGovIdType = req.other_gov_id_type;
            applicant.GovIdNumber = req.gov_id_number ?? "";
            applicant.Categories = req.cert_category != null ? string.Join(", ", req.cert_category) : string.Empty;
            applicant.EnclosureDescription = req.enclosure_desc;
            
            applicant.LastSavedAt = DateTime.Now;
            if (isFinalSubmit)
            {
                applicant.Status = "Submitted";
                applicant.SubmittedAt = DateTime.Now;
                applicant.CurrentStep = 15;
            }
            else
            {
                applicant.CurrentStep = req.current_step ?? applicant.CurrentStep;
            }

            // --- FILE SAVING ---
            if (req.applicant_photo != null)
                applicant.PhotoPath = await _fileService.SaveFileAsync(req.applicant_photo.OpenReadStream(), req.applicant_photo.FileName);
            if (req.gov_id_upload != null)
                applicant.GovIdPath = await _fileService.SaveFileAsync(req.gov_id_upload.OpenReadStream(), req.gov_id_upload.FileName);

            // --- COLLECTION SYNC (IDEMPOTENCY) ---
            
            // 1. Education
            if (req.edu_degree != null)
            {
                _context.Educations.RemoveRange(applicant.Educations);
                for (int i = 0; i < req.edu_degree.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(req.edu_degree[i])) continue;
                    string? certPath = null;
                    if (req.edu_cert != null && i < req.edu_cert.Count && req.edu_cert[i] != null)
                        certPath = await _fileService.SaveFileAsync(req.edu_cert[i].OpenReadStream(), req.edu_cert[i].FileName);

                    applicant.Educations.Add(new Education {
                        Degree = req.edu_degree[i],
                        Discipline = i < (req.edu_discipline?.Count ?? 0) ? req.edu_discipline![i] : "",
                        Institution = i < (req.edu_institution?.Count ?? 0) ? req.edu_institution![i] : "",
                        Year = i < (req.edu_year?.Count ?? 0) ? req.edu_year![i] : 0,
                        CertificatePath = certPath
                    });
                }
            }

            // 2. Experience
            if (req.exp_org != null)
            {
                _context.ProfessionalExperiences.RemoveRange(applicant.ProfessionalExperiences);
                for (int i = 0; i < req.exp_org.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(req.exp_org[i])) continue;
                    string? proofPath = null;
                    if (req.exp_proof != null && i < req.exp_proof.Count && req.exp_proof[i] != null)
                        proofPath = await _fileService.SaveFileAsync(req.exp_proof[i].OpenReadStream(), req.exp_proof[i].FileName);

                    applicant.ProfessionalExperiences.Add(new ProfessionalExperience {
                        Organization = req.exp_org[i],
                        Designation = i < (req.exp_designation?.Count ?? 0) ? req.exp_designation![i] : "",
                        Duration = i < (req.exp_duration?.Count ?? 0) ? req.exp_duration![i] : "",
                        NatureOfWork = i < (req.exp_nature?.Count ?? 0) ? req.exp_nature![i] : "",
                        ReferencePhone = i < (req.ref2_phone?.Count ?? 0) ? req.ref2_phone![i] : "",
                        ProofPath = proofPath
                    });
                }
            }

            // 3. Projects
            if (req.project_name_cat1 != null)
            {
                _context.ProjectExperiences.RemoveRange(applicant.ProjectExperiences);
                for (int i = 0; i < req.project_name_cat1.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(req.project_name_cat1[i])) continue;
                    applicant.ProjectExperiences.Add(new ProjectExperience {
                        Category = "Category 1",
                        Name = req.project_name_cat1[i],
                        Client = i < (req.project_client_cat1?.Count ?? 0) ? req.project_client_cat1![i] : "",
                        Location = i < (req.project_location_cat1?.Count ?? 0) ? req.project_location_cat1![i] : "",
                        Year = i < (req.project_year_cat1?.Count ?? 0) ? req.project_year_cat1![i] : 0,
                        Role = i < (req.project_role_cat1?.Count ?? 0) ? req.project_role_cat1![i] : ""
                    });
                }
            }

            // 4. Audit Reports
            if (req.audit_report != null && req.audit_report.Any(f => f != null))
            {
                _context.UploadReports.RemoveRange(applicant.UploadReports);
                foreach (var file in req.audit_report)
                {
                    if (file == null) continue;
                    var path = await _fileService.SaveFileAsync(file.OpenReadStream(), file.FileName);
                    applicant.UploadReports.Add(new UploadReport { FilePath = path, FileName = file.FileName });
                }
            }

            // 5. Training
            if (req.training_name != null)
            {
                _context.CertificationTrainings.RemoveRange(applicant.CertificationTrainings);
                for (int i = 0; i < req.training_name.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(req.training_name[i])) continue;
                    string? proof = null;
                    if (req.training_proof != null && i < req.training_proof.Count && req.training_proof[i] != null)
                        proof = await _fileService.SaveFileAsync(req.training_proof[i].OpenReadStream(), req.training_proof[i].FileName);

                    applicant.CertificationTrainings.Add(new CertificationTraining {
                        Name = req.training_name[i],
                        ObtainedFrom = i < (req.training_from?.Count ?? 0) ? req.training_from![i] : "",
                        Duration = i < (req.training_duration?.Count ?? 0) ? req.training_duration![i] : "",
                        Year = i < (req.training_year?.Count ?? 0) ? req.training_year![i] : 0,
                        ProofPath = proof
                    });
                }
            }

            // 6. Memberships
            if (req.membership_name != null)
            {
                _context.Memberships.RemoveRange(applicant.Memberships);
                for (int i = 0; i < req.membership_name.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(req.membership_name[i])) continue;
                    string? proof = null;
                    if (req.membership_proof != null && i < req.membership_proof.Count && req.membership_proof[i] != null)
                        proof = await _fileService.SaveFileAsync(req.membership_proof[i].OpenReadStream(), req.membership_proof[i].FileName);

                    applicant.Memberships.Add(new Membership {
                        Name = req.membership_name[i],
                        ObtainedFrom = i < (req.membership_from?.Count ?? 0) ? req.membership_from![i] : "",
                        Year = i < (req.membership_year?.Count ?? 0) ? req.membership_year![i] : 0,
                        Duration = i < (req.membership_duration?.Count ?? 0) ? req.membership_duration![i] : "",
                        ProofPath = proof
                    });
                }
            }

            // 7. Papers
            if (req.paper_name != null)
            {
                _context.PaperPublisheds.RemoveRange(applicant.PaperPublisheds);
                for (int i = 0; i < req.paper_name.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(req.paper_name[i])) continue;
                    string? proof = null;
                    if (req.paper_proof != null && i < req.paper_proof.Count && req.paper_proof[i] != null)
                        proof = await _fileService.SaveFileAsync(req.paper_proof[i].OpenReadStream(), req.paper_proof[i].FileName);

                    applicant.PaperPublisheds.Add(new PaperPublished {
                        Name = req.paper_name[i],
                        Place = i < (req.paper_place?.Count ?? 0) ? req.paper_place![i] : "",
                        Year = i < (req.paper_year?.Count ?? 0) ? req.paper_year![i] : 0,
                        Role = i < (req.paper_role?.Count ?? 0) ? req.paper_role![i] : "",
                        ProofPath = proof
                    });
                }
            }

            // 8. Awards
            if (req.award_name != null)
            {
                _context.Awards.RemoveRange(applicant.Awards);
                for (int i = 0; i < req.award_name.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(req.award_name[i])) continue;
                    string? proof = null;
                    if (req.award_proof != null && i < req.award_proof.Count && req.award_proof[i] != null)
                        proof = await _fileService.SaveFileAsync(req.award_proof[i].OpenReadStream(), req.award_proof[i].FileName);

                    applicant.Awards.Add(new Award {
                        Name = req.award_name[i],
                        ReceivedFrom = i < (req.award_from?.Count ?? 0) ? req.award_from![i] : "",
                        Year = i < (req.award_year?.Count ?? 0) ? req.award_year![i] : 0,
                        ProofPath = proof
                    });
                }
            }

            // 9. Software Skills
            if (req.software_skill != null)
            {
                _context.SoftwareSkills.RemoveRange(applicant.SoftwareSkills);
                for (int i = 0; i < req.software_skill.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(req.software_skill[i])) continue;
                    applicant.SoftwareSkills.Add(new SoftwareSkill {
                        SoftwareName = req.software_skill[i],
                        ProficiencyLevel = i < (req.proficiency_level?.Count ?? 0) ? req.proficiency_level![i] : ""
                    });
                }
            }

            // 10. Other Enclosures
            if (req.other_enclosure != null)
            {
                _context.OtherEnclosures.RemoveRange(applicant.OtherEnclosures);
                var path = await _fileService.SaveFileAsync(req.other_enclosure.OpenReadStream(), req.other_enclosure.FileName);
                applicant.OtherEnclosures.Add(new OtherEnclosure { FilePath = path, FileName = req.other_enclosure.FileName });
            }

            // 11. Payment Details
            if (req.payment_receipt != null || !string.IsNullOrEmpty(req.payment_utr))
            {
                _context.PaymentDetails.RemoveRange(applicant.PaymentDetails);
                
                // UTR Dup Check
                if (isFinalSubmit && !string.IsNullOrEmpty(req.payment_utr))
                {
                    if (await _context.PaymentDetails.AnyAsync(p => p.UtrNumber == req.payment_utr && p.ApplicantId != applicant.Id))
                        return BadRequest(new { message = "Duplicate UTR Number detected." });
                }

                string? receipt = applicant.PaymentDetails.LastOrDefault()?.ReceiptPath;
                if (req.payment_receipt != null)
                    receipt = await _fileService.SaveFileAsync(req.payment_receipt.OpenReadStream(), req.payment_receipt.FileName);

                applicant.PaymentDetails.Add(new PaymentDetail {
                    Amount = req.payment_amount,
                    PaymentDate = DateTime.TryParse(req.payment_date, out var pd) ? pd : DateTime.Now,
                    UtrNumber = req.payment_utr ?? "",
                    ReceiptPath = receipt
                });
            }

            // 12. Declaration
            if (!string.IsNullOrEmpty(req.decl_name) || req.signature_file != null)
            {
                if (applicant.Declaration == null) applicant.Declaration = new Declaration();
                
                applicant.Declaration.Name = req.decl_name ?? "";
                applicant.Declaration.Date = DateTime.TryParse(req.decl_date, out var dd) ? dd : DateTime.Now;
                applicant.Declaration.Place = req.decl_place;
                
                if (req.signature_file != null)
                    applicant.Declaration.SignaturePath = await _fileService.SaveFileAsync(req.signature_file.OpenReadStream(), req.signature_file.FileName);
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { 
                message = isFinalSubmit ? "Application submitted successfully!" : "Progress saved successfully", 
                savedAt = DateTime.Now.ToString("h:mm tt"),
                isFinal = isFinalSubmit
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "An error occurred while saving. Please try again.", error = ex.Message });
        }
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
            .Include(a => a.Declaration)
            .FirstOrDefaultAsync(a => a.UserId == userId);

        if (applicant == null) return NotFound();

        return Ok(new
        {
            current_step = applicant.CurrentStep,
            last_saved_at = applicant.LastSavedAt?.ToString("dd MMM yyyy, h:mm tt") ?? "Unknown",
            title = applicant.Title,
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
            other_gov_id_type = applicant.OtherGovIdType,
            gov_id_number = applicant.GovIdNumber,
            categories = applicant.Categories.Split(", ", StringSplitOptions.RemoveEmptyEntries).ToList(),
            enclosure_desc = applicant.EnclosureDescription,
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
                nature = ex.NatureOfWork,
                phone = ex.ReferencePhone
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
            }).FirstOrDefault(),
            declaration = applicant.Declaration != null ? new {
                name = applicant.Declaration.Name,
                date = applicant.Declaration.Date.ToString("yyyy-MM-dd"),
                place = applicant.Declaration.Place
            } : null
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
            if (file.Length > 10 * 1024 * 1024)
            {
                errorMessage = $"File '{file.FileName}' exceeds 10MB limit.";
                return false;
            }
        }

        return true;
    }
}
