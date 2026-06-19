import 'package:package_info_plus/package_info_plus.dart';

class AppInfo {
  AppInfo({
    required this.appName,
    required this.version,
    required this.buildNumber,
    required this.packageName,
  });

  final String appName;
  final String version;
  final String buildNumber;
  final String packageName;

  String get versionLabel => '$version+$buildNumber';
}

class AppInfoService {
  AppInfo? _cached;

  Future<AppInfo> load() async {
    if (_cached != null) return _cached!;
    final info = await PackageInfo.fromPlatform();
    _cached = AppInfo(
      appName: info.appName,
      version: info.version,
      buildNumber: info.buildNumber,
      packageName: info.packageName,
    );
    return _cached!;
  }
}
