namespace ITCompliance.API.Models
{
    // Workflow (department-conditional, see Services/WorkflowRouter.cs):
    //   Requester in the IT department (Workflow:ItDepartmentCode):
    //     AwaitingItOfficerOrSecurityHead -> AwaitingItHod -> Approved
    //   Requester in any other department:
    //     AwaitingOwnHod -> AwaitingItOfficerOrSecurityHead -> AwaitingItHod -> Approved
    // Any stage can reject; rejection ends the request immediately.
    // Who acted at each stage (and any remarks) is recorded in
    // RequestTransaction, not in per-stage columns on the request itself.
    public static class RequestStatus
    {
        public const string AwaitingOwnHod = "Awaiting Department HOD";

        // Label says "and" since both roles are notified/involved, but
        // the rule is OR: either one approving advances the request.
        public const string AwaitingItOfficerOrSecurityHead = "Awaiting IT Officer and Security Head";

        public const string AwaitingItHod = "Awaiting IT HOD";

        public const string Approved = "Approved";
        public const string Rejected = "Rejected";

        public static bool IsRejected(string? status) =>
            status == Rejected;

        public static bool IsInProgress(string? status) =>
            status is AwaitingOwnHod or AwaitingItOfficerOrSecurityHead
                or AwaitingItHod;
    }

    public record Kpi(string Label, int Count, string Tone);
}
