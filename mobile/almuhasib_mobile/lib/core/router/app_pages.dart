import 'package:get/get.dart';

import '../bindings/accounting_feature_bindings.dart';
import '../bindings/accounting_shell_binding.dart';
import '../bindings/auth_binding.dart';
import '../bindings/car_bindings.dart';
import '../bindings/car_trade_bindings.dart';
import '../bindings/hotel_bindings.dart';
import '../../features/auth/presentation/login_screen.dart';
import '../../features/car/car_shell.dart';
import '../../features/car/contracts/car_contract_detail_screen.dart';
import '../../features/car/contracts/car_contract_form_screen.dart';
import '../../features/car/contracts/car_contracts_screen.dart';
import '../../features/car/dashboard/car_dashboard_screen.dart';
import '../../features/car/payments/car_payments_screen.dart';
import '../../features/car/reports/car_report_screen.dart';
import '../../features/car_trade/car_trade_shell.dart';
import '../../features/car_trade/reports/car_trade_party_statement_screen.dart';
import '../../features/car_trade/transactions/car_trade_transaction_detail_screen.dart';
import '../../features/car_trade/transactions/car_trade_transaction_form_screen.dart';
import '../../features/dashboard/presentation/dashboard_screen.dart';
import '../../features/data_tab/presentation/data_list_screen.dart';
import '../../features/data_tab/presentation/data_screen.dart';
import '../../features/data_tab/presentation/pricing_type_form_screen.dart';
import '../../features/data_tab/presentation/pricing_types_screen.dart';
import '../../features/data_tab/presentation/product_price_form_screen.dart';
import '../../features/data_tab/presentation/product_prices_screen.dart';
import '../../features/finance/presentation/finance_list_screen.dart';
import '../../features/hotel/check_in_out/hotel_check_in_out_screen.dart';
import '../../features/hotel/dashboard/hotel_dashboard_screen.dart';
import '../../features/hotel/guests/hotel_guest_form_screen.dart';
import '../../features/hotel/guests/hotel_guests_screen.dart';
import '../../features/hotel/hotel_shell.dart';
import '../../features/hotel/models/hotel_models.dart'
    hide ApplicationSystemType;
import '../../features/hotel/reservations/hotel_reservation_detail_screen.dart';
import '../../features/hotel/reservations/hotel_reservation_form_screen.dart';
import '../../features/hotel/reservations/hotel_reservations_screen.dart';
import '../../features/hotel/restaurant/pos/restaurant_hub_screen.dart';
import '../../features/hotel/rooms/hotel_rooms_screen.dart';
import '../../features/installments/presentation/installments_screens.dart';
import '../../features/notifications/notifications_screen.dart';
import '../../features/offline/presentation/pending_sync_screen.dart';
import '../../features/onboarding/onboarding_screen.dart';
import '../../features/operations/presentation/forms/customer_form_screen.dart';
import '../../features/operations/presentation/forms/entity_forms.dart';
import '../../features/operations/presentation/forms/finance/finance_entity_forms.dart';
import '../../features/operations/presentation/forms/finance/finance_transaction_forms.dart';
import '../../features/operations/presentation/invoice_wizard/invoice_wizard_screen.dart';
import '../../features/profile/about_screen.dart';
import '../../features/profile/privacy_policy_screen.dart';
import '../../features/profile/profile_screen.dart';
import '../../features/reports/presentation/report_detail_screen.dart';
import '../../features/reports/presentation/reports_screen.dart';
import '../../features/settings/settings_screen.dart';
import '../../features/shell/main_shell.dart';
import '../../features/splash/splash_screen.dart';
import '../../features/system/system_launch_screen.dart';
import '../../shared/models/master_data_models.dart';
import '../config/application_system_type.dart';
import 'app_routes.dart';
import 'page_transitions.dart';
import 'route_guard.dart';

abstract final class AppPages {
  static const initial = AppRoutes.splash;

