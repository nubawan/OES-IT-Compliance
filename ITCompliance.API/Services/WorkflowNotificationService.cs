using ITCompliance.API.Data;
using ITCompliance.API.Models;
using Microsoft.EntityFrameworkCore;

namespace ITCompliance.API.Services
{
    // Resolves recipients per stage (via RoleAssignment + OESEmployees,
    // same pattern as ClaimsPrincipalExtensions/AccountController use
    // for role resolution) and sends + logs each notification. Every
    // public method swallows its own exceptions - a bad query or SMTP
    // failure can never break the approval action that triggered it.
    public class WorkflowNotificationService : IWorkflowNotificationService
    {
        private readonly AppDbContext _context;
        private readonly IEmailSender _emailSender;
        private readonly IConfiguration _configuration;
        private readonly ILogger<WorkflowNotificationService> _logger;

        public WorkflowNotificationService(
            AppDbContext context,
            IEmailSender emailSender,
            IConfiguration configuration,
            ILogger<WorkflowNotificationService> logger)
        {
            _context = context;
            _emailSender = emailSender;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task NotifySubmittedAsync(InternetAccessRequest request)
        {
            try
            {
                await SendToEmployeeAsync(
                    request,
                    "Submitted",
                    "Your internet access request has been submitted",
                    $"Your request for <strong>{Encode(request.Website)}</strong> " +
                    "has been submitted and is now awaiting approval.");

                var recipients = await GetStageRecipientsAsync(request);

                await SendAndLogAsync(
                    request,
                    recipients,
                    "Submitted",
                    "New internet access request awaiting your approval",
                    BuildRequestSummaryHtml(
                        request,
                        "A new internet access request needs your approval."));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "NotifySubmittedAsync failed for request {RequestId}",
                    request.Id);
            }
        }

        public async Task NotifyAdvancedAsync(
            InternetAccessRequest request,
            string completedStageStatus)
        {
            try
            {
                await SendToEmployeeAsync(
                    request,
                    completedStageStatus,
                    "Your internet access request has moved forward",
                    $"Your request for <strong>{Encode(request.Website)}</strong> " +
                    $"was approved at one stage and is now: <strong>{Encode(request.Status)}</strong>.");

                var recipients = await GetStageRecipientsAsync(request);

                await SendAndLogAsync(
                    request,
                    recipients,
                    request.Status,
                    "Internet access request awaiting your approval",
                    BuildRequestSummaryHtml(
                        request,
                        "An internet access request needs your approval."));
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "NotifyAdvancedAsync failed for request {RequestId}",
                    request.Id);
            }
        }

        public async Task NotifyFinalDecisionAsync(
            InternetAccessRequest request,
            string decidedAtStage)
        {
            try
            {
                var outcome = request.Status == RequestStatus.Approved
                    ? "approved"
                    : "rejected";

                await SendToEmployeeAsync(
                    request,
                    "FinalDecision",
                    $"Your internet access request has been {outcome}",
                    $"Your request for <strong>{Encode(request.Website)}</strong> " +
                    $"has been <strong>{outcome}</strong>.");

                // Only IT's HOD is the terminal decision-maker for
                // every route - other stages rejecting early don't
                // need to intimate IT Officer/Security Head, they
                // were never involved.
                if (decidedAtStage == RequestStatus.AwaitingItHod)
                {
                    var recipients = await GetJointStageEmailsAsync();

                    await SendAndLogAsync(
                        request,
                        recipients,
                        "FinalDecision",
                        $"IT HOD has {outcome} an internet access request",
                        BuildRequestSummaryHtml(
                            request,
                            $"For your information - IT's HOD has {outcome} this request."));
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "NotifyFinalDecisionAsync failed for request {RequestId}",
                    request.Id);
            }
        }

        private Task<List<string>> GetStageRecipientsAsync(InternetAccessRequest request)
        {
            return request.Status == RequestStatus.AwaitingItOfficerOrSecurityHead
                ? GetJointStageEmailsAsync()
                : GetHodEmailsAsync(request.PendingDepartmentCode);
        }

        private async Task<List<string>> GetJointStageEmailsAsync()
        {
            var roles = new[] { RoleNames.ITOfficer, RoleNames.SecurityHead };

            var empIds = await _context.RoleAssignments
                .AsNoTracking()
                .Where(r => roles.Contains(r.Role) && r.IsActive)
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToListAsync();

            return await ResolveEmailsAsync(empIds);
        }

        private async Task<List<string>> GetHodEmailsAsync(string? departmentCode)
        {
            var empIds = await _context.RoleAssignments
                .AsNoTracking()
                .Where(r =>
                    r.Role == RoleNames.HOD &&
                    r.IsActive &&
                    (r.DepartmentCode == departmentCode || r.DepartmentCode == null))
                .Select(r => r.EmployeeId)
                .Distinct()
                .ToListAsync();

            return await ResolveEmailsAsync(empIds);
        }

        private async Task<List<string>> ResolveEmailsAsync(List<string> empIds)
        {
            if (empIds.Count == 0)
            {
                return new List<string>();
            }

            return await _context.OESEmployees
                .AsNoTracking()
                .Where(e => empIds.Contains(e.EmpId) && e.Email != "")
                .Select(e => e.Email)
                .ToListAsync();
        }

        private async Task SendToEmployeeAsync(
            InternetAccessRequest request,
            string purpose,
            string subject,
            string bodyHtml)
        {
            if (string.IsNullOrWhiteSpace(request.EmployeeEmail))
            {
                return;
            }

            await SendAndLogAsync(
                request,
                new[] { request.EmployeeEmail },
                purpose,
                subject,
                bodyHtml);
        }

        private async Task SendAndLogAsync(
            InternetAccessRequest request,
            IEnumerable<string> recipients,
            string purpose,
            string subject,
            string bodyHtml)
        {
            var recipientList = recipients.ToList();

            if (recipientList.Count == 0)
            {
                _logger.LogWarning(
                    "No recipients resolved for request {RequestId}, purpose {Purpose}",
                    request.Id,
                    purpose);
                return;
            }

            var (success, error) = await _emailSender.SendAsync(
                recipientList,
                subject,
                bodyHtml);

            foreach (var recipient in recipientList)
            {
                _context.EmailLogs.Add(new EmailLog
                {
                    RequestId = request.Id,
                    RecipientEmail = recipient,
                    Purpose = purpose,
                    Subject = subject,
                    Success = success,
                    ErrorMessage = error
                });
            }

            await _context.SaveChangesAsync();
        }

        private static string Encode(string? value) =>
            System.Net.WebUtility.HtmlEncode(value ?? "");

        private static string BuildRequestSummaryHtml(
            InternetAccessRequest request,
            string intro)
        {
            return $"""
                <p>{intro}</p>
                <ul>
                    <li>Employee: {Encode(request.EmployeeId)}</li>
                    <li>Website: {Encode(request.Website)}</li>
                    <li>Reason: {Encode(request.Reason)}</li>
                    <li>Duration: {Encode(request.Duration)}</li>
                </ul>
                """;
        }
    }
}
