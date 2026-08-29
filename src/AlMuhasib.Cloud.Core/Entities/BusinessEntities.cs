using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Enums.Gold;

namespace AlMuhasib.Cloud.Core.Entities;

public class CloudCategory : CloudBaseEntity { public string Name { get; set; } = string.Empty; }
public class CloudProduct : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? Barcode { get; set; }
    public string? ScientificName { get; set; }
    public string? UsageInstructions { get; set; }
    public int CategoryId { get; set; }
    public decimal Weight { get; set; }
    public string? WeightUnit { get; set; }
    public DiscountType DiscountType { get; set; }
    public decimal DiscountValue { get; set; }
    public DateTime? DiscountExpiresAt { get; set; }
    public CloudCategory Category { get; set; } = null!;
    public ICollection<CloudProductPrice> ProductPrices { get; set; } = [];
}
public class CloudPricingType : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public ICollection<CloudProductPrice> ProductPrices { get; set; } = [];
}
public class CloudProductPrice : CloudBaseEntity
{
    public int ProductId { get; set; }
    public int PricingTypeId { get; set; }
    public decimal SalePrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public CloudProduct Product { get; set; } = null!;
    public CloudPricingType PricingType { get; set; } = null!;
}
public class CloudBusinessSettings : CloudBaseEntity
{
    public bool ProductPricingEnabled { get; set; }
    public bool UpdateProductPriceOnPurchase { get; set; }
    public bool PeriodLockEnabled { get; set; }
    public DateTime? LockedThroughDate { get; set; }
}
public class CloudWarehouse : CloudBaseEntity { public string Name { get; set; } = string.Empty; public string? Location { get; set; } }
public class CloudCustomer : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? FileNumber { get; set; }
    public string? Notes { get; set; }
}
public class CloudSupplier : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? Notes { get; set; }
}
public class CloudCashBox : CloudBaseEntity { public string Name { get; set; } = string.Empty; public decimal Balance { get; set; } }
public class CloudBankAccount : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? AccountNumber { get; set; }
    public decimal Balance { get; set; }
}
public class CloudInvestor : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Phone { get; set; }
    public decimal TotalDeposit { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal ProfitPercentage { get; set; }
    public ICollection<CloudInvestorTransaction> Transactions { get; set; } = [];
}
public class CloudExpenseType : CloudBaseEntity { public string Name { get; set; } = string.Empty; }
public class CloudPrintBrandingSettings : CloudBaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string PhonePrimary { get; set; } = string.Empty;
    public string PhoneSecondary { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
    public bool ShowHeaderText { get; set; } = true;
    public bool ShowHeaderImage { get; set; }
    public byte[]? HeaderImageData { get; set; }
    public string? HeaderImageContentType { get; set; }
    public bool ShowFooterText { get; set; } = true;
    public string FooterText { get; set; } = string.Empty;
    public bool ShowFooterImage { get; set; }
    public byte[]? FooterImageData { get; set; }
    public string? FooterImageContentType { get; set; }
}
public class CloudWarehouseStock : CloudBaseEntity
{
    public int WarehouseId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public decimal OpeningQuantity { get; set; }
    public decimal UnitCost { get; set; }
    /// <summary>كمية الحد الأدنى للمنتج في هذا المخزن</summary>
    public decimal MinQuantity { get; set; }
    public CloudWarehouse Warehouse { get; set; } = null!;
    public CloudProduct Product { get; set; } = null!;
}
public class CloudWarehouseTransfer : CloudBaseEntity
{
    public string TransferNumber { get; set; } = string.Empty;
    public int FromWarehouseId { get; set; }
    public int ToWarehouseId { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public CloudWarehouse FromWarehouse { get; set; } = null!;
    public CloudWarehouse ToWarehouse { get; set; } = null!;
    public ICollection<CloudWarehouseTransferItem> Items { get; set; } = [];
}
public class CloudWarehouseTransferItem : CloudBaseEntity
{
    public int WarehouseTransferId { get; set; }
    public int ProductId { get; set; }
    public decimal Quantity { get; set; }
    public CloudWarehouseTransfer WarehouseTransfer { get; set; } = null!;
    public CloudProduct Product { get; set; } = null!;
}
public class CloudInvoice : CloudBaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public InvoiceType InvoiceType { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public int WarehouseId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal NetAmount { get; set; }
    public decimal CompanyFeePercentage { get; set; }
    public decimal CompanyFeeAmount { get; set; }
    public decimal RoundingAmount { get; set; }
    public RoundingType RoundingType { get; set; }
    public int? CashBoxId { get; set; }
    public DateTime Date { get; set; }
    public DateTime? CreditDueDate { get; set; }
    public string? Notes { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsCreditPaid { get; set; }
    public int? RelatedInvoiceId { get; set; }
    public CloudCustomer? Customer { get; set; }
    public CloudSupplier? Supplier { get; set; }
    public CloudWarehouse Warehouse { get; set; } = null!;
    public CloudCashBox? CashBox { get; set; }
    public CloudInvoice? RelatedInvoice { get; set; }
    public ICollection<CloudInvoiceItem> Items { get; set; } = [];
    public ICollection<CloudInstallmentPlan> InstallmentPlans { get; set; } = [];
}
public class CloudInvoiceItem : CloudBaseEntity
{
    public int InvoiceId { get; set; }
    public int? ProductId { get; set; }
    public int? PricingTypeId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalPrice { get; set; }
    public bool IsOfferGift { get; set; }
    public int? OfferId { get; set; }
    public CloudInvoice Invoice { get; set; } = null!;
    public CloudProduct? Product { get; set; }
    public CloudPricingType? PricingType { get; set; }
}

public class CloudProductOffer : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public int TriggerProductId { get; set; }
    public decimal TriggerQuantity { get; set; }
    public int GiftProductId { get; set; }
    public decimal GiftQuantity { get; set; }
    public string? Notes { get; set; }
}

public class CloudInstallmentPlan : CloudBaseEntity
{
    public int InvoiceId { get; set; }
    public int CustomerId { get; set; }
    public string? FileNumber { get; set; }
    public decimal TotalAmount { get; set; }
    public int NumberOfInstallments { get; set; }
    public decimal InstallmentAmount { get; set; }
    public DateTime StartDate { get; set; }
    public InstallmentType InstallmentType { get; set; }
    public decimal CompanyFeePercentage { get; set; }
    public decimal CompanyFeeAmount { get; set; }
    public CloudInvoice Invoice { get; set; } = null!;
    public CloudCustomer Customer { get; set; } = null!;
    public ICollection<CloudInstallment> Installments { get; set; } = [];
}
public class CloudInstallment : CloudBaseEntity
{
    public int InstallmentPlanId { get; set; }
    public DateTime DueDate { get; set; }
    public decimal Amount { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public InstallmentStatus Status { get; set; }
    public DateTime? PaymentDate { get; set; }
    public int? CashBoxId { get; set; }
    public CloudInstallmentPlan InstallmentPlan { get; set; } = null!;
    public CloudCashBox? CashBox { get; set; }
}
public class CloudVoucher : CloudBaseEntity
{
    public string VoucherNumber { get; set; } = string.Empty;
    public VoucherType VoucherType { get; set; }
    public decimal Amount { get; set; }
    public decimal BankFees { get; set; }
    public int? CustomerId { get; set; }
    public int? InvestorId { get; set; }
    public int CashBoxId { get; set; }
    public int? BankAccountId { get; set; }
    public int? InvoiceId { get; set; }
    public int? InstallmentId { get; set; }
    public bool IsReconciled { get; set; }
    public DateTime? ReconciledAt { get; set; }
    public string? ReconciledBy { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
    public CloudCustomer? Customer { get; set; }
    public CloudInvestor? Investor { get; set; }
    public CloudCashBox CashBox { get; set; } = null!;
    public CloudBankAccount? BankAccount { get; set; }
    public CloudInvoice? Invoice { get; set; }
    public CloudInstallment? Installment { get; set; }
}
public class CloudExpense : CloudBaseEntity
{
    public int ExpenseTypeId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public int CashBoxId { get; set; }
    public string? Notes { get; set; }
    public CloudExpenseType ExpenseType { get; set; } = null!;
    public CloudCashBox CashBox { get; set; } = null!;
}
public class CloudTransfer : CloudBaseEntity
{
    public TransferAccountType FromType { get; set; }
    public int FromId { get; set; }
    public TransferAccountType ToType { get; set; }
    public int ToId { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
public class CloudInvestorTransaction : CloudBaseEntity
{
    public int InvestorId { get; set; }
    public InvestorTransactionType Type { get; set; }
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public string? Notes { get; set; }
}
public class CloudProfitDistribution : CloudBaseEntity
{
    public DateTime Date { get; set; }
    public decimal TotalProfit { get; set; }
    public decimal DistributedAmount { get; set; }
}
public class CloudProfitDistributionDetail : CloudBaseEntity
{
    public int ProfitDistributionId { get; set; }
    public int InvestorId { get; set; }
    public decimal ProfitPercentage { get; set; }
    public decimal Amount { get; set; }
    public CloudInvestor Investor { get; set; } = null!;
}
public class CloudCapitalEntry : CloudBaseEntity
{
    public decimal Amount { get; set; }
    public DateTime Date { get; set; }
    public CapitalEntryType Type { get; set; }
    public string? Notes { get; set; }
}
public class CloudCustomerAttachment : CloudBaseEntity
{
    public int CustomerId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string? Description { get; set; }
    public byte[]? FileData { get; set; }
}

// Hotel sync entities
public class CloudHotelSettings : CloudBaseEntity
{
    public string HotelName { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public TimeSpan CheckInTime { get; set; } = new(14, 0, 0);
    public TimeSpan CheckOutTime { get; set; } = new(12, 0, 0);
    public string CancellationPolicy { get; set; } = string.Empty;
    public string Currency { get; set; } = "IQD";
    public bool IsConfigured { get; set; }
}

public class CloudHotelFloor : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
}

public class CloudHotelRoomType : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public int Capacity { get; set; } = 2;
    public decimal BasePrice { get; set; }
    public int SortOrder { get; set; }
}

public class CloudHotelRoom : CloudBaseEntity
{
    public string RoomNumber { get; set; } = string.Empty;
    public int FloorId { get; set; }
    public int RoomTypeId { get; set; }
    public RoomStatus Status { get; set; } = RoomStatus.Available;
    public string Notes { get; set; } = string.Empty;
    public CloudHotelFloor Floor { get; set; } = null!;
    public CloudHotelRoomType RoomType { get; set; } = null!;
}

public class CloudHotelGuest : CloudBaseEntity
{
    public string FullName { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class CloudHotelReservation : CloudBaseEntity
{
    public string ReservationNumber { get; set; } = string.Empty;
    public int GuestId { get; set; }
    public int? RoomId { get; set; }
    public string GuestName { get; set; } = string.Empty;
    public string? RoomNumber { get; set; }
    public DateTime CheckInDate { get; set; }
    public DateTime CheckOutDate { get; set; }
    public DateTime? ActualCheckIn { get; set; }
    public DateTime? ActualCheckOut { get; set; }
    public int GuestCount { get; set; } = 1;
    public ReservationStatus Status { get; set; } = ReservationStatus.Confirmed;
    public decimal TotalAmount { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public CloudHotelGuest Guest { get; set; } = null!;
    public CloudHotelRoom? Room { get; set; }
}

public class CloudHotelReservationCharge : CloudBaseEntity
{
    public int ReservationId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ChargeDate { get; set; }
    public string Notes { get; set; } = string.Empty;
    public CloudHotelReservation Reservation { get; set; } = null!;
}

public class CloudHotelReservationPayment : CloudBaseEntity
{
    public int ReservationId { get; set; }
    public DateTime PaymentDate { get; set; }
    public decimal Amount { get; set; }
    public string PaymentMethod { get; set; } = "نقد";
    public string Notes { get; set; } = string.Empty;
    public int? HotelCashBoxId { get; set; }
    public CloudHotelReservation Reservation { get; set; } = null!;
    public CloudHotelCashBox? HotelCashBox { get; set; }
}

public class CloudHotelCashBox : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsBank { get; set; }
    public decimal OpeningBalance { get; set; }
    public decimal CurrentBalance { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}

public class CloudHotelVoucher : CloudBaseEntity
{
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; }
    public HotelVoucherType Type { get; set; }
    public decimal Amount { get; set; }
    public int HotelCashBoxId { get; set; }
    public int? ReservationId { get; set; }
    public int? HotelExpenseId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public CloudHotelCashBox HotelCashBox { get; set; } = null!;
    public CloudHotelReservation? Reservation { get; set; }
    public CloudHotelExpense? HotelExpense { get; set; }
}

public class CloudHotelExpenseType : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class CloudHotelExpense : CloudBaseEntity
{
    public int HotelExpenseTypeId { get; set; }
    public DateTime ExpenseDate { get; set; }
    public decimal Amount { get; set; }
    public int? HotelCashBoxId { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public CloudHotelExpenseType ExpenseType { get; set; } = null!;
    public CloudHotelCashBox? HotelCashBox { get; set; }
}

public class CloudHotelRatePlan : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int RoomTypeId { get; set; }
    public decimal BasePrice { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
    public CloudHotelRoomType RoomType { get; set; } = null!;
}

public class CloudHotelRatePlanSeason : CloudBaseEntity
{
    public int RatePlanId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public decimal PricePerNight { get; set; }
    public CloudHotelRatePlan RatePlan { get; set; } = null!;
}

public class CloudHotelHousekeepingTask : CloudBaseEntity
{
    public int RoomId { get; set; }
    public HousekeepingStatus Status { get; set; } = HousekeepingStatus.Pending;
    public string AssignedTo { get; set; } = string.Empty;
    public DateTime? StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string Notes { get; set; } = string.Empty;
    public CloudHotelRoom Room { get; set; } = null!;
}

public class CloudRestaurantIngredient : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public decimal MinQuantity { get; set; }
    public decimal AverageCost { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CloudRestaurantIngredientStock : CloudBaseEntity
{
    public int RestaurantIngredientId { get; set; }
    public decimal Quantity { get; set; }
    public CloudRestaurantIngredient Ingredient { get; set; } = null!;
}

public class CloudRestaurantMenuCategory : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public string ColorHex { get; set; } = "#00897B";
    public bool IsActive { get; set; } = true;
}

public class CloudRestaurantRecipe : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
}

public class CloudRestaurantMenuItem : CloudBaseEntity
{
    public int RestaurantMenuCategoryId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal SalePrice { get; set; }
    public int? RecipeId { get; set; }
    public bool IsActive { get; set; } = true;
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
    public CloudRestaurantMenuCategory Category { get; set; } = null!;
    public CloudRestaurantRecipe? Recipe { get; set; }
}

public class CloudRestaurantRecipeLine : CloudBaseEntity
{
    public int RestaurantRecipeId { get; set; }
    public int RestaurantIngredientId { get; set; }
    public decimal Quantity { get; set; }
    public CloudRestaurantRecipe Recipe { get; set; } = null!;
    public CloudRestaurantIngredient Ingredient { get; set; } = null!;
}

public class CloudRestaurantTable : CloudBaseEntity
{
    public string TableNumber { get; set; } = string.Empty;
    public int Capacity { get; set; } = 4;
    public RestaurantTableStatus Status { get; set; }
    public int SortOrder { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CloudRestaurantOrder : CloudBaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public RestaurantOrderType OrderType { get; set; }
    public RestaurantOrderStatus Status { get; set; }
    public RestaurantKitchenStatus KitchenStatus { get; set; }
    public int? RestaurantTableId { get; set; }
    public int? ReservationId { get; set; }
    public int? RoomId { get; set; }
    public int? GuestId { get; set; }
    public decimal SubTotal { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal CogsAmount { get; set; }
    public decimal GrossProfit { get; set; }
    public DateTime OrderDate { get; set; }
    public DateTime? PaidAt { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CloudRestaurantOrderLine : CloudBaseEntity
{
    public int RestaurantOrderId { get; set; }
    public int RestaurantMenuItemId { get; set; }
    public string ItemName { get; set; } = string.Empty;
    public decimal Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal LineTotal { get; set; }
    public decimal CogsAmount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public CloudRestaurantOrder Order { get; set; } = null!;
}

public class CloudRestaurantOrderPayment : CloudBaseEntity
{
    public int RestaurantOrderId { get; set; }
    public decimal Amount { get; set; }
    public RestaurantPaymentMethod PaymentMethod { get; set; }
    public int? HotelCashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public CloudRestaurantOrder Order { get; set; } = null!;
}

public class CloudRestaurantStockMovement : CloudBaseEntity
{
    public int RestaurantIngredientId { get; set; }
    public RestaurantStockMovementType MovementType { get; set; }
    public decimal Quantity { get; set; }
    public decimal UnitCost { get; set; }
    public int? RestaurantOrderId { get; set; }
    public DateTime MovementDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

// Car sync entities
public class CloudCarSaleContract : CloudBaseEntity
{
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; } = DateTime.Today;

    public string SellerName { get; set; } = string.Empty;
    public string SellerAddress { get; set; } = string.Empty;
    public string SellerIdNumber { get; set; } = string.Empty;
    public DateTime? SellerIdDate { get; set; }
    public string SellerPhone { get; set; } = string.Empty;

    public string BuyerName { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public string BuyerIdNumber { get; set; } = string.Empty;
    public DateTime? BuyerIdDate { get; set; }
    public string BuyerPhone { get; set; } = string.Empty;

    public string AnnualOwnerName { get; set; } = string.Empty;
    public string AnnualOwnerAddress { get; set; } = string.Empty;

    public string PlateNumber { get; set; } = string.Empty;
    public string CarType { get; set; } = string.Empty;
    public string CarModel { get; set; } = string.Empty;
    public string CarColor { get; set; } = string.Empty;
    public string ChassisNumber { get; set; } = string.Empty;

    public decimal CarPrice { get; set; }
    public bool IsAgreedPrice { get; set; }
    public string CarPriceInWords { get; set; } = string.Empty;
    public decimal AmountReceived { get; set; }
    public decimal RemainingAmount { get; set; }

    public string WitnessOneName { get; set; } = string.Empty;
    public string WitnessTwoName { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
    public CarContractStatus Status { get; set; } = CarContractStatus.Active;

    public ICollection<CloudCarContractPayment> Payments { get; set; } = [];
}

public class CloudCarContractPayment : CloudBaseEntity
{
    public int ContractId { get; set; }
    public CloudCarSaleContract Contract { get; set; } = null!;

    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal RemainingBefore { get; set; }
    public decimal RemainingAfter { get; set; }
}

// Car trade sync entities
public class CloudCarTradeTransaction : CloudBaseEntity
{
    public string TransactionNumber { get; set; } = string.Empty;
    public DateTime TransactionDate { get; set; } = DateTime.Today;
    public CarTradeType TradeType { get; set; } = CarTradeType.Buy;

    public string CarName { get; set; } = string.Empty;
    public string CarColor { get; set; } = string.Empty;
    public string PlateNumber { get; set; } = string.Empty;
    public string ChassisNumber { get; set; } = string.Empty;
    public string CarType { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;
    public string SellerPhone { get; set; } = string.Empty;
    public string BuyerName { get; set; } = string.Empty;
    public string BuyerPhone { get; set; } = string.Empty;

    public decimal PurchasePrice { get; set; }
    public decimal SalePrice { get; set; }
    public decimal TotalAmount { get; set; }

    public CarTradePaymentMode PaymentMode { get; set; } = CarTradePaymentMode.FullCash;
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }

    public CarTradeStatus Status { get; set; } = CarTradeStatus.Active;
    public string Notes { get; set; } = string.Empty;

    public bool IsSold { get; set; }
    public DateTime? SaleDate { get; set; }
    public CarTradePaymentMode SalePaymentMode { get; set; } = CarTradePaymentMode.FullCash;
    public decimal SaleAmountPaid { get; set; }
    public decimal SaleRemainingAmount { get; set; }

    public ICollection<CloudCarTradePayment> Payments { get; set; } = [];
}

public class CloudCarTradePayment : CloudBaseEntity
{
    public int TransactionId { get; set; }
    public CloudCarTradeTransaction Transaction { get; set; } = null!;

    public CarTradePaymentKind PaymentKind { get; set; } = CarTradePaymentKind.Purchase;

    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal RemainingBefore { get; set; }
    public decimal RemainingAfter { get; set; }
}

// Real estate sync entities
public class CloudRealEstateContract : CloudBaseEntity
{
    public string ContractNumber { get; set; } = string.Empty;
    public DateTime ContractDate { get; set; } = DateTime.Today;

    public RealEstateContractType ContractType { get; set; } = RealEstateContractType.Sale;
    public RealEstatePropertyType PropertyType { get; set; } = RealEstatePropertyType.House;

    public string PropertyLocation { get; set; } = string.Empty;
    public string PropertyAddress { get; set; } = string.Empty;
    public decimal PropertyAreaSqm { get; set; }
    public string PropertyDescription { get; set; } = string.Empty;

    public string SellerName { get; set; } = string.Empty;
    public string SellerAddress { get; set; } = string.Empty;
    public string SellerIdNumber { get; set; } = string.Empty;
    public DateTime? SellerIdDate { get; set; }
    public string SellerPhone { get; set; } = string.Empty;

    public string BuyerName { get; set; } = string.Empty;
    public string BuyerAddress { get; set; } = string.Empty;
    public string BuyerIdNumber { get; set; } = string.Empty;
    public DateTime? BuyerIdDate { get; set; }
    public string BuyerPhone { get; set; } = string.Empty;

    public decimal TotalPrice { get; set; }
    public string TotalPriceInWords { get; set; } = string.Empty;
    public decimal DownPayment { get; set; }
    public decimal AmountPaid { get; set; }
    public decimal RemainingAmount { get; set; }

    public RealEstatePaymentMode PaymentMode { get; set; } = RealEstatePaymentMode.Cash;
    public RealEstateDebtorParty DebtorParty { get; set; } = RealEstateDebtorParty.None;
    public DateTime? DueDate { get; set; }

    public string WitnessOneName { get; set; } = string.Empty;
    public string WitnessTwoName { get; set; } = string.Empty;

    public string Notes { get; set; } = string.Empty;
    public RealEstateContractStatus Status { get; set; } = RealEstateContractStatus.Active;

    public ICollection<CloudRealEstateContractPayment> Payments { get; set; } = [];
    public ICollection<CloudRealEstateContractClause> Clauses { get; set; } = [];
}

public class CloudRealEstateContractPayment : CloudBaseEntity
{
    public int ContractId { get; set; }
    public CloudRealEstateContract Contract { get; set; } = null!;

    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Notes { get; set; } = string.Empty;
    public decimal RemainingBefore { get; set; }
    public decimal RemainingAfter { get; set; }
}

public class CloudRealEstateContractClause : CloudBaseEntity
{
    public int ContractId { get; set; }
    public CloudRealEstateContract Contract { get; set; } = null!;

    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public class CloudRealEstateClauseTemplate : CloudBaseEntity
{
    public int SortOrder { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CloudRealEstateParty : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string IdNumber { get; set; } = string.Empty;
    public DateTime? IdDate { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CloudRealEstateExpenseType : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CloudRealEstateExpense : CloudBaseEntity
{
    public int ExpenseTypeId { get; set; }
    public CloudRealEstateExpenseType ExpenseType { get; set; } = null!;

    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public string Description { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;

    public int? RelatedContractId { get; set; }
    public CloudRealEstateContract? RelatedContract { get; set; }
}

// Gold shop sync entities
public class CloudGoldSettings : CloudBaseEntity
{
    public decimal MithqalGrams { get; set; } = 5;
    public string ScaleComPort { get; set; } = string.Empty;
    public int ScaleBaudRate { get; set; } = 9600;
    public decimal ScaleStabilityThresholdGrams { get; set; } = 0.01m;
    public bool AllowManualWeightEdit { get; set; } = true;
    public decimal LowStockAlertGrams { get; set; } = 10;
    public int OverdueDaysThreshold { get; set; } = 30;
    public string EnabledKaratsCsv { get; set; } = "24,22,21,18";
    public GoldMakingChargeMode DefaultMakingChargeMode { get; set; } = GoldMakingChargeMode.Fixed;
    public bool IsConfigured { get; set; }
}

public class CloudGoldFxRate : CloudBaseEntity
{
    public DateTime RateDate { get; set; } = DateTime.Today;
    public decimal UsdToIqd { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CloudGoldKarat : CloudBaseEntity
{
    public int KaratValue { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal PurityFactor { get; set; } = 1.0m;
    public bool IsActive { get; set; } = true;
    public int DisplayOrder { get; set; }
}

public class CloudGoldMithqalPrice : CloudBaseEntity
{
    public DateTime PriceDate { get; set; } = DateTime.Today;
    public int KaratValue { get; set; }
    public decimal PricePerMithqal { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.USD;
    public decimal? FxRateUsed { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CloudGoldItem : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Barcode { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal SuggestedMakingCharge { get; set; }
    public GoldCurrency MakingChargeCurrency { get; set; } = GoldCurrency.IQD;
    public decimal CostPerGram { get; set; }
    public GoldItemStatus Status { get; set; } = GoldItemStatus.InStock;
    public bool TrackAsPiece { get; set; } = true;
}

public class CloudGoldStockBalance : CloudBaseEntity
{
    public int WarehouseId { get; set; }
    public CloudGoldWarehouse? Warehouse { get; set; }
    public int KaratValue { get; set; }
    public decimal GramsOnHand { get; set; }
    public decimal AverageCostPerGram { get; set; }
}

public class CloudGoldCustomer : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public decimal GoldCreditGrams { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CloudGoldSupplier : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Notes { get; set; } = string.Empty;
    public decimal CreditBalanceIqd { get; set; }
    public decimal CreditBalanceUsd { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CloudGoldWarehouse : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}

public class CloudGoldExpenseType : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
}

public class CloudGoldExpense : CloudBaseEntity
{
    public DateTime ExpenseDate { get; set; } = DateTime.Today;
    public int ExpenseTypeId { get; set; }
    public CloudGoldExpenseType? ExpenseType { get; set; }
    public decimal Amount { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public int CashBoxId { get; set; }
    public CloudGoldCashBox? CashBox { get; set; }
    public string Notes { get; set; } = string.Empty;
    public int? WarehouseId { get; set; }
    public CloudGoldWarehouse? Warehouse { get; set; }
}

public class CloudGoldWarehouseTransfer : CloudBaseEntity
{
    public DateTime TransferDate { get; set; } = DateTime.Today;
    public int FromWarehouseId { get; set; }
    public CloudGoldWarehouse? FromWarehouse { get; set; }
    public int ToWarehouseId { get; set; }
    public CloudGoldWarehouse? ToWarehouse { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CloudGoldCashBox : CloudBaseEntity
{
    public string Name { get; set; } = string.Empty;
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal Balance { get; set; }
    public bool IsDefault { get; set; }
    public bool IsActive { get; set; } = true;
}

public class CloudGoldInvoice : CloudBaseEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public DateTime InvoiceDate { get; set; } = DateTime.Today;
    public GoldInvoiceType InvoiceType { get; set; } = GoldInvoiceType.Sale;
    public GoldPaymentMethod PaymentMethod { get; set; } = GoldPaymentMethod.Cash;
    public GoldInvoiceStatus Status { get; set; } = GoldInvoiceStatus.Completed;
    public int? CustomerId { get; set; }
    public CloudGoldCustomer? Customer { get; set; }
    public int? SupplierId { get; set; }
    public CloudGoldSupplier? Supplier { get; set; }
    public int? WarehouseId { get; set; }
    public CloudGoldWarehouse? Warehouse { get; set; }
    public bool IsExchange { get; set; }
    public decimal ExchangeCashDifference { get; set; }
    public GoldCurrency PricingCurrency { get; set; } = GoldCurrency.USD;
    public GoldCurrency PaymentCurrency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public decimal TotalGoldValue { get; set; }
    public decimal TotalMakingCharge { get; set; }
    public decimal DiscountAmount { get; set; }
    public decimal TotalAmount { get; set; }
    public decimal TotalAmountIqd { get; set; }
    public decimal TotalAmountUsd { get; set; }
    public decimal PaidAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public decimal TotalWeightGrams { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public int? RelatedInvoiceId { get; set; }

    public ICollection<CloudGoldInvoiceLine> Lines { get; set; } = [];
    public ICollection<CloudGoldPayment> Payments { get; set; } = [];
}

public class CloudGoldInvoiceLine : CloudBaseEntity
{
    public int InvoiceId { get; set; }
    public CloudGoldInvoice Invoice { get; set; } = null!;
    public int? ItemId { get; set; }
    public int KaratValue { get; set; }
    public decimal WeightGrams { get; set; }
    public decimal MithqalPrice { get; set; }
    public decimal PricePerGram { get; set; }
    public decimal GoldValue { get; set; }
    public decimal MakingCharge { get; set; }
    public GoldMakingChargeMode MakingChargeMode { get; set; } = GoldMakingChargeMode.Fixed;
    public decimal MakingChargeRate { get; set; }
    public decimal LineTotal { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool WeightFromScale { get; set; }
    public GoldInvoiceLineDirection LineDirection { get; set; } = GoldInvoiceLineDirection.Out;
}

public class CloudGoldPayment : CloudBaseEntity
{
    public int InvoiceId { get; set; }
    public CloudGoldInvoice Invoice { get; set; } = null!;
    public DateTime PaymentDate { get; set; } = DateTime.Today;
    public decimal Amount { get; set; }
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal FxRate { get; set; }
    public int? CashBoxId { get; set; }
    public string Notes { get; set; } = string.Empty;
}

public class CloudGoldVoucher : CloudBaseEntity
{
    public string VoucherNumber { get; set; } = string.Empty;
    public DateTime VoucherDate { get; set; } = DateTime.Today;
    public GoldVoucherType VoucherType { get; set; } = GoldVoucherType.Receipt;
    public GoldCurrency Currency { get; set; } = GoldCurrency.IQD;
    public decimal Amount { get; set; }
    public int? CashBoxId { get; set; }
    public int? CustomerId { get; set; }
    public int? SupplierId { get; set; }
    public bool IsOpeningBalance { get; set; }
    public bool AffectsCashBox { get; set; } = true;
    public string Notes { get; set; } = string.Empty;
}

public class CloudGoldNotification : CloudBaseEntity
{
    public GoldNotificationType Type { get; set; } = GoldNotificationType.Info;
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public bool IsRead { get; set; }
    public DateTime? ReadAt { get; set; }
    public string? RelatedEntity { get; set; }
    public int? RelatedId { get; set; }
}
