using System;
using System.Collections.Generic;

namespace CityDino
{
    /// <summary>Telegraphed attack plans and finite physical obstacles for each arena theme.</summary>
    public sealed partial class RunnerSession
    {
        private readonly List<BossObstacle> bossObstacles = new List<BossObstacle>();
        public IReadOnlyList<BossObstacle> BossObstacles { get { return bossObstacles; } }

        private void ResetBossPatternState() { bossObstacles.Clear(); bossAttackBag.Clear(); previousBossPlan = previousBossCombo = -1; }

        private readonly List<int> bossAttackBag = new List<int>();
        private BossKind bossPlanKind;
        private int previousBossPlan = -1;

        private int ChooseBossAttack(int count)
        {
            if (BossTier == 1) return bossAttackIndex % count;
            if (bossPlanKind != CurrentBoss.Kind)
            { bossPlanKind = CurrentBoss.Kind; bossAttackBag.Clear(); previousBossPlan = -1; }
            if (bossAttackBag.Count == 0)
            {
                for (int i = 0; i < count; i++) bossAttackBag.Add(i);
                for (int i = count - 1; i > 0; i--)
                {
                    int swap = random.Next(i + 1), value = bossAttackBag[i];
                    bossAttackBag[i] = bossAttackBag[swap]; bossAttackBag[swap] = value;
                }
                if (count > 1 && bossAttackBag[count - 1] == previousBossPlan)
                { int value = bossAttackBag[count - 1]; bossAttackBag[count - 1] = bossAttackBag[0]; bossAttackBag[0] = value; }
            }
            int last = bossAttackBag.Count - 1;
            int choice = bossAttackBag[last]; bossAttackBag.RemoveAt(last); previousBossPlan = choice;
            return choice;
        }

        private void BeginBossAttack()
        {
            bossPatternRage = GetBossRage(CurrentBoss.Health, CurrentBoss.MaxHealth);
            CurrentBoss.RageLevel = bossPatternRage;
            float basicCycle = IsFreeMovementBoss
                ? (bossPatternRage == 1 ? 4.6f : bossPatternRage == 2 ? 4.2f : 3.8f)
                : (bossPatternRage == 1 ? 3.9f : bossPatternRage == 2 ? 3.5f : 3.2f);
            bossActionCycle = basicCycle / (BossPracticePace * BossTierTempo);
            float warning = Math.Max(.75f, 1.02f - (bossPatternRage - 1) * .09f
                - Math.Min(.12f, (BossTier - 1) * .015f)) * BossPracticeWarningScale;
            if (BossTier >= 3)
                warning = Math.Max(.45f, .62f - .025f * (bossPatternRage - 1)
                    - .018f * (float)Math.Log(BossTier, 2d)) * BossPracticeWarningScale;
            CurrentBoss.Vulnerable = false;
            if (TryPlanBossCombo(warning)) return;
            if (CurrentBoss.Kind == BossKind.Tank) PlanTankPattern(warning);
            else if (CurrentBoss.Kind == BossKind.Sentinel) PlanRobotPattern(warning);
            else if (CurrentBoss.Kind == BossKind.Submarine) PlanSubmarinePattern(warning);
            else PlanAircraftPattern(warning);
        }

