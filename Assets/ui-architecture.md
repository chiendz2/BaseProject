# Kiến trúc UI — CatMe

Đọc khi thêm màn hình, popup, HUD, khi chạm Canvas, hoặc khi tối ưu UI.

> **Quy mô đã chốt: S (nhỏ).** Hyper casual, 3 scene, dự kiến ~6 màn hình kể cả popup, 1 người code.
> Mọi quyết định dưới đây bám vào con số đó. Khi số màn hình vượt **8** hoặc có list > 200 item,
> đọc lại file này và cân nhắc nâng cấp — đừng nâng cấp trước.

## 1. Chốt công nghệ

| Hạng mục | Chọn | Vì sao |
|---|---|---|
| Hệ UI | **uGUI + TextMeshPro** | Cần mask (`ShotAreaMask` khoét lỗ bằng 4 tấm), `RenderTexture` cho ảnh chụp, và world-space UI về sau. UI Toolkit yếu cả ba. Unity vẫn ghi uGUI là lựa chọn khuyến nghị cho runtime |
| Pattern | **View / Logic tách sẵn theo feature** | Xem §4. Không đổi tên class thành `*View`/`*Presenter` — cấu trúc đã đúng tinh thần |
| DI container | **Không** | 1 người code, `GameBootstrap` đã giải xong bài toán nối phụ thuộc |
| Addressables | **Không** | Ngưỡng là > 15 screen prefab; CatMe sẽ có ~6 |
| Reactive (R3/UniRx) | **Không** | Không có luồng state nào chảy qua nhiều view |
| Async | **`Awaitable`** | Đã chốt ở `docs/decisions.md` (2026-08-17) |
| Tween | **PrimeTween** | Đã có sẵn trong `manifest.json`. Thay chỗ của DOTween khi port UI từ AntFlow; `.SetUpdate(true)` → `useUnscaledTime: true` |
| Screen Stack | **Chưa** | Xem §3 — hyper casual mỗi lúc chỉ mở một popup. Hàng đợi thì CÓ: popup thứ hai chờ tới lượt chứ không mở đè |

## 2. Ba scene, một object bền

```
Scene_Spash     logo + khởi tạo + tạo UIRoot  →  yêu cầu load Lobby
Scene_Lobby     nội dung chốt sau             →  yêu cầu load Gameplay
Scene_Gameplay  HUD chơi (đã có)              →  yêu cầu load Lobby khi thoát màn
```

> **ĐÃ DỰNG (2026-08-19).** Xem `UIRoot` trong `Scene_Spash` và `Core/UI/`.

**`UIRoot`** là GameObject **duy nhất** sống qua chuyển scene (`DontDestroyOnLoad`). Nó chứa:

| Con | Class | Việc |
|---|---|---|
| `ScreenFader` | `Core/UI/ScreenFader.cs` | Tấm đen che khoảng chuyển scene. `sortingOrder = 30` — trên cùng |
| `LoadingOverlay` | `Core/UI/LoadingOverlay.cs` | Tấm chờ có spinner và timeout tự ẩn. `sortingOrder = 10` |
| `NoInternetPopup` | `Core/UI/MessagePopup.cs` | Che màn khi mất mạng, do `NoInternetWatcher` điều khiển. `sortingOrder = 20` |

Canvas cha (`Canvas_Persistent`) để `sortingOrder = 100` để luôn nằm trên mọi UI của scene.

`SceneLoader` cũng nằm trên `UIRoot` — nó **phải** ở đó, vì object chạy fade bắt buộc sống qua lần
load. Hệ quả phải biết: **bấm Play thẳng vào `Scene_Lobby` hay `Scene_Gameplay` thì không có
`SceneLoader` nào**, mọi nút chuyển scene im lặng không làm gì. `SceneLoadChannel.Request` vì thế
`LogWarning` khi không có subscriber. Lúc dev, chọn `Scene_Spash` ở nút PlayFromScene trên toolbar.

**Thanh loading của màn Splash** (`LoadingCanvas`) nằm trong chính `Scene_Spash`, KHÔNG phải trên
`UIRoot` — nó chết cùng scene khi sang Lobby, đúng ý. `SplashScreen` chạy nó hai pha: 0 → 0.9 theo
nhịp chậm dần trong lúc chưa biết còn chờ bao lâu, rồi 0.9 → 1.0 bám `SceneLoader.Progressed`.

