namespace ITCompliance.API.Models
{
    // Workflow: Pending -> IT Officer -> HOD -> Security Head -> Boss.
    // Any stage can reject; rejection ends the request.
    public static class RequestStatus
    {
        public const string Pending = "Pending";

        public const string ItOfficerApproved = "IT Officer Approved";
        public const string HodApproved = "HOD Approved";
        public const string SecurityHeadApproved = "Security Head Approved";
        public const string BossApproved = "Boss Approved";

        public const string Rejected = "Rejected";
        public const string RejectedByHod = "Rejected by HOD";
        public const string RejectedBySecurityHead = "Rejected by Security Head";
        public const string RejectedByBoss = "Rejected by Boss";

        public static bool IsRejected(string? status) =>
            status != null && status.StartsWith("Rejected");

        public static bool IsInProgress(string? status) =>
            status is ItOfficerApproved or HodApproved
                or SecurityHeadApproved;
    }

    public record Kpi(string Label, int Count, string Tone);
}
