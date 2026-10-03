/*
================================================================================
  AlMuhasib — سكربت بيانات تجريبية كاملة للنظام المحاسبي (AppDbContext)
================================================================================
  يغطي كل جداول المحاسبة (59 جدولاً) ببيانات حقيقية/تجريبية مرتّبة حسب FK:
  - 3 فروع: بغداد (MAIN) / البصرة (BSR) / أربيل (ERB)
  - كل أنواع الفواتير: Purchase, Sale, Installment, SaleReturn, PurchaseReturn, Damage
  - كل أنواع السندات: Receipt, Payment, BankReceipt, InvestorDeposit/Withdrawal, DebtReceipt
  - مخازن، قاصات IQD/USD، بنوك، أقساط، مناديب، ولاء، مستثمرين، رأس مال، ...
  - التعدادات كنص (Sale, IQD...) — لا تُدرج RowVersion
  - الدخول: admin / baghdad / basra / erbil  — كلمة المرور: admin

  تحذير: يحذف كل بيانات قاعدة المحاسبة الحالية (DELETE ثم إعادة إدخال). للتطوير فقط.
================================================================================
*/

SET NOCOUNT ON;
SET XACT_ABORT ON;
BEGIN TRANSACTION;

DECLARE @Now  datetime2     = SYSUTCDATETIME();
DECLARE @By   nvarchar(100) = N'Seed';
DECLARE @Fx   decimal(18,4) = 1320.0000; -- دولار → دينار

------------------------------------------------------------
-- 1) تعطيل القيود ثم تفريغ الجداول
------------------------------------------------------------
DECLARE @sql nvarchar(max) = N'';
DECLARE @t sysname;
DECLARE @trunc nvarchar(512);

-- ملاحظة: لا تضع QUOTENAME داخل EXEC(...) مباشرة — ابنِ النص في متغير أولاً
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))
    + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
    + N' NOCHECK CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk;
IF LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

DECLARE @tables TABLE (Ord int, Name sysname);
INSERT INTO @tables (Ord, Name) VALUES
( 1, N'LoyaltyPointTransactions'),
( 2, N'CustomerLoyaltyAccounts'),
( 3, N'CustomerAttachments'),
( 4, N'SalesRepCollections'),
( 5, N'SalesRepCommissionEntries'),
( 6, N'SalesRepCommissionRules'),
( 7, N'SalesRepTargets'),
( 8, N'ProfitDistributionDetails'),
( 9, N'ProfitDistributions'),
(10, N'InvestorTransactions'),
(11, N'Installments'),
(12, N'InstallmentPlans'),
(13, N'InvoiceItems'),
(14, N'Vouchers'),
(15, N'CurrencyExchanges'),
(16, N'Expenses'),
(17, N'Transfers'),
(18, N'WarehouseTransferItems'),
(19, N'WarehouseTransfers'),
(20, N'ProductSizeStocks'),
(21, N'ProductBatches'),
(22, N'ProductSerials'),
(23, N'Invoices'),
(24, N'WarehouseStocks'),
(25, N'AuditLogs'),
(26, N'UserLoginLogs'),
(27, N'UserTasks'),
(28, N'UserNotes'),
(29, N'Permissions'),
(30, N'UserBranches'),
(31, N'EntityCustomFieldSettings'),
(32, N'ExchangeRates'),
(33, N'LoyaltySettings'),
(34, N'BusinessSettings'),
(35, N'PrintBrandingSettings'),
(36, N'CapitalEntries'),
(37, N'Customers'),
(38, N'Suppliers'),
(39, N'Drivers'),
(40, N'Employees'),
(41, N'SalesRepresentatives'),
(42, N'Investors'),
(43, N'ExpenseTypes'),
(44, N'CashBoxes'),
(45, N'BankAccounts'),
(46, N'Warehouses'),
(47, N'ProductOffers'),
(48, N'ProductPrices'),
(49, N'ProductUnits'),
(50, N'ProductSizes'),
(51, N'ProductColors'),
(52, N'Products'),
(53, N'Categories'),
(54, N'PricingTypes'),
(55, N'PackagingTypes'),
(56, N'Users'),
(57, N'Branches'),
(58, N'SyncStates'),
(59, N'CloudSyncSettings');

