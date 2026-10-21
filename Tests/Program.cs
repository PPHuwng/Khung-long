using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityDino;

internal static class Program
{
    private static int passed;
    private static int notSelected;
    private static string testFilter;
    private static readonly List<string> failures = new List<string>();

    private static int Main(string[] args)
    {
        if (args.Length == 2 && args[0] == "--only") testFilter = args[1];
        Test("menu does not simulate and starting resets a run", MenuAndStart);
        Test("double jump rejects a third jump and resets on landing", DoubleJump);
        Test("pause freezes world, score, speed, and player", Pause);
        Test("coyote jumping and walking off ledges preserve jump limits", CoyoteJump);
        Test("opening hydrant can be cleared by one normal jump", OpeningJump);
        Test("ducking avoids planes while standing collides", DuckPlane);
        Test("speed rises smoothly without a ceiling and score follows distance", Progression);
        Test("high speed steps still hit thin hazards and collect small stars", HighSpeedContacts);
        Test("hour-long and ten-hour clocks keep advancing without float drift", LongRunClocks);
        Test("same seed produces the same endless world", Determinism);
        Test("different seeds vary the world", SeedVariation);
        Test("stars credit the wallet once and notify persistence", StarCollection);
        Test("hearts restore one life without exceeding the cap", HeartCollection);
        Test("heart purchases require funds and apply to the next run", Upgrade);
        Test("purchased hearts are capped and consumed for one run only", UpgradeCap);
        Test("legacy permanent hearts refund once while preserving progress", LegacyHeartRefund);
        Test("collisions consume one life with invulnerability", Damage);
        Test("fatal collision records once and freezes gameplay", FatalCollision);
        Test("restart retains money and high score but resets the run", Restart);
        Test("fall recovery removes the gap and avoids repeated falling", FallRecovery);
        Test("fatal falling ends the run instead of falling forever", FatalFall);
        Test("hearts have a rare randomized spawn cadence", HeartCadence);
        Test("starting map selection supports every district and validates changes", StartingMapSelection);
        Test("random starts cover all maps and selected starts remain repeatable", RandomStartingMaps);
        Test("district transitions randomize without adjacent repeats", Districts);
        Test("night blackouts are brief, warned, and limited to night", BlackoutTimeline);
        Test("blackouts freeze on pause and reset between maps and runs", BlackoutStateTransitions);
        Test("storm flashes repeat with matching thunder identities and variable darkness", LightningCadence);
        Test("skyway starts give a safe opening before real gaps", SkywayOpening);
        Test("obstacles and gaps do not form impossible overlapping hazards", SpawnSafety);
        Test("horizontal control moves left/right and respects screen bounds", HorizontalMovement);
        Test("airborne steering works in both directions", AirSteering);
        Test("invalid horizontal inputs are safe and finite inputs clamp", HorizontalInputValidation);
        Test("pause, menu, death and restart stop horizontal intent", HorizontalStateTransitions);
        Test("hazard collisions follow the player after moving", MovingCollision);
        Test("pickups follow the player after moving", MovingPickups);
        Test("floor and fall recovery follow the player after moving", MovingFloor);
        Test("moving into a gap removes floor support", WalkIntoGap);
        Test("spike collisions use the triangle instead of empty corners", SpikeShape);
        Test("saw collisions use a circle instead of empty corners", SawShape);
        Test("saw contact follows visible head and transparent neck in both directions", SawVisibleBody);
        Test("saw teeth contact uses the visible circle and ignores diagonal empty space", SawToothEdges);
        Test("saw contact follows jump frame, crouch and running bob", SawAnimatedPose);
        Test("six action silhouettes follow opaque head and body while excluding clear space", ActionSilhouettes);
        Test("jump and crouch animation selects the correct phase and pauses consistently", ActionPoseTransitions);
        Test("saw hits consume one life and respect temporary invulnerability", SawDamage);
        Test("fatal saw contact freezes the exact visible animation and crouch pose", SawFatalPose);
        Test("saws rotate and floating blades leave a safe duck lane", SawMotion);
        Test("moving hazards oscillate without drift and pause or restart cleanly", HazardMotion);
        Test("moving hazards use their live position for contact without spending stars", HazardMotionCollision);
        Test("vertical hazard motion preserves a safe duck lane through every phase", MovingDuckLane);
        Test("moving hazard integration is independent of frame partition", MovingFrameStep);
        Test("random packs cover all hazard types and composite rhythms", PatternVariety);
        Test("sampled generated packs have a real jump or duck solution", PatternReplay);
        Test("late motion grows in range and frequency without blocking duck lanes", LateMotionGrowth);
        Test("late generated motion packs retain a real jump or duck solution", LatePatternReplay);
        Test("faster pace keeps increasing beyond the old ceiling", FasterPacing);
        Test("seeded full runs can traverse successive packs without taking damage", SequentialRunner);
        Test("six-map shuffle bags visit every map before repeating", MapShuffleBag);
        Test("snow slides on release and brakes gradually when reversing", SnowInertia);
        Test("skyway wind pushes airborne players and allows counter steering", SkyWind);
        Test("warmth and water drain after grace and refill from matching supplies", SurvivalResources);
        Test("empty resources cost one life with recovery and freeze after fatal damage", SurvivalDepletion);
        Test("production supplies sustain snow and heat without resource refills", ProductionSupplyRuns);
        Test("late biome supplies remain clear and reachable after fallback placement", LateSupplyApproach);
        Test("skyway packs favor holes and include double gaps and collapsing floors", SkyHoleDensity);
        Test("collapsing floors crack monotonically then become recoverable real holes", CollapsingFloors);
        Test("item upgrades follow prices and caps while next-run stocks consume once", ItemShop);
        Test("water and warmth reserves rescue one depletion and do not become permanent", SurvivalReserves);
        Test("double score applies only to active travel and leaves wallet unchanged", DoubleScoreAccounting);
        Test("double score and magnet stack duration within their cap and pause", TimedItemLifecycle);
        Test("magnet attracts nearby stars only and respects its expiry", MagnetAttraction);
        Test("shield absorbs one physical hit without hiding later damage", ShieldDamage);
        Test("random powerups use clear lanes and cover all three pickup kinds", PowerupGeneration);
        Test("save input is sanitized without discarding valid progress", SaveValidation);
        Test("long frame updates behave like fixed small steps", FrameStep);
        Console.WriteLine();
        Console.WriteLine("Gameplay core: " + passed + " passed, " + failures.Count + " failed.");
        foreach (string failure in failures) Console.Error.WriteLine(failure);
        if (notSelected>0) Console.WriteLine(notSelected+" checks were not selected and did not run.");
        Console.WriteLine("These checks execute production C# gameplay. Unity rendering/import was not executed.");
        return failures.Count == 0 ? 0 : 1;
    }

    private static void Test(string name, Action body)
    {
        if (!string.IsNullOrEmpty(testFilter) && name.IndexOf(testFilter,StringComparison.OrdinalIgnoreCase)<0) { notSelected++; return; }
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception e) { failures.Add("FAIL " + name + ": " + e.Message); Console.WriteLine(failures[failures.Count - 1]); }
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Near(float actual, float expected, float tolerance, string message)
    {
        Check(Math.Abs(actual - expected) <= tolerance, message + " (actual " + actual + ", expected " + expected + ")");
    }

    private static RunnerSession New(int seed = 123, int upgrade = 0, bool randomStart = false)
    {
        var run = new RunnerSession(new RunnerSave { PurchasedHearts = upgrade }, seed);
        // Generic physics fixtures start on a neutral road. Random-start and
        // biome tests explicitly opt into the production map-selection flow.
        if (!randomStart) run.SetStartingDistrict(District.Day);
        return run;
    }

