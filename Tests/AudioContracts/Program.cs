using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityDino;
using UnityEngine;

internal static class Program
{
    private static int passed, failed;
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Main()
    {
        Test("every warning and projectile wave sounds once with a bounded mix", WaveIdentity);
        Test("active lasers, impacts and boss-created scenery emit once", BeamAndScenery);
        Test("muting and pausing never replay missed attack cues", Silence);
        Test("boss defeat sounds once per encounter and replay resets identities", Defeat);
        Test("production attacks include charge, missiles, electricity and bounded tracking", ProductionAudit);
        Test("movement sources follow boss type, pause, mute, focus and menu", ControllerLifecycle);
        Test("footsteps only play on real ground and aircraft approach", Footsteps);
        Test("generated effects contain bounded non-silent samples and distinct defeat bursts", Samples);
        Test("higher-tier repeated sweeps each emit one charge sound without replay on silent resume", RepeatedChargeIdentity);
        Test("running missile volleys sound once and never replay after silence or boss entry",RunMissileAudio);
        Test("underwater entry naval attacks defeat and surfacing have distinct bounded cues",UnderwaterCues);
        Test("submarine and breathing loops stop on pause mute and a completed swim exit",UnderwaterLoops);
        Test("warmed audio identity observers allocate no managed objects on stable live frames",TrackerAllocations);
        Console.WriteLine(passed + " passed; " + failed + " failed. Stub sound commands and samples only; no audio device or Unity mixer.");
        if (failed != 0) Environment.Exit(1);
    }
    private static void Test(string name, Action action)
    { try { action(); passed++; Console.WriteLine("PASS " + name); } catch (Exception e) { failed++; Console.WriteLine("FAIL " + name + ": " + (e.InnerException ?? e).Message); } }
    private static void Check(bool value, string message) { if (!value) throw new Exception(message); }
    private static RunnerSession Practice(BossKind kind = BossKind.Tank, bool skip = true)
    { var run = new RunnerSession(null, 81); run.StartBossPractice(kind, CharacterKind.Classic, false); if (skip) run.SkipPracticeIntro(); return run; }
    private static IList<BossProjectile> Shots(RunnerSession run) { return (IList<BossProjectile>)run.BossProjectiles; }
    private static IList<BossTelegraph> Marks(RunnerSession run) { return (IList<BossTelegraph>)run.BossTelegraphs; }
    private static IList<BossObstacle> Obstacles(RunnerSession run) { return (IList<BossObstacle>)run.BossObstacles; }
    private static object Get(object target, string field) { return target.GetType().GetField(field, Private).GetValue(target); }
    private static void Set(object target, string field, object value) { target.GetType().GetField(field, Private).SetValue(target, value); }
    private static void Property(object target, string name, object value) { target.GetType().GetProperty(name).SetValue(target, value); }
    private static object Call(object target, string method, params object[] args) { return target.GetType().GetMethod(method, Private).Invoke(target, args); }
    private static AudioSource Source(object target, string field) { return (AudioSource)Get(target, field); }
    private static int SoundCount(IReadOnlyList<BossSoundCue> cues, BossSoundKind kind) { return cues.Count(c => c.Kind == kind); }
    private static void WaveIdentity()
    {
        var run = Practice(); var sounds = new BossSoundTracker();
        Check(SoundCount(sounds.Observe(run, false), BossSoundKind.Windup) == 1, "Initial warning silent");
        Check(sounds.Observe(run, false).Count == 0, "Same warning repeated");
        for (int i = 0; i < 8; i++) Shots(run).Add(new BossProjectile { Id = 9000 + i, Kind = BossAttackKind.CrossMissile, VelocityX = i % 2 == 0 ? 320 : -320 });
        var cues = sounds.Observe(run, false);
        Check(SoundCount(cues, BossSoundKind.Missile) == 2 && cues.Any(c => c.Pan < 0) && cues.Any(c => c.Pan > 0), "Volley was not grouped per entry side");
        Check(sounds.Observe(run, false).Count == 0, "Live missiles sounded again");
        Shots(run).Clear(); Marks(run).Clear(); sounds.Observe(run, false);
        Check(sounds.TrackedCount == 0, "Expired identities accumulated");
    }
    private static void BeamAndScenery()
    {
        var run = Practice(); Marks(run).Clear(); var sounds = new BossSoundTracker();
        Marks(run).Add(new BossTelegraph { Id = 8001, Kind = BossAttackKind.LaserLow, Active = true });
        Check(SoundCount(sounds.Observe(run, false), BossSoundKind.Laser) == 1, "Active beam silent");
        Check(sounds.Observe(run, false).Count == 0, "Active beam repeated every frame");
        Marks(run).Add(new BossTelegraph { Id = 8002, Kind = BossAttackKind.MissileRain, Active = true });
        Check(SoundCount(sounds.Observe(run, false), BossSoundKind.Blast) == 1, "Rain ground explosion silent");
        Check(sounds.Observe(run, false).Count == 0, "Ground explosion replayed");
        Obstacles(run).Add(new BossObstacle { Id = 8003, Kind = BossAttackKind.CrateDrop });
        Obstacles(run).Add(new BossObstacle { Id = 8004, Kind = BossAttackKind.ElectricPillar });
        var cues = sounds.Observe(run, false);
        Check(SoundCount(cues, BossSoundKind.Impact) == 1 && SoundCount(cues, BossSoundKind.Electric) == 1, "Scenery spawn cues missing");
        Check(sounds.Observe(run, false).Count == 0, "Scenery sounded twice");
    }
    private static void Silence()
    {
        var run = Practice(); var sounds = new BossSoundTracker();
        Check(sounds.Observe(run, true).Count == 0 && sounds.Observe(run, false).Count == 0, "Unmuting replayed old warning");
        Shots(run).Add(new BossProjectile { Id = 7101, Kind = BossAttackKind.ElectricOrb });
        sounds.Observe(run, true);
        Check(sounds.Observe(run, false).Count == 0, "Muted projectile replayed");
        run.TogglePause(); Check(sounds.Observe(run, false).Count == 0, "Paused cue played");
        run.TogglePause(); Check(sounds.Observe(run, false).Count == 0, "Resume replayed old attack");
        Shots(run).Add(new BossProjectile { Id = 7102, Kind = BossAttackKind.ElectricOrb });
        Check(SoundCount(sounds.Observe(run, false), BossSoundKind.Electric) == 1, "Fresh unmuted attack silent");
    }
    private static void Defeat()
    {
        var run = Practice(); var sounds = new BossSoundTracker(); sounds.Observe(run, false);
        run.CurrentBoss.Health = 0; run.Update(.01f);
        Check(SoundCount(sounds.Observe(run, false), BossSoundKind.Defeat) == 1, "Defeat cue missing");
        Check(sounds.Observe(run, false).Count == 0, "Defeat replayed on next frame");
        run.TogglePause(); sounds.Observe(run, false); run.TogglePause();
        Check(sounds.Observe(run, false).Count == 0, "Defeat replayed after resume");
        run.ReturnToMenu(); sounds.Observe(run, false); run.StartBossPractice(BossKind.Tank, CharacterKind.Classic, false); run.SkipPracticeIntro();
        Check(SoundCount(sounds.Observe(run, false), BossSoundKind.Windup) == 1, "Replay identity reset failed");
        run.CurrentBoss.Health = 0; run.Update(.01f);
        Check(SoundCount(sounds.Observe(run, false), BossSoundKind.Defeat) == 1, "New encounter reused old victory latch");
    }
    private static void ProductionAudit()
    {
        // Explicit output-only audit: protect the observer to hear complete
        // production cycles. This is not survival or boss-win evidence.
        var heard = new HashSet<BossSoundKind>();
        foreach (BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            var run = Practice(kind); run.CurrentBoss.Health = run.CurrentBoss.MaxHealth * .2f; Property(run, "InvincibleRemaining", 1000f);
            var sounds = new BossSoundTracker();
            for (int i = 0; i < 120 * 120; i++)
            {
                foreach (var cue in sounds.Observe(run, false)) heard.Add(cue.Kind);
                Check(sounds.TrackedCount < 70, "Long encounter leaked identity records"); run.Update(1f / 120);
            }
        }
        foreach (var kind in new[] { BossSoundKind.Windup, BossSoundKind.Cannon, BossSoundKind.Missile, BossSoundKind.Electric, BossSoundKind.Laser, BossSoundKind.Charge, BossSoundKind.Blast })
            Check(heard.Contains(kind), "Actual attack never emitted " + kind);
    }
    private static CityRunnerController Controller(RunnerSession run)
    {
        var controller = new CityRunnerController { gameObject = new GameObject("Audio contract") };
        Set(controller, "session", run); Set(controller, "audioSource", new AudioSource()); Call(controller, "InitializeBossAudio"); return controller;
    }
    private static void ControllerLifecycle()
    {
        foreach (BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            var run = Practice(kind); var controller = Controller(run); Call(controller, "UpdateBossAudio");
            AudioSource movement = Source(controller, "bossMovement");
            Check(movement.isPlaying && movement.LoopStarts.Count == 1, "Boss movement loop missing");
            Call(controller, "UpdateBossAudio"); Check(movement.LoopStarts.Count == 1, "Movement loop restarted every frame");
            Call(controller, "ToggleSound"); Check(!movement.isPlaying && !Source(controller, "bossCentre").isPlaying, "Mute did not stop immediately");
            Call(controller, "UpdateBossAudio"); Check(!movement.isPlaying, "Muted loop restarted");
            Call(controller, "ToggleSound"); Call(controller, "UpdateBossAudio"); Check(movement.isPlaying, "Unmute failed to restore movement");
            Call(controller, "OnApplicationFocus", false); Check(run.State == RunState.Paused && !movement.isPlaying, "Background focus did not stop sound immediately");
            run.TogglePause(); Call(controller, "UpdateBossAudio");
            run.CurrentBoss.Health = 0; run.Update(.01f); Call(controller, "UpdateBossAudio");
            AudioSource centre = Source(controller, "bossCentre");
            Check(!movement.isPlaying && centre.OneShots.Count(x => x.Contains("Defeat")) == 1, "Defeat failed to stop boss engine/play explosion");
            Call(controller, "UpdateBossAudio"); Check(centre.OneShots.Count(x => x.Contains("Defeat")) == 1, "Controller replayed defeat");
            Call(controller, "GoToMenu"); Check(!centre.isPlaying && !Source(controller, "boardMovement").isPlaying, "Menu retained playing sounds");
            Call(controller, "DisposeBossAudio"); Check(Get(controller, "bossClips") == null && Get(controller, "bossMovement") == null, "Audio cleanup retained owned references");
        }
    }
    private static void Footsteps()
    {
        var run = Practice(BossKind.Gunship, false); var controller = Controller(run); Call(controller, "UpdateBossAudio");
        AudioSource centre = Source(controller, "bossCentre"); Check(centre.OneShots.Any(x => x.Contains("Footstep")), "Aircraft approach had no footsteps");
        var normal = new RunnerSession(null, 1); normal.SetStartingDistrict(District.Day); normal.StartRun(); controller = Controller(normal);
        Property(normal, "PlayerY", -20f); Call(controller, "UpdateBossAudio");
        Check(!Source(controller, "bossCentre").OneShots.Any(x => x.Contains("Footstep")), "Falling player sounded like walking");
        var ground = Practice(); controller = Controller(ground); Call(controller, "UpdateBossAudio");
        Check(!Source(controller, "bossCentre").OneShots.Any(x => x.Contains("Footstep")), "Stationary arena player made footsteps");
        ground.SetHorizontal(1f); ground.Update(.05f); Call(controller, "UpdateBossAudio");
        Check(Source(controller, "bossCentre").OneShots.Any(x => x.Contains("Footstep")), "Actual arena movement had no footsteps");
        // The victorious actor walks through the gate behind the boss. The
        // explosion hold is still motionless and must not make footsteps.
        ground=Practice();controller=Controller(ground);Call(controller,"UpdateBossAudio");
        ground.CurrentBoss.Health=0;ground.Update(.01f);Call(controller,"UpdateBossAudio");
        Source(controller,"bossCentre").OneShots.Clear();
        for(int i=0;i<60;i++){ground.Update(1f/120);Call(controller,"UpdateBossAudio");}
        Check(!Source(controller,"bossCentre").OneShots.Any(x=>x.Contains("Footstep")),"Explosion hold made walking footsteps before departure");
        for(int i=0;i<240;i++){ground.Update(1f/120);Call(controller,"UpdateBossAudio");}
        Check(ground.ExitTravelProgress>0&&Source(controller,"bossCentre").OneShots.Any(x=>x.Contains("Footstep")),"Actual forward gate traversal has no walking sound");
    }
    private static void Samples()
    {
        var controller = Controller(Practice()); var clips = (AudioClip[])Get(controller, "bossClips");
        Check(clips.Length == Enum.GetValues(typeof(BossSoundKind)).Length, "Missing sound variant");
        foreach (var clip in clips)
        {
            Check(clip.frequency == 22050 && clip.channels == 1 && clip.Samples.Length > 1000, "Wrong audio shape");
            Check(clip.Samples.All(v => !float.IsNaN(v) && Math.Abs(v) <= .8f) && clip.Samples.Any(v => Math.Abs(v) > .01f), "Clipped, non-finite or silent synthesis");
        }
        AudioClip defeat = clips[(int)BossSoundKind.Defeat];
        Check(defeat.Samples.Length > 22050, "Defeat did not contain a full multi-burst tail");
        foreach (float time in new[] { .03f, .26f, .59f, .95f })
        { int offset = (int)(time * defeat.frequency); Check(defeat.Samples.Skip(offset).Take(300).Any(v => Math.Abs(v) > .02f), "Defeat burst missing"); }
    }
    private static void RepeatedChargeIdentity()
    {
        // Output-only audio audit: protection lets the observer hear every cast;
        // this does not claim survival or an easy path at this danger tier.
        var run=new RunnerSession(null,441);run.StartBossPractice(BossKind.Tank,CharacterKind.Classic,false,5);run.SkipPracticeIntro();
        run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*.2f;Property(run,"InvincibleRemaining",1000f);
        var tracker=new BossSoundTracker();var seen=new HashSet<int>();int heard=0;
        for(int i=0;i<120*95;i++)
        {
            BossEncounter boss=run.CurrentBoss;bool first=boss.ContactActive&&seen.Add(boss.ChargeSourceTelegraphId);
            var cues=tracker.Observe(run,false);int chargeCues=SoundCount(cues,BossSoundKind.Charge);heard+=chargeCues;
            Check(chargeCues==(first?1:0),"Repeated charge was silent or sounded more than once for its source ID");
            Check(SoundCount(tracker.Observe(run,false),BossSoundKind.Charge)==0,"Second observation replayed the same active sweep");
            Check(tracker.TrackedCount<100,"Repeated high-tier waves leaked audio identity records");run.Update(1f/120);
        }
        Check(seen.Count>=3&&heard==seen.Count,"High-tier repeated casts were not heard as distinct actions");
        // One muted action is consumed silently, then the next new source may play.
        run.CurrentBoss.ContactActive=true;run.CurrentBoss.ChargeSourceTelegraphId=900001;
        Check(SoundCount(tracker.Observe(run,true),BossSoundKind.Charge)==0&&
            SoundCount(tracker.Observe(run,false),BossSoundKind.Charge)==0,"Unmuting replayed a missed sweep");
        run.CurrentBoss.ChargeSourceTelegraphId=900002;
        Check(SoundCount(tracker.Observe(run,false),BossSoundKind.Charge)==1&&
            SoundCount(tracker.Observe(run,false),BossSoundKind.Charge)==0,"A fresh source did not re-arm exactly once");
    }