        private void PlanTankPattern(float warning)
        {
            int choice = ChooseBossAttack(7);
            primaryBossAttack = choice == 0 ? BossAttackKind.Cannon : choice == 1 ? BossAttackKind.MineDrop
                : choice == 2 ? BossAttackKind.Artillery : choice == 3 ? BossAttackKind.CrateDrop
                : choice == 4 ? BossAttackKind.TankRam : choice == 5 ? BossAttackKind.CrossMissile : BossAttackKind.MissileRain;
            if (PlanBossSpecial(primaryBossAttack, warning)) return;
            if (primaryBossAttack == BossAttackKind.Cannon)
            {
                int count = bossPatternRage + 1;
                int bursts = BossRepeatCount;
                float burstSpacing = BossCastSpacing(count * .28f + .75f), last = 0f;
                for (int burst = 0; burst < bursts; burst++)
                    for (int shot = 0; shot < count; shot++)
                    {
                        last = burst * burstSpacing + BossCastSpacing(shot * .28f);
                        ScheduleBossAttack(BossAttackKind.Cannon, last, warning, PlayerX);
                    }
                if (BossTier > 1) ExtendBossCycle(last, warning, 2.15f / BossTierPace);
                CurrentBoss.AttackName = bursts > 1 ? "Pháo liên thanh · " + bursts + " loạt" : "Loạt pháo " + count + " viên";
            }
            else if (primaryBossAttack == BossAttackKind.Artillery)
            {
                float centre = Math.Max(180f, Math.Min(640f, PlayerX)), last = 0f;
                int bursts = BossRepeatCount;
                for (int burst = 0; burst < bursts; burst++)
                    for (int i = 0; i < bossPatternRage; i++)
                    {
                        last = BossCastSpacing(burst * 1.8f + i * .4f);
                        ScheduleBossAttack(BossAttackKind.Artillery, last, warning,
                            centre + (i == 0 ? 0f : i % 2 == 1 ? 140f : -140f));
                    }
                if (BossTier > 1) ExtendBossCycle(last, warning, 1.45f / BossTierPace);
                CurrentBoss.AttackName = bursts > 1 ? "Pháo phủ truy kích · " + bursts + " đợt" : "Pháo phủ vị trí · " + bossPatternRage + " điểm";
            }
            else
            {
                int count = primaryBossAttack == BossAttackKind.MineDrop ? Math.Min(2, bossPatternRage) + BossExtraWaves
                    : Math.Min(5, BossRepeatCount);
                float spacing = primaryBossAttack == BossAttackKind.MineDrop ? BossCastSpacing(.92f) : BossCastSpacing(1.35f), last = 0f;
                // Preserve the first tour's mine rhythm.
                if (BossTier == 1) spacing = .62f;
                for (int i = 0; i < count; i++)
                { last = i * spacing; ScheduleBossAttack(primaryBossAttack, last, warning, PlayerX); }
                if (primaryBossAttack == BossAttackKind.CrateDrop && bossPatternRage >= 2)
                    ScheduleBossAttack(BossAttackKind.MineDrop, last + 1.1f, warning, PlayerX);
                if (BossTier > 1) ExtendBossCycle(last + (bossPatternRage >= 2 ? 1.1f : 0f), warning, 3.3f / BossTierPace);
                CurrentBoss.AttackName = AttackName(primaryBossAttack) + (count > 1 ? " · " + count + " đợt" : "");
            }
        }

        private void PlanRobotPattern(float warning)
        {
            int choice = ChooseBossAttack(8);
            primaryBossAttack = choice == 0 ? BossAttackKind.LaserLow : choice == 1 ? BossAttackKind.ShieldDrone
                : choice == 2 ? BossAttackKind.LaserHigh : choice == 3 ? BossAttackKind.ElectricPillar
                : choice == 4 ? BossAttackKind.Shockwave : choice == 5 ? BossAttackKind.RobotDash
                : choice == 6 ? BossAttackKind.ElectricOrb : BossAttackKind.CrossMissile;
            if (PlanBossSpecial(primaryBossAttack, warning)) return;
            int casts = primaryBossAttack == BossAttackKind.LaserLow || primaryBossAttack == BossAttackKind.LaserHigh
                ? Math.Max(bossPatternRage >= 2 ? 2 : 1, BossRepeatCount) : BossRepeatCount;
            float last = 0f;
            for (int i = 0; i < casts; i++)
            {
                BossAttackKind kind = primaryBossAttack;
                if (primaryBossAttack == BossAttackKind.LaserLow || primaryBossAttack == BossAttackKind.LaserHigh)
                {
                    if (i % 2 != 0) kind = primaryBossAttack == BossAttackKind.LaserLow ? BossAttackKind.LaserHigh : BossAttackKind.LaserLow;
                    last = BossCastSpacing(i * 1.2f);
                }
                else last = BossCastSpacing(i * (primaryBossAttack == BossAttackKind.ElectricPillar ? 1.9f : 1.35f));
                float lockX = primaryBossAttack == BossAttackKind.ElectricPillar && i % 2 != 0 ? Math.Min(660f, PlayerX + 145f) : PlayerX;
                ScheduleBossAttack(kind, last, warning, lockX);
            }
            if (BossTier == 1 && primaryBossAttack == BossAttackKind.Shockwave && bossPatternRage >= 2)
            { last = 1.15f; ScheduleBossAttack(BossAttackKind.Shockwave, last, warning, PlayerX); }
            else if (BossTier == 1 && primaryBossAttack == BossAttackKind.ElectricPillar && bossPatternRage >= 3)
            { last = .45f; ScheduleBossAttack(BossAttackKind.ElectricPillar, last, warning, Math.Min(660f, PlayerX + 145f)); }
            if (BossTier > 1) ExtendBossCycle(last, warning,
                primaryBossAttack == BossAttackKind.Shockwave || primaryBossAttack == BossAttackKind.ShieldDrone ? 2.8f / BossTierPace : 1.6f);
            CurrentBoss.AttackName = AttackName(primaryBossAttack) + (casts > 1 ? " · " + casts + " nhịp" : "");
        }

