namespace AlMuhasib.Core.Models.Ux;

/// <summary>
/// Represents a SQL Server / LocalDB instance discovered on the local machine.
/// </summary>
public sealed class SqlServerInstanceInfo
{
    public required string DataSource { get; init; }
    public required string DisplayName { get; init; }
    public string? InstanceName { get; init; }
    public bool IsLocalDb { get; init; }
    public bool IsDefaultInstance { get; init; }
    public string Source { get; init; } = "Detected";

    public override string ToString() => DisplayName;

    public override bool Equals(object? obj) =>
        obj is SqlServerInstanceInfo other
        && string.Equals(DataSource, other.DataSource, StringComparison.OrdinalIgnoreCase);

    public override int GetHashCode() =>
        StringComparer.OrdinalIgnoreCase.GetHashCode(DataSource);
}
