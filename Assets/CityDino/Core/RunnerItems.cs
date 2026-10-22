using System;

namespace CityDino
{
    public sealed partial class RunnerSave
    {
        public int ScoreBoostLevel;
        public int MagnetLevel;
        public bool PurchasedShield;
        public bool PurchasedWaterReserve;
        public bool PurchasedWarmReserve;
    }

    /// <summary>
    /// Consumables, persistent duration upgrades and temporary pickup effects.
    /// Score bonuses are kept separate from real distance and the star wallet.
    /// </summary>
    public sealed partial class RunnerSession
    {
        public const int StartingShieldPrice = 200;
        public const int SurvivalReservePrice = 150;
        public const int MaximumItemUpgradeLevel = 4;
        public const float MagnetAttractionRadius = 260f;
        private static readonly int[] ItemUpgradeCosts = { 200, 400, 700, 1100 };

        private double bonusScoreDistance;
        private double scoreBoostRemainingClock;
        private double magnetRemainingClock;
        private double nextPowerupAt;

        public int NextScoreBoostCost
        {
            get { return UpgradePrice(Save.ScoreBoostLevel); }
        }
        public int NextMagnetCost
        {
            get { return UpgradePrice(Save.MagnetLevel); }
        }
        public float ScoreBoostDuration
        {
            get { return 8f + Save.ScoreBoostLevel * 2f; }
        }
        public float MagnetDuration
        {
            get { return 8f + Save.MagnetLevel * 2f; }
        }
        public float ScoreBoostRemaining
        {
            get { return (float)scoreBoostRemainingClock; }
        }
        public float MagnetRemaining
        {
            get { return (float)magnetRemainingClock; }
        }
        public bool ShieldReady { get; private set; }
        public bool WaterReserveReady { get; private set; }
        public bool WarmReserveReady { get; private set; }

        public bool BuyScoreBoostUpgrade()
        {
            int price = NextScoreBoostCost;
            if (!CanPurchaseItem(price)) return false;
            Save.Wallet -= price;
            Save.ScoreBoostLevel++;
            NotifySaveChanged();
            return true;
        }

        public bool BuyMagnetUpgrade()
        {
            int price = NextMagnetCost;
            if (!CanPurchaseItem(price)) return false;
            Save.Wallet -= price;
            Save.MagnetLevel++;
            NotifySaveChanged();
            return true;
        }

        public bool BuyStartingShield()
        {
            if (Save.PurchasedShield || !CanPurchaseItem(StartingShieldPrice)) return false;
            Save.Wallet -= StartingShieldPrice;
            Save.PurchasedShield = true;
            NotifySaveChanged();
            return true;
        }

        public bool BuyWaterReserve()
        {
            if (Save.PurchasedWaterReserve || !CanPurchaseItem(SurvivalReservePrice)) return false;
            Save.Wallet -= SurvivalReservePrice;
            Save.PurchasedWaterReserve = true;
            NotifySaveChanged();
            return true;
        }

        public bool BuyWarmReserve()
        {
            if (Save.PurchasedWarmReserve || !CanPurchaseItem(SurvivalReservePrice)) return false;
            Save.Wallet -= SurvivalReservePrice;
            Save.PurchasedWarmReserve = true;
            NotifySaveChanged();
            return true;
        }

        private bool CanPurchaseItem(int price)
        {
            return (State == RunState.Menu || State == RunState.GameOver) &&
                price > 0 && Save.Wallet >= price;
        }

        private static int UpgradePrice(int level)
        {
            return level >= MaximumItemUpgradeLevel ? 0 : ItemUpgradeCosts[Math.Max(0, level)];
        }

        private static void SanitizeItemSave(RunnerSave save)
        {
            save.ScoreBoostLevel = Clamp(save.ScoreBoostLevel, 0, MaximumItemUpgradeLevel);
            save.MagnetLevel = Clamp(save.MagnetLevel, 0, MaximumItemUpgradeLevel);
        }

        private void BeginRunItems()
        {
            bonusScoreDistance = 0d;
            scoreBoostRemainingClock = magnetRemainingClock = 0d;
            ShieldReady = Save.PurchasedShield;
            WaterReserveReady = Save.PurchasedWaterReserve;
            WarmReserveReady = Save.PurchasedWarmReserve;
            bool consumedInventory = ShieldReady || WaterReserveReady || WarmReserveReady;
            Save.PurchasedShield = false;
            Save.PurchasedWaterReserve = false;
            Save.PurchasedWarmReserve = false;
            nextPowerupAt = elapsedClock + Range(18f, 26f);
            if (consumedInventory) NotifySaveChanged();
        }

