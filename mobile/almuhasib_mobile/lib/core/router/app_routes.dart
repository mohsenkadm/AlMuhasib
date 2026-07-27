abstract final class AppRoutes {
  static const splash = '/splash';
  static const onboarding = '/onboarding';
  static const login = '/login';
  static const launchAccounting = '/launch/accounting';
  static const launchCar = '/launch/car';
  static const launchCarTrade = '/launch/car-trade';
  static const launchHotel = '/launch/hotel';
  static const launchRealEstate = '/launch/real-estate';

  static const home = '/home';
  static const reports = '/reports';
  static const reportDetail = '/reports/detail/:type';
  static const data = '/data';
  static const dataList = '/data/list/:type';
  static const invoiceNew = '/data/invoice/new';
  static const invoiceDetail = '/data/invoice/:syncId';
  static const customerNew = '/data/customer/new';
  static const customerEdit = '/data/customer/:syncId/edit';
  static const customerDetail = '/data/customer/:syncId';
  static const productNew = '/data/product/new';
  static const productEdit = '/data/product/:syncId/edit';
  static const productDetail = '/data/product/:syncId';
  static const supplierNew = '/data/supplier/new';
  static const supplierEdit = '/data/supplier/:syncId/edit';
  static const supplierDetail = '/data/supplier/:syncId';
  static const investorNew = '/data/investor/new';
  static const investorEdit = '/data/investor/:syncId/edit';
  static const investorDetail = '/data/investor/:syncId';
  static const pricingTypes = '/data/pricing-types';
  static const pricingTypeNew = '/data/pricing-types/new';
  static const pricingTypeEdit = '/data/pricing-types/:syncId/edit';
  static const productPrices = '/data/product-prices';
  static const productPriceNew = '/data/product-prices/new';
  static const productPriceEdit = '/data/product-prices/:syncId/edit';

  static const voucherNew = '/data/voucher/new';
  static const expenseNew = '/data/expense/new';
  static const transferNew = '/data/transfer/new';
  static const cashBoxNew = '/data/cash-box/new';
  static const bankAccountNew = '/data/bank-account/new';
  static const expenseTypeNew = '/data/expense-type/new';
  static const warehouseNew = '/data/warehouse/new';
  static const warehouseTransferNew = '/data/warehouse-transfer/new';
  static const stockAdjustment = '/data/stock-adjustment';
  static const warehouseStocks = '/data/warehouse-stocks';
  static const financeList = '/data/finance/:type';
  static const installments = '/data/installments';
  static const installmentPlanDetail = '/data/installment-plans/:syncId';
  static const installmentPay = '/data/installments/:syncId/pay';
  static const pendingSync = '/settings/pending-sync';
  static const quickActions = '/data/quick-actions';

  static const settings = '/settings';
  static const notifications = '/notifications';

  static const hotelHome = '/hotel/home';
  static const hotelReservations = '/hotel/reservations';
  static const hotelReservationNew = '/hotel/reservations/new';
  static const hotelReservationDetail = '/hotel/reservations/:syncId';
  static const hotelRooms = '/hotel/rooms';
  static const hotelOperations = '/hotel/operations';
  static const hotelGuests = '/hotel/guests';
  static const hotelGuestNew = '/hotel/guests/new';
  static const hotelGuestEdit = '/hotel/guests/:syncId/edit';
  static const hotelRestaurant = '/hotel/restaurant';
  static const hotelSettings = '/hotel/settings';

  static const carHome = '/car/home';
  static const carContracts = '/car/contracts';
  static const carContractNew = '/car/contracts/new';
  static const carContractDetail = '/car/contracts/:syncId';
  static const carPayments = '/car/payments';
  static const carReports = '/car/reports';
  static const carSettings = '/car/settings';

  static const carTradeHome = '/car-trade/home';
  static const carTradeTransactions = '/car-trade/transactions';
  static const carTradeTransactionNew = '/car-trade/transactions/new';
  static const carTradeTransactionDetail = '/car-trade/transactions/:syncId';
  static const carTradePayments = '/car-trade/payments';
  static const carTradeReports = '/car-trade/reports';
  static const carTradePartyStatement = '/car-trade/party-statement';
  static const carTradeSettings = '/car-trade/settings';

  static const realEstateHome = '/real-estate/home';
  static const realEstateContracts = '/real-estate/contracts';
  static const realEstateContractNew = '/real-estate/contracts/new';
  static const realEstateContractDetail = '/real-estate/contracts/:syncId';
  static const realEstatePayments = '/real-estate/payments';
  static const realEstateReports = '/real-estate/reports';
  static const realEstateSettings = '/real-estate/settings';

  static const profile = '/profile';
  static const about = '/about';
  static const privacy = '/privacy';

  static String reportDetailPath(String type) => '/reports/detail/$type';
  static String dataListPath(String type) => '/data/list/$type';
  static String invoiceDetailPath(String syncId) => '/data/invoice/$syncId';
  static String customerEditPath(String syncId) => '/data/customer/$syncId/edit';
  static String customerDetailPath(String syncId, {String? name}) {
    final base = '/data/customer/$syncId';
    if (name == null || name.isEmpty) return base;
    return '$base?name=${Uri.encodeComponent(name)}';
  }

  static String productEditPath(String syncId) => '/data/product/$syncId/edit';
  static String productDetailPath(String syncId, {String? name}) {
    final base = '/data/product/$syncId';
    if (name == null || name.isEmpty) return base;
    return '$base?name=${Uri.encodeComponent(name)}';
  }

  static String supplierEditPath(String syncId) =>
      '/data/supplier/$syncId/edit';
  static String supplierDetailPath(String syncId, {String? name}) {
    final base = '/data/supplier/$syncId';
    if (name == null || name.isEmpty) return base;
    return '$base?name=${Uri.encodeComponent(name)}';
  }

  static String investorEditPath(String syncId) =>
      '/data/investor/$syncId/edit';
  static String investorDetailPath(String syncId, {String? name}) {
    final base = '/data/investor/$syncId';
    if (name == null || name.isEmpty) return base;
    return '$base?name=${Uri.encodeComponent(name)}';
  }

  static String pricingTypeEditPath(String syncId) =>
      '/data/pricing-types/$syncId/edit';
  static String productPriceEditPath(String syncId) =>
      '/data/product-prices/$syncId/edit';

  static String financeListPath(String type) => '/data/finance/$type';
  static String installmentPlanDetailPath(String syncId) =>
      '/data/installment-plans/$syncId';
  static String installmentPayPath(String syncId) =>
      '/data/installments/$syncId/pay';

  static String hotelReservationDetailPath(String syncId) =>
      '/hotel/reservations/$syncId';
  static String hotelGuestEditPath(String syncId) =>
      '/hotel/guests/$syncId/edit';
  static String carContractDetailPath(String syncId) =>
      '/car/contracts/$syncId';
  static String carTradeTransactionDetailPath(String syncId) =>
      '/car-trade/transactions/$syncId';
  static String realEstateContractDetailPath(String syncId) =>
      '/real-estate/contracts/$syncId';
}
