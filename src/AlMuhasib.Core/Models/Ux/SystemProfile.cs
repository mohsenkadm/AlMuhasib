using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Models.Ux;

public class SystemProfile
{
    public ApplicationSystemType? SelectedSystem { get; set; }
    public DateTime? SelectedAt { get; set; }
    public DeploymentMode DeploymentMode { get; set; } = DeploymentMode.Standalone;
    public string? BranchDisplayName { get; set; }
    public bool IsConfigured => SelectedSystem.HasValue;

    public bool IsBranchClient => DeploymentMode == DeploymentMode.BranchClient;
    public bool IsMainServer => DeploymentMode == DeploymentMode.MainServer;
    public bool IsStandalone => DeploymentMode == DeploymentMode.Standalone;
}
