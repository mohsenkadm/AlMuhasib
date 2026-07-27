# OneSignal setup (قيد Mobile)

App ID: `5c147934-b2d0-4a63-b92f-01964f8902cf`

Docs: https://documentation.onesignal.com/docs/en/flutter-sdk-setup

## Already configured in this repo

- `.env` / `.env.example` → `ONESIGNAL_APP_ID`
- Flutter SDK init in `NotificationService` (`onesignal_flutter`)
- Android: `POST_NOTIFICATIONS`, default notification icon + accent color
- iOS: `remote-notification` background mode, App Groups key, `Runner.entitlements`
- iOS Notification Service Extension target + Podfile entry

## Required in OneSignal Dashboard (you do this once)

1. Open https://app.onesignal.com → app for this App ID
2. **Android (FCM)**  
   Settings → Platforms → Google Android (FCM)  
   Upload Firebase Server Key / Service Account JSON from your Firebase project  
   Package name must match: `com.almuhasib.almuhasib_mobile`
3. **iOS (APNs)** — p8 recommended  
   Settings → Platforms → Apple iOS  
   Upload `.p8` key + Key ID + Team ID  
   Bundle ID must match: `com.almuhasib.almuhasibMobile`

## Required in Apple Developer / Xcode (you do this once)

1. Open `ios/Runner.xcworkspace` in Xcode
2. **Runner** target → Signing & Capabilities  
   - Enable **Push Notifications**  
   - Enable **Background Modes → Remote notifications** (Info.plist already has it)  
   - Confirm **App Groups** includes:  
     `group.com.almuhasib.almuhasibMobile.onesignal`
3. **OneSignalNotificationServiceExtension** target  
   - Same Team / signing as Runner  
   - Same App Group  
   - Bundle ID: `com.almuhasib.almuhasibMobile.OneSignalNotificationServiceExtension`
4. For App Store / TestFlight builds, set `aps-environment` to `production` in both entitlements files (or let Xcode manage automatically with "Automatically manage signing").

## Test

```bash
cd mobile/almuhasib_mobile
flutter run --release   # click listener works more reliably in release
```

Then send a test push from OneSignal Dashboard → Messages → New Push.

## Backend (REST API Key — do not commit)

The **Rest API Key** must never be committed to git.

Set it on the API/Admin host via environment variable or User Secrets:

```bash
# Environment (production / hosting)
OneSignal__AppId=5c147934-b2d0-4a63-b92f-01964f8902cf
OneSignal__RestApiKey=YOUR_REST_API_KEY
OneSignal__Enabled=true
```

```bash
# Local User Secrets (Api project)
dotnet user-secrets set "OneSignal:RestApiKey" "YOUR_REST_API_KEY" --project src/AlMuhasib.Api
```

`appsettings.json` may keep the public App ID; leave `RestApiKey` empty in the repo.
