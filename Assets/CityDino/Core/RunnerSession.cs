using System;
using System.Collections.Generic;

namespace CityDino
{
    public enum RunState { Menu, Playing, Paused, GameOver }
    public enum District { Day, Rain, Sunset, Night, Skyway, Storm }
    public enum EntityKind { House, Hydrant, Plane, Star, Heart, Gap }

    [Serializable]
    public sealed class RunRecord
    {
        public int Score;
        public int Stars;
        public float Distance;
        public string EndReason;
    }

    [Serializable]
    public sealed class RunnerSave
    {
        public int Version = 1;
        public int Wallet;
        public int BestScore;
        public int LastScore;
        public int ExtraHeartsLevel;
        public List<RunRecord> History = new List<RunRecord>();
    }

    // Coordinates use a 1280 x 720 virtual canvas. X is the left edge; Y is
    // the bottom's height above the road. The road is rendered at canvas Y=560.
    public sealed class WorldEntity
    {
        public int Id;
        public int Variant;
        public EntityKind Kind;
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public bool Removed;
    }

    /// <summary>
    /// Engine-independent endless runner simulation. No Rigidbody/scrolling
    /// collider is used: the floor and all hazards share one coordinate system.
    /// Update is internally substepped to keep fast hazards from tunnelling.
    /// </summary>
    public sealed class RunnerSession
    {
        public const float CanvasWidth = 1280f;
        public const float CanvasHeight = 720f;
        public const float GroundY = 560f;
        public const float PlayerX = 220f;
        public const float PlayerWidth = 60f;
        public const float StandingHeight = 88f;
        public const float DuckHeight = 44f;
        public const float InitialSpeed = 250f;
        public const float SpeedAcceleration = 1.1f;
        public const float MaximumSpeed = 430f;
        public const float Gravity = 1800f;
        public const float JumpVelocity = 780f;
        public const float DistrictDuration = 35f;
        public const float MaximumFrameDelta = 0.25f;
        public const int MaximumHearts = 5;
        public const int MaximumUpgradeLevel = 3;

        private const float PhysicsStep = 1f / 120f;
        private const float SpawnX = CanvasWidth + 140f;
        private const int WalletLimit = 1000000000;
        private static readonly int[] HeartCosts = { 40, 100, 180 };
        private static readonly District[] FirstDistricts =
        {
            District.Day, District.Rain, District.Sunset,
            District.Night, District.Skyway, District.Storm
        };

        private readonly Random random;
        private readonly List<WorldEntity> entities = new List<WorldEntity>();
        private bool grounded;
        private bool duckRequested;
        private bool recorded;
        private int jumpsUsed;
        private int nextEntityId;
        private float coyoteRemaining;
        private float distanceToPattern;
        private float nextHeartAt;

        public RunnerSession(RunnerSave save, int seed)
        {
            Save = SanitizeSave(save);
            random = new Random(seed);
            State = RunState.Menu;
            Speed = InitialSpeed;
            CurrentDistrict = District.Day;
            Lives = 1 + Save.ExtraHeartsLevel;
            StageNumber = 1;
            EndReason = "";
            Toast = "";
        }

        public event Action SaveChanged;
        public RunnerSave Save { get; private set; }
        public IReadOnlyList<WorldEntity> Entities { get { return entities; } }
        public RunState State { get; private set; }
        public District CurrentDistrict { get; private set; }
        public float PlayerY { get; private set; }
        public float VelocityY { get; private set; }
        public float Distance { get; private set; }
        public float Elapsed { get; private set; }
        public float Speed { get; private set; }
        public float InvincibleRemaining { get; private set; }
        public float DistrictElapsed { get; private set; }
        public int Score { get; private set; }
        public int RunStars { get; private set; }
        public int Lives { get; private set; }
        public int MaxLives { get { return MaximumHearts; } }
        public int StageNumber { get; private set; }
        public bool IsDucking { get; private set; }
        public string EndReason { get; private set; }
        public string Toast { get; private set; }
        public float ToastRemaining { get; private set; }
        public int NextHeartCost
        {
            get
            {
                return Save.ExtraHeartsLevel >= MaximumUpgradeLevel
                    ? 0 : HeartCosts[Save.ExtraHeartsLevel];
            }
        }

