using System;
using System.Collections.Generic;

namespace CityDino
{
    public enum RunState { Menu, Playing, Paused, GameOver }
    public enum District { Day, Snow, Sunset, Night, Skyway, Storm }
    public enum EntityKind { House, Hydrant, Plane, Star, Heart, Gap, Spike, Saw, Barrier, Water, HotDrink, DoubleScore, Magnet, Shield, CollapsingFloor, FallingIce, HeatVent, StormDebris }

    [Serializable]
    public sealed class RunRecord
    {
        public int Score;
        public int Stars;
        public float Distance;
        public string EndReason;
    }

    [Serializable]
    public sealed partial class RunnerSave
    {
        public int Version = 3;
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
        public float CollapseProgress;
        public bool Collapsed;
        // The renderer consumes these exact production states. Warnings and
        // cooldown hardware are never invisible damage volumes.
        public float HazardAge;
        public float HazardWarning;
        public float HazardHeight;
        public float HazardVelocityY;
        public bool HazardActive;
        public int HazardPhase;
        public bool IsOpenGap { get { return Kind == EntityKind.Gap || (Kind == EntityKind.CollapsingFloor && Collapsed); } }
    }

    /// <summary>
    /// Engine-independent endless runner simulation. No Rigidbody/scrolling
    /// collider is used: the floor and all hazards share one coordinate system.
    /// Update is internally substepped to keep fast hazards from tunnelling.
    /// </summary>
    public sealed partial class RunnerSession
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
        public const float SpeedAcceleration = 2.2f;
        public const float Gravity = 1800f;
        public const float JumpVelocity = 780f;
        public const float DistrictDuration = 35f;
        public const float MaximumFrameDelta = 0.25f;
        public const int MaximumHearts = 5;
        public const int MaximumPurchasedHearts = 3;
        public const int HeartPrice = 100;
        private const float BlackoutFade = 0.18f;

        private const float PhysicsStep = 1f / 120f;
        private const float SpawnX = CanvasWidth + 140f;
        private const int WalletLimit = 1000000000;
        private static readonly int[] LegacyHeartCosts = { 40, 100, 180 };
        private static readonly int[] PurchasedHeartCosts = { 100, 300, 600 };
        private static readonly District[] AllDistricts =
        {
            District.Day, District.Snow, District.Sunset,
            District.Night, District.Skyway, District.Storm
        };

