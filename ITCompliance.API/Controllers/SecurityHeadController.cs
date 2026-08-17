using ITCompliance.API.Data;
using ITCompliance.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Controllers
{
    [Authorize(Roles = RoleNames.SecurityHead)]
    public class SecurityHeadController : Controller
    {
        private readonly AppDbContext _context;

        public SecurityHeadController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var requests = await _context.InternetAccessRequests
                .Where(r => r.Status == RequestStatus.HodApproved)
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
                new("Awaiting my approval",
                    Count(RequestStatus.HodApproved), "info"),

                new("Approved by me",
                    Count(RequestStatus.SecurityHeadApproved) +
                    Count(RequestStatus.BossApproved), "ok"),

                new("Rejected by me",
                    Count(RequestStatus.RejectedBySecurityHead), "danger")
            };
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? remarks)
        {
            try
            {
                var request = await _context.InternetAccessRequests
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "The selected request was not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                if (request.Status != RequestStatus.HodApproved)
                {
                    TempData["ErrorMessage"] =
                        "This request is not available for Security Head approval.";
                    return RedirectToAction(nameof(Dashboard));
                }

                request.Status = RequestStatus.SecurityHeadApproved;
                request.SecurityHeadRemarks = string.IsNullOrWhiteSpace(remarks)
                    ? "Approved by Security Head"
                    : remarks.Trim();
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Request approved successfully by Security Head.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Approval failed. Please try again.";
                return RedirectToAction(nameof(Dashboard));
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? remarks)
        {
            try
            {
                var request = await _context.InternetAccessRequests
                    .FirstOrDefaultAsync(r => r.Id == id);

                if (request == null)
                {
                    TempData["ErrorMessage"] = "The selected request was not found.";
                    return RedirectToAction(nameof(Dashboard));
                }

                if (request.Status != RequestStatus.HodApproved)
                {
                    TempData["ErrorMessage"] =
                        "This request is not available for Security Head processing.";
                    return RedirectToAction(nameof(Dashboard));
                }

                request.Status = RequestStatus.RejectedBySecurityHead;
                request.SecurityHeadRemarks = string.IsNullOrWhiteSpace(remarks)
                    ? "Rejected by Security Head"
                    : remarks.Trim();
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Request rejected successfully by Security Head.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "Rejection failed. Please try again.";
                return RedirectToAction(nameof(Dashboard));
            }
        }
    }
}
