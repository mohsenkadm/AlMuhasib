using AlMuhasib.Core.Enums;
using AlMuhasib.Core.Models.Ux;
using AlMuhasib.UI.Models;
using AlMuhasib.UI.Modules;
using AlMuhasib.UI.ViewModels;
using AlMuhasib.UI.ViewModels.Car;
using AlMuhasib.UI.ViewModels.Hotel;
using MaterialDesignThemes.Wpf;

namespace AlMuhasib.UI.Services;

public sealed class VoiceCommandCatalog
{
    private static readonly Dictionary<string, (Type Vm, string Title, PackIconKind Icon, string[] Phrases)> AccountingCommands =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["Dashboard"] = (typeof(DashboardViewModel), "لوحة التحكم", PackIconKind.ViewDashboard,
                ["لوحة التحكم", "الرئيسية", "الداشبورد", "dashboard", "الصفحة الرئيسية"]),
            ["Products"] = (typeof(ProductsViewModel), "المنتجات", PackIconKind.PackageVariantClosed,
                ["المنتجات", "منتجات", "المخزون", "الاصناف", "الأصناف"]),
            ["Customers"] = (typeof(CustomersViewModel), "العملاء", PackIconKind.AccountGroup,
                ["العملاء", "عميل", "زبائن", "الزبائن"]),
            ["Suppliers"] = (typeof(SuppliersViewModel), "الموردون", PackIconKind.TruckDelivery,
                ["الموردين", "الموردون", "مورد", "المورد"]),
            ["SaleInvoice"] = (typeof(SalesInvoiceViewModel), "فاتورة مبيعات", PackIconKind.CashRegister,
                ["فاتورة مبيعات", "مبيعات", "بيع", "فاتورة بيع", "مبيعات جديدة", "انشاء فاتورة مبيعات"]),
            ["PurchaseInvoice"] = (typeof(PurchaseInvoiceViewModel), "فاتورة مشتريات", PackIconKind.CartArrowDown,
                ["فاتورة مشتريات", "مشتريات", "شراء", "فاتورة شراء"]),
            ["InstallmentInvoice"] = (typeof(InstallmentInvoiceViewModel), "فاتورة أقساط", PackIconKind.CalendarClock,
                ["فاتورة اقساط", "فاتورة أقساط", "اقساط", "أقساط"]),
            ["Installments"] = (typeof(InstallmentsViewModel), "الأقساط", PackIconKind.CalendarClock,
                ["لوحة الاقساط", "الاقساط", "تحصيل الاقساط"]),
            ["Vouchers"] = (typeof(VouchersViewModel), "السندات", PackIconKind.FileDocument,
                ["السندات", "سند", "سندات"]),
            ["Expenses"] = (typeof(ExpenseViewModel), "المصاريف", PackIconKind.CashMinus,
                ["المصاريف", "مصروف", "مصاريف"]),
            ["CashAndBank"] = (typeof(CashBankViewModel), "القاصات والمصرف", PackIconKind.Bank,
                ["القاصات", "المصرف", "البنك", "الصندوق"]),
            ["Warehouses"] = (typeof(WarehousesViewModel), "المخازن", PackIconKind.Warehouse,
                ["المخازن", "مخزن", "المخزن"]),
            ["Reports"] = (typeof(SalesReportViewModel), "التقارير", PackIconKind.ChartLine,
                ["التقارير", "تقرير", "تقارير"]),
            ["Backup"] = (typeof(BackupRestoreViewModel), "النسخ الاحتياطي", PackIconKind.BackupRestore,
                ["النسخ الاحتياطي", "نسخ احتياطي", "backup", "نسخة احتياطية"]),
            ["Users"] = (typeof(UsersViewModel), "المستخدمون", PackIconKind.AccountCog,
                ["المستخدمين", "المستخدمون", "مستخدمين"]),
        };

    public IReadOnlyList<VoiceCommandDefinition> Build(
        SystemModuleRegistry moduleRegistry,
        Func<Type, bool> canOpenScreen,
        IReadOnlyList<NavigationMenuItem> menuItems)
    {
        var commands = new List<VoiceCommandDefinition>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        void Add(VoiceCommandDefinition cmd)
        {
            if (!seen.Add(cmd.Id))
                return;
            commands.Add(cmd);
        }

        Add(new VoiceCommandDefinition
        {
            Id = "global-search",
            DisplayLabel = "بحث",
            Phrases = ["بحث", "ابحث", "البحث", "بحث شامل"],
            ActionType = VoiceCommandActionType.OpenGlobalSearch,
            SuccessMessage = "تم فتح البحث"
        });

        Add(new VoiceCommandDefinition
        {
            Id = "quick-pos",
            DisplayLabel = "بيع سريع",
            Phrases = ["بيع سريع", "نقطة بيع", "pos", "بيع سريع pos"],
            ActionType = VoiceCommandActionType.QuickPosSale,
            ViewModelType = typeof(PosQuickSaleViewModel),
            TabTitle = "بيع سريع (POS)",
            SuccessMessage = "تم فتح بيع سريع"
        });

        Add(new VoiceCommandDefinition
        {
            Id = "quick-sale",
            DisplayLabel = "فاتورة مبيعات",
            Phrases = ["فاتورة جديدة", "فاتورة مبيعات سريعة"],
            ActionType = VoiceCommandActionType.QuickNewSale,
            SuccessMessage = "تم فتح فاتورة المبيعات"
        });

        Add(new VoiceCommandDefinition
        {
            Id = "close",
            DisplayLabel = "إغلاق",
            Phrases = ["اغلاق", "إغلاق", "الغاء", "إلغاء", "اخرج", "خروج"],
            ActionType = VoiceCommandActionType.CloseAssistant,
            SuccessMessage = string.Empty
        });

        Add(new VoiceCommandDefinition
        {
            Id = "help",
            DisplayLabel = "مساعدة",
            Phrases = ["مساعدة", "ساعدني", "ماذا يمكنك", "الاوامر", "الأوامر"],
            ActionType = VoiceCommandActionType.ShowHelp,
            SuccessMessage = string.Empty
        });

        if (moduleRegistry.IsCarContracts)
            AddCarCommands(Add, canOpenScreen);
        else if (moduleRegistry.IsHotelManagement)
            AddHotelCommands(Add, canOpenScreen);
        else
            AddAccountingCommands(Add, canOpenScreen);

        foreach (var menu in menuItems.Where(m => m.ViewModelType is not null && !m.IsGroupHeader))
        {
            if (!canOpenScreen(menu.ViewModelType!))
                continue;

            var screen = ScreenPermissionRegistry.GetScreenName(menu.ViewModelType!);
            if (seen.Contains($"menu:{screen}"))
                continue;

            Add(new VoiceCommandDefinition
            {
                Id = $"menu:{screen}",
                DisplayLabel = menu.Title,
                Phrases = [menu.Title, NormalizeMenuTitle(menu.Title)],
                ActionType = VoiceCommandActionType.OpenScreen,
                ViewModelType = menu.ViewModelType,
                ScreenName = screen,
                TabTitle = menu.Title,
                SuccessMessage = $"تم فتح {menu.Title}"
            });
        }

        return commands;
    }

    private static void AddAccountingCommands(Action<VoiceCommandDefinition> add, Func<Type, bool> canOpen)
    {
        foreach (var (screen, def) in AccountingCommands)
        {
            if (!canOpen(def.Vm))
                continue;

            add(new VoiceCommandDefinition
            {
                Id = $"screen:{screen}",
                DisplayLabel = def.Title,
                Phrases = def.Phrases,
                ActionType = VoiceCommandActionType.OpenScreen,
                ViewModelType = def.Vm,
                ScreenName = screen,
                TabTitle = def.Title,
                SuccessMessage = $"تم فتح {def.Title}"
            });
        }
    }

    private static void AddCarCommands(Action<VoiceCommandDefinition> add, Func<Type, bool> canOpen)
    {
        if (canOpen(typeof(CarDashboardViewModel)))
            add(MakeScreen("car-dashboard", typeof(CarDashboardViewModel), "لوحة التحكم", PackIconKind.ViewDashboard,
                ["لوحة التحكم", "الرئيسية"]));
        if (canOpen(typeof(CarContractFormViewModel)))
            add(MakeScreen("car-new", typeof(CarContractFormViewModel), "عقد جديد", PackIconKind.FileDocumentPlus,
                ["عقد جديد", "انشاء عقد", "عقد"]));
        if (canOpen(typeof(CarContractsViewModel)))
            add(MakeScreen("car-list", typeof(CarContractsViewModel), "العقود", PackIconKind.FormatListBulleted,
                ["العقود", "عقود"]));
    }

    private static void AddHotelCommands(Action<VoiceCommandDefinition> add, Func<Type, bool> canOpen)
    {
        if (canOpen(typeof(HotelDashboardViewModel)))
            add(MakeScreen("hotel-dashboard", typeof(HotelDashboardViewModel), "لوحة التحكم", PackIconKind.ViewDashboard,
                ["لوحة التحكم", "الرئيسية"]));
        if (canOpen(typeof(HotelReservationFormViewModel)))
            add(MakeScreen("hotel-reservation", typeof(HotelReservationFormViewModel), "حجز جديد", PackIconKind.CalendarPlus,
                ["حجز جديد", "حجز", "انشاء حجز"]));
        if (canOpen(typeof(HotelCheckInOutViewModel)))
            add(MakeScreen("hotel-checkin", typeof(HotelCheckInOutViewModel), "تسجيل دخول/خروج", PackIconKind.Login,
                ["تسجيل دخول", "دخول وخروج", "check in"]));
        if (canOpen(typeof(HotelRoomsViewModel)))
            add(MakeScreen("hotel-rooms", typeof(HotelRoomsViewModel), "الغرف", PackIconKind.Door,
                ["الغرف", "غرف"]));
    }

    private static VoiceCommandDefinition MakeScreen(string id, Type vm, string title, PackIconKind _, string[] phrases) =>
        new()
        {
            Id = id,
            DisplayLabel = title,
            Phrases = phrases,
            ActionType = VoiceCommandActionType.OpenScreen,
            ViewModelType = vm,
            TabTitle = title,
            SuccessMessage = $"تم فتح {title}"
        };

    private static string NormalizeMenuTitle(string title) =>
        title.Replace("ـ", string.Empty, StringComparison.Ordinal).Trim();
}