    private static void RunMissileAudio()
    {
        var run=new RunnerSession(null,81);run.SetStartingDistrict(District.Day);run.StartRun();
        var warnings=(IList<RunMissileWarning>)run.RunMissileWarnings;var missiles=(IList<RunMissile>)run.RunMissiles;
        warnings.Clear();missiles.Clear();var tracker=new RunMissileSoundTracker();
        warnings.Add(new RunMissileWarning{Id=99001,VolleyId=71,Count=4,Remaining=1.2f,Duration=1.2f});
        Check(SoundCount(tracker.Observe(run,false),BossSoundKind.Windup)==1&&tracker.Observe(run,false).Count==0,"Normal volley warning replayed or was silent");
        warnings.Clear();for(int i=0;i<4;i++)missiles.Add(new RunMissile{Id=99002+i,VolleyId=71,VelocityX=-500});
        Check(SoundCount(tracker.Observe(run,false),BossSoundKind.Missile)==1&&tracker.Observe(run,false).Count==0,"Four simultaneous rockets stacked or replayed launch sounds");
        missiles.Clear();tracker.Observe(run,false);Check(tracker.TrackedCount==0,"Expired normal missile IDs accumulate");
        warnings.Add(new RunMissileWarning{Id=99009,VolleyId=72,Count=2});
        Check(tracker.Observe(run,true).Count==0&&tracker.Observe(run,false).Count==0,"Unmuting replayed a consumed normal warning");
        warnings.Clear();missiles.Add(new RunMissile{Id=99010,VolleyId=72,VelocityX=-500});run.TogglePause();
        Check(tracker.Observe(run,false).Count==0,"Paused normal volley made launch sound");run.TogglePause();
        Check(tracker.Observe(run,false).Count==0,"Resuming replayed an already observed launch");
        run.ReturnToMenu();run.StartBossPractice(BossKind.Submarine,CharacterKind.Classic,false);tracker.Observe(run,false);
        Check(tracker.TrackedCount==0,"Normal missile sounds leaked into boss transition");
    }
    private static void UnderwaterCues()
    {
        var run=Practice(BossKind.Submarine,false);var tracker=new BossSoundTracker();int splash=0,defeat=0;
        for(int i=0;i<120*5&&run.ArenaPhase==BossPhase.Intro;i++)
        {splash+=SoundCount(tracker.Observe(run,false),BossSoundKind.Splash);run.Update(1f/120);}
        Check(run.SwimControlsEnabled&&splash==1,"Dive did not play precisely one entry splash");
        Property(run,"InvincibleRemaining",1000f);Marks(run).Clear();Shots(run).Clear();Obstacles(run).Clear();tracker.Observe(run,false);
        Marks(run).Add(new BossTelegraph{Id=90001,Kind=BossAttackKind.SonarBeam,Active=true});
        Shots(run).Add(new BossProjectile{Id=90002,Kind=BossAttackKind.Torpedo,VelocityX=-300});
        for(int i=0;i<6;i++)Shots(run).Add(new BossProjectile{Id=90003+i,Kind=BossAttackKind.FishShoal,VelocityX=-300});
        Obstacles(run).Add(new BossObstacle{Id=90009,Kind=BossAttackKind.SeaMine,Active=true});
        var cues=tracker.Observe(run,false);
        Check(SoundCount(cues,BossSoundKind.Sonar)==1&&SoundCount(cues,BossSoundKind.Torpedo)==1&&SoundCount(cues,BossSoundKind.Swarm)==1&&SoundCount(cues,BossSoundKind.Splash)==1,"Naval attacks lack distinct sonar torpedo fish and mine cues");
        Check(tracker.Observe(run,false).Count==0,"Live naval objects repeated their audio");
        run.CurrentBoss.Health=0;run.Update(.01f);
        for(int i=0;i<120*5&&run.State==RunState.Playing;i++)
        {cues=tracker.Observe(run,false);defeat+=SoundCount(cues,BossSoundKind.UnderwaterDefeat);splash+=SoundCount(cues,BossSoundKind.Splash);run.Update(1f/120);}
        Check(defeat==1&&splash==2,"Underwater victory or surfacing sounded repeatedly or was missing");
        Check(tracker.TrackedCount<100,"Underwater audio identities are unbounded");
    }
    private static void UnderwaterLoops()
    {
        var run=Practice(BossKind.Submarine);var controller=Controller(run);Call(controller,"UpdateBossAudio");
        Check(Source(controller,"bossMovement").clip.name.Contains("Submarine")&&Source(controller,"bossMovement").isPlaying,"Submarine uses the wrong engine loop");
        Check(Source(controller,"boardMovement").clip==(AudioClip)Get(controller,"breathingLoop")&&Source(controller,"boardMovement").isPlaying,"Equipped diver has no breathing loop");
        run.TogglePause();Call(controller,"UpdateBossAudio");Check(!Source(controller,"bossMovement").isPlaying&&!Source(controller,"boardMovement").isPlaying,"Pause retained underwater loops");
        run.TogglePause();Call(controller,"UpdateBossAudio");Call(controller,"ToggleSound");Check(!Source(controller,"bossMovement").isPlaying&&!Source(controller,"boardMovement").isPlaying,"Mute retained breathing or submarine engine");
        Call(controller,"ToggleSound");run.CurrentBoss.Health=0;run.Update(.01f);Call(controller,"UpdateBossAudio");
        for(int i=0;i<120*5&&run.State==RunState.Playing;i++){run.Update(1f/120);Call(controller,"UpdateBossAudio");}
        Check(!Source(controller,"bossMovement").isPlaying&&!Source(controller,"boardMovement").isPlaying,"Completed dive retained an underwater loop on the street/results");
        Call(controller,"DisposeBossAudio");
    }

