using System;
using System.Collections.Generic;

namespace CityDino
{
    public enum RunState { Menu, Playing, Paused, GameOver }
    public enum District { Day, Rain, Sunset, Night, Skyway, Storm }
    public enum EntityKind { House, Hydrant, Plane, Star, Heart, Gap, Spike, Saw, Barrier }

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
        public int Version = 2;
        public int Wallet;
        public int BestScore;
        public int LastScore;
        public int ExtraHeartsLevel;
        public int PurchasedHearts;
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
        public int PatternId;
        // Rotation is expressed in degrees. X/Y are the actual visible and
        // collidable position; motion is applied before collision resolution.
        public float Rotation;
        public float MotionPhase;
        public float BaseY;
        public float MotionAmplitude;
        public float MotionAmplitudeX;
        public float MotionSpeed = 2.1f;
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
        public const float StartPlayerX = 220f;
        public const float MinimumPlayerX = 100f;
        public const float MaximumPlayerX = 720f;
        public const float HorizontalSpeed = 330f;
        public const float PlayerWidth = 60f;
        public const float StandingHeight = 88f;
        public const float DuckHeight = 44f;
        public const float InitialSpeed = 360f;
        public const float SpeedAcceleration = 1.6f;
        public const float MaximumSpeed = 610f;
        public const float Gravity = 1800f;
        public const float JumpVelocity = 780f;
        public const float DistrictDuration = 35f;
        public const float MaximumFrameDelta = 0.25f;
        public const int MaximumHearts = 5;
        public const int MaximumPurchasedHearts = 3;
        public const int HeartPrice = 25;
        private const float BlackoutFade = 0.18f;

        private const float PhysicsStep = 1f / 120f;
        private const float SpawnX = CanvasWidth + 140f;
        private const int WalletLimit = 1000000000;
        private static readonly int[] HeartCosts = { 40, 100, 180 };
        private static readonly District[] AllDistricts =
        {
            District.Day, District.Rain, District.Sunset,
            District.Night, District.Skyway, District.Storm
        };

        private readonly Random random;
        private readonly List<WorldEntity> entities = new List<WorldEntity>();
        private readonly List<PatternType> patternBag = new List<PatternType>();
        private enum PatternType
        {
            Hydrant, House, SingleSpike, DoubleSpike, TripleSpike, Barrier,
            BarrierSteps, GroundSaw, FloatingSaw, Plane, Gap, PatrolSaw, MovingBarrier
        }
        private bool grounded;
        private bool duckRequested;
        private bool recorded;
        private int jumpsUsed;
        private int nextEntityId;
        private float coyoteRemaining;
        private float distanceToPattern;
        private float nextHeartAt;
        private float horizontalAxis;
        private int nextPatternId;
        private int activePatternId;
        private int bagTier = -1;
        private bool bagSkyway;
        private District bagDistrict;
        private PatternType previousPattern;

        public RunnerSession(RunnerSave save, int seed)
        {
            Save = SanitizeSave(save);
            random = new Random(seed);
            State = RunState.Menu;
            PlayerX = StartPlayerX;
            Speed = InitialSpeed;
            CurrentDistrict = District.Day;
            Lives = 1 + Save.PurchasedHearts;
            StageNumber = 1;
            EndReason = "";
        }

