/// Mirrors backend ApplicationSystemType on Tenant.
enum ApplicationSystemType {
  accounting(0),
  carContracts(1),
  hotelManagement(2),
  carTrading(3);

  const ApplicationSystemType(this.value);

  final int value;

  static ApplicationSystemType fromInt(int? raw) {
    return ApplicationSystemType.values.firstWhere(
      (e) => e.value == raw,
      orElse: () => ApplicationSystemType.accounting,
    );
  }
}
