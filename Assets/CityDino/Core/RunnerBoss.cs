using System;
using System.Collections.Generic;

namespace CityDino
{
    /// <summary>
    /// Milestone battles have an independent clock and a themed arena. No running
    /// distance, biome timer, survival resource or timed pickup advances here.
    /// </summary>
    public sealed partial class RunnerSession
    {
        public const int BossScoreInterval = 10000;
        public const float BossIntroDuration = 4.8f;
        public const float BossVictoryDuration = 4.8f;
        public const float BossShotInterval = 0.24f;
        public const float BossShotDamage = 8f;
        public const float BossReflectedDamage = 24f;

        private readonly List<BossProjectile> bossProjectiles = new List<BossProjectile>();
        private readonly List<BossTelegraph> bossTelegraphs = new List<BossTelegraph>();
        private readonly List<BossKind> bossBag = new List<BossKind>();
        private double bossClock;
        private double attackClock;
        private double shotCooldown;
        private bool shootingHeld;
        private bool bossRewardPaid;
        private bool bossMilestonesComplete;
        private int nextBossObjectId;
        private int bossAttackIndex;
        private BossKind previousBossKind;
        private float bossActionCycle = 3.7f;
        private int bossPatternRage = 1;
        private float bossMotionRage = 1f;
        private BossAttackKind primaryBossAttack;

        public BossPhase ArenaPhase { get; private set; }
        public bool IsBossArena { get { return ArenaPhase != BossPhase.None; } }
        public BossEncounter CurrentBoss { get; private set; }
        public int BossNumber { get; private set; }
        public int NextBossScore { get; private set; } = BossScoreInterval;
        public float BossPhaseRemaining { get; private set; }
        public float BossElapsed { get { return (float)bossClock; } }
        public float PlayerAnimationTime { get { return IsBossArena ? BossElapsed : Elapsed; } }
        public IReadOnlyList<BossProjectile> BossProjectiles { get { return bossProjectiles; } }
        public IReadOnlyList<BossTelegraph> BossTelegraphs { get { return bossTelegraphs; } }

        public static string BossName(BossKind kind)
        {
            return kind == BossKind.Tank ? "XE TĂNG K-01"
                : kind == BossKind.Sentinel ? "VỆ BINH G-02" : "TRỰC THĂNG H-03";
        }

        public void SetShooting(bool shooting)
        {
            shootingHeld = shooting && State == RunState.Playing && ArenaPhase == BossPhase.Fighting;
        }

        public bool TryShoot()
        {
            // Shooting is a common arena action, not a character skill.
            if (State != RunState.Playing || ArenaPhase != BossPhase.Fighting ||
                CurrentBoss == null || CurrentBoss.Health <= 0f || shotCooldown > 0d)
                return false;
            BossProjectile shot = new BossProjectile
            {
                Id = ++nextBossObjectId, Friendly = true, Kind = BossAttackKind.Cannon,
                X = PlayerX + 28f, Y = PlayerY + (IsDucking ? 23f : 54f),
                Width = 12f, Height = 8f, Lifetime = 3f
            };
            AimAtBoss(shot);
            bossProjectiles.Add(shot);
            shotCooldown = BossShotInterval;
            return true;
        }

        public void ReflectBossAttack()
        {
            if (State != RunState.Playing || ArenaPhase != BossPhase.Fighting || CurrentBoss == null)
                return;
            // A reflected shot has a fixed damage budget, even against a fan of
            // incoming projectiles. One guard absorbs one hit in character core.
            DamageBoss(BossReflectedDamage, true);
        }

        private void ResetBossRun()
        {
            ResetBossArena();
            ResetPracticeRun();
            BossNumber = 0;
            NextBossScore = BossScoreInterval;
            bossMilestonesComplete = false;
            bossBag.Clear();
            previousBossKind = BossKind.Gunship;
        }

        private void ResetBossArena()
        {
            ArenaPhase = BossPhase.None;
            CurrentBoss = null;
            BossPhaseRemaining = 0f;
            bossClock = attackClock = shotCooldown = 0d;
            bossProjectiles.Clear();
            bossTelegraphs.Clear();
            ResetBossPatternState();
            ResetBossMotion();
            shootingHeld = bossRewardPaid = false;
            bossActionCycle = 3.7f;
            bossPatternRage = 1;
            bossMotionRage = 1f;
            nextBossObjectId = bossAttackIndex = 0;
        }

