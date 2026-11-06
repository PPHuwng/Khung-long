using System;

namespace CityDino
{
    /// <summary>Helmet journey, buoyant swimming and separately warned submarine attacks.</summary>
    public sealed partial class RunnerSession
    {
        private bool swimLiftHeld, swimDiveHeld;
        private float swimStrokeCooldown;
        public bool IsUnderwaterBoss { get { return IsBossArena && CurrentBoss != null && CurrentBoss.Kind == BossKind.Submarine; } }
        public bool IsFreeMovementBoss { get { return IsFlyingBoss || IsUnderwaterBoss; } }
        public bool SwimControlsEnabled { get { return State == RunState.Playing && IsUnderwaterBoss && ArenaPhase == BossPhase.Fighting; } }
        public bool DivingHelmetEquipped { get; private set; }
        public float DivingHelmetX { get { return 470f; } }
        public float DivingHelmetY { get { return 18f; } }
        public float WaterTransitionProgress { get { return WaterDepth; } }
        public float WaterDepth
        {
            get
            {
                if (!IsUnderwaterBoss) return 0f;
                if (ArenaPhase == BossPhase.Intro) return SmoothClamp((entranceClock - 1.9f) / 1.9f, true);
                if (ArenaPhase == BossPhase.Victory) return 1f - ExitLandingProgress;
                return 1f;
            }
        }
        public void SetSwimLift(bool held) { swimLiftHeld = held && SwimControlsEnabled; }
        public void SetSwimDive(bool held) { swimDiveHeld = held && SwimControlsEnabled; }
        private void ClearSwimIntent() { swimLiftHeld = swimDiveHeld = false; }
        private void ResetUnderwaterState()
        {
            ClearSwimIntent(); swimStrokeCooldown = 0f; DivingHelmetEquipped = false;
        }
        public bool SwimStroke()
        {
            if (!SwimControlsEnabled || swimStrokeCooldown > 0f) return false;
            VelocityY = Math.Max(VelocityY, 135f); AnimationVelocityY = VelocityY;
            swimStrokeCooldown = .18f; return true;
        }
        private void IntegrateBossSwim(float dt)
        {
            swimStrokeCooldown = Math.Max(0f, swimStrokeCooldown - dt);
            // Dive is an explicit direction command, including when the
            // player is still holding the rise key or another touch button.
            float force = swimDiveHeld ? -580f : swimLiftHeld ? 420f : -230f;
            VelocityY += force * dt;
            VelocityY *= Math.Max(0f, 1f - 1.5f * dt);
            VelocityY = Math.Max(-220f, Math.Min(200f, VelocityY));
            PlayerY += VelocityY * dt;
            if (PlayerY < FlightMinimumY) { PlayerY = FlightMinimumY; VelocityY = Math.Max(0f, VelocityY); }
            if (PlayerY > FlightMaximumY) { PlayerY = FlightMaximumY; VelocityY = Math.Min(0f, VelocityY); }
            AnimationVelocityY = VelocityY; grounded = false; duckRequested = IsDucking = false;
        }

