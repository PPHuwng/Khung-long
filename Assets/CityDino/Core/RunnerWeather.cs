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
        public static LightningFrame GetLightning(District district, float districtElapsed)
        {
            if ((district != District.Rain && district != District.Storm) ||
                float.IsNaN(districtElapsed) || float.IsInfinity(districtElapsed) || districtElapsed < 2f)
                return new LightningFrame();

            float interval = district == District.Rain ? 3.4f : 2.8f;
            float sinceFirst = districtElapsed - 2f;
            int strike = (int)Math.Floor(sinceFirst / interval) + 1;
            float phase = sinceFirst - (strike - 1) * interval;
            const float pulse = 0.42f;
            float amount = phase < pulse ? (float)Math.Sin(Math.PI * phase / pulse) : 0f;
            uint hash = unchecked((uint)strike * 2654435761u + (uint)district * 374761393u);
            return new LightningFrame
            {
                Amount = Math.Max(0f, amount),
                StrikeId = strike,
                BoltX = 180f + (hash % 920u)
            };
        }
    }
}
