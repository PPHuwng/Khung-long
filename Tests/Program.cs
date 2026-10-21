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
        Test("speed rises gradually, caps, and score follows distance", Progression);
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
        Test("night and storm blackouts are brief, warned, and limited to their maps", BlackoutTimeline);
        Test("blackouts freeze on pause and reset between maps and runs", BlackoutStateTransitions);
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
        Test("saw hits consume one life and respect temporary invulnerability", SawDamage);
        Test("fatal saw contact freezes the exact visible animation and crouch pose", SawFatalPose);
        Test("saws rotate and floating blades leave a safe duck lane", SawMotion);
        Test("moving hazards oscillate without drift and pause or restart cleanly", HazardMotion);
        Test("moving hazards use their live position for contact without spending stars", HazardMotionCollision);
        Test("vertical hazard motion preserves a safe duck lane through every phase", MovingDuckLane);
        Test("moving hazard integration is independent of frame partition", MovingFrameStep);
        Test("random packs cover all hazard types and composite rhythms", PatternVariety);
        Test("sampled generated packs have a real jump or duck solution", PatternReplay);
        Test("faster start and speed ceiling exceed the previous version", FasterPacing);
        Test("seeded full runs can traverse successive packs without taking damage", SequentialRunner);
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

    private static RunnerSession New(int seed = 123, int upgrade = 0)
    {
        return new RunnerSession(new RunnerSave { PurchasedHearts = upgrade }, seed);
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
        return entity.Kind != EntityKind.Star && entity.Kind != EntityKind.Heart;
    }

    private static void ClearHazards(RunnerSession run)
    {
        foreach (WorldEntity e in run.Entities) if (Hazard(e)) e.Removed = true;
    }

    private static void Advance(RunnerSession run, float seconds, bool clearHazards = true)
    {
        int steps = (int)Math.Ceiling(seconds / (1f / 120f));
        float dt = seconds / steps;
        for (int n = 0; n < steps; n++)
        {
            if (clearHazards) ClearHazards(run);
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
        RunnerSession run = New(); run.StartRun();
        Advance(run, 10);
        Near(run.Speed, RunnerSession.InitialSpeed + 10 * RunnerSession.SpeedAcceleration, 0.1f, "Acceleration must be small and time based");
        Check(run.Score == (int)(run.Distance / 20), "Score does not follow distance");
        Advance(run, 590);
        Near(run.Speed, RunnerSession.MaximumSpeed, 0.01f, "Speed did not cap");
        Check(run.State == RunState.Playing && run.Score > 10000, "Long run did not progress");
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
        run.Save.Wallet=25; int saved=0; run.SaveChanged+=()=>saved++;
        Check(run.NextHeartCost==25 && run.BuyExtraHeart(),"Funded heart purchase rejected");
        Check(run.Save.Wallet==0 && run.Save.PurchasedHearts==1 && saved==1,"Heart purchase not persisted at exact price");
        run.StartRun();
        Check(run.Lives==2 && run.Save.PurchasedHearts==0 && saved==2,"Starting did not consume and persist the reserved heart");
        run.Save.Wallet=100;
        Check(!run.BuyExtraHeart(),"Shop allowed purchase while playing");
        run.TogglePause(); Check(!run.BuyExtraHeart(),"Shop allowed purchase while paused");
    }

    private static void UpgradeCap()
    {
        RunnerSession run=New(); run.Save.Wallet=1000;
        for(int i=0;i<3;i++) Check(run.NextHeartCost==25 && run.BuyExtraHeart(),"Repeatable heart purchase failed");
        Check(run.Save.Wallet==925 && run.Save.PurchasedHearts==3,"Heart total/price incorrect");
        Check(!run.BuyExtraHeart() && run.NextHeartCost==0 && run.Save.Wallet==925,"Reserved-heart cap was not enforced");
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
            Check(migrated.Save.Version==2 && migrated.Save.ExtraHeartsLevel==0,"Legacy upgrade not migrated");
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
            ClearHazards(run); run.Update(1f / 120);
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
        RunnerSession run = New(99);
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
            RunnerSession run=New(seed); run.StartRun(); seeds.Add(run.CurrentDistrict);
        }
        Check(seeds.Count == 6, "Random starting map omitted one or more districts across seeds");
        RunnerSession repeat=New(227);
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
                ClearHazards(run); run.Update(1f/120); visited.Add(run.CurrentDistrict);
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
            bool darkMap=district==District.Night || district==District.Storm;
            float firstDark=-1,firstWarning=-1,full=0,block=0,peak=0;
            int completed=0; bool partial=false;
            starts.Add((int)(run.NextBlackoutAt*100)); durations.Add((int)(run.BlackoutDuration*100));
            for(int i=0;i<120*34;i++)
            {
                ClearHazards(run); run.Update(dt); float amount=run.DarknessAmount;
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
        foreach (District district in new[] {District.Night,District.Storm})
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

    private static void SkywayOpening()
    {
        for (int seed=0; seed<6; seed++)
        {
            RunnerSession run=New(seed); run.SetStartingDistrict(District.Skyway); run.StartRun();
            bool gap=false;
            for (int i=0; i<120*34; i++)
            {
                ClearHazards(run); run.Update(1f/120);
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
            MotionAmplitudeX = source.MotionAmplitudeX, MotionSpeed = source.MotionSpeed
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
                ClearHazards(run); run.Update(1f / 120);
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
                    Check(pack.Entities.Count == 1, "A ground obstacle covers a gap in the same pack");
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
        World(airborne).Add(BladeAt(46,120)); airborne.Update(1f/120);
        Check(airborne.State==RunState.GameOver,"Production airborne collision used the wrong pose or height");
        RunnerSession duck=New(); duck.StartRun(); World(duck).Clear(); duck.SetDucking(true);
        World(duck).Add(BladeAt(33,38)); duck.Update(1f/120);
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

    private static void SawFatalPose()
    {
        foreach (bool duck in new[] {false,true})
        {
            RunnerSession run=New(); run.StartRun(); World(run).Clear();
            SetProperty(run,"Elapsed",0.3f); SetProperty(run,"FacingLeft",true);
            run.SetDucking(duck);
            World(run).Add(duck ? BladeAt(-33,38,8) : BladeAt(-5,35,12));
            run.Update(1f/120);
            Check(run.State==RunState.GameOver,"Fatal pose fixture did not contact a solid body region");
            Check(run.IsDucking==duck && run.FacingLeft,"Fatal contact changed crouch or facing pose");
            int frame=RunnerGeometry.GetPlayerFrame(run.Elapsed,run.Speed,run.PlayerY,run.State);
            int contactFrame=RunnerGeometry.GetPlayerFrame(run.Elapsed,run.Speed,run.PlayerY,RunState.Playing);
            float bob=RunnerGeometry.GetPlayerBob(run.Elapsed,run.PlayerY,run.IsDucking,run.State);
            float contactBob=RunnerGeometry.GetPlayerBob(run.Elapsed,run.PlayerY,run.IsDucking,RunState.Playing);
            Check(frame==contactFrame,"Fatal contact reset the displayed animation to a different frame");
            Near(bob,contactBob,0,"Fatal contact reset the visible running bob");
            run.Update(10);
            Check(RunnerGeometry.GetPlayerFrame(run.Elapsed,run.Speed,run.PlayerY,run.State)==frame,
                "Dead animation continued advancing");
            Near(RunnerGeometry.GetPlayerBob(run.Elapsed,run.PlayerY,run.IsDucking,run.State),bob,0,
                "Dead bob continued advancing");
            run.ReturnToMenu(); run.StartRun();
            Check(!run.IsDucking && !run.FacingLeft,"New run retained the fatal pose");
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
    { SetField(run,"distanceToPattern",1000000f); SetField(run,"nextHeartAt",float.MaxValue); }

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
            Math.Ceiling(e.X/10) + ":" + Math.Ceiling(e.Width/10) + ":" + Math.Ceiling(e.Height/10) + ":" + e.BaseY +
            ":motion=" + e.MotionAmplitudeX + "," + e.MotionAmplitude + "," + e.MotionSpeed));
    }

    private static bool ReplayPack(PackSpec pack, float speed, float playerX, float lead, float secondJumpDelay, float phaseShift=0)
    {
        RunnerSession run = New(991); run.StartRun(); World(run).Clear();
        SetProperty(run, "Elapsed", (speed-RunnerSession.InitialSpeed)/RunnerSession.SpeedAcceleration);
        SetProperty(run, "Speed", speed); SetProperty(run, "PlayerX", playerX);
        SetField(run, "distanceToPattern", 1000000f); SetField(run, "nextHeartAt", float.MaxValue);
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
            foreach (float speed in new[] {RunnerSession.InitialSpeed, RunnerSession.MaximumSpeed})
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
        Console.WriteLine("  Replayed " + samples.Count + " sampled geometries at both speed limits and both screen bounds.");
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
                            float approach=Math.Min(RunnerSession.MaximumSpeed,run.Speed+arrival*RunnerSession.SpeedAcceleration);
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
                    int before=run.Lives;
                    run.Update(1f/120);
                    Check(run.State==RunState.Playing && run.Lives>=before,
                        "Sequence bot hit a hazard for seed "+seed+" X="+x+" at "+
                        run.Elapsed.ToString("0.00")+"s pack="+current+" Y="+run.PlayerY+" VY="+run.VelocityY+
                        " jumped="+jumped+" double="+second+" lead="+jumpLead+" plan="+description+" since="+(run.Elapsed-jumpTime)+
                        " hazards="+string.Join(";",run.Entities.Where(e=>Hazard(e)&&e.X<run.PlayerX+180&&e.X+e.Width>run.PlayerX-100)
                            .Select(e=>e.PatternId+":"+e.Kind+"@"+e.X+","+e.Y+" size="+e.Width+","+e.Height))+" reason="+run.EndReason);
                }
                Check(solved>=70 && run.Score>4000 && run.Speed==RunnerSession.MaximumSpeed,
                    "Sequential run did not cover enough authored packs and capped-speed play");
            }
    }

    private static void FasterPacing()
    {
        Check(RunnerSession.InitialSpeed>=350 && RunnerSession.InitialSpeed>250*1.3f,
            "Starting pace was not materially faster than the previous version");
        Check(RunnerSession.MaximumSpeed>430 && RunnerSession.MaximumSpeed<RunnerSession.InitialSpeed*2,
            "Top speed did not increase with a controlled ceiling");
    }

    private static void SaveValidation()
    {
        var save = new RunnerSave { Wallet = -100, BestScore = -10, LastScore = -7, PurchasedHearts = 100, History = null };
        RunnerSession run = new RunnerSession(save, 1);
        Check(run.Save.Wallet >= 0 && run.Save.BestScore >= 0 && run.Save.LastScore >= 0, "Negative saved values remain");
        Check(run.Save.PurchasedHearts == 3 && run.Save.History != null, "Invalid upgrade/history not sanitized");
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
