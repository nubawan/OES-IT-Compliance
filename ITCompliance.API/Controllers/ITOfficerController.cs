using ITCompliance.API.Data;
using ITCompliance.API.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Controllers
{
    //[Authorize(Roles = "ITOfficer")]
    [Route("ITOfficer")]
    public class ITOfficerController : Controller
    {
        private readonly AppDbContext _context;

        public ITOfficerController(AppDbContext context)
        {
            _context = context;
        }

        [HttpGet("Dashboard")]
        public async Task<IActionResult> Dashboard()
        {
            var requests = await _context.InternetAccessRequests
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Kpis = new List<Kpi>
            {
                new("Awaiting my approval",
                    requests.Count(r => r.Status == RequestStatus.Pending),
                    "warn"),

                new("Approved by me",
                    requests.Count(r =>
                        r.Status != RequestStatus.Pending &&
                        !RequestStatus.IsRejected(r.Status)),
                    "ok"),

                new("Rejected by me",
                    requests.Count(r => r.Status == RequestStatus.Rejected),
                    "danger")
            };

            return View(requests);
        }

        [HttpPost("Approve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(int id, string? remarks)
        {
            var request = await _context.InternetAccessRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "The selected request was not found.";
                return Redirect("/ITOfficer/Dashboard");
            }

            if (request.Status != RequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "This request has already been processed.";
                return Redirect("/ITOfficer/Dashboard");
            }

            request.Status = RequestStatus.ItOfficerApproved;
            request.ITOfficerRemarks = string.IsNullOrWhiteSpace(remarks)
                ? "Approved by IT Officer"
                : remarks.Trim();
            request.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Request approved successfully.";
            return Redirect("/ITOfficer/Dashboard");
        }

        [HttpPost("Reject")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(int id, string? remarks)
        {
            var request = await _context.InternetAccessRequests
                .FirstOrDefaultAsync(r => r.Id == id);

            if (request == null)
            {
                TempData["ErrorMessage"] = "The selected request was not found.";
                return Redirect("/ITOfficer/Dashboard");
            }

            if (request.Status != RequestStatus.Pending)
            {
                TempData["ErrorMessage"] = "This request has already been processed.";
                return Redirect("/ITOfficer/Dashboard");
            }

            request.Status = RequestStatus.Rejected;
            request.ITOfficerRemarks = string.IsNullOrWhiteSpace(remarks)
                ? "Rejected by IT Officer"
                : remarks.Trim();
            request.UpdatedAt = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Request rejected successfully.";
            return Redirect("/ITOfficer/Dashboard");
        }
    }
}