        private void PlanAircraftPattern(float warning)
        {
            int choice = ChooseBossAttack(7);
            primaryBossAttack = choice == 0 ? BossAttackKind.AirGate : choice == 1 ? BossAttackKind.AirSpread
                : choice == 2 ? BossAttackKind.AirMine : choice == 3 ? BossAttackKind.AirLaser
                : choice == 4 ? BossAttackKind.GunshipDive : choice == 5 ? BossAttackKind.CrossMissile : BossAttackKind.MissileRain;
            if (PlanBossSpecial(primaryBossAttack, warning)) return;
            int casts = primaryBossAttack == BossAttackKind.AirMine ? Math.Min(7, (bossPatternRage >= 2 ? 2 : 1) + BossExtraWaves) : BossRepeatCount;
            float last = 0f;
            for (int i = 0; i < casts; i++)
            {
                float locked;
                if (primaryBossAttack == BossAttackKind.AirGate)
                {
                    locked = ((bossAttackIndex / 7 + i) % 2 == 0) ? 55f : 135f;
                    last = BossCastSpacing(i * 2.6f);
                }
                else if (primaryBossAttack == BossAttackKind.AirMine)
                { locked = 95f + ((bossAttackIndex + i) % 3) * 80f; last = BossCastSpacing(i * .95f); }
                else if (primaryBossAttack == BossAttackKind.AirSpread)
                { locked = PlayerY + 45f + (i == 0 ? 0f : i % 2 == 1 ? 70f : -70f); last = BossCastSpacing(i * 2.2f); }
                else
                { locked = PlayerY + 42f + (i == 0 ? 0f : i % 2 == 1 ? 100f : -100f); last = BossCastSpacing(i * 1.55f); }
                if (BossTier == 1 && primaryBossAttack == BossAttackKind.AirMine) last = i * .6f;
                ScheduleBossAttack(primaryBossAttack, last,
                    primaryBossAttack == BossAttackKind.AirLaser ? Math.Max(warning, 1.2f * BossPracticeWarningScale) : warning, locked);
            }
            if (BossTier > 1) ExtendBossCycle(last,
                primaryBossAttack == BossAttackKind.AirLaser ? Math.Max(warning, 1.2f * BossPracticeWarningScale) : warning,
                primaryBossAttack == BossAttackKind.AirLaser ? .8f : 3f / BossTierPace);
            CurrentBoss.AttackName = AttackName(primaryBossAttack) + (casts > 1 ? " · " + casts + " đợt" : "");
        }