Để đoạn cuối đó nhìn thấy được, `SceneLoader` giữ scene mới lại bằng `allowSceneActivation = false`
cho tới khi đã báo 100%, và **bỏ qua fade ở lần load ĐẦU TIÊN** (`_skipFadeOnFirstLoad`) — màn
Splash đã che kín sẵn, fade lúc đó chỉ đè mất thanh.

Ba overlay trên `UIRoot` **không** đi qua `PopupHost` (§3.4): host thì mỗi scene một cái, còn chúng
sống xuyên scene. Chúng gọi thẳng `OpenAsync`/`CloseAsync`.

**`UIRoot` đặt trong `Scene_Spash` và chỉ ở đó.** Splash là scene đầu, không bao giờ quay lại, nên không có nguy cơ tạo trùng — không cần một dòng code chống trùng nào.

### Chuyển scene: publish, không gọi

**Không** một GameObject nào được giữ reference tới `UIRoot`, và `UIRoot` cũng không lộ ra `.Instance`. Thay vào đó `Core/Events/SceneLoadChannel.cs`:

```csharp
/// <summary>Yêu cầu chuyển sang scene khác, có fade.</summary>
/// <remarks>
/// Publisher: nút Play ở Lobby, nút Thoát màn ở Gameplay, Splash khi khởi tạo xong.
/// Subscriber: SceneLoader trên UIRoot.
/// </remarks>
```

`SceneLoader` (trên `UIRoot`) nghe channel → fade out → `SceneManager.LoadSceneAsync` → fade in.

Đây là lý do **không cần thêm một ngoại lệ singleton nào**: bên gửi chỉ cầm một `ScriptableObject` asset, không cầm object bền.

## 3. Popup

> Popup đầu tiên đã dựng: `Features/Settings/` (xem `SettingsPopup.cs`, `SettingsLauncher.cs`,
> `SlideToggleVisual.cs` và `SettingsPopup.prefab`). Mục này mô tả cách nó làm, và là chuẩn cho
> popup tiếp theo.

### 3.0. Popup nằm ở đâu — KHÔNG có `Features/Popups/`

**Popup không phải một feature.** Nó là cơ chế trình bày, cùng loại với Canvas hay Button. Cái là feature là **nội dung** của popup.

**Quy tắc: popup thuộc về feature sở hữu dữ liệu nó hiển thị.**

| Popup | Đặt ở |
|---|---|
| Xem album ảnh đã chụp | `Features/PhotoCamera/UI/` — nó hiển thị `PhotoAlbum` |
| Bảng chỉnh thông số | `Features/Player/UI/` — `MovementTuningPanel` đã ở đó, và nó thực chất là một popup |
| Pause, Kết quả màn | feature mới (`GameFlow`…) — chưa feature nào sở hữu khái niệm "ván chơi" |
| Settings | **`Features/Settings/`** — nó tự sở hữu ba tuỳ chọn Nhạc/Âm thanh/Rung, và Rate/Restore/Quit/version là API platform. Không đụng feature gameplay nào nên không cần tầng cắt ngang |

**Vì sao không gom thành `Features/Popups/`:**

1. Phá vertical slice — `AlbumPopup` sẽ nằm xa `PhotoAlbum` dù cả đời chỉ làm việc với đúng class đó.
2. **Không compile được.** Assembly đó sẽ phải ref **mọi** feature (album → `PhotoCamera`, kết quả màn → `GameFlow`…), mà §5 cấm feature ref feature. Đúng lý do này đã đẩy `GameBootstrap` ra một assembly riêng ở **tầng trên** — popup không có lý do gì để ở tầng đó.

**Hạ tầng chung (`PopupBase`, `PopupHost`) — Rule of Three ĐÃ KÍCH HOẠT (2026-08-19).**

