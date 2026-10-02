namespace AlMuhasib.Core.Enums;

public enum DeploymentMode
{
    Standalone = 0,
    MainServer = 1,
    BranchClient = 2,
    /// <summary>
    /// All PCs share one remote SQL Server via ConnectionStrings:DefaultConnection.
    /// </summary>
    SharedServer = 3
}