        public event Action SaveChanged;
        public RunnerSave Save { get; private set; }
        public IReadOnlyList<WorldEntity> Entities { get { return entities; } }
        public RunState State { get; private set; }
        public District CurrentDistrict { get; private set; }
        // A null selection means a fresh random starting district each run.
        public District? StartingDistrict { get; private set; }
        public float PlayerX { get; private set; }
        public float PlayerY { get; private set; }
        public float VelocityY { get; private set; }
        public float Distance { get; private set; }
        public float Elapsed { get; private set; }
        public float Speed { get; private set; }
        public float InvincibleRemaining { get; private set; }
        public float DistrictElapsed { get; private set; }
        public float NextBlackoutAt { get; private set; }
        // The fully black plateau lasts 2-3 seconds, excluding the soft fades.
        public float BlackoutDuration { get; private set; }
        public int Score { get; private set; }
        public int RunStars { get; private set; }
        public int Lives { get; private set; }
        public int MaxLives { get { return MaximumHearts; } }
        public int StageNumber { get; private set; }
        public bool IsDucking { get; private set; }
        public bool FacingLeft { get; private set; }
        public string EndReason { get; private set; }
        public bool BlackoutWarning
        {
            get
            {
                return HasBlackouts && DistrictElapsed >= NextBlackoutAt - 1.5f &&
                    DistrictElapsed < NextBlackoutAt;
            }
        }
        public float DarknessAmount
        {
            get
            {
                if (!HasBlackouts) return 0f;
                float local = DistrictElapsed - NextBlackoutAt;
                if (local <= 0f || local >= BlackoutDuration + BlackoutFade * 2f) return 0f;
                float fade = Math.Min(1f, Math.Min(local,
                    BlackoutDuration + BlackoutFade * 2f - local) / BlackoutFade);
                return fade * fade * (3f - 2f * fade);
            }
        }
        public int NextHeartCost
        {
            get
            {
                return Save.PurchasedHearts >= MaximumPurchasedHearts ? 0 : HeartPrice;
            }
        }

        public bool SetStartingDistrict(District? district)
        {
            if (State == RunState.Playing || State == RunState.Paused ||
                (district.HasValue && !Enum.IsDefined(typeof(District), district.Value)))
                return false;
            StartingDistrict = district;
            if (State == RunState.Menu)
                CurrentDistrict = district ?? District.Day;
            return true;
        }

