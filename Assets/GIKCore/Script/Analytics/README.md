# GIKCore / Analytics

**Purpose** — one façade for every analytics provider. Game code calls `Analytics` and never touches an SDK type, so the project still compiles with every SDK absent (§3.6).

**Entry point** — `Analytics`. Everything else in this folder is either a provider it fans out to, or a `MonoBehaviour` that boots an SDK.

```
Analytics ──► FirebaseAnalyticsProvider ──► Firebase.Analytics
          ├─► AppsFlyerProvider         ──► AppsFlyerSDK
          └─► FacebookProvider          ──► Facebook.Unity
```

## Define symbols

`SdkDefineSynchronizer` (in `GIKCore/Editor/`) runs on every domain reload, looks for the SDK type in the loaded assemblies, and adds or removes the symbol for Android, iOS and Standalone. Import an SDK and the symbol turns on by itself; delete it and the symbol goes away. Re-run by hand from **GIKCore ▸ Sync SDK Defines**.

| Symbol | Detected type |
|---|---|
| `FIREBASE_SDK` | `Firebase.FirebaseApp` |
| `FIREBASE_ANALYTICS` | `Firebase.Analytics.FirebaseAnalytics` |
| `FIREBASE_CRASHLYTICS` | `Firebase.Crashlytics.Crashlytics` |
| `FIREBASE_REMOTE_CONFIG` | `Firebase.RemoteConfig.FirebaseRemoteConfig` |
| `APPSFLYER_SDK` | `AppsFlyerSDK.AppsFlyer` |
| `FACEBOOK_SDK` | `Facebook.Unity.FB` |

Never add these by hand in Player Settings — the synchronizer will strip a symbol whose SDK is not there.

## Routing

- An event in `Analytics.AppsFlyerOnlyEvents` (`af_session`, `af_tutorial_completion`, the four `af_*_displayed` / `af_*_successfullyloaded`) goes to **AppsFlyer + Facebook only**.
- Every other event goes to **Firebase only**.
- `LogPurchase` and `LogAdRevenue` are the exceptions: they fan out to all three, because each provider needs its own revenue call.

## A provider type exists only when its SDK does

`FirebaseAnalyticsProvider`, `AppsFlyerProvider` and `FacebookProvider` are each wrapped **whole** in their define — file opens with `#if APPSFLYER_SDK` and closes with `#endif`. With the SDK absent the type does not exist at all; there is no stub to keep in sync.

That means nothing may name a provider outside its define. `Analytics` keeps every `#if` in its private `Dispatch*` methods at the bottom of the file, so the public API above them stays free of preprocessor noise:

```csharp
private static void DispatchAppsFlyer(string eventName, Dictionary<string, object> payload)
{
#if APPSFLYER_SDK
    AppsFlyerProvider.LogEvent(eventName, payload);
#endif
}
```

Adding a provider means: wrap its class in the define, add one `Dispatch*` method here, call that. Never call a provider type directly from a public method.

## Queueing

Each provider holds its own queue, capped at 50, and drops the oldest entry when full. Events fired before an SDK reports ready are replayed in order when it does. Nothing is lost between app start and `FirebaseApp.CheckAndFixDependenciesAsync` finishing.

## Events out

- `FirebaseSDK.Ready` — publisher: `FirebaseSDK.Initialize()` · subscriber: `RemoteConfigService.OnFirebaseReady`
- `FirebaseSDK.Failed` — publisher: `FirebaseSDK.Fail()` · subscriber: `RemoteConfigService.OnFirebaseFailed`
- `FirebaseSDK.UserIdReady` — publisher: `FirebaseSDK.ResolveUserId()` · subscriber: none in core; game code may subscribe
- `RemoteConfigService.LoadEnded` — publisher: `RemoteConfigService.EndLoad()` · subscriber: none in core

## Scene setup

`FirebaseSDK`, `RemoteConfigService`, `AppsFlyerManager` and `FacebookManager` are `DontDestroyOnLoad` process services (§3.2). Each goes on its own prefab in the splash scene. Execution order is fixed by attribute: `UserDataManager` (-100) → `FirebaseSDK` (-90) → `AppsFlyerManager` / `FacebookManager` (-85) → `RemoteConfigService` (-80).

`FirebaseSDK` stores the analytics instance id through `UserDataManager.SetUserPseudoId` — there is no extra PlayerPrefs key (§3.5).

## Usage

```csharp
Analytics.LogEvent(EventName.LevelStart, Analytics.CreateParam(ParameterName.Level, 12));

Analytics.LogLevel(EventName.LevelWin, 12,
    Analytics.CreateParam(ParameterName.Playtime, 43.5f));

Analytics.LogScreen("HomeScreen");

Analytics.LogEvent(EventName.AdShow, Analytics.CreateParam(
    ParameterName.AdFormat, ParameterValue.AdFormatRewarded,
    ParameterName.AdPlacement, "double_reward"));

Analytics.LogPurchase("starter_pack", 4.99m, "USD");
Analytics.LogAdRevenue(0.0032d, "USD", ParameterValue.AdFormatInterstitial);

Analytics.SetUserProperty(UserPropertyName.LevelMax, 12);
Analytics.LogException("LevelLoader", "level 12 asset missing");

RemoteConfigService.SetDefault("inter_cooldown", 30);
int cooldown = RemoteConfigService.GetInt("inter_cooldown", 30);
```

Call `RemoteConfigService.SetDefault` before `FirebaseSDK` reports ready — defaults are pushed to the SDK at the start of `Fetch()`.

A new event name goes in `EventName` only if every game needs it; game-specific names belong in `Assets/<GameName>/` (§2.2).
