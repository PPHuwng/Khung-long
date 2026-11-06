using System;
using CityDino;

internal static partial class Program
{
    private static void RegisterV14JourneyTests()
    {
        Test("submarine victory swims to the waterline then climbs the shared bank without a sky drop", UnderwaterShoreRouteV14);
        Test("underwater shore departure freezes on pause and resumes the original scored run only after the gate", UnderwaterShoreStateV14);
    }

    private static void UnderwaterShoreRouteV14()
    {
        int routes = 0, bankSamples = 0, swimmingSamples = 0;
        foreach (CharacterKind character in Enum.GetValues(typeof(CharacterKind)))
        foreach (float startX in new[] { 100f, 260f, 720f })
        foreach (float startY in new[] { 20f, 145f, 260f })
        {
            var run = new RunnerSession(StockedSaveV10(), 314);
            string saved = SaveSnapshotV10(run.Save);
            Check(run.StartBossPractice(BossKind.Submarine, character, false, 8), "Diving trial rejected");
            Check(run.SkipPracticeIntro(), "Diving trial intro failed");
            SetProperty(run, "PlayerX", startX);
            SetProperty(run, "PlayerY", startY);
            run.CurrentBoss.Health = 0f;
            run.Update(1f / 120f);
            Check(run.ArenaPhase == BossPhase.Victory, "Submarine victory did not begin");
            float previousY = run.PlayerY, previousWorld = run.DepartureWorldX;
            float elapsed = run.Elapsed, distance = run.Distance, speed = run.Speed;
            float water = run.Water, warmth = run.Warmth;
            int score = run.Score, lives = run.Lives;
            bool swam = false, bank = false, crossed = false;
            for (int frame = 0; frame < 120 * 6 && run.State == RunState.Playing; frame++)
            {
                run.Update(1f / 120f);
                Check(run.Elapsed == elapsed && run.Distance == distance && run.Speed == speed &&
                    run.Score == score && run.Lives == lives && run.Water == water && run.Warmth == warmth,
                    "Swimming departure changed the suspended scored run");
                if (run.ArenaPhase != BossPhase.Victory) break;
                Check(run.DepartureWorldX >= previousWorld - .001f, "Diver reversed toward the entrance");
                Check(Math.Abs(run.PlayerY - previousY) < 8f, "Diver snapped vertically between cinematic frames");
                Near(run.PlayerX + run.BossExitCameraX, run.DepartureWorldX, .02f, "Cinematic camera detached from diver");
                if (run.ExitTravelProgress > 0f && !run.UnderwaterOnShore)
                {
                    float feet = RunnerSession.GroundY - run.PlayerY;
                    Check(feet > run.UnderwaterExitWaterlineY + 20f,
                        "Diver floated in the sky above the water before reaching the bank");
                    Check(run.DivingHelmetEquipped, "Diver removed helmet while still approaching through the water");
                    swam = true; swimmingSamples++;
                }
                if (run.UnderwaterOnShore && run.DepartureWorldX < RunnerSession.UnderwaterRampEndX)
                {
                    float expected = RunnerSession.UnderwaterShoreHeightAt(run.DepartureWorldX);
                    Near(run.PlayerY, expected, .001f, "Diver's feet are detached from the visible shore slope");
                    Check((previousWorld < RunnerSession.UnderwaterRampStartX || run.PlayerY >= previousY - .001f) && run.PlayerY <= 0f,
                        "Diver fell downward instead of walking up the shore: character=" + character + " start=" + startX + "," + startY + " world=" + run.DepartureWorldX + " previousWorld=" + previousWorld + " y=" + run.PlayerY + " previousY=" + previousY);
                    Check(run.WaterDepth == 0f && !run.DivingHelmetEquipped,
                        "Dry bank retained submerged camera or diving equipment");
                    bank = true; bankSamples++;
                }
                if (run.DepartureWorldX >= RunnerSession.BossExitGateX)
                {
                    crossed = true;
                    Near(run.PlayerY, 0f, .001f, "Diver reached the exit gate before completing the ramp");
                }
                previousY = run.PlayerY; previousWorld = run.DepartureWorldX;
            }
            Check(swam && bank && crossed && run.PracticeFinished && run.PracticeWon,
                "Diving victory omitted swimming, climbing or the forward exit");
            Check(SaveSnapshotV10(run.Save) == saved, "Cinematic training exit changed the saved inventory");
            routes++;
        }
        Check(routes == 27 && bankSamples > 100 && swimmingSamples > 100,
            "Shore route audit did not cover every character and edge/depth combination");
        Console.WriteLine("  Shared water/bank route checked " + routes + " real departures, " + bankSamples + " slope frames.");
    }

    private static void UnderwaterShoreStateV14()
    {
        RunnerSession run = EnterBossFixture(3, hearts: 2);
        ReachFighting(run);
        SetProperty(run, "PlayerX", 720f);
        SetProperty(run, "PlayerY", 260f);
        run.CurrentBoss.Health = 0f; run.Update(1f / 120f);
        float elapsed = run.Elapsed, distance = run.Distance, water = run.Water, warmth = run.Warmth;
        bool paused = false;
        for (int frame = 0; frame < 120 * 6 && run.IsBossArena; frame++)
        {
            if (!paused && run.UnderwaterOnShore && run.UnderwaterRampProgress < .8f)
            {
                float x = run.PlayerX, y = run.PlayerY, camera = run.BossExitCameraX, depth = run.WaterDepth;
                run.TogglePause(); run.Update(3f);
                Check(run.PlayerX == x && run.PlayerY == y && run.BossExitCameraX == camera && run.WaterDepth == depth,
                    "Pause advanced the swimmer or shore camera");
                run.TogglePause(); paused = true;
            }
            run.SetHorizontal(-1f); run.SetSwimLift(true); run.SetSwimDive(true);
            Check(!run.Jump() && !run.TryShoot() && !run.TryUseSkill(), "Shore cinematic accepted manual combat input");
            run.Update(1f / 120f);
            Check(run.Elapsed == elapsed && run.Distance == distance && run.Water == water && run.Warmth == warmth,
                "Shore cinematic advanced normal clocks or survival bars");
        }
        Check(paused && !run.IsBossArena && run.PlayerY == 0f && !run.DivingHelmetEquipped,
            "Shore departure never completed its pause-safe surface route");
        run.Update(.05f);
        Check(run.Elapsed > elapsed && run.Distance > distance, "Scored running did not resume after the far gate");
    }
}
