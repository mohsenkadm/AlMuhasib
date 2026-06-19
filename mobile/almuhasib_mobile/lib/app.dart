import 'dart:ui' as ui;

import 'package:easy_localization/easy_localization.dart';
import 'package:flutter/material.dart';
import 'package:get/get.dart' hide Trans;

import 'core/getx/app_services.dart';
import 'core/router/app_pages.dart';
import 'core/router/route_guard.dart';

class AlMuhasibApp extends StatelessWidget {
  const AlMuhasibApp({super.key});

  @override
  Widget build(BuildContext context) {
    final themeController = AppServices.theme;

    return Obx(
      () {
        final themes = themeController.themes;
        return GetMaterialApp(
          title: 'app_name'.tr(),
          debugShowCheckedModeBanner: false,
          localizationsDelegates: context.localizationDelegates,
          supportedLocales: context.supportedLocales,
          locale: context.locale,
          theme: themes.$1,
          darkTheme: themes.$2,
          themeMode: themeController.themeMode.value,
          initialRoute: AppPages.initial,
          getPages: AppPages.routes,
          routingCallback: (routing) {
            if (routing?.current == null) return;
            final redirect = RouteGuard.redirect(routing!.current);
            if (redirect != null && redirect != routing.current) {
              Get.offAllNamed(redirect);
            }
          },
          builder: (context, child) {
            return Directionality(
              textDirection: context.locale.languageCode == 'ar'
                  ? ui.TextDirection.rtl
                  : ui.TextDirection.ltr,
              child: child ?? const SizedBox.shrink(),
            );
          },
        );
      },
    );
  }
}
