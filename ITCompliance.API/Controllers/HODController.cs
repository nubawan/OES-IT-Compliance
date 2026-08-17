using ITCompliance.API.Data;
using ITCompliance.API.Models;
using ITCompliance.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Controllers
{
    [Authorize(Roles = RoleNames.HOD)]
    public class HODController : Controller
    {
        private readonly AppDbContext _context;

        public HODController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var deptCodes = User.GetDepartmentScopes(RoleNames.HOD);

            var query = _context.InternetAccessRequests
                .Where(r => r.Status == RequestStatus.ItOfficerApproved);

            if (deptCodes.Count > 0)
            {
                query = query.Where(r => deptCodes.Contains(r.DepartmentCode));
            }

            var requests = await query
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Kpis = await GetKpisAsync(deptCodes);

            return View(requests);
        }

        private async Task<List<Kpi>> GetKpisAsync(
            IReadOnlyList<string> deptCodes)
        {
            var query = _context.InternetAccessRequests.AsQueryable();

            if (deptCodes.Count > 0)
            {
                query = query.Where(r => deptCodes.Contains(r.DepartmentCode));
            }

            var counts = await query
                .GroupBy(r => r.Status)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            int Count(string status) =>
                counts.TryGetValue(status, out var c) ? c : 0;

            return new List<Kpi>
            {
                new("Awaiting my approval",
                    Count(RequestStatus.ItOfficerApproved), "info"),

                new("Approved by me",
                    Count(RequestStatus.HodApproved) +
                    Count(RequestStatus.SecurityHeadApproved) +
                    Count(RequestStatus.BossApproved), "ok"),

                new("Rejected by me",
                    Count(RequestStatus.RejectedByHod), "danger")
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

                if (request.Status != RequestStatus.ItOfficerApproved)
                {
                    TempData["ErrorMessage"] =
                        "This request is not available for HOD approval.";
                    return RedirectToAction(nameof(Dashboard));
                }

                request.Status = RequestStatus.HodApproved;
                request.HODRemarks = string.IsNullOrWhiteSpace(remarks)
                    ? "Approved by HOD"
                    : remarks.Trim();
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Request approved successfully.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "HOD approval failed. Please try again.";
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

                if (request.Status != RequestStatus.ItOfficerApproved)
                {
                    TempData["ErrorMessage"] =
                        "This request is not available for HOD processing.";
                    return RedirectToAction(nameof(Dashboard));
                }

                request.Status = RequestStatus.RejectedByHod;
                request.HODRemarks = string.IsNullOrWhiteSpace(remarks)
                    ? "Rejected by HOD"
                    : remarks.Trim();
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Request rejected successfully.";
                return RedirectToAction(nameof(Dashboard));
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "HOD rejection failed. Please try again.";
                return RedirectToAction(nameof(Dashboard));
            }
        }
    }
}
