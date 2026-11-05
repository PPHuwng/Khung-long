using System;

namespace CityDino
{
    public enum BossTransitionStage { None, Approach, Collect, Takeoff, Arrive, Depart, Land, Diving, Surfacing }

    /// <summary>Practice isolation, staged arena journeys and the gunship flight device.</summary>
    public sealed partial class RunnerSession
    {
        public const float FlightMinimumY = 20f;
        public const float FlightMaximumY = 260f;
        public const float FlightGravity = 550f;
        public const float FlightThrust = 1000f;
        public const float BossExplosionDuration = 1.6f;
        public const float BossOutroDuration = 3.2f;
        // These coordinates belong to the cinematic corridor, not scored
        // running distance. The exit sits beyond the defeated boss.
        public const float BossExitGateX = 1320f;
        public const float BossExitStreetX = 1536f;
        private bool flightLiftHeld;
        private float flapCooldown;
        private float entranceClock;
        private float departureClock;
        private float departurePlayerX, departurePlayerY;
        private double practiceCombatClock;
        private int practiceBossTier = 1;

        public bool IsPractice { get; private set; }
        public bool PracticeLearning { get; private set; }
        public bool PracticeFinished { get; private set; }
        public bool PracticeWon { get; private set; }
        public int PracticeHits { get; private set; }
        public float PracticeElapsed { get { return (float)practiceCombatClock; } }
        public float BossPracticePace { get { return IsPractice && PracticeLearning ? 0.8f : 1f; } }
        public float BossPracticeWarningScale { get { return IsPractice && PracticeLearning ? 1.25f : 1f; } }
        public bool IsFlyingBoss { get { return IsBossArena && CurrentBoss != null && CurrentBoss.Kind == BossKind.Gunship; } }
        public bool FlightControlsEnabled { get { return State == RunState.Playing && IsFlyingBoss && ArenaPhase == BossPhase.Fighting; } }
        public float PreBossPlayerX { get; private set; }
        public float PreBossPlayerY { get; private set; }
        public float FlightDeviceX { get { return 470f; } }
        public float FlightDeviceY { get { return 18f; } }
        public bool FlightDeviceCollected { get; private set; }
        public float ExitTravelProgress
        {
            get { return ArenaPhase == BossPhase.Victory
                ? SmoothClamp((departureClock - BossExplosionDuration) / BossOutroDuration, true) : 0f; }
        }
        public float DepartureWorldX
        {
            get { return ArenaPhase == BossPhase.Victory
                ? Blend(departurePlayerX, BossExitStreetX + StartPlayerX, ExitTravelProgress) : PlayerX; }
        }
        public float BossExitCameraX
        {
            get { return ArenaPhase == BossPhase.Victory ? BossExitStreetX * ExitTravelProgress : 0f; }
        }
        public float ExitLandingProgress
        {
            get
            {
                // Land before reaching the exit, including a victory from
                // the far-right edge where the gate is approached sooner.
                float route = Math.Max(1f, BossExitStreetX + StartPlayerX - departurePlayerX);
                float landingEnd = Math.Max(.1f, Math.Min(.58f,
                    (BossExitGateX - 48f - departurePlayerX) / route));
                return SmoothClamp(ExitTravelProgress / landingEnd, true);
            }
        }
        public float BossTransitionProgress
        {
            get { return ArenaPhase == BossPhase.Intro ? SmoothClamp(entranceClock / BossIntroDuration, false)
                : ArenaPhase == BossPhase.Victory ? SmoothClamp((departureClock - BossExplosionDuration) / BossOutroDuration, false) : 1f; }
        }
        public float TransitionSceneBlend
        {
            get { return ArenaPhase == BossPhase.Intro ? SmoothClamp((entranceClock - 1.4f) / 2.4f, true)
                : ArenaPhase == BossPhase.Victory ? 1f - SmoothClamp((departureClock - BossExplosionDuration) / BossOutroDuration, true) : 1f; }
        }
        public float TransitionElevation
        {
            get { return IsFlyingBoss ? ArenaPhase == BossPhase.Victory
                ? 1f - ExitLandingProgress : TransitionSceneBlend : 0f; }
        }
        public BossTransitionStage TransitionStage
        {
            get
            {
                if (ArenaPhase == BossPhase.Intro)
                {
                    if (entranceClock < 1.2f) return BossTransitionStage.Approach;
                    if (entranceClock < 1.6f) return IsFreeMovementBoss ? BossTransitionStage.Collect : BossTransitionStage.Approach;
                    if (entranceClock < 3.8f) return IsUnderwaterBoss ? BossTransitionStage.Diving
                        : IsFlyingBoss ? BossTransitionStage.Takeoff : BossTransitionStage.Approach;
                    return BossTransitionStage.Arrive;
                }
                if (ArenaPhase == BossPhase.Victory && departureClock > BossExplosionDuration)
                    return IsUnderwaterBoss ? BossTransitionStage.Surfacing
                        : IsFlyingBoss ? BossTransitionStage.Land : BossTransitionStage.Depart;
                return BossTransitionStage.None;
            }
        }

