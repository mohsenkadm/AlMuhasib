import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/constants/app_colors.dart';
import '../../core/providers/core_providers.dart';
import '../../core/services/app_info_service.dart';
import '../../core/theme/theme_provider.dart';
import '../../shared/widgets/app_animations.dart';
import '../../shared/widgets/common_widgets.dart';

class ProfileScreen extends ConsumerWidget {
  const ProfileScreen({super.key});

  Future<void> _confirmLogout(BuildContext context, WidgetRef ref) async {
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
      await ref.read(authStateProvider.notifier).logout();
      if (context.mounted) context.go('/login');
    }
  }

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    final prefs = ref.watch(preferencesServiceProvider);
    final appInfo = ref.watch(appInfoProvider);
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
            onTap: () => context.push('/settings'),
          ),
          _ProfileMenuTile(
            index: 1,
            icon: Icons.info_outline,
            title: 'about_title'.tr(),
            subtitle: 'about_subtitle'.tr(),
            onTap: () => context.push('/about'),
          ),
          _ProfileMenuTile(
            index: 2,
            icon: Icons.privacy_tip_outlined,
            title: 'privacy_title'.tr(),
            subtitle: 'privacy_subtitle'.tr(),
            onTap: () => context.push('/privacy'),
          ),
          const SizedBox(height: 24),
          OutlinedButton.icon(
            onPressed: () => _confirmLogout(context, ref),
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
            child: appInfo.when(
              data: (info) => Text(
                '${'version'.tr()} ${info.versionLabel}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
              loading: () => const SizedBox(
                width: 16,
                height: 16,
                child: CircularProgressIndicator(strokeWidth: 2),
              ),
              error: (_, __) => Text('version'.tr()),
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
