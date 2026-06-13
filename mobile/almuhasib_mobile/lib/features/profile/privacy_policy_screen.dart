import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';

import '../../core/constants/app_colors.dart';
import '../../shared/widgets/app_animations.dart';

class PrivacyPolicyScreen extends StatelessWidget {
  const PrivacyPolicyScreen({super.key});

  @override
  Widget build(BuildContext context) {
    final sections = [
      _Section('privacy_section_1_title'.tr(), 'privacy_section_1_body'.tr()),
      _Section('privacy_section_2_title'.tr(), 'privacy_section_2_body'.tr()),
      _Section('privacy_section_3_title'.tr(), 'privacy_section_3_body'.tr()),
      _Section('privacy_section_4_title'.tr(), 'privacy_section_4_body'.tr()),
      _Section('privacy_section_5_title'.tr(), 'privacy_section_5_body'.tr()),
    ];

    return Scaffold(
      appBar: AppBar(title: Text('privacy_title'.tr())),
      body: ListView(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 32),
        children: [
          Container(
            padding: const EdgeInsets.all(16),
            decoration: BoxDecoration(
              gradient: LinearGradient(
                colors: [
                  AppColors.primary.withValues(alpha: 0.15),
                  AppColors.accent.withValues(alpha: 0.08),
                ],
              ),
              borderRadius: BorderRadius.circular(AppColors.cardRadius),
            ),
            child: Row(
              children: [
                const Icon(Icons.shield_outlined, color: AppColors.accent, size: 32),
                const SizedBox(width: 12),
                Expanded(
                  child: Text(
                    'privacy_intro'.tr(),
                    style: Theme.of(context).textTheme.bodyLarge,
                  ),
                ),
              ],
            ),
          ).fadeSlideIn(),
          const SizedBox(height: 20),
          for (var i = 0; i < sections.length; i++)
            Padding(
              padding: const EdgeInsets.only(bottom: 16),
              child: _PrivacySection(section: sections[i]),
            ).fadeSlideInList(index: i + 1),
          Text(
            'privacy_updated'.tr(),
            style: Theme.of(context).textTheme.bodySmall,
            textAlign: TextAlign.center,
          ).fadeSlideIn(delayMs: 400),
        ],
      ),
    );
  }
}

class _Section {
  const _Section(this.title, this.body);
  final String title;
  final String body;
}

class _PrivacySection extends StatelessWidget {
  const _PrivacySection({required this.section});

  final _Section section;

  @override
  Widget build(BuildContext context) {
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Text(
              section.title,
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    color: AppColors.primaryLight,
                  ),
            ),
            const SizedBox(height: 8),
            Text(
              section.body,
              style: Theme.of(context).textTheme.bodyLarge?.copyWith(height: 1.6),
            ),
          ],
        ),
      ),
    );
  }
}
