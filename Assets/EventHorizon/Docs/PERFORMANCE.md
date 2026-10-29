# Performance plan and budget

Target: **72 Hz (13.9 ms)** first; assess 90 Hz only after profiling.

## Budget reasoning (engineering targets, not measurements)

- Draw calls in `EH_Experience`: sky dome, starfield, disk, ≤2 instanced dust
  draws, comfort quad (only in comfort mode), UI canvas — under 10 total.
- Triangles: dome ~4.6k, stars 2600 quads = 5.2k tris… (2600×2), disk
  24×180×2 ≈ 8.6k, dust ≤512 quads. Total well under 30k tris.
- No shadow maps (no lights), no post-processing, no fullscreen passes, no
  screen-space texture sampling, no per-frame allocations in steady state
  (preallocated pools; materials cached by property ID).
- Biggest risks: 4x MSAA at render scale on high-DPI Quest 3 panels, and
  additive star overdraw when looking along the disk plane. Mitigations are
  ordered in the brief (kill fullscreen passes → overdraw → shader cost →
  particle density → MSAA/scale → refresh rate).

## Profile matrix (on device, per scene)

1. Starfield only (disable disk/dust GameObjects).
2. Starfield + disk.
3. Silhouette + lensing sweep (vary `lensingStrength` 0→1.7).
4. Full particles (Act III–IV region via developer skip-to-phase).
5. Full 300 s run, thermal + memory (Meta Quest Developer Hub / OVR Metrics).

Record: CPU/GPU frame times, app GPU %, temperature, memory. Do not accept
Editor Game-view numbers.

## Quality settings

- **Quest 3 Target**: Mobile URP asset — 4x MSAA, HDR on, render scale 1.0
  (native Quest 3 panel resolution through pancake optics), dithering on
  (eliminates 8-bit color banding in deep space), Bloom (threshold 1.0,
  scatter 0.6) + Neutral tonemapping for HDR relativistic emission.
- **Quest 3S / Thermal Fallback**: 2x MSAA + render scale 0.85 if thermal
  throttling is detected under sustained high ambient temperatures.
