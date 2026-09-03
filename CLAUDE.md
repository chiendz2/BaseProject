# CLAUDE.md — BaseProject Coding Standards (AI-First)

> **Purpose**: This file defines how AI agents (and humans) write code in this project. Read it fully before any task. When in doubt, follow existing patterns in the codebase — consistency beats personal preference.
> *(VN: File này định nghĩa cách AI và người viết code trong dự án. Đọc kỹ trước mọi task. Khi phân vân: theo pattern có sẵn, nhất quán quan trọng hơn sở thích cá nhân.)*

> **Scope — ALL agents**: these rules are normative for **every** AI agent in this repo — Claude Code, **Codex** (`AGENTS.md`), **Antigravity / Gemini** (`GEMINI.md`, `.antigravity/rules/`), and anything else. Those files are thin loaders that point back here; **this file is the single source of truth**. See §9.
> *(VN: Rule áp cho MỌI agent — Claude, Codex, Antigravity/Gemini. AGENTS.md và GEMINI.md chỉ là file trỏ về đây, không chép lại rule. CLAUDE.md là nguồn chuẩn duy nhất — xem §9.)*

## 0. Project Info

- **BaseProject** = a reusable Unity base/template. `Assets/GIKCore/` is the shared core that gets carried into each new game.
- Unity `6000.0.73f1` — Render pipeline: `URP 17.3.0` — target: mobile (Android / iOS). The project was originally authored on a **6.3** Editor, so `manifest.json` carried two built-in modules that do not exist in 6.0 (`com.unity.modules.adaptiveperformance`, `com.unity.modules.vectorgraphics`) and package resolution failed outright. They are removed. **A `com.unity.modules.*` entry is only valid if the running Editor ships it** — check `Editor/Data/Resources/PackageManager/BuiltInPackages/` before adding one back.
- **Assemblies: none for our code.** No `.asmdef` under `Assets/GIKCore/` or a game folder; everything we write compiles into `Assembly-CSharp`. Third-party folders ship their own (`Demigiant/`, `Sirenix/`, `Spine/`) — leave those exactly as imported. The compiler enforces no boundaries on our code — you enforce them by hand (§2.2, §2.3).
- **Async style: callback-first.** `Action<T>` callbacks (`AddressablePrefabLoader.Load`, `UIManager.ShowPopup`) with thin `Task`-returning wrappers built on `TaskCompletionSource` (`ShowPopupAsync`). There is **no coroutine, no `Awaitable`, no UniTask** in our code. Keep it that way.
- **Tweening: DOTween Pro**, imported as an asset into `Assets/Demigiant/` — **not** the `com.demigiant.dotween` UPM package. It is here to back `com.brunomikoski.animationsequencer`. Because there is no UPM package id to match, that package's `versionDefines` never fire: **`DOTWEEN_ENABLED` is set by hand in Player Settings for Android, iOS and Standalone.** Drop that define and Animation Sequencer compiles to an empty assembly (58 of its 65 files are inside `#if DOTWEEN_ENABLED`).
- **Asset loading: Addressables** (`com.unity.addressables 2.9.1`) — `Addressables.InstantiateAsync` by string key. No `Resources.Load` in our code; `Assets/Resources/` exists only to hold third-party `DOTweenSettings.asset`.
- **Firebase `12.10.1`** — `app`, `analytics`, `crashlytics`, `installations`, `messaging`, `remote-config`, plus External Dependency Manager `1.2.186`. All installed as **local tarballs** from `FirebaseLib/*.tgz` (§0.1). `google-services.json` and `GoogleService-Info.plist` are **not** in the repo — Firebase init fails at runtime until they are added (§3.6).
- Input: new Input System (`Assets/InputSystem_Actions.inputactions`).
- Tests: Test Framework `1.6.0` is installed but **no test assembly exists yet** (§5, step 4).
- Editor automation: MCP for Unity (`com.coplaydev.unity-mcp`) is installed — agents can drive the Editor. §4.1 applies to everything written through it.

*(VN: Đây là project base dùng lại cho nhiều game. `Assets/GIKCore/` là phần lõi dùng chung. Code mình viết không có asmdef — ranh giới module phải tự giữ. Async dùng callback + Task, KHÔNG coroutine/UniTask. DOTween Pro cài dạng asset ở `Assets/Demigiant/` để phục vụ Animation Sequencer — bắt buộc phải có define `DOTWEEN_ENABLED`, gỡ ra là package đó rỗng. Firebase cài từ file .tgz trong `FirebaseLib/`, còn thiếu `google-services.json`.)*

### 0.1 Local packages — `FirebaseLib/`

`FirebaseLib/` sits **next to** `Assets/`, not inside it, so Unity never imports it. It is the drop folder for everything installed off-disk, wired through `Packages/manifest.json`:

| Entry in manifest | Source in `FirebaseLib/` |
|---|---|
| `com.antonysze.custom-play-button` | `unity-custom-play-button-main/` |
| `com.brunomikoski.animationsequencer` | `Animation-Sequencer-develop/` |
| `com.codewriter.tutorial-mask` | `TutorialMaskForUGUI-main/` |
| `com.coffee.ui-particle` | `ParticleEffectForUGUI-main/` |
| `com.qiaozhilei.easy-text-effects` | `Easy-Text-Effects-for-Unity-main/` |
| `com.google.external-dependency-manager` | `com.google.external-dependency-manager-1.2.186.tgz` |
| `com.google.firebase.*` (6 packages) | `com.google.firebase.*-12.10.1.tgz` |