Đếm: `SettingsPopup` (#1) → `MovementTuningPanel` (#2) → `PausePopup`, `ConfirmPopup`,
`MessagePopup`, `LoadingOverlay`. Vượt ngưỡng, nên `Core/UI/` đã dựng — xem `Core/UI/README.md`.

Phải là `Core/` chứ không phải `Bootstrap/`: feature cần **gọi** `PopupHost.Open()`, mà chiều tham chiếu là `Bootstrap → Features → Core`. Đặt ở `Bootstrap/` thì không feature nào với tới được.

⚠️ **`Features/Settings/` CỐ Ý không đổi sang `PopupBase`.** Nó đang chạy, có test, và đổi là một
diff lớn không truy được về yêu cầu nào. Nó vì thế còn giữ bản sao của `Fade`. Khi nào phải sửa
`SettingsPopup` vì lý do khác thì gộp luôn, đừng làm một commit chỉ để gộp.

`PopupHost` **không** giữ danh sách popup của scene — mỗi nơi mở popup tự cầm cái của mình
(`PauseButton` cầm prefab `PausePopup`, `LobbyScreen` cầm `ConfirmPopup`), nên không cần `Bind`
danh sách. Cái `GameBootstrap` phải nối là thứ đi **chéo feature**: `PausePopup.SettingsRequested`
sang `SettingsLauncher.Open()`.

### 3.1. Ai mở popup

Ba luồng, và **không luồng nào** để gameplay giữ reference tới popup:

| Nguồn | Đường đi | Ví dụ |
|---|---|---|
| Người chơi bấm nút HUD | Nút → `PopupHost.Open(popup)`. Cả hai cùng nằm trong Canvas của scene | Nút Pause |
| Gameplay xảy ra chuyện | `Runtime/` bắn C# event → một UI class nghe → `PopupHost.Open(...)` | Hết giờ → popup kết quả |
| Cross-feature | ScriptableObject channel trong `Core/Events/` | Xem `docs/architecture.md` §1 |

Luồng 2 đúng bằng cơ chế `InteractionDetector.TargetChanged` → `InteractionHud` đang chạy. Không phát minh gì mới.

### 3.2. Ẩn/hiện bằng gì

Mỗi popup là một **prefab** (không dựng sẵn trong scene), nạp lên bằng `Instantiate` ở lần mở đầu tiên. Root prefab mang ba component:

```
PausePopup
├── Canvas            overrideSorting = true, sortingOrder = 10  ← nằm trên HUD
├── GraphicRaycaster  bắt buộc: raycaster của canvas cha KHÔNG duyệt canvas lồng
└── CanvasGroup       để fade và chặn chạm
```

| Trạng thái | Làm gì |
|---|---|
| Ẩn hoàn toàn | **Tắt component `Canvas`** — không vẽ, không raycast, **giữ nguyên mesh cache** nên bật lại gần như free |
| Đang fade | Bật `Canvas`, chạy `CanvasGroup.alpha` 0 → 1 |
| Mở, nhận chạm | `alpha = 1`, `blocksRaycasts = true` |

`PopupBase` tự lấy ba component này bằng `GetComponent` ở `Awake` — **không có ô kéo thả nào** cho chúng. Chúng bắt buộc nằm trên chính root: `Canvas` ở đây thì `overrideSorting` mới quyết được thứ tự vẽ, `CanvasGroup` ở đây thì fade mới phủ cả popup. `PopupAnimator` vẫn tuỳ chọn — không gắn thì popup chỉ mờ dần.

**Chỗ đặt popup nằm ở `PopupHost.PopupParent`**, một ô cho cả scene. Launcher đọc từ host chứ không tự giữ `_popupParent` riêng.

**Không dùng `SetActive(false)`** để ẩn — nó huỷ mesh cache, và mỗi lần bật lại phải dựng lại toàn bộ vertex. Đây là luật ở `CLAUDE.md` §6.

**Vòng đời:** một launcher trong scene giữ reference tới prefab, `Instantiate` ở **lần mở đầu tiên** rồi giữ lại; các lần sau chỉ bật lại `Canvas`. Prefab **không giữ được reference nào trỏ ra scene** (Unity không lưu), nên mọi phụ thuộc đi qua `Bind(...)` — cùng cơ chế `GameBootstrap` dùng, xem `docs/architecture.md` §2.

**Popup dùng chung nhiều scene:** chia sẻ bằng **prefab**, không bằng instance sống qua scene. `SettingsPopup.prefab` dùng cho cả Lobby lẫn Gameplay; mỗi scene một launcher riêng, khác biệt truyền vào lúc `Bind` (Gameplay `_allowRestart = true`, Lobby `false`). Không cần `DontDestroyOnLoad`, không phải quản lý vòng đời chéo scene.

⚠️ Ba overlay trên `UIRoot` (`ScreenFader`, `LoadingOverlay`, popup mất mạng) là **ngoại lệ**:
chúng dựng sẵn trong `Scene_Spash` chứ không nạp prefab, vì chỉ có đúng một cái cho cả game.

Popup có `Canvas` riêng còn vì lý do ở §5: lúc fade nó dirty mỗi frame, cô lập ra thì không kéo theo 35 graphic tĩnh của HUD.

⚠️ `overrideSorting = true` ở đây là **ngoại lệ có chủ ý** so với §5 (nơi để `false`): popup phải vẽ đè lên HUD bất kể vị trí trong hierarchy. Bật `overrideSorting` thì **bắt buộc** có `GraphicRaycaster` riêng, nếu không popup hiện lên mà bấm không được.

### 3.3. Bẫy `timeScale` — chỗ dễ sai nhất

Popup pause thường đi kèm `Time.timeScale = 0f`. Khi đó:

- `Time.deltaTime` **bằng 0** → animation fade viết bằng `deltaTime` sẽ **đứng im vĩnh viễn**, popup không bao giờ hiện xong.
- `Awaitable.WaitForSecondsAsync()` chạy theo **scaled time** → treo mãi.

Nên animation của popup **phải** dùng `Time.unscaledDeltaTime` và `Awaitable.NextFrameAsync()` (frame vẫn tick khi `timeScale = 0`):

```csharp
public async Awaitable Open()
{
    _canvas.enabled = true;
    _group.blocksRaycasts = true;

    float elapsed = 0f;
    while (elapsed < _fadeDuration)
    {
        await Awaitable.NextFrameAsync(destroyCancellationToken);
        elapsed += Time.unscaledDeltaTime;   // KHÔNG dùng deltaTime: pause là timeScale = 0
        _group.alpha = Mathf.Clamp01(elapsed / _fadeDuration);
    }

    _group.alpha = 1f;
}
```

**PrimeTween — cái bẫy `Sequence.Create(tween)`:** cờ `useUnscaledTime` của Sequence CHA đè lên mọi
tween con, và overload `Sequence.Create(tween)` tạo Sequence với giá trị **mặc định (`false`)**. Truyền
`useUnscaledTime: true` cho tween con trong overload đó là vô nghĩa — PrimeTween log một dòng
"'useUnscaledTime' was ignored after adding child animation" rồi chạy theo scaled time, và popup pause
sẽ **treo vĩnh viễn ở animation đóng**. Luôn dựng Sequence rỗng trước:

```csharp
Sequence sequence = Sequence.Create(useUnscaledTime: true);   // cờ phải nằm ở ĐÂY
sequence.Group(Tween.Scale(..., useUnscaledTime: true));
```

Đặt `timeScale = 0` **sau** khi popup hiện xong, và trả về `1f` **trước** khi bắt đầu fade đóng — để chính animation không bị đóng băng. Việc đổi `timeScale` là của `PopupHost`, không phải của popup.

### 3.4. `PopupHost` — một trường, không phải một stack

Hyper casual mỗi lúc chỉ mở một popup, nên chỉ cần biết "đang mở cái nào":

```csharp
public class PopupHost : MonoBehaviour
{
    public event Action CancelWithNoPopup;   // bấm Back mà không có gì đang mở

    public bool HasOpenPopup { get; }
    public PopupBase Current { get; }

    public async Awaitable Open(PopupBase popup);
    public async Awaitable CloseCurrent();
}
```

Logic hàng đợi nằm ở `PopupQueue<T>` — **plain C#**, không đụng Unity API, nên test được bằng
EditMode (`Core/Tests/PopupQueueTests`). `PopupHost` chỉ là vỏ Unity quanh nó cộng việc đổi
`timeScale`.

**Đóng popup phải gọi `PopupBase.RequestClose()`, không gọi thẳng `CloseAsync()`** — gọi thẳng thì
hàng đợi không nhích và `timeScale` không được trả về.

**Chưa dựng Screen Stack** (`PushAsync`/`PopAsync`/`IScreenService`). Ngưỡng hợp lý là khi thật sự cần popup **chồng** popup, hoặc khi số màn hình vượt 8 — xem §7.

### 3.5. Nút Back của Android

Input System map nút Back của Android thành `<Keyboard>/escape`. Trong `CatMe_InputActions` có sẵn `UI/Cancel`, nhưng nó bind theo **usage** (`*/{Cancel}`) và đang được `InputSystemUIInputModule` dùng cho điều hướng UI — **chưa kiểm chứng trên máy Android thật**. Lúc dựng popup, thử `UI/Cancel` trước; nếu không ăn thì thêm một action riêng bind thẳng `<Keyboard>/escape` thay vì đoán.

**ĐÃ NỐI (2026-08-19), nhưng vẫn CHƯA thử trên máy Android thật.** `PopupHost` nghe `UI/Cancel`:

- Có popup đang mở → `CloseCurrent()`.
- Không có → bắn `CancelWithNoPopup`, và scene tự quyết: Gameplay mở Pause (`PauseButton` nghe),
  Lobby hỏi thoát game (`LobbyScreen` nghe).

Đúng một `if` trong `PopupHost`, không có router. Nếu trên máy thật `UI/Cancel` không ăn thì thêm
một action riêng bind thẳng `<Keyboard>/escape` — chỉ sửa `PopupHost`, hai scene không phải đổi gì.

## 4. Pattern — giữ nguyên cái đang có

CatMe **đã** ở đúng hình dạng mà MVP hướng tới, chỉ khác tên gọi:

| Vai MVP | Ở CatMe là |
|---|---|
| Model | `PhotoZoom`, `PhotoAlbum`, `FirstPersonMotor`, `InputModeResolver` — C# thuần, test được bằng EditMode |
| View | `ViewfinderPanel`, `InteractionHud`, `PlayerActionHud` trong `UI/` |
| Presenter | `PhotoCameraController`, `InteractionDetector` trong `Runtime/` |

Luật chiều phụ thuộc và cách nối: `docs/architecture.md` §1–§2. Tóm tắt: `UI/` ref xuống `Runtime/`, ref chéo hai cây scene đi qua `GameBootstrap.Bind(...)`.

**Một điều chỉnh so với tài liệu MVP phổ biến:** quy tắc *"View không bao giờ được `using` namespace gameplay"* **không** áp dụng nguyên văn ở đây. CatMe theo vertical slice, `ViewfinderPanel` và `PhotoCameraController` cùng namespace `CatMe.Features.PhotoCamera` là **chủ ý** — để đọc một folder là hiểu một feature. Điều bị cấm là UI của feature này biết feature **khác**, và ranh giới đó `.asmdef` đã cưỡng chế sẵn.

Tương tự, *"Model không được `using UnityEngine`"* quá cứng: `PhotoZoom` dùng `Mathf.Clamp` nhưng vẫn test được bằng EditMode. Tiêu chí đúng là **test được mà không cần vào Play mode**, không phải dòng `using`.

## 5. Canvas — luật quan trọng nhất về hiệu năng

Một element đổi thì Unity rebuild mesh của **cả Canvas chứa nó**. Nên element đổi liên tục phải được cô lập khỏi đám graphic tĩnh.

Cách tách **không phải** dời object sang nhánh khác — làm thế sẽ phá `ShotAreaMask` (nó định vị các con theo tỉ lệ khung) và `SafeAreaFitter`. Cách đúng là **thêm component `Canvas` ngay trên chính object động** (nested canvas): nó tách batch và tách rebuild mà giữ nguyên vị trí trong cây.

Trạng thái `Scene_Gameplay` sau khi tách:

| Canvas | Graphic | Rebuild khi |
|---|---|---|
| `UI_Mobile` | **35** | gần như không bao giờ |
| `ZoomSlider` | 5 | zoom đổi |
| `MoveJoystick` | 2 | vuốt joystick |
| `ZoomLabel` | 1 | số zoom đổi |
| `FlashOverlay` | 1 | 0.15s lúc chụp |

⚠️ **Nested canvas cần `GraphicRaycaster` RIÊNG nếu graphic bên trong phải nhận chạm.** Raycaster của canvas cha chỉ duyệt `graphicList` của chính nó, không duyệt canvas lồng bên trong. `ZoomSlider` và `MoveJoystick` vì thế có raycaster riêng; `ZoomLabel` và `FlashOverlay` thì không cần vì đã tắt `RaycastTarget`.

Để `overrideSorting = false` để nested canvas kế thừa sorting của cha và giữ nguyên thứ tự vẽ theo hierarchy.

Đánh đổi: mỗi nested canvas là một batch riêng nên draw call tăng. Với 4 canvas nhỏ (tổng 9 graphic) thì đổi lấy việc không rebuild 35 graphic tĩnh mỗi frame là lời. **Đừng thêm nested canvas cho element đổi hiếm** — `PhotoCountLabel` (chỉ đổi lúc chụp) và `InteractPrompt` (bật/tắt) cố ý để nguyên trong canvas tĩnh.

### Checklist khi thêm UI mới

| # | Luật | Cách kiểm |
|---|---|---|
| 1 | Element đổi mỗi frame **phải** ở Canvas động | Nhìn cây Canvas |
| 2 | `label.SetText("{0}", v)`, **không** `label.text = v.ToString()` | `.text =` sinh rác GC mỗi lần gọi |
| 3 | Chỉ ghi text khi giá trị **thật sự đổi** | Giữ giá trị đang hiển thị, so trước khi ghi |
| 4 | Tắt `RaycastTarget` trên mọi Image/Text không nhận chạm | Nhãn con của nút, tấm trang trí, chỉ báo |
| 5 | Ẩn nhóm UI bằng tắt component `Canvas`, không `SetActive` cả cây | Tắt Canvas giữ mesh cache, bật lại gần như free |
| 6 | Không `Update()` trong UI trừ khi giá trị thật sự đổi liên tục | `ViewfinderPanel` được phép; nhớ luật 3 |
| 7 | Mọi `+=` có `-=` tương ứng | Đăng ký ở `Start`, huỷ ở `OnDestroy` (xem `docs/architecture.md` §2) |
| 8 | Không nối logic vào `Button.onClick` qua Inspector | `AddListener` trong code; hiện repo có **0** vi phạm, giữ nguyên con số đó |
| 9 | Không Layout Group trong UI đổi thường xuyên | Layout Group đánh dấu dirty và tính lại liên tục |
| 10 | Tránh nhiều lớp full-screen trong suốt chồng nhau | Ăn fillrate rất nặng trên Android yếu |

Nghi ngờ giật: đo bằng Profiler ở **Development Build trên máy Android thật** — không phải Editor, không phải Unity Remote — rồi xem `Canvas.SendWillRenderCanvases` và `Canvas.BuildBatch` chiếm bao nhiêu ms. Không tối ưu theo cảm giác.

## 6. Thư mục

Mỗi màn hình là một feature, đúng vertical slice:

```
Features/Lobby/
  CatMe.Features.Lobby.asmdef
  README.md
  Runtime/    logic màn (C# thuần được thì càng tốt)
  UI/         MonoBehaviour hiển thị
  Data/       ScriptableObject config
  Tests/
```

Xoá một màn = xoá một folder, không sót gì.

## 7. Những gì KHÔNG làm

1. **Không migrate sang UI Toolkit.** Mất Animator/Timeline, mất mask và shader tuỳ biến, world-space phải workaround. Chi phí lớn, lợi ích bằng 0 ở quy mô này.
2. **Không dựng Screen Stack / `IScreenService` lúc này.** `PopupQueue` chỉ nhớ "đang mở cái nào + ai đang chờ", KHÔNG cho popup chồng popup. Ngưỡng để nâng lên stack thật là khi có nhu cầu chồng thật, hoặc số màn hình vượt 8.
3. **Không thêm VContainer/Zenject.** `GameBootstrap` đã đủ, và thêm DI là thêm một khái niệm nữa cho 1 người code.
4. **Không thêm UniTask.** `Awaitable` của Unity 6 đang chạy tốt.
5. **Không đổi tên class thành `*View`/`*Presenter`/`*Model`.** Cấu trúc đã đúng; đổi tên chỉ tạo diff lớn và làm lệch toàn bộ `docs/`.
6. **Không tạo `UIManager` với `.Instance`.** Chuyển scene đi qua `SceneLoadChannel`; mở popup do chính scene đó lo.
