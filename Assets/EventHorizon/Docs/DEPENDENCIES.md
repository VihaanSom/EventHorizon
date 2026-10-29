# Dependencies and versions

Verified against the installed editor (Unity 6000.4.10f1, URP 17.4.0).

| Package | Version | Reason pinned |
|---|---|---|
| `com.unity.xr.management` | 4.5.3 | XR loader lifecycle; pairs with OpenXR 1.14+ on Unity 6 |
| `com.unity.xr.openxr` | 1.14.0 | OpenXR + built-in Meta Quest feature group (no Oculus plugin; it is deprecated) |
| `com.unity.inputsystem` | 1.19.0 (kept) | Already installed; XR + fallback input |
| `com.unity.render-pipelines.universal` | 17.4.0 (kept) | Installed; all shaders target URP |
| `com.unity.ugui` | 2.0.0 (kept) | Runtime UI uses legacy `Text`, no TextMeshPro dependency |

Deliberately NOT added: Meta XR Core/Interaction SDKs (not needed for the
shipping path — the OpenXR plug-in's Meta Quest support is sufficient),
AI/cloud/analytics/multiplayer packages (forbidden by the brief), HDRP or
desktop-only features, compute-shader tooling.

## Defaults chosen

- Graphics API: project default order kept; verify Vulkan on device first,
  fall back to GLES3 only if a device test fails (see MANUAL_ACTIONS).
- MSAA 4x on the Mobile URP asset, HDR off, render scale 0.8.
- Minimum Android SDK 32, target ARM64 only, IL2CPP (all Quest 3/3S compatible).
- Stereo: Single Pass Instanced.
