import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/getx/app_services.dart';
import '../../../core/router/app_routes.dart';
import '../../../core/services/app_info_service.dart';
import '../../../core/theme/system_themes.dart';
import '../../../shared/widgets/app_animations.dart';
import '../../settings/settings_controller.dart';
import '../widgets/gold_kpi_card.dart';

/// "More" hub: prices, notifications, theme, profile, and shared settings.
class GoldSettingsScreen extends GetView<SettingsController> {
  const GoldSettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final prefs = AppServices.prefs;
    final topPadding = MediaQuery.paddingOf(context).top;

    return Scaffold(
      body: ListView(
        padding: EdgeInsets.fromLTRB(20, topPadding + 12, 20, 120),
        children: [
          GoldSectionHeader(title: 'gold_nav_more'.tr()).fadeSlideIn(),
          const SizedBox(height: 16),
          _ModuleTile(
            icon: Icons.monetization_on_outlined,
            title: 'gold_prices_title'.tr(),
            subtitle: 'gold_mithqal_prices'.tr(),
            onTap: () => Get.toNamed(AppRoutes.goldShopPrices),
          ).fadeSlideIn(delayMs: 40),
          _ModuleTile(
            icon: Icons.notifications_outlined,
            title: 'gold_notifications_title'.tr(),
            subtitle: 'gold_alerts'.tr(),
            onTap: () => Get.toNamed(AppRoutes.goldShopNotifications),
          ).fadeSlideIn(delayMs: 80),
          _ModuleTile(
            icon: Icons.inventory_2_outlined,
            title: 'مخزون الذهب',
            subtitle: 'أرصدة العيارات',
            onTap: () => Get.toNamed(AppRoutes.goldShopStock),
          ).fadeSlideIn(delayMs: 100),
          _ModuleTile(
            icon: Icons.local_shipping_outlined,
            title: 'الموردون',
            subtitle: 'قائمة الموردين والذمم',
            onTap: () => Get.toNamed(AppRoutes.goldShopSuppliers),
          ).fadeSlideIn(delayMs: 120),
          _ModuleTile(
            icon: Icons.assessment_outlined,
            title: 'gold_reports_hub'.tr(),
            subtitle: 'gold_reports_hub_desc'.tr(),
            onTap: () => Get.toNamed(AppRoutes.goldShopReports),
          ).fadeSlideIn(delayMs: 140),
          _ModuleTile(
            icon: Icons.receipt_long_outlined,
            title: 'gold_vouchers_title'.tr(),
            subtitle: 'gold_vouchers_desc'.tr(),
            onTap: () => Get.toNamed(AppRoutes.goldShopVouchers),
          ).fadeSlideIn(delayMs: 160),
          _ModuleTile(
            icon: Icons.payments_outlined,
            title: 'gold_collection_title'.tr(),
            subtitle: 'gold_collection_desc'.tr(),
            onTap: () => Get.toNamed(AppRoutes.goldShopCollection),
          ).fadeSlideIn(delayMs: 180),
          _ModuleTile(
            icon: Icons.shopping_cart_outlined,
            title: 'gold_new_purchase'.tr(),
            subtitle: 'gold_ops_purchase_desc'.tr(),
            onTap: () => Get.toNamed(AppRoutes.goldShopPurchaseNew),
          ).fadeSlideIn(delayMs: 200),
          _ModuleTile(
            icon: Icons.swap_horiz,
            title: 'gold_new_exchange'.tr(),
            subtitle: 'gold_ops_exchange_desc'.tr(),
            onTap: () => Get.toNamed(AppRoutes.goldShopExchangeNew),
          ).fadeSlideIn(delayMs: 220),
          _ModuleTile(
            icon: Icons.undo,
            title: 'gold_new_return'.tr(),
            subtitle: 'gold_ops_return_desc'.tr(),
            onTap: () => Get.toNamed(AppRoutes.goldShopReturnNew),
          ).fadeSlideIn(delayMs: 240),
          const SizedBox(height: 8),
          const Divider(height: 32),
          Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: ListTile(
              leading: CircleAvatar(
                backgroundColor:
                    SystemThemes.goldPrimary.withValues(alpha: 0.14),
                child: const Icon(
                  Icons.person_outline,
                  color: SystemThemes.goldPrimary,
                ),
              ),
              title: Text('profile_title'.tr()),
              subtitle: Text('profile_settings_desc'.tr()),
              trailing: const Icon(Icons.chevron_left),
              onTap: () => Get.toNamed(AppRoutes.profile),
            ),
          ).fadeSlideIn(delayMs: 100),
          ListTile(
            leading: const Icon(Icons.business_outlined),
            title: Text('company_name'.tr()),
            subtitle: Text(prefs.companyName ?? '—'),
          ).fadeSlideIn(delayMs: 120),
          Obx(() {
            final themeMode = AppServices.theme.themeMode.value;
            return SwitchListTile(
              title: Text('theme_mode'.tr()),
              subtitle: Text(
                themeMode == ThemeMode.dark
                    ? 'theme_dark'.tr()
                    : 'theme_light'.tr(),
              ),
              secondary: Icon(
                themeMode == ThemeMode.dark
                    ? Icons.dark_mode
                    : Icons.light_mode,
              ),
              value: themeMode == ThemeMode.dark,
              onChanged: (_) => AppServices.theme.toggle(),
            );
          }).fadeSlideIn(delayMs: 140),
          ListTile(
            leading: const Icon(Icons.language),
            title: Text('language'.tr()),
            subtitle: Text(
              context.locale.languageCode == 'ar' ? 'العربية' : 'English',
            ),
            trailing: const Icon(Icons.chevron_left),
            onTap: () async {
              final isArabic = context.locale.languageCode == 'ar';
              await context.setLocale(
                isArabic ? const Locale('en') : const Locale('ar'),
              );
            },
          ).fadeSlideIn(delayMs: 160),
          ListTile(
            leading: const Icon(Icons.info_outline),
            title: Text('about_title'.tr()),
            trailing: const Icon(Icons.chevron_left),
            onTap: () => Get.toNamed(AppRoutes.about),
          ).fadeSlideIn(delayMs: 180),
          FutureBuilder<AppInfo>(
            future: AppServices.appInfo.load(),
            builder: (context, snapshot) {
              final info = snapshot.data;
              return ListTile(
                leading: const Icon(Icons.verified_outlined),
                title: Text('version'.tr()),
                subtitle: Text(info?.versionLabel ?? '—'),
              );
            },
          ).fadeSlideIn(delayMs: 200),
          const SizedBox(height: 12),
          FilledButton.tonalIcon(
            onPressed: () => AppServices.auth.logout(),
            icon: const Icon(Icons.logout),
            label: Text('logout'.tr()),
            style: FilledButton.styleFrom(
              foregroundColor: Theme.of(context).colorScheme.error,
              padding: const EdgeInsets.symmetric(vertical: 14),
            ),
          ).fadeSlideIn(delayMs: 220),
        ],
      ),
    );
  }
}

