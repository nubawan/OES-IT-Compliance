using ITCompliance.API.Data;
using ITCompliance.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Controllers
{
    //[Authorize(Roles = "HOD")]
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
            var requests = await _context.InternetAccessRequests
                .Where(r => r.Status == RequestStatus.ItOfficerApproved)
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
                    TempData["ErrorMessage"] = "Request not found.";
                    return Redirect("/HOD/Dashboard");
                }

                if (request.Status != RequestStatus.ItOfficerApproved)
                {
                    TempData["ErrorMessage"] =
                        "This request is not available for HOD approval.";
                    return Redirect("/HOD/Dashboard");
                }

                request.Status = RequestStatus.HodApproved;
                request.HODRemarks = string.IsNullOrWhiteSpace(remarks)
                    ? "Approved by HOD"
                    : remarks.Trim();
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Request approved successfully.";
                return Redirect("/HOD/Dashboard");
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "HOD approval failed. Please try again.";
                return Redirect("/HOD/Dashboard");
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
                    TempData["ErrorMessage"] = "Request not found.";
                    return Redirect("/HOD/Dashboard");
                }

                if (request.Status != RequestStatus.ItOfficerApproved)
                {
                    TempData["ErrorMessage"] =
                        "This request is not available for HOD processing.";
                    return Redirect("/HOD/Dashboard");
                }

                request.Status = RequestStatus.RejectedByHod;
                request.HODRemarks = string.IsNullOrWhiteSpace(remarks)
                    ? "Rejected by HOD"
                    : remarks.Trim();
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                TempData["SuccessMessage"] =
                    "Request rejected successfully.";
                return Redirect("/HOD/Dashboard");
            }
            catch
            {
                TempData["ErrorMessage"] =
                    "HOD rejection failed. Please try again.";
                return Redirect("/HOD/Dashboard");
            }
        }
    }
}
