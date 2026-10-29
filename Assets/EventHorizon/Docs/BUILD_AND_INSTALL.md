# Build and install (Quest 3 / 3S)

1. Complete MANUAL_ACTIONS.md steps 1–7 in Unity 6000.4.10f1.
2. File → Build Settings → Android → Scenes: `EH_Boot` (first),
   `EH_Experience`. Package name `com.eventhorizon.xr` is pre-set.
3. Development Build ON for the first APK (autoconnect profiler optional).
   Install over USB (`adb install -r`) or Build And Run.
4. Acceptance: launches to prompt, both eyes render, head tracking stable,
   Space/start begins the 300 s timeline, pause/resume/restart/exit work,
   no internet required, no keys bundled.
5. Only after the Development Build passes: Release build for thermal and
   frame-time sign-off.

Headset prerequisites: developer mode + USB debugging on the Quest,
SideQuest/Meta Quest Developer Hub or `adb` on the workstation, Quest 3/3S
charged and on the same tracking space. No companion app or account beyond
Meta's standard developer-mode onboarding is needed by this application.
