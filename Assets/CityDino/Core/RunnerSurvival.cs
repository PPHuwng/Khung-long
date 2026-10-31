using System;

namespace CityDino
{
    /// <summary>
    /// True gradual recovery: pickups add a finite amount to a queue rather
    /// than jumping the resource bar. Weather drain remains independent.
    /// </summary>
    public sealed partial class RunnerSession
    {
        public const float WaterPickupRecovery = 30f;
        public const float WarmthPickupRecovery = 35f;
        public const float SurvivalRecoveryRate = 10f;
        public const float WaterRecoveryQueueLimit = 60f;
        public const float WarmthRecoveryQueueLimit = 70f;
        public const float ReserveImmediateRecovery = 10f;
        public const float ReserveGradualRecovery = 50f;

        private float waterRecoveryRemaining;
        private float warmthRecoveryRemaining;
        public float WaterRecoveryRemaining { get { return waterRecoveryRemaining; } }
        public float WarmthRecoveryRemaining { get { return warmthRecoveryRemaining; } }

        private void BeginRunSurvival()
        {
            ResetSurvivalForDistrict();
        }

        private void ResetSurvivalForDistrict()
        {
            waterRecoveryRemaining = warmthRecoveryRemaining = 0f;
        }

        private void QueueSurvivalPickup(bool cold)
        {
            if (cold)
                warmthRecoveryRemaining = Math.Min(WarmthRecoveryQueueLimit,
                    warmthRecoveryRemaining + WarmthPickupRecovery);
            else
                waterRecoveryRemaining = Math.Min(WaterRecoveryQueueLimit,
                    waterRecoveryRemaining + WaterPickupRecovery);
        }

        private void TickSurvivalRecovery(float dt)
        {
            if (State != RunState.Playing || IsBossArena || !IsFinite(dt) || dt <= 0f) return;
            float maximum = SurvivalRecoveryRate * dt;
            if (CurrentDistrict == District.Snow)
            {
                float amount = Math.Min(maximum, warmthRecoveryRemaining);
                warmthRecoveryRemaining = Math.Max(0f, warmthRecoveryRemaining - amount);
                Warmth = Math.Min(100f, Warmth + amount);
            }
            else if (CurrentDistrict == District.Sunset)
            {
                float amount = Math.Min(maximum, waterRecoveryRemaining);
                waterRecoveryRemaining = Math.Max(0f, waterRecoveryRemaining - amount);
                Water = Math.Min(100f, Water + amount);
            }
        }

        private bool TryUseGradualSurvivalReserve(bool cold)
        {
            if (!TryUseSurvivalReserve(cold)) return false;
            if (cold)
            {
                Warmth = Math.Min(100f, Warmth + ReserveImmediateRecovery);
                warmthRecoveryRemaining = Math.Min(WarmthRecoveryQueueLimit,
                    warmthRecoveryRemaining + ReserveGradualRecovery);
            }
            else
            {
                Water = Math.Min(100f, Water + ReserveImmediateRecovery);
                waterRecoveryRemaining = Math.Min(WaterRecoveryQueueLimit,
                    waterRecoveryRemaining + ReserveGradualRecovery);
            }
            return true;
        }
    }
}
