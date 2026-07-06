import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;
import 'package:smooth_page_indicator/smooth_page_indicator.dart';

import '../../core/constants/app_colors.dart';
import '../../shared/widgets/app_animations.dart';
import 'onboarding_controller.dart';

class OnboardingScreen extends GetView<OnboardingController> {
  const OnboardingScreen({super.key}) : super(tag: 'onboarding');

  static const _slides = [
    _OnboardingSlideData(
      icon: Icons.analytics_outlined,
      titleKey: 'onboarding_title_1',
      descKey: 'onboarding_desc_1',
      colors: [Color(0xFF1565C0), Color(0xFF00ACC1)],
    ),
    _OnboardingSlideData(
      icon: Icons.receipt_long_outlined,
      titleKey: 'onboarding_title_2',
      descKey: 'onboarding_desc_2',
      colors: [Color(0xFF0D47A1), Color(0xFF1565C0)],
    ),
    _OnboardingSlideData(
      icon: Icons.notifications_active_outlined,
      titleKey: 'onboarding_title_3',
      descKey: 'onboarding_desc_3',
      colors: [Color(0xFF006064), Color(0xFF00ACC1)],
    ),
  ];

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      body: SafeArea(
        child: Column(
          children: [
            Align(
              alignment: AlignmentDirectional.centerEnd,
              child: TextButton(
                onPressed: controller.complete,
                child: Text('onboarding_skip'.tr()),
              ),
            ),
            Expanded(
              child: PageView.builder(
                controller: controller.pageController,
                itemCount: _slides.length,
                onPageChanged: controller.onPageChanged,
                itemBuilder: (_, index) {
                  final slide = _slides[index];
                  return Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 28),
                    child: Column(
                      mainAxisAlignment: MainAxisAlignment.center,
                      children: [
                        Container(
                          width: 140,
                          height: 140,
                          decoration: BoxDecoration(
                            gradient: LinearGradient(colors: slide.colors),
                            borderRadius: BorderRadius.circular(36),
                            boxShadow: [
                              BoxShadow(
                                color: slide.colors.last.withValues(alpha: 0.35),
                                blurRadius: 32,
                                offset: const Offset(0, 12),
                              ),
                            ],
                          ),
                          child: Icon(slide.icon, size: 64, color: Colors.white),
                        ).scaleIn(),
                        const SizedBox(height: 40),
                        Text(
                          slide.titleKey.tr(),
                          textAlign: TextAlign.center,
                          style: Theme.of(context).textTheme.displaySmall,
                        ).fadeSlideIn(delayMs: 100),
                        const SizedBox(height: 16),
                        Text(
                          slide.descKey.tr(),
                          textAlign: TextAlign.center,
                          style: Theme.of(context).textTheme.bodyLarge,
                        ).fadeSlideIn(delayMs: 180),
                      ],
                    ),
                  );
                },
              ),
            ),
            SmoothPageIndicator(
              controller: controller.pageController,
              count: _slides.length,
              effect: ExpandingDotsEffect(
                activeDotColor: AppColors.accent,
                dotColor: AppColors.accent.withValues(alpha: 0.25),
                dotHeight: 8,
                dotWidth: 8,
                expansionFactor: 3,
              ),
            ),
            const SizedBox(height: 24),
            Padding(
              padding: const EdgeInsets.fromLTRB(24, 0, 24, 24),
              child: SizedBox(
                width: double.infinity,
                child: FilledButton(
                  onPressed: controller.next,
                  style: FilledButton.styleFrom(
                    padding: const EdgeInsets.symmetric(vertical: 16),
                    shape: RoundedRectangleBorder(
                      borderRadius: BorderRadius.circular(14),
                    ),
                  ),
                  child: Obx(
                    () => Text(
                      controller.currentPage.value == _slides.length - 1
                          ? 'onboarding_start'.tr()
                          : 'onboarding_next'.tr(),
                    ),
                  ),
                ),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _OnboardingSlideData {
  const _OnboardingSlideData({
    required this.icon,
    required this.titleKey,
    required this.descKey,
    required this.colors,
  });

  final IconData icon;
  final String titleKey;
  final String descKey;
  final List<Color> colors;
}