        private void ScheduleBossAttack(BossAttackKind kind, float delay, float warning, float lockedPosition)
        {
            if (BossTier >= 3 && (kind == BossAttackKind.LaserLow || kind == BossAttackKind.LaserHigh || kind == BossAttackKind.AirLaser))
                warning = BossBeamWarning;
            BossTelegraph mark = new BossTelegraph
            {
                Id = ++nextBossObjectId, Kind = kind, Remaining = warning, Duration = warning,
                DelayRemaining = Math.Max(0f, delay) / BossPracticePace,
                X = 90f, Y = 12f, Width = 690f, Height = 24f
            };
            mark.SourceTelegraphId = mark.Id;
            if (kind == BossAttackKind.Artillery || kind == BossAttackKind.Missile)
            {
                mark.Width = kind == BossAttackKind.Artillery ? 100f : 78f;
                mark.X = Math.Max(MinimumPlayerX - 20f, Math.Min(MaximumPlayerX - mark.Width + 20f, lockedPosition - mark.Width * .5f));
                mark.Y = 0f; mark.Height = 14f;
            }
            else if (kind == BossAttackKind.LaserLow || kind == BossAttackKind.LaserHigh)
            { mark.X = 75f; mark.Width = 760f; mark.Y = kind == BossAttackKind.LaserHigh ? 62f : 13f; mark.Height = 14f; }
            else if (kind == BossAttackKind.Shockwave) { mark.Y = 0f; mark.Height = 25f; }
            else if (kind == BossAttackKind.Drone) { mark.Y = 56f; mark.Height = 32f; }
            else if (kind == BossAttackKind.DroneLow) { mark.Y = 8f; mark.Height = 26f; }
            else if (kind == BossAttackKind.MineDrop) { mark.Y = 0f; mark.Height = 32f; }
            else if (kind == BossAttackKind.CrateDrop) { mark.Y = 0f; mark.Height = 68f; }
            else if (kind == BossAttackKind.ElectricPillar)
            {
                mark.Width = 62f; mark.Height = 142f; mark.Y = 0f;
                mark.X = Math.Max(90f, Math.Min(710f - mark.Width, lockedPosition - mark.Width * .5f));
            }
            else if (kind == BossAttackKind.ShieldDrone) { mark.Y = 62f; mark.Height = 46f; }
            else if (kind == BossAttackKind.AirGate)
            {
                mark.X = 90f; mark.Y = 0f; mark.Width = 690f; mark.Height = 360f;
                mark.GapY = Math.Max(45f, Math.Min(155f, lockedPosition)); mark.GapHeight = BossAirGapHeight;
            }
            else if (kind == BossAttackKind.AirSpread)
            {
                // Keep the target fixed throughout the warning. Moving after
                // the cue must evade the volley rather than redirect it.
                mark.X = PlayerX; mark.Width = 0f;
                mark.GapY = Math.Max(55f, Math.Min(300f, lockedPosition));
                mark.GapHeight = 74f; // vertical velocity separation of the fan
                mark.Y = 20f; mark.Height = 340f;
            }
            else if (kind == BossAttackKind.AirMine)
            { mark.X = 90f; mark.Width = 690f; mark.Y = Math.Max(45f, Math.Min(300f, lockedPosition)) - 17f; mark.Height = 34f; }
            else if (kind == BossAttackKind.AirLaser)
            { mark.X = 75f; mark.Width = 760f; mark.Y = BossBeamLaneY(lockedPosition,warning); mark.Height = 14f; }
            bossTelegraphs.Add(mark);
        }

        private static string AttackName(BossAttackKind kind)
        {
            switch (kind)
            {
                case BossAttackKind.Artillery: return "Pháo khóa vị trí";
                case BossAttackKind.LaserLow: return "Laser thấp";
                case BossAttackKind.LaserHigh: return "Laser trên cao";
                case BossAttackKind.Shockwave: return "Sóng dậm đất";
                case BossAttackKind.Missile: return "Tên lửa truy kích";
                case BossAttackKind.Drone: return "Drone trên cao";
                case BossAttackKind.DroneLow: return "Drone thấp";
                case BossAttackKind.MineDrop: return "Mìn lăn · nhảy hoặc bắn phá";
                case BossAttackKind.CrateDrop: return "Drone xung kích · bắn phá";
                case BossAttackKind.ElectricPillar: return "Cột điện khóa vị trí";
                case BossAttackKind.ShieldDrone: return "Robot chắn đạn";
                case BossAttackKind.AirGate: return "Cổng năng lượng · bay qua khe";
                case BossAttackKind.AirSpread: return "Loạt đạn quạt trên không";
                case BossAttackKind.AirMine: return "Mìn bay · né hoặc bắn phá";
                case BossAttackKind.AirLaser: return "Laser khóa độ cao";
                case BossAttackKind.TankRam: return "Thiết giáp xung phong · nhảy qua mũi húc";
                case BossAttackKind.RobotDash: return "Quyền điện đột kích · nhảy né đòn";
                case BossAttackKind.GunshipDive: return "Bổ nhào · rời khỏi làn đỏ";
                case BossAttackKind.CrossMissile: return "Tên lửa gọng kìm · trước và sau";
                case BossAttackKind.MissileRain: return "Mưa tên lửa · tìm hành lang an toàn";
                case BossAttackKind.ElectricOrb: return "Lôi cầu liên hoàn";
                case BossAttackKind.Torpedo: return "Ngư lôi gọng kìm";
                case BossAttackKind.FishShoal: return "Triệu hồi đàn cá máy";
                case BossAttackKind.SeaMine: return "Thủy lôi nổi · bắn phá hoặc tránh cột";
                case BossAttackKind.SonarBeam: return "Tia sonar khóa độ sâu";
                case BossAttackKind.DepthCharge: return "Bom chìm · tìm hành lang trống";
                case BossAttackKind.SubmarineRam: return "Tàu ngầm đột kích · đổi độ sâu";
                default: return "Đạn pháo thấp";
            }
        }