- Adding a package here means **two** steps: put the folder/tarball in `FirebaseLib/`, then add the `file:../FirebaseLib/...` line to `manifest.json`. Dropping the file alone does nothing — UPM never sees it.
- A local **tarball** resolves no dependencies from a registry: every dependency it declares must have its own manifest line pointing at its own local tarball.
- `ShinyEffectForUGUI-develop/` is the exception — its root `package.json` is a UPM *exporter* manifest (`"src": "Assets/ShinyEffectForUGUI"`), not a package. It is installed by copying `Assets/ShinyEffectForUGUI/` into `Assets/` as a plain folder, and stays out of `manifest.json`.

*(VN: `FirebaseLib/` nằm ngoài `Assets/` nên Unity không import. Muốn cài: bỏ file vào đây RỒI thêm dòng `file:../FirebaseLib/...` vào `manifest.json` — chỉ copy file thôi là vô nghĩa. File .tgz không tự kéo dependency, phải khai đủ từng cái. Riêng ShinyEffect không phải package UPM, copy thẳng vào `Assets/`.)*

---

## 1. Core Principle — Context Locality

The single measure of good architecture here:

> **"Can an agent read ONE folder and make a correct change without spelunking the whole repo?"**

- Optimize for **locality of behavior**, not layer purity.
- **Indirection is a cost, not a virtue.** Every extra hop (interface → impl → factory → event) must pay for itself.
- SOLID is applied pragmatically (§2.5), never dogmatically.

*(VN: Kiến trúc tốt = AI chỉ cần đọc 1 folder là sửa đúng. Mỗi tầng gián tiếp là một chi phí, chỉ thêm khi nó tự trả được giá của nó.)*

---

## 2. Project Structure

### 2.1 The real tree — this is what exists today

```
Assets/
  GIKCore/                          ← shared core. Treat it as a library.
    Scene/SplasSence.unity          ← entry scene, build index 0 (§3.7)
    Scene/Home.unity                ← build index 1. Empty on purpose: the scene each game builds on
    Prefab/                         ← Initialize, UIManager, UserDataManager, SceneLoader, InitBG,
                                       PopupTemplate, FirebaseSDK, RemoteConfigService,
                                       AppsFlyerManager, FacebookManager, GDPRManager, and the
                                       empty placeholders AdsManager, AdsManagerEventHandler,
                                       AnalyticManager, Appsflyer, FirebaseInitializer
    Script/                         ← every file is namespace `GIKCore` (§4)
      SplashController.cs           ← splash timing only, delegates loading to SceneLoader (§3.7)
      SceneLoader.cs                ← THE scene-loading service. Nothing else calls LoadSceneAsync (§3.7)
      PersistentRoot.cs             ← keeps the Initialize prefab alive across scene loads (§3.2)
      SoundManager.cs               ← sfx + music, reads UserDataManager sound/music flags (§3.5)
      GDPRManager.cs                ← UMP consent, guarded by GOOGLE_MOBILE_ADS (§3.6)
      SystemTime.cs                 ← platform uptime, [DefaultExecutionOrder(-1)]
      AnimationId.cs                ← cached Animator hashes (§3.1)
      ShaderPropertyId.cs           ← cached Shader property ids (§3.1)
      Data/
        UserData.cs                 ← serializable save DTO, fields only (§3.5)
        UserDataManager.cs          ← parse/save + Get/Set functions (§3.5)
      Analytics/                    ← Analytics façade + providers + SDK managers. README inside (§3.6)
        Analytics.cs                ← the ONLY entry point game code calls
        FirebaseSDK.cs  RemoteConfigService.cs  CrashlyticsReporter.cs
        AppsFlyerManager.cs  FacebookManager.cs
        EventName.cs  ParameterName.cs  ParameterValue.cs  UserPropertyName.cs
      UI/                           ← everything popup/canvas related
        UIManager.cs                ← popup stack, static facade, shared blocker (§3.4)
        PopupBase.cs                ← base class for every popup
        PopupTemplate.cs            ← the worked example to copy
        PopupId.cs                  ← popup key constants. Holds PopupTemplate (§3.4)
        AddressablePrefabLoader.cs  ← the only Addressables entry point
        UIScaleToFillScreen.cs
    Editor/                         ← SdkDefineSynchronizer — auto sets/clears SDK defines (§3.6)
    Pool/                           ← reserved for pooling, empty today (§7.1)
    Textures/
  AddressableAssetsData/            ← Addressables settings. 'Default Local Group' holds PopupTemplate (§3.4)
  Settings/                         ← URP pipeline + renderer assets
  Resources/                        ← DOTweenSettings.asset ONLY. Not a place for game assets (§0)
  InputSystem_Actions.inputactions
  TextMesh Pro/                     ← THIRD PARTY ↓ do not edit, do not lint (§4.1)
  Demigiant/                        ← DOTween Pro + DemiLib
  Sirenix/                          ← Odin Inspector
  Spine/  Spine Examples/           ← Spine runtime
  ShinyEffectForUGUI/               ← copied out of FirebaseLib, no UPM entry (§0.1)
  Plugins/Android/                  ← GENERATED by External Dependency Manager
  GeneratedLocalRepo/               ← GENERATED by External Dependency Manager
FirebaseLib/                        ← local packages + Firebase tarballs, outside Assets (§0.1)
.editorconfig                       ← formatting contract, see §4
Packages/manifest.json
Packages/packages-lock.json
```

`Assets/Plugins/Android/` (`mainTemplate.gradle`, `settingsTemplate.gradle`, `gradleTemplate.properties`, `AndroidManifest.xml`) and `Assets/GeneratedLocalRepo/` are **written by External Dependency Manager**. Never hand-edit them — change the dependency and re-run *Assets ▸ External Dependency Manager ▸ Android Resolver ▸ Force Resolve*.

*(VN: `Assets/Plugins/Android/` và `Assets/GeneratedLocalRepo/` do External Dependency Manager sinh ra — KHÔNG sửa tay, muốn đổi thì chạy lại Force Resolve.)*

