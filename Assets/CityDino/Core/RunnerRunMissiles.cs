using System;
using System.Collections.Generic;

namespace CityDino
{
    /// <summary>A live rocket in the same bottom-up space as the runner.</summary>
    public sealed class RunMissile
    {
        public int Id, VolleyId;
        public float X, Y, Width, Height, VelocityX, Radius, Lifetime;
        public bool Removed;
    }

    /// <summary>One harmless cue owns the complete simultaneously launched volley.</summary>
    public sealed class RunMissileWarning
    {
        public int Id, VolleyId, Count;
        public float X, Y, Width, Height, Remaining, Duration;
    }

    public sealed partial class RunnerSession
    {
        public const float RunMissileWarningDuration = 1.2f;
        public const float RunMissileSpacingSeconds = .22f;
        private readonly List<RunMissile> runMissiles = new List<RunMissile>();
        private readonly List<RunMissileWarning> runMissileWarnings = new List<RunMissileWarning>();
        private Random runMissileRandom;
        private double nextRunMissileAt;
        private double runMissileResumeAt;
        private double runMissileWarningStartedAt;
        private bool runMissilePending;
        private bool runMissileRecoverySupplyPending;
        private District runMissileRecoveryDistrict;
        private int nextRunMissileId;
        private int nextRunVolleyId;
        public IReadOnlyList<RunMissile> RunMissiles { get { return runMissiles; } }
        public IReadOnlyList<RunMissileWarning> RunMissileWarnings { get { return runMissileWarnings; } }
        public bool RunMissileCorridorReserved
        {
            get { return runMissilePending || runMissileRecoverySupplyPending || runMissiles.Count > 0 || runMissileWarnings.Count > 0 || elapsedClock < runMissileResumeAt; }
        }

        private float RunMissileRange(float minimum, float maximum)
        {
            return minimum + (maximum - minimum) * (float)runMissileRandom.NextDouble();
        }

        private void ResetRunMissiles()
        {
            runMissiles.Clear(); runMissileWarnings.Clear();
            nextRunMissileId = nextRunVolleyId = 0;
            runMissilePending = runMissileRecoverySupplyPending = false;
            runMissileResumeAt = runMissileWarningStartedAt = 0d;
            nextRunMissileAt = elapsedClock + RunMissileRange(20f, 28f);
        }

        // Shared by boss entry/exit. Only the running clock sets the grace,
        // so long arena fights cannot age an unseen rocket into the return lane.
        private void SuspendRunMissiles()
        {
            runMissiles.Clear(); runMissileWarnings.Clear();
            runMissilePending = runMissileRecoverySupplyPending = false;
            runMissileResumeAt = elapsedClock + 5d;
            nextRunMissileAt = Math.Max(nextRunMissileAt, elapsedClock + 5d);
        }

        private bool RunMissileLaneClear()
        {
            if (PlayerY < -.5f || !HasFloor()) return false;
            foreach (WorldEntity entity in entities)
                if (!entity.Removed && IsPowerupObstacle(entity.Kind) &&
                    entity.X + entity.Width >= MinimumPlayerX - 100f)
                    return false;
            return true;
        }

        private bool RunMissileSightWindowClear()
        {
            if (CurrentDistrict != District.Night) return true;
            if (BlackoutWarning || DarknessAmount > 0f) return false;
            float speed = Math.Max(1f, Speed + 170f);
            // All four low rockets must clear even the leftmost playable lane
            // before the next scheduled blackout, including their .66s tail.
            float visibleTime = RunMissileWarningDuration +
                (CanvasWidth + 140f - MinimumPlayerX) / speed + 3f * RunMissileSpacingSeconds + .75f;
            return NextBlackoutAt - DistrictElapsed > visibleTime;
        }

        private void PrepareRunMissileCorridor()
        {
            if (State != RunState.Playing || IsBossArena || IsPractice) return;
            if (!runMissilePending && runMissiles.Count == 0 && runMissileWarnings.Count == 0 &&
                elapsedClock >= nextRunMissileAt && elapsedClock >= runMissileResumeAt)
                runMissilePending = true;
            if (!runMissilePending || !RunMissileLaneClear() || !RunMissileSightWindowClear()) return;
            // Later runs choose the larger wave more often, while every count
            // stays possible. Counts are deliberately bounded to four rockets.
            bool large = runMissileRandom.NextDouble() < Math.Min(.72d, .3d + elapsedClock / 600d);
            int count = large ? runMissileRandom.Next(3, 5) : runMissileRandom.Next(1, 3);
            int volley = ++nextRunVolleyId;
            runMissileWarnings.Add(new RunMissileWarning
            {
                Id = ++nextRunMissileId, VolleyId = volley, Count = count,
                X = 90f, Y = 14f, Width = CanvasWidth - 90f, Height = 22f,
                Remaining = RunMissileWarningDuration, Duration = RunMissileWarningDuration
            });
            runMissileWarningStartedAt = elapsedClock;
            runMissilePending = false;
        }