        private void TickBossTelegraphs(float dt)
        {
            for (int i = bossTelegraphs.Count - 1; i >= 0 && State == RunState.Playing && ArenaPhase == BossPhase.Fighting; i--)
            {
                BossTelegraph mark = bossTelegraphs[i];
                float activeDt = dt;
                if (mark.DelayRemaining > 0f)
                {
                    float consumed = Math.Min(mark.DelayRemaining, activeDt);
                    mark.DelayRemaining -= consumed; activeDt -= consumed;
                    if (activeDt <= 0f) continue;
                }
                LockBossComboAim(mark);
                mark.Remaining -= activeDt;
                if (mark.Active)
                {
                    if (mark.Remaining <= 0f) { bossTelegraphs.RemoveAt(i); continue; }
                    if (InvincibleRemaining <= 0f && RunnerGeometry.RectTouchesPlayer(mark.X, mark.Y, mark.Width, mark.Height, this))
                        if (!TryAbsorbHazard(false)) TakeHit("Trúng " + AttackName(mark.Kind));
                }
                else if (mark.Remaining <= 0f)
                {
                    if (mark.Kind == BossAttackKind.LaserLow || mark.Kind == BossAttackKind.LaserHigh || mark.Kind == BossAttackKind.AirLaser || mark.Kind == BossAttackKind.SonarBeam)
                    {
                        mark.Active = true; mark.Remaining = mark.Duration = .32f;
                        CurrentBoss.ShotFlashRemaining = .18f; CurrentBoss.Recoil = 1f;
                    }
                    else
                    {
                        SpawnBossAttack(mark);
                        bossTelegraphs.RemoveAt(i);
                    }
                }
            }
        }

