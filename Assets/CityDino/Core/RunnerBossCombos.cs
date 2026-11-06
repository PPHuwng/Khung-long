using System;

namespace CityDino
{
    /// <summary>Seeded combined attacks: flank, redirect, then punish the old safe position.</summary>
    public sealed partial class RunnerSession
    {
        private int previousBossCombo = -1;
        private float BossBeamWarning
        {
            get { return Math.Max(.54f, .68f - .035f * (float)Math.Log(BossTier, 2d)) * BossPracticeWarningScale; }
        }

        private bool TryPlanBossCombo(float warning)
        {
            // The early practice tiers retain isolated drills. Scored C3+
            // combines skills twice, then offers an ordinary family pattern.
            if (BossTier < 3 || bossAttackIndex % 3 == 2) return false;
            int variant = random.Next(4);
            if (variant == previousBossCombo) variant = (variant + 1 + random.Next(3)) % 4;
            previousBossCombo = variant;
            int waves = Math.Min(5, 2 + (BossTier - 3) / 2 + (bossPatternRage >= 3 ? 1 : 0));
            float spacing = BossCastSpacing(IsFreeMovementBoss ? 2.2f : 2.35f);
            int firstCue = bossTelegraphs.Count;
            if (CurrentBoss.Kind == BossKind.Tank) PlanTankCombo(variant, waves, spacing, warning);
            else if (CurrentBoss.Kind == BossKind.Sentinel) PlanRobotCombo(variant, waves, spacing, warning);
            else if (CurrentBoss.Kind == BossKind.Gunship) PlanAircraftCombo(variant, waves, spacing, warning);
            else PlanNavalCombo(variant, waves, spacing, warning);
            // A combo has several kinds of attacks, not repeated copies of a
            // single lane. Leave finite flight time, not a long empty cooldown.
            float last = 0f;
            for (int i = firstCue; i < bossTelegraphs.Count; i++)
                last = Math.Max(last, bossTelegraphs[i].DelayRemaining * BossPracticePace);
            // Charge variants schedule one lunge and a followup, not "waves"
            // lunges. Basing their recovery on the unused wave count left an
            // increasingly long idle window at high tiers.
            ExtendBossCycle(last, Math.Max(warning, BossBeamWarning),
                (IsFreeMovementBoss ? 3.1f : 2.7f) / BossTierPace);
            return true;
        }

        private void PlanTankCombo(int variant, int waves, float spacing, float warning)
        {
            primaryBossAttack = variant == 3 ? BossAttackKind.TankRam : variant == 2 ? BossAttackKind.MineDrop : BossAttackKind.Cannon;
            CurrentBoss.AttackName = variant == 0 ? "Hỏa lực kẹp · pháo và tên lửa sườn"
                : variant == 1 ? "Đạn xiên · pháo khóa đường lui" : variant == 2 ? "Bãi mìn · pháo phủ truy kích" : "Xung phong · yểm trợ từ phía sau";
            if (variant == 3)
            {
                PlanComboCharge(BossAttackKind.TankRam, 0f, 0f);
                float after = 3.05f;
                AddAimedComboShot(BossAttackKind.CrossMissile, after, warning, -80f, 125f, 300f, 50f, 0f);
                ScheduleBossAttack(BossAttackKind.Artillery, after + .55f, warning, PlayerX);
                ExtendBossCycle(after + .55f, warning, 2.2f);
                return;
            }
            for (int i = 0; i < waves; i++)
            {
                float delay = i * spacing;
                if (variant == 0)
                {
                    AddFixedComboShot(BossAttackKind.Cannon, delay, warning, CanvasWidth + 20f, 14f, -400f, 0f);
                    AddAimedComboShot(BossAttackKind.CrossMissile, delay + .45f, warning, -80f, 150f, 285f, 48f, i % 2 == 0 ? 50f : -50f);
                    AddComboArtillery(delay + .9f, warning, i % 2 == 0 ? 95f : -95f);
                }
                else if (variant == 1)
                {
                    AddAimedComboShot(BossAttackKind.CrossMissile, delay, warning, CanvasWidth + 25f, 255f, 380f, 65f, 0f);
                    AddFixedComboShot(BossAttackKind.Cannon, delay + .5f, warning, CanvasWidth + 25f, i % 2 == 0 ? 14f : 104f, -350f, 0f);
                    AddComboArtillery(delay + 1f, warning, i % 2 == 0 ? -120f : 120f);
                }
                else
                {
                    ScheduleBossAttack(BossAttackKind.MineDrop, delay, warning, PlayerX);
                    AddAimedComboShot(BossAttackKind.CrossMissile, delay + .7f, warning,
                        i % 2 == 0 ? -80f : CanvasWidth + 30f, 240f, 310f, 70f, 0f);
                    AddComboArtillery(delay + 1.2f, warning, i % 2 == 0 ? 100f : -100f);
                }
            }
        }

