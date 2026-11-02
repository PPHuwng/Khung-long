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
            float growth = 1f + Math.Min(0.6f, ((BossNumber - 1) / 3) * 0.1f);
            float hp = (kind == BossKind.Tank ? 750f : kind == BossKind.Sentinel ? 840f : 800f) * growth;
            CurrentBoss = new BossEncounter
            {
                Kind = kind, Health = hp, MaxHealth = hp,
                X = CanvasWidth + 40f,
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
            bossActionCycle = 3.7f;
            bossPatternRage = 1;
            bossMotionRage = 1f;
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
            if (CurrentBoss != null && ArenaPhase != BossPhase.Victory)
            {
                CurrentBoss.RageLevel = GetBossRage(CurrentBoss.Health, CurrentBoss.MaxHealth);
                CurrentBoss.HitFlashRemaining = Math.Max(0f, CurrentBoss.HitFlashRemaining - dt);
                CurrentBoss.ShotFlashRemaining = Math.Max(0f, CurrentBoss.ShotFlashRemaining - dt);
                CurrentBoss.LandingFlashRemaining = Math.Max(0f, CurrentBoss.LandingFlashRemaining - dt);
                CurrentBoss.Recoil = Math.Max(0f, CurrentBoss.Recoil - dt * 6f);
                float difference = CurrentBoss.RageLevel - bossMotionRage;
                bossMotionRage += Math.Max(-dt * 2f, Math.Min(dt * 2f, difference));
                UpdateBossBody();
            }
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
            CurrentBoss.AttackProgress = Math.Min(1f, (float)attackClock / bossActionCycle);
            // The hatches spend over half of each cycle exposed. Difficulty
            // comes from movement and combinations rather than long HP sponges.
            CurrentBoss.Vulnerable = attackClock >= bossActionCycle * 0.43d;
            TickBossTelegraphs(dt);
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
            if (ArenaPhase == BossPhase.Intro)
            {
                float progress = Math.Max(0f, Math.Min(1f, (float)bossClock / BossIntroDuration));
                progress = progress * progress * (3f - 2f * progress);
                CurrentBoss.X = (CanvasWidth + 40f) * (1f - progress) + baseX * progress;
            }
            else CurrentBoss.X = baseX;
            CurrentBoss.Y = y;
        }

        private void BeginBossAttack()
        {
            bossPatternRage = GetBossRage(CurrentBoss.Health, CurrentBoss.MaxHealth);
            CurrentBoss.RageLevel = bossPatternRage;
            bossActionCycle = bossPatternRage == 1 ? 3.7f : bossPatternRage == 2 ? 3.25f : 2.9f;
            // Do not shorten an already announced attack when HP crosses a
            // phase boundary. Each newly selected combination keeps its timing.
            float warning = Math.Max(0.7f, 0.96f - (bossPatternRage - 1) * 0.09f
                - Math.Min(0.06f, ((BossNumber - 1) / 3) * 0.015f));
            CurrentBoss.Vulnerable = false;
            if (CurrentBoss.Kind == BossKind.Tank)
            {
                primaryBossAttack = bossAttackIndex % 2 == 0 ? BossAttackKind.Cannon : BossAttackKind.Artillery;
                if (primaryBossAttack == BossAttackKind.Cannon)
                {
                    int count = bossPatternRage + 1;
                    for (int i = 0; i < count; i++) ScheduleBossAttack(BossAttackKind.Cannon, i * 0.28f, warning, PlayerX);
                    CurrentBoss.AttackName = "Loạt pháo " + count + " viên";
                }
                else
                {
                    int count = bossPatternRage;
                    float centre = Math.Max(180f, Math.Min(640f, PlayerX));
                    for (int i = 0; i < count; i++)
                    {
                        float offset = i == 0 ? 0f : i % 2 == 1 ? 140f : -140f;
                        ScheduleBossAttack(BossAttackKind.Artillery, i * 0.38f, warning, centre + offset);
                    }
                    CurrentBoss.AttackName = "Pháo phủ vị trí · " + count + " điểm";
                }
            }
            else if (CurrentBoss.Kind == BossKind.Sentinel)
            {
                primaryBossAttack = bossAttackIndex % 3 == 0 ? BossAttackKind.LaserLow
                    : bossAttackIndex % 3 == 1 ? BossAttackKind.LaserHigh : BossAttackKind.Shockwave;
                ScheduleBossAttack(primaryBossAttack, 0f, warning, PlayerX);
                CurrentBoss.AttackName = AttackName(primaryBossAttack);
                if (bossPatternRage >= 2)
                {
                    BossAttackKind next = primaryBossAttack == BossAttackKind.LaserLow ? BossAttackKind.LaserHigh
                        : primaryBossAttack == BossAttackKind.LaserHigh ? BossAttackKind.LaserLow : BossAttackKind.Shockwave;
                    ScheduleBossAttack(next, 1.15f, warning, PlayerX);
                    CurrentBoss.AttackName += " → " + AttackName(next);
                }
            }
            else
            {
                primaryBossAttack = bossAttackIndex % 2 == 0 ? BossAttackKind.Missile : BossAttackKind.Drone;
                int count = bossPatternRage;
                if (primaryBossAttack == BossAttackKind.Missile)
                {
                    float centre = Math.Max(170f, Math.Min(650f, PlayerX));
                    for (int i = 0; i < count; i++)
                        ScheduleBossAttack(BossAttackKind.Missile, i * 0.4f, warning, centre + (i == 0 ? 0 : i % 2 == 1 ? 150 : -150));
                    CurrentBoss.AttackName = "Tên lửa khóa " + count + " vị trí";
                }
                else
                {
                    for (int i = 0; i < count; i++)
                    {
                        BossAttackKind kind = i % 2 == 0 ? BossAttackKind.Drone : BossAttackKind.DroneLow;
                        ScheduleBossAttack(kind, i * 0.72f, warning, PlayerX);
                    }
                    CurrentBoss.AttackName = "Drone quét cao / thấp · " + count + " đợt";
                }
            }
        }

        private void ScheduleBossAttack(BossAttackKind kind, float delay, float warning, float lockedX)
        {
            BossTelegraph mark = new BossTelegraph
            {
                Id = ++nextBossObjectId, Kind = kind, Remaining = warning, Duration = warning,
                DelayRemaining = Math.Max(0f, delay), X = 90f, Y = 12f, Width = 690f, Height = 24f
            };
            mark.SourceTelegraphId = mark.Id;
            if (kind == BossAttackKind.Artillery || kind == BossAttackKind.Missile)
            {
                mark.Width = kind == BossAttackKind.Artillery ? 100f : 78f;
                mark.X = Math.Max(MinimumPlayerX - 20f, Math.Min(MaximumPlayerX - mark.Width + 20f, lockedX - mark.Width * 0.5f));
                mark.Y = 0f; mark.Height = 14f;
            }
            else if (kind == BossAttackKind.LaserLow || kind == BossAttackKind.LaserHigh)
            {
                mark.X = 75f; mark.Width = 760f;
                mark.Y = kind == BossAttackKind.LaserHigh ? 62f : 13f;
                mark.Height = 14f;
            }
            else if (kind == BossAttackKind.Drone) { mark.Y = 56f; mark.Height = 32f; }
            else if (kind == BossAttackKind.DroneLow) { mark.Y = 8f; mark.Height = 26f; }
            else if (kind == BossAttackKind.Shockwave) { mark.Y = 0f; mark.Height = 25f; }
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
                    mark.DelayRemaining -= consumed;
                    activeDt -= consumed;
                    if (activeDt <= 0f) continue;
                }
                mark.Remaining -= activeDt;
                if (mark.Active)
                {
                    if (mark.Remaining <= 0f) { bossTelegraphs.RemoveAt(i); continue; }
                    if (InvincibleRemaining <= 0f && RunnerGeometry.RectTouchesPlayer(mark.X, mark.Y, mark.Width, mark.Height, this))
                        if (!TryAbsorbHazard(false)) TakeHit("Trúng " + AttackName(mark.Kind));
                }
                else if (mark.Remaining <= 0f)
                {
                    if (mark.Kind == BossAttackKind.LaserLow || mark.Kind == BossAttackKind.LaserHigh)
                    {
                        mark.Active = true;
                        mark.Remaining = mark.Duration = 0.32f;
                        CurrentBoss.ShotFlashRemaining = 0.18f;
                        CurrentBoss.Recoil = 1f;
                    }
                    else
                    {
                        SpawnBossAttack(mark.Kind, mark.X + mark.Width * 0.5f, mark.Id);
                        bossTelegraphs.RemoveAt(i);
                    }
                }
            }
        }

        private void SpawnBossAttack(BossAttackKind kind, float lockedX, int sourceTelegraphId)
        {
            float pace = 1f + Math.Min(0.3f, ((BossNumber - 1) / 3) * 0.04f) + (bossPatternRage - 1) * 0.08f;
            BossProjectile p = new BossProjectile
            {
                Id = ++nextBossObjectId, Kind = kind, X = CurrentBoss.X - 30f, Y = 14f,
                Width = 30f, Height = 22f, VelocityX = -425f * pace, Lifetime = 7f,
                SourceTelegraphId = sourceTelegraphId
            };
            if (kind == BossAttackKind.Artillery || kind == BossAttackKind.Missile)
            {
                p.Width = kind == BossAttackKind.Artillery ? 32f : 22f;
                p.Height = kind == BossAttackKind.Artillery ? 32f : 54f;
                p.X = lockedX - p.Width * 0.5f; p.Y = 430f;
                p.VelocityX = 0f; p.VelocityY = -(kind == BossAttackKind.Artillery ? 485f : 620f) * pace;
            }
            else if (kind == BossAttackKind.Shockwave)
            {
                p.Width = 62f; p.Height = 25f; p.Y = 0f; p.VelocityX = -375f * pace;
                CurrentBoss.LandingFlashRemaining = 0.35f;
            }
            else if (kind == BossAttackKind.Drone || kind == BossAttackKind.DroneLow)
            {
                p.Width = 50f; p.Height = kind == BossAttackKind.Drone ? 32f : 26f;
                p.Y = kind == BossAttackKind.Drone ? 56f : 8f;
                p.VelocityX = -350f * pace;
            }
            bossProjectiles.Add(p);
            CurrentBoss.ShotFlashRemaining = 0.18f;
            CurrentBoss.Recoil = 1f;
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
                            Active = true, Remaining = 0.32f, Duration = 0.32f,
                            SourceTelegraphId = p.SourceTelegraphId
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
            bossProjectiles.Clear(); bossTelegraphs.Clear();
            shootingHeld = false;
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