        private void SpawnBossAttack(BossTelegraph source)
        {
            BossAttackKind kind = source.Kind;
            float lockedX = source.X + source.Width * .5f;
            float pace = (BossTierPace + (bossPatternRage - 1) * .08f) * BossPracticePace;
            if (SpawnComboProjectile(source, pace)) { }
            else if (SpawnUnderwaterAttack(source, pace)) { }
            else if (SpawnSpecialBossAttack(source, pace)) { }
            else if (kind == BossAttackKind.MineDrop)
            {
                AddBossObstacle(kind, CurrentBoss.X - 26f, 0f, 32f, 32f, -190f * pace, 0f, 10f, 16f, source.Id, 16f);
            }
            else if (kind == BossAttackKind.CrateDrop)
                AddBossObstacle(kind, CurrentBoss.X - 28f, 0f, 56f, 68f, -140f * pace, 0f, 12f, 32f, source.Id, 28f);
            else if (kind == BossAttackKind.ElectricPillar)
                AddBossObstacle(kind, source.X, source.Y, source.Width, source.Height, 0f, 0f, 1.35f, 0f, source.Id);
            else if (kind == BossAttackKind.ShieldDrone)
                AddBossObstacle(kind, CurrentBoss.X - 28f, 62f, 66f, 46f, -205f * pace, 0f, 10f, 32f, source.Id);
            else if (kind == BossAttackKind.AirGate)
            {
                float x = CurrentBoss.X - 20f;
                AddBossObstacle(kind, x, 0f, 44f, source.GapY, -280f * pace, 0f, 8f, 0f, source.Id);
                AddBossObstacle(kind, x, source.GapY + source.GapHeight, 44f,
                    Math.Max(1f, 360f - source.GapY - source.GapHeight), -280f * pace, 0f, 8f, 0f, source.Id);
            }
            else if (kind == BossAttackKind.AirMine)
                AddBossObstacle(kind, CurrentBoss.X - 22f, source.Y, 34f, 34f, -185f * pace, 0f, 10f, 24f, source.Id, 17f);
            else if (kind == BossAttackKind.AirSpread)
            {
                float startX = CurrentBoss.X - 20f, startY = CurrentBoss.Y + CurrentBoss.Height * .45f;
                float dx = Math.Min(-100f, source.X - startX);
                float dy = source.GapY - startY;
                float magnitude = (float)Math.Sqrt(dx * dx + dy * dy);
                float vx = dx / magnitude * 325f * pace, vy = dy / magnitude * 325f * pace;
                for (int i = -1; i <= 1; i++)
                    bossProjectiles.Add(new BossProjectile {Id=++nextBossObjectId, Kind=kind, X=startX, Y=startY,
                        Width=18f,Height=12f,Radius=6f,VelocityX=vx,VelocityY=vy+i*source.GapHeight*pace,Lifetime=6f,SourceTelegraphId=source.Id});
            }
            else
            {
                BossProjectile p = new BossProjectile
                {
                    Id = ++nextBossObjectId, Kind = kind, X = CurrentBoss.X - 30f, Y = 14f,
                    Width = 30f, Height = 22f, Radius = 11f, VelocityX = -425f * pace, Lifetime = 7f, SourceTelegraphId = source.Id
                };
                if (kind == BossAttackKind.Artillery || kind == BossAttackKind.Missile)
                {
                    p.Width = kind == BossAttackKind.Artillery ? 32f : 22f;
                    p.Height = kind == BossAttackKind.Artillery ? 32f : 54f;
                    p.Radius = kind == BossAttackKind.Artillery ? 14f : 0f;
                    p.X = lockedX - p.Width * .5f; p.Y = 430f;
                    p.VelocityX = 0f; p.VelocityY = -(kind == BossAttackKind.Artillery ? 485f : 620f) * pace;
                }
                else if (kind == BossAttackKind.Shockwave)
                { p.Radius = 0f; p.Width = 62f; p.Height = 25f; p.Y = 0f; p.VelocityX = -375f * pace; CurrentBoss.LandingFlashRemaining = .35f; }
                else if (kind == BossAttackKind.Drone || kind == BossAttackKind.DroneLow)
                { p.Radius = 0f; p.Width = 50f; p.Height = kind == BossAttackKind.Drone ? 32f : 26f; p.Y = kind == BossAttackKind.Drone ? 56f : 8f; p.VelocityX = -350f * pace; }
                bossProjectiles.Add(p);
            }
            CurrentBoss.ShotFlashRemaining = .18f; CurrentBoss.Recoil = 1f;
        }

        private BossObstacle AddBossObstacle(BossAttackKind kind,float x,float y,float width,float height,float vx,float vy,
            float life,float hp,int source,float radius=0f)
        {
            BossObstacle obstacle = new BossObstacle {Id=++nextBossObjectId,GroupId=source,SourceTelegraphId=source,Kind=kind,
                X=x,Y=y,BaseY=y,Width=width,Height=height,VelocityX=vx,VelocityY=vy,Remaining=life,Health=hp,MaxHealth=hp,Radius=radius};
            bossObstacles.Add(obstacle); return obstacle;
        }

