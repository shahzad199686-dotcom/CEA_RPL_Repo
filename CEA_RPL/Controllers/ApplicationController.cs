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

        // Check if user already submitted an application
        if (_context.Applicants.Any(a => a.UserId == userId))
        {
            return BadRequest(new { message = "You have already submitted an application." });
        }

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

        var photoPath = await _fileService.SaveFileAsync(req.applicant_photo.OpenReadStream(), req.applicant_photo.FileName);
        var govIdPath = await _fileService.SaveFileAsync(req.gov_id_upload.OpenReadStream(), req.gov_id_upload.FileName);

        var applicant = new Applicant
        {
            UserId = userId,
            FullName = req.full_name ?? "",
            ParentRelation = req.parent_relation,
            ParentName = req.parent_name,
            DateOfBirth = DateTime.TryParse(req.dob, out var dob) ? dob : DateTime.Now,
            Gender = string.IsNullOrWhiteSpace(req.gender) ? "Other" : req.gender,
            Citizenship = string.IsNullOrWhiteSpace(req.citizenship) ? "Indian" : req.citizenship,
            PermanentAddress = req.address_perm ?? "",
            CorrespondenceAddress = req.address_corr,
            Email = req.email ?? "",
            Mobile = req.mobile ?? "",
            AlternateMobile = req.alt_mobile,
            PhotoPath = photoPath,
            GovIdType = req.gov_id_type ?? "",
            GovIdNumber = req.gov_id_number ?? "",
            GovIdPath = govIdPath,
            Categories = req.cert_category != null ? string.Join(", ", req.cert_category) : string.Empty,
            Status = "Submitted"
        };

        // Section 6: Audit Reports
        if (report1 != null) applicant.UploadReports.Add(new UploadReport { FilePath = report1, FileName = req.audit_report?[0].FileName ?? "Report1" });
        if (report2 != null) applicant.UploadReports.Add(new UploadReport { FilePath = report2, FileName = req.audit_report?[1].FileName ?? "Report2" });

        // Section 12: Other Enclosures
        if (otherEnc != null) applicant.OtherEnclosures.Add(new OtherEnclosure { FilePath = otherEnc, FileName = req.other_enclosure?.FileName ?? "AdditionalDoc" });

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
                    Degree = req.edu_degree[i],
                    Discipline = req.edu_discipline?[i] ?? "",
                    Institution = req.edu_institution?[i] ?? "",
                    Year = req.edu_year != null && i < req.edu_year.Count ? req.edu_year[i] : 0,
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
                    Organization = req.exp_org[i],
                    Designation = req.exp_designation?[i] ?? "",
                    Duration = req.exp_duration?[i] ?? "",
                    NatureOfWork = req.exp_nature?[i] ?? "",
                    ReferencePhone = req.ref2_phone?[i] ?? "",
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
                    Name = req.project_name_cat1[i],
                    Client = req.project_client_cat1?[i] ?? "",
                    Location = req.project_location_cat1?[i] ?? "",
                    Year = req.project_year_cat1 != null && i < req.project_year_cat1.Count ? req.project_year_cat1[i] : 0,
                    Role = req.project_role_cat1?[i] ?? ""
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
                    Name = req.training_name[i],
                    ObtainedFrom = req.training_from != null && i < req.training_from.Count ? req.training_from[i] : "",
                    Duration = req.training_duration != null && i < req.training_duration.Count ? req.training_duration[i] : "",
                    Year = req.training_year != null && i < req.training_year.Count ? req.training_year[i] : 0,
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
                    Name = req.membership_name[i],
                    ObtainedFrom = req.membership_from != null && i < req.membership_from.Count ? req.membership_from[i] : "",
                    Year = req.membership_year != null && i < req.membership_year.Count ? req.membership_year[i] : 0,
                    Duration = req.membership_duration != null && i < req.membership_duration.Count ? req.membership_duration[i] : "",
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
                    Name = req.paper_name?[i] ?? "",
                    Place = req.paper_place != null && i < req.paper_place.Count ? req.paper_place[i] : "",
                    Year = req.paper_year != null && i < req.paper_year.Count ? req.paper_year[i] : 0,
                    Role = req.paper_role != null && i < req.paper_role.Count ? req.paper_role[i] : "",
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
                    Name = req.award_name[i],
                    ReceivedFrom = req.award_from != null && i < req.award_from.Count ? req.award_from[i] : "",
                    Year = req.award_year != null && i < req.award_year.Count ? req.award_year[i] : 0,
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
                    SoftwareName = req.software_skill[i],
                    ProficiencyLevel = req.proficiency_level != null && i < req.proficiency_level.Count ? req.proficiency_level[i] : ""
                });
            }
        }

        _context.Applicants.Add(applicant);
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Application submitted successfully", applicantId = applicant.Id });
    }
}
