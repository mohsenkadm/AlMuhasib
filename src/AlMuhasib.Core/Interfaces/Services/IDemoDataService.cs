namespace AlMuhasib.Core.Interfaces.Services;

public interface IDemoDataService
{
    Task<DemoDataSeedResult> TrySeedAsync(CancellationToken cancellationToken = default);
}

public class DemoDataSeedResult
{
    public bool Success { get; init; }
    public string Message { get; init; } = string.Empty;
    public int ProductsCreated { get; init; }
    public int CustomersCreated { get; init; }
}
