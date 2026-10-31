using System;
using System.Collections.Generic;

namespace CityDino
{
    /// <summary>
    /// Milestone battles have an independent clock and a flat arena. No running
    /// distance, biome timer, survival resource or timed pickup advances here.
    /// </summary>
    public sealed partial class RunnerSession
    {
        public const int BossScoreInterval = 10000;
        public const float BossIntroDuration = 1.8f;
        public const float BossVictoryDuration = 1.6f;
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
        private float secondShotRemaining = -1f;
        private BossAttackKind secondShotKind;

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
            shootingHeld = bossRewardPaid = false;
            secondShotRemaining = -1f;
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
            float growth = 1f + Math.Min(0.6f, ((BossNumber - 1) / 3) * 0.1f);
            float hp = (kind == BossKind.Tank ? 750f : kind == BossKind.Sentinel ? 840f : 800f) * growth;
            CurrentBoss = new BossEncounter
            {
                Kind = kind, Health = hp, MaxHealth = hp,
                X = kind == BossKind.Sentinel ? 1000f : 965f,
                Y = kind == BossKind.Gunship ? 210f : 0f,
                Width = kind == BossKind.Sentinel ? 172f : kind == BossKind.Gunship ? 270f : 250f,
                Height = kind == BossKind.Sentinel ? 225f : kind == BossKind.Gunship ? 130f : 150f,
                Vulnerable = false, AttackName = "Đang tiến vào khu vực", AttackProgress = 0f
            };
            ArenaPhase = BossPhase.Intro;
            BossPhaseRemaining = BossIntroDuration;
            bossClock = attackClock = shotCooldown = 0d;
            bossProjectiles.Clear();
            bossTelegraphs.Clear();
            shootingHeld = bossRewardPaid = false;
            nextBossObjectId = bossAttackIndex = 0;
            secondShotRemaining = -1f;
            // Entry is a transition, not an extra source of damage or healing.
            PlayerY = VelocityY = AnimationVelocityY = 0f;
            HorizontalVelocity = 0f;
            grounded = true;
            jumpsUsed = 0;
            coyoteRemaining = 0.1f;
            IsDucking = duckRequested = false;
            if (NextBossScore > int.MaxValue - BossScoreInterval)
                bossMilestonesComplete = true;
            else NextBossScore += BossScoreInterval;
        }

        private BossKind SelectBossKind()
        {
            if (BossNumber <= 3) return (BossKind)(BossNumber - 1);
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
            for (int i = 0; i < bossProjectiles.Count; i++)
            {
                BossProjectile p = bossProjectiles[i];
                if (!p.Removed) fastest = Math.Max(fastest,
                    Math.Max(Math.Abs(p.VelocityX) + HorizontalSpeed, Math.Abs(p.VelocityY) + Math.Abs(VelocityY)));
            }
            return Math.Min(PhysicsStep, 5f / Math.Max(1f, fastest));
        }

        private void StepBossArena(float dt)
        {
            bossClock += dt;
            if (CurrentBoss != null && ArenaPhase != BossPhase.Victory) UpdateBossBody();
            TickCharacter(dt);
            InvincibleRemaining = Math.Max(0f, InvincibleRemaining - dt);
            float oldX = PlayerX;
            IntegrateHorizontal(dt);
            if (Math.Abs(PlayerX - oldX) > 0.0001f) FacingLeft = PlayerX < oldX;
            IntegratePlayer(dt);
            AnimationVelocityY = VelocityY;
            if (ArenaPhase == BossPhase.Intro)
            {
                BossPhaseRemaining = Math.Max(0f, BossPhaseRemaining - dt);
                if (BossPhaseRemaining <= 0f)
                {
                    ArenaPhase = BossPhase.Fighting;
                    attackClock = 0d;
                    BeginBossAttack();
                }
                return;
            }
            if (ArenaPhase == BossPhase.Victory)
            {
                BossPhaseRemaining = Math.Max(0f, BossPhaseRemaining - dt);
                if (BossPhaseRemaining <= 0f) LeaveBossArena();
                return;
            }
            if (ArenaPhase != BossPhase.Fighting || CurrentBoss == null) return;
            if (CurrentBoss.Health <= 0f) { BeginBossVictory(); return; }

            shotCooldown = Math.Max(0d, shotCooldown - dt);
            if (shootingHeld && shotCooldown <= 0d) TryShoot();
            attackClock += dt;
            float cycle = CurrentBoss.Health <= CurrentBoss.MaxHealth * 0.5f ? 4.15f : 4.8f;
            CurrentBoss.AttackProgress = Math.Min(1f, (float)attackClock / cycle);
            CurrentBoss.Vulnerable = attackClock >= (CurrentBoss.Kind == BossKind.Sentinel ? 2.15d : 1.9d);
            if (secondShotRemaining >= 0f)
            {
                secondShotRemaining -= dt;
                if (secondShotRemaining <= 0f)
                {
                    SpawnBossAttack(secondShotKind, PlayerX, false);
                    secondShotRemaining = -1f;
                }
            }
            TickBossTelegraphs(dt);
            TickBossProjectiles(dt);
            if (State != RunState.Playing || ArenaPhase != BossPhase.Fighting) return;
            if (attackClock >= cycle)
            {
                attackClock -= cycle;
                bossAttackIndex++;
                BeginBossAttack();
            }
        }

