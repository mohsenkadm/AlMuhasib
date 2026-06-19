import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../core/config/env_config.dart';
import '../../core/getx/app_services.dart';
import '../../core/router/app_routes.dart';
import '../../core/services/app_info_service.dart';
import '../../shared/widgets/app_animations.dart';
import 'settings_controller.dart';

class SettingsScreen extends StatelessWidget {
  const SettingsScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final controller = Get.put(SettingsController(), tag: 'settings');
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
                backgroundColor: Theme.of(context).colorScheme.primary.withValues(alpha: 0.12),
                child: Icon(Icons.person_outline, color: Theme.of(context).colorScheme.primary),
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
          const SizedBox(height: 8),
          TextField(
            controller: controller.apiUrlController,
            decoration: InputDecoration(
              labelText: 'api_url'.tr(),
              prefixIcon: const Icon(Icons.link),
            ),
          ).fadeSlideIn(delayMs: 120),
          const SizedBox(height: 12),
          FilledButton.icon(
            onPressed: controller.saveApiUrl,
            icon: const Icon(Icons.save_outlined),
            label: Text('save'.tr()),
          ).fadeSlideIn(delayMs: 160),
          const Divider(height: 32),
          Obx(() {
            final themeMode = AppServices.theme.themeMode.value;
            return SwitchListTile(
              title: Text('theme_mode'.tr()),
              subtitle: Text(
                themeMode == ThemeMode.dark ? 'theme_dark'.tr() : 'theme_light'.tr(),
              ),
              secondary: Icon(
                themeMode == ThemeMode.dark ? Icons.dark_mode : Icons.light_mode,
              ),
              value: themeMode == ThemeMode.dark,
              onChanged: (_) => AppServices.theme.toggle(),
            );
          }).fadeSlideIn(delayMs: 200),
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
