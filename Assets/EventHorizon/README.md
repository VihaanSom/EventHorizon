# EVENT HORIZON — Autonomous Unity VR Implementation

A five-minute, first-person VR descent into a Schwarzschild black hole.
Unity 6000.4.10f1 · URP 17.4.0 · OpenXR · Meta Quest 3/3S · Android.

## Audit (Phase 0)

| Item | Finding |
|---|---|
| Unity editor | 6000.4.10f1 (installed, Hub-managed) |
| Render pipeline | URP 17.4.0, `Mobile_RPAsset` active |
| Packages (before) | No XR packages; Input System 1.19.0, Timeline, UGUI present |
| Scenes (before) | Only `Assets/Scenes/SampleScene.unity` in build |
| XR Plug-in Management | Not installed or configured |
| Android module | Installed (`AndroidPlayer` playback engine present) |
| Scripting backend (Android) | IL2CPP already |
| Min SDK (before) | 25 → raised to 32 for Quest 3/3S |
| Stereo path (before) | Multi-pass (0) → set to Single Pass Instanced (2) |
| Git | Repo exists (`.git`); sandbox user mapping blocks CLI use here |

Compatibility risks: UPM must download XR packages (needs network + Hub license
on first import); XR Management settings assets can only be finished in the
Editor UI; device validation needs a headset + USB/ADB.

## What was implemented

- `Assets/EventHorizon/Scenes/` — `EH_Boot` (init + async load) and
  `EH_Experience` (full rig), both registered in Build Settings.
- `Scripts/` — 16 small components: deterministic 5-phase timeline
  (45/75/60/60/60 s), director (sole writer per property set), black-hole /
  disk / lensing / starfield / dust / procedural audio / comfort / input /
  runtime UI / validator / boot loader.
- `Shaders/` — 5 URP shaders, no compute, no screen-space blits, no HDR-only
  or desktop-only features. Lensing is an analytic ray-direction bend, shared
  by the sky dome and the star vertices so both eyes agree.
- `Materials/`, `Settings/EHExperienceSettings_Default.asset`,
  `Prefabs/EHBlackHoleRig.prefab`.
- Project edits: `Packages/manifest.json` (+ XR Management 4.5.3, OpenXR
  1.14.0), Android app id `com.eventhorizon.xr`, minSdk 32, ARM64, Single Pass
  Instanced, Mobile URP asset → 4x MSAA, HDR off. No full Meta XR SDK (the
  OpenXR plug-in's Meta Quest feature group covers the shipping path).

## Quick start (Editor)

1. Open the project in Unity 6000.4.10f1; let the Package Manager resolve.
2. Finish XR setup in the Editor (one-time, see
   [MANUAL_ACTIONS](Docs/MANUAL_ACTIONS.md) — OpenXR + Meta Quest feature,
   Android target, quality check).
3. Open `Assets/EventHorizon/Scenes/EH_Boot.unity`, press Play.
4. Space / tap = start, P = pause, C = comfort, R = restart.

Full details: [Docs/BUILD_AND_INSTALL](Docs/BUILD_AND_INSTALL.md) ·
[TEST_REPORT](Docs/TEST_REPORT.md) · [PERFORMANCE](Docs/PERFORMANCE.md) ·
[KNOWN_LIMITATIONS](Docs/KNOWN_LIMITATIONS.md) ·
[DEPENDENCIES](Docs/DEPENDENCIES.md).
