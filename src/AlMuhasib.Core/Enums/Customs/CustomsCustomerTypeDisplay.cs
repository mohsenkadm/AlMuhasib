namespace AlMuhasib.Core.Enums.Customs;

public static class CustomsCustomerTypeDisplay
{
    public static string ToArabic(CustomsCustomerType type) => type switch
    {
        CustomsCustomerType.Individual => "فرد",
        CustomsCustomerType.Company => "شركة",
        _ => type.ToString()
    };
}

public static class CustomsInvoiceStatusDisplay
{
    public static string ToArabic(CustomsInvoiceStatus status) => status switch
    {
        CustomsInvoiceStatus.Draft => "مسودة",
        CustomsInvoiceStatus.Approved => "معتمدة",
        CustomsInvoiceStatus.Cancelled => "ملغاة",
        _ => status.ToString()
    };
}

public static class CustomsServiceTypeDisplay
{
    public static string ToArabic(CustomsServiceType type) => type switch
    {
        CustomsServiceType.Clearance => "تخليص",
        CustomsServiceType.Transport => "نقل",
        CustomsServiceType.ClearanceAndTransport => "تخليص ونقل",
        _ => type.ToString()
    };
}

public static class CustomsVehicleStatusDisplay
{
    public static string ToArabic(CustomsVehicleStatus status) => status switch
    {
        CustomsVehicleStatus.InProgress => "قيد الإجراء",
        CustomsVehicleStatus.Exited => "خارجة",
        CustomsVehicleStatus.Arrived => "واصلة",
        _ => status.ToString()
    };
}

public static class CustomsPaymentMethodDisplay
{
    public static string ToArabic(CustomsPaymentMethod method) => method switch
    {
        CustomsPaymentMethod.Cash => "نقداً",
        CustomsPaymentMethod.Bank => "مصرف",
        CustomsPaymentMethod.Other => "أخرى",
        _ => method.ToString()
    };
}