### 2.2 Where new code goes — HARD RULE

- **`Assets/GIKCore/` is the shared core.** Game-specific code, art, scenes and tuning values **never** go in there.
- **Game code lives in `Assets/<GameName>/`**, mirroring the split already used in the studio's shipped projects (`Assets/00SeatPuzzle/` + `Assets/Framework/`):

  ```
  Assets/<GameName>/
    Scripts/      Prefabs/      Scenes/      Data/      Textures/
  ```

- **Dependency direction is one-way and non-negotiable**:
  `Assets/<GameName>/` **may** reference `GIKCore`. `GIKCore` must **never** reference game code — no `using <GameName>`, no game type in a core signature, no game string in a core file.
- **Promotion into `GIKCore`** requires the **Rule of Three**: used by ≥3 games, and its public surface mentions nothing game-specific. Until then it stays in the game folder, duplicated if necessary.
- Inside a folder, group by **feature**, never by technical layer. No `Scripts/Managers/` dump.

*(VN: `GIKCore` = lõi dùng chung, KHÔNG bỏ code riêng của game vào. Code game nằm ở `Assets/<TênGame>/`. Phụ thuộc một chiều: game → GIKCore, tuyệt đối không ngược lại. Muốn đẩy code lên GIKCore thì phải dùng ở ≥3 game và không dính type riêng của game nào.)*

### 2.3 Assemblies (`.asmdef`)

- **We own none, and that is the current state — not a bug to fix.** The `.asmdef` files under `Assets/Demigiant/`, `Assets/Sirenix/` and `Assets/Spine/` belong to those vendors; they are not ours and are not a precedent.
- Do **not** add an asmdef "for cleanliness". Add one only when there is a concrete reason: compile time actually hurts, a folder must be excluded from a platform, or a test assembly is needed.
- Adding one asmdef forces asmdefs on everything it touches. **Flag it to the user before doing it** — never as a side effect of another task.

*(VN: Code của mình chưa có asmdef nào và đó là bình thường — mấy file asmdef trong `Demigiant/`, `Sirenix/`, `Spine/` là của bên thứ ba, không tính. Đừng tự ý thêm — chỉ thêm khi có lý do thật, và phải hỏi user trước.)*

### 2.4 Composition over inheritance

- Inheritance depth: **max 2 levels** below `MonoBehaviour`. The existing chain `MonoBehaviour → PopupBase → PopupTemplate` is already at the limit — do not add a third level under `PopupBase`.
- Prefer small components + `[RequireComponent]` over base-class hierarchies.
- One component = one behavior. `UIManager.cs` (~300 lines) is the largest class here and is the ceiling, not the target — anything bigger must be split.

*(VN: Ưu tiên ghép component nhỏ. Kế thừa tối đa 2 tầng — `PopupBase → PopupTemplate` đã kịch trần. `UIManager` ~300 dòng là mức trần về độ lớn của một class.)*

### 2.5 Pragmatic SOLID

**KEEP** *(giữ)*:
- Single Responsibility — one class, one reason to change.
- Small interfaces (ISP) — when an interface exists, keep it minimal.
- Dependency inversion **at the `GIKCore` ↔ game boundary only** (§2.2).

**DROP** *(bỏ)*:
- Speculative generality. **No interface with a single implementation** — there is not one in this codebase today, and that is correct.
- No factories, DI containers, or service locators. This project has none; don't be the one who adds them.
- **Duplication is cheaper than the wrong abstraction.** Write it inline first; extract on the third occurrence (Rule of Three).

*(VN: Giữ SRP + interface nhỏ + đảo phụ thuộc ở ranh giới GIKCore↔game. Bỏ trừu tượng đón đầu — lặp 2 lần còn rẻ hơn abstraction sai, đến lần 3 mới tách.)*

---

## 3. Unity-Specific Rules

### 3.1 Explicit over magic — HARD RULES

**NEVER** *(cấm)*:
- `GameObject.Find`, `FindObjectOfType`, `FindAnyObjectByType` in runtime logic
- `SendMessage`, `BroadcastMessage`
- `Resources.Load` — this project uses Addressables (§3.4)
- String literals for Animator params, shader properties, tags, or layers scattered through logic
- Reflection in runtime gameplay code (editor tooling only)
- `?.` / `??` null-propagation on `UnityEngine.Object` types — it bypasses Unity's lifetime check. Use `if (x != null)`. *(On plain C# delegates and exceptions, `?.` is fine and already used: `onLoaded?.Invoke(...)`.)*

**ALWAYS** *(bắt buộc)*:
- `[SerializeField] private` typed references for dependencies, `_camelCase` name, with `[Header]` / `[Tooltip]` for anything a designer touches — that is how `UIManager` and `PopupBase` are wired, and it is the sanctioned alternative to comments (§4.1).
- Cached string ids in a static holder, following the existing pattern:

  ```csharp
  public class AnimationId : MonoBehaviour
  {
      public static int Alive;

      private void Awake()
      {
          Alive = Animator.StringToHash("Alive");
      }
  }
  ```

  `AnimationId` for Animator params, `ShaderPropertyId` for shader properties, `PopupId` for popup keys (§3.4). New id families get their own holder next to these — never a loose string in logic.

*(VN: Mọi liên kết phải tường minh, trace được. Chuỗi id phải cache trong holder tĩnh (`AnimationId`, `ShaderPropertyId`, `PopupId`), không rải string trong logic.)*

### 3.2 Managers, singletons & wiring — how this project really works