        private void PlanRobotCombo(int variant, int waves, float spacing, float warning)
        {
            primaryBossAttack = variant == 3 ? BossAttackKind.RobotDash : BossAttackKind.ElectricOrb;
            CurrentBoss.AttackName = variant == 0 ? "Laser đổi nhịp · lôi cầu sau lưng"
                : variant == 1 ? "Tam giác điện · khóa vị trí" : variant == 2 ? "Sóng dậm · lôi cầu chéo" : "Quyền chớp · phản kích sườn";
            if (variant == 3)
            {
                PlanComboCharge(BossAttackKind.RobotDash, 0f, 0f);
                AddFixedComboShot(BossAttackKind.ElectricOrb, 3.05f, warning, -70f, 16f, 290f, 0f);
                AddComboPillar(3.65f, warning, 110f);
                ExtendBossCycle(3.65f, warning, 1.8f);
                return;
            }
            for (int i = 0; i < waves; i++)
            {
                float delay = i * spacing;
                if (variant == 0)
                {
                    ScheduleBossAttack(i % 2 == 0 ? BossAttackKind.LaserLow : BossAttackKind.LaserHigh,
                        delay, BossBeamWarning, PlayerX);
                    AddAimedComboShot(BossAttackKind.ElectricOrb, delay + .75f, warning, -70f, 230f, 280f, 50f, 0f);
                }
                else if (variant == 1)
                {
                    AddAimedComboShot(BossAttackKind.ElectricOrb, delay, warning, CanvasWidth + 30f, 240f, 340f, 55f, -70f);
                    AddComboPillar(delay + .8f, warning, i % 2 == 0 ? 100f : -100f);
                    AddFixedComboShot(BossAttackKind.ElectricOrb, delay + 1.25f, warning, -70f, 106f, 280f, 0f);
                }
                else
                {
                    ScheduleBossAttack(BossAttackKind.Shockwave, delay, warning, PlayerX);
                    AddAimedComboShot(BossAttackKind.ElectricOrb, delay + .8f, warning, -80f, 230f, 285f, 45f, 65f);
                    ScheduleBossAttack(BossAttackKind.ShieldDrone, delay + 1.3f, warning, PlayerX);
                }
            }
        }

