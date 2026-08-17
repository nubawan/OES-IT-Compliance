using ITCompliance.API.Models;

namespace ITCompliance.API.Services
{
    // Pure routing logic, no DB access. Two route shapes only - see
    // Models/RequestStatus.cs for the stage constants.
    public static class WorkflowRouter
    {
        public static (string Status, string? PendingDepartmentCode) GetInitialStage(
            string requesterDepartmentCode,
            string itDepartmentCode)
        {
            if (string.Equals(
                    requesterDepartmentCode,
                    itDepartmentCode,
                    StringComparison.OrdinalIgnoreCase))
            {
                // Already in the IT department - own-dept HOD stage
                // would just be the same person as the final stage.
                return (RequestStatus.AwaitingItOfficerOrSecurityHead, null);
            }

            return (RequestStatus.AwaitingOwnHod, requesterDepartmentCode);
        }

        // Given the stage that just got approved, returns the next
        // one. The two routes converge after the first stage, so this
        // doesn't need to know the requester's department.
        public static (string Status, string? PendingDepartmentCode) GetNextStage(
            string currentStatus,
            string itDepartmentCode)
        {
            return currentStatus switch
            {
                RequestStatus.AwaitingOwnHod =>
                    (RequestStatus.AwaitingItOfficerOrSecurityHead, (string?)null),

                RequestStatus.AwaitingItOfficerOrSecurityHead =>
                    (RequestStatus.AwaitingItHod, itDepartmentCode),

                RequestStatus.AwaitingItHod =>
                    (RequestStatus.Approved, (string?)null),

                _ => throw new InvalidOperationException(
                    $"Cannot advance from status '{currentStatus}'.")
            };
        }
    }
}
