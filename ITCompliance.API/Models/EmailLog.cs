using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITCompliance.API.Models
{
    // Delivery auditing only - separate concern from RequestTransaction
    // (which records the approval decisions themselves).
    public class EmailLog
    {
        [Key]
        public int Id { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public InternetAccessRequest? Request { get; set; }

        public string RecipientEmail { get; set; } = string.Empty;

        // What this email was about, e.g. "Submitted",
        // RequestStatus.AwaitingItHod, "FinalDecision".
        public string Purpose { get; set; } = string.Empty;

        public string Subject { get; set; } = string.Empty;

        public bool Success { get; set; }

        public string? ErrorMessage { get; set; }

        public DateTime SentAt { get; set; } = DateTime.Now;
    }
}
