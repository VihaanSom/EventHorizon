using UnityEngine;

namespace EventHorizon.Environment
{
    /// <summary>
    /// Plain data for one pooled dust mote. No MonoBehaviour per particle:
    /// the controller owns a fixed preallocated array and issues a single
    /// instanced draw call, so normal playback allocates nothing.
    /// </summary>
    [System.Serializable]
    public struct EHDustParticle
    {
        public Vector3 position;
        public Vector3 velocitySeed; // deterministic drift direction
        public float size;
        public float brightness;
        public float phase; // 0..1 scatter offset for staggered motion
    }
}