- Bootstrapping is the **`Initialize` prefab** in `GIKCore/Prefab/`: it carries the persistent managers. That prefab is this project's composition root.
- **`PersistentRoot` on the `Initialize` root is what keeps it alive** across scene loads, and it guards duplicates. A manager parked on a **child** of `Initialize` must not call `DontDestroyOnLoad` itself — Unity ignores that call on a non-root object, which is why `SoundManager` and `GDPRManager` rely on `PersistentRoot` instead. A manager that lives on its **own root prefab** (`UIManager`, `UserDataManager`, `SceneLoader`, `FirebaseSDK`, …) still calls `DontDestroyOnLoad` in its own `Awake`. See `Docs/adr/002`.
- `Initialize` may exist **once** in the whole project. A second copy destroys itself, so never author a scene expecting its own private instance.
- **`UIManager` and `UserDataManager` are deliberate singletons** (`Instance` + `DontDestroyOnLoad`), each shipped as its own prefab and dropped into the splash scene. These are accepted exceptions, **not** a licence to add more (§7.3).
  - A new singleton is allowed **only** if it is a process-wide service that must outlive scene loads (ads, analytics, consent, time, save data), it is placed in the splash scene as its own prefab, and it guards re-entry the way `UIManager.Awake` does (`Destroy(gameObject)` on a duplicate, `Instance = null` in `OnDestroy`).
  - **Gameplay code gets no singletons.** Use serialized references.
- Everything else is wired **in code** through `[SerializeField]` references — not through Inspector `UnityEvent`s.
- Inspector `UnityEvent`s are allowed **only** for trivial UI, and even then the code path is preferred: `PopupTemplate` hooks its own close button in `OnShow()` (`_closeButton.onClick.AddListener(Close)`) and unhooks in `OnClose()`. Copy that, don't wire it in the Inspector.

*(VN: `Initialize` prefab là composition root. `UIManager` là singleton có chủ đích — được phép, nhưng KHÔNG được đẻ thêm singleton cho code gameplay. Nối phụ thuộc bằng `[SerializeField]` trong code, không nối logic bằng UnityEvent trong Inspector.)*

### 3.3 Events & callbacks

- The project's communication style is **direct and typed**: a C# `event Action<T>` on the owner (`PopupBase.Closed`, `GDPRManager.onLoadedFormDone`), or a callback passed into the call (`Action<GameObject> onLoaded`).
- **There is no global event bus, and you must not add one.** If ≥3 unrelated systems genuinely need to talk, raise it with the user first — don't smuggle in a static `GameEvents` class.
- Every event must have exactly one obvious owner, and the subscriber must unsubscribe. `PopupBase.Close()` shows the pattern: capture the handler, null the field, then invoke — so a handler can never fire twice.
- Because code carries **no comments** (§4.1), publishers and subscribers of a long-lived event are listed in the owning folder's `README.md`:

  ```markdown
  ### Events out
  - `PopupBase.Closed` — publisher: PopupBase.Close() · subscriber: UIManager.OnPopupClosed
  ```

*(VN: Giao tiếp bằng `event Action<T>` của chính object đó hoặc callback truyền vào. KHÔNG có event bus toàn cục và không được thêm. Ai bắn / ai nghe ghi trong README của folder, không ghi comment trong code.)*

### 3.4 Popups — the project's main pattern

The popup system is `UIManager` + `PopupBase` + `AddressablePrefabLoader`. `PopupTemplate` is the worked example — **read it before writing a new popup**.

**Adding a popup** *(các bước thêm popup mới)*:

1. Add the key constant to `PopupId.cs` — `public const string PopupWin = "PopupWin";`
2. Write `PopupWin.cs` inheriting `PopupBase`; override `OnShow()` / `OnClose()` only.
3. Give it static `Show` / `Hide` / `IsShowing` helpers that call `UIManager`, exactly like `PopupTemplate`.
4. Build the prefab in the game's `Prefabs/` folder, **named identically to the `PopupId` string**, with a `PopupBase`-derived component on the **active root**.
5. Register the prefab in **Addressables**, address = the same string.

**Modal blocking — the blocker is SHARED, never per popup** *(blocker dùng chung, không kéo từng popup)*:

- `UIManager` **creates one blocker itself** in `Awake` under `PopupRoot` (full-stretch `Image`, `raycastTarget`, colour from `_blockerColor`). Nothing is wired by hand, and a popup prefab must **not** carry its own blocker child.
- Every time the stack changes, `ApplyBlocker` moves it to the sibling index directly **below the top-most modal popup**, so that popup still receives clicks and everything under it does not. No modal popup open → the blocker is deactivated.
- A popup declares whether it needs blocking with `[SerializeField] private bool _isModal` on `PopupBase` (default `true`). Untick it for a toast or floating notice.
- The blocker is created in code, so it must inherit `_popupParent.gameObject.layer` — a `new GameObject` starts on `Default`, which `UICamera` (culling mask = `UI` only) does not render or raycast.

**Rules** *(luật)*:
- **`PopupId` constants only** — never a raw string at a call site. `PopupTemplate` is the worked example end to end: constant in `PopupId`, prefab registered in `Default Local Group` under the address `PopupTemplate`, `Show`/`Hide`/`IsShowing` reading `PopupId.PopupTemplate`. Verified in Play mode — show, shared blocker landing at the sibling index directly below the popup, close, Addressables instance released.
- All Addressables traffic goes through `AddressablePrefabLoader`. Never call `Addressables.InstantiateAsync` directly.
- Never destroy a popup yourself. `Close()` → `Closed` → `UIManager.OnPopupClosed` → `AddressablePrefabLoader.Release`. `Destroy(popup.gameObject)` leaks the Addressables handle.
- `Awake` in a popup must call `base.Awake()` — that is what hands the instance to `UIManager.RegisterAwakenedPopup`.
- Stacking order is owned by `UIManager` (`Order` + `ApplySiblingOrder`). Do not call `SetSiblingIndex` from a popup.

