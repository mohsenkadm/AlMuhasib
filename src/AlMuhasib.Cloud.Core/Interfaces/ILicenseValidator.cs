using AlMuhasib.Cloud.Core.Entities;

namespace AlMuhasib.Cloud.Core.Interfaces;

public sealed class LicenseValidationResult
{
    public bool IsValid { get; init; }
    public string? ErrorCode { get; init; }
    public string? Message { get; init; }

    public static LicenseValidationResult Ok() => new() { IsValid = true };
    public static LicenseValidationResult Fail(string code, string message) =>
        new() { IsValid = false, ErrorCode = code, Message = message };
}

public interface ILicenseValidator
{
    LicenseValidationResult Validate(Tenant tenant, TenantAccount account);
}
