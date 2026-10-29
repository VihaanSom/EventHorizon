# Meshes — intentionally empty

All geometry (sky dome, star quads, accretion annulus, dust quad) is generated
deterministically at runtime by the controllers (seeded `System.Random` +
closed-form builders). No mesh assets to version, no per-frame regeneration.