        private void PlanAircraftCombo(int variant, int waves, float spacing, float warning)
        {
            primaryBossAttack = variant == 3 ? BossAttackKind.GunshipDive : BossAttackKind.AirSpread;
            CurrentBoss.AttackName = variant == 0 ? "Cổng lệch tầng · tên lửa xiên"
                : variant == 1 ? "Gọng kìm không chiến · đạn quạt"
                : variant == 2 ? "Laser phản ứng · mìn khóa tầng" : "Bổ nhào · truy kích từ hậu tuyến";
            if (variant == 3)
            {
                PlanComboCharge(BossAttackKind.GunshipDive, 0f, PlayerY + 12f);
                AddAimedComboShot(BossAttackKind.AirSpread, 3.05f, warning, -80f, 290f, 280f, 42f, 0f);
                AddAimedComboShot(BossAttackKind.CrossMissile, 3.8f, warning, CanvasWidth + 30f, 30f, 340f, 42f, 0f);
                ExtendBossCycle(3.8f, warning, 2.4f);
                return;
            }
            for (int i = 0; i < waves; i++)
            {
                float delay = i * spacing;
                if (variant == 0)
                {
                    float gap = 45f + random.Next(0, 4) * 30f;
                    ScheduleBossAttack(BossAttackKind.AirGate, delay, warning, gap);
                    AddAimedComboShot(BossAttackKind.CrossMissile, delay + .9f, warning, -80f,
                        i % 2 == 0 ? 30f : 325f, 285f, 42f, 0f);
                }
                else if (variant == 1)
                {
                    AddAimedComboShot(BossAttackKind.AirSpread, delay, warning, CanvasWidth + 30f,
                        i % 2 == 0 ? 315f : 30f, 325f, 42f, 0f);
                    AddAimedComboShot(BossAttackKind.CrossMissile, delay + .85f, warning, -80f,
                        i % 2 == 0 ? 35f : 315f, 270f, 42f, 0f);
                }
                else
                {
                    AddComboBeam(BossAttackKind.AirLaser, delay, 42f);
                    ScheduleBossAttack(BossAttackKind.AirMine, delay + .9f, warning, i % 2 == 0 ? 90f : 255f);
                    AddAimedComboShot(BossAttackKind.CrossMissile, delay + 1.3f, warning, -80f,
                        i % 2 == 0 ? 320f : 25f, 285f, 42f, 70f);
                }
            }
        }

        private void PlanNavalCombo(int variant, int waves, float spacing, float warning)
        {
            primaryBossAttack = variant == 3 ? BossAttackKind.SubmarineRam : BossAttackKind.Torpedo;
            CurrentBoss.AttackName = variant == 0 ? "Ngư lôi xiên · đàn cá truy kích"
                : variant == 1 ? "Thủy lôi kẹp · sonar phản ứng"
                : variant == 2 ? "Bom chìm · ngư lôi từ phía sau" : "Đột kích sâu · cá máy bọc hậu";
            if (variant == 3)
            {
                PlanComboCharge(BossAttackKind.SubmarineRam, 0f, PlayerY + 12f);
                AddAimedComboShot(BossAttackKind.FishShoal, 3.1f, warning, -90f, 300f, 260f, 42f, 0f);
                AddComboBeam(BossAttackKind.SonarBeam, 4.1f, 42f);
                ExtendBossCycle(4.1f, BossBeamWarning, 1.8f);
                return;
            }
            for (int i = 0; i < waves; i++)
            {
                float delay = i * spacing;
                if (variant == 0)
                {
                    AddAimedComboShot(BossAttackKind.Torpedo, delay, warning, CanvasWidth + 30f, i % 2 == 0 ? 300f : 20f, 330f, 42f, 0f);
                    AddAimedComboShot(BossAttackKind.FishShoal, delay + .95f, warning, -90f, i % 2 == 0 ? 20f : 300f, 255f, 42f, 50f);
                }
                else if (variant == 1)
                {
                    // Two mines bracket a movable 260px corridor. The sonar
                    // changes depth, so camping inside that corridor is unsafe.
                    float centre = Math.Max(260f, Math.Min(620f, PlayerX + (i % 2 == 0 ? 90f : -90f)));
                    for (int side = -1; side <= 1; side += 2)
                    {
                        float x = centre + side * 160f;
                        BossTelegraph cue = AddSpecialCue(BossAttackKind.SeaMine, delay, warning, x - 19f, 0f, 38f, 360f);
                        cue.OriginX = x - 19f; cue.OriginY = 8f; cue.VelocityY = 78f;
                        cue.GapX = centre - 130f; cue.GapWidth = 260f;
                    }
                    AddComboBeam(BossAttackKind.SonarBeam, delay + .95f, 42f);
                }
                else
                {
                    float centre = Math.Max(230f, Math.Min(640f, PlayerX));
                    foreach (float x in new[] { centre - 155f, centre + 155f })
                    {
                        BossTelegraph cue = AddSpecialCue(BossAttackKind.DepthCharge, delay, Math.Max(warning, .6f * BossPracticeWarningScale), x - 32f, 0f, 64f, 430f);
                        cue.OriginX = x - 14f; cue.OriginY = 420f; cue.VelocityY = -225f;
                        cue.GapX = centre - 120f; cue.GapWidth = 240f;
                    }
                    AddAimedComboShot(BossAttackKind.Torpedo, delay + .8f, warning, -90f, i % 2 == 0 ? 300f : 20f, 285f, 42f, 0f);
                }
            }
        }

