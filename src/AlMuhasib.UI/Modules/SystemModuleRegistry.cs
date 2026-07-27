using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Interfaces.Services;
using AlMuhasib.UI.Services;

namespace AlMuhasib.UI.Modules;

public sealed class SystemModuleRegistry
{
    private readonly ISystemProfileService _profile;
    private readonly AccountingSystemModule _accounting;
    private readonly CarContractsSystemModule _carContracts;
    private readonly CarTradingSystemModule _carTrading;
    private readonly HotelSystemModule _hotel;
    private readonly RealEstateContractsSystemModule _realEstate;

    public SystemModuleRegistry(ISystemProfileService profile)
    {
        _profile = profile;
        _accounting = new AccountingSystemModule();
        _carContracts = new CarContractsSystemModule();
        _carTrading = new CarTradingSystemModule();
        _hotel = new HotelSystemModule();
        _realEstate = new RealEstateContractsSystemModule();
    }

    public ISystemModule ActiveModule => _profile.ActiveSystem switch
    {
        ApplicationSystemType.CarContracts => _carContracts,
        ApplicationSystemType.CarTrading => _carTrading,
        ApplicationSystemType.HotelManagement => _hotel,
        ApplicationSystemType.RealEstateContracts => _realEstate,
        _ => _accounting
    };

    public bool IsCarContracts => _profile.ActiveSystem == ApplicationSystemType.CarContracts;
    public bool IsCarTrading => _profile.ActiveSystem == ApplicationSystemType.CarTrading;
    public bool IsHotelManagement => _profile.ActiveSystem == ApplicationSystemType.HotelManagement;
    public bool IsRealEstateContracts => _profile.ActiveSystem == ApplicationSystemType.RealEstateContracts;
    public bool IsAccounting => _profile.ActiveSystem == ApplicationSystemType.Accounting;
}
