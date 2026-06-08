using AlMuhasib.Core.Entities;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IUserNoteService
{
    Task<IReadOnlyList<UserNote>> GetAllAsync();
    Task<UserNote> CreateAsync(string? title = null);
    Task UpdateAsync(int id, string title, string content);
    Task DeleteAsync(int id);
}