        private void PlanSubmarinePattern(float ordinaryWarning)
        {
            int choice = ChooseBossAttack(6);
            primaryBossAttack = choice == 0 ? BossAttackKind.Torpedo : choice == 1 ? BossAttackKind.FishShoal
                : choice == 2 ? BossAttackKind.SeaMine : choice == 3 ? BossAttackKind.SonarBeam
                : choice == 4 ? BossAttackKind.DepthCharge : BossAttackKind.SubmarineRam;
            float warning = BossTier >= 3 ? ordinaryWarning : Math.Max(1.1f * BossPracticeWarningScale, ordinaryWarning);
            int casts = Math.Min(5, BossRepeatCount);
            float last = 0f, clearance = 3.2f / BossTierPace;
            if (primaryBossAttack == BossAttackKind.SubmarineRam)
            {
                PlanBossSpecial(primaryBossAttack, warning);
                return;
            }
            if (primaryBossAttack == BossAttackKind.Torpedo || primaryBossAttack == BossAttackKind.FishShoal)
            {
                // The corridor stays fixed for this complete wave, even if the
                // actor moves after the signal. No projectile secretly homes.
                float gapBottom = (bossAttackIndex % 2 == 0) ? 55f : 130f;
                float gapHeight = Math.Max(180f, BossAirGapHeight);
                for (int wave = 0; wave < casts; wave++)
                {
                    last = BossCastSpacing(wave * .92f);
                    float low = Math.Max(8f, gapBottom - 29f);
                    float high = Math.Min(330f, gapBottom + gapHeight + 5f);
                    AddWaterRow(primaryBossAttack, last, warning, low, wave % 2 == 0, gapBottom, gapHeight);
                    AddWaterRow(primaryBossAttack, last, warning, high, wave % 2 != 0, gapBottom, gapHeight);
                }
            }
            else if (primaryBossAttack == BossAttackKind.SeaMine)
            {
                casts = Math.Min(2, Math.Max(1, BossRepeatCount - 1));
                float centre = Math.Max(240f, Math.Min(620f, PlayerX + (bossAttackIndex % 2 == 0 ? 90f : -90f)));
                float gapX = centre - 115f;
                for (int wave = 0; wave < casts; wave++)
                for (int lane = 0; lane < 6; lane++)
                {
                    float x = 110f + lane * 140f;
                    if (x + 19f > gapX && x - 19f < gapX + 230f) continue;
                    last = BossCastSpacing(wave * 1.35f);
                    BossTelegraph cue = AddSpecialCue(primaryBossAttack, last, warning,
                        x - 19f, 0f, 38f, 360f);
                    cue.GapX = gapX; cue.GapWidth = 230f;
                    cue.OriginX = x - 19f; cue.OriginY = 8f;
                    cue.VelocityY = 65f;
                }
                clearance = 6.8f / BossTierPace;
            }
            else if (primaryBossAttack == BossAttackKind.SonarBeam)
            {
                float lockedY = Math.Max(45f, Math.Min(305f, PlayerY + 42f));
                for (int wave = 0; wave < casts; wave++)
                {
                    last = BossCastSpacing(wave * 1.1f);
                    float y = Math.Max(35f, Math.Min(325f, lockedY + (wave % 2 == 0 ? 0f : lockedY > 170f ? -110f : 110f)));
                    float beamWarning = BossTier >= 3 ? BossBeamWarning : Math.Max(warning, 1.2f * BossPracticeWarningScale);
                    AddSpecialCue(primaryBossAttack, last, beamWarning, 75f, BossBeamLaneY(y,beamWarning), 760f, 14f);
                }
                warning = BossTier >= 3 ? BossBeamWarning : Math.Max(warning, 1.2f * BossPracticeWarningScale);
                clearance = .8f;
            }
            else if (primaryBossAttack == BossAttackKind.DepthCharge)
            {
                casts = Math.Min(4, BossRepeatCount);
                warning = BossTier >= 3 ? Math.Max(warning, .6f * BossPracticeWarningScale) : Math.Max(warning, 1.35f * BossPracticeWarningScale);
                float centre = Math.Max(235f, Math.Min(625f, PlayerX + (bossAttackIndex % 2 == 0 ? 100f : -100f)));
                float gapX = centre - 120f;
                for (int wave = 0; wave < casts; wave++)
                for (int lane = 0; lane < 6; lane++)
                {
                    float x = 110f + lane * 135f;
                    if (x + 32f > gapX && x - 32f < gapX + 240f) continue;
                    last = BossCastSpacing(wave * .9f);
                    BossTelegraph cue = AddSpecialCue(primaryBossAttack, last, warning, x - 32f, 0f, 64f, 430f);
                    cue.OriginX = x - 14f; cue.OriginY = 420f;
                    cue.VelocityY = -225f; cue.GapX = gapX; cue.GapWidth = 240f;
                }
                clearance = 2.4f / BossTierPace;
            }
            ExtendBossCycle(last, warning, clearance);
            CurrentBoss.AttackName = AttackName(primaryBossAttack) + (casts > 1 ? " · " + casts + " đợt" : "");
        }

        private void AddWaterRow(BossAttackKind kind, float delay, float warning, float y,
            bool fromLeft, float gapY, float gapHeight)
        {
            BossTelegraph cue = AddSpecialCue(kind, delay, warning, 75f, y, 1100f,
                kind == BossAttackKind.Torpedo ? 20f : 24f);
            cue.OriginX = fromLeft ? -80f : CanvasWidth + 50f; cue.OriginY = y;
            cue.VelocityX = fromLeft ? 290f : -290f;
            cue.GapY = gapY; cue.GapHeight = gapHeight;
        }

        private bool SpawnUnderwaterAttack(BossTelegraph source, float pace)
        {
            if (source.Kind == BossAttackKind.Torpedo || source.Kind == BossAttackKind.FishShoal)
            {
                int count = source.Kind == BossAttackKind.FishShoal ? 3 : 1;
                for (int i = 0; i < count; i++)
                {
                    bool fish = source.Kind == BossAttackKind.FishShoal;
                    float width = fish ? 32f : 48f, height = fish ? 24f : 20f;
                    float x = source.OriginX - Math.Sign(source.VelocityX) * i * 86f;
                    bossProjectiles.Add(new BossProjectile {
                        Id = ++nextBossObjectId, Kind = source.Kind, X = x, Y = source.OriginY,
                        Width = width, Height = height, Radius = fish ? 11f : 10f,
                        VelocityX = source.VelocityX * pace, VelocityY = source.VelocityY * pace, Lifetime = 8f, SourceTelegraphId = source.Id
                    });
                }
            }
            else if (source.Kind == BossAttackKind.SeaMine)
                AddBossObstacle(source.Kind, source.OriginX, source.OriginY, 38f, 38f, 0f,
                    source.VelocityY * pace, 6.5f / pace, 24f, source.Id, 19f);
            else if (source.Kind == BossAttackKind.DepthCharge)
                bossProjectiles.Add(new BossProjectile { Id = ++nextBossObjectId, Kind = source.Kind,
                    X = source.OriginX, Y = source.OriginY, Width = 28f, Height = 28f, Radius = 14f,
                    VelocityY = source.VelocityY * pace, Lifetime = 6f, SourceTelegraphId = source.Id });
            else return false;
            CurrentBoss.ShotFlashRemaining = .2f; CurrentBoss.Recoil = 1f;
            return true;
        }
    }
}