*(VN: Popup mới: thêm const vào `PopupId` → kế thừa `PopupBase` → static `Show/Hide/IsShowing` như `PopupTemplate` → prefab đặt tên ĐÚNG bằng PopupId → đăng ký Addressables. Không tự `Destroy` popup, không tự gọi Addressables, không tự set sibling index.)*

### 3.5 Data

**Save data** is `UserData` + `UserDataManager` in `GIKCore/Script/Data/`:

- `UserData` is a `[Serializable]` DTO — `camelCase` fields because the field name is the JSON key (§4), plus `const int CurrentVersion` and **a parameterless constructor that carries every default**. No other logic, no properties.
- **Defaults live in that constructor, nowhere else.** `JsonUtility.FromJson` invokes it before populating fields (verified), so a field missing from an older save keeps its constructor default instead of falling back to `0`/`false`. Adding a field therefore needs no `Migrate` change — just give it a default in the constructor.
- `UserDataManager` is the singleton that parses and writes it. Its public surface is **functions, not properties**: `GetCoin()` / `SetCoin(int)`, `GetCurrentLevel()` / `SetCurrentLevel(int)`, `GetSoundOn()` / `SetSoundOn(bool)`, and so on, plus `AddCoin`, `TrySpendCoin`, `GetRawJson`, `Load`, `Save`, `ResetAll`. A new field on `UserData` gets a matching `Get`/`Set` pair — never a public property, never a public static variable.
- The `UserData` object itself is **private**. There is no way to reach it from a call site, so every write goes through a setter that can clamp (`SetCoin` floors at 0, `SetCurrentLevel` floors at 1).
- **Runtime changes stay in memory.** PlayerPrefs is written only on `OnApplicationPause(true)`, `OnApplicationFocus(false)`, `OnApplicationQuit`, or an explicit `UserDataManager.Save()`. Do not add a save-on-every-set or a periodic flush.
- One PlayerPrefs key (`GIKCore.UserData`) holds the whole JSON blob. Do not add loose `PlayerPrefs.GetInt`/`SetInt` keys alongside it — add a field to `UserData` instead.
- Bump `UserData.CurrentVersion` and extend `Migrate` only when an **existing** field changes meaning — not when adding one.

**Tuning values**:

- Config and tuning values live in **ScriptableObjects** under `Assets/<GameName>/Data/` — never hardcoded numbers inside components.
- ScriptableObjects are **read-only at runtime**. Mutable runtime state lives in components.
- `Assets/Settings/` is URP pipeline configuration — not a place for gameplay data.

*(VN: Số liệu cân bằng game nằm trong ScriptableObject ở `Assets/<TênGame>/Data/`, không hardcode. SO chỉ đọc lúc runtime.)*

### 3.6 Optional SDKs — scripting define symbols

`GDPRManager` is the reference: the real implementation sits behind `#if GOOGLE_MOBILE_ADS`, and the `#else` branch is a **working no-op stub with the same public API** that logs a warning.

Any code touching an SDK that may not be installed (Ads, Firebase, AppsFlyer, IAP) follows the same shape:

- Guard with the SDK's define symbol.
- Provide a complete `#else` stub — same members, same signatures, degraded behavior.
- **The project must compile with every SDK absent.** Verify before declaring done.

**Defines are synchronised, not typed by hand.** `GIKCore/Editor/SdkDefineSynchronizer` probes the loaded assemblies for each SDK's type on every domain reload and adds or removes the matching symbol for Android, iOS and Standalone (`FIREBASE_SDK`, `FIREBASE_ANALYTICS`, `FIREBASE_CRASHLYTICS`, `FIREBASE_REMOTE_CONFIG`, `APPSFLYER_SDK`, `FACEBOOK_SDK`). Adding one of these in Player Settings for an SDK that is not installed does nothing — it is stripped on the next reload. Manual entry in Player Settings is still the rule for **non-SDK** defines such as `DOTWEEN_ENABLED` (§0). See `GIKCore/Script/Analytics/README.md` and `Docs/adr/001-analytics-facade-and-sdk-defines.md`.

**Analytics goes through one façade.** Game code calls `GIKCore.Analytics` and never an SDK type — `Analytics.LogEvent`, `LogLevel`, `LogScreen`, `LogPurchase`, `LogAdRevenue`, `SetUserProperty`, `LogException`. Event and parameter keys come from `EventName` / `ParameterName` / `ParameterValue` / `UserPropertyName`, never a raw string (§3.1). `GIKCore` carries only keys every game needs; game-specific names live in `Assets/<GameName>/`.

**Firebase specifically** (§0): the packages are installed, but the project is **not** configured. `google-services.json` (Android) and `GoogleService-Info.plist` (iOS) are missing from `Assets/`, so `FirebaseApp.CheckAndFixDependenciesAsync` will fail at runtime. Download both from the Firebase Console before shipping. `Assets/GIKCore/Prefab/FirebaseInitializer.prefab` predates `FirebaseSDK.cs` and still has no script on it — attach `FirebaseSDK` to it.

*(VN: Code dính SDK ngoài phải bọc `#if <DEFINE>` và có nhánh `#else` stub đủ API. Project phải build được khi KHÔNG có SDK nào. Riêng Firebase: package đã cài nhưng CHƯA có `google-services.json` / `GoogleService-Info.plist` nên init sẽ fail lúc chạy; prefab `FirebaseInitializer` cũng chưa có script đi kèm.)*

### 3.7 Scene flow

