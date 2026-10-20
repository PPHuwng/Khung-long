using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityDino;

internal static class Program
{
    private static int passed;
    private static readonly List<string> failures = new List<string>();

    private static int Main()
    {
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
        Test("upgrades require funds and apply to the next run", Upgrade);
        Test("upgrade prices and maximum level are enforced", UpgradeCap);
        Test("collisions consume one life with invulnerability", Damage);
        Test("fatal collision records once and freezes gameplay", FatalCollision);
        Test("restart retains money and high score but resets the run", Restart);
        Test("fall recovery removes the gap and avoids repeated falling", FallRecovery);
        Test("fatal falling ends the run instead of falling forever", FatalFall);
        Test("hearts have a rare randomized spawn cadence", HeartCadence);
        Test("districts progress and skyway includes real gaps", Districts);
        Test("obstacles and gaps do not form impossible overlapping hazards", SpawnSafety);
        Test("save input is sanitized without discarding valid progress", SaveValidation);
        Test("long frame updates behave like fixed small steps", FrameStep);
        Console.WriteLine();
        Console.WriteLine("Gameplay core: " + passed + " passed, " + failures.Count + " failed.");
        foreach (string failure in failures) Console.Error.WriteLine(failure);
        Console.WriteLine("These checks execute production C# gameplay. Unity rendering/import was not executed.");
        return failures.Count == 0 ? 0 : 1;
    }

    private static void Test(string name, Action body)
    {
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
        return new RunnerSession(new RunnerSave { ExtraHeartsLevel = upgrade }, seed);
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
        return entity.Kind == EntityKind.House || entity.Kind == EntityKind.Hydrant || entity.Kind == EntityKind.Plane || entity.Kind == EntityKind.Gap;
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
        var entity = new WorldEntity { Id = 1000000 + World(run).Count, Kind = kind, X = 220 - width / 2, Y = y, Width = width, Height = height };
        World(run).Add(entity);
        return entity;
    }

    private static string Snapshot(RunnerSession run)
    {
        return string.Join("|", run.State, run.Distance, run.Elapsed, run.Score, run.Speed, run.PlayerY, run.VelocityY,
            run.Lives, run.IsDucking, run.CurrentDistrict, run.StageNumber,
            string.Join(";", run.Entities.Where(e => !e.Removed).Select(e => e.Kind + ":" + e.Variant + ":" + e.X + ":" + e.Y + ":" + e.Width)));
    }

    private static void MenuAndStart()
    {
        RunnerSession run = New(upgrade: 2);
        Check(run.State == RunState.Menu, "Initial state must be Menu");
        run.Update(5);
        Near(run.Distance, 0, 0, "Menu changed distance");
        Check(!run.Jump(), "Menu accepted jump");
        run.StartRun();
        Check(run.State == RunState.Playing && run.Lives == 3, "Start did not apply permanent extra lives");
        Check(run.Score == 0 && run.RunStars == 0 && run.PlayerY == 0, "New run was not clean");
        Near(run.Speed, 250, 0.01f, "Unexpected initial speed");
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
        while (obstacle.X - RunnerSession.PlayerX > 100 && run.State==RunState.Playing)
            run.Update(1f/120);
        Check(run.Jump(), "Could not jump for opening obstacle");
        for (int i=0; i<120 && obstacle.X+obstacle.Width>RunnerSession.PlayerX-50; i++)
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
        Near(run.Speed, 261, 0.1f, "Acceleration must be small and time based");
        Check(run.Score == (int)(run.Distance / 20), "Score does not follow distance");
        Advance(run, 590);
        Near(run.Speed, 430, 0.01f, "Speed did not cap");
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
        RunnerSession run = New();
        Check(!run.BuyExtraHeart() && run.Save.Wallet == 0 && run.Save.ExtraHeartsLevel == 0, "Insufficient funds changed progress");
        run.Save.Wallet = 40;
        Check(run.NextHeartCost == 40 && run.BuyExtraHeart(), "First upgrade rejected");
        Check(run.Save.Wallet == 0 && run.Save.ExtraHeartsLevel == 1, "Upgrade did not charge exact price");
        run.StartRun();
        Check(run.Lives == 2, "Upgrade did not add starting life");
        run.Save.Wallet = 100;
        Check(!run.BuyExtraHeart(), "Shop purchase allowed during run");
    }

    private static void UpgradeCap()
    {
        RunnerSession run = New(); run.Save.Wallet = 1000;
        foreach (int cost in new[] {40, 100, 180})
        {
            Check(run.NextHeartCost == cost, "Wrong next upgrade cost");
            Check(run.BuyExtraHeart(), "Funded upgrade failed");
        }
        Check(run.Save.Wallet == 680 && run.Save.ExtraHeartsLevel == 3, "Upgrade totals incorrect");
        Check(!run.BuyExtraHeart() && run.Save.Wallet == 680, "Upgrade maximum not enforced");
        run.StartRun(); Check(run.Lives == 4, "Maximum upgrade should start with four lives");
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

    private static void Districts()
    {
        RunnerSession run = New(99); run.StartRun();
        var districts = new HashSet<District>(); bool gap = false;
        for (int i = 0; i < 120 * 245; i++)
        {
            ClearHazards(run); run.Update(1f / 120); districts.Add(run.CurrentDistrict);
            if (run.Entities.Any(e => e.Kind == EntityKind.Gap && !e.Removed)) gap = true;
        }
        Check(districts.Count == 6, "Not all weather/difficulty districts were visited");
        Check(gap && run.StageNumber >= 6, "Skyway did not produce gaps/stage progression");
    }

    private static void SpawnSafety()
    {
        for (int seed = 0; seed < 12; seed++)
        {
            RunnerSession run = New(seed); run.StartRun();
            var seen = new HashSet<int>();
            float previousEnd = float.NegativeInfinity;
            for (int i = 0; i < 120 * 215; i++)
            {
                foreach (WorldEntity e in run.Entities)
                {
                    if (!e.Removed && Hazard(e) && seen.Add(e.Id))
                    {
                        float absoluteStart = e.X + run.Distance;
                        Check(absoluteStart - previousEnd >= 350, "Hazards lack recovery spacing for seed " + seed);
                        previousEnd = absoluteStart + e.Width;
                    }
                }
                ClearHazards(run); run.Update(1f / 120);
            }
            Check(seen.Count > 40, "Spacing check did not exercise enough generated hazards");
        }
    }

    private static void SaveValidation()
    {
        var save = new RunnerSave { Wallet = -100, BestScore = -10, LastScore = -7, ExtraHeartsLevel = 100, History = null };
        RunnerSession run = new RunnerSession(save, 1);
        Check(run.Save.Wallet >= 0 && run.Save.BestScore >= 0 && run.Save.LastScore >= 0, "Negative saved values remain");
        Check(run.Save.ExtraHeartsLevel == 3 && run.Save.History != null, "Invalid upgrade/history not sanitized");
        RunnerSession fresh = new RunnerSession(null, 1); fresh.StartRun();
        Check(fresh.Lives == 1, "Missing save did not use safe defaults");
        var valid = new RunnerSave { Wallet = 43, BestScore = 900, LastScore = 500, ExtraHeartsLevel = 1 };
        RunnerSession retained = new RunnerSession(valid, 1);
        Check(retained.Save.Wallet == 43 && retained.Save.BestScore == 900 && retained.Save.LastScore == 500 && retained.Save.ExtraHeartsLevel == 1, "Valid progress was discarded");
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