        public void StartRun()
        {
            if (State == RunState.Playing || State == RunState.Paused)
                FinishRun("Bắt đầu lượt mới");

            entities.Clear();
            nextEntityId = 0;
            recorded = false;
            State = RunState.Playing;
            CurrentDistrict = District.Day;
            DistrictElapsed = 0f;
            StageNumber = 1;
            PlayerY = 0f;
            VelocityY = 0f;
            Distance = 0f;
            Elapsed = 0f;
            Speed = InitialSpeed;
            Score = 0;
            RunStars = 0;
            Lives = 1 + Save.ExtraHeartsLevel;
            InvincibleRemaining = 0f;
            IsDucking = false;
            duckRequested = false;
            grounded = true;
            jumpsUsed = 0;
            coyoteRemaining = 0.1f;
            EndReason = "";
            ShowToast("Nhảy qua vật cản · Nhấn lần nữa để nhảy kép", 4f);

            // A few easy stars teach collecting before the first hazard arrives.
            for (int i = 0; i < 4; i++)
                Add(EntityKind.Star, 560f + i * 46f, 8f, 24f, 24f);

            AddPattern(EntityKind.Hydrant);
            distanceToPattern = NextPatternSpacing(38f);
            nextHeartAt = Range(20f, 30f);
        }

        public void Update(float deltaTime)
        {
            if (State != RunState.Playing || !IsFinite(deltaTime) || deltaTime <= 0f)
                return;

            // A suspended tab does not fast-forward the runner into an unseen hit.
            float remaining = Math.Min(deltaTime, MaximumFrameDelta);
            while (remaining > 0.000001f && State == RunState.Playing)
            {
                float step = Math.Min(remaining, PhysicsStep);
                Step(step);
                remaining -= step;
            }
        }

        public bool Jump()
        {
            if (State != RunState.Playing)
                return false;

            if (grounded || coyoteRemaining > 0f)
                jumpsUsed = 1;
            else if (jumpsUsed < 2)
                jumpsUsed++;
            else
                return false;

            VelocityY = JumpVelocity;
            grounded = false;
            coyoteRemaining = 0f;
            duckRequested = false;
            IsDucking = false;
            return true;
        }

        public void SetDucking(bool ducking)
        {
            duckRequested = State == RunState.Playing && ducking;
            IsDucking = duckRequested && grounded;
        }

        public void TogglePause()
        {
            if (State == RunState.Playing)
            {
                State = RunState.Paused;
                duckRequested = false;
                IsDucking = false;
            }
            else if (State == RunState.Paused)
                State = RunState.Playing;
        }

        public void ReturnToMenu()
        {
            if (State == RunState.Playing || State == RunState.Paused)
                FinishRun("Trở về menu");
            State = RunState.Menu;
            IsDucking = false;
            duckRequested = false;
        }

        public bool BuyExtraHeart()
        {
            if ((State != RunState.Menu && State != RunState.GameOver) ||
                Save.ExtraHeartsLevel >= MaximumUpgradeLevel ||
                Save.Wallet < NextHeartCost)
                return false;

            Save.Wallet -= NextHeartCost;
            Save.ExtraHeartsLevel++;
            ShowToast("Đã nâng cấp! Lượt sau bắt đầu với " +
                (1 + Save.ExtraHeartsLevel) + " mạng.", 4f);
            NotifySaveChanged();
            return true;
        }