- `GIKCore/Scene/SplasSence.unity` is the entry scene (build index 0). It carries `Main Camera`, `Directional Light`, `Initialize`, `UIManager`, `UserDataManager`, `SceneLoader`, `SplashController`, `FirebaseSDK`, `RemoteConfigService`, `AppsFlyerManager`, `FacebookManager`.
- `GIKCore/Scene/Home.unity` (build index 1) is deliberately **empty** — it is the scene each game builds its home screen on. `SplashController._nextScene` points at it.
- **`SceneLoader` is the only thing that calls `SceneManager.LoadSceneAsync`.** It gates on `allowSceneActivation = false` until both a minimum duration and `progress >= 0.9` are met, counts time in `Update()` (**no coroutine**, §0), and finishes from `SceneManager.sceneLoaded` so `IsLoading` is already false by the time the new scene's `Start()` runs. `SplashController` only owns its own `_minDisplaySeconds` and calls `SceneLoader.Load(_nextScene, _minDisplaySeconds)`. Never write a second `LoadSceneAsync` (§7 litmus test, `Docs/adr/002`).
- Every persistent service enters the game through this scene. A new game scene must **not** add its own `AudioListener`: the persistent one lives on `UIManager/UICamera`. It also needs no camera of its own for UI — `UIManager/UICamera` is `DontDestroyOnLoad` and survives every load.
- UI input uses `InputSystemUIInputModule` on `UIManager/EventSystem`. Never reintroduce `StandaloneInputModule` — Player Settings runs Input System only, and the legacy module throws every frame.

*(VN: `SplasSence` là scene khởi động duy nhất, chứa toàn bộ service sống xuyên scene. Scene game mới KHÔNG được thêm `AudioListener` — cái duy nhất nằm ở `UIManager/UICamera`. Input module phải là `InputSystemUIInputModule`.)*

### 3.8 Performance

- No LINQ, no allocations, no `foreach` over a `List<T>` in per-frame paths. `UIManager` uses indexed `for` loops throughout — match that.
- Pooling goes in `GIKCore/Pool/` using built-in `UnityEngine.Pool` (§7.1). Do not write a bespoke pool.
- DOTS/ECS: **not used, and not to be introduced** without a measured need and explicit user sign-off. Default is MonoBehaviour composition.

*(VN: Đường chạy mỗi frame: không LINQ, không cấp phát, dùng vòng `for` chỉ số như `UIManager`. Pool dùng `UnityEngine.Pool` đặt ở `GIKCore/Pool/`. Không tự ý đưa DOTS/ECS vào.)*

---

## 4. Code Style

- **Namespaces** — the whole shared core sits in **one flat namespace: `GIKCore`**. Folders organise files; they do **not** become sub-namespaces. Game code uses `<GameName>` / `<GameName>.<Area>`. Block-scoped `namespace X { }`.
  Every core file is already on `GIKCore` — the old `GameBase` / `GameUI` / `Inwave` / global-namespace split has been unified. **Do not reintroduce a second core namespace.**
- **Formatting is a contract, not a preference** — `.editorconfig` at the project root defines it: UTF-8, **LF** line endings, **4 spaces, never tabs**, trailing whitespace trimmed, final newline, opening brace on its own line, `System` usings first. Match it exactly; Unity YAML (`.unity`, `.prefab`, `.asset`, `.meta`) is excluded and must never be reformatted by hand.
- One public type per file; the file is named after the type.
- Naming: `_camelCase` private fields, `PascalCase` public members, `On<Event>` for handlers, `Do<X>` for the instance implementation behind a static facade (`ShowPopup` → `DoShowPopup`), `Try<X>` for bool-returning lookups.
- **Serializable DTOs are the one exception to that**: a `[Serializable]` data container aimed at `JsonUtility` (e.g. `UserData`) uses **`camelCase` public fields**, because the field name *is* the JSON key and that is Unity’s own convention. It holds fields only — no logic, no properties.
- Log with a bracketed source tag: `Debug.LogError("[UIManager] ...")`. Every failure path logs before it returns.
- **Explicit over clever**: no expression chains that hide side effects.
- **Names are the documentation** — see §4.1. A name that needs explaining is the wrong name.
- Logic that doesn't need Unity APIs goes in **plain C# classes**, with a thin MonoBehaviour wrapper ("humble object").

*(VN: Toàn bộ lõi dùng MỘT namespace phẳng `GIKCore` — folder chỉ để sắp xếp file, không đẻ ra sub-namespace. Code game dùng `<TênGame>`. Format do `.editorconfig` quy định: LF, 4 space, không tab, không trailing space, có newline cuối file. Tên hàm/biến chính là tài liệu. Log luôn có tag `[TênClass]`.)*

### 4.1 ZERO COMMENTS — HARD RULE *(cấm comment trong code)*

> **Code in this repo contains NO comments. None.** Meaning is carried by naming and structure, not by prose sitting next to the code.

**FORBIDDEN in every `.cs` file** *(cấm tuyệt đối)*:
- `//` line comments — including `// TODO`, `// FIXME`, `// HACK`, and commented-out code
- `/* */` block comments
- `/// <summary>` XML doc comments — **on every member, public API included**
- `#region` / `#endregion` banners
- Header / author / licence blocks, ASCII separators, step markers (`// 1. …`)

**Instead** *(thay bằng)*:

| Urge to write | Do this instead *(làm thế này)* |
|---|---|
| `// calculate score for combo` | Extract a method: `CalculateComboScore()` |
| `// 0.35f = drag coefficient` | Name it: `const float DragCoefficient = 0.35f;` or a ScriptableObject field (§3.5) |
| `// TODO: handle null` | Handle it now, or file a real task — never leave it in code |
| `// this is the tricky part…` | The design is wrong: split until each step is obvious (§2.4) |
| `/// <summary>` on a public API | Self-describing type + member names; the *why* goes in the folder `README.md` or an ADR (§5) |
| `#region Lifecycle` | The class is too big — split it (§2.4) |
| Explaining a serialized field | `[Tooltip("…")]` — an attribute, not a comment, and it shows up in the Inspector |
| Commented-out code | Delete it; git history is the archive |

