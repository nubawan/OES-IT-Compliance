using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ITCompliance.API.Models
{
    public static class TransactionAction
    {
        public const string Approve = "Approve";
        public const string Reject = "Reject";
    }

    // Audit trail: one row per approve/reject action taken on a
    // request. Replaces the old per-stage remarks columns on
    // InternetAccessRequest for anything going through the new
    // workflow - those columns stay for historical data only.
    public class RequestTransaction
    {
        [Key]
        public int Id { get; set; }

        public int RequestId { get; set; }

        [ForeignKey(nameof(RequestId))]
        public InternetAccessRequest? Request { get; set; }

        // The status the request was in when this action happened
        // (e.g. RequestStatus.AwaitingItOfficerOrSecurityHead).
        public string StageStatus { get; set; } = string.Empty;

        public string ActorEmpId { get; set; } = string.Empty;

        // The role the actor acted as (RoleNames.HOD/ITOfficer/SecurityHead).
        public string ActorRole { get; set; } = string.Empty;

        // TransactionAction.Approve or TransactionAction.Reject.
        public string Action { get; set; } = string.Empty;

        public string? Remarks { get; set; }

        public DateTime ActionedAt { get; set; } = DateTime.Now;
    }
}
