using System.Security.Claims;
using CEA_RPL.Models;
using CEA_RPL.Application.Interfaces;
using CEA_RPL.Domain.Entities;
using CEA_RPL.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    public IActionResult Index()
    {
        return View();
    }

    [HttpPost("submit")]
    [Authorize]
    public async Task<IActionResult> Submit([FromForm] ApplicationSubmissionRequest req)
    {
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString) || !int.TryParse(userIdString, out var userId))
            return Unauthorized();
            
        // Check if user already submitted an application
        if (_context.Applicants.Any(a => a.UserId == userId))
        {
            return BadRequest(new { message = "You have already submitted an application." });
        }

        var photoPath = await _fileService.SaveFileAsync(req.applicant_photo.OpenReadStream(), req.applicant_photo.FileName);
        var govIdPath = await _fileService.SaveFileAsync(req.gov_id_upload.OpenReadStream(), req.gov_id_upload.FileName);

        var applicant = new Applicant
        {
            UserId = userId,
            FullName = req.full_name,
            ParentName = req.parent_name,
            DateOfBirth = DateTime.Parse(req.dob),
            Gender = req.gender,
            Citizenship = req.citizenship,
            PermanentAddress = req.address_perm,
            CorrespondenceAddress = req.address_corr,
            Email = req.email,
            Mobile = req.mobile,
            AlternateMobile = req.alt_mobile,
            PhotoPath = photoPath,
            GovIdType = req.gov_id_type,
            GovIdNumber = req.gov_id_number,
            GovIdPath = govIdPath,
            Categories = req.cert_category != null ? string.Join(", ", req.cert_category) : string.Empty,
            Status = "Submitted"
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

        _context.Applicants.Add(applicant);
        await _context.SaveChangesAsync();
        
        return Ok(new { message = "Application submitted successfully", applicantId = applicant.Id });
    }
}