        public void StartRun()
        {
            if (State == RunState.Playing || State == RunState.Paused)
                FinishRun("Bắt đầu lượt mới");

            entities.Clear();
            patternBag.Clear();
            nextEntityId = 0;
            nextPatternId = 0;
            activePatternId = 0;
            bagTier = -1;
            previousPattern = PatternType.Hydrant;
            recorded = false;
            State = RunState.Playing;
            CurrentDistrict = StartingDistrict ?? AllDistricts[random.Next(AllDistricts.Length)];
            DistrictElapsed = 0f;
            ScheduleBlackout();
            StageNumber = 1;
            PlayerX = StartPlayerX;
            PlayerY = 0f;
            VelocityY = 0f;
            Distance = 0f;
            Elapsed = 0f;
            Speed = InitialSpeed;
            Score = 0;
            RunStars = 0;
            Lives = 1 + Save.PurchasedHearts;
            bool spentHearts = Save.PurchasedHearts > 0;
            Save.PurchasedHearts = 0;
            InvincibleRemaining = 0f;
            IsDucking = false;
            FacingLeft = false;
            duckRequested = false;
            horizontalAxis = 0f;
            grounded = true;
            jumpsUsed = 0;
            coyoteRemaining = 0.1f;
            EndReason = "";
            if (spentHearts) NotifySaveChanged();

            // A few easy stars teach collecting before the first hazard arrives.
            for (int i = 0; i < 4; i++)
                Add(EntityKind.Star, 560f + i * 46f, 8f, 24f, 24f);

            AddPattern(PatternType.Hydrant);
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

        public void SetHorizontal(float axis)
        {
            horizontalAxis = State == RunState.Playing && IsFinite(axis)
                ? Math.Max(-1f, Math.Min(1f, axis)) : 0f;
        }

        public void TogglePause()
        {
            if (State == RunState.Playing)
            {
                State = RunState.Paused;
                duckRequested = false;
                IsDucking = false;
                horizontalAxis = 0f;
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
            horizontalAxis = 0f;
        }

        public bool BuyExtraHeart()
        {
            if ((State != RunState.Menu && State != RunState.GameOver) ||
                Save.PurchasedHearts >= MaximumPurchasedHearts ||
                Save.Wallet < NextHeartCost)
                return false;

            Save.Wallet -= NextHeartCost;
            Save.PurchasedHearts++;
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
            float previousPlayerX = PlayerX;
            PlayerX = Math.Max(MinimumPlayerX, Math.Min(MaximumPlayerX,
                PlayerX + horizontalAxis * HorizontalSpeed * dt));
            if (Math.Abs(PlayerX - previousPlayerX) > 0.0001f)
                FacingLeft = PlayerX < previousPlayerX;

            DistrictElapsed += dt;
            if (DistrictElapsed >= DistrictDuration)
            {
                DistrictElapsed -= DistrictDuration;
                StageNumber++;
                District previous = CurrentDistrict;
                // Select uniformly from the other districts, starting with
                // the very first transition rather than a scripted first lap.
                int next = random.Next(AllDistricts.Length - 1);
                if (next >= (int)previous) next++;
                CurrentDistrict = AllDistricts[next];
                ScheduleBlackout();
                patternBag.Clear();
                bagTier = -1;
            }
            else if (HasBlackouts && DistrictElapsed >= NextBlackoutAt + BlackoutDuration + BlackoutFade * 2f)
                ScheduleBlackout();

            for (int i = 0; i < entities.Count; i++)
            {
                WorldEntity entity = entities[i];
                entity.X -= travel;
                if (entity.Kind == EntityKind.Saw)
                    entity.Rotation = (entity.Rotation + dt * 280f) % 360f;
                if (entity.MotionAmplitudeX > 0f || entity.MotionAmplitude > 0f)
                {
                    float motion = (float)Math.Sin(Elapsed * entity.MotionSpeed + entity.MotionPhase);
                    if (entity.MotionAmplitudeX > 0f)
                    {
                        // Apply only this substep's offset difference. The road
                        // scroll remains the anchor, without cumulative sine drift.
                        float previousMotion = (float)Math.Sin((Elapsed - dt) * entity.MotionSpeed + entity.MotionPhase);
                        entity.X += (motion - previousMotion) * entity.MotionAmplitudeX;
                    }
                    if (entity.MotionAmplitude > 0f)
                        entity.Y = entity.BaseY + motion * entity.MotionAmplitude;
                }
            }

            distanceToPattern -= travel;
            if (distanceToPattern <= 0f)
            {
                PatternType kind = ChoosePattern();
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
                if (e.Removed || e.Kind == EntityKind.Gap)
                    continue;
                // A rectangle includes the transparent space behind the neck
                // and misses the nose. Saws use the same animated silhouette,
                // pose and facing as the renderer instead.
                bool touches = e.Kind == EntityKind.Saw
                    ? RunnerGeometry.SawTouchesPlayer(e, PlayerX, PlayerY, IsDucking, FacingLeft,
                        RunnerGeometry.GetPlayerFrame(Elapsed, Speed, PlayerY, State),
                        RunnerGeometry.GetPlayerBob(Elapsed, PlayerY, IsDucking, State))
                    : IntersectsPlayer(e, playerLeft, playerRight, playerBottom, playerTop);
                if (!touches)
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

        private static bool IntersectsPlayer(WorldEntity e, float left, float right,
            float bottom, float top)
        {
            if (right <= e.X + 3f || left >= e.X + e.Width - 3f ||
                top <= e.Y + 3f || bottom >= e.Y + e.Height - 3f)
                return false;

            if (e.Kind == EntityKind.Spike)
            {
                // Use the actual triangle rather than its empty upper corners.
                float centre = e.X + e.Width * 0.5f;
                float nearestX = Math.Max(left, Math.Min(right, centre));
                float heightAtOverlap = e.Height * Math.Max(0f,
                    1f - Math.Abs(nearestX - centre) / (e.Width * 0.5f));
                return bottom < e.Y + heightAtOverlap - 3f;
            }
            return true;
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
                FinishRun(reason);
            }
            else
            {
                InvincibleRemaining = 2f;
            }
        }

        private void RecoverFromFall()
        {
            // Remove the hole that caused the fall AND any near-future hole.
            // Resetting only velocity would drop the player through it again.
            for (int i = 0; i < entities.Count; i++)
            {
                WorldEntity e = entities[i];
                if (e.Kind == EntityKind.Gap && e.X < PlayerX + (Speed + HorizontalSpeed) * 2f &&
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
            duckRequested = false;
            horizontalAxis = 0f;
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

        private PatternType ChoosePattern()
        {
            int tier = Elapsed < 15f ? 0 : Elapsed < 30f ? 1 : Elapsed < 60f ? 2 : 3;
            // Starting on Skyway still grants time to learn the jump controls.
            bool skyway = CurrentDistrict == District.Skyway && Elapsed >= 10f;
            bool onlyPrevious = patternBag.Count > 0;
            for (int i = 0; i < patternBag.Count; i++)
                if (patternBag[i] != previousPattern) { onlyPrevious = false; break; }
            if (patternBag.Count == 0 || bagTier != tier || bagSkyway != skyway ||
                bagDistrict != CurrentDistrict || onlyPrevious)
            {
                patternBag.Clear();
                patternBag.Add(PatternType.Hydrant);
                patternBag.Add(PatternType.House);
                patternBag.Add(PatternType.SingleSpike);
                if (tier >= 1)
                {
                    patternBag.Add(PatternType.DoubleSpike);
                    patternBag.Add(PatternType.Barrier);
                    patternBag.Add(PatternType.GroundSaw);
                }
                if (tier >= 2)
                {
                    patternBag.Add(PatternType.TripleSpike);
                    patternBag.Add(PatternType.BarrierSteps);
                    patternBag.Add(PatternType.FloatingSaw);
                    patternBag.Add(PatternType.Plane);
                    patternBag.Add(PatternType.PatrolSaw);
                    patternBag.Add(PatternType.MovingBarrier);
                }
                if (skyway)
                {
                    patternBag.Add(PatternType.Gap);
                    patternBag.Add(PatternType.Gap);
                }
                AddDistrictPatterns(tier);
                for (int i = patternBag.Count - 1; i > 0; i--)
                {
                    int j = random.Next(i + 1);
                    PatternType temporary = patternBag[i];
                    patternBag[i] = patternBag[j];
                    patternBag[j] = temporary;
                }
                bagTier = tier;
                bagSkyway = skyway;
                bagDistrict = CurrentDistrict;
            }

            int last = patternBag.Count - 1;
            if (patternBag[last] == previousPattern)
            {
                // The shuffled bag covers every unlocked pattern, with no
                // immediate repeats unless only identical entries remain.
                for (int i = 0; i < last; i++)
                {
                    if (patternBag[i] == previousPattern) continue;
                    PatternType temporary = patternBag[last];
                    patternBag[last] = patternBag[i];
                    patternBag[i] = temporary;
                    break;
                }
            }
            PatternType chosen = patternBag[last];
            patternBag.RemoveAt(last);
            previousPattern = chosen;
            return chosen;
        }

        private void AddDistrictPatterns(int tier)
        {
            // Districts favour their own rhythms while retaining the common
            // easy packs. Tier unlocks and safe inter-pack spacing still apply.
            switch (CurrentDistrict)
            {
                case District.Day:
                    patternBag.Add(PatternType.House);
                    patternBag.Add(PatternType.Hydrant);
                    break;
                case District.Rain:
                    if (tier >= 1) patternBag.Add(PatternType.Barrier);
                    if (tier >= 2)
                    {
                        patternBag.Add(PatternType.Plane);
                        patternBag.Add(PatternType.Plane);
                    }
                    break;
                case District.Sunset:
                    if (tier >= 1) patternBag.Add(PatternType.DoubleSpike);
                    if (tier >= 2)
                    {
                        patternBag.Add(PatternType.TripleSpike);
                        patternBag.Add(PatternType.BarrierSteps);
                        patternBag.Add(PatternType.MovingBarrier);
                    }
                    break;
                case District.Night:
                    if (tier >= 1) patternBag.Add(PatternType.GroundSaw);
                    if (tier >= 2)
                    {
                        patternBag.Add(PatternType.FloatingSaw);
                        patternBag.Add(PatternType.FloatingSaw);
                        patternBag.Add(PatternType.PatrolSaw);
                    }
                    break;
                case District.Storm:
                    if (tier >= 1)
                    {
                        patternBag.Add(PatternType.GroundSaw);
                        patternBag.Add(PatternType.DoubleSpike);
                    }
                    if (tier >= 2)
                    {
                        patternBag.Add(PatternType.FloatingSaw);
                        patternBag.Add(PatternType.Plane);
                        patternBag.Add(PatternType.MovingBarrier);
                    }
                    break;
            }
        }

        private bool HasBlackouts
        {
            get { return State != RunState.Menu && (CurrentDistrict == District.Night || CurrentDistrict == District.Storm); }
        }

        private void ScheduleBlackout()
        {
            bool storm = CurrentDistrict == District.Storm;
            if (!storm && CurrentDistrict != District.Night)
            {
                NextBlackoutAt = BlackoutDuration = 0f;
                return;
            }
            NextBlackoutAt = DistrictElapsed + Range(storm ? 7f : 8f, storm ? 11f : 13f);
            BlackoutDuration = Range(2f, 3f);
        }

        private float AddPattern(PatternType kind)
        {
            activePatternId = ++nextPatternId;
            float width;
            float height;
            float starBase;
            if (kind == PatternType.Gap)
            {
                width = Range(140f, 205f);
                Add(EntityKind.Gap, SpawnX, 0f, width, 160f);
                starBase = 95f;
            }
            else if (kind == PatternType.Plane || kind == PatternType.FloatingSaw)
            {
                width = kind == PatternType.Plane ? 110f : 76f;
                if (kind == PatternType.Plane)
                    Add(EntityKind.Plane, SpawnX, 57f, width, 42f, random.Next(3));
                else
                {
                    WorldEntity saw = Add(EntityKind.Saw, SpawnX, 72f, width, 76f, 1);
                    SetMotion(saw, 0f, 16f, 1.8f);
                }
                // No floor hazard occupies this pack: the low star lane tells
                // the player to duck beneath the airborne obstacle.
                for (int i = 0; i < 4; i++)
                    Add(EntityKind.Star, SpawnX - 35f + i * 48f, 7f, 24f, 24f);
                activePatternId = 0;
                return width;
            }
            else if (kind == PatternType.SingleSpike || kind == PatternType.DoubleSpike ||
                kind == PatternType.TripleSpike)
            {
                int count = kind == PatternType.TripleSpike ? 3 : kind == PatternType.DoubleSpike ? 2 : 1;
                const float spikeWidth = 44f;
                const float spikeStep = 58f;
                height = Elapsed < 60f ? 56f : 68f;
                width = spikeWidth + (count - 1) * spikeStep;
                for (int i = 0; i < count; i++)
                    Add(EntityKind.Spike, SpawnX + i * spikeStep, 0f, spikeWidth, height, i % 2);
                starBase = height + 34f;
            }
            else if (kind == PatternType.BarrierSteps)
            {
                width = 152f;
                height = Elapsed < 60f ? 88f : 104f;
                Add(EntityKind.Barrier, SpawnX, 0f, 58f, 52f, 0);
                Add(EntityKind.Barrier, SpawnX + 94f, 0f, 58f, height, 1);
                starBase = height + 25f;
            }
            else if (kind == PatternType.MovingBarrier)
            {
                const float objectWidth = 70f;
                const float amplitude = 24f;
                height = 76f;
                width = objectWidth + amplitude * 2f;
                WorldEntity barrier = Add(EntityKind.Barrier, SpawnX, 0f, objectWidth, height, 1);
                SetMotion(barrier, amplitude, 0f, 1.3f);
                starBase = height + 28f;
            }
            else if (kind == PatternType.Barrier)
            {
                width = Range(66f, 94f);
                height = Range(65f, Elapsed < 60f ? 88f : 100f);
                Add(EntityKind.Barrier, SpawnX, 0f, width, height, random.Next(2));
                starBase = height + 28f;
            }
            else if (kind == PatternType.PatrolSaw)
            {
                const float objectWidth = 66f;
                const float amplitude = 32f;
                height = 66f;
                width = objectWidth + amplitude * 2f;
                WorldEntity saw = Add(EntityKind.Saw, SpawnX, 0f, objectWidth, height, 2);
                SetMotion(saw, amplitude, 0f, 1.5f);
                starBase = 94f;
            }
            else if (kind == PatternType.GroundSaw)
            {
                width = 66f;
                height = 66f;
                Add(EntityKind.Saw, SpawnX, 0f, width, height);
                starBase = 94f;
            }
            else if (kind == PatternType.House)
            {
                width = Range(76f, 98f);
                height = StageNumber <= 2 ? Range(64f, 82f) : Range(70f, 94f);
                Add(EntityKind.House, SpawnX, 0f, width, height, random.Next(3));
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
            activePatternId = 0;
            return width;
        }

        private void SetMotion(WorldEntity entity, float horizontalAmplitude,
            float verticalAmplitude, float speed)
        {
            entity.MotionAmplitudeX = horizontalAmplitude;
            entity.MotionAmplitude = verticalAmplitude;
            entity.MotionSpeed = speed;
            entity.MotionPhase = Range(0f, (float)Math.PI * 2f);
            float motion = (float)Math.Sin(Elapsed * speed + entity.MotionPhase);
            // All phases stay inside a known spawn envelope. AddPattern returns
            // its full swept width, so the next pack retains recovery space.
            entity.X += horizontalAmplitude * (1f + motion);
            entity.Y = entity.BaseY + verticalAmplitude * motion;
        }

        private float NextPatternSpacing(float previousWidth)
        {
            // previousWidth includes horizontal moving hazards' full envelope.
            // Packs are hand-authored to share one safe jump/duck profile.
            // Separate packs retain recovery room even while moving right into
            // the scroll. The small speed look-ahead covers travel from spawn
            // to the player; short clusters still feel rhythmic rather than sparse.
            float approachSpeed = Math.Min(MaximumSpeed, Speed + SpeedAcceleration * 4f);
            float recoveryGap = Math.Max((approachSpeed + HorizontalSpeed) * 0.72f,
                approachSpeed * 1.05f);
            return previousWidth + recoveryGap + Range(25f, 120f);
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
                Variant = variant,
                BaseY = y,
                PatternId = activePatternId
            };
            entities.Add(entity);
            return entity;
        }

        private float Range(float minimum, float maximum)
        {
            return minimum + (float)random.NextDouble() * (maximum - minimum);
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

        public static string DistrictChallenge(District district)
        {
            switch (district)
            {
                case District.Rain: return "Cúi tránh bẫy bay, nhảy qua rào chắn.";
                case District.Sunset: return "Canh nhịp vượt cụm gai và rào bậc thang.";
                case District.Night: return "Nhiều cưa xoay; đèn tắt ngắn có báo trước.";
                case District.Skyway: return "Nhảy qua khe cầu; 10 giây đầu chưa có hố.";
                case District.Storm: return "Đổi nhịp nhảy/cúi; trời tối ngắn có báo trước.";
                default: return "Vượt nhà và trụ nước, làm quen nhịp chạy.";
            }
        }

        private static RunnerSave SanitizeSave(RunnerSave input)
        {
            RunnerSave save = input ?? new RunnerSave();
            save.Wallet = Clamp(save.Wallet, 0, WalletLimit);
            save.BestScore = Math.Max(0, save.BestScore);
            save.LastScore = Math.Max(0, save.LastScore);
            if (save.Version < 2)
            {
                int legacyLevel = Clamp(save.ExtraHeartsLevel, 0, HeartCosts.Length);
                for (int i = 0; i < legacyLevel; i++)
                    save.Wallet = Math.Min(WalletLimit, save.Wallet + HeartCosts[i]);
            }
            save.Version = 2;
            save.ExtraHeartsLevel = 0;
            save.PurchasedHearts = Clamp(save.PurchasedHearts, 0, MaximumPurchasedHearts);
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