        private readonly Random random;
        private readonly List<WorldEntity> entities = new List<WorldEntity>();
        private readonly List<PatternType> patternBag = new List<PatternType>();
        private readonly List<District> districtBag = new List<District>();
        private enum PatternType
        {
            Hydrant, House, SingleSpike, DoubleSpike, TripleSpike, Barrier,
            BarrierSteps, GroundSaw, FloatingSaw, Plane, Gap, PatrolSaw, MovingBarrier,
            MovingPlane, DoubleGap, CollapsingBridge, FallingIce, HeatVent, StormDebris
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
        private double nextSurvivalSupplyAt;
        // Long runs use double accumulation: a tiny adaptive substep must not
        // disappear when the public float clock has reached a large value.
        private double elapsedClock;
        private double distanceClock;
        private double districtClock;
        private int nextPatternId;
        private int activePatternId;
        private int bagTier = -1;
        private bool bagSkyway;
        private District bagDistrict;
        private PatternType previousPattern;
        private bool biomeShowcaseSpawned;

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
        public float HorizontalVelocity { get; private set; }
        public float Water { get; private set; } = 100f;
        public float Warmth { get; private set; } = 100f;
        public float WindStrength
        {
            get
            {
                if (CurrentDistrict != District.Skyway || State == RunState.Menu || DistrictElapsed < 3f) return 0f;
                float phase = (DistrictElapsed - 3f) % 8f;
                if (phase >= 5f) return 0.1f;
                float wave = (float)Math.Sin(Math.PI * phase / 5f);
                return 0.3f + 0.7f * wave * wave;
            }
        }
        public float WindPush
        {
            get
            {
                float strength = Math.Min(HorizontalSpeed * 0.55f, 45f + 12f * (float)Math.Sqrt(Math.Max(0f, Elapsed) / 120f));
                return -strength * WindStrength * (grounded ? 0.35f : 1f);
            }
        }
        public float VelocityY { get; private set; }
        // Rendering keeps the last physical pose even when a fatal hit stops
        // VelocityY, so the death frame cannot switch from falling to rising.
        public float AnimationVelocityY { get; private set; }
        public float Distance { get; private set; }
        public float Elapsed { get; private set; }
        public float Speed { get; private set; }
        // A spawned pack keeps its exact envelope. Later packs gradually move
        // farther and faster; there is no discontinuous stage-based multiplier.
        public float MotionRangeScale
        {
            get { return 1f + 0.35f * (float)Math.Sqrt(Math.Max(0f, Elapsed) / 120f); }
        }
        public float MotionFrequencyScale
        {
            get { return 1f + 0.4f * (float)Math.Sqrt(Math.Max(0f, Elapsed) / 120f); }
        }
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
                return Save.PurchasedHearts >= MaximumPurchasedHearts ? 0 : PurchasedHeartCosts[Save.PurchasedHearts];
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
            districtBag.Clear();
            RefillDistrictBag();
            CurrentDistrict = StartingDistrict ?? DrawDistrict(null);
            if (StartingDistrict.HasValue) districtBag.Remove(StartingDistrict.Value);
            DistrictElapsed = 0f;
            districtClock = 0d;
            StageNumber = 1;
            PlayerX = StartPlayerX;
            HorizontalVelocity = 0f;
            PlayerY = 0f;
            VelocityY = 0f;
            AnimationVelocityY = 0f;
            Distance = 0f;
            Elapsed = 0f;
            distanceClock = elapsedClock = 0d;
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
            EnterDistrict();
            BeginRunItems();
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
            double remaining = Math.Min((double)deltaTime, MaximumFrameDelta);
            while (remaining > 0.00000001d && State == RunState.Playing)
            {
                float step = (float)Math.Min(remaining, CollisionStep());
                Step(step);
                remaining -= step;
            }
        }

