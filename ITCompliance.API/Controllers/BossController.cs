using ITCompliance.API.Data;
using ITCompliance.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Controllers
{
    //[Authorize(Roles = "Boss")]
    public class BossController : Controller
    {
        private readonly AppDbContext _context;

        public BossController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var requests = await _context.InternetAccessRequests
                .Where(r => r.Status == RequestStatus.SecurityHeadApproved)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Kpis = await GetKpisAsync();

            return View(requests);
        }

        private async Task<List<Kpi>> GetKpisAsync()
        {
            var counts = await _context.InternetAccessRequests
                .GroupBy(r => r.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            int Count(string status) =>
                counts.TryGetValue(status, out var c) ? c : 0;

            return new List<Kpi>
            {
                new("Awaiting final approval",
                    Count(RequestStatus.SecurityHeadApproved), "info"),

                new("Approved by me",
                    Count(RequestStatus.BossApproved), "ok"),

                new("Rejected by me",
                    Count(RequestStatus.RejectedByBoss), "danger")
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? remarks)
        {
            var request = await _context.InternetAccessRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != RequestStatus.SecurityHeadApproved)
            {
                TempData["ErrorMessage"] =
                    "This request is not available for Boss approval.";
                return RedirectToAction("Dashboard");
            }

            request.Status = RequestStatus.BossApproved;
            request.BossRemarks = string.IsNullOrWhiteSpace(remarks)
                ? "Approved by Boss"
                : remarks.Trim();
            request.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Request approved successfully by Boss.";
            return RedirectToAction("Dashboard");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? remarks)
        {
            var request = await _context.InternetAccessRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                return NotFound();
            }

            if (request.Status != RequestStatus.SecurityHeadApproved)
            {
                TempData["ErrorMessage"] =
                    "This request is not available for Boss processing.";
                return RedirectToAction("Dashboard");
            }

            request.Status = RequestStatus.RejectedByBoss;
            request.BossRemarks = string.IsNullOrWhiteSpace(remarks)
                ? "Rejected by Boss"
                : remarks.Trim();
            request.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] =
                "Request rejected successfully by Boss.";
            return RedirectToAction("Dashboard");
        }
    }
}
