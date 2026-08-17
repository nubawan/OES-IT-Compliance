using System.Security.Claims;
using ITCompliance.API.Data;
using ITCompliance.API.Models;
using ITCompliance.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Controllers
{
    // Shares the AwaitingItOfficerOrSecurityHead stage with
    // ITOfficerController - whichever of the two approves first
    // advances the request (OR gate); the other simply won't find it
    // in their queue anymore. See Services/WorkflowRouter.cs.
    [Authorize(Roles = RoleNames.SecurityHead)]
    public class SecurityHeadController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWorkflowNotificationService _notifications;
        private readonly IConfiguration _configuration;

        public SecurityHeadController(
            AppDbContext context,
            IWorkflowNotificationService notifications,
            IConfiguration configuration)
        {
            _context = context;
            _notifications = notifications;
            _configuration = configuration;
        }

        private string ItDepartmentCode =>
            _configuration["Workflow:ItDepartmentCode"] ?? "93";

        [HttpGet]
        public async Task<IActionResult> Dashboard()
        {
            var requests = await _context.InternetAccessRequests
                .Where(r => r.Status == RequestStatus.AwaitingItOfficerOrSecurityHead)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            ViewBag.Kpis = await GetKpisAsync();

            return View(requests);
        }

        private async Task<List<Kpi>> GetKpisAsync()
        {
            var awaitingCount = await _context.InternetAccessRequests
                .CountAsync(r => r.Status == RequestStatus.AwaitingItOfficerOrSecurityHead);

            var actedCounts = await _context.RequestTransactions
                .Where(t => t.ActorRole == RoleNames.SecurityHead)
                .GroupBy(t => t.Action)
                .Select(g => new { g.Key, Count = g.Count() })
                .ToDictionaryAsync(x => x.Key, x => x.Count);

            int Count(string action) =>
                actedCounts.TryGetValue(action, out var c) ? c : 0;

            return new List<Kpi>
            {
                new("Awaiting my approval", awaitingCount, "info"),
                new("Approved by me", Count(TransactionAction.Approve), "ok"),
                new("Rejected by me", Count(TransactionAction.Reject), "danger")
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

                if (request.Status != RequestStatus.AwaitingItOfficerOrSecurityHead)
                {
                    TempData["ErrorMessage"] = "This request has already been processed.";
                    return RedirectToAction(nameof(Dashboard));
                }

                var completedStage = request.Status;

                RecordTransaction(request, completedStage, TransactionAction.Approve, remarks);

                var (nextStatus, nextPendingDept) =
                    WorkflowRouter.GetNextStage(request.Status, ItDepartmentCode);

                request.Status = nextStatus;
                request.PendingDepartmentCode = nextPendingDept ?? "";
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await _notifications.NotifyAdvancedAsync(request, completedStage);

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

                if (request.Status != RequestStatus.AwaitingItOfficerOrSecurityHead)
                {
                    TempData["ErrorMessage"] = "This request has already been processed.";
                    return RedirectToAction(nameof(Dashboard));
                }

                var rejectedAtStage = request.Status;

                RecordTransaction(request, rejectedAtStage, TransactionAction.Reject, remarks);

                request.Status = RequestStatus.Rejected;
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await _notifications.NotifyFinalDecisionAsync(request, rejectedAtStage);

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

        private void RecordTransaction(
            InternetAccessRequest request,
            string stageStatus,
            string action,
            string? remarks)
        {
            var actorEmpId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? "";

            _context.RequestTransactions.Add(new RequestTransaction
            {
                RequestId = request.Id,
                StageStatus = stageStatus,
                ActorEmpId = actorEmpId,
                ActorRole = RoleNames.SecurityHead,
                Action = action,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim()
            });

            request.SecurityHeadRemarks = string.IsNullOrWhiteSpace(remarks)
                ? $"{action}d by Security Head"
                : remarks.Trim();
        }
    }
}