        private void TryEnterBossArena()
        {
            if (State != RunState.Playing || IsBossArena || bossMilestonesComplete || Score < NextBossScore)
                return;
            EnterBossArena();
        }

        private void EnterBossArena()
        {
            BossNumber++;
            BossKind kind = SelectBossKind();
            previousBossKind = kind;
            CreateBossArena(kind);
            if (NextBossScore > int.MaxValue - BossScoreInterval) bossMilestonesComplete = true;
            else NextBossScore += BossScoreInterval;
        }

        private void CreateBossArena(BossKind kind)
        {
            float growth = BossHealthScale;
            float hp = (kind == BossKind.Tank ? 750f : kind == BossKind.Sentinel ? 840f : 800f) * growth;
            if (IsPractice && PracticeLearning) hp *= 0.65f;
            CurrentBoss = new BossEncounter
            {
                Kind = kind, Health = hp, MaxHealth = hp, X = CanvasWidth + 40f,
                Y = kind == BossKind.Gunship ? 148f : 0f,
                Width = kind == BossKind.Sentinel ? 172f : kind == BossKind.Gunship ? 270f : 250f,
                Height = kind == BossKind.Sentinel ? 225f : kind == BossKind.Gunship ? 130f : 150f,
                Vulnerable = false, AttackName = "Đang tiến vào khu vực", AttackProgress = 0f,
                Tier = BossTier
            };
            ArenaPhase = BossPhase.Intro;
            BossPhaseRemaining = BossIntroDuration;
            bossClock = attackClock = shotCooldown = 0d;
            bossProjectiles.Clear(); bossTelegraphs.Clear(); ResetBossPatternState();
            ResetBossMotion();
            shootingHeld = bossRewardPaid = false;
            nextBossObjectId = bossAttackIndex = 0;
            bossActionCycle = 3.7f; bossPatternRage = 1; bossMotionRage = 1f;
            BeginBossEntrance();
        }

        private BossKind SelectBossKind()
        {
            if (BossNumber <= 3) return BossNumber == 1 ? BossKind.Tank : BossNumber == 2 ? BossKind.Gunship : BossKind.Sentinel;
            if (bossBag.Count == 0)
            {
                bossBag.Add(BossKind.Tank); bossBag.Add(BossKind.Sentinel); bossBag.Add(BossKind.Gunship);
                for (int i = bossBag.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    BossKind temporary = bossBag[i]; bossBag[i] = bossBag[j]; bossBag[j] = temporary;
                }
            }
            int last = bossBag.Count - 1;
            if (bossBag[last] == previousBossKind && last > 0)
            {
                BossKind temporary = bossBag[last]; bossBag[last] = bossBag[0]; bossBag[0] = temporary;
            }
            BossKind result = bossBag[last]; bossBag.RemoveAt(last); return result;
        }

        private float BossCollisionStep()
        {
            // Independent of the unbounded scrolling speed left outside arena.
            float fastest = Math.Max(IsDashing ? Math.Abs(DashVelocity) : HorizontalSpeed,
                Math.Abs(VelocityY) + Gravity * PhysicsStep);
            // The visible leading weapon moves with the boss rather than a
            // projectile. Include its fastest recovery/strike speed as well.
            if (bossChargeActive) fastest = Math.Max(fastest, 2200f + HorizontalSpeed);
            for (int i = 0; i < bossProjectiles.Count; i++)
            {
                BossProjectile p = bossProjectiles[i];
                if (!p.Removed) fastest = Math.Max(fastest,
                    Math.Max(Math.Abs(p.VelocityX) + HorizontalSpeed, Math.Abs(p.VelocityY) + Math.Abs(VelocityY)));
            }
            for (int i = 0; i < bossObstacles.Count; i++)
            {
                BossObstacle obstacle = bossObstacles[i];
                if (!obstacle.Removed)
                    fastest = Math.Max(fastest, Math.Max(
                        Math.Abs(obstacle.VelocityX) + Math.Max(HorizontalSpeed, Math.Abs(DashVelocity)),
                        Math.Abs(obstacle.VelocityY) + Math.Abs(obstacle.MotionAmplitude * obstacle.MotionSpeed) + Math.Abs(VelocityY)));
            }
            return Math.Min(PhysicsStep, 5f / Math.Max(1f, fastest));
        }

