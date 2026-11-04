using System;

namespace CityDino
{
    /// <summary>Locked attack plans: a visible cue owns every charge and missile wave.</summary>
    public sealed partial class RunnerSession
    {
        private bool PlanBossSpecial(BossAttackKind kind, float warning)
        {
            if (IsBossCharge(kind))
            {
                float height = kind == BossAttackKind.TankRam ? 68f : kind == BossAttackKind.RobotDash ? 110f : 70f;
                float y = kind == BossAttackKind.GunshipDive ? Math.Max(35f, Math.Min(280f, PlayerY + 12f)) : 0f;
                float cueTime = Math.Max(1.25f * BossPracticeWarningScale, warning), delay = 0f, end = 0f;
                int casts = BossRepeatCount;
                for (int i = 0; i < casts; i++)
                {
                    // Variant is chosen once when planning and appears in the
                    // cue. It never secretly switches destination during travel.
                    bool full = random.Next(2) == 0;
                    float endpoint = full ? -CurrentBoss.Width - 24f : Math.Max(100f, Math.Min(500f, PlayerX - 170f));
                    float left = Math.Min(90f, endpoint + 8f);
                    BossTelegraph cue = AddSpecialCue(kind, delay * BossPracticePace, cueTime,
                        left, y, CanvasWidth - left, height);
                    cue.OriginX = CurrentBoss.X; cue.OriginY = CurrentBoss.Y;
                    cue.AimX = cue.SweepEndX = endpoint; cue.AimY = y; cue.FullSweep = full;
                    float strike = BossChargeStrikeDuration(full), recovery = BossChargeRecoveryDuration(full);
                    end = delay + cueTime + strike + recovery;
                    // Next warning is visible during the harmless return, but
                    // its expiry is safely later than the complete recovery.
                    delay += cueTime + strike + Math.Max(.1f, recovery - cueTime + .15f);
                }
                bossActionCycle = Math.Max(bossActionCycle, end + .2f / BossPracticePace);
                CurrentBoss.AttackName = AttackName(kind) + (casts > 1 ? " · " + casts + " lượt" : "");
                return true;
            }
            if (kind == BossAttackKind.CrossMissile)
            {
                float cueTime = Math.Max(1.2f * BossPracticeWarningScale, warning), last = 0f;
                if (IsFlyingBoss)
                {
                    float gapY = (bossAttackIndex / 7) % 2 == 0 ? 55f : 135f;
                    float gapHeight = BossAirGapHeight;
                    float low = Math.Max(8f, gapY * .5f - 12f);
                    float high = Math.Min(330f, gapY + gapHeight + (360f - gapY - gapHeight) * .5f - 12f);
                    int waves = BossTier == 1 ? (bossPatternRage >= 2 ? 2 : 1) : Math.Min(5, BossRepeatCount + (bossPatternRage >= 2 ? 1 : 0));
                    for (int wave = 0; wave < waves; wave++)
                    {
                        last = BossTier == 1 ? wave * .55f : BossCastSpacing(wave * .7f);
                        AddCrossMissileCue(last, cueTime, low, wave % 2 == 0, gapY, gapHeight);
                        AddCrossMissileCue(last, cueTime, high, wave % 2 != 0, gapY, gapHeight);
                    }
                }
                else if (BossTier == 1)
                {
                    AddCrossMissileCue(0f, cueTime, 18f, true, 0f, 0f);
                    AddCrossMissileCue(0f, cueTime, 18f, false, 0f, 0f);
                    if (bossPatternRage >= 3)
                    { last = .9f; AddCrossMissileCue(last, cueTime, 18f, bossAttackIndex % 2 == 0, 0f, 0f); }
                }
                else
                {
                    for (int wave = 0; wave < BossRepeatCount; wave++)
                    {
                        last = BossCastSpacing(wave * 1.3f);
                        AddCrossMissileCue(last, cueTime, 18f, true, 0f, 0f);
                        AddCrossMissileCue(last, cueTime, 18f, false, 0f, 0f);
                    }
                }
                bossActionCycle = Math.Max(bossActionCycle, 4.8f / BossPracticePace);
                if (BossTier > 1) ExtendBossCycle(last, cueTime, 3.4f / BossTierPace);
            }
            else if (kind == BossAttackKind.MissileRain)
            {
                float side = BossTier == 1 ? (bossAttackIndex % 2 == 0 ? -1f : 1f) : (random.Next(2) == 0 ? -1f : 1f);
                float centre = Math.Max(220f, Math.Min(620f, PlayerX + side * 140f));
                float gapWidth = Math.Max(210f, 240f - BossExtraWaves * 10f), gapX = centre - gapWidth * .5f;
                float cueTime = Math.Max(1.45f * BossPracticeWarningScale, warning), last = 0f;
                int waves = Math.Min(4, (bossPatternRage >= 3 ? 2 : 1) + BossExtraWaves);
                for (int wave = 0; wave < waves; wave++)
                {
                    for (int lane = 0; lane < 6; lane++)
                    {
                        float x = 110f + lane * 130f;
                        const float blastWidth = 62f;
                        if (x + blastWidth * .5f > gapX && x - blastWidth * .5f < gapX + gapWidth) continue;
                        float stagger = BossTier >= 3 ? ((wave % 2 == 0 ? lane : 5 - lane) * .055f) : 0f;
                        float delay = BossCastSpacing(wave * .8f + stagger);
                        last = Math.Max(last, delay);
                        BossTelegraph cue = AddSpecialCue(kind, delay, cueTime,
                            x - blastWidth * .5f, 0f, blastWidth, 430f);
                        cue.GapX = gapX; cue.GapWidth = gapWidth;
                        cue.OriginX = x - 13f; cue.OriginY = 430f;
                        cue.VelocityX = 0f; cue.VelocityY = -335f;
                    }
                }
                bossActionCycle = Math.Max(bossActionCycle, 4.9f / BossPracticePace);
                if (BossTier > 1) ExtendBossCycle(last, cueTime, 1.9f / BossTierPace);
            }
            else if (kind == BossAttackKind.ElectricOrb)
            {
                int count = Math.Min(6, (bossPatternRage >= 3 ? 3 : 2) + BossExtraWaves);
                float last = 0f;
                for (int i = 0; i < count; i++)
                {
                    float y = i % 2 == 0 ? 12f : 62f;
                    last = BossCastSpacing(i * 1.1f);
                    BossTelegraph cue = AddSpecialCue(kind, last, Math.Max(.95f * BossPracticeWarningScale, warning),
                        90f, y, 1000f, 36f);
                    cue.OriginX = CurrentBoss.X - 20f; cue.OriginY = y;
                    cue.VelocityX = -300f; cue.VelocityY = 0f;
                }
                bossActionCycle = Math.Max(bossActionCycle, 4.8f / BossPracticePace);
                if (BossTier > 1) ExtendBossCycle(last, Math.Max(.95f * BossPracticeWarningScale, warning), 2.8f / BossTierPace);
            }
            else return false;
            CurrentBoss.AttackName = AttackName(kind) + (BossTier > 1 ? " · liên hoàn cấp " + BossTier : "");
            return true;
        }

