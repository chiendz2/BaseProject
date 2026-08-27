# GEMINI.md — loader (Antigravity / Gemini)

> **All coding standards for this repo live in [`CLAUDE.md`](./CLAUDE.md). Read it in full before your first edit.**
> This file is only a pointer + a digest. It does **not** contain the rules and must never be turned into a second copy of them — copies drift, loaders don't. If this file and `CLAUDE.md` ever disagree, **`CLAUDE.md` wins**.
>
> *(VN: Toàn bộ rule nằm ở `CLAUDE.md` — đọc hết file đó trước khi sửa bất kỳ dòng code nào. File này chỉ trỏ đường + tóm tắt, KHÔNG chép lại rule. Mâu thuẫn thì `CLAUDE.md` thắng.)*

Applies to **every** agent working here — Antigravity / Gemini, Codex, Claude Code, Cursor, Copilot, sub-agents, and code written through **Unity MCP**.

---

## Rule #1 — ZERO COMMENTS IN CODE *(cấm comment trong code)*

`CLAUDE.md` §4.1 is the full rule. The short version:

**Every `.cs` file this repo owns contains NO comments.**

Forbidden — no exceptions for "just this one":

- `//` line comments, including `// TODO`, `// FIXME`, `// HACK`
- `/* */` block comments
- `/// <summary>` XML doc comments — **including on public APIs**
- `#region` / `#endregion` banners
- Commented-out code, header/author blocks, ASCII separators, step markers

Instead: **name things properly and extract small methods.** A comment you feel you need is a naming or design problem. To explain a serialized field use `[Tooltip("…")]` — an attribute, not a comment. The *why* of a decision goes in a folder `README.md` or an ADR, never next to the code.

**This applies to code YOU generate.** If a template, a snippet, Unity MCP, or another agent emits comments, strip them before you call the task done. *A diff containing a comment is not done.*

Real exceptions only: attributes (`[Header]`, `[Tooltip]`, `[SerializeField]`, …), compiler directives (`#if UNITY_EDITOR`, `#if GOOGLE_MOBILE_ADS`, `#pragma`), and third-party files this repo doesn't own (`Assets/TextMesh Pro/`, `Assets/InputSystem_Actions.*`, `Packages/`, `Library/`).

Known debt: `GIKCore/Script/UIManager.cs` and `GIKCore/Script/GDPRManager.cs` still carry old comments — clean them only when a task already takes you into the file.

*(VN: Trong code KHÔNG có comment: không `//`, không `/* */`, không `/// <summary>`, không `#region`, không code comment lại. Đặt tên rõ + tách hàm nhỏ + `[Tooltip]` cho field. Code do bạn sinh ra — kể cả qua Unity MCP — cũng phải sạch comment trước khi báo xong.)*

---

## Project shape you must respect

- `Assets/GIKCore/` = **shared core** carried into every game. Game-specific code never goes in it.
- Game code goes in `Assets/<GameName>/{Scripts,Prefabs,Scenes,Data,Textures}`.
- Dependency direction is one-way: game → `GIKCore`, **never** the reverse.
- **We own no `.asmdef` files** — everything we write is `Assembly-CSharp`. The ones inside `Demigiant/`, `Sirenix/`, `Spine/` are third-party. Do not add one without asking (§2.3).
- Async is **callback-first** (`Action<T>`) with `Task` wrappers. No coroutines, no `Awaitable`, no UniTask. **DOTween Pro** is installed as an asset (`Assets/Demigiant/`) to back Animation Sequencer and requires the `DOTWEEN_ENABLED` define (§0).
- Assets load through **Addressables** only, and only via `AddressablePrefabLoader`.
- **One flat namespace `GIKCore` for the whole core.** Folders organise files, never sub-namespaces (§4).
- **Formatting is fixed by `.editorconfig`**: UTF-8, LF, 4 spaces (no tabs), no trailing whitespace, final newline, brace on its own line (§4).

## Other non-negotiables (digest — `CLAUDE.md` is authoritative)

| # | Rule | Ref |
|---|---|---|
| 2 | New popup = `PopupId` const → inherit `PopupBase` → static `Show/Hide/IsShowing` → prefab named exactly as the key → register in Addressables. Copy `PopupTemplate.cs`. | §3.4 |
| 3 | Never `Destroy` a popup, never call `Addressables.InstantiateAsync` directly, never `SetSiblingIndex` from a popup | §3.4 |
| 3b | The modal blocker is **shared and created by `UIManager`**. Never add a blocker child to a popup prefab — set `_isModal` on `PopupBase` instead | §3.4 |
| 4 | No `GameObject.Find` / `FindObjectOfType` / `SendMessage` / `Resources.Load` / runtime reflection / loose magic strings | §3.1 |
| 5 | No `?.` or `??` on `UnityEngine.Object` types (fine on plain delegates) | §3.1 |
| 6 | `UIManager` + `UserDataManager` are the sanctioned singletons. **Do not add new ones** for gameplay | §3.2, §7.3 |
| 7 | No global event bus. Typed `event Action<T>` owned by one class | §3.3 |
| 8 | SDK code sits behind `#if <DEFINE>` with a working `#else` stub — project must compile with every SDK absent | §3.6 |
| 9 | Tuning numbers in ScriptableObjects under `Assets/<GameName>/Data/`, never hardcoded | §3.5 |
| 9b | Save data: `UserDataManager` exposes **functions, not properties** — `GetCoin()`/`SetCoin(int)` etc. The `UserData` object is private, and its defaults live in `UserData`’s parameterless constructor (JsonUtility calls it). Runtime stays in memory; PlayerPrefs is written only on pause/focus-loss/quit or an explicit `Save()`. No loose PlayerPrefs keys | §3.5 |
| 9c | `SplasSence` is the entry scene. A new scene must NOT add an `AudioListener` (the persistent one is on `UIManager/UICamera`), and UI input stays on `InputSystemUIInputModule` | §3.7 |
| 10 | Inheritance max 2 below `MonoBehaviour`; `PopupBase → PopupTemplate` is already at the limit | §2.4 |
| 11 | No interface with a single implementation; no factories, DI containers, service locators | §2.5 |
| 12 | No LINQ / allocations in per-frame paths — indexed `for` loops, as in `UIManager` | §3.8 |
| 13 | Minimal diff — never reformat or refactor unrelated code | §5 |
| 14 | Zero new warnings, Editor console clean, checklist ticked before "done" | §8 |

## Before you report done

```bash
grep -rnE "//|/\*|\*/|#region" --include="*.cs" --exclude-dir="TextMesh Pro" Assets
```

Every hit in a file you touched must be gone, minus the exceptions above.

Full Definition of Done: `CLAUDE.md` §8.

---

## UnitySkills

- unity-skills: Unity Editor automation via REST API
