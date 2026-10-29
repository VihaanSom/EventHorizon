# Manual actions remaining (Editor UI required)

These cannot be performed through files alone; each takes ~1 minute.

1. **Resolve packages** — open the project in Unity 6000.4.10f1 with network
   access; Package Manager installs XR Management 4.5.3 + OpenXR 1.14.0 from
   `Packages/manifest.json`. If resolve fails, install via
   Window → Package Manager → + → "Add package by name":
   `com.unity.xr.management` (4.5.3), `com.unity.xr.openxr` (1.14.0).
2. **Enable the XR loader** — Edit → Project Settings → XR Plug-in Management:
   check **OpenXR** (and assign the Mobile/Quest loader) for the **Android**
   tab. Desktop tab optional (Link testing).
3. **Enable the Meta Quest feature** — same window → OpenXR → Android →
   check **Meta Quest Support** feature group; add the
   *Meta Quest Touch Pro/Plus* interaction profile if controllers are used.
4. **Android target** — File → Build Settings → switch to **Android**;
   confirm Texture Compression ASTC, ARM64, IL2CPP, min SDK 32.
5. **Graphics API check** — Project Settings → Player → Android → Other
   Settings: confirm **Vulkan** is first; keep OpenGLES3 second as fallback
   (remove only if Vulkan is proven on-device).
6. **Quality sanity** — quality level running on device should use the Mobile
   URP asset (4x MSAA, HDR off). Keep a debug quality copy at 2x MSAA /
   render scale 0.8 for thermal comparison.
7. **XR rig (optional)** — the scene ships a plain tracked camera, which is
   enough for head-tracked playback. For controller poses, add an XROrigin
   (requires XR Interaction Toolkit, NOT installed by default) and parent the
   existing camera under it; nothing in code assumes a specific rig.
8. **Device build** — connect Quest 3/3S via USB, enable developer mode,
   Build And Run; accept the install prompt. See BUILD_AND_INSTALL.md.
9. **Tune after first device run** — lensing strength, disk emission, and
   72 Hz vs 90 Hz per PERFORMANCE.md; never judge from the Editor Game view.