class _ModuleTile extends StatelessWidget {
  const _ModuleTile({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Material(
        color: Colors.transparent,
        child: InkWell(
          onTap: onTap,
          borderRadius: BorderRadius.circular(18),
          child: Ink(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              borderRadius: BorderRadius.circular(18),
              gradient: LinearGradient(
                begin: Alignment.topRight,
                end: Alignment.bottomLeft,
                colors: [
                  SystemThemes.goldPrimary.withValues(alpha: 0.12),
                  SystemThemes.goldSecondary.withValues(alpha: 0.08),
                ],
              ),
              border: Border.all(
                color: SystemThemes.goldPrimary.withValues(alpha: 0.2),
              ),
            ),
            child: Row(
              children: [
                Container(
                  width: 48,
                  height: 48,
                  decoration: BoxDecoration(
                    color: SystemThemes.goldPrimary.withValues(alpha: 0.16),
                    borderRadius: BorderRadius.circular(14),
                  ),
                  child: Icon(icon, color: SystemThemes.goldPrimary),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: const TextStyle(fontWeight: FontWeight.w800),
                      ),
                      Text(
                        subtitle,
                        style: Theme.of(context).textTheme.bodySmall,
                      ),
                    ],
                  ),
                ),
                const Icon(Icons.chevron_left),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