        private static float SmoothClamp(float value, bool smooth)
        {
            value = Math.Max(0f, Math.Min(1f, value));
            return smooth ? value * value * (3f - 2f * value) : value;
        }
        private static float Blend(float a, float b, float progress) { return a + (b - a) * progress; }

        private void ResetPracticeRun()
        {
            IsPractice = PracticeLearning = PracticeFinished = PracticeWon = false;
            PracticeHits = 0;
            practiceCombatClock = 0d;
            flightLiftHeld = false;
            flapCooldown = entranceClock = departureClock = 0f;
            departurePlayerX = StartPlayerX;
            departurePlayerY = 0f;
            FlightDeviceCollected = false;
            practiceBossTier = 1;
            ResetUnderwaterState();
        }

        public bool StartBossPractice(BossKind kind, CharacterKind character, bool learning = true, int tier = 1)
        {
            if ((State != RunState.Menu && State != RunState.GameOver) ||
                !Enum.IsDefined(typeof(BossKind), kind) || !Enum.IsDefined(typeof(CharacterKind), character) ||
                tier < 1 || tier > 100000) return false;
            // Do not call StartRun/BeginRunItems: training must not spend stock,
            // grant stars or change the character selected for normal runs.
            ResetBossRun();
            IsPractice = true;
            PracticeLearning = learning;
            practiceBossTier = tier;
            entities.Clear(); patternBag.Clear(); districtBag.Clear();
            nextEntityId = nextPatternId = activePatternId = 0;
            recorded = false;
            State = RunState.Playing;
            CurrentDistrict = StartingDistrict ?? District.Day;
            DistrictElapsed = Elapsed = Distance = 0f;
            elapsedClock = distanceClock = districtClock = 0d;
            StageNumber = 1;
            Speed = InitialSpeed;
            Score = RunStars = 0;
            Lives = learning ? MaximumHearts : 3;
            PlayerX = StartPlayerX; PlayerY = VelocityY = AnimationVelocityY = 0f;
            HorizontalVelocity = horizontalAxis = 0f;
            grounded = true; jumpsUsed = 0; coyoteRemaining = 0.1f;
            InvincibleRemaining = 0f;
            IsDucking = duckRequested = FacingLeft = false;
            EndReason = "";
            runCharacter = character;
            skillCooldownRemaining = guardRemaining = dashRemaining = 0f;
            dashDirection = 1f;
            ShieldReady = WaterReserveReady = WarmReserveReady = false;
            scoreBoostRemainingClock = magnetRemainingClock = 0d;
            bonusScoreDistance = 0d;
            Water = Warmth = 100f;
            ResetSurvivalForDistrict();
            BossNumber = kind == BossKind.Tank ? 1 : kind == BossKind.Gunship ? 2 : kind == BossKind.Submarine ? 3 : 4;
            CreateBossArena(kind);
            return true;
        }

        public bool SkipPracticeIntro()
        {
            if (!IsPractice || State != RunState.Playing || ArenaPhase != BossPhase.Intro) return false;
            entranceClock = BossIntroDuration;
            ApplyBossEntrance(0f);
            FinishBossEntrance();
            return true;
        }

        public void SetFlightLift(bool held)
        {
            flightLiftHeld = held && FlightControlsEnabled;
        }

        public bool Flap()
        {
            if (!FlightControlsEnabled || flapCooldown > 0f) return false;
            VelocityY = Math.Max(VelocityY, 150f);
            AnimationVelocityY = VelocityY;
            flapCooldown = 0.12f;
            return true;
        }

        private void IntegrateBossFlight(float dt)
        {
            flapCooldown = Math.Max(0f, flapCooldown - dt);
            VelocityY += (-FlightGravity + (flightLiftHeld ? FlightThrust : 0f)) * dt;
            VelocityY = Math.Max(-260f, Math.Min(230f, VelocityY));
            PlayerY += VelocityY * dt;
            if (PlayerY < FlightMinimumY) { PlayerY = FlightMinimumY; VelocityY = Math.Max(0f, VelocityY); }
            if (PlayerY > FlightMaximumY) { PlayerY = FlightMaximumY; VelocityY = Math.Min(0f, VelocityY); }
            AnimationVelocityY = VelocityY;
            grounded = false;
            IsDucking = duckRequested = false;
        }