        private void StepBossArena(float dt)
        {
            bossClock += dt;
            if (CurrentBoss != null && ArenaPhase != BossPhase.Victory)
            {
                CurrentBoss.RageLevel = GetBossRage(CurrentBoss.Health, CurrentBoss.MaxHealth);
                CurrentBoss.HitFlashRemaining = Math.Max(0f, CurrentBoss.HitFlashRemaining - dt);
                CurrentBoss.ShotFlashRemaining = Math.Max(0f, CurrentBoss.ShotFlashRemaining - dt);
                CurrentBoss.LandingFlashRemaining = Math.Max(0f, CurrentBoss.LandingFlashRemaining - dt);
                // Keep a short follow-through after the 0.18s muzzle flash so
                // ordinary volleys visibly pass through their recovery pose.
                CurrentBoss.Recoil = Math.Max(0f, CurrentBoss.Recoil - dt * 2.5f);
                float difference = CurrentBoss.RageLevel - bossMotionRage;
                bossMotionRage += Math.Max(-dt * 2f, Math.Min(dt * 2f, difference));
                UpdateBossBody();
            }
            TickCharacter(dt);
            InvincibleRemaining = Math.Max(0f, InvincibleRemaining - dt);
            if (ArenaPhase == BossPhase.Intro)
            {
                entranceClock = Math.Min(BossIntroDuration, entranceClock + dt);
                BossPhaseRemaining = Math.Max(0f, BossIntroDuration - entranceClock);
                ApplyBossEntrance(dt);
                if (BossPhaseRemaining <= 0f) FinishBossEntrance();
                return;
            }
            if (ArenaPhase == BossPhase.Victory)
            {
                departureClock = Math.Min(BossVictoryDuration, departureClock + dt);
                BossPhaseRemaining = Math.Max(0f, BossVictoryDuration - departureClock);
                ApplyBossDeparture(dt);
                if (BossPhaseRemaining <= 0f) LeaveBossArena();
                return;
            }
            if (ArenaPhase != BossPhase.Fighting || CurrentBoss == null) return;
            if (IsPractice) practiceCombatClock += dt;
            float oldX = PlayerX;
            IntegrateHorizontal(dt);
            if (Math.Abs(PlayerX - oldX) > .0001f) FacingLeft = PlayerX < oldX;
            if (IsFlyingBoss) IntegrateBossFlight(dt);
            else { IntegratePlayer(dt); AnimationVelocityY = VelocityY; }
            if (CurrentBoss.Health <= 0f) { BeginBossVictory(); return; }
            shotCooldown = Math.Max(0d, shotCooldown - dt);
            if (shootingHeld && shotCooldown <= 0d) TryShoot();
            attackClock += dt;
            CurrentBoss.AttackProgress = Math.Min(1f, (float)attackClock / bossActionCycle);
            CurrentBoss.Vulnerable = attackClock >= bossActionCycle * 0.43d;
            TickBossTelegraphs(dt);
            UpdateBossAction();
            TickBossChargeContact();
            TickBossObstacles(dt);
            TickBossProjectiles(dt);
            if (State != RunState.Playing || ArenaPhase != BossPhase.Fighting) return;
            if (attackClock >= bossActionCycle)
            {
                attackClock -= bossActionCycle;
                bossAttackIndex++;
                BeginBossAttack();
            }
        }

        private static int GetBossRage(float health, float maximum)
        {
            float fraction = maximum > 0f ? health / maximum : 0f;
            return fraction > 0.65f ? 1 : fraction > 0.35f ? 2 : 3;
        }

