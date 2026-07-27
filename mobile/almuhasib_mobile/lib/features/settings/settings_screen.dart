import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../core/constants/app_colors.dart';
import '../../core/getx/app_services.dart';
import '../../core/router/app_routes.dart';
import '../../core/services/app_info_service.dart';
import '../../core/theme/app_spacing.dart';
import '../../shared/widgets/app_animations.dart';
import 'settings_controller.dart';

class SettingsScreen extends GetView<SettingsController> {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final prefs = AppServices.prefs;

    return Scaffold(
      appBar: AppBar(title: Text('settings_title'.tr())),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
        children: [
          Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: ListTile(
              leading: CircleAvatar(
                backgroundColor:
                    Theme.of(context).colorScheme.primary.withValues(alpha: 0.12),
                child: Icon(
                  Icons.person_outline,
                  color: Theme.of(context).colorScheme.primary,
                ),
              ),
              title: Text('profile_title'.tr()),
              subtitle: Text('profile_settings_desc'.tr()),
              trailing: const Icon(Icons.chevron_left),
              onTap: () => Get.toNamed(AppRoutes.profile),
            ),
          ).fadeSlideIn(),
          _SettingsTile(
            index: 0,
            icon: Icons.business_outlined,
            title: 'company_name'.tr(),
            subtitle: prefs.companyName ?? '—',
          ),
          Obx(
            () => _SettingsTile(
              index: 1,
              icon: Icons.verified_outlined,
              title: 'license_status'.tr(),
              subtitle: controller.licenseText.value ?? 'loading'.tr(),
            ),
          ),
          const Divider(height: 32),
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
          }).fadeSlideIn(delayMs: 120),
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
          Obx(() {
            final count = AppServices.offlineQueue.pending.length;
            return ListTile(
              leading: Badge(
                isLabelVisible: count > 0,
                label: Text('$count'),
                child: const Icon(Icons.cloud_sync_outlined),
              ),
              title: Text('pending_sync'.tr()),
              subtitle: Text(
                count == 0
                    ? 'offline_queue_empty'.tr()
                    : 'pending_sync_tap'.tr(args: ['$count']),
              ),
              trailing: const Icon(Icons.chevron_left),
              onTap: () => Get.toNamed(AppRoutes.pendingSync),
            );
          }).fadeSlideIn(delayMs: 170),
          const SizedBox(height: 28),
          Text(
            'account_section'.tr(),
            style: Theme.of(context).textTheme.titleMedium?.copyWith(
                  fontWeight: FontWeight.w800,
                ),
          ).fadeSlideIn(delayMs: 180),
          const SizedBox(height: 12),
          _AccountActionCard(
            icon: Icons.logout_rounded,
            title: 'logout'.tr(),
            subtitle: 'logout_card_hint'.tr(),
            accent: AppColors.moduleOrange,
            onTap: () => _confirmLogout(context),
          ).fadeSlideIn(delayMs: 200),
          const SizedBox(height: 12),
          _AccountActionCard(
            icon: Icons.delete_forever_rounded,
            title: 'delete_account'.tr(),
            subtitle: 'delete_account_warning'.tr(),
            accent: AppColors.error,
            destructive: true,
            onTap: () => _confirmDeleteAccount(context),
          ).fadeSlideIn(delayMs: 220),
          const SizedBox(height: 32),
          Center(
            child: FutureBuilder<AppInfo>(
              future: AppServices.appInfo.load(),
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const SizedBox.shrink();
                }
                if (snapshot.hasError || !snapshot.hasData) {
                  return const SizedBox.shrink();
                }
                return Text(
                  '${'version'.tr()} ${snapshot.data!.versionLabel}',
                  style: Theme.of(context).textTheme.bodySmall,
                );
              },
            ),
          ).fadeSlideIn(delayMs: 240),
        ],
      ),
    );
  }

  Future<void> _confirmLogout(BuildContext context) async {
    final ok = await _showConfirmSheet(
      context: context,
      icon: Icons.logout_rounded,
      accent: AppColors.moduleOrange,
      title: 'logout_confirm_title'.tr(),
      message: 'logout_confirm_message'.tr(),
      confirmLabel: 'logout'.tr(),
    );
    if (ok == true) {
      await AppServices.auth.logout();
    }
  }

  Future<void> _confirmDeleteAccount(BuildContext context) async {
    final ok = await _showConfirmSheet(
      context: context,
      icon: Icons.delete_forever_rounded,
      accent: AppColors.error,
      title: 'delete_account'.tr(),
      message: 'delete_account_confirm'.tr(),
      confirmLabel: 'delete_account'.tr(),
      destructive: true,
    );
    if (ok == true) {
      await AppServices.auth.logout();
      Get.snackbar(
        'delete_account'.tr(),
        'delete_account_done_local'.tr(),
        snackPosition: SnackPosition.BOTTOM,
        margin: const EdgeInsets.all(16),
        borderRadius: 14,
      );
    }
  }

  Future<bool?> _showConfirmSheet({
    required BuildContext context,
    required IconData icon,
    required Color accent,
    required String title,
    required String message,
    required String confirmLabel,
    bool destructive = false,
  }) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return showModalBottomSheet<bool>(
      context: context,
      isScrollControlled: true,
      backgroundColor: Colors.transparent,
      builder: (ctx) {
        return Padding(
          padding: const EdgeInsets.fromLTRB(16, 0, 16, 16),
          child: DecoratedBox(
            decoration: BoxDecoration(
              color: isDark ? AppColors.surfaceDarkCard : Colors.white,
              borderRadius: BorderRadius.circular(24),
              boxShadow: AppColors.cardShadow(dark: isDark),
            ),
            child: SafeArea(
              top: false,
              child: Padding(
                padding: const EdgeInsets.fromLTRB(20, 12, 20, 20),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  children: [
                    Container(
                      width: 42,
                      height: 4,
                      decoration: BoxDecoration(
                        color: Colors.grey.withValues(alpha: 0.35),
                        borderRadius: BorderRadius.circular(99),
                      ),
                    ),
                    const SizedBox(height: 20),
                    Container(
                      width: 72,
                      height: 72,
                      decoration: BoxDecoration(
                        color: accent.withValues(alpha: 0.12),
                        shape: BoxShape.circle,
                      ),
                      child: Icon(icon, size: 34, color: accent),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      title,
                      textAlign: TextAlign.center,
                      style: Theme.of(ctx).textTheme.titleLarge?.copyWith(
                            fontWeight: FontWeight.w900,
                          ),
                    ),
                    const SizedBox(height: 10),
                    Text(
                      message,
                      textAlign: TextAlign.center,
                      style: Theme.of(ctx).textTheme.bodyMedium?.copyWith(
                            color: isDark
                                ? AppColors.textMuted
                                : AppColors.textDarkMuted,
                            height: 1.45,
                          ),
                    ),
                    const SizedBox(height: 24),
                    Row(
                      children: [
                        Expanded(
                          child: OutlinedButton(
                            onPressed: () => Navigator.pop(ctx, false),
                            style: OutlinedButton.styleFrom(
                              minimumSize: const Size.fromHeight(50),
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(14),
                              ),
                            ),
                            child: Text('cancel'.tr()),
                          ),
                        ),
                        const SizedBox(width: 12),
                        Expanded(
                          child: FilledButton(
                            onPressed: () => Navigator.pop(ctx, true),
                            style: FilledButton.styleFrom(
                              minimumSize: const Size.fromHeight(50),
                              backgroundColor:
                                  destructive ? AppColors.error : accent,
                              foregroundColor: Colors.white,
                              shape: RoundedRectangleBorder(
                                borderRadius: BorderRadius.circular(14),
                              ),
                            ),
                            child: Text(confirmLabel),
                          ),
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
          ),
        );
      },
    );
  }
}

class _AccountActionCard extends StatelessWidget {
  const _AccountActionCard({
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.accent,
    required this.onTap,
    this.destructive = false,
  });

  final IconData icon;
  final String title;
  final String subtitle;
  final Color accent;
  final VoidCallback onTap;
  final bool destructive;

  @override
  Widget build(BuildContext context) {
    final isDark = Theme.of(context).brightness == Brightness.dark;

    return Material(
      color: Colors.transparent,
      child: InkWell(
        onTap: onTap,
        borderRadius: BorderRadius.circular(AppSpacing.radiusLg),
        child: Ink(
          decoration: BoxDecoration(
            color: isDark ? AppColors.surfaceDarkCard : Colors.white,
            borderRadius: BorderRadius.circular(AppSpacing.radiusLg),
            border: Border.all(
              color: accent.withValues(alpha: destructive ? 0.28 : 0.18),
            ),
            boxShadow: AppColors.cardShadow(dark: isDark),
          ),
          child: Padding(
            padding: const EdgeInsets.all(16),
            child: Row(
              children: [
                Container(
                  width: 52,
                  height: 52,
                  decoration: BoxDecoration(
                    color: accent.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(16),
                  ),
                  child: Icon(icon, color: accent, size: 26),
                ),
                const SizedBox(width: 14),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        title,
                        style: Theme.of(context).textTheme.titleMedium?.copyWith(
                              fontWeight: FontWeight.w800,
                              color: accent,
                            ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        subtitle,
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                              color: isDark
                                  ? AppColors.textMuted
                                  : AppColors.textDarkMuted,
                              height: 1.35,
                            ),
                      ),
                    ],
                  ),
                ),
                Icon(
                  Icons.chevron_left_rounded,
                  color: accent.withValues(alpha: 0.7),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}

class _SettingsTile extends StatelessWidget {
  const _SettingsTile({
    required this.index,
    required this.icon,
    required this.title,
    required this.subtitle,
  });

  final int index;
  final IconData icon;
  final String title;
  final String subtitle;

  @override
  Widget build(BuildContext context) {
    return Card(
      margin: const EdgeInsets.only(bottom: 10),
      child: ListTile(
        leading: Icon(icon),
        title: Text(title),
        subtitle: Text(subtitle),
      ),
    ).fadeSlideInList(index: index);
  }
}