        private void TickItems(float dt, float travel)
        {
            // The bonus applies only to travel inside the active portion of this
            // step, including the step where the timer expires. Never multiply
            // previous points, the star wallet or the distance shown in metres.
            double boostedSeconds = Math.Min((double)dt, scoreBoostRemainingClock);
            if (boostedSeconds > 0d)
                bonusScoreDistance += travel * (boostedSeconds / dt);
            scoreBoostRemainingClock = Math.Max(0d, scoreBoostRemainingClock - dt);
            Score = (int)Math.Min(int.MaxValue, Math.Floor((distanceClock + bonusScoreDistance) / 20d));

            double magnetActiveSeconds = Math.Min((double)dt, magnetRemainingClock);
            magnetRemainingClock = Math.Max(0d, magnetRemainingClock - dt);
            if (magnetActiveSeconds > 0d) AttractStars((float)magnetActiveSeconds);

            if (elapsedClock >= nextPowerupAt)
                nextPowerupAt = elapsedClock + (TrySpawnPowerup() ? Range(20f, 30f) : 1f);
        }

        private void AttractStars(float dt)
        {
            float targetX = PlayerX;
            float targetY = PlayerY + (IsDucking ? DuckHeight : StandingHeight) * 0.45f;
            float maxMove = (850f + Speed * 1.75f) * dt;
            float radiusSquared = MagnetAttractionRadius * MagnetAttractionRadius;
            for (int i = 0; i < entities.Count; i++)
            {
                WorldEntity entity = entities[i];
                if (entity.Removed || entity.Kind != EntityKind.Star) continue;
                float dx = targetX - entity.X - entity.Width * 0.5f;
                float dy = targetY - entity.Y - entity.Height * 0.5f;
                float squared = dx * dx + dy * dy;
                if (squared > radiusSquared || squared <= 0.000001f) continue;
                float distance = (float)Math.Sqrt(squared);
                float fraction = Math.Min(1f, maxMove / distance);
                entity.X += dx * fraction;
                entity.Y += dy * fraction;
            }
        }

        private bool TrySpawnPowerup()
        {
            // Spawn in a currently visible clear lane. Future packs enter from
            // beyond the right edge, so they cannot appear underneath this item.
            float leftLimit = Math.Max(800f, PlayerX + 180f);
            for (float x = CanvasWidth - 60f; x >= leftLimit; x -= 60f)
            {
                bool blocked = false;
                for (int i = 0; i < entities.Count; i++)
                {
                    WorldEntity hazard = entities[i];
                    if (hazard.Removed || !IsPowerupObstacle(hazard.Kind)) continue;
                    float sweep = Math.Abs(hazard.MotionAmplitudeX) * 2f;
                    if (x + 30f > hazard.X - sweep - 64f && x < hazard.X + hazard.Width + sweep + 64f)
                    {
                        blocked = true;
                        break;
                    }
                }
                if (blocked) continue;
                int choice = random.Next(3);
                EntityKind kind = choice == 0 ? EntityKind.DoubleScore
                    : choice == 1 ? EntityKind.Magnet : EntityKind.Shield;
                Add(kind, x, 110f, 30f, 30f);
                return true;
            }
            return false;
        }

        private static bool IsPowerupObstacle(EntityKind kind)
        {
            return kind == EntityKind.House || kind == EntityKind.Hydrant ||
                RunnerBiomeHazards.IsBiomeHazard(kind) ||
                kind == EntityKind.Plane || kind == EntityKind.Gap ||
                kind == EntityKind.Spike || kind == EntityKind.Saw ||
                kind == EntityKind.Barrier || kind == EntityKind.CollapsingFloor;
        }

        private bool HandleItemPickup(WorldEntity entity)
        {
            switch (entity.Kind)
            {
                case EntityKind.DoubleScore:
                    scoreBoostRemainingClock = Math.Min(ScoreBoostDuration * 2d,
                        scoreBoostRemainingClock + ScoreBoostDuration);
                    break;
                case EntityKind.Magnet:
                    magnetRemainingClock = Math.Min(MagnetDuration * 2d,
                        magnetRemainingClock + MagnetDuration);
                    break;
                case EntityKind.Shield:
                    ShieldReady = true;
                    break;
                default:
                    return false;
            }
            entity.Removed = true;
            return true;
        }

        private bool TryAbsorbHazard()
        {
            if (State != RunState.Playing || !ShieldReady) return false;
            ShieldReady = false;
            InvincibleRemaining = Math.Max(InvincibleRemaining, 1f);
            return true;
        }

        private bool TryUseSurvivalReserve(bool cold)
        {
            if (State != RunState.Playing) return false;
            if (cold)
            {
                if (!WarmReserveReady) return false;
                WarmReserveReady = false;
            }
            else
            {
                if (!WaterReserveReady) return false;
                WaterReserveReady = false;
            }
            return true;
        }
    }
}
