#!/usr/bin/env ruby
# Adds OneSignal Notification Service Extension target + Runner entitlements.
# Docs: https://documentation.onesignal.com/docs/en/flutter-sdk-setup

require 'xcodeproj'
require 'securerandom'

ROOT = File.expand_path('..', __dir__)
PROJECT_PATH = File.join(ROOT, 'ios', 'Runner.xcodeproj')
NSE_NAME = 'OneSignalNotificationServiceExtension'
BUNDLE_ID = 'com.almuhasib.almuhasibMobile'
NSE_BUNDLE_ID = "#{BUNDLE_ID}.OneSignalNotificationServiceExtension"
APP_GROUP = "group.#{BUNDLE_ID}.onesignal"

project = Xcodeproj::Project.open(PROJECT_PATH)

# Skip if already present
if project.targets.any? { |t| t.name == NSE_NAME }
  puts "#{NSE_NAME} already exists — updating Runner entitlements only"
else
  nse_group = project.main_group.find_subpath(NSE_NAME, true)
  nse_group.set_source_tree('<group>')
  nse_group.set_path(NSE_NAME)

  swift_ref = nse_group.new_file('NotificationService.swift')
  info_ref = nse_group.new_file('Info.plist')
  entitlements_ref = nse_group.new_file("#{NSE_NAME}.entitlements")

  target = project.new_target(:app_extension, NSE_NAME, :ios, '13.0')
  target.add_file_references([swift_ref])

  target.build_configurations.each do |config|
    config.build_settings['INFOPLIST_FILE'] = "#{NSE_NAME}/Info.plist"
    config.build_settings['PRODUCT_BUNDLE_IDENTIFIER'] = NSE_BUNDLE_ID
    config.build_settings['CODE_SIGN_ENTITLEMENTS'] = "#{NSE_NAME}/#{NSE_NAME}.entitlements"
    config.build_settings['CODE_SIGN_STYLE'] = 'Automatic'
    config.build_settings['CURRENT_PROJECT_VERSION'] = '1'
    config.build_settings['GENERATE_INFOPLIST_FILE'] = 'NO'
    config.build_settings['IPHONEOS_DEPLOYMENT_TARGET'] = '13.0'
    config.build_settings['LD_RUNPATH_SEARCH_PATHS'] = [
      '$(inherited)',
      '@executable_path/Frameworks',
      '@executable_path/../../Frameworks',
    ]
    config.build_settings['SKIP_INSTALL'] = 'YES'
    config.build_settings['TARGETED_DEVICE_FAMILY'] = '1,2'
    config.build_settings['SWIFT_VERSION'] = '5.0'
    config.build_settings['PRODUCT_NAME'] = '$(TARGET_NAME)'
    config.build_settings['MARKETING_VERSION'] = '1.0'
  end

  # Embed NSE into Runner
  runner = project.targets.find { |t| t.name == 'Runner' }
  raise 'Runner target not found' unless runner

  embed = runner.copy_files_build_phases.find { |p| p.name == 'Embed Foundation Extensions' }
  unless embed
    embed = runner.new_copy_files_build_phase('Embed Foundation Extensions')
    embed.dst_subfolder_spec = '13' # PlugIns
  end
  embed.add_file_reference(target.product_reference) unless embed.files_references.include?(target.product_reference)

  runner.add_dependency(target)
  puts "Added #{NSE_NAME} target (#{NSE_BUNDLE_ID})"
end

# Runner entitlements
runner = project.targets.find { |t| t.name == 'Runner' }
raise 'Runner target not found' unless runner

runner_group = project.main_group.find_subpath('Runner', true)
ent_path = 'Runner/Runner.entitlements'
unless runner_group.files.any? { |f| f.path&.end_with?('Runner.entitlements') }
  runner_group.new_file('Runner.entitlements')
end

runner.build_configurations.each do |config|
  config.build_settings['CODE_SIGN_ENTITLEMENTS'] = ent_path
  # Development for Debug/Profile; Release uses production APNs when distributing.
  # Entitlements file uses "development"; App Store build should switch via Xcode.
end

project.save
puts "Saved #{PROJECT_PATH}"
puts "App Group: #{APP_GROUP}"
puts 'Next: cd ios && pod install'