        private void UpdateBossBody()
        {
            if (CurrentBoss.Kind == BossKind.Gunship)
                CurrentBoss.Y = 210f + (float)Math.Sin(bossClock * 0.8d) * 27f;
        }

        private void BeginBossAttack()
        {
            BossAttackKind kind;
            if (CurrentBoss.Kind == BossKind.Tank)
                kind = bossAttackIndex % 2 == 0 ? BossAttackKind.Cannon : BossAttackKind.Artillery;
            else if (CurrentBoss.Kind == BossKind.Sentinel)
                kind = bossAttackIndex % 3 == 0 ? BossAttackKind.LaserLow
                    : bossAttackIndex % 3 == 1 ? BossAttackKind.LaserHigh : BossAttackKind.Shockwave;
            else kind = bossAttackIndex % 2 == 0 ? BossAttackKind.Missile : BossAttackKind.Drone;
            CurrentBoss.AttackName = AttackName(kind);
            if (CurrentBoss.Health <= CurrentBoss.MaxHealth * 0.5f &&
                (kind == BossAttackKind.Cannon || kind == BossAttackKind.Shockwave || kind == BossAttackKind.Drone))
                CurrentBoss.AttackName += " · Hai đợt";
            CurrentBoss.Vulnerable = false;
            float warning = Math.Max(0.8f, 1.1f - ((BossNumber - 1) / 3) * 0.05f);
            BossTelegraph mark = new BossTelegraph
            {
                Id = ++nextBossObjectId, Kind = kind, Remaining = warning, Duration = warning,
                X = 90f, Y = 12f, Width = 690f, Height = 24f, Active = false
            };
            if (kind == BossAttackKind.Artillery || kind == BossAttackKind.Missile)
            {
                mark.Width = kind == BossAttackKind.Artillery ? 100f : 78f;
                mark.X = Math.Max(MinimumPlayerX - 20f, Math.Min(MaximumPlayerX - mark.Width + 20f, PlayerX - mark.Width * 0.5f));
                mark.Y = 0f; mark.Height = 14f;
            }
            else if (kind == BossAttackKind.LaserLow || kind == BossAttackKind.LaserHigh)
            {
                mark.X = 75f; mark.Width = 760f;
                mark.Y = kind == BossAttackKind.LaserHigh ? 62f : 13f;
                mark.Height = 14f;
            }
            else if (kind == BossAttackKind.Drone) { mark.Y = 56f; mark.Height = 32f; }
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
                case BossAttackKind.Drone: return "Drone quét ngang";
                default: return "Đạn pháo thấp";
            }
        }

        private void TickBossTelegraphs(float dt)
        {
            for (int i = bossTelegraphs.Count - 1; i >= 0 && State == RunState.Playing && ArenaPhase == BossPhase.Fighting; i--)
            {
                BossTelegraph mark = bossTelegraphs[i];
                mark.Remaining -= dt;
                if (mark.Active)
                {
                    if (mark.Remaining <= 0f) { bossTelegraphs.RemoveAt(i); continue; }
                    if (InvincibleRemaining <= 0f && RunnerGeometry.RectTouchesPlayer(mark.X, mark.Y, mark.Width, mark.Height, this))
                    {
                        if (!TryAbsorbHazard(false)) TakeHit("Trúng " + AttackName(mark.Kind));
                    }
                }
                else if (mark.Remaining <= 0f)
                {
                    if (mark.Kind == BossAttackKind.LaserLow || mark.Kind == BossAttackKind.LaserHigh)
                    {
                        mark.Active = true;
                        mark.Remaining = mark.Duration = 0.45f;
                    }
                    else
                    {
                        SpawnBossAttack(mark.Kind, mark.X + mark.Width * 0.5f);
                        bossTelegraphs.RemoveAt(i);
                    }
                }
            }
        }

        private void SpawnBossAttack(BossAttackKind kind, float lockedX, bool allowFollowup = true)
        {
            float pace = 1f + Math.Min(0.35f, ((BossNumber - 1) / 3) * 0.05f);
            BossProjectile p = new BossProjectile
            {
                Id = ++nextBossObjectId, Kind = kind, X = CurrentBoss.X - 30f, Y = 14f,
                Width = 30f, Height = 22f, VelocityX = -355f * pace, Lifetime = 7f
            };
            if (kind == BossAttackKind.Artillery || kind == BossAttackKind.Missile)
            {
                p.Width = kind == BossAttackKind.Artillery ? 32f : 22f;
                p.Height = kind == BossAttackKind.Artillery ? 32f : 54f;
                p.X = lockedX - p.Width * 0.5f; p.Y = 430f;
                p.VelocityX = 0f; p.VelocityY = -(kind == BossAttackKind.Artillery ? 440f : 580f) * pace;
            }
            else if (kind == BossAttackKind.Shockwave)
            {
                p.Width = 62f; p.Height = 25f; p.Y = 0f; p.VelocityX = -315f * pace;
            }
            else if (kind == BossAttackKind.Drone)
            {
                p.Width = 56f; p.Height = 32f; p.Y = 56f; p.VelocityX = -300f * pace;
            }
            bossProjectiles.Add(p);
            if (allowFollowup && CurrentBoss.Health <= CurrentBoss.MaxHealth * 0.5f && secondShotRemaining < 0f &&
                (kind == BossAttackKind.Cannon || kind == BossAttackKind.Shockwave || kind == BossAttackKind.Drone))
            {
                secondShotRemaining = kind == BossAttackKind.Shockwave ? 0.65f : 0.42f;
                secondShotKind = kind;
            }
        }

        private void AimAtBoss(BossProjectile p)
        {
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
                    if (RectanglesOverlap(p.X, p.Y, p.Width, p.Height, CurrentBoss.X, CurrentBoss.Y,
                        CurrentBoss.Width, CurrentBoss.Height))
                    {
                        p.Removed = true;
                        DamageBoss(BossShotDamage, false);
                    }
                }
                else
                {
                    if ((p.Kind == BossAttackKind.Artillery || p.Kind == BossAttackKind.Missile) && p.Y <= 0f)
                    {
                        p.Removed = true;
                        float width = p.Kind == BossAttackKind.Artillery ? 100f : 78f;
                        bossTelegraphs.Add(new BossTelegraph
                        {
                            Id = ++nextBossObjectId, Kind = p.Kind, X = p.X + p.Width * 0.5f - width * 0.5f,
                            Y = 0f, Width = width, Height = 58f,
                            Active = true, Remaining = 0.32f, Duration = 0.32f
                        });
                    }
                    else if (InvincibleRemaining <= 0f && RunnerGeometry.RectTouchesPlayer(p.X, p.Y, p.Width, p.Height, this))
                    {
                        p.Removed = true;
                        if (!TryAbsorbHazard(true)) TakeHit("Trúng " + AttackName(p.Kind));
                    }
                }
                if (p.X + p.Width < 0f || p.X > CanvasWidth + 100f || p.Y > 700f || p.Y + p.Height < -50f)
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
            bossProjectiles.Clear(); bossTelegraphs.Clear();
            shootingHeld = false; secondShotRemaining = -1f;
            if (!bossRewardPaid)
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