        private void UpdateBossBody()
        {
            float time = (float)bossClock;
            float baseX;
            float y = 0f;
            if (CurrentBoss.Kind == BossKind.Tank)
            {
                float advance = 20f + bossMotionRage * 8f;
                baseX = 955f + (float)Math.Sin(time * 0.91f) * 43f
                    - advance * (float)Math.Sin(Math.PI * Math.Min(1d, attackClock / bossActionCycle))
                    + CurrentBoss.Recoil * 11f;
            }
            else if (CurrentBoss.Kind == BossKind.Sentinel)
            {
                baseX = 986f + (float)Math.Sin(time * 0.82f) * 46f - bossMotionRage * 7f;
                y = 2f + (float)Math.Sin(time * 5.6f) * 2f;
                if (primaryBossAttack == BossAttackKind.Shockwave && ArenaPhase == BossPhase.Fighting)
                {
                    float phase = (float)(attackClock - 0.18d) / 0.7f;
                    if (phase > 0f && phase < 1f)
                        y += (float)Math.Sin(Math.PI * phase) * (48f + bossMotionRage * 10f);
                    if (CurrentBoss.Y > 8f && y <= 4f) CurrentBoss.LandingFlashRemaining = 0.32f;
                }
            }
            else
            {
                baseX = 934f + (float)Math.Sin(time * 0.73f) * 49f;
                y = 148f + (float)Math.Sin(time * 1.13f) * (38f + bossMotionRage * 7f)
                    + (float)Math.Sin(time * 2.3f) * 5f;
            }
            baseX = Math.Max(810f, Math.Min(CanvasWidth - CurrentBoss.Width - 16f, baseX));
            if (ApplyBossCharge(baseX, y)) return;
            if (ArenaPhase == BossPhase.Intro)
            {
                float progress = Math.Max(0f, Math.Min(1f, (float)bossClock / BossIntroDuration));
                progress = progress * progress * (3f - 2f * progress);
                CurrentBoss.X = (CanvasWidth + 40f) * (1f - progress) + baseX * progress;
            }
            else CurrentBoss.X = baseX;
            CurrentBoss.Y = y;
        }

        private void AimAtBoss(BossProjectile p)
        {
            if (TryAimAtBossObstacle(p)) return;
            float targetX = CurrentBoss.X + CurrentBoss.Width * 0.5f;
            float targetY = CurrentBoss.Y + CurrentBoss.Height * 0.55f;
            float dx = targetX - p.X - p.Width * 0.5f;
            float dy = targetY - p.Y - p.Height * 0.5f;
            float distance = (float)Math.Sqrt(dx * dx + dy * dy);
            float scale = 880f / Math.Max(1f, distance);
            p.VelocityX = dx * scale; p.VelocityY = dy * scale;
        }

        private void TickBossProjectiles(float dt)
        {
            for (int i = 0; i < bossProjectiles.Count && State == RunState.Playing && ArenaPhase == BossPhase.Fighting; i++)
            {
                BossProjectile p = bossProjectiles[i];
                if (p.Removed) continue;
                p.Lifetime -= dt;
                if (p.Lifetime <= 0f) { p.Removed = true; continue; }
                if (p.Friendly) AimAtBoss(p);
                p.X += p.VelocityX * dt; p.Y += p.VelocityY * dt;
                if (p.Friendly)
                {
                    if (TryHitBossObstacle(p)) continue;
                    if (RectanglesOverlap(p.X, p.Y, p.Width, p.Height, CurrentBoss.X, CurrentBoss.Y,
                        CurrentBoss.Width, CurrentBoss.Height))
                    {
                        p.Removed = true;
                        DamageBoss(BossShotDamage, false);
                    }
                }
                else
                {
                    if ((p.Kind == BossAttackKind.Artillery || p.Kind == BossAttackKind.Missile ||
                        p.Kind == BossAttackKind.MissileRain) && p.Y <= 0f)
                    {
                        p.Removed = true;
                        float width = p.Kind == BossAttackKind.Artillery ? 100f : p.Kind == BossAttackKind.MissileRain ? 62f : 78f;
                        bossTelegraphs.Add(new BossTelegraph
                        {
                            Id = ++nextBossObjectId, Kind = p.Kind, X = p.X + p.Width * 0.5f - width * 0.5f,
                            Y = 0f, Width = width, Height = 58f,
                            Active = true, Remaining = 0.32f, Duration = 0.32f,
                            SourceTelegraphId = p.SourceTelegraphId
                        });
                    }
                    else if (InvincibleRemaining <= 0f && (p.Radius > 0f
                        ? RunnerGeometry.CircleTouchesPlayer(p.X + p.Width * .5f, p.Y + p.Height * .5f, p.Radius, this)
                        : RunnerGeometry.RectTouchesPlayer(p.X, p.Y, p.Width, p.Height, this)))
                    {
                        p.Removed = true;
                        if (!TryAbsorbHazard(true)) TakeHit("Trúng " + AttackName(p.Kind));
                    }
                }
                // New crossfire can enter from either off-screen edge. Cull
                // only after an outgoing projectile has left its boundary.
                if ((p.X + p.Width < -100f && p.VelocityX <= 0f) ||
                    (p.X > CanvasWidth + 160f && p.VelocityX >= 0f) ||
                    (p.Y > 700f && p.VelocityY >= 0f) ||
                    (p.Y + p.Height < -50f && p.VelocityY <= 0f))
                    p.Removed = true;
            }
            for (int i = bossProjectiles.Count - 1; i >= 0; i--)
                if (bossProjectiles[i].Removed) bossProjectiles.RemoveAt(i);
        }

