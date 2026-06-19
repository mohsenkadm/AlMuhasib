import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import '../../core/config/application_system_type.dart';
import '../../core/config/system_profile.dart';
import '../../core/getx/app_services.dart';
import '../../shared/widgets/app_animations.dart';

class SystemLaunchScreen extends StatefulWidget {
  const SystemLaunchScreen({super.key, required this.systemType});

  final ApplicationSystemType systemType;

  @override
  State<SystemLaunchScreen> createState() => _SystemLaunchScreenState();
}

class _SystemLaunchScreenState extends State<SystemLaunchScreen>
    with SingleTickerProviderStateMixin {
  late final AnimationController _controller;
  late final Animation<double> _scale;
  late final Animation<double> _fade;

  @override
  void initState() {
    super.initState();
    _controller = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 900),
    );
    _scale = Tween<double>(begin: 0.6, end: 1).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeOutBack),
    );
    _fade = Tween<double>(begin: 0, end: 1).animate(
      CurvedAnimation(parent: _controller, curve: Curves.easeOut),
    );
    _controller.forward();
    Future<void>.delayed(const Duration(milliseconds: 1200), _goHome);
  }

  void _goHome() {
    if (!mounted) return;
    final profile = SystemProfile.of(widget.systemType);
    Get.offNamed(profile.homeRoute);
  }

  @override
  void dispose() {
    _controller.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    final profile = SystemProfile.of(widget.systemType);
    final company = AppServices.prefs.companyName;

    return Scaffold(
      body: Container(
        width: double.infinity,
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topRight,
            end: Alignment.bottomLeft,
            colors: [
              profile.primary,
              profile.secondary,
              profile.accent.withValues(alpha: 0.85),
            ],
          ),
        ),
        child: SafeArea(
          child: FadeTransition(
            opacity: _fade,
            child: Column(
              mainAxisAlignment: MainAxisAlignment.center,
              children: [
                ScaleTransition(
                  scale: _scale,
                  child: Container(
                    width: 108,
                    height: 108,
                    decoration: BoxDecoration(
                      color: Colors.white.withValues(alpha: 0.18),
                      shape: BoxShape.circle,
                      border: Border.all(
                        color: Colors.white.withValues(alpha: 0.35),
                        width: 2,
                      ),
                    ),
                    child: Icon(profile.icon, size: 52, color: Colors.white),
                  ),
                ),
                const SizedBox(height: 28),
                Text(
                  profile.nameKey.tr(),
                  style: Theme.of(context).textTheme.headlineMedium?.copyWith(
                        color: Colors.white,
                        fontWeight: FontWeight.bold,
                      ),
                ).fadeSlideIn(delayMs: 200),
                const SizedBox(height: 8),
                Text(
                  profile.taglineKey.tr(),
                  textAlign: TextAlign.center,
                  style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                        color: Colors.white.withValues(alpha: 0.9),
                      ),
                ).fadeSlideIn(delayMs: 280),
                if (company != null && company.isNotEmpty) ...[
                  const SizedBox(height: 16),
                  Text(
                    company,
                    style: Theme.of(context).textTheme.titleMedium?.copyWith(
                          color: Colors.white.withValues(alpha: 0.85),
                        ),
                  ).fadeSlideIn(delayMs: 360),
                ],
                const SizedBox(height: 48),
                const SizedBox(
                  width: 28,
                  height: 28,
                  child: CircularProgressIndicator(
                    strokeWidth: 2.5,
                    color: Colors.white,
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
