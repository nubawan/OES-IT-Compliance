using System.Security.Claims;
using ITCompliance.API.Data;
using ITCompliance.API.Models;
using ITCompliance.API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Controllers
{
    // Serves two "hats": a department's own HOD (first stage, for
    // requesters outside the IT department) and IT's HOD (the final,
    // universal approval stage for every request - see
    // Services/WorkflowRouter.cs). Both are just the HOD role scoped
    // to different departments via RoleAssignment.
    [Authorize(Roles = RoleNames.HOD)]
    public class HODController : Controller
    {
        private readonly AppDbContext _context;
        private readonly IWorkflowNotificationService _notifications;
        private readonly IConfiguration _configuration;

        public HODController(
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
            var deptCodes = User.GetDepartmentScopes(RoleNames.HOD);

            var query = _context.InternetAccessRequests
                .Where(r =>
                    r.Status == RequestStatus.AwaitingOwnHod ||
                    r.Status == RequestStatus.AwaitingItHod);

            if (deptCodes.Count > 0)
            {
                query = query.Where(r => deptCodes.Contains(r.PendingDepartmentCode));
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
            var awaitingQuery = _context.InternetAccessRequests
                .Where(r =>
                    r.Status == RequestStatus.AwaitingOwnHod ||
                    r.Status == RequestStatus.AwaitingItHod);

            if (deptCodes.Count > 0)
            {
                awaitingQuery = awaitingQuery.Where(
                    r => deptCodes.Contains(r.PendingDepartmentCode));
            }

            var awaitingCount = await awaitingQuery.CountAsync();

            var actedCounts = await _context.RequestTransactions
                .Where(t => t.ActorRole == RoleNames.HOD)
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

                if (!IsValidHodStage(request) || !IsInMyScope(request))
                {
                    TempData["ErrorMessage"] =
                        "This request is not available for your approval.";
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

                if (request.Status == RequestStatus.Approved)
                {
                    await _notifications.NotifyFinalDecisionAsync(request, completedStage);
                }
                else
                {
                    await _notifications.NotifyAdvancedAsync(request, completedStage);
                }

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

                if (!IsValidHodStage(request) || !IsInMyScope(request))
                {
                    TempData["ErrorMessage"] =
                        "This request is not available for your approval.";
                    return RedirectToAction(nameof(Dashboard));
                }

                var rejectedAtStage = request.Status;

                RecordTransaction(request, rejectedAtStage, TransactionAction.Reject, remarks);

                request.Status = RequestStatus.Rejected;
                request.UpdatedAt = DateTime.Now;

                await _context.SaveChangesAsync();

                await _notifications.NotifyFinalDecisionAsync(request, rejectedAtStage);

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

        private static bool IsValidHodStage(InternetAccessRequest request) =>
            request.Status == RequestStatus.AwaitingOwnHod ||
            request.Status == RequestStatus.AwaitingItHod;

        private bool IsInMyScope(InternetAccessRequest request)
        {
            var deptCodes = User.GetDepartmentScopes(RoleNames.HOD);

            return deptCodes.Count == 0 ||
                deptCodes.Contains(request.PendingDepartmentCode);
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
                ActorRole = RoleNames.HOD,
                Action = action,
                Remarks = string.IsNullOrWhiteSpace(remarks) ? null : remarks.Trim()
            });

            // Legacy column, kept for continuity/visibility on the
            // request itself - RequestTransaction is the source of truth.
            request.HODRemarks = string.IsNullOrWhiteSpace(remarks)
                ? $"{action}d by HOD"
                : remarks.Trim();
        }
    }
}
