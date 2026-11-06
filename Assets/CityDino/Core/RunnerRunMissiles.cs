using System;
using System.Collections.Generic;

namespace CityDino
{
    /// <summary>A live rocket in the same bottom-up space as the runner.</summary>
    public sealed class RunMissile
    {
        public int Id, VolleyId;
        public float X, Y, Width, Height, VelocityX, Radius, Lifetime, GapY, GapHeight;
        public bool Removed;
    }

    /// <summary>One harmless cue owns the complete simultaneously launched volley.</summary>
    public sealed class RunMissileWarning
    {
        public int Id, VolleyId, Count;
        public float X, Y, Width, Height, Remaining, Duration, GapY, GapHeight;

        // The launch marker and the damaging column share this exact geometry.
        // An unset gap is useful for older/injected fixtures, not a second path.
        public float MissileY(int index)
        {
            float bottom = GapHeight > 0f ? GapY : 85f;
            float top = bottom + (GapHeight > 0f ? GapHeight : 180f);
            if (index == 0) return 5f; // centre 16: the floor is never a free route
            if (Count >= 3 && index == 1) return bottom - 22f;
            return top + (index - (Count >= 3 ? 2 : 1)) * 44f;
        }
    }

    public sealed partial class RunnerSession
    {
        public const float RunMissileWarningDuration = .9f;
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
        // Terrain keeps accelerating without a ceiling. Rockets use a separate
        // approach speed so even a very long run retains visible flight time.
        public float RunMissileSpeed
        {
            get { return Math.Min(870f, 530f + 110f * (float)(Math.Log(1d + elapsedClock / 120d) / Math.Log(2d))); }
        }
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
            float speed = RunMissileSpeed;
            // A column must clear the leftmost playable lane before blackout.
            float visibleTime = RunMissileWarningDuration +
                (CanvasWidth + 140f - MinimumPlayerX) / speed + .75f;
            return NextBlackoutAt - DistrictElapsed > visibleTime;
        }

        private void PrepareRunMissileCorridor()
        {
            if (State != RunState.Playing || IsBossArena || IsPractice) return;
            if (!runMissilePending && runMissiles.Count == 0 && runMissileWarnings.Count == 0 &&
                elapsedClock >= nextRunMissileAt && elapsedClock >= runMissileResumeAt)
                runMissilePending = true;
            if (!runMissilePending || !RunMissileLaneClear() || !RunMissileSightWindowClear()) return;
            // Three or four rockets form a vertical wall, not a low train that
            // any jump clears. The hole is locked before the launch and varies
            // between a low timed pass and a higher pass near the jump apex.
            bool large = runMissileRandom.NextDouble() < Math.Min(.72d, .3d + elapsedClock / 600d);
            int count = large ? 4 : 3;
            float gapY = 55f + 30f * runMissileRandom.Next(3);
            int volley = ++nextRunVolleyId;
            runMissileWarnings.Add(new RunMissileWarning
            {
                Id = ++nextRunMissileId, VolleyId = volley, Count = count,
                X = CanvasWidth + 40f, Y = 5f, Width = 40f, Height = gapY + 246f,
                GapY = gapY, GapHeight = 180f,
                Remaining = RunMissileWarningDuration, Duration = RunMissileWarningDuration
            });
            runMissileWarningStartedAt = elapsedClock;
            runMissilePending = false;
        }

        private void LaunchRunMissileVolley(RunMissileWarning warning)
        {
            float speed = RunMissileSpeed;
            for (int i = 0; i < warning.Count; i++)
            {
                float start = CanvasWidth + 40f;
                runMissiles.Add(new RunMissile
                {
                    Id = ++nextRunMissileId, VolleyId = warning.VolleyId,
                    X = start, Y = warning.MissileY(i), Width = 40f, Height = 22f, Radius = 11f,
                    GapY = warning.GapHeight > 0f ? warning.GapY : 85f,
                    GapHeight = warning.GapHeight > 0f ? warning.GapHeight : 180f,
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
                // visible for any simulated time yet. Keep its complete cue.
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
                if (!TryAbsorbHazard(true)) TakeHit("Trúng tên lửa trên đường chạy · Canh nhảy qua khe");
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
