using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;

namespace ITCompliance.API.Services
{
    public class SmtpEmailSender : IEmailSender
    {
        private readonly EmailOptions _options;
        private readonly ILogger<SmtpEmailSender> _logger;

        public SmtpEmailSender(
            IOptions<EmailOptions> options,
            ILogger<SmtpEmailSender> logger)
        {
            _options = options.Value;
            _logger = logger;
        }

        public async Task<(bool Success, string? ErrorMessage)> SendAsync(
            IEnumerable<string> toEmails,
            string subject,
            string bodyHtml,
            CancellationToken ct = default)
        {
            var recipients = toEmails
                .Where(e => !string.IsNullOrWhiteSpace(e))
                .Distinct()
                .ToList();

            if (recipients.Count == 0)
            {
                _logger.LogWarning(
                    "SendAsync called with no recipients - subject: {Subject}",
                    subject);
                return (false, "No recipients.");
            }

            try
            {
                var actualRecipients = recipients;
                var actualSubject = subject;

                if (_options.DevMode)
                {
                    actualRecipients = new List<string> { _options.DevModeRedirectTo };
                    actualSubject =
                        $"[DEV - would go to: {string.Join(", ", recipients)}] {subject}";
                }

                var message = new MimeMessage();

                message.From.Add(new MailboxAddress(
                    _options.FromName,
                    _options.FromAddress));

                foreach (var to in actualRecipients)
                {
                    message.To.Add(MailboxAddress.Parse(to));
                }

                message.Subject = actualSubject;
                message.Body = new TextPart("html") { Text = bodyHtml };

                using var client = new SmtpClient();

                await client.ConnectAsync(
                    _options.Host,
                    _options.Port,
                    _options.UseStartTls
                        ? SecureSocketOptions.StartTls
                        : SecureSocketOptions.Auto,
                    ct);

                if (!string.IsNullOrEmpty(_options.Username))
                {
                    await client.AuthenticateAsync(
                        _options.Username,
                        _options.Password ?? "",
                        ct);
                }

                await client.SendAsync(message, ct);
                await client.DisconnectAsync(true, ct);

                return (true, null);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Email send failed - subject: {Subject}, recipients: {Recipients}",
                    subject,
                    string.Join(", ", recipients));

                return (false, ex.Message);
            }
        }
    }
}