    private static IList<WorldEntity> World(RunnerSession run)
    {
        foreach (FieldInfo f in typeof(RunnerSession).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            if (typeof(IList<WorldEntity>).IsAssignableFrom(f.FieldType))
                return (IList<WorldEntity>)f.GetValue(run);
        throw new InvalidOperationException("Cannot find entity list for deterministic collision fixture");
    }

    private static bool Hazard(WorldEntity entity)
    {
        return entity.Kind == EntityKind.House || entity.Kind == EntityKind.Hydrant ||
            entity.Kind == EntityKind.Plane || entity.Kind == EntityKind.Gap ||
            entity.Kind == EntityKind.Spike || entity.Kind == EntityKind.Saw ||
            entity.Kind == EntityKind.Barrier || entity.Kind == EntityKind.CollapsingFloor;
    }

    private static void ClearHazards(RunnerSession run)
    {
        foreach (WorldEntity e in run.Entities) if (Hazard(e)) e.Removed = true;
    }

    private static void IsolateWorld(RunnerSession run)
    {
        // This helper is used only by isolated mechanics and generation audits.
        // Real survival/supply traversal checks below do not call it.
        ClearHazards(run);
        foreach (WorldEntity e in run.Entities)
            if (e.Kind == EntityKind.DoubleScore || e.Kind == EntityKind.Magnet || e.Kind == EntityKind.Shield) e.Removed = true;
        SetProperty(run, "Water", 100f); SetProperty(run, "Warmth", 100f);
    }

    private static void Advance(RunnerSession run, float seconds, bool clearHazards = true)
    {
        int steps = (int)Math.Ceiling(seconds / (1f / 120f));
        float dt = seconds / steps;
        for (int n = 0; n < steps; n++)
        {
            if (clearHazards) IsolateWorld(run);
            run.Update(dt);
        }
    }

    private static WorldEntity Inject(RunnerSession run, EntityKind kind, float y = 0, float width = 60, float height = 64)
    {
        var entity = new WorldEntity { Id = 1000000 + World(run).Count, Kind = kind, X = run.PlayerX - width / 2, Y = y, BaseY = y, Width = width, Height = height };
        World(run).Add(entity);
        return entity;
    }

    private static string Snapshot(RunnerSession run)
    {
        return string.Join("|", run.State, run.Distance, run.Elapsed, run.Score, run.Speed, run.PlayerX, run.PlayerY, run.VelocityY,
            run.Lives, run.IsDucking, run.CurrentDistrict, run.StageNumber,
            string.Join(";", run.Entities.Where(e => !e.Removed).Select(e => e.Kind + ":" + e.Variant + ":" + e.PatternId + ":" + e.X + ":" + e.Y + ":" + e.Width + ":" + e.Rotation)));
    }

    private static void MenuAndStart()
    {
        RunnerSession run = New(upgrade: 2);
        Check(run.State == RunState.Menu, "Initial state must be Menu");
        run.Update(5);
        Near(run.Distance, 0, 0, "Menu changed distance");
        Check(!run.Jump(), "Menu accepted jump");
        run.StartRun();
        Check(run.State == RunState.Playing && run.Lives == 3, "Start did not consume purchased extra lives");
        Check(run.Score == 0 && run.RunStars == 0 && run.PlayerY == 0, "New run was not clean");
        Near(run.Speed, RunnerSession.InitialSpeed, 0.01f, "Unexpected initial speed");
    }

    private static void DoubleJump()
    {
        RunnerSession run = New(); run.StartRun();
        Check(run.Jump(), "First jump failed");
        Advance(run, 0.12f);
        Check(run.PlayerY > 0, "Jump never left ground");
        Check(run.Jump(), "Second jump failed");
        Check(!run.Jump(), "Third jump was allowed");
        Advance(run, 2);
        Near(run.PlayerY, 0, 0.01f, "Player did not land");
        Check(run.Jump(), "Landing did not restore jumping");
    }

    private static void Pause()
    {
        RunnerSession run = New(); run.StartRun(); run.Jump(); Advance(run, 0.2f);
        run.TogglePause();
        Check(run.State == RunState.Paused, "Pause state missing");
        string before = Snapshot(run);
        run.Update(30);
        Check(!run.Jump(), "Pause accepted jump");
        run.SetDucking(true);
        Check(Snapshot(run) == before, "Pause allowed simulation/input to change state");
        run.TogglePause();
        Advance(run, 0.1f);
        Check(run.State == RunState.Playing && run.Elapsed > 0.2f, "Resume failed");
    }

    private static void CoyoteJump()
    {
        RunnerSession coyote = New(); coyote.StartRun();
        World(coyote).Clear();
        World(coyote).Add(new WorldEntity { Id=9000, Kind=EntityKind.Gap, X=210, Y=0, Width=500, Height=160 });
        Advance(coyote, 0.03f, false);
        Check(coyote.Jump(), "Coyote window did not allow the first jump");
        Check(coyote.Jump() && !coyote.Jump(), "Coyote jump granted the wrong number of air jumps");
        RunnerSession late = New(); late.StartRun(); World(late).Clear();
        World(late).Add(new WorldEntity { Id=9000, Kind=EntityKind.Gap, X=210, Y=0, Width=500, Height=160 });
        Advance(late, 0.15f, false);
        Check(late.Jump() && !late.Jump(), "Walking off a ledge should leave one recovery air jump");
    }

    private static void OpeningJump()
    {
        RunnerSession run = New(23); run.StartRun();
        WorldEntity obstacle = run.Entities.First(e => e.Kind==EntityKind.Hydrant);
        while (obstacle.X - run.PlayerX > 100 && run.State==RunState.Playing)
            run.Update(1f/120);
        Check(run.Jump(), "Could not jump for opening obstacle");
        for (int i=0; i<120 && obstacle.X+obstacle.Width>run.PlayerX-50; i++)
            run.Update(1f/120);
        Check(run.State==RunState.Playing && run.Lives==1, "Opening obstacle cannot be cleared by a normal jump");
        Check(run.RunStars>0, "Opening did not teach collecting stars");
    }

    private static void DuckPlane()
    {
        RunnerSession ducked = New(); ducked.StartRun(); World(ducked).Clear();
        ducked.SetDucking(true); Inject(ducked, EntityKind.Plane, 57, 110, 42);
        ducked.Update(1f/120);
        Check(ducked.Lives==1 && ducked.State==RunState.Playing, "Ducking still collided with plane");
        RunnerSession standing = New(); standing.StartRun(); World(standing).Clear();
        Inject(standing, EntityKind.Plane, 57, 110, 42); standing.Update(1f/120);
        Check(standing.State==RunState.GameOver, "Standing did not collide with plane");
    }

    private static void Progression()
    {
        RunnerSession run=New(); run.StartRun(); Advance(run,10);
        Near(run.Speed,RunnerSession.InitialSpeed+run.Elapsed*RunnerSession.SpeedAcceleration,0.02f,"Acceleration must be smooth and time based");
        Check(run.Score==(int)(run.Distance/20),"Score does not follow distance");
        Advance(run,590);
        Check(run.State==RunState.Playing&&run.Score>20000&&run.Speed>1500,"Long run retained the old speed ceiling");
        float before=run.Speed; run.Update(0.1f);
        Near(run.Speed-before,RunnerSession.SpeedAcceleration*0.1f,0.01f,"Speed jumped suddenly after a long run");
        foreach(float elapsed in new[]{0f,60f,300f,1000f,3600f})
        {
            RunnerSession sample=New(); sample.StartRun(); World(sample).Clear(); StopSpawns(sample);
            SetProperty(sample,"Elapsed",elapsed); SetProperty(sample,"Speed",RunnerSession.InitialSpeed+elapsed*RunnerSession.SpeedAcceleration);
            float old=sample.Speed; sample.Update(0.1f);
            Near(sample.Speed-old,0.1f*RunnerSession.SpeedAcceleration,0.01f,"Late acceleration changed or stopped at "+elapsed+"s");
            Check(sample.Speed>old,"Speed capped at "+elapsed+"s");
        }
    }

    private static void HighSpeedContacts()
    {
        foreach(float speed in new[]{8280f,79560f})
        {
            RunnerSession hit=New(); hit.StartRun(); World(hit).Clear(); StopSpawns(hit);
            SetProperty(hit,"Elapsed",(speed-RunnerSession.InitialSpeed)/RunnerSession.SpeedAcceleration);
            SetProperty(hit,"Speed",speed);
            World(hit).Add(new WorldEntity{Id=9970,Kind=EntityKind.Barrier,X=hit.PlayerX+35,Y=0,Width=8,Height=64});
            hit.Update(1f/120);
            Check(hit.State==RunState.GameOver,"High-speed update tunnelled through an 8px hazard at "+speed);
            RunnerSession pickup=New(); pickup.StartRun(); World(pickup).Clear(); StopSpawns(pickup);
            SetProperty(pickup,"Elapsed",(speed-RunnerSession.InitialSpeed)/RunnerSession.SpeedAcceleration);
            SetProperty(pickup,"Speed",speed);
            World(pickup).Add(new WorldEntity{Id=9971,Kind=EntityKind.Star,X=pickup.PlayerX+35,Y=22,Width=8,Height=8});
            pickup.Update(1f/120);
            Check(pickup.RunStars==1&&pickup.Save.Wallet==1,"High-speed update skipped a small star at "+speed);
        }
    }

    private static void LongRunClocks()
    {
        foreach(float elapsed in new[]{3600f,36000f})
        {
            RunnerSession run=New(); run.StartRun(); World(run).Clear(); StopSpawns(run);
            float speed=RunnerSession.InitialSpeed+elapsed*RunnerSession.SpeedAcceleration;
            SetProperty(run,"Elapsed",elapsed); SetProperty(run,"Distance",0f); SetProperty(run,"Speed",speed);
            run.Update(0.1f);
            Near(run.Elapsed-elapsed,0.1f,elapsed<10000?0.001f:0.005f,"Long-run elapsed clock drifted or froze");
            Near(run.Speed-speed,RunnerSession.SpeedAcceleration*0.1f,0.02f,"Long-run speed stopped increasing");
            Near(run.Distance,speed*0.1f+0.5f*RunnerSession.SpeedAcceleration*0.01f,0.05f,"Long-run distance lost adaptive substeps");
            Check(run.Score==(int)(run.Distance/20),"Long-run score lost distance progress");
        }
    }

    private static void Determinism()
    {
        RunnerSession a = New(91), b = New(91); a.StartRun(); b.StartRun();
        for (int i = 0; i < 100; i++) { Advance(a, 1); Advance(b, 1); Check(Snapshot(a) == Snapshot(b), "Same seed diverged at " + i + "s"); }
    }

    private static void SeedVariation()
    {
        RunnerSession a = New(1), b = New(2); a.StartRun(); b.StartRun();
        Advance(a, 5); Advance(b, 5);
        Check(Snapshot(a) != Snapshot(b), "Two seeds generated identical worlds");
    }

    private static void StarCollection()
    {
        RunnerSession run = New(); int saves = 0; run.SaveChanged += () => saves++; run.StartRun();
        WorldEntity star = Inject(run, EntityKind.Star, 22, 28, 28);
        run.Update(1f / 120);
        Check(star.Removed, "Collected star still exists");
        Check(run.RunStars == 1 && run.Save.Wallet == 1, "Star did not credit run and wallet");
        Check(saves > 0, "Star did not request persistence");
        run.Update(1f / 120);
        Check(run.RunStars == 1 && run.Save.Wallet == 1, "Star was collected more than once");
    }

    private static void HeartCollection()
    {
        RunnerSession run = New(); run.StartRun(); int before = run.Lives;
        Inject(run, EntityKind.Heart, 22, 28, 28); run.Update(1f / 120);
        Check(run.Lives == before + 1, "Heart did not restore one life");
        for (int i = 0; i < 10; i++) { Inject(run, EntityKind.Heart, 22, 28, 28); run.Update(1f / 120); }
        Check(run.Lives == run.MaxLives && run.MaxLives == 5, "Heart cap not enforced");
    }

    private static void Upgrade()
    {
        RunnerSession run=New();
        Check(!run.BuyExtraHeart() && run.Save.Wallet==0 && run.Save.PurchasedHearts==0,"Unfunded purchase changed progress");
        run.Save.Wallet=100; int saved=0; run.SaveChanged+=()=>saved++;
        Check(run.NextHeartCost==100 && run.BuyExtraHeart(),"Funded heart purchase rejected");
        Check(run.Save.Wallet==0 && run.Save.PurchasedHearts==1 && saved==1,"Heart purchase not persisted at exact price");
        run.StartRun();
        Check(run.Lives==2 && run.Save.PurchasedHearts==0 && saved==2,"Starting did not consume and persist the reserved heart");
        run.Save.Wallet=100;
        Check(!run.BuyExtraHeart(),"Shop allowed purchase while playing");
        run.TogglePause(); Check(!run.BuyExtraHeart(),"Shop allowed purchase while paused");
    }

    private static void UpgradeCap()
    {
        RunnerSession run=New(); run.Save.Wallet=2000;
        foreach(int price in new[]{100,300,600}) Check(run.NextHeartCost==price && run.BuyExtraHeart(),"Escalating heart purchase failed");
        Check(run.Save.Wallet==1000 && run.Save.PurchasedHearts==3,"Heart total/price incorrect");
        Check(!run.BuyExtraHeart() && run.NextHeartCost==0 && run.Save.Wallet==1000,"Reserved-heart cap was not enforced");
        RunnerSession reloaded=new RunnerSession(run.Save,9);
        Check(reloaded.Save.PurchasedHearts==3,"Reload lost unspent reserved hearts");
        reloaded.StartRun(); Check(reloaded.Lives==4 && reloaded.Save.PurchasedHearts==0,"Run did not use all reserved hearts once");
        reloaded.ReturnToMenu(); reloaded.StartRun();
        Check(reloaded.Lives==1,"Consumed hearts became permanent");
        reloaded.ReturnToMenu(); Check(reloaded.BuyExtraHeart(),"Heart could not be bought again after use");
        reloaded.StartRun(); Check(reloaded.Lives==2,"Repeated purchase did not apply to the following run");
    }

    private static void LegacyHeartRefund()
    {
        foreach(int level in new[]{0,1,2,3})
        {
            int[] refunds={0,40,140,320};
            var old=new RunnerSave { Version=1,Wallet=57,ExtraHeartsLevel=level,BestScore=900,LastScore=700,
                History=new List<RunRecord>{new RunRecord{Score=700,Stars=5,EndReason="test"}} };
            RunnerSession migrated=new RunnerSession(old,7);
            Check(migrated.Save.Version==3 && migrated.Save.ExtraHeartsLevel==0,"Legacy upgrade not migrated");
            Check(migrated.Save.Wallet==57+refunds[level] && migrated.Save.PurchasedHearts==0,"Legacy spending not refunded correctly");
            Check(migrated.Save.BestScore==900 && migrated.Save.LastScore==700 && migrated.Save.History.Count==1,"Migration lost achievements");
            RunnerSession twice=new RunnerSession(migrated.Save,8);
            Check(twice.Save.Wallet==57+refunds[level],"Migration refunded twice");
            twice.StartRun(); Check(twice.Lives==1,"Legacy permanent hearts still apply");
        }
    }

    private static void Damage()
    {
        RunnerSession run = New(upgrade: 2); run.StartRun();
        Inject(run, EntityKind.House); Inject(run, EntityKind.Hydrant);
        run.Update(1f / 120);
        Check(run.Lives == 2 && run.State == RunState.Playing, "Overlapping hazards caused repeated damage");
        Check(run.InvincibleRemaining > 0, "Damage has no grace period");
        Inject(run, EntityKind.House); Advance(run, 0.5f, false);
        Check(run.Lives == 2, "Invulnerability failed");
    }

    private static void FatalCollision()
    {
        RunnerSession run = New(); run.StartRun(); Advance(run, 2);
        Inject(run, EntityKind.House); run.Update(1f / 120);
        Check(run.State == RunState.GameOver && run.Lives == 0, "Fatal hazard did not end run");
        Check(run.Save.History != null && run.Save.History.Count == 1, "Fatal run was not recorded");
        Check(run.Save.LastScore == run.Score && run.Save.BestScore == run.Score, "Scores not persisted");
        string before = Snapshot(run); run.Update(20); run.Jump(); run.SetDucking(true);
        Check(Snapshot(run) == before, "Game over continued simulating");
        run.ReturnToMenu(); run.ReturnToMenu();
        Check(run.Save.History.Count == 1, "Run was recorded more than once");
    }

    private static void Restart()
    {
        RunnerSession run = New(); run.StartRun(); Advance(run, 3);
        Inject(run, EntityKind.Star, 22, 28, 28); run.Update(1f / 120);
        int wallet = run.Save.Wallet;
        run.ReturnToMenu();
        int best = run.Save.BestScore;
        run.StartRun();
        Check(run.Score == 0 && run.RunStars == 0 && run.Distance == 0 && run.Lives == 1, "Restart retained transient state");
        Check(run.Save.Wallet == wallet && wallet >= 1 && run.Save.BestScore == best && best > 0, "Restart lost persistent progress");
        Check(run.State == RunState.Playing, "Restart did not play");
    }

    private static void FallRecovery()
    {
        RunnerSession run = New(upgrade: 1); run.StartRun();
        Inject(run, EntityKind.Gap, 0, 500, 0);
        for (int i = 0; i < 180 && run.Lives == 2; i++) run.Update(1f / 120);
        Check(run.State == RunState.Playing && run.Lives == 1, "Fall did not consume exactly one spare life");
        Near(run.PlayerY, 0, 0.01f, "Fall recovery did not return to safety");
        Advance(run, 2);
        Check(run.State == RunState.Playing && run.Lives == 1 && run.PlayerY == 0, "Recovered player fell repeatedly");
    }

    private static void FatalFall()
    {
        RunnerSession run = New(); run.StartRun(); Inject(run, EntityKind.Gap, 0, 500, 0);
        for (int i = 0; i < 180 && run.State == RunState.Playing; i++) run.Update(1f / 120);
        Check(run.State == RunState.GameOver && run.Save.History.Count == 1, "Fatal fall did not show game over");
        float y = run.PlayerY; run.Update(20);
        Near(run.PlayerY, y, 0, "Fatal player kept falling");
    }

    private static void HeartCadence()
    {
        RunnerSession run = New(449); run.StartRun();
        var seen = new HashSet<int>(); var times = new List<float>();
        for (int i = 0; i < 120 * 180; i++)
        {
            IsolateWorld(run); run.Update(1f / 120);
            foreach (WorldEntity e in run.Entities)
                if (e.Kind == EntityKind.Heart && !e.Removed && seen.Add(e.Id)) times.Add(run.Elapsed);
        }
        Check(times.Count >= 3 && times.Count <= 9, "Heart cadence was absent or too frequent: " + times.Count);
        Check(times[0] >= 20, "A heart spawned in the opening 20 seconds");
        for (int i = 1; i < times.Count; i++) Check(times[i] - times[i-1] >= 17.9f, "Hearts spawned too close together");
        Check(times.Skip(1).Select((t,i) => Math.Round(t-times[i])).Distinct().Count() > 1, "Heart intervals are not randomized");
    }

    private static void StartingMapSelection()
    {
        RunnerSession run = New(99, randomStart: true);
        Check(!run.StartingDistrict.HasValue, "New profile should default to random starting map");
        foreach (District district in Enum.GetValues(typeof(District)))
        {
            Check(run.SetStartingDistrict(district), "Menu rejected " + district);
            Check(run.StartingDistrict == district && run.CurrentDistrict == district,
                "Selected map preview differs from requested map");
            run.StartRun();
            Check(run.CurrentDistrict == district && run.StageNumber == 1 && run.DistrictElapsed == 0,
                "Run did not start cleanly on selected map");
            Check(!run.SetStartingDistrict(null), "Active run allowed map selection changes");
            run.TogglePause();
            Check(!run.SetStartingDistrict(District.Day), "Paused run allowed map selection changes");
            run.TogglePause(); run.ReturnToMenu();
        }
        District? before = run.StartingDistrict;
        Check(!run.SetStartingDistrict((District)999) && run.StartingDistrict == before,
            "Invalid map enum was accepted or altered selection");
        Check(run.SetStartingDistrict(null) && !run.StartingDistrict.HasValue,
            "Could not return to random starting maps");
        run.StartRun(); World(run).Clear(); Inject(run, EntityKind.House); run.Update(1f/120);
        Check(run.State == RunState.GameOver && run.SetStartingDistrict(District.Storm),
            "Results screen rejected a valid next-run selection");
        run.StartRun();
        Check(run.CurrentDistrict == District.Storm, "Results selection did not apply to retry");
    }

    private static void RandomStartingMaps()
    {
        var seeds = new HashSet<District>();
        for (int seed=0; seed<72; seed++)
        {
            RunnerSession run=New(seed, randomStart:true); run.StartRun(); seeds.Add(run.CurrentDistrict);
        }
        Check(seeds.Count == 6, "Random starting map omitted one or more districts across seeds");
        RunnerSession repeat=New(227, randomStart:true);
        var retries=new HashSet<District>();
        for (int i=0; i<72; i++)
        {
            repeat.StartRun(); retries.Add(repeat.CurrentDistrict); repeat.ReturnToMenu();
        }
        Check(retries.Count == 6, "Random retries reused a fixed starting district");
        foreach (District district in Enum.GetValues(typeof(District)))
        {
            repeat.SetStartingDistrict(district);
            for (int i=0; i<3; i++)
            {
                repeat.StartRun(); Check(repeat.CurrentDistrict == district, "Retry lost explicit map selection");
                repeat.ReturnToMenu();
            }
        }
    }

    private static void Districts()
    {
        var visited = new HashSet<District>();
        var firstTransitions = new HashSet<District>();
        for (int seed=0; seed<12; seed++)
        {
            RunnerSession run = New(seed); run.SetStartingDistrict(District.Day); run.StartRun();
            District previous=run.CurrentDistrict; int stage=run.StageNumber;
            for (int i=0; i<120*245; i++)
            {
                IsolateWorld(run); run.Update(1f/120); visited.Add(run.CurrentDistrict);
                if (run.StageNumber != stage)
                {
                    Check(run.StageNumber == stage+1, "Transition skipped a stage");
                    Check(run.CurrentDistrict != previous, "Adjacent stages repeated the same district");
                    Check(run.DistrictElapsed < 0.02f && run.DarknessAmount == 0 && !run.BlackoutWarning,
                        "A new stage retained elapsed time or blackout from its predecessor");
                    if (stage == 1) firstTransitions.Add(run.CurrentDistrict);
                    previous=run.CurrentDistrict; stage=run.StageNumber;
                }
            }
            Check(run.StageNumber >= 7, "District clock failed to progress");
        }
        Check(visited.Count == 6, "Random stage progression omitted one or more districts across seeds");
        Check(firstTransitions.Count > 2, "First transition still follows a fixed map order");
    }

    private static void BlackoutTimeline()
    {
        const float dt=1f/120;
        var starts=new HashSet<int>(); var durations=new HashSet<int>();
        foreach(District district in Enum.GetValues(typeof(District)))
        for(int seed=0;seed<8;seed++)
        {
            RunnerSession run=New(seed); run.SetStartingDistrict(district); run.StartRun();
            bool darkMap=district==District.Night;
            float firstDark=-1,firstWarning=-1,full=0,block=0,peak=0;
            int completed=0; bool partial=false;
            starts.Add((int)(run.NextBlackoutAt*100)); durations.Add((int)(run.BlackoutDuration*100));
            for(int i=0;i<120*34;i++)
            {
                IsolateWorld(run); run.Update(dt); float amount=run.DarknessAmount;
                Check(!float.IsNaN(amount)&&amount>=0&&amount<=1,"Blackout opacity outside normalized bounds");
                if(!darkMap) Check(amount==0&&!run.BlackoutWarning,"Unexpected blackout on "+district);
                if(run.BlackoutWarning&&firstWarning<0) firstWarning=run.DistrictElapsed;
                if(amount>0)
                {
                    if(firstDark<0) firstDark=run.DistrictElapsed;
                    block+=dt; if(amount>=0.99999f) full+=dt;
                    Check(block<=3.4f,"Blackout exceeded 3 seconds plus soft fades");
                    partial|=amount>0.05f&&amount<0.95f;
                }
                else if(block>0)
                {
                    Check(full>=1.98f&&full<=3.02f,"Fully opaque blackout did not last 2-3 seconds: "+full);
                    completed++; block=full=0;
                }
                peak=Math.Max(peak,amount);
            }
            if(darkMap)
            {
                Check(firstDark>=6.98f&&firstDark<=13.02f,"Opening blackout grace outside intended range");
                Check(firstWarning>=0&&firstDark-firstWarning>=1.4f,"Blackout missing advance warning");
                Check(completed>=1&&peak==1&&partial,"No complete opaque/faded blackout");
            }
        }
        Check(starts.Count>8&&durations.Count>8,"Blackout timing and duration were not randomized");
    }

    private static void BlackoutStateTransitions()
    {
        foreach (District district in new[] {District.Night})
        {
            RunnerSession run=New(778); run.SetStartingDistrict(district); run.StartRun();
            while (run.DarknessAmount<0.9f) Advance(run,1f/120);
            float elapsed=run.DistrictElapsed, darkness=run.DarknessAmount;
            bool warning=run.BlackoutWarning;
            run.TogglePause(); run.Update(10);
            Near(run.DistrictElapsed,elapsed,0,"Paused blackout advanced its clock");
            Near(run.DarknessAmount,darkness,0,"Paused blackout changed opacity");
            Check(run.BlackoutWarning==warning,"Paused blackout changed warning");
            run.TogglePause(); Advance(run,3.4f);
            Check(run.DarknessAmount==0,"Blackout failed to end after resume");
            run.ReturnToMenu();
            Check(run.DarknessAmount==0 && !run.BlackoutWarning,"Menu retained a blackout");
            run.StartRun();
            Check(run.CurrentDistrict==district && run.DistrictElapsed==0 &&
                run.DarknessAmount==0 && !run.BlackoutWarning,"Retry retained old blackout phase");
            Advance(run,RunnerSession.DistrictDuration+0.05f);
            Check(run.CurrentDistrict!=district && run.DistrictElapsed<0.1f &&
                run.DarknessAmount==0 && !run.BlackoutWarning,"Map transition retained old blackout phase");
        }
    }

    private static void LightningCadence()
    {
        foreach(District district in Enum.GetValues(typeof(District)))
        {
            bool active=district==District.Storm;
            float previousPulse=-100,block=0,minDark=1,maxDark=0; int pulseCount=0,lastStrike=0;
            for(int i=0;i<3000;i++)
            {
                float time=i/100f; LightningFrame frame=RunnerWeather.GetLightning(district,time);
                Check(frame.Amount>=0&&frame.Amount<=1&&!float.IsNaN(frame.Amount),"Lightning amount outside finite bounds");
                if(!active) Check(frame.Amount==0&&frame.StrikeId==0,"Lightning appeared outside storm");
                if(time<RunnerWeather.StormFirstStrike) Check(frame.Amount==0,"Lightning skipped opening grace");
                if(frame.Amount>0.001f)
                {
                    if(block==0)
                    {
                        if(pulseCount>0) Near(time-previousPulse,RunnerWeather.StormStrikeInterval,0.03f,"Lightning interval drifted");
                        Check(frame.StrikeId>lastStrike,"A new flash reused its thunder identity");
                        previousPulse=time; lastStrike=frame.StrikeId; pulseCount++;
                    }
                    block+=0.01f;
                    Check(block<=RunnerWeather.LightningPulseDuration+0.02f,"Lightning stayed continuously white");
                    Check(frame.BoltX>=180&&frame.BoltX<=1100,"Lightning bolt position escaped the canvas");
                }
                else block=0;
                float darkness=RunnerWeather.GetStormDarkness(time);
                Check(darkness>=0&&darkness<=1&&!float.IsNaN(darkness),"Storm darkness invalid");
                minDark=Math.Min(minDark,darkness); maxDark=Math.Max(maxDark,darkness);
            }
            Check(active?pulseCount>=15:pulseCount==0,"Unexpected lightning cadence on "+district);
            Check(minDark<0.15f&&maxDark>0.85f,"Storm did not alternate clear flashes and deep darkness");
        }
        foreach(float invalid in new[]{float.NaN,float.PositiveInfinity,-1f})
            Check(RunnerWeather.GetLightning(District.Storm,invalid).Amount==0&&RunnerWeather.GetStormDarkness(invalid)==0,
                "Invalid weather clock produced an effect");
    }

    private static void SkywayOpening()
    {
        for (int seed=0; seed<6; seed++)
        {
            RunnerSession run=New(seed); run.SetStartingDistrict(District.Skyway); run.StartRun();
            bool gap=false;
            for (int i=0; i<120*34; i++)
            {
                IsolateWorld(run); run.Update(1f/120);
                bool newGap=run.Entities.Any(e=>e.Kind==EntityKind.Gap && !e.Removed);
                if (run.Elapsed<10-0.02f) Check(!newGap,"Skyway spawned a gap during opening grace");
                gap|=newGap;
            }
            Check(gap,"Selected Skyway failed to generate a real gap after opening grace");
        }
    }

    private sealed class PackSpec
    {
        public int Seed;
        public District District;
        public int Id;
        public float Speed;
        public float Start;
        public float End;
        public float SpawnTime;
        public List<WorldEntity> Entities;
    }

    private static List<PackSpec> catalog;

    private static WorldEntity CopyEntity(WorldEntity source)
    {
        return new WorldEntity
        {
            Id = source.Id, PatternId = source.PatternId, Kind = source.Kind, Variant = source.Variant,
            X = source.X, Y = source.Y, BaseY = source.BaseY, Width = source.Width, Height = source.Height,
            Rotation = source.Rotation, MotionPhase = source.MotionPhase, MotionAmplitude = source.MotionAmplitude,
            MotionAmplitudeX = source.MotionAmplitudeX, MotionSpeed = source.MotionSpeed, CollapseProgress=source.CollapseProgress, Collapsed=source.Collapsed
        };
    }

    private static List<PackSpec> Packs()
    {
        if (catalog != null) return catalog;
        catalog = new List<PackSpec>();
        for (int seed = 0; seed < 12; seed++)
        {
            RunnerSession run = New(seed); run.SetStartingDistrict((District)(seed%6)); run.StartRun();
            var seen = new HashSet<int>();
            for (int i = 0; i < 120 * 215; i++)
            {
                foreach (var group in run.Entities.Where(e => !e.Removed && Hazard(e)).GroupBy(e => e.PatternId))
                {
                    if (!seen.Add(group.Key)) continue;
                    Check(group.Key > 0, "Generated hazard pack lacks a shared identity");
                    var entities = group.Select(CopyEntity).ToList();
                    // Audit the complete swept envelope, not only the current
                    // sine position, so opposing motion cannot hide an unsafe gap.
                    float start = entities.Min(e => e.X + run.Distance - MotionOffset(e,run.Elapsed) - e.MotionAmplitudeX);
                    float end = entities.Max(e => e.X + e.Width + run.Distance - MotionOffset(e,run.Elapsed) + e.MotionAmplitudeX);
                    foreach (WorldEntity e in entities) e.X = e.X + run.Distance - start;
                    catalog.Add(new PackSpec { Seed=seed, District=run.CurrentDistrict, Id=group.Key, Speed=run.Speed, SpawnTime=run.Elapsed,
                        Start=start, End=end, Entities=entities });
                }
                // Generation checks remove hazards only after recording the complete
                // pack. Physical traversability is checked separately with real collisions.
                IsolateWorld(run); run.Update(1f / 120);
            }
            Check(seen.Count > 60, "Generation did not exercise enough packs for seed " + seed);
        }
        return catalog;
    }

    private static void SpawnSafety()
    {
        foreach (var seed in Packs().GroupBy(p => p.Seed))
        {
            PackSpec previous = null;
            foreach (PackSpec pack in seed.OrderBy(p => p.Id))
            {
                if (previous != null)
                {
                    float gap = pack.Start - previous.End;
                    // At least 0.7s recovery remains even if the player holds right.
                    float recovery = gap / (previous.Speed + RunnerSession.HorizontalSpeed);
                    Check(recovery >= 0.70f, "Insufficient inter-pack recovery for seed " + pack.Seed +
                        " pack " + pack.Id + ": " + recovery.ToString("0.000") + "s");
                }
                bool floorHazard = pack.Entities.Any(e => e.Kind != EntityKind.Plane &&
                    !(e.Kind == EntityKind.Saw && e.BaseY > RunnerSession.DuckHeight));
                bool ceilingHazard = pack.Entities.Any(e => e.Kind == EntityKind.Plane ||
                    (e.Kind == EntityKind.Saw && e.BaseY > RunnerSession.DuckHeight));
                Check(!(floorHazard && ceilingHazard), "A pack combines incompatible jump/duck lanes");
                if (pack.Entities.Any(e => e.Kind == EntityKind.Gap))
                    Check(pack.Entities.All(e=>e.Kind==EntityKind.Gap), "A ground obstacle covers a gap in the same pack");
                previous = pack;
            }
        }
    }

    private static void HorizontalMovement()
    {
        RunnerSession run = New(); run.StartRun();
        Near(run.PlayerX, RunnerSession.StartPlayerX, 0, "Incorrect starting position");
        run.SetHorizontal(1); Advance(run, 0.5f);
        Near(run.PlayerX, RunnerSession.StartPlayerX + RunnerSession.HorizontalSpeed * 0.5f, 0.1f,
            "Right input did not move at the configured speed");
        Advance(run, 3);
        Near(run.PlayerX, RunnerSession.MaximumPlayerX, 0.01f, "Right screen bound failed");
        run.SetHorizontal(-1); Advance(run, 4);
        Near(run.PlayerX, RunnerSession.MinimumPlayerX, 0.01f, "Left screen bound failed");
        run.SetHorizontal(0); float before = run.PlayerX; Advance(run, 0.5f);
        Near(run.PlayerX, before, 0, "Releasing movement did not stop horizontal travel");
    }

    private static void AirSteering()
    {
        RunnerSession run = New(); run.StartRun(); run.Jump();
        run.SetHorizontal(1); Advance(run, 0.25f);
        Check(run.PlayerY > 0 && run.PlayerX > RunnerSession.StartPlayerX, "Right steering failed in the air");
        run.SetHorizontal(-1); Advance(run, 0.25f);
        Check(run.PlayerY > 0, "Steering unexpectedly cancelled the jump");
        Near(run.PlayerX, RunnerSession.StartPlayerX, 0.1f, "Left steering failed in the air");
    }

    private static void HorizontalInputValidation()
    {
        RunnerSession run = New(); run.StartRun();
        foreach (float invalid in new[] { float.NaN, float.PositiveInfinity, float.NegativeInfinity })
        {
            float before = run.PlayerX; run.SetHorizontal(invalid); Advance(run, 0.1f);
            Near(run.PlayerX, before, 0, "Invalid axis changed position");
        }
        run.SetHorizontal(100); Advance(run, 0.1f);
        Near(run.PlayerX, RunnerSession.StartPlayerX + RunnerSession.HorizontalSpeed * 0.1f, 0.1f, "Axis above one was not clamped");
        run.SetHorizontal(-100); Advance(run, 0.1f);
        Near(run.PlayerX, RunnerSession.StartPlayerX, 0.1f, "Axis below minus one was not clamped");
    }

    private static void HorizontalStateTransitions()
    {
        RunnerSession run = New();
        run.SetHorizontal(1); run.Update(0.1f);
        Near(run.PlayerX, RunnerSession.StartPlayerX, 0, "Menu accepted horizontal movement");
        run.StartRun(); run.SetHorizontal(1); Advance(run, 0.2f);
        run.TogglePause(); float pausedX = run.PlayerX;
        run.SetHorizontal(-1); run.Update(30);
        Near(run.PlayerX, pausedX, 0, "Pause accepted horizontal movement");
        run.TogglePause(); Advance(run, 0.1f);
        Near(run.PlayerX, pausedX, 0, "Resume reused stale horizontal input");
        run.SetHorizontal(1); run.ReturnToMenu();
        float menuX = run.PlayerX; run.SetHorizontal(-1); run.Update(1);
        Near(run.PlayerX, menuX, 0, "Return to menu retained horizontal movement");
        run.StartRun(); Advance(run, 0.1f);
        Near(run.PlayerX, RunnerSession.StartPlayerX, 0, "Restart did not reset horizontal position/input");
        Inject(run, EntityKind.House); run.Update(1f / 120);
        Check(run.State == RunState.GameOver, "Fixture did not end run");
        float deadX = run.PlayerX; run.SetHorizontal(1); run.Update(1);
        Near(run.PlayerX, deadX, 0, "Game over accepted horizontal movement");
    }

    private static void MovingCollision()
    {
        RunnerSession run = New(); run.StartRun(); run.SetHorizontal(1); Advance(run, 0.7f);
        run.SetHorizontal(0); World(run).Clear();
        World(run).Add(new WorldEntity { Id=9100, Kind=EntityKind.House,
            X=RunnerSession.StartPlayerX-30, Width=60, Height=64 });
        run.Update(1f / 120);
        Check(run.State == RunState.Playing, "An obstacle at the old anchor hit the moved player");
        Inject(run, EntityKind.Barrier); run.Update(1f / 120);
        Check(run.State == RunState.GameOver, "An obstacle at the moved player did not hit");
    }

    private static void MovingPickups()
    {
        RunnerSession run = New(); run.StartRun(); run.SetHorizontal(1); Advance(run, 0.7f);
        run.SetHorizontal(0); World(run).Clear();
        int before = run.RunStars;
        WorldEntity old = new WorldEntity { Id=9101, Kind=EntityKind.Star,
            X=RunnerSession.StartPlayerX-14, Y=22, Width=28, Height=28 };
        World(run).Add(old); run.Update(1f / 120);
        Check(!old.Removed && run.RunStars == before, "A star at the old anchor was collected remotely");
        Inject(run, EntityKind.Star, 22, 28, 28); run.Update(1f / 120);
        Check(run.RunStars == before + 1, "A star at the current player position was not collected");
        Inject(run, EntityKind.Heart, 22, 28, 28); run.Update(1f / 120);
        Check(run.Lives == 2, "A heart at the current player position was not collected");
    }

    private static void MovingFloor()
    {
        RunnerSession run = New(upgrade: 1); run.StartRun(); run.SetHorizontal(1); Advance(run, 0.7f);
        run.SetHorizontal(0); World(run).Clear();
        World(run).Add(new WorldEntity { Id=9102, Kind=EntityKind.Gap,
            X=RunnerSession.StartPlayerX-40, Width=80, Height=160 });
        run.Update(0.1f);
        Near(run.PlayerY, 0, 0, "A gap at the old anchor removed floor under the moved player");
        World(run).Clear(); Inject(run, EntityKind.Gap, 0, 500, 160);
        Advance(run, 0.15f, false);
        Check(run.PlayerY < 0, "A gap at the new player position did not remove floor");
        for (int i=0; i<120 && run.Lives==2; i++) run.Update(1f/120);
        Check(run.Lives==1 && run.State==RunState.Playing, "Moved player did not recover once from a fall");
        Advance(run, 1, false);
        Check(run.Lives==1 && run.PlayerY==0, "Fall recovery left a hole under the moved player");
    }

    private static void WalkIntoGap()
    {
        RunnerSession run = New(); run.StartRun(); World(run).Clear();
        World(run).Add(new WorldEntity { Id=9103, Kind=EntityKind.Gap,
            X=400, Width=300, Height=160 });
        run.SetHorizontal(1); Advance(run, 0.35f, false);
        Check(run.PlayerX > RunnerSession.StartPlayerX && run.PlayerY < 0,
            "Walking into a scrolling gap retained floor support");
    }

    private static void SetField(RunnerSession run, string name, object value)
    {
        FieldInfo field = typeof(RunnerSession).GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        if (field == null) throw new InvalidOperationException("Fixture cannot find state " + name);
        field.SetValue(run, value);
    }

    private static void SetProperty(RunnerSession run, string name, object value)
    {
        SetField(run, "<" + name + ">k__BackingField", value);
        string clock=name=="Elapsed"?"elapsedClock":name=="Distance"?"distanceClock":name=="DistrictElapsed"?"districtClock":null;
        if(clock!=null) SetField(run,clock,(double)(float)value);
    }

    private static void SpikeShape()
    {
        RunnerSession corner = New(); corner.StartRun(); World(corner).Clear();
        SetProperty(corner, "PlayerY", 20f); SetField(corner, "grounded", false);
        World(corner).Add(new WorldEntity { Id=9104, Kind=EntityKind.Spike,
            X=corner.PlayerX+18, Y=0, Width=100, Height=100 });
        corner.Update(1f/120);
        Check(corner.State==RunState.Playing, "Empty upper corner of a spike was treated as solid");
        RunnerSession middle = New(); middle.StartRun(); World(middle).Clear();
        SetProperty(middle, "PlayerY", 20f); SetField(middle, "grounded", false);
        Inject(middle, EntityKind.Spike, 0, 100, 100); middle.Update(1f/120);
        Check(middle.State==RunState.GameOver, "Solid triangle centre did not cause damage");
    }

    private static void SawShape()
    {
        RunnerSession corner = New(); corner.StartRun(); World(corner).Clear();
        World(corner).Add(new WorldEntity { Id=9105, Kind=EntityKind.Saw,
            // The original X+21/Y80 corner now legitimately overlaps the
            // dinosaur's visible nose. This corner lies above its actual head.
            X=corner.PlayerX+48, Y=92, BaseY=92, Width=80, Height=80 });
        corner.Update(1f/120);
        Check(corner.State==RunState.Playing, "Empty corner outside the circular blade was treated as solid");
        RunnerSession middle = New(); middle.StartRun(); World(middle).Clear();
        Inject(middle, EntityKind.Saw, 16, 80, 80); middle.Update(1f/120);
        Check(middle.State==RunState.GameOver, "Blade centre did not cause damage");
    }

    private static WorldEntity BladeAt(float localX, float localY, float diameter=4)
    {
        return new WorldEntity { Kind=EntityKind.Saw, X=RunnerSession.StartPlayerX+localX-diameter/2,
            Y=localY-diameter/2, BaseY=localY-diameter/2, Width=diameter, Height=diameter };
    }

    private static bool BladeTouches(float x, float y, bool facingLeft=false, int frame=0,
        bool duck=false, float diameter=4, float playerY=0, float bob=0)
    {
        return RunnerGeometry.SawTouchesPlayer(BladeAt(x,y,diameter),RunnerSession.StartPlayerX,
            playerY,duck,facingLeft,frame,bob);
    }

    private static void SawVisibleBody()
    {
        foreach (bool left in new[] {false,true})
        {
            float sign=left ? -1 : 1;
            Check(!BladeTouches(-15*sign,75,left), "Transparent space behind the neck caused damage");
            Check(BladeTouches(30*sign,80,left), "Visible head was missed outside the old player rectangle");
            RunnerSession safe=New(); safe.StartRun(); World(safe).Clear();
            SetProperty(safe,"FacingLeft",left);
            World(safe).Add(BladeAt(-15*sign,75)); safe.Update(1f/120);
            Check(safe.State==RunState.Playing,"Production collision hit transparent neck space");
            RunnerSession hit=New(); hit.StartRun(); World(hit).Clear();
            SetProperty(hit,"FacingLeft",left);
            World(hit).Add(BladeAt(30*sign,80)); hit.Update(1f/120);
            Check(hit.State==RunState.GameOver,"Production collision missed the visible head");
        }
        RunnerSession facing=New(); facing.StartRun(); facing.SetHorizontal(-1); Advance(facing,0.1f);
        Check(facing.FacingLeft,"Left movement did not mirror saw contact pose");
        facing.SetHorizontal(0); Advance(facing,0.1f);
        Check(facing.FacingLeft,"Releasing movement changed the facing pose");
        facing.SetHorizontal(1); Advance(facing,0.1f);
        Check(!facing.FacingLeft,"Right movement did not restore the right-facing contact pose");
        facing.SetHorizontal(-1); Advance(facing,0.1f); facing.ReturnToMenu(); facing.StartRun();
        Check(!facing.FacingLeft,"Restart retained the old facing pose");
    }

    private static void SawToothEdges()
    {
        foreach (bool left in new[] {false,true})
        {
            float sign=left ? -1 : 1;
            Check(BladeTouches(70*sign,80,left,diameter:66),
                "Visible blade teeth overlapped the head without registering contact");
            Check(!BladeTouches(76*sign,80,left,diameter:66),
                "Blade damaged the dinosaur with a clear gap beyond its tooth tips");
            Check(!BladeTouches(70*sign,125,left,diameter:66),
                "An empty diagonal corner of the blade caused damage");
        }
    }

    private static void SawAnimatedPose()
    {
        Check(BladeTouches(46,80,frame:2),"Airborne sprite's visible extended nose was missed");
        Check(!BladeTouches(46,80,frame:0),"Ground pose incorrectly used the jump frame's extended nose");
        Check(!BladeTouches(0,75,frame:2),"Jump-frame neck transparency caused damage");
        Check(BladeTouches(33,38,duck:true),"Crouched head was missed");
        Check(!BladeTouches(-15,36,duck:true),"Crouched neck transparency caused damage");
        Check(!BladeTouches(33,80,duck:true),"Crouched dinosaur retained the standing silhouette");
        Check(BladeTouches(30,89.8f,diameter:1),"Fixture did not touch the visible top edge before bob");
        Check(!BladeTouches(30,89.8f,diameter:1,bob:1.3f),"Saw collision ignored the rendered downward bob");
        RunnerSession airborne=New(); airborne.StartRun(); World(airborne).Clear();
        SetProperty(airborne,"PlayerY",40f); SetField(airborne,"grounded",false);
        // New hang pose has a lower, tucked head; use a PNG-alpha-confirmed
        // opaque point rather than the old running sprite nose location.
        World(airborne).Add(BladeAt(34,112,8)); airborne.Update(1f/120);
        Check(airborne.State==RunState.GameOver,"Production airborne collision used the wrong pose or height");
        RunnerSession duck=New(); duck.StartRun(); World(duck).Clear(); duck.SetDucking(true);
        World(duck).Add(BladeAt(40,24,8)); duck.Update(1f/120);
        Check(duck.State==RunState.GameOver,"Production crouch collision missed the visible head");
    }

    private static void SawDamage()
    {
        RunnerSession run=New(upgrade:2); run.StartRun(); World(run).Clear();
        World(run).Add(BladeAt(30,80)); run.Update(1f/120);
        Check(run.Lives==2 && run.State==RunState.Playing && run.InvincibleRemaining>0,
            "Saw contact did not consume exactly one life and start grace");
        World(run).Add(BladeAt(30,80)); run.Update(1f/120);
        Check(run.Lives==2,"Saw ignored temporary invulnerability");
        Advance(run,2.1f); World(run).Clear();
        int frame=RunnerGeometry.GetPlayerFrame(run.Elapsed+1f/120,run.Speed,run.PlayerY,run.State);
        // All supplied animation frames share this solid middle-body region.
        World(run).Add(BladeAt(5,35,12)); run.Update(1f/120);
        Check(run.Lives==1,"Saw stayed harmless after the grace period expired (frame " + frame + ")");
    }

    private static void ActionSilhouettes()
    {
        // Coordinates were inspected against actual action-atlas alpha, not
        // copied from the collision bands. Check both mirrored orientations.
        foreach(bool left in new[]{false,true})
        {
            float sign=left?-1:1;
            foreach(int frame in new[]{5,6,7})
            {
                Check(BladeTouches(40*sign,24,left,frame,true),"Action crouch head missed in frame "+frame);
                Check(BladeTouches(0,22,left,frame,true),"Action crouch body missed in frame "+frame);
                Check(!BladeTouches(-45*sign,42,left,frame,true),"Action crouch transparency was solid in frame "+frame);
            }
            foreach(int frame in new[]{8,9,10})
            {
                Check(BladeTouches(34*sign,72,left,frame),"Action jump head missed in frame "+frame);
                Check(BladeTouches(0,35,left,frame),"Action jump body missed in frame "+frame);
                Check(!BladeTouches(-15*sign,75,left,frame),"Action jump neck transparency was solid in frame "+frame);
            }
        }
    }

    private static void ActionPoseTransitions()
    {
        RunnerSession run=New(); run.StartRun(); World(run).Clear(); StopSpawns(run);
        Check(RunnerGeometry.GetPlayerFrame(run)<5,"Running selected an action-atlas frame");
        run.SetDucking(true);
        for(int i=0;i<3;i++)
        {
            SetProperty(run,"Elapsed",i*0.1f);
            Check(RunnerGeometry.GetPlayerFrame(run)==5+i,"Crouch did not cycle through three dedicated frames");
        }
        run.SetDucking(false); Check(run.Jump(),"Jump pose fixture could not launch"); run.Update(0.02f);
        Check(RunnerGeometry.GetPlayerFrame(run)==8,"Rising jump did not select the launch pose");
        while(run.AnimationVelocityY>200) run.Update(1f/120);
        Check(run.PlayerY>3&&RunnerGeometry.GetPlayerFrame(run)==9,"Jump apex did not select the tucked pose");
        while(run.AnimationVelocityY>=-230) run.Update(1f/120);
        Check(run.PlayerY>3&&RunnerGeometry.GetPlayerFrame(run)==10,"Falling jump did not select the landing pose");
        run.TogglePause(); int paused=RunnerGeometry.GetPlayerFrame(run); run.Update(10);
        Check(RunnerGeometry.GetPlayerFrame(run)==paused,"Paused airborne action frame advanced");
        run.TogglePause(); Check(run.Jump(),"Second jump pose fixture was rejected");
        Check(RunnerGeometry.GetPlayerFrame(run)==8,"Double jump did not return to the launch pose");
    }

    private static void SawFatalPose()
    {
        foreach(string pose in new[]{"run","crouch","rising","apex","falling"})
        {
            RunnerSession run=New(); run.StartRun(); World(run).Clear(); StopSpawns(run);
            SetProperty(run,"Elapsed",0.3f); SetProperty(run,"FacingLeft",true);
            bool duck=pose=="crouch";
            bool airborne=pose=="rising"||pose=="apex"||pose=="falling";
            run.SetDucking(duck);
            if(airborne)
            {
                SetProperty(run,"PlayerY",40f); SetField(run,"grounded",false);
                SetProperty(run,"VelocityY",pose=="rising"?500f:pose=="falling"?-500f:0f);
            }
            World(run).Add(duck?BladeAt(-40,24,12):BladeAt(0,airborne?75:35,16));
            run.Update(1f/120);
            Check(run.State==RunState.GameOver,"Fatal "+pose+" fixture did not contact a solid body region");
            Check(run.IsDucking==duck&&run.FacingLeft,"Fatal contact changed crouch or facing pose");
            int frame=RunnerGeometry.GetPlayerFrame(run);
            int expected=duck?5+(int)(Math.Floor((double)run.Elapsed*10)%3)
                :pose=="rising"?8:pose=="apex"?9:pose=="falling"?10
                :RunnerGeometry.GetPlayerFrame(run.Elapsed,run.Speed,run.PlayerY,RunState.Playing);
            Check(frame==expected,"Fatal contact reset the displayed "+pose+" frame");
            if(airborne)
            {
                Check(run.VelocityY==0,"Fatal airborne contact did not stop physics");
                if(pose!="apex") Check(Math.Abs(run.AnimationVelocityY)>220,"Fatal airborne contact lost its animation velocity snapshot");
            }
            float bob=RunnerGeometry.GetPlayerBob(run.Elapsed,run.PlayerY,run.IsDucking,run.State);
            float contactBob=RunnerGeometry.GetPlayerBob(run.Elapsed,run.PlayerY,run.IsDucking,RunState.Playing);
            Near(bob,contactBob,0,"Fatal contact reset the visible bob");
            run.Update(10);
            Check(RunnerGeometry.GetPlayerFrame(run)==frame,"Dead action animation continued advancing");
            Near(RunnerGeometry.GetPlayerBob(run.Elapsed,run.PlayerY,run.IsDucking,run.State),bob,0,"Dead bob continued advancing");
            run.ReturnToMenu(); run.StartRun();
            Check(!run.IsDucking&&!run.FacingLeft&&run.AnimationVelocityY==0,"New run retained the fatal action pose");
        }
    }

    private static void SawMotion()
    {
        RunnerSession run = New(); run.StartRun(); World(run).Clear();
        WorldEntity saw = Inject(run, EntityKind.Saw, 61, 76, 76);
        saw.MotionAmplitude = 8; saw.MotionPhase = 0;
        run.SetDucking(true);
        for (int i=0; i<360; i++)
        {
            saw.X = run.PlayerX-saw.Width/2;
            run.Update(1f/120);
            Check(run.State==RunState.Playing, "A floating saw invaded the safe duck lane");
            Check(saw.Y >= 53-0.01f && saw.Y <= 69+0.01f, "Saw left its authored vertical motion band");
        }
        Check(saw.Rotation != 0 && saw.Y != saw.BaseY, "Saw did not rotate and bob");
    }

    private static float MotionOffset(WorldEntity e,float time)
    { return (float)Math.Sin(time*e.MotionSpeed+e.MotionPhase)*e.MotionAmplitudeX; }

    private static void StopSpawns(RunnerSession run)
    { SetField(run,"distanceToPattern",1000000f); SetField(run,"nextHeartAt",float.MaxValue); SetField(run,"nextPowerupAt",double.MaxValue); SetField(run,"nextSurvivalSupplyAt",double.MaxValue); }

    private static WorldEntity MotionFixture(RunnerSession run)
    {
        World(run).Clear(); StopSpawns(run);
        var e=new WorldEntity { Id=9981,Kind=EntityKind.Barrier,X=10000,Width=70,Height=76,
            MotionAmplitudeX=24,MotionSpeed=1.3f,MotionPhase=0.7f };
        World(run).Add(e); return e;
    }

    private static void HazardMotion()
    {
        RunnerSession run=New(); run.StartRun(); WorldEntity e=MotionFixture(run);
        float anchor=e.X-MotionOffset(e,run.Elapsed);
        for(int i=0;i<120*12;i++)
        {
            run.Update(1f/120);
            Near(e.X+run.Distance-MotionOffset(e,run.Elapsed),anchor,0.15f,"Moving hazard accumulated sine drift");
        }
        run.TogglePause(); float x=e.X; run.Update(20); Near(e.X,x,0,"Paused hazard moved");
        run.ReturnToMenu(); run.StartRun(); Check(!run.Entities.Contains(e),"Restart retained old moving obstacle");
    }

    private static void HazardMotionCollision()
    {
        RunnerSession run=New(); run.StartRun(); World(run).Clear(); StopSpawns(run); run.Save.Wallet=80;
        var e=new WorldEntity { Id=9982,Kind=EntityKind.Barrier,X=run.PlayerX+150,Width=30,Height=70,
            MotionAmplitudeX=100,MotionSpeed=10,MotionPhase=(float)Math.PI/2 };
        World(run).Add(e); Advance(run,0.16f,false);
        Check(run.State==RunState.GameOver,"Moving obstacle did not hit at its live position");
        Check(run.Save.Wallet==80,"Hazard collision spent collected stars");
    }

    private static void MovingDuckLane()
    {
        foreach(EntityKind kind in new[]{EntityKind.Saw,EntityKind.Plane})
        {
            RunnerSession run=New(); run.StartRun(); World(run).Clear(); StopSpawns(run); run.SetDucking(true);
            var e=new WorldEntity { Id=9983,Kind=kind,Width=76,Height=76,BaseY=72,Y=72,MotionAmplitude=16,MotionSpeed=1.8f };
            World(run).Add(e);
            for(int i=0;i<120*4;i++)
            {
                e.X=run.PlayerX-e.Width/2; run.Update(1f/120);
                Check(run.State==RunState.Playing,"Vertical moving hazard invaded duck lane");
                Check(e.Y>=55.99f&&e.Y<=88.01f,"Vertical hazard left its authored envelope");
            }
        }
    }

    private static void MovingFrameStep()
    {
        RunnerSession a=New(),b=New(); a.StartRun(); b.StartRun();
        WorldEntity ea=MotionFixture(a),eb=MotionFixture(b);
        a.Update(0.2f); Advance(b,0.2f,false);
        Near(ea.X,eb.X,0.02f,"Frame partition changed horizontal hazard motion");
        Near(ea.Y,eb.Y,0.01f,"Frame partition changed vertical hazard motion");
    }

    private static void PatternVariety()
    {
        HashSet<EntityKind> kinds = new HashSet<EntityKind>(Packs().SelectMany(p => p.Entities).Select(e => e.Kind));
        foreach (EntityKind expected in new[] { EntityKind.House, EntityKind.Hydrant, EntityKind.Plane,
            EntityKind.Gap, EntityKind.Spike, EntityKind.Saw, EntityKind.Barrier })
            Check(kinds.Contains(expected), "Endless generation omitted " + expected);
        Check(Packs().Any(p => p.Entities.Count(e => e.Kind==EntityKind.Spike)==3), "No triple spike rhythm appeared");
        Check(Packs().Any(p => p.Entities.Count(e => e.Kind==EntityKind.Barrier)==2), "No stepped barrier rhythm appeared");
        Check(Packs().Any(p => p.Entities.Any(e => e.Kind==EntityKind.Saw && e.MotionAmplitude>0)), "No floating moving blade appeared");
        Check(Packs().Any(p => p.Entities.Any(e => e.Kind==EntityKind.Saw && e.MotionAmplitudeX>0)), "No horizontally patrolling saw appeared");
        Check(Packs().Any(p => p.Entities.Any(e => e.Kind==EntityKind.Barrier && e.MotionAmplitudeX>0)), "No moving barrier appeared");
        Check(Packs().Where(p=>p.SpawnTime<30).All(p=>p.Entities.All(e=>e.MotionAmplitudeX==0)),
            "Advanced moving packs appeared before their unlock time");
    }

    private static string PackSignature(PackSpec pack)
    {
        return string.Join(";", pack.Entities.Select(e => e.Kind + ":" +
            Math.Ceiling(e.X/10) + ":" + Math.Ceiling(e.Width/10) + ":" + Math.Ceiling(e.Height/10) + ":" + Math.Ceiling(e.BaseY/8f) +
            ":motion=" + Math.Ceiling(e.MotionAmplitudeX/16f) + "," + Math.Ceiling(e.MotionAmplitude/8f) + "," + Math.Ceiling(e.MotionSpeed*2f)));
    }

    private static bool ReplayPack(PackSpec pack, float speed, float playerX, float lead, float secondJumpDelay, float phaseShift=0)
    {
        RunnerSession run = New(991); run.StartRun(); World(run).Clear();
        SetProperty(run, "Elapsed", (speed-RunnerSession.InitialSpeed)/RunnerSession.SpeedAcceleration);
        SetProperty(run, "Speed", speed); SetProperty(run, "PlayerX", playerX);
        StopSpawns(run);
        var obstacles = new List<WorldEntity>();
        foreach (WorldEntity e in pack.Entities)
        {
            WorldEntity clone = CopyEntity(e);
            float oldPhase=pack.SpawnTime*e.MotionSpeed+e.MotionPhase;
            clone.X += e.MotionAmplitudeX*((float)Math.Sin(oldPhase+phaseShift)-(float)Math.Sin(oldPhase));
            // Preserve the recorded motion phase even when testing another speed
            // whose elapsed clock differs from the original generated pack.
            clone.MotionPhase += (pack.SpawnTime-run.Elapsed)*e.MotionSpeed + phaseShift;
            clone.X += playerX + speed*0.65f;
            obstacles.Add(clone); World(run).Add(clone);
        }
        bool duck = obstacles.All(e => e.Kind==EntityKind.Plane || (e.Kind==EntityKind.Saw && e.BaseY>RunnerSession.DuckHeight));
        run.SetDucking(duck);
        bool jumped = false, jumpedAgain = false;
        float firstAt = 0.65f-lead;
        for (int i=0; i<120*4; i++)
        {
            float t = i/120f;
            if (!duck && !jumped && t>=firstAt) { jumped=run.Jump(); }
            if (!duck && jumped && secondJumpDelay>=0 && !jumpedAgain && t>=firstAt+secondJumpDelay)
                jumpedAgain=run.Jump();
            run.Update(1f/120);
            if (run.State!=RunState.Playing || run.Lives!=1) return false;
            if (obstacles.All(e => e.X+e.Width<run.PlayerX-RunnerSession.PlayerWidth))
            {
                // A missed ledge can cause death after the hole has scrolled past;
                // require an actual landing rather than only a passing X position.
                if (run.PlayerY<=0.01f && run.VelocityY==0) return true;
            }
        }
        return false;
    }

    private static void PatternReplay()
    {
        var samples = Packs().GroupBy(PackSignature).Select(g => g.OrderByDescending(p => p.End-p.Start).First()).ToList();
        int replayed = 0;
        foreach (PackSpec pack in samples)
            foreach (float speed in new[] {RunnerSession.InitialSpeed,900f,1800f,3600f})
                foreach (float x in new[] {RunnerSession.MinimumPlayerX, RunnerSession.MaximumPlayerX})
                {
                    bool pass = false;
                    foreach (float lead in new[] {0.15f,0.2f,0.25f,0.3f,0.35f,0.4f,0.45f,0.5f,0.55f})
                    {
                        foreach (float second in new[] {-1f,0.28f,0.4f,0.52f,0.64f})
                            if (ReplayPack(pack,speed,x,lead,second)) { pass=true; break; }
                        if (pass) break;
                    }
                    Check(pass, "No jump/duck solution for pack " + PackSignature(pack) +
                        " at speed " + speed + " and player X " + x);
                    replayed++;
                }
        Check(samples.Count>=20 && replayed>=80, "Traversability replay had too few distinct real pack geometries");
        Console.WriteLine("  Replayed " + samples.Count + " sampled geometries at four speed samples and both screen bounds.");
    }

    private static bool RobustReplay(PackSpec pack,float speed,float x,float lead,float delay)
    {
        bool moving=pack.Entities.Any(e=>e.MotionAmplitudeX>0);
        foreach(float phase in moving?new[]{0f,1.5707963f,3.1415927f,4.712389f}:new[]{0f})
            if(!ReplayPack(pack,speed,x,lead-0.025f,delay,phase)||!ReplayPack(pack,speed,x,lead+0.025f,delay,phase)) return false;
        return true;
    }

    private static void SequentialRunner()
    {
        int auditedSupplyFrames=0;
        for (int seed=0; seed<6; seed++)
            foreach (float x in new[] {RunnerSession.StartPlayerX,RunnerSession.MaximumPlayerX})
            {
                RunnerSession run=New(seed); run.SetStartingDistrict((District)(seed%6)); run.StartRun(); SetProperty(run,"PlayerX",x);
                int current=0, solved=0;
                bool jumped=false, second=false;
                float jumpTime=0, jumpLead=0.28f, doubleDelay=-1;
                string description="";
                for (int step=0; step<120*215; step++)
                {
                    var group=run.Entities.Where(e => !e.Removed && Hazard(e) &&
                        e.X+e.Width>run.PlayerX-RunnerSession.PlayerWidth).GroupBy(e=>e.PatternId)
                        .OrderBy(g=>g.Min(e=>e.X)).FirstOrDefault();
                    if (group!=null)
                    {
                        if (group.Key!=current)
                        {
                            if (current!=0) solved++;
                            current=group.Key; jumped=false; second=false;
                            var copies=group.Select(CopyEntity).ToList();
                            float left=copies.Min(e=>e.X);
                            foreach (WorldEntity e in copies) e.X-=left;
                            var livePack=new PackSpec { Entities=copies, SpawnTime=run.Elapsed };
                            description=PackSignature(livePack);
                            float arrival=Math.Max(0,(left-run.PlayerX)/run.Speed);
                            float approach=run.Speed+arrival*RunnerSession.SpeedAcceleration;
                            bool found=false;
                            foreach (float lead in new[] {0.15f,0.2f,0.25f,0.3f,0.35f,0.4f,0.45f,0.5f,0.55f})
                            {
                                foreach (float delay in new[] {-1f,0.28f,0.4f,0.52f,0.64f})
                                    if (RobustReplay(livePack,approach,run.PlayerX,lead,delay))
                                    { jumpLead=lead; doubleDelay=delay; found=true; break; }
                                if (found) break;
                            }
                            Check(found,"No sequential plan for "+description);
                        }
                        bool overhead=group.All(e=>e.Kind==EntityKind.Plane ||
                            (e.Kind==EntityKind.Saw && e.BaseY>RunnerSession.DuckHeight));
                        run.SetDucking(overhead);
                        float distance=group.Min(e=>e.X)-run.PlayerX;
                        if (!overhead && !jumped && run.PlayerY<=0.01f &&
                            distance<=run.Speed*jumpLead)
                        {
                            jumped=run.Jump(); jumpTime=run.Elapsed;
                        }
                        if (!overhead && jumped && !second && doubleDelay>=0 && run.Elapsed-jumpTime>=doubleDelay)
                            second=run.Jump();
                    }
                    foreach(WorldEntity item in run.Entities)
                        if(item.Kind==EntityKind.Shield||item.Kind==EntityKind.Magnet||item.Kind==EntityKind.DoubleScore) item.Removed=true;
                    run.SetHorizontal((x-run.PlayerX)*8f/RunnerSession.HorizontalSpeed-run.WindPush/RunnerSession.HorizontalSpeed);
                    int before=run.Lives;
                    run.Update(1f/120);
                    auditedSupplyFrames+=AssertSupplyLanes(run,"seed="+seed+" age="+run.Elapsed);
                    Check(run.State==RunState.Playing && run.Lives>=before,
                        "Sequence bot hit a hazard for seed "+seed+" X="+x+" at "+
                        run.Elapsed.ToString("0.00")+"s pack="+current+" Y="+run.PlayerY+" VY="+run.VelocityY+
                        " jumped="+jumped+" double="+second+" lead="+jumpLead+" plan="+description+" since="+(run.Elapsed-jumpTime)+
                        " warmth="+run.Warmth+" water="+run.Water+" hazards="+string.Join(";",run.Entities.Where(e=>Hazard(e)&&e.X<run.PlayerX+180&&e.X+e.Width>run.PlayerX-100)
                            .Select(e=>e.PatternId+":"+e.Kind+"@"+e.X+","+e.Y+" size="+e.Width+","+e.Height))+" reason="+run.EndReason);
                }
                Check(solved>=70 && run.Score>4000 && run.Speed>800f,
                    "Sequential run did not cover enough authored packs and accelerating play");
            }
        Check(auditedSupplyFrames>10000,"Sequential traversal omitted live supply overlap coverage");
        Console.WriteLine("  Real traversal checked "+auditedSupplyFrames+" visible supply frames against live ground hazards.");
    }

    private static int AssertSupplyLanes(RunnerSession run,string context)
    {
        int checkedFrames=0;
        foreach(WorldEntity supply in run.Entities)
        {
            if(supply.Removed||(supply.Kind!=EntityKind.Water&&supply.Kind!=EntityKind.HotDrink)||
                supply.X+supply.Width<0||supply.X>RunnerSession.CanvasWidth) continue;
            checkedFrames++;
            foreach(WorldEntity hazard in run.Entities)
            {
                if(hazard.Removed||!Hazard(hazard)||hazard.Kind==EntityKind.Plane) continue;
                bool hole=hazard.Kind==EntityKind.Gap||hazard.Kind==EntityKind.CollapsingFloor;
                if(!hole&&(supply.Y+supply.Height<=hazard.Y||supply.Y>=hazard.Y+hazard.Height)) continue;
                Check(supply.X+supply.Width<=hazard.X||supply.X>=hazard.X+hazard.Width,
                    "Live supply overlaps "+hazard.Kind+" "+context+" supplyX="+supply.X+" hazardX="+hazard.X);
            }
        }
        return checkedFrames;
    }

    private static List<PackSpec> lateCatalog;
    private static List<PackSpec> LatePacks()
    {
        if(lateCatalog!=null) return lateCatalog;
        lateCatalog=new List<PackSpec>(); int runId=100;
        foreach(float elapsed in new[]{600f,1200f,3600f})
        foreach(District district in Enum.GetValues(typeof(District)))
        {
            RunnerSession run=New(runId); run.SetStartingDistrict(district); run.StartRun(); World(run).Clear();
            SetProperty(run,"Elapsed",elapsed); SetProperty(run,"Speed",RunnerSession.InitialSpeed+elapsed*RunnerSession.SpeedAcceleration);
            SetField(run,"distanceToPattern",0f); SetField(run,"nextHeartAt",float.MaxValue);
            var seen=new HashSet<int>();
            for(int step=0;step<120*20;step++)
            {
                foreach(var group in run.Entities.Where(e=>!e.Removed&&Hazard(e)).GroupBy(e=>e.PatternId))
                {
                    if(!seen.Add(group.Key)) continue;
                    var entities=group.Select(CopyEntity).ToList();
                    float start=entities.Min(e=>e.X+run.Distance-MotionOffset(e,run.Elapsed)-e.MotionAmplitudeX);
                    float end=entities.Max(e=>e.X+e.Width+run.Distance-MotionOffset(e,run.Elapsed)+e.MotionAmplitudeX);
                    foreach(WorldEntity e in entities) e.X=e.X+run.Distance-start;
                    lateCatalog.Add(new PackSpec{Seed=runId,District=district,Id=group.Key,Speed=run.Speed,SpawnTime=run.Elapsed,Start=start,End=end,Entities=entities});
                }
                IsolateWorld(run); run.Update(1f/120);
            }
            Check(seen.Count>=10,"Late run did not generate enough real packs at "+elapsed+"s on "+district);
            runId++;
        }
        return lateCatalog;
    }

    private static void LateMotionGrowth()
    {
        float previousRange=0,previousFrequency=0;
        foreach(float time in new[]{0f,60f,300f,1000f,3600f})
        {
            RunnerSession run=New(); run.StartRun(); SetProperty(run,"Elapsed",time);
            Check(run.MotionRangeScale>=previousRange&&run.MotionFrequencyScale>=previousFrequency,"Difficulty motion scaling decreased with elapsed time");
            previousRange=run.MotionRangeScale; previousFrequency=run.MotionFrequencyScale;
        }
        Check(previousRange>2.5f&&previousFrequency>2.5f,"Late motion remained as slow and narrow as the beginning");
        bool horizontal=false,verticalSaw=false,movingPlane=false;
        foreach(var grouped in LatePacks().GroupBy(p=>p.Seed))
        {
            PackSpec previous=null;
            foreach(PackSpec pack in grouped.OrderBy(p=>p.Id))
            {
                if(previous!=null)
                    Check((pack.Start-previous.End)/(previous.Speed+RunnerSession.HorizontalSpeed)>=0.699f,
                        "Late swept envelopes overlap their recovery interval at "+pack.SpawnTime);
                foreach(WorldEntity e in pack.Entities)
                {
                    if(e.MotionAmplitudeX>0)
                    {
                        horizontal=true;
                        Check(e.MotionAmplitudeX>40&&e.MotionSpeed>2,"Late patrol did not move wider and faster");
                        Check(e.Width+e.MotionAmplitudeX*2<=pack.End-pack.Start+2,"Spawn width omitted a moving hazard's swept envelope");
                    }
                    if(e.MotionAmplitude>0)
                    {
                        Check(e.BaseY-e.MotionAmplitude>=55.99f,"Growing vertical motion invaded the crouch lane");
                        verticalSaw|=e.Kind==EntityKind.Saw;
                        movingPlane|=e.Kind==EntityKind.Plane;
                    }
                }
                previous=pack;
            }
        }
        Check(horizontal&&verticalSaw&&movingPlane,"Late samples missed patrol, moving saw or moving plane challenges");
    }

    private static void LatePatternReplay()
    {
        var samples=LatePacks().Where(p=>p.Entities.Any(e=>e.MotionAmplitudeX>0||e.MotionAmplitude>0))
            .GroupBy(p=>((int)(p.SpawnTime/600))+":"+string.Join(";",p.Entities.Select(e=>e.Kind+":"+e.Variant+":"+(e.MotionAmplitudeX>0))))
            .Select(g=>g.OrderByDescending(p=>p.End-p.Start).First()).ToList();
        int cases=0;
        foreach(PackSpec pack in samples)
        foreach(float x in new[]{RunnerSession.MinimumPlayerX,RunnerSession.MaximumPlayerX})
        foreach(float phase in new[]{0f,1.5707963f,3.1415927f,4.712389f})
        {
            bool pass=false;
            foreach(float lead in new[]{0.1f,0.15f,0.2f,0.25f,0.3f,0.35f,0.4f,0.45f,0.5f,0.55f})
            {
                foreach(float second in new[]{-1f,0.28f,0.4f,0.52f,0.64f})
                    if(ReplayPack(pack,pack.Speed,x,lead,second,phase)){pass=true;break;}
                if(pass) break;
            }
            Check(pass,"Late moving pack has no jump/duck path at "+pack.Speed+" X="+x+" phase="+phase+" "+PackSignature(pack));
            cases++;
        }
        Check(samples.Count>=9&&cases>=72,"Late replay omitted motion variants or long-run stages");
        Console.WriteLine("  Replayed "+samples.Count+" late moving geometries across both screen bounds and four phases ("+cases+" cases).");
    }

    private static void FasterPacing()
    {
        Check(RunnerSession.InitialSpeed>=350&&RunnerSession.SpeedAcceleration>0&&RunnerSession.SpeedAcceleration<3,
            "Pace no longer starts briskly and accelerates gradually");
        RunnerSession run=New(); run.StartRun(); World(run).Clear(); StopSpawns(run);
        SetProperty(run,"Elapsed",600f); SetProperty(run,"Speed",1680f); run.Update(0.1f);
        Check(run.Speed>1680&&run.Speed>610,"Speed retained the old upper limit");
    }

    private static RunnerSession EmptyRun(District district=District.Day, int hearts=0)
    {
        RunnerSession run=New(123,hearts); run.SetStartingDistrict(district); run.StartRun();
        World(run).Clear(); StopSpawns(run); return run;
    }

    private static void MapShuffleBag()
    {
        foreach(District? selected in new District?[]{null,District.Day,District.Snow,District.Sunset,District.Night,District.Skyway,District.Storm})
        for(int seed=0;seed<4;seed++)
        {
            RunnerSession run=New(seed,randomStart:true); run.SetStartingDistrict(selected); run.StartRun();
            var sequence=new List<District>{run.CurrentDistrict};
            if(selected.HasValue) Check(run.CurrentDistrict==selected.Value,"Chosen map was not first");
            while(sequence.Count<18)
            {
                int stage=run.StageNumber; Advance(run,0.25f);
                if(stage!=run.StageNumber) sequence.Add(run.CurrentDistrict);
            }
            for(int i=1;i<sequence.Count;i++) Check(sequence[i]!=sequence[i-1],"Bag boundary repeated adjacent maps");
            for(int i=0;i<18;i+=6) Check(sequence.Skip(i).Take(6).Distinct().Count()==6,
                "Six-map bag repeated before all maps were visited: "+string.Join(",",sequence.Skip(i).Take(6)));
        }
    }

    private static void SnowInertia()
    {
        RunnerSession run=EmptyRun(District.Snow); SetProperty(run,"PlayerX",350f);
        run.SetHorizontal(1); Advance(run,0.35f,false);
        Check(run.HorizontalVelocity>200&&run.HorizontalVelocity<RunnerSession.HorizontalSpeed,"Snow accelerated instantly or never accelerated");
        float x=run.PlayerX,velocity=run.HorizontalVelocity; run.SetHorizontal(0); Advance(run,0.2f,false);
        Check(run.PlayerX>x+20&&run.HorizontalVelocity>0&&run.HorizontalVelocity<velocity,"Releasing snow input did not slide and slow");
        Advance(run,0.6f,false); Near(run.HorizontalVelocity,0,0.01f,"Snow drag never stopped sliding");
        run.SetHorizontal(-1); Advance(run,0.55f,false); float negative=run.HorizontalVelocity;
        run.SetHorizontal(1); Advance(run,0.15f,false);
        Check(negative<0&&run.HorizontalVelocity<0&&run.HorizontalVelocity>negative,"Snow reversal skipped braking");
        run.TogglePause(); x=run.PlayerX; velocity=run.HorizontalVelocity; run.Update(5);
        Near(run.PlayerX,x,0,"Paused snow slid"); Near(run.HorizontalVelocity,velocity,0,"Pause advanced friction");
        run.TogglePause(); Advance(run,0.1f,false); Check(run.PlayerX<x,"Resume lost existing snow momentum");
        run.SetHorizontal(-1); Advance(run,2,false);
        Near(run.PlayerX,RunnerSession.MinimumPlayerX,0.01f,"Sliding escaped screen bounds");
        Near(run.HorizontalVelocity,0,0.01f,"Snow retained velocity into screen edge");
        run.ReturnToMenu(); run.StartRun(); Near(run.HorizontalVelocity,0,0,"Restart retained snow velocity");
    }

    private static void SkyWind()
    {
        RunnerSession ground=EmptyRun(District.Skyway); SetProperty(ground,"PlayerX",450f); SetProperty(ground,"DistrictElapsed",5.5f);
        Check(ground.WindStrength>0.9f&&ground.WindPush<0,"Sky gust was absent");
        Advance(ground,0.2f,false); float groundLoss=450-ground.PlayerX;
        RunnerSession air=EmptyRun(District.Skyway); SetProperty(air,"PlayerX",450f); SetProperty(air,"DistrictElapsed",5.5f);
        air.Jump(); Advance(air,0.2f,false);
        Check(450-air.PlayerX>groundLoss*2,"Airborne wind was not stronger than grounded wind");
        float x=air.PlayerX; air.SetHorizontal(1); Advance(air,0.15f,false); Check(air.PlayerX>x,"Player cannot overcome wind");
        x=air.PlayerX;
        for(int i=0;i<60;i++){air.SetHorizontal(-air.WindPush/RunnerSession.HorizontalSpeed);air.Update(1f/120);}
        Near(air.PlayerX,x,0.03f,"Counter steering cannot hold position against live wind");
        air.TogglePause(); float wind=air.WindPush; air.Update(5); Near(air.PlayerX,x,0.03f,"Paused gust moved player");
        Near(air.WindPush,wind,0,"Paused wind clock advanced");
        air.ReturnToMenu(); air.SetStartingDistrict(District.Day); air.StartRun(); Near(air.WindPush,0,0,"Wind leaked into another map");
    }

    private static void SurvivalResources()
    {
        foreach(District district in new[]{District.Snow,District.Sunset})
        {
            RunnerSession run=EmptyRun(district); bool cold=district==District.Snow;
            Advance(run,1.9f,false); Near(cold?run.Warmth:run.Water,100,0,"Thermal opening grace missing");
            Advance(run,1.1f,false); Near(cold?run.Warmth:run.Water,cold?93:94,0.1f,"Wrong depletion rate after grace");
            Near(cold?run.Water:run.Warmth,100,0,"Inactive resource drained");
            SetProperty(run,cold?"Warmth":"Water",20f);
            Inject(run,cold?EntityKind.HotDrink:EntityKind.Water,10,32,36); run.Update(1f/120);
            Near(cold?run.Warmth:run.Water,cold?65:60,0.1f,"Matching supply did not replenish resource");
            for(int i=0;i<3;i++) Inject(run,cold?EntityKind.HotDrink:EntityKind.Water,10,32,36);
            run.Update(1f/120); Near(cold?run.Warmth:run.Water,100,0.1f,"Supply overflowed resource cap");
            run.TogglePause(); float resource=cold?run.Warmth:run.Water; run.Update(5);
            Near(cold?run.Warmth:run.Water,resource,0,"Paused survival depleted resource");
            run.TogglePause(); SetProperty(run,"DistrictElapsed",RunnerSession.DistrictDuration-0.01f); run.Update(0.02f);
            Near(run.Water,100,0,"Map entry did not restore water"); Near(run.Warmth,100,0,"Map entry did not restore warmth");
        }
    }

    private static void SurvivalDepletion()
    {
        foreach(District district in new[]{District.Snow,District.Sunset})
        {
            bool cold=district==District.Snow; RunnerSession run=EmptyRun(district,1);
            SetProperty(run,"DistrictElapsed",3f); SetProperty(run,cold?"Warmth":"Water",0.01f); run.Update(0.02f);
            Check(run.Lives==1&&run.State==RunState.Playing,"Depletion did not consume exactly one life");
            Near(cold?run.Warmth:run.Water,45,0.15f,"Survivor did not receive recovery supply");
            Advance(run,0.5f,false); Check(run.Lives==1,"Depletion repeatedly consumed hearts in adjacent frames");
            Advance(run,2f,false); SetProperty(run,cold?"Warmth":"Water",0.01f); run.Update(0.02f);
            Check(run.State==RunState.GameOver&&run.Lives==0&&run.Save.History.Count==1,"Fatal depletion failed to end the run once");
            Near(cold?run.Warmth:run.Water,0,0,"Fatal depletion falsely refilled resource");
            string end=Snapshot(run); run.Update(1); Check(Snapshot(run)==end,"Fatal survival continued simulation");
        }
    }

    private static void ProductionSupplyRuns()
    {
        int picked=0,spawned=0;
        foreach(District district in new[]{District.Snow,District.Sunset})
        for(int seed=0;seed<12;seed++)
        {
            RunnerSession run=New(seed); run.SetStartingDistrict(district); run.StartRun();
            var seen=new HashSet<int>(); float minimum=100,previous=100;
            for(int i=0;i<120*34;i++)
            {
                // Isolate supply cadence while preserving real drain, actual
                // pickup travel/contact, and random production supply placement.
                ClearHazards(run);
                foreach(WorldEntity item in run.Entities)
                    if(item.Kind==EntityKind.DoubleScore||item.Kind==EntityKind.Magnet||item.Kind==EntityKind.Shield) item.Removed=true;
                run.Update(1f/120);
                foreach(WorldEntity item in run.Entities.Where(e=>!e.Removed&&(e.Kind==EntityKind.Water||e.Kind==EntityKind.HotDrink)))
                {
                    if(!seen.Add(item.Id)) continue;
                    spawned++;
                    Check(item.Kind==(district==District.Snow?EntityKind.HotDrink:EntityKind.Water),"Wrong biome supply spawned");
                    Check(item.X>RunnerSession.MaximumPlayerX+90&&item.Y==10,"Supply spawned without an approach lane");
                    foreach(WorldEntity hazard in run.Entities.Where(e=>!e.Removed&&Hazard(e)))
                        if(hazard.Kind!=EntityKind.Plane&&!(hazard.Kind==EntityKind.Saw&&hazard.BaseY>=56))
                            Check(item.X+item.Width<=hazard.X-hazard.MotionAmplitudeX*2-90||item.X>=hazard.X+hazard.Width+hazard.MotionAmplitudeX*2+90,
                                "Supply spawned inside hazard envelope");
                }
                float resource=district==District.Snow?run.Warmth:run.Water;
                if(resource>previous+1) picked++;
                minimum=Math.Min(minimum,resource); previous=resource;
                Check(run.State==RunState.Playing&&run.Lives==1,"Production supply stream failed to sustain a grounded player on "+district+" seed="+seed);
            }
            Check(minimum>15&&seen.Count>=7,"Supply stream too sparse on "+district+" seed="+seed+" minimum="+minimum+" count="+seen.Count);
        }
        Check(picked>=120&&spawned>=160,"Supply audit covered too few real pickups");
        Console.WriteLine("  Real supply audit: "+spawned+" spawns, "+picked+" replenishments, 24 unrefilled 34-second biome runs.");
    }

    private static void LateSupplyApproach()
    {
        int cases=0; float slowest=0;
        MethodInfo spawn=typeof(RunnerSession).GetMethod("SpawnSurvivalSupply",BindingFlags.Instance|BindingFlags.NonPublic);
        foreach(PackSpec pack in LatePacks().Where(p=>p.District==District.Snow||p.District==District.Sunset))
        foreach(float left in new[]{650f,1000f})
        {
            RunnerSession run=EmptyRun(pack.District); SetProperty(run,"Speed",pack.Speed); SetProperty(run,"Elapsed",pack.SpawnTime);
            SetProperty(run,"PlayerX",RunnerSession.MaximumPlayerX);
            foreach(WorldEntity original in pack.Entities)
            {
                WorldEntity copy=CopyEntity(original); copy.X+=left; World(run).Add(copy);
            }
            // Exercise the production placement against real late packs at two
            // approach positions, including its behind-the-pack fallback lane.
            spawn.Invoke(run,new object[]{pack.District==District.Snow});
            WorldEntity supply=run.Entities.Single(e=>e.Kind==EntityKind.Water||e.Kind==EntityKind.HotDrink);
            foreach(WorldEntity hazard in run.Entities.Where(Hazard))
            {
                if(hazard.Kind==EntityKind.Plane||hazard.Kind==EntityKind.Saw&&hazard.BaseY>=56) continue;
                Check(supply.X+supply.Width<=hazard.X-hazard.MotionAmplitudeX*2-95||
                    supply.X>=hazard.X+hazard.Width+hazard.MotionAmplitudeX*2+95,"Late supply fallback overlaps hazard sweep");
            }
            float approach=(supply.X-run.PlayerX)/pack.Speed;
            Check(approach>0&&approach<7f,"Late supply cannot be reached before half-full warmth depletes");
            slowest=Math.Max(slowest,approach); cases++;
        }
        Check(cases>=100,"Late supply audit omitted real thermal pack variants");
        Console.WriteLine("  Late supply approach audit: "+cases+" production placements; longest scroll approach "+slowest.ToString("0.00")+"s.");
    }

    private static void SkyHoleDensity()
    {
        int total=0,holes=0,doubleGaps=0,collapse=0;
        foreach(float age in new[]{0f,90f})
        for(int seed=0;seed<12;seed++)
        {
            RunnerSession run=New(seed); run.SetStartingDistrict(District.Skyway); run.StartRun();
            SetProperty(run,"Elapsed",age); SetProperty(run,"Speed",RunnerSession.InitialSpeed+age*RunnerSession.SpeedAcceleration);
            var seen=new HashSet<int>();
            for(int i=0;i<120*34;i++)
            {
                foreach(var group in run.Entities.Where(e=>!e.Removed&&Hazard(e)).GroupBy(e=>e.PatternId))
                {
                    if(!seen.Add(group.Key)||run.Elapsed<10) continue;
                    total++; bool hole=group.Any(e=>e.Kind==EntityKind.Gap||e.Kind==EntityKind.CollapsingFloor);
                    if(hole) holes++; if(group.Count(e=>e.Kind==EntityKind.Gap)==2) doubleGaps++;
                    if(group.Any(e=>e.Kind==EntityKind.CollapsingFloor)) collapse++;
                }
                ClearHazards(run); run.Update(1f/120);
            }
        }
        Console.WriteLine("  Skyway hole families: "+holes+"/"+total+" packs; double gaps="+doubleGaps+", collapses="+collapse+".");
        Check(total>180&&holes>=total*0.5f,"Skyway hole families are less than half of generated packs");
        Check(doubleGaps>12&&collapse>12,"Skyway omitted double gaps or collapses");
    }

    private static void CollapsingFloors()
    {
        RunnerSession run=EmptyRun(District.Skyway,1); var bridge=Inject(run,EntityKind.CollapsingFloor,0,180,0); bridge.X=1300;
        Check(!bridge.IsOpenGap,"New bridge starts open"); float previous=0; bool cracked=false,opened=false;
        for(int i=0;i<120*6&&run.Lives==2;i++)
        {
            run.Update(1f/120);
            Check(bridge.CollapseProgress>=previous,"Bridge repaired itself while approaching"); previous=bridge.CollapseProgress;
            cracked|=previous>0&&previous<1; opened|=bridge.IsOpenGap;
        }
        Check(cracked&&opened&&bridge.Collapsed,"Bridge did not visibly crack before collapsing");
        Check(run.Lives==1&&run.State==RunState.Playing,"Collapsed bridge did not act as a real hole");
        Check(bridge.Removed,"Fall recovery left the collapsed floor underneath the player");
        Advance(run,2,false); Check(run.Lives==1&&run.PlayerY==0,"Recovered player fell repeatedly through collapsed bridge");
    }

    private static void ItemShop()
    {
        var run=new RunnerSession(new RunnerSave{Wallet=6000},42); int notifications=0; run.SaveChanged+=()=>notifications++;
        int start=run.Save.Wallet;
        foreach(int price in new[]{200,400,700,1100})
        {
            Check(run.NextScoreBoostCost==price&&run.NextMagnetCost==price,"Duration upgrade price changed");
            Check(run.BuyScoreBoostUpgrade()&&run.BuyMagnetUpgrade(),"Funded duration upgrade rejected");
        }
        Check(run.Save.Wallet==start-4800&&run.ScoreBoostDuration==16&&run.MagnetDuration==16,"Upgrade cost or duration incorrect");
        Check(!run.BuyScoreBoostUpgrade()&&!run.BuyMagnetUpgrade()&&run.NextScoreBoostCost==0&&run.NextMagnetCost==0,"Max upgrade exceeded cap");
        Check(run.BuyStartingShield()&&run.BuyWaterReserve()&&run.BuyWarmReserve(),"Funded consumable rejected");
        Check(run.Save.Wallet==700&&run.Save.PurchasedShield&&run.Save.PurchasedWaterReserve&&run.Save.PurchasedWarmReserve,"Consumable prices or inventory incorrect");
        Check(!run.BuyStartingShield()&&!run.BuyWaterReserve()&&!run.BuyWarmReserve(),"Consumables stacked more than one stock");
        run.StartRun();
        Check(run.ShieldReady&&run.WaterReserveReady&&run.WarmReserveReady&&!run.Save.PurchasedShield&&!run.Save.PurchasedWaterReserve&&!run.Save.PurchasedWarmReserve,
            "Starting inventory was not consumed exactly once");
        Check(!run.BuyScoreBoostUpgrade()&&!run.BuyStartingShield(),"Active run allowed shop purchase");
        run.ReturnToMenu(); run.StartRun();
        Check(!run.ShieldReady&&!run.WaterReserveReady&&!run.WarmReserveReady&&run.Save.ScoreBoostLevel==4&&run.Save.MagnetLevel==4,
            "Next run retained consumables or lost permanent duration upgrades");
        Check(notifications>=12,"Shop or consumption failed to notify persistence");
        var invalid=new RunnerSession(new RunnerSave{ScoreBoostLevel=999,MagnetLevel=-5},4);
        Check(invalid.Save.ScoreBoostLevel==4&&invalid.Save.MagnetLevel==0,"Saved duration levels were not sanitized");
        Check(!new RunnerSession(new RunnerSave{Wallet=149},2).BuyWarmReserve(),"Unfunded survival purchase succeeded");
    }

    private static void SurvivalReserves()
    {
        foreach(District district in new[]{District.Snow,District.Sunset})
        {
            bool cold=district==District.Snow;
            var run=new RunnerSession(new RunnerSave{PurchasedWarmReserve=cold,PurchasedWaterReserve=!cold,PurchasedHearts=1},31);
            run.SetStartingDistrict(district); run.StartRun(); World(run).Clear(); StopSpawns(run);
            SetProperty(run,"DistrictElapsed",3f); SetProperty(run,cold?"Warmth":"Water",0.01f); run.Update(0.02f);
            Check(run.Lives==2&&!(cold?run.WarmReserveReady:run.WaterReserveReady),"Reserve did not absorb one depletion");
            Near(cold?run.Warmth:run.Water,85,0.2f,"Reserve restored wrong resource amount");
            SetProperty(run,cold?"Warmth":"Water",0.01f); run.Update(0.02f);
            Check(run.Lives==1,"Spent reserve rescued a second depletion");
            run.ReturnToMenu(); run.StartRun();
            Check(!run.WarmReserveReady&&!run.WaterReserveReady,"Reserve became permanent");
        }
    }

    private static void DoubleScoreAccounting()
    {
        RunnerSession run=EmptyRun(); Advance(run,2,false); float unboosted=run.Distance;
        Inject(run,EntityKind.DoubleScore,10,30,30); run.Update(1f/120); float start=run.Distance;
        Inject(run,EntityKind.Star,10,24,24); run.Update(1f/120);
        Advance(run,1.2f,false); float boostedTravel=run.Distance-start;
        int expected=(int)Math.Floor((run.Distance+boostedTravel)/20f);
        Check(run.Score==expected,"Double score multiplied prior points or missed active travel");
        Check(run.Save.Wallet==1&&run.RunStars==1,"Double score multiplied wallet credits");
        Near(start-unboosted,run.Speed/120f,0.1f,"Pickup changed real travelled distance");
        // Cross timer expiry inside a single public Update, keeping distance at
        // double precision rather than multiplying the final displayed score.
        SetField(run,"scoreBoostRemainingClock",0.004d); double priorBonus=(double)typeof(RunnerSession).GetField("bonusScoreDistance",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        float before=run.Distance,speed=run.Speed; run.Update(0.02f);
        double afterBonus=(double)typeof(RunnerSession).GetField("bonusScoreDistance",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(run);
        Near((float)(afterBonus-priorBonus),speed*0.004f,0.002f,"Expiry frame received a full-frame bonus");
        Check(run.ScoreBoostRemaining==0&&run.Distance>before,"Expiry stopped actual travel");
        run.ReturnToMenu(); Check(run.Save.LastScore==run.Score&&run.Save.History[0].Score==run.Score,"Boosted result did not persist");
        run.StartRun(); Check(run.Score==0&&run.ScoreBoostRemaining==0,"Restart retained score bonus");
    }

    private static void TimedItemLifecycle()
    {
        RunnerSession run=EmptyRun();
        for(int i=0;i<4;i++){Inject(run,EntityKind.DoubleScore,10,30,30);Inject(run,EntityKind.Magnet,10,30,30);run.Update(1f/120);}
        Check(run.ScoreBoostRemaining>15.9f&&run.ScoreBoostRemaining<=16&&run.MagnetRemaining>15.9f&&run.MagnetRemaining<=16,
            "Repeated pickups exceeded twice-duration cap or failed to extend");
        run.TogglePause(); float score=run.ScoreBoostRemaining,magnet=run.MagnetRemaining; run.Update(20);
        Near(run.ScoreBoostRemaining,score,0,"Pause expired double score"); Near(run.MagnetRemaining,magnet,0,"Pause expired magnet");
        run.TogglePause(); Advance(run,16.1f,false);
        Check(run.ScoreBoostRemaining==0&&run.MagnetRemaining==0,"Timed pickup failed to expire");
    }

    private static void MagnetAttraction()
    {
        RunnerSession run=EmptyRun(); Inject(run,EntityKind.Magnet,10,30,30); run.Update(1f/120);
        WorldEntity star=Inject(run,EntityKind.Star,160,24,24); star.X=run.PlayerX+140;
        WorldEntity heart=Inject(run,EntityKind.Heart,160,30,30); heart.X=star.X;
        WorldEntity water=Inject(run,EntityKind.Water,160,32,36); water.X=star.X;
        WorldEntity far=Inject(run,EntityKind.Star,400,24,24); far.X=run.PlayerX+600;
        float y=star.Y,farY=far.Y; run.Update(0.02f);
        Check(star.Y<y,"Magnet did not pull nearby star vertically");
        Near(heart.Y,160,0,"Magnet attracted a heart"); Near(water.Y,160,0,"Magnet attracted water"); Near(far.Y,farY,0,"Magnet attracted a star outside its radius");
        Advance(run,0.3f,false); Check(star.Removed&&run.Save.Wallet==1,"Attracted star did not credit once");
        SetField(run,"magnetRemainingClock",0.004d);
        WorldEntity edge=Inject(run,EntityKind.Star,160,24,24); edge.X=run.PlayerX+140; float before=edge.Y;
        run.Update(0.02f);
        Check(before-edge.Y<10,"Magnet expiry pulled for an entire frame");
        before=edge.Y; run.Update(0.02f); Near(edge.Y,before,0,"Expired magnet continued attracting");
    }

    private static void ShieldDamage()
    {
        RunnerSession run=EmptyRun(District.Day,1); Inject(run,EntityKind.Shield,10,30,30); run.Update(1f/120);
        Inject(run,EntityKind.House); run.Update(1f/120);
        Check(run.Lives==2&&!run.ShieldReady&&run.InvincibleRemaining>0,"Shield did not absorb exactly one contact");
        Inject(run,EntityKind.Hydrant); run.Update(1f/120); Check(run.Lives==2,"Shield grace failed against same-frame contact");
        World(run).Clear(); Advance(run,1.1f,false); Inject(run,EntityKind.House); run.Update(1f/120);
        Check(run.Lives==1&&!run.ShieldReady,"Spent shield continued absorbing hits");
        RunnerSession fall=EmptyRun(); Inject(fall,EntityKind.Shield,10,30,30); fall.Update(1f/120); Inject(fall,EntityKind.Gap,0,600,0);
        Advance(fall,0.7f,false); Check(fall.State==RunState.GameOver&&fall.ShieldReady,"Physical shield incorrectly rescued falling");
    }

    private static void PowerupGeneration()
    {
        var kinds=new HashSet<EntityKind>(); int count=0;
        for(int seed=0;seed<12;seed++)
        {
            RunnerSession run=New(seed); run.StartRun(); var seen=new HashSet<int>(); float previous=-100;
            for(int i=0;i<120*95;i++)
            {
                IsolateWorld(run); run.Update(1f/120);
                foreach(WorldEntity item in run.Entities.Where(e=>!e.Removed&&(e.Kind==EntityKind.DoubleScore||e.Kind==EntityKind.Magnet||e.Kind==EntityKind.Shield)))
                {
                    if(!seen.Add(item.Id)) continue;
                    Check(run.Elapsed>=17.9f&&run.Elapsed-previous>=19.9f,"Powerups spawned too frequently"); previous=run.Elapsed;
                    Check(item.Y==110&&item.X>=800&&item.X+item.Width<=RunnerSession.CanvasWidth,"Powerup lacks a visible approach lane");
                    foreach(WorldEntity hazard in run.Entities.Where(e=>!e.Removed&&Hazard(e)))
                        Check(item.X+item.Width<=hazard.X-hazard.MotionAmplitudeX*2-63||item.X>=hazard.X+hazard.Width+hazard.MotionAmplitudeX*2+63,
                            "Powerup spawned inside swept hazard envelope");
                    count++; kinds.Add(item.Kind);
                }
            }
        }
        Check(kinds.Count==3&&count>=36,"Random powerup stream omitted a kind or useful cadence");
    }

    private static void SaveValidation()
    {
        var save = new RunnerSave { Wallet = -100, BestScore = -10, LastScore = -7, PurchasedHearts = 100, History = null };
        RunnerSession run = new RunnerSession(save, 1);
        Check(run.Save.Wallet >= 0 && run.Save.BestScore >= 0 && run.Save.LastScore >= 0, "Negative saved values remain");
        Check(run.Save.PurchasedHearts == 3 && run.Save.History != null, "Invalid upgrade/history not sanitized");
        var v2=new RunnerSession(new RunnerSave{Version=2,Wallet=43,BestScore=900,LastScore=500,
            PurchasedHearts=2,ExtraHeartsLevel=3,History=new List<RunRecord>{new RunRecord{Score=500,Distance=1234}}},1);
        Check(v2.Save.Version==3&&v2.Save.Wallet==43&&v2.Save.PurchasedHearts==2&&v2.Save.BestScore==900&&v2.Save.History.Count==1,
            "Version 2 migration refunded twice or lost consumable hearts/history");
        v2.StartRun(); Check(v2.Lives==3,"Migrated reserved hearts were not usable");
        RunnerSession fresh = new RunnerSession(null, 1); fresh.StartRun();
        Check(fresh.Lives == 1, "Missing save did not use safe defaults");
        var valid = new RunnerSave { Wallet = 43, BestScore = 900, LastScore = 500, PurchasedHearts = 1 };
        RunnerSession retained = new RunnerSession(valid, 1);
        Check(retained.Save.Wallet == 43 && retained.Save.BestScore == 900 && retained.Save.LastScore == 500 && retained.Save.PurchasedHearts == 1, "Valid progress was discarded");
    }

    private static void FrameStep()
    {
        RunnerSession a = New(), b = New(); a.StartRun(); b.StartRun();
        a.Update(0.2f); Advance(b, 0.2f);
        Near(a.Distance, b.Distance, 0.1f, "A long frame changed distance integration");
        Near(a.Elapsed, b.Elapsed, 0.01f, "A long frame lost elapsed time");
        a.TogglePause(); a.Update(float.NaN); a.Update(float.PositiveInfinity);
        Check(!float.IsNaN(a.Distance), "Invalid frame input corrupted simulation");
    }
}
