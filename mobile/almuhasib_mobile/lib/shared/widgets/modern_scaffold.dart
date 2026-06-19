import 'package:flutter/material.dart';

/// Scaffold with optional top gradient header background.
class ModernScaffold extends StatelessWidget {
  const ModernScaffold({
    super.key,
    this.appBar,
    required this.body,
    this.floatingActionButton,
    this.gradientColors,
  });

  final PreferredSizeWidget? appBar;
  final Widget body;
  final Widget? floatingActionButton;
  final List<Color>? gradientColors;

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      extendBodyBehindAppBar: appBar != null,
      appBar: appBar,
      floatingActionButton: floatingActionButton,
      body: Container(
        decoration: BoxDecoration(
          gradient: LinearGradient(
            begin: Alignment.topCenter,
            end: Alignment.bottomCenter,
            colors: [
              (gradientColors?.first ?? Theme.of(context).colorScheme.primary)
                  .withValues(alpha: 0.1),
              Theme.of(context).scaffoldBackgroundColor,
            ],
          ),
        ),
        child: body,
      ),
    );
  }
}
