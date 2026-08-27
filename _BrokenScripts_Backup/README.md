# Broken Scripts Backup

Moved out of `Assets/` on 2026-08-25 to clear 103+ compile errors.
Folder structure mirrors the original paths — drag a file back to restore it.

These scripts did NOT fail because of a code bug. They were copied from another
project without their dependencies. Two separate things are missing:

## 1. Third-party SDKs (not in Packages/manifest.json, not in Assets/)
- DOTween                  -> DG.Tweening
- Addressables             -> UnityEngine.AddressableAssets, UnityEngine.ResourceManagement
- Unity IAP                -> UnityEngine.Purchasing
- Firebase (Core/Analytics/RemoteConfig)
- AppsFlyer                -> AppsFlyerSDK
- Facebook SDK
- AppLovin MAX             -> MaxSdk / MaxSdkBase
- Nice Vibrations          -> MoreMountains.NiceVibrations
- SRDebugger               -> SRDebug
- Unity Mobile Notifications -> Unity.Notifications.Android / .iOS

## 2. The game's own core code (this is the blocker)
Installing the SDKs above is NOT enough — these types are also absent:

  Namespaces: Inwave.Production.Utils, Inwave.Production.Helper,
              Inwave.Production.Ads.AdNetwork, GamePopup, GameReward
  Types:      GlobalValues, GameEvents, GamePrefs, KeyPrefs, EventName, SceneId,
              Location, ParameterValue, ResourceType, AnalyticsFTUE, AnalyticsResouces,
              Singleton<>, FastSingleton<>, AdNetwork, AdsEvents, AdImpressionData,
              AppOpenAdNetwork, BannerCollapsibleNetwork, BannerCollapsiblePosition,
              IAPManager, ShopIAPPack, Reward, LifeConfig, LevelShelf,
              UISceneLoading, UISceneTransition, PopupNoticeFloat, PopupWait

To restore: copy the missing core code from the source project first,
then import the SDKs, then move these files back into Assets/.

## Still referenced by Assets/00FoodMaster/Prefabs/Initialize.prefab
Left intact on purpose — the "Missing Script" slots are a map of what was wired up:
  InitScript, SoundManager, LifeManager, GameIAPManager, VibrationsManager, NotificationManager