        private void TickBossObstacles(float dt)
        {
            for (int i=0;i<bossObstacles.Count && State==RunState.Playing && ArenaPhase==BossPhase.Fighting;i++)
            {
                BossObstacle obstacle=bossObstacles[i]; if(obstacle.Removed)continue;
                obstacle.Remaining-=dt; obstacle.HitFlashRemaining=Math.Max(0f,obstacle.HitFlashRemaining-dt);
                if(obstacle.Remaining<=0f){obstacle.Removed=true;continue;}
                if(obstacle.WarningRemaining>0f)
                {
                    obstacle.WarningRemaining=Math.Max(0f,obstacle.WarningRemaining-dt);
                    obstacle.Active=obstacle.WarningRemaining<=0f;
                }
                if(!obstacle.Active)continue;
                obstacle.X+=obstacle.VelocityX*dt;obstacle.Y+=obstacle.VelocityY*dt;
                if(obstacle.MotionAmplitude!=0f)
                    obstacle.Y=obstacle.BaseY+(float)Math.Sin(BossElapsed*obstacle.MotionSpeed)*obstacle.MotionAmplitude;
                if(obstacle.X+obstacle.Width<-100f||obstacle.X>CanvasWidth+400f){obstacle.Removed=true;continue;}
                bool contact=obstacle.Radius>0f
                    ?RunnerGeometry.CircleTouchesPlayer(obstacle.X+obstacle.Width*.5f,obstacle.Y+obstacle.Height*.5f,obstacle.Radius,this)
                    :RunnerGeometry.RectTouchesPlayer(obstacle.X,obstacle.Y,obstacle.Width,obstacle.Height,this);
                if(contact&&InvincibleRemaining<=0f)
                {
                    if(!TryAbsorbHazard(false))TakeHit("Va vào " + AttackName(obstacle.Kind));
                    if(obstacle.Destructible)obstacle.Removed=true;
                }
            }
            for(int i=bossObstacles.Count-1;i>=0;i--)if(bossObstacles[i].Removed)bossObstacles.RemoveAt(i);
        }

        private bool TryAimAtBossObstacle(BossProjectile shot)
        {
            BossObstacle closest=null;float best=float.PositiveInfinity;
            for(int i=0;i<bossObstacles.Count;i++)
            {
                BossObstacle obstacle=bossObstacles[i];
                if(obstacle.Removed||!obstacle.Active||!obstacle.Destructible||
                    obstacle.X+obstacle.Width*.5f<=shot.X+shot.Width*.5f||
                    obstacle.X>CurrentBoss.X+CurrentBoss.Width)continue;
                float dx=obstacle.X+obstacle.Width*.5f-shot.X-shot.Width*.5f;
                float dy=obstacle.Y+obstacle.Height*.5f-shot.Y-shot.Height*.5f;
                float squared=dx*dx+dy*dy;
                if(squared<best){best=squared;closest=obstacle;}
            }
            if(closest==null)return false;
            float aimX=closest.X+closest.Width*.5f-shot.X-shot.Width*.5f;
            float aimY=closest.Y+closest.Height*.5f-shot.Y-shot.Height*.5f;
            float length=(float)Math.Sqrt(aimX*aimX+aimY*aimY);
            float factor=880f/Math.Max(1f,length);shot.VelocityX=aimX*factor;shot.VelocityY=aimY*factor;return true;
        }

        private bool TryHitBossObstacle(BossProjectile shot)
        {
            for(int i=0;i<bossObstacles.Count;i++)
            {
                BossObstacle obstacle=bossObstacles[i];if(obstacle.Removed||!obstacle.Active)continue;
                bool contact;
                if(obstacle.Radius>0f)
                {
                    float cx=obstacle.X+obstacle.Width*.5f,cy=obstacle.Y+obstacle.Height*.5f;
                    float dx=cx-Math.Max(shot.X,Math.Min(shot.X+shot.Width,cx));
                    float dy=cy-Math.Max(shot.Y,Math.Min(shot.Y+shot.Height,cy));
                    contact=dx*dx+dy*dy<obstacle.Radius*obstacle.Radius;
                }
                else contact=RectanglesOverlap(shot.X,shot.Y,shot.Width,shot.Height,obstacle.X,obstacle.Y,obstacle.Width,obstacle.Height);
                if(!contact)continue;
                shot.Removed=true;
                if(obstacle.Destructible)
                {
                    obstacle.Health=Math.Max(0f,obstacle.Health-BossShotDamage);obstacle.HitFlashRemaining=.12f;
                    if(obstacle.Health<=0f)obstacle.Removed=true;
                }
                return true;
            }
            return false;
        }
    }
}