        private BossTelegraph AddFixedComboShot(BossAttackKind kind, float delay, float warning,
            float x, float y, float vx, float vy)
        {
            BossTelegraph cue = AddSpecialCue(kind, delay, warning, x, y, 40f, 30f);
            cue.OriginX = x; cue.OriginY = y; cue.VelocityX = vx; cue.VelocityY = vy;
            return cue;
        }

        private void AddAimedComboShot(BossAttackKind kind, float delay, float warning,
            float x, float y, float speed, float aimHeight, float aimOffsetX)
        {
            BossTelegraph cue = AddFixedComboShot(kind, delay, warning, x, y, speed, 0f);
            cue.LockOnReveal = true; cue.AimOffsetX = aimOffsetX; cue.AimOffsetY = aimHeight;
            SnapshotBossComboAim(cue);
        }

        private void AddComboArtillery(float delay, float warning, float offsetX)
        {
            ScheduleBossAttack(BossAttackKind.Artillery, delay, warning, PlayerX + offsetX);
            BossTelegraph cue = bossTelegraphs[bossTelegraphs.Count - 1];
            cue.LockOnReveal = true; cue.AimOffsetX = offsetX;
        }

        private void AddComboPillar(float delay, float warning, float offsetX)
        {
            ScheduleBossAttack(BossAttackKind.ElectricPillar, delay, warning, PlayerX + offsetX);
            BossTelegraph cue = bossTelegraphs[bossTelegraphs.Count - 1];
            cue.LockOnReveal = true; cue.AimOffsetX = offsetX;
        }

        private void AddComboBeam(BossAttackKind kind, float delay, float offsetY)
        {
            BossTelegraph cue = AddSpecialCue(kind, delay, BossBeamWarning, 75f,
                Math.Max(35f, Math.Min(325f, PlayerY + offsetY)), 760f, 14f);
            cue.LockOnReveal = true; cue.AimOffsetY = offsetY;
        }

        private void PlanComboCharge(BossAttackKind kind, float delay, float y)
        {
            bool full = random.Next(2) == 0;
            float endpoint = full ? -CurrentBoss.Width - 24f : Math.Max(100f, Math.Min(500f, PlayerX - 170f));
            float height = kind == BossAttackKind.TankRam ? 68f : kind == BossAttackKind.RobotDash ? 110f : 70f;
            float left = Math.Min(90f, endpoint + 8f);
            BossTelegraph cue = AddSpecialCue(kind, delay, 1.05f * BossPracticeWarningScale,
                left, Math.Max(0f, Math.Min(280f, y)), CanvasWidth - left, height);
            cue.OriginX = CurrentBoss.X; cue.OriginY = CurrentBoss.Y;
            cue.AimX = cue.SweepEndX = endpoint; cue.AimY = y; cue.FullSweep = full;
            ExtendBossCycle(delay, cue.Duration, BossChargeStrikeDuration(full) + BossChargeRecoveryDuration(full) + .3f);
        }

        private void LockBossComboAim(BossTelegraph cue)
        {
            if (!cue.LockOnReveal || !cue.Visible || cue.Active) return;
            SnapshotBossComboAim(cue);
            cue.LockOnReveal = false;
        }

