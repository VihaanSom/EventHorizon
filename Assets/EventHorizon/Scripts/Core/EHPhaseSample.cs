namespace EventHorizon
{
    /// <summary>Timeline playback state.</summary>
    public enum EHTimelineState
    {
        Stopped,
        Playing,
        Paused,
        Finished
    }

    /// <summary>
    /// Deterministic interpolated sample of every animated experience parameter.
    /// A pure function of (timeSeconds, settings): same inputs, same outputs.
    /// </summary>
    public struct EHPhaseSample
    {
        public int phaseIndex;
        public float phaseLocalT;
        public float totalT;
        public float blackHoleScale;
        public float lensingStrength;
        public float diskEmission;
        public float starBrightness;
        public float dustDensity;
        public float streakAmount;
        public float environmentMotion;
        public float audioIntensity;

        public static EHPhaseSample Lerp(in EHPhaseSample a, in EHPhaseSample b, float t)
        {
            return new EHPhaseSample
            {
                phaseIndex = t < 1f ? a.phaseIndex : b.phaseIndex,
                phaseLocalT = LerpFloat(a.phaseLocalT, b.phaseLocalT, t),
                totalT = LerpFloat(a.totalT, b.totalT, t),
                blackHoleScale = LerpFloat(a.blackHoleScale, b.blackHoleScale, t),
                lensingStrength = LerpFloat(a.lensingStrength, b.lensingStrength, t),
                diskEmission = LerpFloat(a.diskEmission, b.diskEmission, t),
                starBrightness = LerpFloat(a.starBrightness, b.starBrightness, t),
                dustDensity = LerpFloat(a.dustDensity, b.dustDensity, t),
                streakAmount = LerpFloat(a.streakAmount, b.streakAmount, t),
                environmentMotion = LerpFloat(a.environmentMotion, b.environmentMotion, t),
                audioIntensity = LerpFloat(a.audioIntensity, b.audioIntensity, t)
            };
        }

        static float LerpFloat(float a, float b, float t) => a + (b - a) * t;
    }
}
