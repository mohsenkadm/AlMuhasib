using AlMuhasib.Cloud.Application.Models;
using AlMuhasib.Cloud.Application.Models.Mobile;

namespace AlMuhasib.Cloud.Application.Abstractions;

public interface ICloudMobileWriteService
{
    Task<MobileWriteResponse> UpsertCustomerAsync(int tenantId, CreateCustomerRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> UpsertSupplierAsync(int tenantId, CreateSupplierRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> UpsertProductAsync(int tenantId, CreateProductRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> UpsertInvestorAsync(int tenantId, CreateInvestorRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> UpsertPricingTypeAsync(int tenantId, UpsertPricingTypeRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> DeletePricingTypeAsync(int tenantId, Guid syncId, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> UpsertProductPriceAsync(int tenantId, UpsertProductPriceRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> DeleteProductPriceAsync(int tenantId, Guid syncId, string username, CancellationToken ct = default);
    Task<BusinessSettingsDto> UpdateBusinessSettingsAsync(int tenantId, UpdateBusinessSettingsRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> CreateInvoiceAsync(int tenantId, CreateInvoiceRequest request, string username, CancellationToken ct = default);
}
