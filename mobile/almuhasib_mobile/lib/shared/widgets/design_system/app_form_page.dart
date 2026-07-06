import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../../core/theme/app_spacing.dart';
import '../form_section_card.dart';
import 'app_progress_button.dart';
import 'app_sliver_app_bar.dart';

class AppFormPage extends StatelessWidget {
  const AppFormPage({
    super.key,
    required this.title,
    this.subtitle,
    required this.formKey,
    required this.sections,
    required this.saveLabel,
    required this.onSave,
    this.isSaving,
    this.leading,
    this.extraActions,
    this.bottomBar,
  });

  final String title;
  final String? subtitle;
  final GlobalKey<FormState> formKey;
  final List<AppFormSection> sections;
  final String saveLabel;
  final VoidCallback onSave;
  final RxBool? isSaving;
  final Widget? leading;
  final List<Widget>? extraActions;
  final Widget? bottomBar;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      appBar: AppStandardAppBar(
        title: title,
        subtitle: subtitle,
        leading: leading,
        actions: extraActions,
      ),
      body: Column(
        children: [
          if (isSaving != null)
            Obx(
              () => isSaving!.value
                  ? const LinearProgressIndicator(minHeight: 3)
                  : const SizedBox.shrink(),
            ),
          Expanded(
            child: Form(
              key: formKey,
              child: Column(
                children: [
                  Expanded(
                    child: ListView(
                      padding: const EdgeInsets.all(AppSpacing.xl),
                      children: [
                        for (final section in sections)
                          FormSectionCard(
                            title: section.title,
                            subtitle: section.subtitle,
                            children: section.children,
                          ),
                        const SizedBox(height: 80),
                      ],
                    ),
                  ),
                  bottomBar ??
                      SafeArea(
                        minimum: const EdgeInsets.all(AppSpacing.xl),
                        child: isSaving != null
                            ? Obx(
                                () => AppProgressButton(
                                  label: saveLabel,
                                  isLoading: isSaving!.value,
                                  onPressed: onSave,
                                  icon: Icons.save_rounded,
                                ),
                              )
                            : AppProgressButton(
                                label: saveLabel,
                                onPressed: onSave,
                                icon: Icons.save_rounded,
                              ),
                      ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}

class AppFormSection {
  const AppFormSection({
    required this.title,
    required this.children,
    this.subtitle,
  });

  final String title;
  final String? subtitle;
  final List<Widget> children;
}
