# Known limitations and future work

1. **Relativistic Disk Geometry** — Upgraded to a 4-part relativistic warped mesh:
   equatorial primary disk, upper lensed arch (bending over the event horizon),
   lower lensed arch (bending under the event horizon), and ISCO photon collar.
   Renders in 1 draw call (~14k tris) with 0 per-frame raymarching cost.
2. **Lensing & Star Shear** — Schwarzschild deflection curve shared between sky
   and starfield. Stars dynamically stretch into tangential Einstein arcs near
   the event horizon with pin-point Airy diffraction cores.
3. **Interior is artistic interpretation** — stated in the end card; the final
   descent deliberately fades toward black rather than inventing a landscape.
4. **Device validation** — Target is Meta Quest 3 / Quest 3S (Snapdragon XR2 Gen 2).
   Profile with OVR Metrics Tool / Meta Quest Developer Hub.
5. **Audio is synthesized beds** — no recorded content, no HRTF; spatial cues
   are limited to stereo pan. Fine for ambience; revisit only if user testing
   asks for directional cues.