        private float CollisionStep()
        {
            float motionX = 0f;
            float motionY = 0f;
            for (int i = 0; i < entities.Count; i++)
            {
                WorldEntity entity = entities[i];
                if (entity.Removed) continue;
                motionX = Math.Max(motionX, Math.Abs(entity.MotionAmplitudeX * entity.MotionSpeed));
                motionY = Math.Max(motionY, Math.Abs(entity.MotionAmplitude * entity.MotionSpeed));
                motionY = Math.Max(motionY, Math.Abs(entity.HazardVelocityY));
                if (entity.Kind == EntityKind.FallingIce)
                    motionY = Math.Max(motionY, Speed * 1.6f);
                else if (entity.Kind == EntityKind.HeatVent)
                    motionY = Math.Max(motionY, entity.Height / 0.12f * Speed / InitialSpeed /
                        RunnerBiomeHazards.GetVentCycleScale(entity));
            }
            // Bound displacement, not speed. At high runner/moving-hazard speeds
            // smaller physics steps still detect thin spikes, pickups and ledges.
            float currentSpeed = (float)(InitialSpeed + elapsedClock * SpeedAcceleration);
            float horizontal = currentSpeed + SpeedAcceleration * PhysicsStep + HorizontalSpeed + Math.Abs(WindPush) + motionX;
            float vertical = Math.Abs(VelocityY) + Gravity * PhysicsStep + motionY;
            return Math.Min(PhysicsStep, 6f / Math.Max(1f, Math.Max(horizontal, vertical)));
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
            AnimationVelocityY = JumpVelocity;
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
            HorizontalVelocity = 0f;
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
            elapsedClock += dt;
            Elapsed = (float)elapsedClock;
            Speed = (float)(InitialSpeed + elapsedClock * SpeedAcceleration);
            float travel = Speed * dt;
            distanceClock += travel;
            Distance = (float)distanceClock;
            InvincibleRemaining = Math.Max(0f, InvincibleRemaining - dt);
            float previousPlayerX = PlayerX;
            IntegrateHorizontal(dt);
            if (Math.Abs(PlayerX - previousPlayerX) > 0.0001f)
                FacingLeft = PlayerX < previousPlayerX;

            districtClock += dt;
            DistrictElapsed = (float)districtClock;
            if (districtClock >= DistrictDuration)
            {
                districtClock -= DistrictDuration;
                DistrictElapsed = (float)districtClock;
                StageNumber++;
                District previous = CurrentDistrict;
                CurrentDistrict = DrawDistrict(previous);
                EnterDistrict();
                patternBag.Clear();
                bagTier = -1;
            }
            else if (HasBlackouts && DistrictElapsed >= NextBlackoutAt + BlackoutDuration + BlackoutFade * 2f)
                ScheduleBlackout();

            for (int i = 0; i < entities.Count; i++)
            {
                WorldEntity entity = entities[i];
                entity.X -= travel;
                if (entity.Kind == EntityKind.CollapsingFloor)
                {
                    float crackStart = CanvasWidth + 60f;
                    float collapseX = MaximumPlayerX + 100f;
                    entity.CollapseProgress = Math.Max(entity.CollapseProgress,
                        Math.Max(0f, Math.Min(1f, (crackStart - entity.X) / (crackStart - collapseX))));
                    if (entity.CollapseProgress >= 1f) entity.Collapsed = true;
                }
                if (entity.Kind == EntityKind.Saw)
                    entity.Rotation = (entity.Rotation + dt * 280f * MotionFrequencyScale) % 360f;
                if (entity.MotionAmplitudeX > 0f || entity.MotionAmplitude > 0f)
                {
                    float motion = (float)Math.Sin(elapsedClock * entity.MotionSpeed + entity.MotionPhase);
                    if (entity.MotionAmplitudeX > 0f)
                    {
                        // Apply only this substep's offset difference. The road
                        // scroll remains the anchor, without cumulative sine drift.
                        float previousMotion = (float)Math.Sin((elapsedClock - dt) * entity.MotionSpeed + entity.MotionPhase);
                        entity.X += (motion - previousMotion) * entity.MotionAmplitudeX;
                    }
                    if (entity.MotionAmplitude > 0f)
                        entity.Y = entity.BaseY + motion * entity.MotionAmplitude;
                }
                RunnerBiomeHazards.Tick(entity, dt, Speed);
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

            TickItems(dt, travel);
            IntegratePlayer(dt);
            AnimationVelocityY = VelocityY;
            if (PlayerY < -180f)
            {
                if (InvincibleRemaining <= 0f)
                    TakeHit("Rơi khỏi đường chạy");
                RecoverFromFall();
            }

            if (State == RunState.Playing)
                ResolveCollisions();
            if (State == RunState.Playing)
                TickSurvival(dt);

            for (int i = entities.Count - 1; i >= 0; i--)
                if (entities[i].Removed || entities[i].X + entities[i].Width < -100f)
                    entities.RemoveAt(i);
        }

        private void IntegrateHorizontal(float dt)
        {
            if (CurrentDistrict == District.Snow)
            {
                float target = horizontalAxis * HorizontalSpeed;
                float acceleration = Math.Abs(horizontalAxis) > 0.001f
                    ? (grounded ? 660f : 990f) : (grounded ? 420f : 160f);
                float difference = target - HorizontalVelocity;
                HorizontalVelocity += Math.Max(-acceleration * dt, Math.Min(acceleration * dt, difference));
            }
            else
                HorizontalVelocity = horizontalAxis * HorizontalSpeed + WindPush;

            float nextX = PlayerX + HorizontalVelocity * dt;
            PlayerX = Math.Max(MinimumPlayerX, Math.Min(MaximumPlayerX, nextX));
            if (CurrentDistrict == District.Snow && PlayerX != nextX)
                HorizontalVelocity = 0f;
        }

        private void RefillDistrictBag()
        {
            districtBag.Clear();
            for (int i = 0; i < AllDistricts.Length; i++) districtBag.Add(AllDistricts[i]);
            for (int i = districtBag.Count - 1; i > 0; i--)
            {
                int index = random.Next(i + 1);
                District temporary = districtBag[i];
                districtBag[i] = districtBag[index];
                districtBag[index] = temporary;
            }
        }

        private District DrawDistrict(District? previous)
        {
            if (districtBag.Count == 0) RefillDistrictBag();
            int last = districtBag.Count - 1;
            if (previous.HasValue && districtBag[last] == previous.Value)
            {
                for (int i = 0; i < last; i++)
                {
                    if (districtBag[i] == previous.Value) continue;
                    District temporary = districtBag[last];
                    districtBag[last] = districtBag[i];
                    districtBag[i] = temporary;
                    break;
                }
            }
            District selected = districtBag[last];
            districtBag.RemoveAt(last);
            return selected;
        }

        private void EnterDistrict()
        {
            biomeShowcaseSpawned = false;
            Water = Warmth = 100f;
            HorizontalVelocity = 0f;
            nextSurvivalSupplyAt = elapsedClock + 0.75d;
            for (int i = 0; i < entities.Count; i++)
                if (entities[i].Kind == EntityKind.Water || entities[i].Kind == EntityKind.HotDrink)
                    entities[i].Removed = true;
            ScheduleBlackout();
        }

        private void TickSurvival(float dt)
        {
            bool cold = CurrentDistrict == District.Snow;
            bool hot = CurrentDistrict == District.Sunset;
            if (!cold && !hot) return;

            if (elapsedClock >= nextSurvivalSupplyAt)
            {
                nextSurvivalSupplyAt = elapsedClock + Range(3.7f, 4.6f);
                SpawnSurvivalSupply(cold);
            }
            if (DistrictElapsed <= 2f) return;
            if (cold) Warmth = Math.Max(0f, Warmth - 7f * dt);
            else Water = Math.Max(0f, Water - 6f * dt);
            float resource = cold ? Warmth : Water;
            if (resource > 0f) return;

            if (TryUseSurvivalReserve(cold))
            {
                if (cold) Warmth = 85f;
                else Water = 85f;
            }
            else
            {
                TakeHit(cold ? "Hạ thân nhiệt · Cần đồ uống nóng" : "Mất nước · Cần nhặt chai nước");
                // A surviving player gets recovery time instead of losing all
                // remaining hearts on consecutive simulation substeps.
                if (State == RunState.Playing)
                {
                    if (cold) Warmth = 45f;
                    else Water = 45f;
                }
            }
        }

        private void SpawnSurvivalSupply(bool cold)
        {
            float x = CanvasWidth + 50f;
            // Find a visible approach lane ahead of the player, before any
            // ground hazard's full moving envelope. Supplies are never placed
            // in a hole or inside a house, saw, spike or barrier.
            for (int pass = 0; pass < 12; pass++)
            {
                bool changed = false;
                for (int i = 0; i < entities.Count; i++)
                {
                    WorldEntity e = entities[i];
                    if (e.Removed || !BlocksGroundSupply(e)) continue;
                    float left = e.X - e.MotionAmplitudeX * 2f - 100f;
                    float right = e.X + e.Width + e.MotionAmplitudeX * 2f + 100f;
                    if (x + 32f <= left || x >= right) continue;
                    x = left - 34f;
                    changed = true;
                }
                if (!changed) break;
            }
            if (x < MaximumPlayerX + 100f)
            {
                // The approach area can be occupied by a very wide late pack.
                // Put the supply after all visible/spawning ground hazards.
                x = CanvasWidth + 50f;
                for (int i = 0; i < entities.Count; i++)
                {
                    WorldEntity e = entities[i];
                    if (e.Removed || !BlocksGroundSupply(e)) continue;
                    x = Math.Max(x, e.X + e.Width + e.MotionAmplitudeX * 2f + 120f);
                }
            }
            Add(cold ? EntityKind.HotDrink : EntityKind.Water, x, 10f, 32f, 36f);
        }

        private static bool BlocksGroundSupply(WorldEntity entity)
        {
            return entity.Kind == EntityKind.Gap || entity.Kind == EntityKind.CollapsingFloor ||
                RunnerBiomeHazards.IsBiomeHazard(entity.Kind) ||
                entity.Kind == EntityKind.House || entity.Kind == EntityKind.Hydrant ||
                entity.Kind == EntityKind.Spike || entity.Kind == EntityKind.Barrier ||
                (entity.Kind == EntityKind.Saw && entity.BaseY < 56f);
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
                if (!e.Removed && e.IsOpenGap &&
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
                if (e.Removed || e.Kind == EntityKind.Gap || e.Kind == EntityKind.CollapsingFloor)
                    continue;
                if (RunnerBiomeHazards.IsBiomeHazard(e.Kind) && !RunnerBiomeHazards.IsDamaging(e))
                    continue;
                // A rectangle includes the transparent space behind the neck
                // and misses the nose. Saws use the same animated silhouette,
                // pose and facing as the renderer instead.
                bool touches = RunnerBiomeHazards.IsBiomeHazard(e.Kind)
                    ? RunnerGeometry.BiomeTouchesPlayer(e, this)
                    : e.Kind == EntityKind.Saw
                    ? RunnerGeometry.SawTouchesPlayer(e, PlayerX, PlayerY, IsDucking, FacingLeft,
                        RunnerGeometry.GetPlayerFrame(this),
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
                else if (e.Kind == EntityKind.Water || e.Kind == EntityKind.HotDrink)
                {
                    e.Removed = true;
                    if (e.Kind == EntityKind.Water) Water = Math.Min(100f, Water + 40f);
                    else Warmth = Math.Min(100f, Warmth + 45f);
                }
                else if (HandleItemPickup(e)) { }
                else if (InvincibleRemaining <= 0f)
                {
                    e.Removed = true;
                    if (TryAbsorbHazard()) continue;
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
                if ((e.Kind == EntityKind.Gap || e.Kind == EntityKind.CollapsingFloor) && e.X < PlayerX + (Speed + HorizontalSpeed) * 2f &&
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
            HorizontalVelocity = 0f;
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
            // Show the district's signature trap on an ordinary authored pack
            // slot, never by inserting an extra surprise beneath the player.
            if (!biomeShowcaseSpawned && Elapsed >= 15f && DistrictElapsed >= 4f)
            {
                PatternType signature = CurrentDistrict == District.Snow ? PatternType.FallingIce
                    : CurrentDistrict == District.Sunset ? PatternType.HeatVent
                    : CurrentDistrict == District.Storm ? PatternType.StormDebris : PatternType.Hydrant;
                if (CurrentDistrict == District.Snow || CurrentDistrict == District.Sunset || CurrentDistrict == District.Storm)
                {
                    biomeShowcaseSpawned = true;
                    previousPattern = signature;
                    return signature;
                }
            }
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
                    patternBag.Add(PatternType.MovingPlane);
                }
                if (skyway)
                {
                    for (int i = 0; i < 9; i++) patternBag.Add(PatternType.Gap);
                    for (int i = 0; i < 6; i++) patternBag.Add(PatternType.CollapsingBridge);
                    if (tier >= 1)
                        for (int i = 0; i < 5; i++) patternBag.Add(PatternType.DoubleGap);
                }
                else if (tier >= 2 && CurrentDistrict != District.Snow)
                    patternBag.Add(PatternType.CollapsingBridge);
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
                    if (tier >= 2) patternBag.Add(PatternType.PatrolSaw);
                    break;
                case District.Snow:
                    if (tier >= 1)
                    {
                        patternBag.Add(PatternType.Barrier);
                        patternBag.Add(PatternType.FallingIce);
                        patternBag.Add(PatternType.FallingIce);
                    }
                    if (tier >= 2)
                    {
                        patternBag.Add(PatternType.Plane);
                        patternBag.Add(PatternType.Plane);
                        patternBag.Add(PatternType.MovingPlane);
                        patternBag.Add(PatternType.MovingPlane);
                    }
                    break;
                case District.Sunset:
                    if (tier >= 1)
                    {
                        patternBag.Add(PatternType.DoubleSpike);
                        patternBag.Add(PatternType.HeatVent);
                        patternBag.Add(PatternType.HeatVent);
                    }
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
                        patternBag.Add(PatternType.StormDebris);
                        patternBag.Add(PatternType.StormDebris);
                    }
                    if (tier >= 2)
                    {
                        patternBag.Add(PatternType.FloatingSaw);
                        patternBag.Add(PatternType.Plane);
                        patternBag.Add(PatternType.MovingBarrier);
                        patternBag.Add(PatternType.MovingPlane);
                    }
                    break;
            }
        }

        private bool HasBlackouts
        {
            get { return State != RunState.Menu && CurrentDistrict == District.Night; }
        }

        private void ScheduleBlackout()
        {
            if (CurrentDistrict != District.Night)
            {
                NextBlackoutAt = BlackoutDuration = 0f;
                return;
            }
            NextBlackoutAt = DistrictElapsed + Range(8f, 13f);
            BlackoutDuration = Range(2f, 3f);
        }

        private float AddPattern(PatternType kind)
        {
            activePatternId = ++nextPatternId;
            float width;
            float height;
            float starBase;
            if (kind == PatternType.FallingIce)
            {
                biomeShowcaseSpawned = true;
                width = 50f;
                WorldEntity ice = Add(EntityKind.FallingIce, SpawnX, RunnerBiomeHazards.IceHangHeight,
                    width, RunnerBiomeHazards.IceHeight);
                ice.BaseY = ice.Y;
                starBase = 110f;
            }
            else if (kind == PatternType.HeatVent)
            {
                biomeShowcaseSpawned = true;
                width = 82f;
                height = Elapsed < 60f ? 96f : 108f;
                Add(EntityKind.HeatVent, SpawnX, 0f, width, height, random.Next(4));
                starBase = height + 28f;
            }
            else if (kind == PatternType.StormDebris)
            {
                biomeShowcaseSpawned = true;
                const float objectWidth = 104f;
                WorldEntity debris = Add(EntityKind.StormDebris, SpawnX, 0f, objectWidth, 52f);
                SetMotion(debris, 36f, 0f, 1.65f);
                // Keep the panel a ground-jump obstacle throughout every gust,
                // rather than sliding into an impossible jump/duck mixture.
                debris.MotionAmplitude = Math.Min(18f, 10f * MotionRangeScale);
                debris.BaseY = debris.MotionAmplitude;
                debris.Y = debris.BaseY + debris.MotionAmplitude *
                    (float)Math.Sin(elapsedClock * debris.MotionSpeed + debris.MotionPhase);
                width = objectWidth + debris.MotionAmplitudeX * 2f;
                starBase = 112f;
            }
            else if (kind == PatternType.DoubleGap)
            {
                float firstWidth = Range(135f, 160f);
                float island = Range(115f, 145f);
                float secondWidth = Range(135f, 160f);
                width = firstWidth + island + secondWidth;
                Add(EntityKind.Gap, SpawnX, 0f, firstWidth, 160f);
                Add(EntityKind.Gap, SpawnX + firstWidth + island, 0f, secondWidth, 160f);
                starBase = 105f;
            }
            else if (kind == PatternType.CollapsingBridge)
            {
                width = Range(155f, 205f);
                Add(EntityKind.CollapsingFloor, SpawnX, 0f, width, 160f);
                starBase = 100f;
            }
            else if (kind == PatternType.Gap)
            {
                width = Range(140f, 205f);
                Add(EntityKind.Gap, SpawnX, 0f, width, 160f);
                starBase = 95f;
            }
            else if (kind == PatternType.Plane || kind == PatternType.FloatingSaw || kind == PatternType.MovingPlane)
            {
                width = kind == PatternType.FloatingSaw ? 76f : 110f;
                if (kind == PatternType.Plane)
                    Add(EntityKind.Plane, SpawnX, 57f, width, 42f, random.Next(3));
                else if (kind == PatternType.MovingPlane)
                {
                    WorldEntity plane = Add(EntityKind.Plane, SpawnX, 56f, width, 42f, random.Next(3));
                    SetMotion(plane, 0f, 10f, 1.5f);
                }
                else
                {
                    WorldEntity saw = Add(EntityKind.Saw, SpawnX, 56f, width, 76f, 1);
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
                WorldEntity barrier = Add(EntityKind.Barrier, SpawnX, 0f, objectWidth, height, 1);
                SetMotion(barrier, amplitude, 0f, 1.3f);
                width = objectWidth + barrier.MotionAmplitudeX * 2f;
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
                WorldEntity saw = Add(EntityKind.Saw, SpawnX, 0f, objectWidth, height, 2);
                SetMotion(saw, amplitude, 0f, 1.5f);
                width = objectWidth + saw.MotionAmplitudeX * 2f;
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
            entity.MotionAmplitudeX = horizontalAmplitude * MotionRangeScale;
            entity.MotionAmplitude = verticalAmplitude * MotionRangeScale;
            entity.MotionSpeed = speed * MotionFrequencyScale;
            if (entity.MotionAmplitude > 0f)
                entity.BaseY = 56f + entity.MotionAmplitude;
            entity.MotionPhase = Range(0f, (float)Math.PI * 2f);
            float motion = (float)Math.Sin(elapsedClock * entity.MotionSpeed + entity.MotionPhase);
            // All phases stay inside a known spawn envelope. AddPattern returns
            // its full swept width, so the next pack retains recovery space.
            entity.X += entity.MotionAmplitudeX * (1f + motion);
            entity.Y = entity.BaseY + entity.MotionAmplitude * motion;
        }

        private float NextPatternSpacing(float previousWidth)
        {
            // previousWidth includes horizontal moving hazards' full envelope.
            // Packs are hand-authored to share one safe jump/duck profile.
            // Separate packs retain recovery room even while moving right into
            // the scroll. The small speed look-ahead covers travel from spawn
            // to the player; short clusters still feel rhythmic rather than sparse.
            float approachSpeed = Speed + SpeedAcceleration * 4f;
            float steeringRecovery = CurrentDistrict == District.Snow ? 1.00f
                : CurrentDistrict == District.Storm ? 0.90f : 0.72f;
            float recoveryGap = Math.Max((approachSpeed + HorizontalSpeed + Math.Abs(WindPush)) * steeringRecovery,
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
                case District.Snow: return "Phố tuyết";
                case District.Sunset: return "Nắng gắt";
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
                case District.Snow: return "Đường trơn và băng rơi; nhặt đồ nóng giữ ấm.";
                case District.Sunset: return "Khe phun nóng có báo trước; nhớ nhặt nước.";
                case District.Night: return "Nhiều cưa xoay; đèn tắt ngắn có báo trước.";
                case District.Skyway: return "Gió đẩy lùi; hố đôi và cầu sập liên tiếp.";
                case District.Storm: return "Biển cuốn theo gió; mưa và sấm chớp liên tục.";
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
                int legacyLevel = Clamp(save.ExtraHeartsLevel, 0, LegacyHeartCosts.Length);
                for (int i = 0; i < legacyLevel; i++)
                    save.Wallet = Math.Min(WalletLimit, save.Wallet + LegacyHeartCosts[i]);
            }
            save.Version = 3;
            save.ExtraHeartsLevel = 0;
            save.PurchasedHearts = Clamp(save.PurchasedHearts, 0, MaximumPurchasedHearts);
            SanitizeItemSave(save);
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
