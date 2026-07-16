import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../../../core/getx/app_services.dart';
import '../common_widgets.dart';
import 'app_sliver_app_bar.dart';

class AppPageScaffold extends StatelessWidget {
  const AppPageScaffold({
    super.key,
    this.title,
    this.subtitle,
    this.appBar,
    this.actions,
    this.leading,
    this.body,
    this.child,
    this.floatingActionButton,
    this.showConnectivityBanner = true,
    this.showGradientBackground = true,
    this.useSliver = false,
    this.slivers,
    this.showProgress = false,
  });

  final String? title;
  final String? subtitle;
  final PreferredSizeWidget? appBar;
  final List<Widget>? actions;
  final Widget? leading;
  final Widget? body;
  final Widget? child;
  final Widget? floatingActionButton;
  final bool showConnectivityBanner;
  final bool showGradientBackground;
  final bool useSliver;
  final List<Widget>? slivers;
  final bool showProgress;

  @override
  Widget build(BuildContext context) {
    final profile = AppServices.prefs.systemProfile;
    final content = child ?? body ?? const SizedBox.shrink();

    return Obx(() {
      final isOffline = showConnectivityBanner
          ? AppServices.connectivity.isOffline.value
          : false;

      if (useSliver && title != null) {
        return Scaffold(
          extendBodyBehindAppBar: true,
          floatingActionButton: floatingActionButton,
          body: Column(
            children: [
              if (showConnectivityBanner)
                ConnectivityBanner(isOffline: isOffline),
              Expanded(
                child: DecoratedBox(
                  decoration: showGradientBackground
                      ? BoxDecoration(
                          gradient: LinearGradient(
                            begin: Alignment.topCenter,
                            end: Alignment.bottomCenter,
                            colors: [
                              profile.primary.withValues(alpha: 0.08),
                              Theme.of(context).scaffoldBackgroundColor,
                            ],
                          ),
                        )
                      : const BoxDecoration(),
                  child: CustomScrollView(
                    slivers: [
                      AppSliverAppBar(
                        title: title!,
                        subtitle: subtitle,
                        actions: actions,
                      ),
                      ...?slivers,
                    ],
                  ),
                ),
              ),
            ],
          ),
        );
      }

      return Scaffold(
        appBar: appBar ??
            (title != null
                ? AppStandardAppBar(
                    title: title!,
                    subtitle: subtitle,
                    actions: actions,
                    leading: leading,
                    showProgress: showProgress,
                  )
                : null),
        floatingActionButton: floatingActionButton,
        body: Column(
          children: [
            if (showConnectivityBanner)
              ConnectivityBanner(isOffline: isOffline),
            Expanded(
              child: DecoratedBox(
                decoration: showGradientBackground
                    ? BoxDecoration(
                        gradient: LinearGradient(
                          begin: Alignment.topCenter,
                          end: Alignment.bottomCenter,
                          colors: [
                            profile.primary.withValues(alpha: 0.06),
                            Theme.of(context).scaffoldBackgroundColor,
                          ],
                        ),
                      )
                    : const BoxDecoration(),
                child: content,
              ),
            ),
          ],
        ),
      );
    });
  }
}