        private void Step(float dt)
        {
            Elapsed += dt;
            Speed = Math.Min(MaximumSpeed, InitialSpeed + Elapsed * SpeedAcceleration);
            float travel = Speed * dt;
            Distance += travel;
            Score = (int)Math.Min(int.MaxValue, Math.Floor(Distance / 20d));
            InvincibleRemaining = Math.Max(0f, InvincibleRemaining - dt);
            ToastRemaining = Math.Max(0f, ToastRemaining - dt);

            DistrictElapsed += dt;
            if (DistrictElapsed >= DistrictDuration)
            {
                DistrictElapsed -= DistrictDuration;
                StageNumber++;
                District previous = CurrentDistrict;
                if (StageNumber <= FirstDistricts.Length)
                    CurrentDistrict = FirstDistricts[StageNumber - 1];
                else
                {
                    do { CurrentDistrict = (District)random.Next(FirstDistricts.Length); }
                    while (CurrentDistrict == previous);
                }
                ShowToast("Khu vực mới: " + DistrictName(CurrentDistrict), 4f);
            }

            for (int i = 0; i < entities.Count; i++)
                entities[i].X -= travel;

            distanceToPattern -= travel;
            if (distanceToPattern <= 0f)
            {
                EntityKind kind = ChoosePattern();
                float width = AddPattern(kind);
                distanceToPattern += NextPatternSpacing(width);
            }

            if (Elapsed >= nextHeartAt)
            {
                // The timer is independent of obstacle spawns. Hearts occur only
                // every 18-32 seconds, never as a repeating row of pickups.
                nextHeartAt = Elapsed + Range(18f, 32f);
                EntityKind pickup = Lives < MaximumHearts ? EntityKind.Heart : EntityKind.Star;
                Add(pickup, SpawnX + 70f, 108f, 30f, 30f);
            }

            IntegratePlayer(dt);
            if (PlayerY < -180f)
            {
                if (InvincibleRemaining <= 0f)
                    TakeHit("Rơi khỏi đường chạy");
                RecoverFromFall();
            }

            if (State == RunState.Playing)
                ResolveCollisions();

            for (int i = entities.Count - 1; i >= 0; i--)
                if (entities[i].Removed || entities[i].X + entities[i].Width < -100f)
                    entities.RemoveAt(i);
        }

        private void IntegratePlayer(float dt)
        {
            bool floor = HasFloor();
            if (grounded && !floor)
                grounded = false;

            if (grounded)
            {
                PlayerY = 0f;
                VelocityY = 0f;
                jumpsUsed = 0;
                coyoteRemaining = 0.1f;
            }
            else
            {
                coyoteRemaining = Math.Max(0f, coyoteRemaining - dt);
                if (coyoteRemaining <= 0f && jumpsUsed == 0)
                    jumpsUsed = 1;

                float previousY = PlayerY;
                VelocityY -= Gravity * dt;
                PlayerY += VelocityY * dt;
                if (VelocityY <= 0f && previousY >= 0f && PlayerY <= 0f && floor)
                {
                    PlayerY = 0f;
                    VelocityY = 0f;
                    grounded = true;
                    jumpsUsed = 0;
                    coyoteRemaining = 0.1f;
                }
            }
            IsDucking = duckRequested && grounded;
        }

        private bool HasFloor()
        {
            for (int i = 0; i < entities.Count; i++)
            {
                WorldEntity e = entities[i];
                if (!e.Removed && e.Kind == EntityKind.Gap &&
                    PlayerX > e.X + 8f && PlayerX < e.X + e.Width - 8f)
                    return false;
            }
            return true;
        }

