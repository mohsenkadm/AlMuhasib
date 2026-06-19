import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../core/constants/app_colors.dart';
import '../../core/getx/app_services.dart';
import '../../core/router/app_routes.dart';
import '../../core/services/app_info_service.dart';
import '../../shared/widgets/app_animations.dart';
import '../../shared/widgets/common_widgets.dart';

class ProfileScreen extends StatelessWidget {
  const ProfileScreen({super.key});

  Future<void> _confirmLogout(BuildContext context) async {
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (ctx) => AlertDialog(
        title: Text('logout_confirm_title'.tr()),
        content: Text('logout_confirm_message'.tr()),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(ctx, false),
            child: Text('cancel'.tr()),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(ctx, true),
            style: FilledButton.styleFrom(backgroundColor: AppColors.error),
            child: Text('logout'.tr()),
          ),
        ],
      ),
    );
    if (confirmed == true) {
      await AppServices.auth.logout();
    }
  }

  @override
  Widget build(BuildContext context) {
    final prefs = AppServices.prefs;
    final company = prefs.companyName ?? 'app_name'.tr();
    final username = prefs.username ?? '—';

    return Scaffold(
      appBar: AppBar(title: Text('profile_title'.tr())),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
        children: [
          GradientCard(
            padding: const EdgeInsets.all(20),
            gradient: AppColors.primaryGradient,
            child: Row(
              children: [
                Container(
                  width: 72,
                  height: 72,
                  decoration: BoxDecoration(
                    color: Colors.white.withValues(alpha: 0.2),
                    borderRadius: BorderRadius.circular(20),
                    border: Border.all(color: Colors.white24),
                  ),
                  alignment: Alignment.center,
                  child: Text(
                    company.isNotEmpty ? company[0] : 'م',
                    style: const TextStyle(
                      color: Colors.white,
                      fontSize: 32,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                ).scaleIn(),
                const SizedBox(width: 16),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        company,
                        style: Theme.of(context).textTheme.titleLarge?.copyWith(
                              color: Colors.white,
                              fontWeight: FontWeight.w800,
                            ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        username,
                        style: Theme.of(context).textTheme.bodyMedium?.copyWith(
                              color: Colors.white.withValues(alpha: 0.85),
                            ),
                      ),
                      if (prefs.tenantId != null) ...[
                        const SizedBox(height: 4),
                        Text(
                          '${'tenant_id'.tr()}: ${prefs.tenantId}',
                          style: Theme.of(context).textTheme.bodySmall?.copyWith(
                                color: Colors.white70,
                              ),
                        ),
                      ],
                    ],
                  ),
                ),
              ],
            ),
          ).fadeSlideIn(),
          const SizedBox(height: 24),
          Text(
            'profile_menu'.tr(),
            style: Theme.of(context).textTheme.titleMedium,
          ).fadeSlideIn(delayMs: 80),
          const SizedBox(height: 12),
          _ProfileMenuTile(
            index: 0,
            icon: Icons.settings_outlined,
            title: 'settings_title'.tr(),
            subtitle: 'profile_settings_desc'.tr(),
            onTap: () => Get.toNamed(AppRoutes.settings),
          ),
          _ProfileMenuTile(
            index: 1,
            icon: Icons.info_outline,
            title: 'about_title'.tr(),
            subtitle: 'about_subtitle'.tr(),
            onTap: () => Get.toNamed(AppRoutes.about),
          ),
          _ProfileMenuTile(
            index: 2,
            icon: Icons.privacy_tip_outlined,
            title: 'privacy_title'.tr(),
            subtitle: 'privacy_subtitle'.tr(),
            onTap: () => Get.toNamed(AppRoutes.privacy),
          ),
          const SizedBox(height: 24),
          OutlinedButton.icon(
            onPressed: () => _confirmLogout(context),
            icon: const Icon(Icons.logout, color: AppColors.error),
            label: Text(
              'logout'.tr(),
              style: const TextStyle(color: AppColors.error),
            ),
            style: OutlinedButton.styleFrom(
              padding: const EdgeInsets.symmetric(vertical: 14),
              side: BorderSide(color: AppColors.error.withValues(alpha: 0.5)),
            ),
          ).fadeSlideIn(delayMs: 280),
          const SizedBox(height: 24),
          Center(
            child: FutureBuilder<AppInfo>(
              future: AppServices.appInfo.load(),
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const SizedBox(
                    width: 16,
                    height: 16,
                    child: CircularProgressIndicator(strokeWidth: 2),
                  );
                }
                if (snapshot.hasError || !snapshot.hasData) {
                  return Text('version'.tr());
                }
                return Text(
                  '${'version'.tr()} ${snapshot.data!.versionLabel}',
                  style: Theme.of(context).textTheme.bodySmall,
                );
              },
            ),
          ).fadeSlideIn(delayMs: 340),
        ],
      ),
    );
  }
}

class _ProfileMenuTile extends StatelessWidget {
  const _ProfileMenuTile({
    required this.index,
    required this.icon,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });

  final int index;
  final IconData icon;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Card(
        child: ListTile(
          leading: Container(
            padding: const EdgeInsets.all(8),
            decoration: BoxDecoration(
              color: AppColors.accent.withValues(alpha: 0.12),
              borderRadius: BorderRadius.circular(10),
            ),
            child: Icon(icon, color: AppColors.accent),
          ),
          title: Text(title),
          subtitle: Text(subtitle),
          trailing: const Icon(Icons.chevron_left),
          onTap: onTap,
        ),
      ).fadeSlideInList(index: index + 1),
    );
  }
}
