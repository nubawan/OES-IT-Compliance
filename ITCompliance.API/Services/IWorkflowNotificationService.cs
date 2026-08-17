using ITCompliance.API.Models;

namespace ITCompliance.API.Services
{
    public interface IWorkflowNotificationService
    {
        Task NotifySubmittedAsync(InternetAccessRequest request);

        Task NotifyAdvancedAsync(InternetAccessRequest request, string completedStageStatus);

        // decidedAtStage is the stage that produced the terminal
        // outcome (Approved or Rejected) - used to decide whether to
        // also intimate IT Officer/Security Head (only when the
        // decision was made at the IT HOD stage).
        Task NotifyFinalDecisionAsync(InternetAccessRequest request, string decidedAtStage);
    }
}
