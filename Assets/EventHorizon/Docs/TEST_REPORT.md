# Test report

## Verified in this session (observed, not assumed)

- **Project audit**: Unity 6000.4.10f1, URP 17.4.0, no XR packages, Android
  module present, IL2CPP already selected, minSdk 25, stereo multi-pass —
  all read from project files and the installed Hub layout.
- **Headless import/compile**: mirror project opened in batchmode with the
  new packages and all EventHorizon sources — result recorded below once the
  run finishes (see Validation log section).
- **Scene YAML cross-check**: every script GUID referenced by the scenes,
  prefab, and settings asset matches a generated `.meta`; every material GUID
  matches its `.mat.meta`; every shader GUID referenced by a material matches
  its shader `.meta`. Checked by inspection of the authored files.

## Validation log (observed)

- **C# compile — PASS (two independent routes).**
  1. Live Editor toolchain: `Library/ScriptAssemblies/Assembly-CSharp.dll`
     rebuilt 30 Oct 2026 01:00:50, after the last source edit (00:57:51) —
     all 16 scripts compiled with no errors reported in the import-worker
     logs (`EH_Sky.mat` and siblings import clean, worker exit 0).
  2. Roslyn check: all 16 scripts compiled with `csc` (net10 ref assemblies
     + real Unity 6000.4 engine modules, InputSystem, URP Runtime, UGUI, XR
     Management): **exit 0, zero errors**. Only CS0649 "never assigned"
     warnings, which are expected — Unity assigns those `[SerializeField]`
     fields from the scenes (verified wired, see below).
- **Shaders — preprocessor PASS.** Unity's shader compiler preprocessed all
  five shaders with `ok=1` (`EHBlackHoleSky`, `EHStarfield`,
  `EHAccretionDisk`, `EHDust`, `EHComfortVignette`). Full GPU-variant
  compilation happens lazily/at build time — confirm zero shader errors in
  the Editor console before building.
- **Reference integrity — PASS.** Every GUID cited by both scenes, the
  prefab, the settings asset, and all materials resolves to a real `.meta`;
  both scenes + prefab contain 34/10/12 objects with zero dangling `fileID`s
  (script-checked).
- **Packages — resolved, download in flight.** `packages-lock.json` records
  XR Management 4.5.3 + OpenXR 1.14.0 (both confirmed to exist on
  packages.unity.com). Management + CoreUtils are already compiled in the
  live project; the OpenXR tarball had not finished downloading at last
  check — it completes automatically in the open Editor.
- **Batchmode mirror — abandoned with cause.** A headless import of an
  isolated copy could not proceed: the project is open in the Editor (lock),
  and the sandbox user context cannot reach the Unity licensing client, so
  the mirror run stalled in licensing retries and was terminated. Replaced by
  the two compile routes above, which check the real tree, not a copy.
- **Headless logic tests — 38/38 PASS.** The real timeline, sampling, and
  mesh-builder sources were compiled unmodified with engine-neutral stubs
  and executed: 300 s total, all phase boundaries (45/120/180/240 s),
  determinism, finiteness, play/pause/resume/restart/skip/multiplier state
  machine, annulus (4525 verts / 25920 indices, UVs in range), starfield
  (10400/15600, seed-deterministic), sky dome (1225/6912, radius exact),
  black-hole radius global. This harness caught and fixed one real
  portability bug: mesh builders used array overloads of `SetVertices`/`SetUVs`;
  all builders now use `List<>`, which every Unity version supports.
- **DrawMeshInstanced crash — fixed, regression test added.**
  Play Mode reported `InvalidOperationException: Material needs to enable
  instancing` from `EHDustController.Update`. Root cause: the dust/streak
  materials never opted into GPU instancing. Fix: `Awake` now sets
  `enableInstancing` on both materials, and both `.mat` assets carry the
  flag. `Assets/Tests/EditMode/EHDustInstancingTests.cs` covers both halves;
  authored but not executed here (no runner access) — run it in the Test
  Runner's EditMode tab.
- **Play Mode in the Editor — NOT run from this session (blocked).**
  Real-engine Play Mode needs the Unity Editor itself: the project is locked
  by the open Editor, headless licensing fails under this sandbox user, and
  there is no display, rendered-frame capture, or headset here. To complete
  it, open `EH_Boot`, press Play, and walk the checklist: start prompt →
  Space starts → phases I–V log at 45/120/180/240 s → P pauses/resumes →
  C toggles comfort vignette → R restarts → end card at 300 s → Esc exits;
  confirm silhouette, disk, lensing toggle, dust ramp, audio swell, and a
  clean console. Device build, thermals, and 72 Hz sign-off remain gated on
  a Quest 3/3S (see PERFORMANCE.md / BUILD_AND_INSTALL.md).

## NOT yet verified (explicitly untested assumptions)

- No APK has been built; no headset was connected to this session.
- XR launch, stereo correctness, head tracking, pause/resume on focus loss,
  300 s timeline run, thermals, and offline operation are all **device-gated**
  and must be checked per PERFORMANCE.md / BUILD_AND_INSTALL.md.
- Shader look (silhouette size, disk tilt, star density) is reasoned, not
  eyeballed — first device run must confirm composition and comfort.

## How to re-verify

1. Delete `VerifyMirror/` (scratch copy, not part of the deliverable).
2. Open the real project in the Editor; confirm zero console errors.
3. Play `EH_Boot` in Editor: prompt appears, Space starts, timeline phases
   log I→V, end card shows, restart works.
4. `EHSceneValidator` warnings must be zero with all references wired.
