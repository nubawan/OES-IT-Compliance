namespace ITCompliance.API.Services
{
    public interface IEmailSender
    {
        // Never throws - failures are logged and swallowed so a
        // bad send can't break the approval action that triggered it.
        // Returns whether the send succeeded, so callers can record
        // an EmailLog row.
        Task<(bool Success, string? ErrorMessage)> SendAsync(
            IEnumerable<string> toEmails,
            string subject,
            string bodyHtml,
            CancellationToken ct = default);
    }
}
