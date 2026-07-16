import 'dart:async';
import 'dart:math' as math;

import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:flutter_animate/flutter_animate.dart';
import 'package:flutter_native_splash/flutter_native_splash.dart';
import 'package:get/get.dart' hide Trans;

import '../../core/constants/app_colors.dart';
import '../../core/getx/app_services.dart';
import '../../shared/widgets/common_widgets.dart';

class SplashScreen extends StatefulWidget {
  const SplashScreen({super.key});

  @override
  State<SplashScreen> createState() => _SplashScreenState();
}

class _SplashScreenState extends State<SplashScreen>
    with TickerProviderStateMixin {
  static const _minDisplay = Duration(milliseconds: 2800);

  late final AnimationController _glowController;
  late final AnimationController _ringController;
  bool _exiting = false;

  @override
  void initState() {
    super.initState();
    _glowController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 2400),
    )..repeat(reverse: true);
    _ringController = AnimationController(
      vsync: this,
      duration: const Duration(milliseconds: 3200),
    )..repeat();

    WidgetsBinding.instance.addPostFrameCallback((_) {
      FlutterNativeSplash.remove();
    });
    unawaited(_runSplashSequence());
  }

  Future<void> _runSplashSequence() async {
    final started = DateTime.now();
    final auth = AppServices.auth;

    await auth.waitUntilReady();

    final elapsed = DateTime.now().difference(started);
    final remaining = _minDisplay - elapsed;
    if (remaining > Duration.zero && mounted) {
      await Future<void>.delayed(remaining);
    }

    if (!mounted || _exiting) return;
    _exiting = true;
    try {
      auth.leaveSplash();
    } catch (_) {
      Get.offAllNamed('/login');
    }
  }

  @override
  void dispose() {
    _glowController.dispose();
    _ringController.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: AppColors.brandNavy,
      body: DecoratedBox(
        decoration: const BoxDecoration(gradient: AppColors.splashGradient),
        child: Stack(
          fit: StackFit.expand,
          children: [
            const _SplashAmbientOrbs(),
            SafeArea(
              child: Column(
                children: [
                  const Spacer(flex: 5),
                  AnimatedBuilder(
                    animation: Listenable.merge([
                      _glowController,
                      _ringController,
                    ]),
                    builder: (context, child) {
                      final glow = 0.35 + (_glowController.value * 0.45);
                      return Stack(
                        alignment: Alignment.center,
                        children: [
                          CustomPaint(
                            size: const Size(220, 220),
                            painter: _SplashRingPainter(
                              progress: _ringController.value,
                              color: AppColors.brandAccent
                                  .withValues(alpha: 0.35 + glow * 0.2),
                            ),
                          ),
                          Container(
                            width: 168,
                            height: 168,
                            decoration: BoxDecoration(
                              shape: BoxShape.circle,
                              boxShadow: [
                                BoxShadow(
                                  color: AppColors.brandAccent
                                      .withValues(alpha: 0.22 * glow),
                                  blurRadius: 48 + (glow * 28),
                                  spreadRadius: 4,
                                ),
                                BoxShadow(
                                  color: Colors.black.withValues(alpha: 0.45),
                                  blurRadius: 28,
                                  offset: const Offset(0, 16),
                                ),
                              ],
                            ),
                          ),
                          child!,
                        ],
                      );
                    },
                    child: const AppLogoMark(size: 132, elevated: true)
                        .animate()
                        .fadeIn(duration: 650.ms, curve: Curves.easeOut)
                        .scale(
                          begin: const Offset(0.72, 0.72),
                          end: const Offset(1, 1),
                          duration: 1100.ms,
                          curve: Curves.elasticOut,
                        )
                        .then(delay: 180.ms)
                        .shimmer(
                          duration: 1400.ms,
                          color: Colors.white.withValues(alpha: 0.18),
                        ),
                  ),
                  const SizedBox(height: 36),
                  Text(
                    'app_name'.tr(),
                    style: Theme.of(context).textTheme.displaySmall?.copyWith(
                          color: Colors.white,
                          fontWeight: FontWeight.w900,
                          letterSpacing: 1.2,
                        ),
                  )
                      .animate()
                      .fadeIn(delay: 420.ms, duration: 500.ms)
                      .slideY(
                        begin: 0.35,
                        end: 0,
                        delay: 420.ms,
                        duration: 650.ms,
                        curve: Curves.easeOutCubic,
                      ),
                  const SizedBox(height: 10),
                  Text(
                    'splash_tagline'.tr(),
                    textAlign: TextAlign.center,
                    style: Theme.of(context).textTheme.bodyLarge?.copyWith(
                          color: Colors.white.withValues(alpha: 0.78),
                          height: 1.4,
                        ),
                  )
                      .animate()
                      .fadeIn(delay: 620.ms, duration: 500.ms)
                      .slideY(
                        begin: 0.25,
                        end: 0,
                        delay: 620.ms,
                        duration: 650.ms,
                        curve: Curves.easeOutCubic,
                      ),
                  const Spacer(flex: 4),
                  Padding(
                    padding: const EdgeInsets.fromLTRB(56, 0, 56, 36),
                    child: Column(
                      children: [
                        ClipRRect(
                          borderRadius: BorderRadius.circular(99),
                          child: const LinearProgressIndicator(
                            minHeight: 3.5,
                            backgroundColor: Color(0x33FFFFFF),
                            color: AppColors.brandAccent,
                          ),
                        )
                            .animate()
                            .fadeIn(delay: 900.ms, duration: 400.ms)
                            .scaleX(
                              begin: 0.4,
                              end: 1,
                              delay: 900.ms,
                              duration: 700.ms,
                              curve: Curves.easeOutCubic,
                              alignment: Alignment.center,
                            ),
                        const SizedBox(height: 14),
                        Text(
                          'splash_loading'.tr(),
                          style:
                              Theme.of(context).textTheme.labelMedium?.copyWith(
                                    color: Colors.white.withValues(alpha: 0.55),
                                    letterSpacing: 0.4,
                                  ),
                        ).animate().fadeIn(delay: 1100.ms, duration: 400.ms),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _SplashAmbientOrbs extends StatelessWidget {
  const _SplashAmbientOrbs();

  @override
  Widget build(BuildContext context) {
    return IgnorePointer(
      child: Stack(
        children: [
          Positioned(
            top: -80,
            right: -60,
            child: _Orb(
              size: 220,
              color: AppColors.brandAccent.withValues(alpha: 0.16),
            ).animate(onPlay: (c) => c.repeat(reverse: true)).moveY(
                  begin: 0,
                  end: 18,
                  duration: 3200.ms,
                  curve: Curves.easeInOut,
                ),
          ),
          Positioned(
            bottom: 40,
            left: -70,
            child: _Orb(
              size: 260,
              color: AppColors.primaryLight.withValues(alpha: 0.12),
            ).animate(onPlay: (c) => c.repeat(reverse: true)).moveY(
                  begin: 0,
                  end: -22,
                  duration: 3800.ms,
                  curve: Curves.easeInOut,
                ),
          ),
        ],
      ),
    );
  }
}

class _Orb extends StatelessWidget {
  const _Orb({required this.size, required this.color});

  final double size;
  final Color color;

  @override
  Widget build(BuildContext context) {
    return Container(
      width: size,
      height: size,
      decoration: BoxDecoration(
        shape: BoxShape.circle,
        gradient: RadialGradient(
          colors: [color, color.withValues(alpha: 0)],
        ),
      ),
    );
  }
}

class _SplashRingPainter extends CustomPainter {
  _SplashRingPainter({required this.progress, required this.color});

  final double progress;
  final Color color;

  @override
  void paint(Canvas canvas, Size size) {
    final center = Offset(size.width / 2, size.height / 2);
    final radius = size.shortestSide / 2;
    final paint = Paint()
      ..style = PaintingStyle.stroke
      ..strokeWidth = 1.6
      ..strokeCap = StrokeCap.round
      ..color = color;

    final start = progress * math.pi * 2;
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius),
      start,
      math.pi * 1.15,
      false,
      paint,
    );
    canvas.drawArc(
      Rect.fromCircle(center: center, radius: radius - 10),
      -start * 1.2,
      math.pi * 0.7,
      false,
      paint..color = color.withValues(alpha: color.a * 0.55),
    );
  }

  @override
  bool shouldRepaint(covariant _SplashRingPainter oldDelegate) =>
      oldDelegate.progress != progress || oldDelegate.color != color;
}
