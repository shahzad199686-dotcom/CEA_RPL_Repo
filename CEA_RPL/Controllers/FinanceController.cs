using CEA_RPL.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace CEA_RPL.Controllers
{
    [Authorize(Roles = "Finance")]
    public class FinanceController : Controller
    {
        private readonly ApplicationDbContext _context;

        public FinanceController(ApplicationDbContext context)
        {
            _context = context;
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
    }
}