**Applies to generated code too**: if a template, a tool, Unity MCP, or another agent emits comments, **strip them before the diff is done**. A diff that still contains a comment is not done. *(VN: Code do BẤT KỲ agent/tool nào sinh ra cũng phải sạch comment trước khi coi là xong.)*

**Only exceptions** *(ngoại lệ duy nhất)*:
- **Attributes are not comments** and are encouraged: `[Header]`, `[Tooltip]`, `[SerializeField]`, `[RequireComponent]`, `[DisallowMultipleComponent]`, `[DefaultExecutionOrder]`. `UIManager` and `PopupBase` document their Inspector surface entirely this way.
- **Compiler and analyzer directives are not comments**: `#if UNITY_EDITOR`, `#if GOOGLE_MOBILE_ADS`, `#pragma warning disable`, `[SuppressMessage]`.
- **Third-party / generated files this repo does not own**: `Assets/TextMesh Pro/`, `Assets/Demigiant/`, `Assets/Sirenix/`, `Assets/Spine/`, `Assets/Spine Examples/`, `Assets/ShinyEffectForUGUI/`, `Assets/Plugins/Android/`, `Assets/GeneratedLocalRepo/`, `Assets/Resources/DOTweenSettings.asset`, `Assets/InputSystem_Actions.*`, and everything under `FirebaseLib/`, `Packages/` and `Library/`. Leave them exactly as they are — they arrive full of comments and that is fine.

**Known debt**: `GIKCore/Script/GDPRManager.cs` and `GIKCore/Script/UIManager.cs` still carry pre-rule comments and `#region` banners. Clean a file when a task already takes you into it; do not open a repo-wide comment purge without asking.

*(VN: Trong code KHÔNG có comment — không `//`, không `/* */`, không `/// <summary>`, không `#region`, không code bị comment lại. Muốn giải thích: đặt tên rõ, tách hàm nhỏ, dùng `[Tooltip]` cho field; lý do "tại sao" viết trong README hoặc ADR. `GDPRManager.cs` và `UIManager.cs` còn comment cũ — dọn khi có việc động vào file đó, đừng dọn cả repo khi chưa hỏi.)*

---

## 5. AI Workflow — EVERY TASK

1. **Orient** — Read this file + the target folder + the nearest similar file **before** writing code. For a popup, that means `PopupTemplate.cs`. For a manager, `UIManager.cs`. Introducing a new pattern requires flagging it to the user first.
2. **Plan** — For changes touching >2 files: state a short plan first (files affected, public API changes, how it will be verified).
3. **Implement** — Minimal diff. Do NOT reformat or refactor unrelated code. New files go in the folder §2.2 dictates.
4. **Verify** —
   - Must compile with **zero new warnings**, and must still compile with every optional SDK absent (§3.6).
   - Check the Editor console after the change — via MCP for Unity `read_console`, or by asking the user to look.
   - There is no test assembly yet. If a change deserves one, create `Assets/Tests/` with its own asmdef (this is the sanctioned asmdef exception, §2.3) and say so; otherwise state plainly how you verified.
5. **Document** —
   - Architecture-affecting change → 3-line ADR in `Docs/adr/NNN-title.md` (context / decision / consequence).
   - New top-level folder → 5-line `README.md` inside it (purpose, entry points, events in/out).
   - The *why* of a decision lives in the ADR / README — **never in a code comment** (§4.1).
6. **Report** — State what you changed, what you verified, and what you did not.

*(VN: Mỗi task: Đọc file mẫu gần nhất trước → Lập kế hoạch nếu >2 file → Sửa tối thiểu → Tự kiểm chứng (compile sạch, console sạch, build được khi thiếu SDK) → Cập nhật tài liệu → Báo cáo trung thực cả phần chưa làm.)*

---

## 6. Anti-Pattern Blacklist — reject on sight

| Anti-pattern | Use instead *(thay bằng)* |
|---|---|
| **Any comment in code** (`//`, `/* */`, `///`, `#region`) | Self-explaining names + extracted methods + `[Tooltip]` (§4.1) |
| `// TODO` / `// HACK` left in a diff | Fix it now or file a real task (§4.1) |
| Commented-out / dead code | Delete it — git history is the archive (§4.1) |
| A new singleton for gameplay | Serialized references; singletons only for `Initialize`-owned services (§3.2) |
| Service locator / static mutable state | Serialized references, init injection |
| God manager | Per-feature components + typed events (§3.3) |
| Global event bus (`GameEvents`-style static) | `event Action<T>` on the owner (§3.3) |
| Inheritance >2 deep | Composition (§2.4) |
| Magic strings (`Find`, animator params, popup keys) | Id holders: `AnimationId`, `ShaderPropertyId`, `PopupId` (§3.1, §3.4) |
| `Addressables.InstantiateAsync` at a call site | `AddressablePrefabLoader` (§3.4) |
| `Destroy(popup.gameObject)` | `popup.Close()` (§3.4) |
| `Resources.Load` | Addressables (§3.4) |
| Interface with one impl, "for the future" | Concrete class until Rule of Three (§2.5) |
| Adding an `.asmdef` unprompted | Ask first (§2.3) |
| Game code inside `GIKCore/` | `Assets/<GameName>/` (§2.2) |
| Logic wired only in Inspector | Wiring in code (§3.2) |
| `Update()` polling for rare events | Events / callbacks |
| Hardcoded tuning numbers | ScriptableObjects (§3.5) |
| LINQ / allocations in per-frame code | Indexed `for`, cached collections (§3.8) |

---

## 7. Design Pattern Policy

**Litmus test** *(phép thử duy nhất)*:

