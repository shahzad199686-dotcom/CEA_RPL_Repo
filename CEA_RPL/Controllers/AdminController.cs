using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CEA_RPL.Infrastructure.Data;
using CEA_RPL.Models;
using CEA_RPL.Domain.Entities;

namespace CEA_RPL.Controllers;

[Authorize(Roles = "Admin")]
public class AdminController : Controller
{
    private readonly ApplicationDbContext _context;
    private readonly ReaDbContext _reaContext;
    private readonly CEA_RPL.Infrastructure.Services.IEncryptionService _encryptionService;

    public AdminController(ApplicationDbContext context, ReaDbContext reaContext, CEA_RPL.Infrastructure.Services.IEncryptionService encryptionService)
    {
        _context = context;
        _reaContext = reaContext;
        _encryptionService = encryptionService;
    }

    [HttpGet("Admin/Dashboard")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Dashboard()
    {
        var applicants = await _context.Applicants
            .OrderByDescending(a => a.SubmittedAt ?? a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .ToListAsync();

        var reaApps = await _reaContext.Applications
            .ToListAsync();

        ViewBag.RplTotal = applicants.Count;
        ViewBag.ReaTotal = reaApps.Count;
        ViewBag.RplPending = applicants.Count(a => a.Status == "Submitted" || a.Status == "UnderReview");
        ViewBag.ReaPending = reaApps.Count(a => a.Status == ApplicationStatus.Submitted || a.Status == ApplicationStatus.InReview);

        // Unified counts for widgets
        ViewBag.Total = applicants.Count + reaApps.Count;
        ViewBag.Pending = ViewBag.RplPending + ViewBag.ReaPending;
        ViewBag.Approved = applicants.Count(a => a.Status == "Approved") + reaApps.Count(a => a.Status == ApplicationStatus.Approved);
        ViewBag.Rejected = applicants.Count(a => a.Status == "Rejected") + reaApps.Count(a => a.Status == ApplicationStatus.Rejected);
        
        ViewBag.SubmittedToday = applicants.Count(a => a.SubmittedAt.HasValue && a.SubmittedAt.Value.Date == DateTime.UtcNow.Date) +
                                 reaApps.Count(a => a.CreatedDate.Date == DateTime.UtcNow.Date && a.Status != ApplicationStatus.Draft);

        ViewBag.FinancePending = applicants.Count(a => a.Status != "Draft" && (a.PaymentStatus ?? "Pending") == "Pending") +
                                 reaApps.Count(a => a.Status != ApplicationStatus.Draft && (a.PaymentStatus ?? "Pending") == "Pending");

        return View(applicants);
    }

    [HttpGet("Admin/Details/{id}")]
    public async Task<IActionResult> Details(int id)
    {
        var applicant = await _context.Applicants
            .Include(a => a.Educations)
            .Include(a => a.ProfessionalExperiences)
            .Include(a => a.ProjectExperiences)
            .Include(a => a.Awards)
            .Include(a => a.CertificationTrainings)
            .Include(a => a.Memberships)
            .Include(a => a.PaperPublisheds)
            .Include(a => a.SoftwareSkills)
            .Include(a => a.UploadReports)
            .Include(a => a.PaymentDetails)
            .Include(a => a.Declaration)
            .Include(a => a.OtherEnclosures)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (applicant == null) return NotFound();

        applicant.GovIdNumber = _encryptionService.Decrypt(applicant.GovIdNumber);
        applicant.Mobile = _encryptionService.Decrypt(applicant.Mobile);
        if (!string.IsNullOrEmpty(applicant.AlternateMobile))
        {
            applicant.AlternateMobile = _encryptionService.Decrypt(applicant.AlternateMobile);
        }

        return View(applicant);
    }

    [HttpPost("api/admin/update-status")]
    public async Task<IActionResult> UpdateStatus([FromForm] int id, [FromForm] string status, [FromForm] string? feedback)
    {
        var applicant = await _context.Applicants.FindAsync(id);
        if (applicant == null) return NotFound();

        // Safety Check: Admin cannot approve if Finance has not verified payment
        if (status == "Approved" && applicant.PaymentStatus != "Verified")
        {
            return BadRequest(new { message = "Cannot approve application. Awaiting Finance Payment Verification." });
        }

        applicant.Status = status;
        applicant.AdminFeedback = feedback;
        
        await _context.SaveChangesAsync();
        return Ok(new { message = $"Application status updated to {status}." });
    }

    [HttpGet("Admin/Applications")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Applications()
    {
        var applicants = await _context.Applicants
            .OrderByDescending(a => a.SubmittedAt ?? a.CreatedAt)
            .ThenByDescending(a => a.Id)
            .ToListAsync();

        foreach (var app in applicants)
        {
            app.GovIdNumber = _encryptionService.Decrypt(app.GovIdNumber);
            app.Mobile = _encryptionService.Decrypt(app.Mobile);
            if (!string.IsNullOrEmpty(app.AlternateMobile))
                app.AlternateMobile = _encryptionService.Decrypt(app.AlternateMobile);
        }

        return View(applicants);
    }

    [HttpGet("Admin/Documents")]
    public async Task<IActionResult> Documents()
    {
        var applicants = await _context.Applicants
            .Include(a => a.UploadReports)
            .Include(a => a.OtherEnclosures)
            .ToListAsync();

        var docs = new List<AdminDocumentViewModel>();
        foreach (var app in applicants)
        {
            var appRef = $"RPL-{app.Id.ToString("D4")}";

            if (!string.IsNullOrEmpty(app.GovIdPath))
                docs.Add(new AdminDocumentViewModel 
                { 
                    ApplicantId = app.Id,
                    ApplicantRef = appRef,
                    ApplicantName = app.FullName, 
                    Type = "GOV ID", 
                    Path = app.GovIdPath, 
                    Date = app.CreatedAt 
                });
            
            foreach (var r in app.UploadReports)
                docs.Add(new AdminDocumentViewModel 
                { 
                    ApplicantId = app.Id,
                    ApplicantRef = appRef,
                    ApplicantName = app.FullName, 
                    Type = "REPORT", 
                    Path = r.FilePath, 
                    Date = r.CreatedAt != DateTime.MinValue ? r.CreatedAt : app.CreatedAt 
                });

            foreach (var e in app.OtherEnclosures)
                docs.Add(new AdminDocumentViewModel 
                { 
                    ApplicantId = app.Id,
                    ApplicantRef = appRef,
                    ApplicantName = app.FullName, 
                    Type = "ENCLOSURE", 
                    Path = e.FilePath, 
                    Date = app.CreatedAt 
                });
        }

        var sortedDocs = docs
            .GroupBy(d => d.ApplicantId)
            .OrderByDescending(g => g.Max(d => d.Date))
            .SelectMany(g => g.OrderByDescending(d => d.Date))
            .ToList();

        return View(sortedDocs);
    }

    [HttpGet("Admin/Reports")]
    public async Task<IActionResult> Reports()
    {
        var applicants = await _context.Applicants.ToListAsync();
        
        var report = new AdminReportViewModel
        {
            Total = applicants.Count,
            Approved = applicants.Count(a => a.Status == "Approved"),
            Pending = applicants.Count(a => a.Status == "Submitted" || a.Status == "UnderReview"),
            Rejected = applicants.Count(a => a.Status == "Rejected"),
            StatusDistribution = applicants
                .GroupBy(a => a.Status)
                .ToDictionary(g => g.Key ?? "Unknown", g => g.Count()),
            RecentActivities = applicants.OrderByDescending(a => a.UpdatedAt).Take(10).Select(a => new RecentActivityViewModel
            {
                Action = "Status Updated",
                Entity = $"Application #{a.Id}",
                Actor = "System AD",
                Timestamp = a.UpdatedAt ?? a.CreatedAt
            }).ToList()
        };

        return View(report);
    }

    [HttpGet("Admin/Help")]
    public IActionResult Help()
    {
        return View();
    }

    [HttpGet("Admin/ReaApplications")]
    [ResponseCache(Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> ReaApplications()
    {
        var apps = await _reaContext.Applications
            .Include(a => a.ContactDetail)
            .Include(a => a.OrganizationDetail)
            .OrderByDescending(a => a.CreatedDate)
            .ToListAsync();

        return View(apps);
    }

    [HttpGet("Admin/ReaDetails/{id}")]
    public async Task<IActionResult> ReaDetails(int id)
    {
        var app = await _reaContext.Applications
            .Include(a => a.OrganizationDetail)
            .Include(a => a.ContactDetail)
            .Include(a => a.Categories)
            .Include(a => a.States)
            .Include(a => a.FinancialDetails)
            .Include(a => a.CEADetails)
            .Include(a => a.LaboratoryInfo)
            .Include(a => a.LaboratoryDetails)
            .Include(a => a.HardwareDetails)
            .Include(a => a.SoftwareDetails)
            .Include(a => a.AuditExperiences)
            .Include(a => a.PaymentDetail)
            .Include(a => a.Declaration)
            .Include(a => a.Checklist)
            .Include(a => a.Documents)
            .Include(a => a.AdminRemarks)
            .Include(a => a.StatusHistories)
            .FirstOrDefaultAsync(a => a.Id == id);

        if (app == null) return NotFound();

        // Decrypt payment UTR if encrypted
        if (app.PaymentDetail != null && !string.IsNullOrEmpty(app.PaymentDetail.UTR))
        {
            try
            {
                app.PaymentDetail.UTR = _encryptionService.Decrypt(app.PaymentDetail.UTR);
            }
            catch { /* Ignore decryption failure */ }
        }

        return View(app);
    }

    [HttpPost("api/admin/update-rea-status")]
    public async Task<IActionResult> UpdateReaStatus([FromForm] int id, [FromForm] string status, [FromForm] string? feedback)
    {
        var app = await _reaContext.Applications.FindAsync(id);
        if (app == null) return NotFound();

        if (!Enum.TryParse<ApplicationStatus>(status, true, out var parsedStatus))
        {
            return BadRequest(new { message = "Invalid status value." });
        }

        // Safety Check: Admin cannot approve if Finance has not verified payment
        if (parsedStatus == ApplicationStatus.Approved && app.PaymentStatus != "Verified")
        {
            return BadRequest(new { message = "Cannot approve application. Awaiting Finance Payment Verification." });
        }

        app.Status = parsedStatus;
        app.LastUpdatedDate = DateTime.UtcNow;

        _reaContext.AdminRemarks.Add(new AdminRemark
        {
            ApplicationId = id,
            Remark = feedback ?? "Status updated by Centralized Admin.",
            AdminId = User.Identity?.Name ?? "Admin",
            CreatedDate = DateTime.UtcNow
        });

        _reaContext.StatusHistories.Add(new StatusHistory
        {
            ApplicationId = id,
            Status = parsedStatus,
            ChangedBy = User.Identity?.Name ?? "Admin",
            ChangedDate = DateTime.UtcNow
        });
        
        await _reaContext.SaveChangesAsync();
        return Ok(new { message = $"Application status updated to {status}." });
    }
}
