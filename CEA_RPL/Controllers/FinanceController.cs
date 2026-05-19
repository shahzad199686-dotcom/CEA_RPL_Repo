using CEA_RPL.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using CEA_RPL.Domain.Entities;

namespace CEA_RPL.Controllers
{
    [Authorize(Roles = "Admin,Finance")]
    public class FinanceController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly ReaDbContext _reaContext;
        private readonly CEA_RPL.Infrastructure.Services.IEncryptionService _encryptionService;

        public FinanceController(ApplicationDbContext context, ReaDbContext reaContext, CEA_RPL.Infrastructure.Services.IEncryptionService encryptionService)
        {
            _context = context;
            _reaContext = reaContext;
            _encryptionService = encryptionService;
        }

        public async Task<IActionResult> Dashboard()
        {
            var applicants = await _context.Applicants
                .Include(a => a.PaymentDetails)
                .Where(a => a.Status != "Draft")
                .OrderByDescending(a => a.SubmittedAt)
                .ToListAsync();

            ViewBag.TotalPayments = applicants.Count;
            ViewBag.PendingCount = applicants.Count(a => (a.PaymentStatus ?? "Pending") == "Pending");
            ViewBag.VerifiedCount = applicants.Count(a => a.PaymentStatus == "Verified");
            ViewBag.NotVerifiedCount = applicants.Count(a => a.PaymentStatus == "NotVerified");

            return View(applicants);
        }

        [HttpPost]
        [Route("api/finance/verify")]
        public async Task<IActionResult> VerifyPayment([FromForm] int id, [FromForm] string action, [FromForm] string? remarks)
        {
            var applicant = await _context.Applicants.FindAsync(id);
            if (applicant == null) return NotFound();

            if (action == "verify")
            {
                applicant.PaymentStatus = "Verified";
                applicant.FinanceRemarks = remarks;
            }
            else if (action == "reject")
            {
                applicant.PaymentStatus = "NotVerified";
                applicant.FinanceRemarks = remarks;
            }

            await _context.SaveChangesAsync();
            return Ok(new { message = $"Payment successfully {(action == "verify" ? "verified" : "marked as not verified")}." });
        }

        [HttpGet]
        [Route("Finance/ReaApplications")]
        public async Task<IActionResult> ReaApplications()
        {
            var apps = await _reaContext.Applications
                .Include(a => a.PaymentDetail)
                .Include(a => a.ContactDetail)
                .Include(a => a.OrganizationDetail)
                .Where(a => a.Status != ApplicationStatus.Draft)
                .OrderByDescending(a => a.CreatedDate)
                .ToListAsync();

            // Decrypt UTR numbers for Finance verification
            foreach (var app in apps)
            {
                if (app.PaymentDetail != null && !string.IsNullOrEmpty(app.PaymentDetail.UTR))
                {
                    try
                    {
                        app.PaymentDetail.UTR = _encryptionService.Decrypt(app.PaymentDetail.UTR);
                    }
                    catch { /* Ignore decryption errors */ }
                }
            }

            ViewBag.TotalPayments = apps.Count;
            ViewBag.PendingCount = apps.Count(a => (a.PaymentStatus ?? "Pending") == "Pending");
            ViewBag.VerifiedCount = apps.Count(a => a.PaymentStatus == "Verified");
            ViewBag.NotVerifiedCount = apps.Count(a => a.PaymentStatus == "NotVerified");

            return View(apps);
        }

        [HttpPost]
        [Route("api/finance/verify-rea")]
        public async Task<IActionResult> VerifyReaPayment([FromForm] int id, [FromForm] string action, [FromForm] string? remarks)
        {
            var app = await _reaContext.Applications.FindAsync(id);
            if (app == null) return NotFound();

            if (action == "verify")
            {
                app.PaymentStatus = "Verified";
                app.FinanceRemarks = remarks;
            }
            else if (action == "reject")
            {
                app.PaymentStatus = "NotVerified";
                app.FinanceRemarks = remarks;
            }

            await _reaContext.SaveChangesAsync();
            return Ok(new { message = $"Payment successfully {(action == "verify" ? "verified" : "marked as not verified")}." });
        }
    }
}