        private void BeginBossEntrance()
        {
            PreBossPlayerX = PlayerX;
            PreBossPlayerY = PlayerY;
            entranceClock = departureClock = 0f;
            FlightDeviceCollected = false;
            ResetUnderwaterState();
            flightLiftHeld = false;
            flapCooldown = 0f;
            HorizontalVelocity = horizontalAxis = 0f;
            IsDucking = duckRequested = false;
        }

        private void ApplyBossEntrance(float dt)
        {
            float beforeY = PlayerY;
            float approach = SmoothClamp(entranceClock / 1.4f, true);
            if (entranceClock <= 1.4f)
            {
                PlayerX = Blend(PreBossPlayerX, 440f, approach);
                PlayerY = Blend(PreBossPlayerY, 0f, approach);
            }
            else if (IsUnderwaterBoss)
            {
                if (entranceClock <= 1.9f)
                {
                    float jump = SmoothClamp((entranceClock - 1.4f) / .5f, false);
                    PlayerX = Blend(440f, 520f, BossMotionEase(jump));
                    PlayerY = (float)Math.Sin(jump * Math.PI) * 88f;
                }
                else
                {
                    float descent = SmoothClamp((entranceClock - 1.9f) / 1.9f, true);
                    PlayerX = Blend(520f, 260f, descent);
                    PlayerY = Blend(0f, 145f, descent);
                }
            }
            else
            {
                float arrival = SmoothClamp((entranceClock - 1.4f) / 2.4f, true);
                PlayerX = Blend(440f, 260f, arrival);
                PlayerY = IsFlyingBoss ? Blend(0f, 145f, arrival) : 0f;
            }
            FlightDeviceCollected = IsFlyingBoss && entranceClock >= 1.4f;
            DivingHelmetEquipped = IsUnderwaterBoss && entranceClock >= 1.4f;
            VelocityY = dt > 0f ? (PlayerY - beforeY) / dt : 0f;
            AnimationVelocityY = VelocityY;
            grounded = !IsFreeMovementBoss && PlayerY <= .001f;
            IsDucking = duckRequested = false;
        }

        private void FinishBossEntrance()
        {
            ArenaPhase = BossPhase.Fighting;
            BossPhaseRemaining = 0f;
            attackClock = 0d;
            horizontalAxis = HorizontalVelocity = 0f;
            VelocityY = AnimationVelocityY = 0f;
            grounded = !IsFreeMovementBoss;
            ClearSwimIntent();
            FacingLeft = false;
            jumpsUsed = 0; coyoteRemaining = IsFreeMovementBoss ? 0f : .1f;
            BeginBossAttack();
        }

        private void BeginBossDeparture()
        {
            departureClock = 0f;
            departurePlayerX = PlayerX;
            departurePlayerY = PlayerY;
            flightLiftHeld = false;
            ClearSwimIntent();
            horizontalAxis = HorizontalVelocity = 0f;
            FacingLeft = false;
            IsDucking = duckRequested = false;
        }

        private void ApplyBossDeparture(float dt)
        {
            if (departureClock <= BossExplosionDuration) return;
            float progress = ExitTravelProgress;
            float beforeY = PlayerY;
            // Move forward through the far exit while the camera follows. The
            // screen coordinate settles at the normal running lane only
            // because scenery advances to the left; world travel never reverses.
            PlayerX = DepartureWorldX - BossExitCameraX;
            PlayerY = IsFreeMovementBoss ? Blend(departurePlayerY, 0f, ExitLandingProgress)
                : Blend(departurePlayerY, 0f, SmoothClamp((departureClock - BossExplosionDuration) / .9f, true));
            VelocityY = dt > 0f ? (PlayerY - beforeY) / dt : 0f;
            AnimationVelocityY = VelocityY;
            FacingLeft = false;
            IsDucking = duckRequested = false;
            if (IsFlyingBoss && ExitLandingProgress >= 1f) FlightDeviceCollected = false;
            if (IsUnderwaterBoss && ExitLandingProgress >= 1f) DivingHelmetEquipped = false;
            grounded = PlayerY <= .001f;
        }

        private void FinishPractice(bool won, string reason)
        {
            PracticeFinished = true;
            PracticeWon = won;
            State = RunState.GameOver;
            EndReason = reason;
            VelocityY = HorizontalVelocity = horizontalAxis = 0f;
            shootingHeld = flightLiftHeld = false;
            ClearSwimIntent();
        }
    }
}
