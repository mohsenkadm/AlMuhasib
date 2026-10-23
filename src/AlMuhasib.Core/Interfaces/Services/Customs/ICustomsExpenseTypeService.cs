using AlMuhasib.Core.Models.Customs;

namespace AlMuhasib.Core.Interfaces.Services.Customs;

public interface ICustomsExpenseTypeService
{
    Task<IReadOnlyList<CustomsExpenseTypeItem>> GetActiveAsync(CancellationToken cancellationToken = default);

    Task<CustomsExpenseTypeItem> CreateAsync(string name, string createdBy, CancellationToken cancellationToken = default);
}