        private void SnapshotBossComboAim(BossTelegraph cue)
        {
            cue.AimX = Math.Max(120f, Math.Min(700f, PlayerX + cue.AimOffsetX));
            cue.AimY = Math.Max(30f, Math.Min(325f, PlayerY + cue.AimOffsetY));
            if (cue.Kind == BossAttackKind.AirLaser || cue.Kind == BossAttackKind.SonarBeam)
                cue.Y = BossBeamLaneY(cue.AimY, cue.Duration);
            else if (cue.Kind == BossAttackKind.Artillery || cue.Kind == BossAttackKind.ElectricPillar)
                cue.X = Math.Max(90f, Math.Min(710f - cue.Width, cue.AimX - cue.Width * .5f));
            else
            {
                float speed = (float)Math.Sqrt(cue.VelocityX * cue.VelocityX + cue.VelocityY * cue.VelocityY);
                float dx = cue.AimX - cue.OriginX, dy = cue.AimY - cue.OriginY;
                float length = Math.Max(1f, (float)Math.Sqrt(dx * dx + dy * dy));
                cue.VelocityX = dx / length * speed; cue.VelocityY = dy / length * speed;
            }
        }

        // A short beam must not trap a rising swimmer against the ceiling.
        // Keep the locked aim unless neither vertical escape can clear it;
        // then move the lane just enough to leave one reachable side.
        private float BossBeamLaneY(float desired, float warning)
        {
            float lane = Math.Max(35f, Math.Min(325f, desired));
            if (!IsFreeMovementBoss) return lane;
            float minimum = float.MaxValue, maximum = float.MinValue;
            for (int direction = -1; direction <= 1; direction += 2)
            {
                float y = PlayerY, velocity = VelocityY;
                const float step = 1f / 120f;
                for (float time = 0f; time < warning; time += step)
                {
                    bool responding = time >= .12f;
                    if (IsUnderwaterBoss)
                    {
                        float force = responding ? direction < 0 ? -580f : 420f
                            : swimDiveHeld ? -580f : swimLiftHeld ? 420f : -230f;
                        velocity += force * step;
                        velocity *= 1f - 1.5f * step;
                        velocity = Math.Max(-220f, Math.Min(200f, velocity));
                    }
                    else
                    {
                        bool lift = responding ? direction > 0 : flightLiftHeld;
                        velocity += (-FlightGravity + (lift ? FlightThrust : 0f)) * step;
                        velocity = Math.Max(-260f, Math.Min(230f, velocity));
                    }
                    y += velocity * step;
                    if (y < FlightMinimumY) { y = FlightMinimumY; velocity = Math.Max(0f, velocity); }
                    if (y > FlightMaximumY) { y = FlightMaximumY; velocity = Math.Min(0f, velocity); }
                }
                minimum = Math.Min(minimum, y); maximum = Math.Max(maximum, y);
            }
            if (minimum + 100f < lane || maximum > lane + 20f) return lane;
            float above = minimum + 102f, below = maximum - 22f;
            if (above <= 325f && (below < 35f || Math.Abs(above - lane) <= Math.Abs(below - lane))) return above;
            return Math.Max(35f, Math.Min(325f, below));
        }

        private bool SpawnComboProjectile(BossTelegraph source, float pace)
        {
            if ((source.Kind != BossAttackKind.Cannon && source.Kind != BossAttackKind.AirSpread) ||
                (source.VelocityX == 0f && source.VelocityY == 0f)) return false;
            int halfFan = source.Kind == BossAttackKind.AirSpread ? 1 : 0;
            for (int i = -halfFan; i <= halfFan; i++)
                bossProjectiles.Add(new BossProjectile {
                    Id = ++nextBossObjectId, Kind = source.Kind, X = source.OriginX, Y = source.OriginY,
                    Width = halfFan > 0 ? 18f : 30f, Height = halfFan > 0 ? 12f : 22f,
                    Radius = halfFan > 0 ? 6f : 11f, VelocityX = source.VelocityX * pace,
                    VelocityY = (source.VelocityY + i * 76f) * pace, Lifetime = 7f, SourceTelegraphId = source.Id
                });
            CurrentBoss.ShotFlashRemaining = .18f; CurrentBoss.Recoil = 1f;
            return true;
        }
    }
}