        private void LaunchRunMissileVolley(RunMissileWarning warning)
        {
            float speed = Speed + 170f;
            for (int i = 0; i < warning.Count; i++)
            {
                float start = CanvasWidth + 40f + i * speed * RunMissileSpacingSeconds;
                runMissiles.Add(new RunMissile
                {
                    Id = ++nextRunMissileId, VolleyId = warning.VolleyId,
                    X = start, Y = warning.Y, Width = 40f, Height = 22f, Radius = 11f,
                    VelocityX = -speed, Lifetime = (start + 180f) / speed + 1f
                });
            }
        }

        private void TickRunMissiles(float dt)
        {
            if (State != RunState.Playing || IsBossArena || IsPractice) return;
            for (int i = runMissileWarnings.Count - 1; i >= 0; i--)
            {
                RunMissileWarning warning = runMissileWarnings[i];
                // A cue created by Prepare in this very substep has not been
                // visible for any simulated time yet. Keep its complete 1.2s.
                float warningDt = (float)Math.Max(0d, Math.Min(dt, elapsedClock - runMissileWarningStartedAt));
                warning.Remaining -= warningDt;
                if (warning.Remaining > 0f) continue;
                LaunchRunMissileVolley(warning);
                runMissileWarnings.RemoveAt(i);
            }
            bool hadMissiles = runMissiles.Count > 0;
            for (int i = 0; i < runMissiles.Count && State == RunState.Playing; i++)
            {
                RunMissile missile = runMissiles[i];
                if (missile.Removed) continue;
                missile.X += missile.VelocityX * dt;
                missile.Lifetime -= dt;
                if (missile.Lifetime <= 0f || missile.X + missile.Width < -100f)
                { missile.Removed = true; continue; }
                if (InvincibleRemaining > 0f) continue;
                if (!RunnerGeometry.CircleTouchesPlayer(missile.X + missile.Width * .5f,
                    missile.Y + missile.Height * .5f, missile.Radius, this)) continue;
                missile.Removed = true;
                if (!TryAbsorbHazard(true)) TakeHit("Trúng tên lửa trên đường chạy · Nhảy để né");
            }
            // A fatal contact preserves the final visible missile positions and
            // warning state along with the death pose; no off-screen cleanup runs.
            if (State != RunState.Playing) return;
            for (int i = runMissiles.Count - 1; i >= 0; i--)
                if (runMissiles[i].Removed) runMissiles.RemoveAt(i);
            if (hadMissiles && runMissiles.Count == 0)
            {
                runMissileResumeAt = elapsedClock + .75d;
                nextRunMissileAt = elapsedClock + RunMissileRange(12f, 19f);
                distanceToPattern = Math.Max(distanceToPattern, Speed * .75f + 200f);
                runMissileRecoverySupplyPending = CurrentDistrict == District.Snow || CurrentDistrict == District.Sunset;
                runMissileRecoveryDistrict = CurrentDistrict;
            }
            OfferRunMissileRecoverySupply();
        }

        private void OfferRunMissileRecoverySupply()
        {
            if (!runMissileRecoverySupplyPending) return;
            if (CurrentDistrict != runMissileRecoveryDistrict)
            { runMissileRecoverySupplyPending = false; return; }
            if (!grounded || PlayerY < -.5f || PlayerY > 1f || !RunMissileLaneClear()) return;
            // Forced jumps can carry the actor over a real ground-level drink.
            // Offer one replacement after landing, in the still-clear lane;
            // recovery still requires an ordinary physical pickup, never a heal.
            float lead = Math.Min(260f, Math.Max(90f, Speed * .4f));
            Add(CurrentDistrict == District.Snow ? EntityKind.HotDrink : EntityKind.Water,
                PlayerX + lead, 10f, 32f, 36f);
            nextSurvivalSupplyAt = Math.Max(nextSurvivalSupplyAt, elapsedClock + 3.7d);
            runMissileResumeAt = Math.Max(runMissileResumeAt, elapsedClock + .75d);
            distanceToPattern = Math.Max(distanceToPattern, Speed * .75f + 200f);
            runMissileRecoverySupplyPending = false;
        }

        private float RunMissileCollisionSpeed()
        {
            float speed = 0f;
            for (int i = 0; i < runMissiles.Count; i++)
                if (!runMissiles[i].Removed) speed = Math.Max(speed, Math.Abs(runMissiles[i].VelocityX));
            return speed;
        }
    }
}
