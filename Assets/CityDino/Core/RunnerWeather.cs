using System;

namespace CityDino
{
    public struct LightningFrame
    {
        public float Amount;
        public int StrikeId;
        public float BoltX;
    }

    /// <summary>One shared clock for the rendered lightning and its thunder cue.</summary>
    public static class RunnerWeather
    {
        public const float StormFirstStrike = 1f;
        public const float StormStrikeInterval = 1.8f;
        public const float LightningPulseDuration = 0.32f;

        public static LightningFrame GetLightning(District district, float districtElapsed)
        {
            if (district != District.Storm || !ValidTime(districtElapsed) || districtElapsed < StormFirstStrike)
                return new LightningFrame();
            float sinceFirst = districtElapsed - StormFirstStrike;
            int strike = (int)Math.Floor(sinceFirst / StormStrikeInterval) + 1;
            float phase = sinceFirst - (strike - 1) * StormStrikeInterval;
            float amount = phase < LightningPulseDuration
                ? (float)Math.Sin(Math.PI * phase / LightningPulseDuration) : 0f;
            uint hash = unchecked((uint)strike * 2654435761u + (uint)district * 374761393u);
            return new LightningFrame { Amount = Math.Max(0f, amount), StrikeId = strike, BoltX = 180f + hash % 920u };
        }

        public static float GetStormDarkness(float districtElapsed)
        {
            if (!ValidTime(districtElapsed)) return 0f;
            double phase = (districtElapsed - StormFirstStrike) % StormStrikeInterval;
            if (phase < 0) phase += StormStrikeInterval;
            float wave = (float)(0.5d + 0.5d * Math.Cos(phase * Math.PI * 2d / StormStrikeInterval));
            float dark = 0.18f + 0.70f * wave;
            float flash = GetLightning(District.Storm, districtElapsed).Amount;
            return dark * (1f - flash * 0.9f);
        }

        private static bool ValidTime(float time)
        { return !float.IsNaN(time) && !float.IsInfinity(time) && time >= 0f; }
    }
}