    private static void TrackerAllocations()
    {
        var bossRun=Practice();Marks(bossRun).Clear();Shots(bossRun).Clear();Obstacles(bossRun).Clear();
        Marks(bossRun).Add(new BossTelegraph{Id=91001,Kind=BossAttackKind.CrossMissile,Duration=.6f,Remaining=.6f});
        Marks(bossRun).Add(new BossTelegraph{Id=91002,Kind=BossAttackKind.LaserLow,Active=true});
        Shots(bossRun).Add(new BossProjectile{Id=91003,Kind=BossAttackKind.CrossMissile,VelocityX=-400});
        Obstacles(bossRun).Add(new BossObstacle{Id=91004,Kind=BossAttackKind.ShieldDrone,Active=true});
        var bossTracker=new BossSoundTracker();

        var roadRun=new RunnerSession(null,81);roadRun.SetStartingDistrict(District.Day);roadRun.StartRun();
        ((IList<RunMissileWarning>)roadRun.RunMissileWarnings).Add(new RunMissileWarning{Id=92001,VolleyId=71,Count=4});
        ((IList<RunMissile>)roadRun.RunMissiles).Add(new RunMissile{Id=92002,VolleyId=72,VelocityX=-530});
        var roadTracker=new RunMissileSoundTracker();
        // Exclude construction, initial capacity growth and JIT warmup. These
        // calls observe real production lists without invoking stub draw/audio
        // recording, reflection, or the simulation's intentional spawn objects.
        for(int i=0;i<1000;i++){bossTracker.Observe(bossRun,false);roadTracker.Observe(roadRun,false);}
        Check(bossTracker.TrackedCount==4&&roadTracker.TrackedCount==2,"Allocation fixture omitted a live audio identity category");
        long before=GC.GetAllocatedBytesForCurrentThread();
        for(int i=0;i<10000;i++)
        {
            bossTracker.Observe(bossRun,(i&1)==0);
            roadTracker.Observe(roadRun,(i&1)==0);
        }
        long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
        Check(bytes==0,"Stable observers allocated "+bytes+" bytes over 10,000 paired frames");
        Console.WriteLine("  Audio event observers: "+bytes+" managed bytes over 10,000 warmed boss + road frames.");
    }

}