  static final routes = <GetPage>[
    GetPage(
      name: AppRoutes.splash,
      page: () => const SplashScreen(),
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.onboarding,
      page: () => const OnboardingScreen(),
      binding: AuthBinding(),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.login,
      page: () => const LoginScreen(),
      binding: AuthBinding(),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.launchAccounting,
      page: () => const SystemLaunchScreen(
        systemType: ApplicationSystemType.accounting,
      ),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.launchCar,
      page: () => const SystemLaunchScreen(
        systemType: ApplicationSystemType.carContracts,
      ),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.launchCarTrade,
      page: () => const SystemLaunchScreen(
        systemType: ApplicationSystemType.carTrading,
      ),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.launchHotel,
      page: () => const SystemLaunchScreen(
        systemType: ApplicationSystemType.hotelManagement,
      ),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.home,
      page: () => const MainShellPage(),
      binding: AccountingShellBinding(),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.reports,
      page: () => const MainShellPage(initialTab: 1),
      binding: AccountingShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.data,
      page: () => const MainShellPage(initialTab: 2),
      binding: AccountingShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.settings,
      page: () => const MainShellPage(initialTab: 3),
      binding: AccountingShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.reportDetail,
      page: () => ReportDetailScreen(
        reportType: Get.parameters['type']!,
      ),
      binding: ReportDetailBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.dataList,
      page: () => DataListScreen(listType: Get.parameters['type']!),
      binding: DataListBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.invoiceNew,
      page: () => const InvoiceWizardScreen(),
      binding: InvoiceWizardBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.invoiceDetail,
      page: () => InvoiceDetailScreen(syncId: Get.parameters['syncId']!),
      binding: InvoiceDetailBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.customerNew,
      page: () => CustomerFormScreen(),
      binding: CustomerFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.customerEdit,
      page: () => CustomerFormScreen(syncId: Get.parameters['syncId']),
      binding: CustomerFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.customerDetail,
      page: () => EntityDetailScreen(
        entityType: 'customer',
        syncId: Get.parameters['syncId']!,
        name: Get.parameters['name'] ?? '',
      ),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.productNew,
      page: () => const ProductFormScreen(),
      binding: ProductFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.productEdit,
      page: () => ProductFormScreen(syncId: Get.parameters['syncId']),
      binding: ProductFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.productDetail,
      page: () => EntityDetailScreen(
        entityType: 'product',
        syncId: Get.parameters['syncId']!,
        name: Get.parameters['name'] ?? '',
      ),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.supplierNew,
      page: () => const SupplierFormScreen(),
      binding: SupplierFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.supplierEdit,
      page: () => SupplierFormScreen(syncId: Get.parameters['syncId']),
      binding: SupplierFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.supplierDetail,
      page: () => EntityDetailScreen(
        entityType: 'supplier',
        syncId: Get.parameters['syncId']!,
        name: Get.parameters['name'] ?? '',
      ),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.investorNew,
      page: () => const InvestorFormScreen(),
      binding: InvestorFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.investorEdit,
      page: () => InvestorFormScreen(syncId: Get.parameters['syncId']),
      binding: InvestorFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.investorDetail,
      page: () => EntityDetailScreen(
        entityType: 'investor',
        syncId: Get.parameters['syncId']!,
        name: Get.parameters['name'] ?? '',
      ),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.pricingTypes,
      page: () => const PricingTypesScreen(),
      binding: PricingTypesBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.pricingTypeNew,
      page: () => const PricingTypeFormScreen(),
      binding: PricingTypeFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.pricingTypeEdit,
      page: () => PricingTypeFormScreen(syncId: Get.parameters['syncId']),
      binding: PricingTypeFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.productPrices,
      page: () => const ProductPricesScreen(),
      binding: ProductPricesBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.productPriceNew,
      page: () => const ProductPriceFormScreen(),
      binding: ProductPriceFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.productPriceEdit,
      page: () => ProductPriceFormScreen(syncId: Get.parameters['syncId']),
      binding: ProductPriceFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.hotelHome,
      page: () => const HotelShellPage(),
      binding: HotelShellBinding(),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.hotelReservations,
      page: () => const HotelShellPage(initialTab: 1),
      binding: HotelShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.hotelRooms,
      page: () => const HotelShellPage(initialTab: 2),
      binding: HotelShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.hotelOperations,
      page: () => const HotelShellPage(initialTab: 3),
      binding: HotelShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.hotelGuests,
      page: () => const HotelShellPage(initialTab: 3),
      binding: HotelShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.hotelRestaurant,
      page: () => const RestaurantHubScreen(),
      binding: HotelShellBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.hotelSettings,
      page: () => const HotelShellPage(initialTab: 4),
      binding: HotelShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.hotelReservationNew,
      page: () => const HotelReservationFormScreen(),
      binding: HotelReservationFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.hotelReservationDetail,
      page: () => HotelReservationDetailScreen(
        syncId: Get.parameters['syncId']!,
        reservation: Get.arguments is HotelReservation
            ? Get.arguments as HotelReservation
            : null,
      ),
      binding: HotelReservationDetailBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.hotelGuestNew,
      page: () => const HotelGuestFormScreen(),
      binding: HotelGuestFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.hotelGuestEdit,
      page: () => HotelGuestFormScreen(
        guest: Get.arguments is HotelGuest ? Get.arguments as HotelGuest : null,
      ),
      binding: HotelGuestFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.carHome,
      page: () => const CarShellPage(),
      binding: CarShellBinding(),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.carContracts,
      page: () => const CarShellPage(initialTab: 1),
      binding: CarShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.carPayments,
      page: () => const CarShellPage(initialTab: 2),
      binding: CarShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.carReports,
      page: () => const CarShellPage(initialTab: 3),
      binding: CarShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.carSettings,
      page: () => const CarShellPage(initialTab: 4),
      binding: CarShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.carContractNew,
      page: () => const CarContractFormScreen(),
      binding: CarContractFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.carContractDetail,
      page: () => CarContractDetailScreen(
        syncId: Get.parameters['syncId']!,
      ),
      binding: CarContractDetailBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.carTradeHome,
      page: () => const CarTradeShellPage(),
      binding: CarTradeShellBinding(),
      middlewares: [AuthMiddleware()],
      transition: fadeSlideTransition,
      transitionDuration: defaultTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.carTradeTransactions,
      page: () => const CarTradeShellPage(initialTab: 1),
      binding: CarTradeShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.carTradePayments,
      page: () => const CarTradeShellPage(initialTab: 2),
      binding: CarTradeShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.carTradeReports,
      page: () => const CarTradeShellPage(initialTab: 3),
      binding: CarTradeShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.carTradeSettings,
      page: () => const CarTradeShellPage(initialTab: 4),
      binding: CarTradeShellBinding(),
      middlewares: [AuthMiddleware()],
    ),
    GetPage(
      name: AppRoutes.carTradeTransactionNew,
      page: () => const CarTradeTransactionFormScreen(),
      binding: CarTradeTransactionFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.carTradeTransactionDetail,
      page: () => CarTradeTransactionDetailScreen(
        syncId: Get.parameters['syncId']!,
      ),
      binding: CarTradeTransactionDetailBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.carTradePartyStatement,
      page: () => const CarTradePartyStatementScreen(),
      binding: CarTradePartyStatementBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.profile,
      page: () => const ProfileScreen(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.notifications,
      page: () => const NotificationsScreen(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.about,
      page: () => const AboutScreen(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.privacy,
      page: () => const PrivacyPolicyScreen(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.financeList,
      page: () => FinanceListScreen(listType: Get.parameters['type']!),
      binding: FinanceListBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.voucherNew,
      page: () => const VoucherFormScreen(),
      binding: VoucherFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.expenseNew,
      page: () => const ExpenseFormScreen(),
      binding: ExpenseFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.transferNew,
      page: () => const TransferFormScreen(),
      binding: TransferFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.cashBoxNew,
      page: () => const CashBoxFormScreen(),
      binding: CashBoxFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.bankAccountNew,
      page: () => const BankAccountFormScreen(),
      binding: BankAccountFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.expenseTypeNew,
      page: () => const ExpenseTypeFormScreen(),
      binding: ExpenseTypeFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.warehouseNew,
      page: () => const WarehouseFormScreen(),
      binding: WarehouseFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.warehouseTransferNew,
      page: () => const WarehouseTransferFormScreen(),
      binding: WarehouseTransferFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.stockAdjustment,
      page: () => const StockAdjustmentFormScreen(),
      binding: StockAdjustmentFormBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.warehouseStocks,
      page: () => const FinanceListScreen(listType: 'warehouse-stocks'),
      binding: BindingsBuilder(() {
        Get.lazyPut(
          () => FinanceListController(listType: 'warehouse-stocks'),
          tag: 'finance_list_warehouse-stocks',
          fenix: true,
        );
      }),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.installments,
      page: () => const InstallmentsScreen(),
      binding: InstallmentsBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.installmentPlanDetail,
      page: () => InstallmentPlanDetailScreen(syncId: Get.parameters['syncId']!),
      binding: InstallmentPlanDetailBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.installmentPay,
      page: () => const InstallmentPayScreen(),
      binding: InstallmentPayBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.pendingSync,
      page: () => const PendingSyncScreen(),
      binding: PendingSyncBinding(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
    GetPage(
      name: AppRoutes.quickActions,
      page: () => const QuickActionsScreen(),
      middlewares: [AuthMiddleware()],
      transition: slideTransition,
      transitionDuration: slideTransitionDuration,
    ),
  ];
}