> **"From the call site, can an agent reach the implementation in ≤1 jump?"**
> *(VN: Đứng tại chỗ gọi, AI lần ra implementation trong ≤1 bước nhảy không?)*

Name patterns explicitly in types (`DashState`, `SpawnCommand`, `EnemyPool`) so agents recognize the structure instantly. *(VN: Đặt tên class theo pattern để AI nhận ra cấu trúc ngay.)*

### 7.1 Encouraged — use freely *(dùng thoải mái)*

| Pattern | Notes |
|---|---|
| Static Facade | `UIManager.ShowPopup` over `Instance.DoShowPopup` — the project's house style |
| Callback / continuation | `Action<T> onLoaded`, with a `TaskCompletionSource` wrapper when a caller wants `await` |
| Template Method — **`PopupBase` only** | `OnShow()` / `OnClose()` hooks, exactly one level deep. Sanctioned here and nowhere else (§7.3) |
| State / FSM | Replaces boolean soup; all states in one folder |
| Command | Input, undo, replay — explicit data |
| Strategy | Delegate / `Func<>` first; a small interface only if genuinely needed |
| Object Pool | Built-in `UnityEngine.Pool`, placed in `GIKCore/Pool/` |
| Composition Root | The `Initialize` prefab (§3.2) |
| Humble Object | Thin MonoBehaviour over testable plain C# (§4) |

### 7.2 Conditional — allowed with constraints *(có điều kiện)*

| Pattern | Constraint *(điều kiện)* |
|---|---|
| Observer | Typed `event Action<T>` owned by one class; pub/sub listed in the folder README (§3.3) |
| Singleton | **Only** an `Initialize`-owned, `DontDestroyOnLoad` process service with duplicate guarding, like `UIManager` (§3.2) |
| Factory | Only when ≥2 real product types exist today — Rule of Three applies |
| Adapter | Boundaries only: wrapping an SDK behind a define symbol (§3.6) |
| Mediator | Keep thin; if it grows into a God object, the split is wrong |

### 7.3 Forbidden — reject on sight *(cấm)*

| Pattern | Why / use instead *(thay bằng)* |
|---|---|
| New singletons | Hide dependencies; only the §3.2 exception stands |
| Service Locator | Dependencies invisible in signatures → serialized refs / init injection |
| Template Method beyond `PopupBase` | Flow buried in a base class → composition + delegates |
| Visitor | Logic scattered via double dispatch → C# pattern matching (`switch` expressions) |
| Chain of Responsibility | Runtime-assembled chain, statically untraceable → explicit ordered calls |
| Stacked Decorators | Runtime composition invisible to static reading → max 1 layer, or flatten |
| Abstract Factory | Two abstraction layers for a problem a `switch` solves |
| Global event bus | §3.3 |

---

## 8. Definition of Done

- [ ] Compiles, zero new warnings
- [ ] **Zero comments in the diff** — no `//`, `/* */`, `///`, `#region`, no commented-out code (§4.1)
- [ ] Editor console clean after the change (§5, step 4)
- [ ] Still compiles with optional SDKs absent (§3.6)
- [ ] `GIKCore` free of game-specific code; dependency direction intact (§2.2)
- [ ] No new `.asmdef` unless the user approved it (§2.3)
- [ ] No blacklist violations (§6) and no forbidden patterns (§7.3)
- [ ] ADR / folder README updated if architecture changed
- [ ] Diff is minimal and scoped to the task

*(VN: Chưa tick đủ checklist = chưa xong. Đặc biệt: còn 1 dòng comment trong diff là chưa xong.)*

**Self-check before reporting done** *(tự kiểm tra trước khi báo xong)*:

```bash
grep -rnE "//|/\*|\*/|#region" --include="*.cs" --exclude-dir="TextMesh Pro" Assets
```

Every hit in a file you touched must be gone, minus the §4.1 exceptions.

---

## 9. Agent Enforcement — Claude, Codex, Antigravity *(áp cho mọi agent)*

This file is the **single source of truth**. Every other agent-config file in the repo is a thin loader whose only job is to send the agent back here.

| Agent | Entry file it reads | What that file contains |
|---|---|---|
| Claude Code | `CLAUDE.md` *(this file)* | The rules themselves |
| Codex (OpenAI) | `AGENTS.md` | "Read `CLAUDE.md` in full first — it is normative" + a short hard-rule digest |
| Antigravity / Gemini | `GEMINI.md`, `.antigravity/rules/coding-standards.md` | Same loader text |
| Anything else (Cursor, Copilot, …) | add a loader file with the same text | Never a second copy of the rules |

**Binding on every agent** *(bắt buộc với mọi agent)*:

1. **Read `CLAUDE.md` in full before your first edit** of any task — no matter which entry file loaded you.
2. **Never duplicate these rules** into `AGENTS.md` / `GEMINI.md`. Copies drift; loaders don't.
3. **Rule changes land in `CLAUDE.md` only.** If a loader file contradicts this one, `CLAUDE.md` wins — fix the loader.
4. **§4.1 (zero comments) applies to every agent's output**: code typed directly, code written through **Unity MCP**, code from templates, code produced by a sub-agent. Strip comments before reporting done.
5. Precedence: a **direct instruction from the user** > `CLAUDE.md` > the agent's own defaults, skills, and habits.

*(VN: CLAUDE.md là nguồn chuẩn duy nhất. AGENTS.md (Codex) và GEMINI.md / .antigravity (Antigravity) chỉ trỏ về đây, KHÔNG chép lại rule. Mọi agent phải đọc hết CLAUDE.md trước khi sửa code. Luật cấm comment §4.1 áp cho code do MỌI agent sinh ra, kể cả code viết qua Unity MCP. Thứ tự ưu tiên: lệnh trực tiếp của user > CLAUDE.md > thói quen mặc định của agent.)*
