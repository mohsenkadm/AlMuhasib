using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IGlobalSearchService
{
    Task<IReadOnlyList<GlobalSearchHit>> SearchAsync(string term, int maxResults = 30, CancellationToken cancellationToken = default);
}
