using AlMuhasib.Core.Models.Import;

namespace AlMuhasib.Core.Interfaces.Services;

public interface IDataImportService
{
    Task<DataImportPreview> PreviewCustomersAsync(string filePath);
    Task<DataImportResult> ImportCustomersAsync(string filePath);
    Task<DataImportPreview> PreviewSuppliersAsync(string filePath);
    Task<DataImportResult> ImportSuppliersAsync(string filePath);
    Task<DataImportPreview> PreviewProductsAsync(string filePath, ProductImportOptions? options = null);
    Task<DataImportResult> ImportProductsAsync(string filePath, ProductImportOptions? options = null);
    void SaveCustomerTemplate(string filePath);
    void SaveSupplierTemplate(string filePath);
    void SaveProductTemplate(string filePath, ProductImportOptions? options = null);
}

public class DataImportPreview
{
    public int RowCount { get; set; }
    public List<string> Headers { get; set; } = [];
    public List<string> SampleRows { get; set; } = [];
}

public class DataImportResult
{
    public int ImportedCount { get; set; }
    public int SkippedCount { get; set; }
    public List<string> Errors { get; set; } = [];
}
