using AlMuhasib.Core.Enums;

namespace AlMuhasib.Core.Helpers;

public sealed class MaintenanceStatusOption
{
    public MaintenanceStatus? Value { get; init; }
    public string Label { get; init; } = string.Empty;
}

public static class MaintenanceStatusHelper
{
    public static string ToArabic(MaintenanceStatus status) => status switch
    {
        MaintenanceStatus.Working => "يعمل",
        MaintenanceStatus.NotWorking => "لا يعمل",
        MaintenanceStatus.UnderRepair => "تحت الصيانة",
        MaintenanceStatus.Delivered => "تم التسليم",
        _ => status.ToString()
    };

    public static IReadOnlyList<MaintenanceStatusOption> All { get; } =
    [
        new() { Value = MaintenanceStatus.Working, Label = "يعمل" },
        new() { Value = MaintenanceStatus.NotWorking, Label = "لا يعمل" },
        new() { Value = MaintenanceStatus.UnderRepair, Label = "تحت الصيانة" },
        new() { Value = MaintenanceStatus.Delivered, Label = "تم التسليم" }
    ];

    public static IReadOnlyList<MaintenanceStatusOption> FilterOptions { get; } =
    [
        new() { Value = null, Label = "الكل" },
        new() { Value = MaintenanceStatus.Working, Label = "يعمل" },
        new() { Value = MaintenanceStatus.NotWorking, Label = "لا يعمل" },
        new() { Value = MaintenanceStatus.UnderRepair, Label = "تحت الصيانة" },
        new() { Value = MaintenanceStatus.Delivered, Label = "تم التسليم" }
    ];
}
