namespace AlMuhasib.Sync.Responses;

public sealed class BranchInfoDto
{
    public int BranchId { get; set; }
    public Guid SyncId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public bool IsMain { get; set; }
    public bool IsDefault { get; set; }
}

public sealed class TenantLoginResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public string RefreshToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public int TenantId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string TenantName { get; set; } = string.Empty;
    public int ApplicationSystemType { get; set; }
    public bool IsMobileEnabled { get; set; }
    public DateTime? LicenseExpiresAt { get; set; }
    public DateTime? AccountExpiresAt { get; set; }

    /// <summary>الفروع المسموحة. إن كان واحداً يُختار تلقائياً على العميل.</summary>
    public List<BranchInfoDto> AllowedBranches { get; set; } = [];

    public int? DefaultBranchId { get; set; }
    public bool RequiresBranchSelection { get; set; }
    public bool CanViewAllBranches { get; set; }
    public bool CanManageAllBranches { get; set; }

    /// <summary>يُملأ عند اختيار فرع (أو فرع وحيد) — JWT يتضمن branch_id.</summary>
    public int? CurrentBranchId { get; set; }
}

public sealed class SelectBranchRequest
{
    public int BranchId { get; set; }

    /// <summary>وضع كل الفروع — للتقارير فقط، يتطلب صلاحية.</summary>
    public bool AllBranches { get; set; }
}

public sealed class SelectBranchResponse
{
    public string AccessToken { get; set; } = string.Empty;
    public DateTime AccessTokenExpiresAt { get; set; }
    public int? CurrentBranchId { get; set; }
    public bool IsAllBranchesMode { get; set; }
    public BranchInfoDto? Branch { get; set; }
}

public sealed class LicenseStatusResponse
{
    public bool IsActive { get; set; }
    public bool IsMobileEnabled { get; set; }
    public DateTime? LicenseExpiresAt { get; set; }
    public DateTime? AccountExpiresAt { get; set; }
    public string? StatusCode { get; set; }
    public string? Message { get; set; }
}

public sealed class ApiErrorResponse
{
    public string Code { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}