        private void ResolveCollisions()
        {
            float playerLeft = PlayerX - PlayerWidth * 0.5f + 7f;
            float playerRight = PlayerX + PlayerWidth * 0.5f - 7f;
            float playerBottom = PlayerY + 5f;
            float playerTop = PlayerY + (IsDucking ? DuckHeight : StandingHeight) - 5f;

            for (int i = 0; i < entities.Count && State == RunState.Playing; i++)
            {
                WorldEntity e = entities[i];
                if (e.Removed || e.Kind == EntityKind.Gap ||
                    playerRight <= e.X + 3f || playerLeft >= e.X + e.Width - 3f ||
                    playerTop <= e.Y + 3f || playerBottom >= e.Y + e.Height - 3f)
                    continue;

                if (e.Kind == EntityKind.Star)
                {
                    e.Removed = true;
                    RunStars = Math.Min(WalletLimit, RunStars + 1);
                    Save.Wallet = Math.Min(WalletLimit, Save.Wallet + 1);
                    NotifySaveChanged();
                }
                else if (e.Kind == EntityKind.Heart)
                {
                    e.Removed = true;
                    if (Lives < MaximumHearts)
                    {
                        Lives++;
                        ShowToast("Thêm một mạng!", 2.5f);
                    }
                    else
                    {
                        RunStars = Math.Min(WalletLimit, RunStars + 1);
                        Save.Wallet = Math.Min(WalletLimit, Save.Wallet + 1);
                        NotifySaveChanged();
                    }
                }
                else if (InvincibleRemaining <= 0f)
                {
                    e.Removed = true;
                    TakeHit(e.Kind == EntityKind.Plane
                        ? "Va vào máy bay · Hãy cúi xuống"
                        : "Va vào chướng ngại vật");
                }
            }
        }

        private void TakeHit(string reason)
        {
            if (State != RunState.Playing || InvincibleRemaining > 0f)
                return;

            Lives = Math.Max(0, Lives - 1);
            if (Lives == 0)
            {
                VelocityY = 0f;
                PlayerY = Math.Max(0f, PlayerY);
                IsDucking = false;
                FinishRun(reason);
            }
            else
            {
                InvincibleRemaining = 2f;
                ShowToast("Mất một mạng · Bất tử trong 2 giây", 2f);
            }
        }

        private void RecoverFromFall()
        {
            // Remove the hole that caused the fall AND any near-future hole.
            // Resetting only velocity would drop the player through it again.
            for (int i = 0; i < entities.Count; i++)
            {
                WorldEntity e = entities[i];
                if (e.Kind == EntityKind.Gap && e.X < PlayerX + Speed * 2f &&
                    e.X + e.Width > PlayerX - 100f)
                    e.Removed = true;
            }
            PlayerY = 0f;
            VelocityY = 0f;
            grounded = true;
            jumpsUsed = 0;
            coyoteRemaining = 0.1f;
        }

        private void FinishRun(string reason)
        {
            State = RunState.GameOver;
            EndReason = reason;
            VelocityY = 0f;
            IsDucking = false;
            duckRequested = false;
            if (recorded)
                return;

            recorded = true;
            Save.LastScore = Score;
            Save.BestScore = Math.Max(Save.BestScore, Score);
            Save.History.Insert(0, new RunRecord
            {
                Score = Score,
                Stars = RunStars,
                Distance = Distance,
                EndReason = reason
            });
            if (Save.History.Count > 10)
                Save.History.RemoveRange(10, Save.History.Count - 10);
            NotifySaveChanged();
        }

        private EntityKind ChoosePattern()
        {
            double roll = random.NextDouble();
            if (CurrentDistrict == District.Skyway && roll < 0.4d)
                return EntityKind.Gap;
            if (StageNumber >= 3 && roll > 0.81d)
                return EntityKind.Plane;
            return roll < 0.48d ? EntityKind.Hydrant : EntityKind.House;
        }

