using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Ux;

public class SystemProfile
{
    public ApplicationSystemType? SelectedSystem { get; set; }
    public DateTime? SelectedAt { get; set; }
    public DeploymentMode DeploymentMode { get; set; } = DeploymentMode.Standalone;
    public string? BranchDisplayName { get; set; }

    /// <summary>
    /// Duhok branch white-label (Express). Set only on first-run setup; missing on existing profiles means false.
    /// </summary>
    public bool IsDuhokBranch { get; set; }

    public bool IsConfigured => SelectedSystem.HasValue;

    public bool IsBranchClient => DeploymentMode == DeploymentMode.BranchClient;
    public bool IsMainServer => DeploymentMode == DeploymentMode.MainServer;
    public bool IsStandalone => DeploymentMode == DeploymentMode.Standalone;
    public bool IsSharedServer => DeploymentMode == DeploymentMode.SharedServer;
}
