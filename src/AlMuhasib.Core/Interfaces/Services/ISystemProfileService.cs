using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;

namespace AlMuhasib.Core.Interfaces.Services;

public interface ISystemProfileService
{
    SystemProfile Current { get; }
    bool IsFirstRun { get; }
    ApplicationSystemType ActiveSystem { get; }
    string ActiveDatabaseName { get; }
    void SaveSelection(ApplicationSystemType system);
    void ChangeSystem(ApplicationSystemType system);
}
