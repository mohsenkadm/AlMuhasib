import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../../core/constants/app_colors.dart';
import '../../core/getx/app_services.dart';
import '../../core/services/app_info_service.dart';
import '../../shared/widgets/app_animations.dart';
import '../../shared/widgets/common_widgets.dart';

class AboutScreen extends StatelessWidget {
  const AboutScreen({super.key});

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppBar(title: Text('about_title'.tr())),
      body: ListView(
        padding: const EdgeInsets.all(24),
        children: [
          Center(child: const AppLogoMark(size: 96).scaleIn()),
          const SizedBox(height: 20),
          Center(
            child: Text(
              'app_name'.tr(),
              style: Theme.of(context).textTheme.displaySmall,
            ),
          ).fadeSlideIn(delayMs: 100),
          const SizedBox(height: 8),
          Center(
            child: FutureBuilder<AppInfo>(
              future: AppServices.appInfo.load(),
              builder: (context, snapshot) {
                if (snapshot.connectionState == ConnectionState.waiting) {
                  return const CircularProgressIndicator(strokeWidth: 2);
                }
                if (snapshot.hasError || !snapshot.hasData) {
                  return Text('version'.tr());
                }
                final info = snapshot.data!;
                return Container(
                  padding: const EdgeInsets.symmetric(horizontal: 14, vertical: 6),
                  decoration: BoxDecoration(
                    color: AppColors.accent.withValues(alpha: 0.12),
                    borderRadius: BorderRadius.circular(20),
                  ),
                  child: Text(
                    '${'version'.tr()} ${info.version} (${info.buildNumber})',
                    style: Theme.of(context).textTheme.labelLarge?.copyWith(
                          color: AppColors.accent,
                        ),
                  ),
                );
              },
            ),
          ).fadeSlideIn(delayMs: 160),
          const SizedBox(height: 32),
          GradientCard(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  'about_description_title'.tr(),
                  style: Theme.of(context).textTheme.titleMedium,
                ),
                const SizedBox(height: 12),
                Text(
                  'about_description'.tr(),
                  style: Theme.of(context).textTheme.bodyLarge,
                ),
              ],
            ),
          ).fadeSlideIn(delayMs: 220),
          const SizedBox(height: 16),
          _AboutFeatureRow(
            index: 0,
            icon: Icons.analytics_outlined,
            text: 'about_feature_1'.tr(),
          ),
          _AboutFeatureRow(
            index: 1,
            icon: Icons.cloud_sync_outlined,
            text: 'about_feature_2'.tr(),
          ),
          _AboutFeatureRow(
            index: 2,
            icon: Icons.notifications_active_outlined,
            text: 'about_feature_3'.tr(),
          ),
          const SizedBox(height: 24),
          Center(
            child: Text(
              'about_copyright'.tr(),
              textAlign: TextAlign.center,
              style: Theme.of(context).textTheme.bodySmall,
            ),
          ).fadeSlideIn(delayMs: 400),
        ],
      ),
    );
  }
}

class _AboutFeatureRow extends StatelessWidget {
  const _AboutFeatureRow({
    required this.index,
    required this.icon,
    required this.text,
  });

  final int index;
  final IconData icon;
  final String text;

  @override
  Widget build(BuildContext context) {
    return Padding(
      padding: const EdgeInsets.only(bottom: 10),
      child: Card(
        child: ListTile(
          leading: Icon(icon, color: AppColors.primaryLight),
          title: Text(text),
        ),
      ).fadeSlideInList(index: index + 3),
    );
  }
}
