using AlMuhasib.Cloud.Application.Models.Mobile;

namespace AlMuhasib.Cloud.Application.Abstractions;

public interface ICloudMobileWriteService
{
    Task<MobileWriteResponse> UpsertCustomerAsync(int tenantId, CreateCustomerRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> UpsertSupplierAsync(int tenantId, CreateSupplierRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> UpsertProductAsync(int tenantId, CreateProductRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> UpsertInvestorAsync(int tenantId, CreateInvestorRequest request, string username, CancellationToken ct = default);
    Task<MobileWriteResponse> CreateInvoiceAsync(int tenantId, CreateInvoiceRequest request, string username, CancellationToken ct = default);
}