DECLARE c CURSOR LOCAL FAST_FORWARD FOR SELECT Name FROM @tables ORDER BY Ord;
OPEN c;
FETCH NEXT FROM c INTO @t;
WHILE @@FETCH_STATUS = 0
BEGIN
    IF OBJECT_ID(N'dbo.' + @t, N'U') IS NOT NULL
    BEGIN
        -- DELETE وليس TRUNCATE: SQL Server يمنع TRUNCATE للجداول المشار إليها بـ FK حتى مع NOCHECK
        SET @trunc = N'DELETE FROM dbo.' + QUOTENAME(@t) + N';';
        EXEC sys.sp_executesql @trunc;
        -- إعادة ضبط IDENTITY إن وُجد
        IF EXISTS (
            SELECT 1 FROM sys.identity_columns
            WHERE object_id = OBJECT_ID(N'dbo.' + @t)
        )
        BEGIN
            SET @trunc = N'DBCC CHECKIDENT (''dbo.' + @t + N''', RESEED, 0);';
            EXEC sys.sp_executesql @trunc;
        END
    END
    FETCH NEXT FROM c INTO @t;
END
CLOSE c; DEALLOCATE c;

------------------------------------------------------------
-- 2) الفروع
------------------------------------------------------------
SET IDENTITY_INSERT dbo.Branches ON;
INSERT INTO dbo.Branches (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, Name, Code, IsActive, IsMain)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'الفرع الرئيسي — بغداد', N'MAIN', 1, 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'فرع البصرة',           N'BSR',  1, 0),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'فرع أربيل',            N'ERB',  1, 0);
SET IDENTITY_INSERT dbo.Branches OFF;

------------------------------------------------------------
-- 3) المستخدمون (كلمة المرور: admin)
--    BCrypt.Net HashPassword("admin")
------------------------------------------------------------
SET IDENTITY_INSERT dbo.Users ON;
INSERT INTO dbo.Users (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, Username, PasswordHash, FullName, Role, IsActive, MustChangePassword)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'admin',   N'$2a$11$vK1jfyItvH7ALAoFLxAMUel.bRAbNjzPXz.6N2iZXHqDlNLtG3V1y', N'مدير النظام',   N'Admin', 1, 0),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'baghdad', N'$2a$11$vK1jfyItvH7ALAoFLxAMUel.bRAbNjzPXz.6N2iZXHqDlNLtG3V1y', N'محاسب بغداد',   N'User',  1, 0),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'basra',   N'$2a$11$vK1jfyItvH7ALAoFLxAMUel.bRAbNjzPXz.6N2iZXHqDlNLtG3V1y', N'محاسب البصرة',  N'User',  1, 0),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'erbil',   N'$2a$11$vK1jfyItvH7ALAoFLxAMUel.bRAbNjzPXz.6N2iZXHqDlNLtG3V1y', N'محاسب أربيل',   N'User',  1, 0);
SET IDENTITY_INSERT dbo.Users OFF;

SET IDENTITY_INSERT dbo.UserBranches ON;
INSERT INTO dbo.UserBranches (Id, UserId, BranchId, IsDefault, CreatedAt) VALUES
(1, 1, 1, 1, @Now),
(2, 1, 2, 0, @Now),
(3, 1, 3, 0, @Now),
(4, 2, 1, 1, @Now),
(5, 3, 2, 1, @Now),
(6, 4, 3, 1, @Now);
SET IDENTITY_INSERT dbo.UserBranches OFF;

SET IDENTITY_INSERT dbo.Permissions ON;
INSERT INTO dbo.Permissions (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, UserId, ScreenName, CanView, CanAdd, CanEdit, CanDelete, CanPrint, CanExport, CanEditPrice, IsViewOnly)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'Sales',     1, 1, 1, 0, 1, 1, 1, 0),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'Purchases', 1, 1, 1, 0, 1, 1, 0, 0),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'Sales',     1, 1, 1, 0, 1, 0, 1, 0),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 4, N'Sales',     1, 1, 1, 0, 1, 1, 1, 0),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 4, N'Reports',   1, 0, 0, 0, 1, 1, 0, 0);
SET IDENTITY_INSERT dbo.Permissions OFF;

------------------------------------------------------------
-- 4) كتالوج مشترك (بدون BranchId)
------------------------------------------------------------
SET IDENTITY_INSERT dbo.Categories ON;
INSERT INTO dbo.Categories (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, Name) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'إلكترونيات'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'مواد غذائية'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'منظفات');
SET IDENTITY_INSERT dbo.Categories OFF;

SET IDENTITY_INSERT dbo.PricingTypes ON;
INSERT INTO dbo.PricingTypes (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, Name, IsDefault, IsActive) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'قطاعي', 1, 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'جملة',  0, 1);
SET IDENTITY_INSERT dbo.PricingTypes OFF;

SET IDENTITY_INSERT dbo.PackagingTypes ON;
INSERT INTO dbo.PackagingTypes (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, Name, IsDefault, IsActive) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'قطعة',  1, 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'كرتون', 0, 1);
SET IDENTITY_INSERT dbo.PackagingTypes OFF;

SET IDENTITY_INSERT dbo.Products ON;
INSERT INTO dbo.Products
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 Name, Description, Barcode, ScientificName, UsageInstructions, CategoryId, Weight, WeightUnit,
 DiscountType, DiscountValue, DiscountExpiresAt, CustomFieldsJson,
 VehicleType, ChassisNumber, CarModel, VehicleColor, PassengerCount, PlateNumber, PlateType)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'لابتوب ديل XPS',     N'لابتوب للأعمال',     N'P-1001', NULL, NULL, 1, 1.80, N'كغ', 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'طابعة HP LaserJet', N'طابعة ليزر',         N'P-1002', NULL, NULL, 1, 7.50, N'كغ', 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'ماوس لاسلكي',        N'ماوس بصري',          N'P-1003', NULL, NULL, 1, 0.10, N'كغ', 1, 5, DATEADD(month, 3, @Now), NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'أرز عنبر 5 كغ',      N'أرز عراقي',          N'P-2001', NULL, NULL, 2, 5.00, N'كغ', 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'زيت نباتي 1 لتر',    N'زيت طبخ',            N'P-2002', NULL, NULL, 2, 1.00, N'لتر', 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'منظف أرضيات 1 لتر',  N'منظف عام',           N'P-3001', NULL, NULL, 3, 1.00, N'لتر', 0, 0, NULL, NULL, NULL, NULL, NULL, NULL, NULL, NULL, 0);
SET IDENTITY_INSERT dbo.Products OFF;

SET IDENTITY_INSERT dbo.ProductPrices ON;
INSERT INTO dbo.ProductPrices
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 ProductId, PricingTypeId, SalePrice, SalePriceUsd, PurchasePrice, PurchasePriceUsd)
VALUES
(1,  NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 950000, 720, 780000, 590),
(2,  NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, 900000, 680, 780000, 590),
(3,  NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 1, 285000, 215, 220000, 165),
(4,  NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 1, 18500,  14,  12000,  9),
(5,  NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 4, 1, 12500,  0,   9000,  0),
(6,  NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 5, 1, 3500,   0,   2500,  0),
(7,  NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 6, 1, 2750,   0,   1800,  0);
SET IDENTITY_INSERT dbo.ProductPrices OFF;

SET IDENTITY_INSERT dbo.ProductUnits ON;
INSERT INTO dbo.ProductUnits
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 ProductId, PackagingTypeId, UnitName, ConversionFactor, IsDefault)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'قطعة', 1, 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 4, 1, N'كيس',  1, 1),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 4, 2, N'كرتون', 10, 0),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 5, 1, N'زجاجة', 1, 1);
SET IDENTITY_INSERT dbo.ProductUnits OFF;

SET IDENTITY_INSERT dbo.ProductSizes ON;
INSERT INTO dbo.ProductSizes (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, ProductId, SizeName, SortOrder) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 6, N'1 لتر', 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 6, N'5 لتر', 2);
SET IDENTITY_INSERT dbo.ProductSizes OFF;

SET IDENTITY_INSERT dbo.ProductColors ON;
INSERT INTO dbo.ProductColors (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, ProductId, ColorName, SortOrder) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'فضي', 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'أسود', 2);
SET IDENTITY_INSERT dbo.ProductColors OFF;

SET IDENTITY_INSERT dbo.ProductOffers ON;
INSERT INTO dbo.ProductOffers
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 Name, IsActive, TriggerProductId, TriggerQuantity, GiftProductId, GiftQuantity, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, N'اشترِ 10 أرز واحصل على زيت', 1, 4, 10, 5, 1, N'عرض ترويجي');
SET IDENTITY_INSERT dbo.ProductOffers OFF;

------------------------------------------------------------
-- 5) بيانات لكل فرع: مخازن / قاصات / بنوك / أنواع مصاريف / أطراف
------------------------------------------------------------
SET IDENTITY_INSERT dbo.Warehouses ON;
INSERT INTO dbo.Warehouses (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, BranchId, Name, Location) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'المخزن الرئيسي', N'الكرادة — بغداد'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'مخزن فرعي',      N'المنصور — بغداد'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'مخزن البصرة',    N'العشار — البصرة'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'مخزن أربيل',     N'عنكاوا — أربيل'),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'مخزن أربيل فرعي', N'شقق أربيل'),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'مخزن البصرة فرعي', N'الزبير');
SET IDENTITY_INSERT dbo.Warehouses OFF;

SET IDENTITY_INSERT dbo.CashBoxes ON;
INSERT INTO dbo.CashBoxes (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, BranchId, Name, Balance, Currency) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'قاصة الدينار — بغداد',  5000000, N'IQD'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'قاصة الدولار — بغداد',  3500,    N'USD'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'قاصة الدينار — البصرة', 2500000, N'IQD'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'قاصة الدولار — البصرة', 1200,    N'USD'),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'قاصة الدينار — أربيل',  1800000, N'IQD'),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'قاصة الدولار — أربيل',  900,     N'USD');
SET IDENTITY_INSERT dbo.CashBoxes OFF;

SET IDENTITY_INSERT dbo.BankAccounts ON;
INSERT INTO dbo.BankAccounts (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, BranchId, Name, AccountNumber, Balance, Currency) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'مصرف الرافدين — جاري', N'IQ-RAF-001', 12000000, N'IQD'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'TBI — دولار',          N'USD-TBI-01', 8000,     N'USD'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'مصرف الجنوب — جاري',   N'IQ-JN-002',  4500000,  N'IQD'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'كي بي آي — جاري',      N'IQ-KBI-03',  3200000,  N'IQD');
SET IDENTITY_INSERT dbo.BankAccounts OFF;

-- أسماء أنواع المصاريف فريدة عالمياً (فهرس فريد على Name)
SET IDENTITY_INSERT dbo.ExpenseTypes ON;
INSERT INTO dbo.ExpenseTypes (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, BranchId, Name) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'إيجار (بغداد)'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'كهرباء (بغداد)'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'رواتب (بغداد)'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'إيجار (البصرة)'),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'نقل وتوصيل (البصرة)'),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'إيجار (أربيل)'),
(7, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'كهرباء (أربيل)'),
(8, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'صيانة (بغداد)');
SET IDENTITY_INSERT dbo.ExpenseTypes OFF;

SET IDENTITY_INSERT dbo.SalesRepresentatives ON;
INSERT INTO dbo.SalesRepresentatives
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, Name, Phone, Region, StartDate, IsActive, MonthlySalary, CompensationNotes, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'علي حسين',  N'07701234567', N'بغداد',  '2025-01-01', 1, 600000, N'عمولة 2%', NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'كريم جاسم', N'07801234567', N'البصرة', '2025-03-01', 1, 550000, NULL, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'دلان محمد', N'07501234567', N'أربيل',  '2025-05-01', 1, 500000, N'عمولة 2%', NULL);
SET IDENTITY_INSERT dbo.SalesRepresentatives OFF;

SET IDENTITY_INSERT dbo.Customers ON;
INSERT INTO dbo.Customers
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, Name, Phone, Address, FileNumber, Notes, MaxCreditLimit, MaxInstallmentDebt, ReliabilityScore,
 GuarantorName, GuarantorPhone, CustomFieldsJson, SalesRepresentativeId, IdNumber, IdIssuer)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'أحمد محمد الكاظمي', N'07711112222', N'بغداد — الكرادة', N'C-BGD-001', NULL, 2000000, 5000000, 80, N'محمد علي', N'07799998888', NULL, 1, NULL, NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'محل النور للإلكترونيات', N'07722223333', N'بغداد — الشورجة', N'C-BGD-002', N'عميل جملة', 5000000, NULL, 90, NULL, NULL, NULL, 1, NULL, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'سارة علي', N'07733334444', N'بغداد — المنصور', N'C-BGD-003', NULL, 500000, 1500000, 70, NULL, NULL, NULL, NULL, NULL, NULL),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'حسن عبدالرضا', N'07811112222', N'البصرة — العشار', N'C-BSR-001', NULL, 1000000, NULL, 75, NULL, NULL, NULL, 2, NULL, NULL),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'نوزاد عبدالله', N'07511112222', N'أربيل', N'C-ERB-001', NULL, 800000, 2000000, 65, NULL, NULL, NULL, 3, NULL, NULL),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'محل الرافدين — البصرة', N'07822223333', N'البصرة', N'C-BSR-002', NULL, 1500000, NULL, 70, NULL, NULL, NULL, 2, NULL, NULL),
(7, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'شيرين أحمد', N'07522223333', N'أربيل — عنكاوا', N'C-ERB-002', NULL, 400000, 1000000, 72, NULL, NULL, NULL, 3, NULL, NULL);
SET IDENTITY_INSERT dbo.Customers OFF;

SET IDENTITY_INSERT dbo.Suppliers ON;
INSERT INTO dbo.Suppliers (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, BranchId, Name, Phone, Address, Notes, CustomFieldsJson) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'شركة الأمل للتجهيزات', N'07800001111', N'بغداد — الشورجة', N'مورد إلكترونيات', NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'مورد الجملة الغذائي', N'07800002222', N'بغداد — جميلة', NULL, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'تجهيزات الجنوب', N'07800003333', N'البصرة', NULL, NULL),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'مورد كردستان', N'07500004444', N'أربيل', NULL, NULL),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'توريد الشمال', N'07500005555', N'أربيل', N'مواد غذائية', NULL);
SET IDENTITY_INSERT dbo.Suppliers OFF;

SET IDENTITY_INSERT dbo.Drivers ON;
INSERT INTO dbo.Drivers (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, BranchId, Name, Phone, Address, Notes) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'سائق التوصيل — أبو أحمد', N'07755556666', N'بغداد', NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'سائق البصرة', N'07855556666', N'البصرة', NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'سائق أربيل', N'07555556666', N'أربيل', NULL);
SET IDENTITY_INSERT dbo.Drivers OFF;

SET IDENTITY_INSERT dbo.Employees ON;
INSERT INTO dbo.Employees
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, Name, Phone, Address, JobTitle, HireDate, IsActive, OpeningBalance, OpeningBalanceCurrency, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'فاطمة كريم', N'07766667777', N'بغداد', N'محاسبة', '2024-06-01', 1, 100000, N'IQD', NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'يوسف ماجد',  N'07866667777', N'البصرة', N'أمين مخزن', '2025-01-15', 1, 0, N'IQD', NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'هيوا كريم',  N'07566667777', N'أربيل', N'كاشير', '2025-04-01', 1, 50000, N'IQD', NULL);
SET IDENTITY_INSERT dbo.Employees OFF;

SET IDENTITY_INSERT dbo.Investors ON;
INSERT INTO dbo.Investors
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, Name, Phone, TotalDeposit, OpeningBalance, ProfitPercentage, CustomFieldsJson)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'المستثمر الأول', N'07777778888', 10000000, 10000000, 60.00, NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'المستثمر الثاني', N'07777779999',  5000000,  5000000, 40.00, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'مستثمر البصرة',  N'07877778888',  3000000,  3000000, 100.00, NULL),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'مستثمر أربيل',   N'07577778888',  2500000,  2500000, 100.00, NULL);
SET IDENTITY_INSERT dbo.Investors OFF;

------------------------------------------------------------
-- 6) إعدادات / صرف / ولاء / طباعة / رأس مال
------------------------------------------------------------
SET IDENTITY_INSERT dbo.BusinessSettings ON;
INSERT INTO dbo.BusinessSettings
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, ProductPricingEnabled, UpdateProductPriceOnPurchase, PeriodLockEnabled, LockedThroughDate, MultiCurrencyEnabled)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 1, 0, NULL, 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 1, 1, 0, NULL, 1),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 1, 0, 0, NULL, 0);
SET IDENTITY_INSERT dbo.BusinessSettings OFF;

SET IDENTITY_INSERT dbo.ExchangeRates ON;
INSERT INTO dbo.ExchangeRates (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, BranchId, RateDate, UsdToIqd, Notes) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, CAST(GETDATE() AS date), @Fx, N'سعر السوق'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, CAST(GETDATE() AS date), @Fx, N'سعر السوق'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, CAST(GETDATE() AS date), @Fx, N'سعر السوق');
SET IDENTITY_INSERT dbo.ExchangeRates OFF;

SET IDENTITY_INSERT dbo.LoyaltySettings ON;
INSERT INTO dbo.LoyaltySettings
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, PointsPerAmount, PointValueInCurrency, MinInvoiceAmountToEarn, MinPointsToRedeem,
 MaxRedeemPercentOfInvoice, PointsExpireAfterDays, EarnOnCreditSales, RoundEarnDown)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1000, 100, 10000, 10, 50, 365, 1, 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 1000, 100, 10000, 10, 50, NULL, 1, 1),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 1000, 100, 5000, 5, 40, 180, 1, 1);
SET IDENTITY_INSERT dbo.LoyaltySettings OFF;

SET IDENTITY_INSERT dbo.PrintBrandingSettings ON;
INSERT INTO dbo.PrintBrandingSettings
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, CompanyName, Address, PhonePrimary, PhoneSecondary, Email, Details,
 CompanyIdNumber, CompanyIdIssuer, ShowHeaderText, ShowHeaderImage, HeaderImageData, HeaderImageContentType,
 ShowFooterText, FooterText, ShowFooterImage, FooterImageData, FooterImageContentType)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'شركة المحاسب التجارية', N'بغداد — الكرادة', N'07700001111', N'07700002222', N'info@almuhasib.local', N'سجل تجاري 12345', N'', N'', 1, 0, NULL, NULL, 1, N'شكراً لتعاملكم معنا', 0, NULL, NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'شركة المحاسب — البصرة', N'البصرة — العشار', N'07800001111', N'', N'basra@almuhasib.local', N'', N'', N'', 1, 0, NULL, NULL, 1, N'شكراً لكم', 0, NULL, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'شركة المحاسب — أربيل', N'أربيل', N'07500001111', N'', N'erb@almuhasib.local', N'', N'', N'', 1, 0, NULL, NULL, 1, N'Spas', 0, NULL, NULL);
SET IDENTITY_INSERT dbo.PrintBrandingSettings OFF;

-- EntityKind فريد عالمياً: سجّل واحد لكل نوع (على الفرع الرئيسي)
SET IDENTITY_INSERT dbo.EntityCustomFieldSettings ON;
INSERT INTO dbo.EntityCustomFieldSettings
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, BranchId, EntityKind, DefinitionsJson)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'[{"key":"cf1","label":"بلد المنشأ","valueType":0,"isRequired":false}]'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, N'[{"key":"cf1","label":"رقم الهوية","valueType":0,"isRequired":false}]'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 3, N'[]'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 4, N'[]');
SET IDENTITY_INSERT dbo.EntityCustomFieldSettings OFF;

SET IDENTITY_INSERT dbo.CapitalEntries ON;
INSERT INTO dbo.CapitalEntries (Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy, BranchId, Amount, Date, Type, Notes) VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 15000000, '2025-01-01', N'Initial', N'رأس المال الافتتاحي — بغداد'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2,  5000000, '2025-01-01', N'Initial', N'رأس المال الافتتاحي — البصرة'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3,  3000000, '2025-01-01', N'Initial', N'رأس المال الافتتاحي — أربيل'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1,   500000, '2026-02-01', N'Adjustment', N'تعديل رأس مال'),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1,   200000, '2026-02-15', N'ProfitOpeningBalance', N'افتتاح أرباح مرحّلة');
SET IDENTITY_INSERT dbo.CapitalEntries OFF;

------------------------------------------------------------
-- 7) المخزون / دفعات / سيريال / مقاسات
------------------------------------------------------------
SET IDENTITY_INSERT dbo.WarehouseStocks ON;
INSERT INTO dbo.WarehouseStocks
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, WarehouseId, ProductId, Quantity, OpeningQuantity, UnitCost, MinQuantity)
VALUES
-- بغداد مخزن 1
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 1, 8,  10, 780000, 2),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 2, 12, 15, 220000, 3),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 3, 40, 50, 12000,  10),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 4, 80, 100, 9000,  20),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 5, 60, 80, 2500,  15),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 6, 45, 50, 1800,  10),
-- بغداد مخزن 2
(7, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, 4, 20, 20, 9000, 5),
-- البصرة
(8, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 3, 1, 3,  5, 780000, 1),
(9, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 3, 4, 40, 50, 9000, 10),
(10,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 3, 5, 30, 40, 2500, 8),
-- أربيل
(11,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 4, 4, 25, 30, 9000, 5),
(12,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 4, 6, 20, 25, 1800, 5),
(13,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 4, 5, 35, 40, 2500, 8),
(14,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 5, 4, 10, 10, 9000, 2),
(15,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 6, 4, 15, 15, 9000, 3);
SET IDENTITY_INSERT dbo.WarehouseStocks OFF;

SET IDENTITY_INSERT dbo.ProductBatches ON;
INSERT INTO dbo.ProductBatches
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, ProductId, WarehouseId, BatchNumber, ExpiryDate, Quantity)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 4, 1, N'RICE-2026-01', '2027-06-01', 50),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 5, 1, N'OIL-2026-02',  '2027-01-01', 40),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 4, 3, N'RICE-BSR-01',  '2027-03-01', 30),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 4, 4, N'RICE-ERB-01',  '2027-08-01', 25),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 6, 4, N'CLN-ERB-01',   '2028-01-01', 20);
SET IDENTITY_INSERT dbo.ProductBatches OFF;

SET IDENTITY_INSERT dbo.ProductSerials ON;
INSERT INTO dbo.ProductSerials
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, ProductId, WarehouseId, SerialNumber, IsSold, InvoiceItemId)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 1, N'SN-DELL-0001', 0, NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 1, N'SN-DELL-0002', 0, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, 1, N'SN-HP-0001',   0, NULL),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 1, 3, N'SN-DELL-BSR1', 0, NULL);
SET IDENTITY_INSERT dbo.ProductSerials OFF;

SET IDENTITY_INSERT dbo.ProductSizeStocks ON;
INSERT INTO dbo.ProductSizeStocks
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, ProductId, ProductSizeId, WarehouseId, Quantity)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 6, 1, 1, 30),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 6, 2, 1, 15),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 6, 1, 4, 12);
SET IDENTITY_INSERT dbo.ProductSizeStocks OFF;

------------------------------------------------------------
-- 8) الفواتير + البنود (كل الفروع + كل أنواع الفواتير)
------------------------------------------------------------
SET IDENTITY_INSERT dbo.Invoices ON;
INSERT INTO dbo.Invoices
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, InvoiceNumber, InvoiceType, CustomerId, SupplierId, DriverId, SalesRepresentativeId, WarehouseId,
 PaymentMethod, Currency, FxRate, TotalAmount, DiscountAmount, NetAmount,
 CompanyFeePercentage, CompanyFeeAmount, TransportFeeAmount, PurchaseExpenseAmount,
 RoundingAmount, RoundingType, CashBoxId, Date, CreditDueDate, Notes,
 PaidAmount, RemainingAmount, IsCreditPaid, HoldStatus, HeldAt, HoldNote,
 LoyaltyPointsEarned, LoyaltyPointsRedeemed, LoyaltyRedeemDiscountAmount, RelatedInvoiceId)
VALUES
-- شراء نقدي بغداد
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'PUR-1001', N'Purchase', NULL, 1, NULL, NULL, 1,
 N'Cash', N'IQD', 1, 1560000, 0, 1560000, 0, 0, 0, 0, 0, N'None', 1, '2026-03-01', NULL, N'شراء لابتوب',
 1560000, 0, 1, 0, NULL, NULL, 0, 0, 0, NULL),
-- بيع نقدي بغداد
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'SAL-2001', N'Sale', 1, NULL, 1, 1, 1,
 N'Cash', N'IQD', 1, 968500, 0, 968500, 0, 0, 15000, 0, 0, N'None', 1, '2026-03-05', NULL, N'بيع لابتوب + ماوس',
 968500, 0, 1, 0, NULL, NULL, 968, 0, 0, NULL),
-- بيع آجل بغداد
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'SAL-2002', N'Sale', 2, NULL, NULL, 1, 1,
 N'Credit', N'IQD', 1, 570000, 0, 570000, 0, 0, 0, 0, 0, N'None', NULL, '2026-03-08', '2026-04-08', N'بيع جملة آجل',
 200000, 370000, 0, 0, NULL, NULL, 0, 0, 0, NULL),
-- بيع أقساط بغداد
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'INS-3001', N'Installment', 3, NULL, NULL, NULL, 1,
 N'Installment', N'IQD', 1, 285000, 0, 285000, 0, 0, 0, 0, 0, N'None', NULL, '2026-03-10', NULL, N'طابعة بالأقساط',
 0, 285000, 0, 0, NULL, NULL, 0, 0, 0, NULL),
-- مرتجع بيع مرتبط بفاتورة 2
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'RET-2001', N'SaleReturn', 1, NULL, NULL, 1, 1,
 N'Cash', N'IQD', 1, 18500, 0, 18500, 0, 0, 0, 0, 0, N'None', 1, '2026-03-12', NULL, N'مرتجع ماوس',
 18500, 0, 1, 0, NULL, NULL, 0, 0, 0, 2),
-- بيع نقدي البصرة
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'SAL-BSR-01', N'Sale', 4, NULL, 2, 2, 3,
 N'Cash', N'IQD', 1, 25000, 0, 25000, 0, 0, 0, 0, 0, N'None', 3, '2026-03-15', NULL, N'بيع أرز',
 25000, 0, 1, 0, NULL, NULL, 25, 0, 0, NULL),
-- شراء البصرة
(7, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'PUR-BSR-01', N'Purchase', NULL, 3, NULL, NULL, 3,
 N'Credit', N'IQD', 1, 45000, 0, 45000, 0, 0, 0, 5000, 0, N'None', NULL, '2026-03-14', '2026-04-14', N'شراء أرز',
 0, 45000, 0, 0, NULL, NULL, 0, 0, 0, NULL),
-- مرتجع شراء بغداد (مرجع فاتورة 1)
(8, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'PRT-1001', N'PurchaseReturn', NULL, 1, NULL, NULL, 1,
 N'Cash', N'IQD', 1, 780000, 0, 780000, 0, 0, 0, 0, 0, N'None', 1, '2026-03-16', NULL, N'مرتجع لابتوب للمورد',
 780000, 0, 1, 0, NULL, NULL, 0, 0, 0, 1),
-- تالف بغداد
(9, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'DMG-1001', N'Damage', NULL, NULL, NULL, NULL, 1,
 N'Cash', N'IQD', 1, 2500, 0, 2500, 0, 0, 0, 0, 0, N'None', NULL, '2026-03-17', NULL, N'تالف زيت',
 0, 0, 1, 0, NULL, NULL, 0, 0, 0, NULL),
-- بيع أربيل
(10, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'SAL-ERB-01', N'Sale', 5, NULL, 3, 3, 4,
 N'Cash', N'IQD', 1, 15250, 0, 15250, 0, 0, 0, 0, 0, N'None', 5, '2026-03-18', NULL, N'بيع أرز + منظف',
 15250, 0, 1, 0, NULL, NULL, 15, 0, 0, NULL),
-- شراء أربيل
(11, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'PUR-ERB-01', N'Purchase', NULL, 4, NULL, NULL, 4,
 N'Cash', N'IQD', 1, 45000, 0, 45000, 0, 0, 0, 0, 0, N'None', 5, '2026-03-10', NULL, N'شراء أرز',
 45000, 0, 1, 0, NULL, NULL, 0, 0, 0, NULL),
-- أقساط أربيل
(12, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'INS-ERB-01', N'Installment', 7, NULL, NULL, 3, 4,
 N'Installment', N'IQD', 1, 12500, 0, 12500, 0, 0, 0, 0, 0, N'None', NULL, '2026-03-20', NULL, N'أرز بالأقساط',
 0, 12500, 0, 0, NULL, NULL, 0, 0, 0, NULL),
-- بيع دولار بغداد
(13, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'SAL-USD-01', N'Sale', 2, NULL, NULL, 1, 1,
 N'Cash', N'USD', @Fx, 215, 0, 215, 0, 0, 0, 0, 0, N'None', 2, '2026-03-22', NULL, N'بيع طابعة بالدولار',
 215, 0, 1, 0, NULL, NULL, 0, 0, 0, NULL);
SET IDENTITY_INSERT dbo.Invoices OFF;

SET IDENTITY_INSERT dbo.InvoiceItems ON;
INSERT INTO dbo.InvoiceItems
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, InvoiceId, ProductId, PricingTypeId, ItemName, Quantity, UnitPrice, DiscountPercent, DiscountAmount, TotalPrice, WarehouseId, IsOfferGift, OfferId, CustomFieldsJson)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 1, 1, N'لابتوب ديل XPS', 2, 780000, 0, 0, 1560000, 1, 0, NULL, NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, 1, 1, N'لابتوب ديل XPS', 1, 950000, 0, 0, 950000, 1, 0, NULL, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, 3, 1, N'ماوس لاسلكي', 1, 18500, 0, 0, 18500, 1, 0, NULL, NULL),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 3, 2, 2, N'طابعة HP LaserJet', 2, 285000, 0, 0, 570000, 1, 0, NULL, NULL),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 4, 2, 1, N'طابعة HP LaserJet', 1, 285000, 0, 0, 285000, 1, 0, NULL, NULL),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 5, 3, 1, N'ماوس لاسلكي', 1, 18500, 0, 0, 18500, 1, 0, NULL, NULL),
(7, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 6, 4, 1, N'أرز عنبر 5 كغ', 2, 12500, 0, 0, 25000, 3, 0, NULL, NULL),
(8, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 7, 4, 1, N'أرز عنبر 5 كغ', 5, 9000, 0, 0, 45000, 3, 0, NULL, NULL),
(9, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 8, 1, 1, N'لابتوب ديل XPS', 1, 780000, 0, 0, 780000, 1, 0, NULL, NULL),
(10,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 9, 5, 1, N'زيت نباتي 1 لتر', 1, 2500, 0, 0, 2500, 1, 0, NULL, NULL),
(11,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 10, 4, 1, N'أرز عنبر 5 كغ', 1, 12500, 0, 0, 12500, 4, 0, NULL, NULL),
(12,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 10, 6, 1, N'منظف أرضيات 1 لتر', 1, 2750, 0, 0, 2750, 4, 0, NULL, NULL),
(13,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 11, 4, 1, N'أرز عنبر 5 كغ', 5, 9000, 0, 0, 45000, 4, 0, NULL, NULL),
(14,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 12, 4, 1, N'أرز عنبر 5 كغ', 1, 12500, 0, 0, 12500, 4, 0, NULL, NULL),
(15,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 13, 2, 1, N'طابعة HP LaserJet', 1, 215, 0, 0, 215, 1, 0, NULL, NULL);
SET IDENTITY_INSERT dbo.InvoiceItems OFF;

------------------------------------------------------------
-- 9) الأقساط
------------------------------------------------------------
SET IDENTITY_INSERT dbo.InstallmentPlans ON;
INSERT INTO dbo.InstallmentPlans
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, InvoiceId, CustomerId, FileNumber, TotalAmount, NumberOfInstallments, InstallmentAmount, StartDate,
 InstallmentType, CompanyFeePercentage, CompanyFeeAmount, GuarantorName, GuarantorPhone, SalespersonUserId, CollectionCommissionRate)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 4, 3, N'INS-F-001', 285000, 3, 95000, '2026-04-01', 0, 0, 0, N'علي أحمد', N'07788889999', 2, 0.02),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 12, 7, N'INS-ERB-001', 12500, 2, 6250, '2026-04-01', 0, 0, 0, NULL, NULL, 4, 0);
SET IDENTITY_INSERT dbo.InstallmentPlans OFF;

SET IDENTITY_INSERT dbo.Installments ON;
INSERT INTO dbo.Installments
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, InstallmentPlanId, DueDate, Amount, PaidAmount, RemainingAmount, Status, PaymentDate, CashBoxId)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, '2026-04-01', 95000, 95000, 0,     N'Paid',    '2026-04-01', 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, '2026-05-01', 95000, 0,     95000, N'Pending', NULL, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, '2026-06-01', 95000, 0,     95000, N'Pending', NULL, NULL),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 2, '2026-04-01', 6250,  0,     6250,  N'Pending', NULL, NULL),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 2, '2026-05-01', 6250,  0,     6250,  N'Pending', NULL, NULL);
SET IDENTITY_INSERT dbo.Installments OFF;

------------------------------------------------------------
-- 10) السندات / المصاريف / التحويلات / الصيرفة
------------------------------------------------------------
SET IDENTITY_INSERT dbo.Vouchers ON;
INSERT INTO dbo.Vouchers
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, VoucherNumber, VoucherType, Currency, FxRate, Amount, BankFees,
 CustomerId, SupplierId, InvestorId, EmployeeId, CashBoxId, BankAccountId, Date, Notes,
 InvoiceId, InstallmentId, IsReconciled, ReconciledAt, ReconciledBy)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'V-R-001', N'Receipt', N'IQD', 1, 200000, 0, 2, NULL, NULL, NULL, 1, NULL, '2026-03-09', N'دفعة على فاتورة آجلة', 3, NULL, 0, NULL, NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'V-P-001', N'Payment', N'IQD', 1, 500000, 0, NULL, 1, NULL, NULL, 1, NULL, '2026-03-02', N'دفعة لمورد', NULL, NULL, 0, NULL, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'V-R-002', N'Receipt', N'IQD', 1, 95000, 0, 3, NULL, NULL, NULL, 1, NULL, '2026-04-01', N'قسط أول', 4, 1, 0, NULL, NULL),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'V-ID-001', N'InvestorDeposit', N'IQD', 1, 2000000, 0, NULL, NULL, 1, NULL, 1, NULL, '2025-01-02', N'إيداع مستثمر', NULL, NULL, 0, NULL, NULL),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'V-BR-001', N'BankReceipt', N'IQD', 1, 1000000, 5000, NULL, NULL, NULL, NULL, 1, 1, '2026-03-20', N'سحب من البنك للقاصة', NULL, NULL, 0, NULL, NULL),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'V-BSR-01', N'Receipt', N'IQD', 1, 25000, 0, 4, NULL, NULL, NULL, 3, NULL, '2026-03-15', N'قبض بيع', 6, NULL, 0, NULL, NULL),
(7, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'V-IW-001', N'InvestorWithdrawal', N'IQD', 1, 100000, 0, NULL, NULL, 2, NULL, 1, NULL, '2026-03-25', N'سحب مستثمر', NULL, NULL, 0, NULL, NULL),
(8, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'V-DR-001', N'DebtReceipt', N'IQD', 1, 50000, 0, 1, NULL, NULL, NULL, 1, NULL, '2026-03-26', N'قبض ذمة عامة', NULL, NULL, 0, NULL, NULL),
(9, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'V-EMP-01', N'Payment', N'IQD', 1, 50000, 0, NULL, NULL, NULL, 1, 1, NULL, '2026-03-27', N'سلفة موظف', NULL, NULL, 0, NULL, NULL),
(10,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'V-ERB-01', N'Receipt', N'IQD', 1, 15250, 0, 5, NULL, NULL, NULL, 5, NULL, '2026-03-18', N'قبض بيع أربيل', 10, NULL, 0, NULL, NULL),
(11,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'V-ERB-02', N'Payment', N'IQD', 1, 45000, 0, NULL, 4, NULL, NULL, 5, NULL, '2026-03-10', N'دفع لمورد أربيل', 11, NULL, 0, NULL, NULL),
(12,NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'V-BSR-02', N'Payment', N'IQD', 1, 20000, 0, NULL, 3, NULL, NULL, 3, NULL, '2026-03-16', N'دفعة جزئية لمورد', 7, NULL, 0, NULL, NULL);
SET IDENTITY_INSERT dbo.Vouchers OFF;

SET IDENTITY_INSERT dbo.Expenses ON;
INSERT INTO dbo.Expenses
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, ExpenseTypeId, Currency, FxRate, Amount, Date, CashBoxId, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'IQD', 1, 1500000, '2026-03-01', 1, N'إيجار شهر آذار'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, N'IQD', 1,  250000, '2026-03-05', 1, N'فاتورة كهرباء'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 4, N'IQD', 1,  800000, '2026-03-01', 3, N'إيجار البصرة'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 5, N'IQD', 1,   75000, '2026-03-12', 3, N'أجور توصيل'),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 6, N'IQD', 1,  600000, '2026-03-01', 5, N'إيجار أربيل'),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 7, N'IQD', 1,  120000, '2026-03-08', 5, N'كهرباء أربيل'),
(7, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 8, N'IQD', 1,   80000, '2026-03-11', 1, N'صيانة طابعة');
SET IDENTITY_INSERT dbo.Expenses OFF;

SET IDENTITY_INSERT dbo.Transfers ON;
INSERT INTO dbo.Transfers
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, FromType, FromId, ToType, ToId, Currency, FxRate, Amount, Date, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'CashBox', 1, N'Bank', 1, N'IQD', 1, 500000, '2026-03-18', N'إيداع قاصة → بنك'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'Bank', 1, N'CashBox', 1, N'IQD', 1, 200000, '2026-03-19', N'سحب بنك → قاصة'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'CashBox', 3, N'Bank', 3, N'IQD', 1, 300000, '2026-03-18', N'إيداع بصرة'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'CashBox', 5, N'Bank', 4, N'IQD', 1, 200000, '2026-03-19', N'إيداع أربيل');
SET IDENTITY_INSERT dbo.Transfers OFF;

SET IDENTITY_INSERT dbo.CurrencyExchanges ON;
INSERT INTO dbo.CurrencyExchanges
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, FromCashBoxId, ToCashBoxId, FromCurrency, ToCurrency, FromAmount, ToAmount, FxRate, Date, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, 1, N'USD', N'IQD', 100, 132000, @Fx, '2026-03-21', N'صرف 100$ إلى دينار'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 4, 3, N'USD', N'IQD', 50, 66000, @Fx, '2026-03-21', N'صرف بصرة'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 6, 5, N'USD', N'IQD', 20, 26400, @Fx, '2026-03-22', N'صرف أربيل');
SET IDENTITY_INSERT dbo.CurrencyExchanges OFF;

------------------------------------------------------------
-- 11) نقل مخزني / مستثمرين / أرباح / مناديب / ولاء
------------------------------------------------------------
SET IDENTITY_INSERT dbo.WarehouseTransfers ON;
INSERT INTO dbo.WarehouseTransfers
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, TransferNumber, FromWarehouseId, ToWarehouseId, Date, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'WT-001', 1, 2, '2026-03-07', N'نقل أرز للمخزن الفرعي'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'WT-BSR-01', 3, 6, '2026-03-16', N'نقل أرز للزبير'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, N'WT-ERB-01', 4, 5, '2026-03-19', N'نقل أرز للمخزن الفرعي');
SET IDENTITY_INSERT dbo.WarehouseTransfers OFF;

SET IDENTITY_INSERT dbo.WarehouseTransferItems ON;
INSERT INTO dbo.WarehouseTransferItems
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, WarehouseTransferId, ProductId, Quantity)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 4, 10),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 2, 4, 5),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 3, 4, 5);
SET IDENTITY_INSERT dbo.WarehouseTransferItems OFF;

SET IDENTITY_INSERT dbo.InvestorTransactions ON;
INSERT INTO dbo.InvestorTransactions
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, InvestorId, Type, Amount, Date, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'OpeningBalance', 10000000, '2025-01-01', N'رصيد افتتاحي'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'Deposit', 2000000, '2025-01-02', N'إيداع إضافي'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, N'OpeningBalance', 5000000, '2025-01-01', N'رصيد افتتاحي'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, N'Withdrawal', 100000, '2026-03-25', N'سحب جزئي'),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'ProfitDistribution', 600000, '2026-02-28', N'حصة أرباح'),
(6, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 3, N'OpeningBalance', 3000000, '2025-01-01', N'رصيد افتتاحي بصرة'),
(7, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 4, N'OpeningBalance', 2500000, '2025-01-01', N'رصيد افتتاحي أربيل'),
(8, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 4, N'Deposit', 500000, '2026-02-01', N'إيداع أربيل');
SET IDENTITY_INSERT dbo.InvestorTransactions OFF;

SET IDENTITY_INSERT dbo.ProfitDistributions ON;
INSERT INTO dbo.ProfitDistributions
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, Date, TotalProfit, DistributedAmount)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, '2026-02-28', 1000000, 1000000),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, '2026-02-28', 200000, 200000),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, '2026-02-28', 150000, 150000);
SET IDENTITY_INSERT dbo.ProfitDistributions OFF;

SET IDENTITY_INSERT dbo.ProfitDistributionDetails ON;
INSERT INTO dbo.ProfitDistributionDetails
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, ProfitDistributionId, InvestorId, ProfitPercentage, Amount)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 1, 60, 600000),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 2, 40, 400000),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 2, 3, 100, 200000),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 3, 4, 100, 150000);
SET IDENTITY_INSERT dbo.ProfitDistributionDetails OFF;

SET IDENTITY_INSERT dbo.SalesRepCommissionRules ON;
INSERT INTO dbo.SalesRepCommissionRules
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, SalesRepresentativeId, CommissionType, Percentage, FixedAmount, ProductId, CustomerId, IsActive, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'PercentOfSales', 2, 0, NULL, NULL, 1, N'عمولة 2% من المبيعات'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 2, N'PercentOfSales', 1.5, 0, NULL, NULL, 1, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 3, N'PercentOfSales', 2, 0, NULL, NULL, 1, NULL),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'FixedPerInvoice', 0, 5000, NULL, NULL, 1, N'ثابت لكل فاتورة'),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'ByProduct', 1, 0, 1, NULL, 1, N'عمولة على اللابتوب');
SET IDENTITY_INSERT dbo.SalesRepCommissionRules OFF;

SET IDENTITY_INSERT dbo.SalesRepCommissionEntries ON;
INSERT INTO dbo.SalesRepCommissionEntries
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, SalesRepresentativeId, InvoiceId, CustomerId, InvoiceDate, CommissionType, BaseAmount, CommissionAmount, PaidAmount, Currency, Status, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 2, 1, '2026-03-05', N'PercentOfSales', 968500, 19370, 0, N'IQD', N'Unpaid', NULL),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 2, 6, 4, '2026-03-15', N'PercentOfSales', 25000, 375, 0, N'IQD', N'Unpaid', NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 3, 10, 5, '2026-03-18', N'PercentOfSales', 15250, 305, 0, N'IQD', N'Unpaid', NULL),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 3, 2, '2026-03-08', N'PercentOfSales', 570000, 11400, 5000, N'IQD', N'Partial', N'دفعة جزئية');
SET IDENTITY_INSERT dbo.SalesRepCommissionEntries OFF;

SET IDENTITY_INSERT dbo.SalesRepTargets ON;
INSERT INTO dbo.SalesRepTargets
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, SalesRepresentativeId, PeriodStart, PeriodEnd, TargetAmount, Notes)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, '2026-03-01', '2026-03-31', 5000000, N'هدف آذار'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 2, '2026-03-01', '2026-03-31', 2000000, NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 3, '2026-03-01', '2026-03-31', 1500000, N'هدف أربيل');
SET IDENTITY_INSERT dbo.SalesRepTargets OFF;

SET IDENTITY_INSERT dbo.SalesRepCollections ON;
INSERT INTO dbo.SalesRepCollections
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, SalesRepresentativeId, CustomerId, Amount, Currency, CollectionDate, ReceiptNumber, PaymentMethod, HandedOverAmount, HandedOverAt, Notes, InvoiceId)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 2, 200000, N'IQD', '2026-03-09', N'RC-001', N'Cash', 200000, '2026-03-09', N'تحصيل آجل', 3),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 2, 4, 50000, N'IQD', '2026-03-20', N'RC-BSR-01', N'Cash', 0, NULL, N'لم يُسلَّم بعد', NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 3, 5, 10000, N'IQD', '2026-03-21', N'RC-ERB-01', N'Cash', 10000, '2026-03-21', NULL, NULL);
SET IDENTITY_INSERT dbo.SalesRepCollections OFF;

SET IDENTITY_INSERT dbo.CustomerLoyaltyAccounts ON;
INSERT INTO dbo.CustomerLoyaltyAccounts
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, CustomerId, PointsBalance, LifetimeEarned, LifetimeRedeemed, Tier, LastEarnedAt, LastRedeemedAt)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 868, 968, 100, N'Silver', '2026-03-05', '2026-03-20'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 4, 25, 25, 0, N'Standard', '2026-03-15', NULL),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 5, 15, 15, 0, N'Standard', '2026-03-18', NULL);
SET IDENTITY_INSERT dbo.CustomerLoyaltyAccounts OFF;

SET IDENTITY_INSERT dbo.LoyaltyPointTransactions ON;
INSERT INTO dbo.LoyaltyPointTransactions
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, CustomerId, InvoiceId, Type, Points, UnitValue, CurrencyAmount, BalanceAfter, Note, CreatedByUserId)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, 2, N'Earn', 968, 100, 96800, 968, N'كسب من فاتورة بيع', 1),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 4, 6, N'Earn', 25, 100, 2500, 25, N'كسب من بيع البصرة', 1),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 5, 10, N'Earn', 15, 100, 1500, 15, N'كسب أربيل', 4),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, NULL, N'Redeem', -100, 100, 10000, 868, N'استبدال نقاط', 1),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, NULL, N'Adjust', 0, 100, 0, 868, N'مراجعة رصيد', 1);
SET IDENTITY_INSERT dbo.LoyaltyPointTransactions OFF;

SET IDENTITY_INSERT dbo.CustomerAttachments ON;
INSERT INTO dbo.CustomerAttachments
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 BranchId, CustomerId, FileName, FilePath, Description)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'هوية.pdf', N'C:\Attachments\Customers\1\id.pdf', N'صورة هوية'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 4, N'عقد.pdf', N'C:\Attachments\Customers\4\contract.pdf', N'عقد تعامل'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 5, N'هوية.pdf', N'C:\Attachments\Customers\5\id.pdf', N'هوية زبون أربيل');
SET IDENTITY_INSERT dbo.CustomerAttachments OFF;

------------------------------------------------------------
-- 12) مهام / ملاحظات / تدقيق / دخول / مزامنة
------------------------------------------------------------
SET IDENTITY_INSERT dbo.UserTasks ON;
INSERT INTO dbo.UserTasks
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 UserId, AssignedByUserId, Title, Details, Status, DueDate)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, 1, N'مراجعة فواتير آذار', N'التأكد من تسديد الآجل', N'Pending', '2026-03-31'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 3, 1, N'جرد مخزن البصرة', NULL, N'InProgress', '2026-03-25'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 4, 1, N'تقرير مبيعات أربيل', N'إرسال تقرير أسبوعي', N'Completed', '2026-03-20');
SET IDENTITY_INSERT dbo.UserTasks OFF;

SET IDENTITY_INSERT dbo.UserNotes ON;
INSERT INTO dbo.UserNotes
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 UserId, Title, Content, LastEditedAt)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'ملاحظات افتتاح', N'تم تجهيز بيانات تجريبية للفحص', @Now),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 4, N'ملاحظات أربيل', N'التحقق من أسعار الصرف أسبوعياً', @Now);
SET IDENTITY_INSERT dbo.UserNotes OFF;

SET IDENTITY_INSERT dbo.AuditLogs ON;
INSERT INTO dbo.AuditLogs
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 UserId, BranchId, Action, EntityName, EntityId, OldValues, NewValues, Timestamp, IpAddress, DeviceInfo)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'Add', N'Invoice', 2, NULL, N'{"InvoiceNumber":"SAL-2001"}', @Now, N'127.0.0.1', N'Seed'),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 2, N'Add', N'Invoice', 6, NULL, N'{"InvoiceNumber":"SAL-BSR-01"}', @Now, N'127.0.0.1', N'Seed'),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 4, 3, N'Add', N'Invoice', 10, NULL, N'{"InvoiceNumber":"SAL-ERB-01"}', @Now, N'127.0.0.1', N'Seed'),
(4, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'Edit', N'Customer', 1, N'{"Phone":"07700000000"}', N'{"Phone":"07711112222"}', @Now, N'127.0.0.1', N'Seed'),
(5, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, 1, N'Delete', N'Expense', 0, N'{"Amount":1000}', NULL, @Now, N'127.0.0.1', N'Seed');
SET IDENTITY_INSERT dbo.AuditLogs OFF;

SET IDENTITY_INSERT dbo.UserLoginLogs ON;
INSERT INTO dbo.UserLoginLogs
(Id, SyncId, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted, DeletedAt, DeletedBy,
 UserId, Username, LoginAt, LogoutAt, MachineName)
VALUES
(1, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 1, N'admin', DATEADD(hour, -2, @Now), DATEADD(hour, -1, @Now), HOST_NAME()),
(2, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 2, N'baghdad', DATEADD(hour, -5, @Now), DATEADD(hour, -4, @Now), HOST_NAME()),
(3, NEWID(), @Now, @By, NULL, NULL, 0, NULL, NULL, 4, N'erbil', DATEADD(hour, -3, @Now), NULL, HOST_NAME());
SET IDENTITY_INSERT dbo.UserLoginLogs OFF;

INSERT INTO dbo.SyncStates (EntityType, LastPulledAt, LastPushedAt, ServerCursor) VALUES
(N'Product',  @Now, @Now, N'0'),
(N'Customer', @Now, @Now, N'0'),
(N'Invoice',  @Now, NULL, N'0'),
(N'Voucher',  @Now, @Now, N'0'),
(N'Supplier', @Now, NULL, N'0');

SET IDENTITY_INSERT dbo.CloudSyncSettings ON;
INSERT INTO dbo.CloudSyncSettings
(Id, ApiBaseUrl, Username, Password, AutoSyncEnabled, AutoSyncIntervalMinutes, LastSuccessfulSyncAt, LastSyncError, AccessToken, RefreshToken, AccessTokenExpiresAt)
VALUES
(1, N'', N'', N'', 0, 15, NULL, NULL, NULL, NULL, NULL);
SET IDENTITY_INSERT dbo.CloudSyncSettings OFF;

------------------------------------------------------------
-- 13) إعادة تفعيل القيود
------------------------------------------------------------
SET @sql = N'';
SELECT @sql = @sql + N'ALTER TABLE ' + QUOTENAME(OBJECT_SCHEMA_NAME(fk.parent_object_id))
    + N'.' + QUOTENAME(OBJECT_NAME(fk.parent_object_id))
    + N' WITH CHECK CHECK CONSTRAINT ' + QUOTENAME(fk.name) + N';' + CHAR(10)
FROM sys.foreign_keys AS fk;
IF LEN(@sql) > 0
    EXEC sys.sp_executesql @sql;

COMMIT TRANSACTION;

PRINT N'تم بنجاح: بيانات كاملة لكل جداول المحاسبة على 3 فروع.';
PRINT N'المستخدمون: admin / baghdad / basra / erbil  — كلمة المرور للجميع: admin';
GO