        private BossTelegraph AddSpecialCue(BossAttackKind kind, float delay, float duration,
            float x, float y, float width, float height)
        {
            BossTelegraph cue = new BossTelegraph
            {
                Id = ++nextBossObjectId, Kind = kind, X = x, Y = y, Width = width, Height = height,
                Duration = duration, Remaining = duration, DelayRemaining = Math.Max(0f, delay) / BossPracticePace
            };
            cue.SourceTelegraphId = cue.Id;
            bossTelegraphs.Add(cue);
            return cue;
        }

        private void AddCrossMissileCue(float delay, float warning, float y, bool fromLeft, float gapY, float gapHeight)
        {
            BossTelegraph cue = AddSpecialCue(BossAttackKind.CrossMissile, delay, warning, 90f, y, 1000f, 24f);
            cue.OriginX = fromLeft ? -30f : CanvasWidth + 40f;
            cue.OriginY = y;
            cue.VelocityX = fromLeft ? 320f : -320f;
            cue.VelocityY = 0f;
            cue.GapY = gapY; cue.GapHeight = gapHeight;
        }

        private bool SpawnSpecialBossAttack(BossTelegraph source, float pace)
        {
            if (source.Kind == BossAttackKind.TankRam || source.Kind == BossAttackKind.RobotDash || source.Kind == BossAttackKind.GunshipDive)
                BeginBossCharge(source);
            else if (source.Kind == BossAttackKind.CrossMissile || source.Kind == BossAttackKind.MissileRain || source.Kind == BossAttackKind.ElectricOrb)
            {
                bool orb = source.Kind == BossAttackKind.ElectricOrb;
                bossProjectiles.Add(new BossProjectile
                {
                    Id = ++nextBossObjectId, Kind = source.Kind, SourceTelegraphId = source.Id,
                    X = source.OriginX, Y = source.OriginY,
                    Width = orb ? 36f : source.Kind == BossAttackKind.MissileRain ? 26f : 40f,
                    Height = orb ? 36f : source.Kind == BossAttackKind.MissileRain ? 50f : 24f,
                    Radius = orb ? 18f : 0f,
                    VelocityX = source.VelocityX * pace, VelocityY = source.VelocityY * pace,
                    Lifetime = source.Kind == BossAttackKind.ElectricOrb ? 7f : 8f
                });
            }
            else return false;
            return true;
        }
    }
}
