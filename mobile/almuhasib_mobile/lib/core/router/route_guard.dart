import 'package:flutter/material.dart';
import 'package:get/get.dart';

import '../config/system_profile.dart';
import '../getx/app_services.dart';
import 'app_routes.dart';

class RouteGuard {
  RouteGuard._();

  static bool _isProfileRoute(String path) =>
      path.startsWith('/profile') ||
      path == AppRoutes.settings ||
      path == AppRoutes.about ||
      path == AppRoutes.privacy ||
      path == AppRoutes.hotelSettings ||
      path == AppRoutes.carSettings;

  static bool _isLaunchRoute(String path) => path.startsWith('/launch');

  static String? redirect(String? path) {
    if (path == null || path.isEmpty) return AppRoutes.splash;

    final auth = AppServices.auth;
    final prefs = AppServices.prefs;

    final isSplash = path == AppRoutes.splash;
    final isOnboarding = path == AppRoutes.onboarding;
    final isLogin = path == AppRoutes.login;

    if (auth.isLoading.value) {
      return isSplash ? null : AppRoutes.splash;
    }

    if (!prefs.onboardingCompleted && !isOnboarding && !isSplash) {
      return AppRoutes.onboarding;
    }

    if (!auth.isAuthenticated.value && !isLogin && !isOnboarding && !isSplash) {
      return AppRoutes.login;
    }

    if (auth.isAuthenticated.value &&
        (isLogin || isOnboarding || isSplash)) {
      return prefs.launchRoute;
    }

    if (auth.isAuthenticated.value &&
        !_isLaunchRoute(path) &&
        !_isProfileRoute(path) &&
        !routeBelongsToSystem(path, prefs.systemType)) {
      return prefs.homeRoute;
    }

    if (!auth.isAuthenticated.value &&
        !auth.isLoading.value &&
        isSplash) {
      if (!prefs.onboardingCompleted) return AppRoutes.onboarding;
      return AppRoutes.login;
    }

    if (!auth.isAuthenticated.value && _isProfileRoute(path)) {
      return AppRoutes.login;
    }

    return null;
  }
}

class AuthMiddleware extends GetMiddleware {
  @override
  int? get priority => 1;

  @override
  RouteSettings? redirect(String? route) {
    final target = RouteGuard.redirect(route);
    if (target != null && target != route) {
      return RouteSettings(name: target);
    }
    return null;
  }
}
