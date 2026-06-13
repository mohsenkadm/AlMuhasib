import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:go_router/go_router.dart';

import '../../core/config/env_config.dart';
import '../../core/providers/core_providers.dart';
import '../../core/services/app_info_service.dart';
import '../../core/theme/theme_provider.dart';
import '../../shared/widgets/app_animations.dart';

class SettingsScreen extends ConsumerStatefulWidget {
  const SettingsScreen({super.key});

  @override
  ConsumerState<SettingsScreen> createState() => _SettingsScreenState();
}

class _SettingsScreenState extends ConsumerState<SettingsScreen> {
  late final TextEditingController _apiUrlController;
  String? _licenseText;

  @override
  void initState() {
    super.initState();
    _apiUrlController = TextEditingController(
      text: ref.read(preferencesServiceProvider).apiBaseUrl,
    );
    _loadLicense();
  }

  Future<void> _loadLicense() async {
    try {
      final status = await ref.read(authRepositoryProvider).getLicenseStatus();
      setState(() {
        _licenseText = status.isActive && status.isMobileEnabled
            ? 'license_active'.tr()
            : status.message ?? status.statusCode ?? 'license_inactive'.tr();
      });
    } catch (_) {
      setState(() => _licenseText = '—');
    }
  }

  @override
  void dispose() {
    _apiUrlController.dispose();
    super.dispose();
  }

  Future<void> _saveApiUrl() async {
    await ref
        .read(preferencesServiceProvider)
        .setApiBaseUrl(_apiUrlController.text.trim());
    ref.read(apiClientProvider).updateBaseUrl();
    if (mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(content: Text('settings_saved'.tr())),
      );
    }
  }

  @override
  Widget build(BuildContext context) {
    final prefs = ref.watch(preferencesServiceProvider);
    final themeMode = ref.watch(themeModeProvider);
    final appInfo = ref.watch(appInfoProvider);

    return Scaffold(
      appBar: AppBar(title: Text('settings_title'.tr())),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 12, 20, 120),
        children: [
          Card(
            margin: const EdgeInsets.only(bottom: 12),
            child: ListTile(
              leading: CircleAvatar(
                backgroundColor: Theme.of(context).colorScheme.primary.withValues(alpha: 0.12),
                child: Icon(Icons.person_outline, color: Theme.of(context).colorScheme.primary),
              ),
              title: Text('profile_title'.tr()),
              subtitle: Text('profile_settings_desc'.tr()),
              trailing: const Icon(Icons.chevron_left),
              onTap: () => context.push('/profile'),
            ),
          ).fadeSlideIn(),
          _SettingsTile(
            index: 0,
            icon: Icons.business_outlined,
            title: 'company_name'.tr(),
            subtitle: prefs.companyName ?? '—',
          ),
          _SettingsTile(
            index: 1,
            icon: Icons.verified_outlined,
            title: 'license_status'.tr(),
            subtitle: _licenseText ?? 'loading'.tr(),
          ),
          const SizedBox(height: 8),
          TextField(
            controller: _apiUrlController,
            decoration: InputDecoration(
              labelText: 'api_url'.tr(),
              prefixIcon: const Icon(Icons.link),
            ),
          ).fadeSlideIn(delayMs: 120),
          const SizedBox(height: 12),
          FilledButton.icon(
            onPressed: _saveApiUrl,
            icon: const Icon(Icons.save_outlined),
            label: Text('save'.tr()),
          ).fadeSlideIn(delayMs: 160),
          const Divider(height: 32),
          SwitchListTile(
            title: Text('theme_mode'.tr()),
            subtitle: Text(
              themeMode == ThemeMode.dark ? 'theme_dark'.tr() : 'theme_light'.tr(),
            ),
            secondary: Icon(
              themeMode == ThemeMode.dark ? Icons.dark_mode : Icons.light_mode,
            ),
            value: themeMode == ThemeMode.dark,
            onChanged: (_) => ref.read(themeModeProvider.notifier).toggle(),
          ).fadeSlideIn(delayMs: 200),
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
          ).fadeSlideIn(delayMs: 240),
          if (!EnvConfig.isOneSignalConfigured)
            ListTile(
              leading: const Icon(Icons.notifications_off_outlined),
              title: const Text('OneSignal'),
              subtitle: Text('onesignal_not_configured'.tr()),
            ).fadeSlideIn(delayMs: 280),
          const SizedBox(height: 32),
          Center(
            child: appInfo.when(
              data: (info) => Text(
                '${'version'.tr()} ${info.versionLabel}',
                style: Theme.of(context).textTheme.bodySmall,
              ),
              loading: () => const SizedBox.shrink(),
              error: (_, __) => const SizedBox.shrink(),
            ),
          ).fadeSlideIn(delayMs: 320),
        ],
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
