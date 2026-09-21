namespace AlMuhasib.Core.Models.Print;

/// <summary>نموذج طباعة عقد بيع وشراء من فاتورة مبيعات (معرض سيارات).</summary>
public sealed class ShowroomSaleContractPrintModel
{
    public string ContractNumber { get; init; } = string.Empty;
    public DateTime ContractDate { get; init; } = DateTime.Now;
    public string City { get; init; } = string.Empty;

    // الطرف الأول — من إعدادات الطباعة / الشركة
    public string SellerName { get; init; } = string.Empty;
    public string SellerPhone { get; init; } = string.Empty;
    public string SellerAddress { get; init; } = string.Empty;
    public string SellerIdNumber { get; init; } = string.Empty;
    public string SellerIdIssuer { get; init; } = string.Empty;
    public string AnnualRegistrationNote { get; init; } = "مطابق";

    // الطرف الثاني — زبون الفاتورة
    public string BuyerName { get; init; } = string.Empty;
    public string BuyerPhone { get; init; } = string.Empty;
    public string BuyerAddress { get; init; } = string.Empty;
    public string BuyerIdNumber { get; init; } = string.Empty;
    public string BuyerIdIssuer { get; init; } = string.Empty;

    // السيارة
    public string VehicleName { get; init; } = string.Empty;
    public string PlateNumber { get; init; } = string.Empty;
    public string ChassisNumber { get; init; } = string.Empty;
    public string VehicleType { get; init; } = string.Empty;
    public string VehicleColor { get; init; } = string.Empty;
    public string VehicleModel { get; init; } = string.Empty;
    public string VehicleSize { get; init; } = string.Empty;
    public string PlateType { get; init; } = string.Empty;

    // المبالغ
    public decimal TotalAmount { get; init; }
    public string TotalAmountInWords { get; init; } = string.Empty;
    public decimal PaidAmount { get; init; }
    public decimal RemainingAmount { get; init; }
    public DateTime? DueDate { get; init; }
}
