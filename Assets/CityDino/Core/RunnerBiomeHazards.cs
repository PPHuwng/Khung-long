using System;

namespace CityDino
{
    /// <summary>
    /// Shared physical states for biome traps. X/Y remain the real body position;
    /// warning marks and decorative glow are excluded from contact tests.
    /// </summary>
    public static class RunnerBiomeHazards
    {
        public const float IceWarningStartX = 1260f;
        public const float IceFallStartX = 840f;
        public const float IceLandX = 500f;
        public const float IceHangHeight = 260f;
        public const float IceHeight = 72f;
        public const float VentWarningDuration = 0.65f;
        public const float VentActiveDuration = 0.90f;
        public const float VentSafeDuration = 0.95f;
        public const float DebrisWarningDuration = 0.65f;

        public static bool IsBiomeHazard(EntityKind kind)
        {
            return kind == EntityKind.FallingIce || kind == EntityKind.HeatVent ||
                kind == EntityKind.StormDebris;
        }

        public static bool IsDamaging(WorldEntity entity)
        {
            return entity != null && !entity.Removed && IsBiomeHazard(entity.Kind) &&
                entity.HazardActive && (entity.Kind != EntityKind.HeatVent || entity.HazardHeight > 0f);
        }

        public static float GetVentCycleScale(WorldEntity entity)
        {
            return 1f + Math.Max(0, Math.Min(3, entity.Variant)) * 0.35f;
        }

        public static void Tick(WorldEntity entity, float dt, float speed)
        {
            if (entity == null || entity.Removed || !IsBiomeHazard(entity.Kind)) return;
            if (entity.Kind == EntityKind.FallingIce)
            {
                // Spatial timing makes the warning visible ahead of every X in
                // the control range. The fallen shard remains jumpable afterwards.
                entity.HazardWarning = Clamp01((IceWarningStartX - entity.X) /
                    (IceWarningStartX - IceFallStartX));
                float fall = Clamp01((IceFallStartX - entity.X) / (IceFallStartX - IceLandX));
                entity.Y = IceHangHeight * (1f - fall) * (1f - fall);
                entity.HazardVelocityY = fall > 0f && fall < 1f
                    ? -2f * IceHangHeight * (1f - fall) * speed / (IceFallStartX - IceLandX) : 0f;
                entity.HazardActive = entity.X <= IceFallStartX;
                entity.HazardPhase = entity.HazardActive ? 1 : 0;
                entity.HazardHeight = entity.Height;
                if (entity.X <= IceWarningStartX) entity.HazardAge += dt;
                return;
            }

            // Do not spend the warning while the complete object is off screen.
            if (entity.X + entity.Width <= RunnerSession.CanvasWidth - 12f || entity.HazardAge > 0f)
                entity.HazardAge += dt * Math.Max(0f, speed) / RunnerSession.InitialSpeed;
            if (entity.Kind == EntityKind.StormDebris)
            {
                entity.HazardWarning = Clamp01(entity.HazardAge / DebrisWarningDuration);
                entity.HazardActive = entity.HazardAge >= DebrisWarningDuration;
                entity.HazardPhase = entity.HazardActive ? 1 : 0;
                entity.HazardHeight = entity.Height;
                return;
            }

            float cycle = VentWarningDuration + VentActiveDuration + VentSafeDuration;
            float phase = (entity.HazardAge / GetVentCycleScale(entity)) % cycle;
            if (phase < VentWarningDuration)
            {
                entity.HazardPhase = 0;
                entity.HazardWarning = phase / VentWarningDuration;
                entity.HazardHeight = 0f;
                entity.HazardActive = false;
            }
            else if (phase < VentWarningDuration + VentActiveDuration)
            {
                float activeAge = phase - VentWarningDuration;
                float rise = Math.Min(activeAge / 0.12f, (VentActiveDuration - activeAge) / 0.12f);
                entity.HazardPhase = 1;
                entity.HazardWarning = 1f;
                entity.HazardHeight = entity.Height * Clamp01(rise);
                entity.HazardActive = entity.HazardHeight > 0f;
            }
            else
            {
                entity.HazardPhase = 2;
                entity.HazardWarning = 0f;
                entity.HazardHeight = 0f;
                entity.HazardActive = false;
            }
        }

        public static bool TouchesPlayer(WorldEntity entity, float left, float right,
            float bottom, float top)
        {
            if (!IsDamaging(entity)) return false;
            float height = entity.Kind == EntityKind.HeatVent ? entity.HazardHeight : entity.Height;
            if (right <= entity.X + 3f || left >= entity.X + entity.Width - 3f ||
                top <= entity.Y + 3f || bottom >= entity.Y + height - 3f)
                return false;

            if (entity.Kind == EntityKind.HeatVent)
            {
                // The plume narrows towards the top; its empty upper corners
                // cannot hurt a player who has already cleared the bright core.
                float centre = entity.X + entity.Width * 0.5f;
                float nearest = Math.Max(left, Math.Min(right, centre));
                float heightAtOverlap = height * Math.Max(0f,
                    1f - Math.Abs(nearest - centre) / (entity.Width * 0.5f));
                return bottom < entity.Y + heightAtOverlap - 3f;
            }
            if (entity.Kind == EntityKind.FallingIce)
            {
                // Downward-pointing shard: wide at its top, narrow at its tip.
                float highestOverlap = Math.Min(top, entity.Y + height);
                float half = entity.Width * 0.5f * Clamp01((highestOverlap - entity.Y) / height);
                float centre = entity.X + entity.Width * 0.5f;
                return right > centre - half + 3f && left < centre + half - 3f;
            }
            return true;
        }

        private static float Clamp01(float value)
        { return Math.Max(0f, Math.Min(1f, value)); }
    }
}