        private static bool RectanglesOverlap(float x1, float y1, float w1, float h1,
            float x2, float y2, float w2, float h2)
        { return x1 + w1 > x2 && x1 < x2 + w2 && y1 + h1 > y2 && y1 < y2 + h2; }

        private void DamageBoss(float damage, bool reflected)
        {
            if (ArenaPhase != BossPhase.Fighting || CurrentBoss == null ||
                (!reflected && !CurrentBoss.Vulnerable)) return;
            CurrentBoss.Health = Math.Max(0f, CurrentBoss.Health - Math.Max(0f, damage));
            CurrentBoss.HitFlashRemaining = 0.11f;
            CurrentBoss.RageLevel = GetBossRage(CurrentBoss.Health, CurrentBoss.MaxHealth);
            if (CurrentBoss.Health <= 0f) BeginBossVictory();
        }

        private void BeginBossVictory()
        {
            if (ArenaPhase != BossPhase.Fighting || CurrentBoss == null || State != RunState.Playing) return;
            CurrentBoss.Health = 0f;
            CurrentBoss.Vulnerable = false;
            CurrentBoss.AttackName = "Đã hạ mục tiêu";
            ArenaPhase = BossPhase.Victory;
            BossPhaseRemaining = BossVictoryDuration;
            bossProjectiles.Clear(); bossTelegraphs.Clear(); ResetBossPatternState();
            ResetBossMotion();
            shootingHeld = false;
            BeginBossDeparture();
            if (!bossRewardPaid && !IsPractice)
            {
                int baseReward = CurrentBoss.Kind == BossKind.Tank ? 300 : CurrentBoss.Kind == BossKind.Sentinel ? 450 : 600;
                int reward = baseReward + Math.Min(600, ((BossNumber - 1) / 3) * 75);
                RunStars = Math.Min(WalletLimit, RunStars + reward);
                Save.Wallet = Math.Min(WalletLimit, Save.Wallet + reward);
                bossRewardPaid = true;
                NotifySaveChanged();
            }
        }

        private void LeaveBossArena()
        {
            if (IsPractice) { FinishPractice(true, "Đã hoàn thành đấu tập"); return; }
            // Paused running entities retain their clock/position until now;
            // clear the return lane so the transition cannot cause a blind hit.
            entities.Clear();
            PlayerY = VelocityY = AnimationVelocityY = 0f;
            PlayerX = Math.Max(MinimumPlayerX, Math.Min(MaximumPlayerX, PlayerX));
            HorizontalVelocity = 0f;
            grounded = true; jumpsUsed = 0; coyoteRemaining = 0.1f;
            duckRequested = IsDucking = false;
            InvincibleRemaining = Math.Max(InvincibleRemaining, 2f);
            distanceToPattern = Math.Max(distanceToPattern, Speed * 2.5f + 300f);
            if (CurrentDistrict == District.Snow || CurrentDistrict == District.Sunset)
            {
                // Entry may have paused a drink that was about to be collected.
                // Replace that opportunity in the cleared lane, without healing
                // resources directly or consuming a purchased reserve.
                float lead = Math.Min(180f, Math.Max(65f, Speed * 0.15f));
                Add(CurrentDistrict == District.Snow ? EntityKind.HotDrink : EntityKind.Water,
                    PlayerX + lead, 10f, 32f, 36f);
                nextSurvivalSupplyAt = Math.Min(nextSurvivalSupplyAt, elapsedClock + 0.75d);
            }
            ResetBossArena();
        }
    }
}