        private float AddPattern(EntityKind kind)
        {
            float width;
            float height;
            float starBase;
            if (kind == EntityKind.Gap)
            {
                width = Range(105f, 155f);
                Add(kind, SpawnX, 0f, width, 160f);
                starBase = 95f;
            }
            else if (kind == EntityKind.Plane)
            {
                width = 110f;
                height = 42f;
                Add(kind, SpawnX, 57f, width, height, random.Next(3));
                // Ducking goes beneath the plane. Collectibles reinforce the safe lane.
                for (int i = 0; i < 4; i++)
                    Add(EntityKind.Star, SpawnX - 35f + i * 48f, 7f, 24f, 24f);
                return width;
            }
            else if (kind == EntityKind.House)
            {
                width = Range(76f, 98f);
                height = StageNumber <= 2 ? Range(64f, 82f) : Range(70f, 94f);
                Add(kind, SpawnX, 0f, width, height, random.Next(3));
                starBase = height + 28f;
            }
            else
            {
                width = 38f;
                height = 54f;
                Add(EntityKind.Hydrant, SpawnX, 0f, width, height);
                starBase = 84f;
            }

            for (int i = 0; i < 5; i++)
            {
                float arc = (float)Math.Sin(i * Math.PI / 4d) * 32f;
                float x = SpawnX - 70f + i * (width + 140f) / 4f;
                Add(EntityKind.Star, x, starBase + arc, 24f, 24f);
            }
            return width;
        }

        private float NextPatternSpacing(float previousWidth)
        {
            // At top speed there is still >1.6 seconds between hazards. A single
            // jump lasts ~0.87 seconds; double jumps remain a recovery option.
            // Only one hazard is placed per pack, so a house cannot cover a hole.
            return Math.Max(420f, Speed * 1.6f + previousWidth) + Range(35f, 155f);
        }

        private WorldEntity Add(EntityKind kind, float x, float y,
            float width, float height, int variant = 0)
        {
            WorldEntity entity = new WorldEntity
            {
                Id = ++nextEntityId,
                Kind = kind,
                X = x,
                Y = y,
                Width = width,
                Height = height,
                Variant = variant
            };
            entities.Add(entity);
            return entity;
        }

        private float Range(float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
        }

        private void ShowToast(string text, float seconds)
        {
            Toast = text;
            ToastRemaining = seconds;
        }

        private void NotifySaveChanged()
        {
            Action handler = SaveChanged;
            if (handler != null)
                handler();
        }

        public static string DistrictName(District district)
        {
            switch (district)
            {
                case District.Rain: return "Thành phố mưa";
                case District.Sunset: return "Nắng chiều";
                case District.Night: return "Đêm neon";
                case District.Skyway: return "Đường trên mây";
                case District.Storm: return "Giông bão";
                default: return "Thành phố ban ngày";
            }
        }

        private static RunnerSave SanitizeSave(RunnerSave input)
        {
            RunnerSave save = input ?? new RunnerSave();
            save.Version = 1;
            save.Wallet = Clamp(save.Wallet, 0, WalletLimit);
            save.BestScore = Math.Max(0, save.BestScore);
            save.LastScore = Math.Max(0, save.LastScore);
            save.ExtraHeartsLevel = Clamp(save.ExtraHeartsLevel, 0, MaximumUpgradeLevel);
            if (save.History == null)
                save.History = new List<RunRecord>();
            for (int i = save.History.Count - 1; i >= 0; i--)
            {
                RunRecord record = save.History[i];
                if (record == null)
                {
                    save.History.RemoveAt(i);
                    continue;
                }
                record.Score = Math.Max(0, record.Score);
                record.Stars = Clamp(record.Stars, 0, WalletLimit);
                record.Distance = IsFinite(record.Distance)
                    ? Math.Max(0f, record.Distance) : 0f;
                record.EndReason = record.EndReason ?? "";
                save.BestScore = Math.Max(save.BestScore, record.Score);
            }
            if (save.History.Count > 10)
                save.History.RemoveRange(10, save.History.Count - 10);
            save.BestScore = Math.Max(save.BestScore, save.LastScore);
            return save;
        }

        private static int Clamp(int value, int minimum, int maximum)
        {
            return Math.Min(maximum, Math.Max(minimum, value));
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }
    }
}
