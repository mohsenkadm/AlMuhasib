using AlMuhasib.Core.Entities.Car;
using AlMuhasib.Core.Enums;

namespace AlMuhasib.UI.ViewModels.Car;

public sealed class CarContractDetailDisplay
{
    public int Id { get; init; }
    public string ContractNumber { get; init; } = string.Empty;
    public string ContractDate { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public string SellerName { get; init; } = string.Empty;
    public string SellerPhone { get; init; } = string.Empty;
    public string SellerAddress { get; init; } = string.Empty;
    public string SellerIdNumber { get; init; } = string.Empty;
    public string SellerIdDate { get; init; } = string.Empty;
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerPhone { get; init; } = string.Empty;
    public string BuyerAddress { get; init; } = string.Empty;
    public string BuyerIdNumber { get; init; } = string.Empty;
    public string BuyerIdDate { get; init; } = string.Empty;
    public string AnnualOwnerName { get; init; } = string.Empty;
    public string AnnualOwnerAddress { get; init; } = string.Empty;
    public string PlateNumber { get; init; } = string.Empty;
    public string CarType { get; init; } = string.Empty;
    public string CarModel { get; init; } = string.Empty;
    public string CarColor { get; init; } = string.Empty;
    public string ChassisNumber { get; init; } = string.Empty;
    public string CarPrice { get; init; } = string.Empty;
    public string AmountReceived { get; init; } = string.Empty;
    public string RemainingAmount { get; init; } = string.Empty;
    public string CarPriceInWords { get; init; } = string.Empty;
    public string Notes { get; init; } = string.Empty;

    public static CarContractDetailDisplay FromEntity(CarSaleContract c) => new()
    {
        Id = c.Id,
        ContractNumber = c.ContractNumber,
        ContractDate = c.ContractDate.ToString("yyyy/MM/dd"),
        Status = GetStatusLabel(c.Status),
        SellerName = Display(c.SellerName),
        SellerPhone = Display(c.SellerPhone),
        SellerAddress = Display(c.SellerAddress),
        SellerIdNumber = Display(c.SellerIdNumber),
        SellerIdDate = FormatDate(c.SellerIdDate),
        BuyerName = Display(c.BuyerName),
        BuyerPhone = Display(c.BuyerPhone),
        BuyerAddress = Display(c.BuyerAddress),
        BuyerIdNumber = Display(c.BuyerIdNumber),
        BuyerIdDate = FormatDate(c.BuyerIdDate),
        AnnualOwnerName = Display(c.AnnualOwnerName),
        AnnualOwnerAddress = Display(c.AnnualOwnerAddress),
        PlateNumber = Display(c.PlateNumber),
        CarType = Display(c.CarType),
        CarModel = Display(c.CarModel),
        CarColor = Display(c.CarColor),
        ChassisNumber = Display(c.ChassisNumber),
        CarPrice = c.CarPrice.ToString("N0"),
        AmountReceived = c.AmountReceived.ToString("N0"),
        RemainingAmount = c.RemainingAmount.ToString("N0"),
        CarPriceInWords = Display(c.CarPriceInWords),
        Notes = Display(c.Notes)
    };

    private static string Display(string? value) =>
        string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

    private static string FormatDate(DateTime? date) =>
        date?.ToString("yyyy/MM/dd") ?? "—";

    private static string GetStatusLabel(CarContractStatus status) => status switch
    {
        CarContractStatus.Completed => "مكتمل",
        CarContractStatus.Cancelled => "ملغى",
        _ => "نشط"
    };
}
