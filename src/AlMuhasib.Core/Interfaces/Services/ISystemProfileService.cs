using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ISystemProfileService
{
    SystemProfile Current { get; }
    bool IsFirstRun { get; }
    ApplicationSystemType ActiveSystem { get; }
    string ActiveDatabaseName { get; }
    DeploymentMode DeploymentMode => Current.DeploymentMode;
    bool IsBranchClient => Current.IsBranchClient;
    bool IsMainServer => Current.IsMainServer;
    bool IsStandalone => Current.IsStandalone;
    void SaveSelection(ApplicationSystemType system, DeploymentMode deploymentMode = DeploymentMode.Standalone, string? branchDisplayName = null);
    void ChangeSystem(ApplicationSystemType system);
    void UpdateDeploymentMode(DeploymentMode mode, string? branchDisplayName = null);
}
