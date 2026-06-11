namespace AlMuhasib.Core.Interfaces.Services;

public interface ILocalQueryService
{
    Task<LocalQueryResult> ExecuteAsync(string queryKey, CancellationToken cancellationToken = default);
    IReadOnlyList<LocalQueryDefinition> GetAvailableQueries();
}

public class LocalQueryDefinition
{
    public string Key { get; init; } = string.Empty;
    public string Question { get; init; } = string.Empty;
    public string Icon { get; init; } = "HelpCircle";
}

public class LocalQueryResult
{
    public string Title { get; set; } = string.Empty;
    public string Summary { get; set; } = string.Empty;
    public List<string> Lines { get; set; } = [];
}
