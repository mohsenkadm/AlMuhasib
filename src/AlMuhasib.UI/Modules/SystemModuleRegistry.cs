using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.Modules;

public sealed class SystemModuleRegistry
{
    private readonly ISystemProfileService _profile;
    private readonly AccountingSystemModule _accounting;
    private readonly CarContractsSystemModule _carContracts;
    private readonly HotelSystemModule _hotel;

    public SystemModuleRegistry(ISystemProfileService profile)
    {
        _profile = profile;
        _accounting = new AccountingSystemModule();
        _carContracts = new CarContractsSystemModule();
        _hotel = new HotelSystemModule();
    }

    public ISystemModule ActiveModule => _profile.ActiveSystem switch
    {
        ApplicationSystemType.CarContracts => _carContracts,
        ApplicationSystemType.HotelManagement => _hotel,
        _ => _accounting
    };

    public bool IsCarContracts => _profile.ActiveSystem == ApplicationSystemType.CarContracts;

    public bool IsHotelManagement => _profile.ActiveSystem == ApplicationSystemType.HotelManagement;
}
