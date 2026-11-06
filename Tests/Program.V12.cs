using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityDino;

internal static partial class Program
{
    private static void RegisterV12Tests()
    {
        Test("successive boss rounds gain real tiers beyond the previous health ceiling", BossTierProgressV12);
        Test("higher boss tiers produce more separately warned waves and faster real ordnance", BossTierPressureV12);
        Test("seeded charges mix partial and full sweeps and actually clear the far left edge", BossSweepVariantsV12);
        Test("repeated boss lunges each complete their own cue before striking and returning", BossRepeatChargesV12);
        Test("practice difficulty tiers preserve progression, stocks and restart selection", PracticeTiersV12);
        Test("later mixed patterns remain traversable with standard practice hearts and ordinary commands", LateTierPathsV12);
        Test("boss victory travels forward through the far gate while the camera follows and running clocks stay frozen", ForwardDepartureV12);
    }

    private static RunnerSession TierPracticeV12(BossKind kind,int tier,int seed=617,bool protect=false,float fraction=1f)
    {
        var run=new RunnerSession(null,seed);
        Check(run.StartBossPractice(kind,CharacterKind.Classic,false,tier),"Tier practice start rejected");
        Check(run.SkipPracticeIntro(),"Tier practice skip rejected");
        if(fraction<1f)run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*fraction;
        if(protect)SetProperty(run,"InvincibleRemaining",100000f);
        return run;
    }

    private static void BossTierProgressV12()
    {
        foreach(int number in new[]{1,2,3,4,6,7,12,24,60})
        {
            RunnerSession run=EnterBossFixture(number);
            int tier=3+(number-1)/4;
            Check(run.BossTier==tier&&run.CurrentBoss.Tier==tier,"Milestone did not capture the actual round tier");
            float basic=BossBaseHpV13(run.CurrentBoss.Kind);
            Near(run.CurrentBoss.MaxHealth/basic,run.BossHealthScale,.002f,"Actual normal boss HP ignored its tier");
        }
        foreach(BossKind kind in LegacyBossKindsV13)
        {
            float previousHp=0,previousPace=0;
            foreach(int tier in new[]{1,2,4,8,32,128})
            {
                RunnerSession run=TierPracticeV12(kind,tier);
                Check(run.CurrentBoss.MaxHealth>previousHp&&run.BossTierPace>=previousPace,"Boss tier stopped increasing health or reduced projectile pace");
                Check(run.BossRepeatCount>=1&&run.BossRepeatCount<=6,"A tier allocated unbounded simultaneous repeat waves");
                previousHp=run.CurrentBoss.MaxHealth;previousPace=run.BossTierPace;
                if(tier>=8)
                {
                    float baseHp=BossBaseHpV13(kind);
                    Check(run.CurrentBoss.MaxHealth>baseHp*1.6f,"Later tiers still hit the old 60% HP ceiling");
                }
            }
        }
    }

    private sealed class TierOutputV12
    {
        public int Dangers,Warnings,Hidden,Repeated;
        public float FirstCannonSpeed;
    }
    private static TierOutputV12 MeasureTierV12(BossKind kind,int tier)
    {
        // Protected output audit deliberately holds rage at phase 1. It measures
        // production counts and completed warning identities, not survival.
        RunnerSession run=TierPracticeV12(kind,tier,617,true);
        var danger=new HashSet<int>();var warnings=new Dictionary<int,Tuple<float,float>>();var hidden=new HashSet<int>();
        int repeated=0;float cannon=0;
        for(int i=0;i<120*75;i++)
        {
            foreach(BossTelegraph mark in run.BossTelegraphs)
            {
                if(!mark.Visible){hidden.Add(mark.Id);continue;}
                if(!mark.Active&&!warnings.ContainsKey(mark.Id))
                {
                    Check(mark.Duration>=.44f,"Tier compressed its visible warning below the reaction floor");
                    warnings.Add(mark.Id,Tuple.Create(run.BossElapsed-(mark.Duration-mark.Remaining),mark.Duration));
                }
                if(mark.Active)danger.Add(mark.Id);
            }
            foreach(BossProjectile shot in run.BossProjectiles.Where(p=>!p.Friendly&&!p.Removed))
            {
                if(shot.Kind==BossAttackKind.Cannon&&cannon==0)cannon=Math.Abs(shot.VelocityX);
                if(!danger.Add(shot.Id))continue;
                Check(warnings.ContainsKey(shot.SourceTelegraphId),"Tier projectile lost its own visible warning");
                var cue=warnings[shot.SourceTelegraphId];
                Check(run.BossElapsed-cue.Item1>=cue.Item2-.025f,"Tier projectile fired before its warning completed");
            }
            foreach(BossObstacle obstacle in run.BossObstacles.Where(o=>o.Active&&!o.Removed))danger.Add(obstacle.Id);
            if(run.CurrentBoss.ContactActive)danger.Add(run.CurrentBoss.ChargeSourceTelegraphId);
            repeated=Math.Max(repeated,run.BossTelegraphs.Count(m=>!m.Active));
            Check(run.BossTelegraphs.Count<100&&run.BossProjectiles.Count<100&&run.BossObstacles.Count<30,"A high tier accumulated unbounded live hazards");
            run.Update(1f/120);
        }
        return new TierOutputV12{Dangers=danger.Count,Warnings=warnings.Count,Hidden=hidden.Count,Repeated=repeated,FirstCannonSpeed=cannon};
    }
    private static void BossTierPressureV12()
    {
        foreach(BossKind kind in LegacyBossKindsV13)
        {
            TierOutputV12 first=MeasureTierV12(kind,1),second=MeasureTierV12(kind,2),fourth=MeasureTierV12(kind,4),eighth=MeasureTierV12(kind,8);
            // A finite timed sample can end mid-combo, so compare advanced
            // output to isolated drills; V14 additionally compares identical
            // seeded formations and proves their extra casts at C8.
            Check(second.Dangers>first.Dangers&&fourth.Dangers>second.Dangers&&eighth.Dangers>second.Dangers,
                "Later tiers did not increase real threat output "+kind+": "+first.Dangers+"/"+second.Dangers+"/"+fourth.Dangers+"/"+eighth.Dangers);
            Check(second.Hidden>first.Hidden&&fourth.Warnings>first.Warnings&&eighth.Repeated>first.Repeated,"Tier escalation only changed a numeric label "+kind);
            if(kind==BossKind.Tank)Check(first.FirstCannonSpeed<second.FirstCannonSpeed&&second.FirstCannonSpeed<fourth.FirstCannonSpeed&&fourth.FirstCannonSpeed<eighth.FirstCannonSpeed,"Tank cannon speed did not rise with actual tier");
            Console.WriteLine("  V12 "+kind+" tiers1/2/4/8: "+first.Dangers+"/"+second.Dangers+"/"+fourth.Dangers+"/"+eighth.Dangers+" real hazards over75s (protected output audit).");
        }
    }

    private static string SweepTraceV12(BossKind kind,int seed,out int full,out int partial)
    {
        RunnerSession run=TierPracticeV12(kind,4,seed,true,.2f);
        var trace=new List<string>();var cues=new Dictionary<int,BossTelegraph>();var active=new HashSet<int>();
        full=partial=0;float previousX=run.CurrentBoss.X;
        var farLeft=new HashSet<int>();var crossed=new HashSet<int>();
        for(int i=0;i<120*85;i++)
        {
            foreach(BossTelegraph cue in run.BossTelegraphs)
            {
                if(!IsChargeV11(cue.Kind)||!cue.Visible||cues.ContainsKey(cue.Id))continue;
                cues.Add(cue.Id,cue);trace.Add(cue.Kind+":"+cue.FullSweep+":"+cue.SweepEndX.ToString("0.000"));
                Check(cue.FullSweep?cue.SweepEndX<-run.CurrentBoss.Width:cue.SweepEndX>=100&&cue.SweepEndX<=500,"Sweep endpoint contradicts its visible variant");
                if(cue.FullSweep)full++;else partial++;
            }
            BossEncounter boss=run.CurrentBoss;
            if(IsChargeV11(boss.AttackKind)&&(boss.Action==BossAction.Strike||boss.Action==BossAction.Recovery))
            {
                Check(Math.Abs(boss.X-previousX)<55,"Sweep teleported during actual strike/return");
                if(boss.ContactActive)
                {
                    Check(cues.ContainsKey(boss.ChargeSourceTelegraphId),"Sweep has no variant warning");
                    BossTelegraph source=cues[boss.ChargeSourceTelegraphId];
                    Check(source.FullSweep==boss.FullSweep&&source.SweepEndX==boss.SweepEndX,"Active sweep changed the locked variant or endpoint");
                    active.Add(source.Id);
                    if(source.FullSweep&&boss.ContactX+boss.ContactWidth<100)crossed.Add(source.Id);
                    if(source.FullSweep&&boss.X<-boss.Width)farLeft.Add(source.Id);
                }
                if(boss.Action==BossAction.Recovery)Check(!boss.ContactActive,"Sweep return remained damaging without a new warning");
            }
            previousX=boss.X;run.Update(1f/120);
        }
        Check(active.Count>0&&farLeft.Count>0&&crossed.Count>0,"Full sweep did not physically clear the leftmost player lane and leave the screen "+kind);
        return string.Join("|",trace);
    }
    private static void BossSweepVariantsV12()
    {
        foreach(BossKind kind in LegacyBossKindsV13)
        {
            int full,partial,againFull,againPartial,otherFull,otherPartial;
            string trace=SweepTraceV12(kind,617,out full,out partial);
            string repeat=SweepTraceV12(kind,617,out againFull,out againPartial);
            string other=SweepTraceV12(kind,991,out otherFull,out otherPartial);
            Check(trace==repeat&&full==againFull&&partial==againPartial,"Same seed changed sweep choices");
            Check(full+otherFull>0&&partial+otherPartial>0&&trace!=other,"Sweep choices omitted a variant or ignored seeded randomness "+kind);
        }
    }

    private static void BossRepeatChargesV12()
    {
        foreach(BossKind kind in LegacyBossKindsV13)
        {
            RunnerSession run=TierPracticeV12(kind,8,317,true,.2f);
            // Isolate the repeated-charge family, which now alternates with
            // mixed formations rather than appearing on a fixed cycle.
            ClearArenaThreats(run); ArenaObstacles(run).Clear();
            BossAttackKind charge = kind==BossKind.Tank ? BossAttackKind.TankRam
                : kind==BossKind.Gunship ? BossAttackKind.GunshipDive : BossAttackKind.RobotDash;
            typeof(RunnerSession).GetMethod("PlanBossSpecial",BindingFlags.Instance|BindingFlags.NonPublic)
                .Invoke(run,new object[]{charge,.6f});
            var cues=new Dictionary<int,Tuple<float,BossTelegraph>>();var struck=new HashSet<int>();
            bool queuedChain=false;int maxChain=0;
            for(int i=0;i<120*65;i++)
            {
                maxChain=Math.Max(maxChain,run.BossTelegraphs.Count(m=>IsChargeV11(m.Kind)&&!m.Active));
                queuedChain|=run.BossTelegraphs.Count(m=>IsChargeV11(m.Kind)&&!m.Active)>1;
                foreach(BossTelegraph mark in run.BossTelegraphs)
                    if(mark.Visible&&IsChargeV11(mark.Kind)&&!cues.ContainsKey(mark.Id))cues.Add(mark.Id,Tuple.Create(run.BossElapsed-(mark.Duration-mark.Remaining),mark));
                BossEncounter boss=run.CurrentBoss;
                if(boss.ContactActive&&struck.Add(boss.ChargeSourceTelegraphId))
                {
                    Check(cues.ContainsKey(boss.ChargeSourceTelegraphId),"Repeated charge skipped an individual visible cue");
                    var cue=cues[boss.ChargeSourceTelegraphId];Check(run.BossElapsed-cue.Item1>=cue.Item2.Duration-.025f,"Repeated charge struck before its own cue completed");
                }
                run.Update(1f/120);
            }
            Check(queuedChain&&maxChain>=2&&struck.Count>=4,"Later tier did not perform real repeated lunges "+kind);
            run.TogglePause();string frozen=SpecialSnapshotV11(run);run.Update(3);Check(frozen==SpecialSnapshotV11(run),"Pause advanced a repeated sweep");
            run.TogglePause();run.CurrentBoss.Health=0;run.Update(1f/120);
            Check(run.ArenaPhase==BossPhase.Victory&&!run.CurrentBoss.ContactActive&&run.BossTelegraphs.Count==0&&run.BossProjectiles.Count==0,"Victory left a queued tier chain alive");
        }
    }

    private static void PracticeTiersV12()
    {
        var run=new RunnerSession(StockedSaveV10(),81);string saved=SaveSnapshotV10(run.Save);int writes=0;run.SaveChanged+=()=>writes++;
        Check(!run.StartBossPractice(BossKind.Tank,CharacterKind.Classic,false,0)&&!run.StartBossPractice(BossKind.Tank,CharacterKind.Classic,false,100001),"Invalid practice tier accepted");
        foreach(int tier in new[]{1,2,5,8})
        {
            Check(run.StartBossPractice(BossKind.Gunship,CharacterKind.Raptor,false,tier),"Valid tier practice rejected");
            Check(run.BossTier==tier&&run.CurrentBoss.Tier==tier,"Practice tier was not applied to the encounter");
            Check(run.SkipPracticeIntro(),"Practice tier intro could not be skipped");run.ReturnToMenu();
            Check(SaveSnapshotV10(run.Save)==saved&&writes==0,"Practice tier spent stocks or recorded normal progression");
        }
        run.StartRun();Check(run.BossTier==3&&!run.IsPractice,"Practice tier leaked into the next normal run");
        Check(run.Lives==4&&run.ShieldReady&&run.WaterReserveReady&&run.WarmReserveReady,"Practice tiers consumed next-run purchases");
    }

    private static void LateTierPathsV12()
    {
        foreach(int tier in new[]{2,4,8})
        foreach(BossKind kind in LegacyBossKindsV13)
        {
            // Standard practice uses the same production attack rules. Reducing
            // lives to one makes this path test stricter; health staging holds
            // phase 3 so all late combinations are exercised without victory.
            var run=new RunnerSession(null,617);
            Check(run.StartBossPractice(kind,CharacterKind.Classic,false,tier),"Late path practice rejected");
            // Stage health before combat starts so even the opening plan is
            // the intended final phase rather than a leftover full-health volley.
            run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*.2f;
            Check(run.SkipPracticeIntro(),"Late path intro skip rejected");
            if(tier==2)SetProperty(run,"Lives",1);
            float health=run.CurrentBoss.Health;int casts=0;var sourceIds=new HashSet<int>();
            int seconds=tier>=3?35:50;
            var kinds=new HashSet<BossAttackKind>();
            for(int i=0;i<120*seconds;i++)
            {
                foreach(var cue in run.BossTelegraphs.Where(m=>m.Visible))kinds.Add(cue.Kind);
                CombatBotInput(run,run.BossObstacles.Any(o=>o.Destructible&&o.X>run.PlayerX&&o.X<run.PlayerX+500),true);
                if(run.CurrentBoss.ContactActive&&sourceIds.Add(run.CurrentBoss.ChargeSourceTelegraphId))casts++;
                run.Update(1f/120);
                Check(run.State==RunState.Playing&&run.Lives>0,"Late tier route exhausted standard practice hearts "+kind+" tier="+tier+" t="+run.BossElapsed+" "+ArenaDescription(run));
            }
            Check(kinds.Count>=2,"Late route omitted distinct skill families "+kind+" tier="+tier);
            Console.WriteLine("  V14 real path "+kind+" tier "+tier+":"+seconds+"s, "+kinds.Count+" skill families, "+casts+" charges, "+((tier==2?1:3)-run.Lives)+" hits; no invulnerability or skill.");
        }
    }

    private static void ForwardDepartureV12()
    {
        foreach(int number in new[]{1,2,3,4})
        {
            // Victory bookkeeping fixture: the full-health combat tests above
            // supply separate win evidence. This fixture isolates the journey.
            RunnerSession run=EnterBossFixture(number,hearts:2);ReachFighting(run);
            SetProperty(run,"PlayerX",620f);SetProperty(run,"FacingLeft",true);
            float oldY=run.PlayerY,elapsed=run.Elapsed,distance=run.Distance,speed=run.Speed;
            float water=run.Water,warmth=run.Warmth,stage=run.DistrictElapsed;int score=run.Score,lives=run.Lives;
            Check(RunnerSession.BossExitGateX>run.CurrentBoss.X+run.CurrentBoss.Width,
                "The exit gate is not beyond the defeated boss");
            run.CurrentBoss.Health=0;run.Update(1f/120);
            Check(run.ArenaPhase==BossPhase.Victory&&!run.FacingLeft,"Victory did not face the forward exit");
            oldY=run.PlayerY;
            float previousWorld=run.DepartureWorldX,previousCamera=run.BossExitCameraX;
            float previousScreen=run.PlayerX,previousY=run.PlayerY;bool crossed=false,paused=false,moved=false;
            for(int i=0;i<120*6&&run.IsBossArena;i++)
            {
                run.SetHorizontal(-1);run.SetDucking(true);run.SetFlightLift(true);
                Check(!run.Jump()&&!run.TryShoot(),"Forward outro accepted manual combat input");
                run.Update(1f/120);
                Check(run.Elapsed==elapsed&&run.Distance==distance&&run.Speed==speed&&run.Score==score&&
                    run.Water==water&&run.Warmth==warmth&&run.DistrictElapsed==stage&&run.Lives==lives,
                    "Forward cinematic changed scored progress, survival resources or hearts");
                if(!run.IsBossArena)break;
                float world=run.DepartureWorldX,camera=run.BossExitCameraX;
                Check(world>=previousWorld-.002f&&camera>=previousCamera-.002f,
                    "Hero or camera reversed back toward the entrance");
                Near(run.PlayerX+camera,world,.02f,"World/camera position detached from the visible hero");
                Check(Math.Abs(run.PlayerX-previousScreen)<5&&Math.Abs(run.PlayerY-previousY)<5,
                    "Forward journey teleported between frames");
                Check(!run.FacingLeft&&!run.IsDucking,"The hero walked backward or crouched during the exit");
                if(run.ExitTravelProgress>0)
                {
                    moved|=world>620.5f&&camera>0;
                    if(world>=RunnerSession.BossExitGateX)
                    {
                        crossed=true;
                        Check(run.PlayerX>=RunnerSession.BossExitGateX-camera-.01f,
                            "The screen hero never passed the far gate");
                    }
                }
                else
                {
                    Near(world,620,.01f,"Explosion pause prematurely moved the hero");
                    Near(camera,0,.01f,"Explosion pause prematurely panned the camera");
                    Near(run.PlayerY,oldY,.01f,"Explosion pause prematurely landed the hero");
                }
                if(!paused&&run.ExitTravelProgress>.35f)
                {
                    paused=true;float progress=run.ExitTravelProgress,clock=run.BossElapsed;
                    run.TogglePause();run.Update(3);
                    Near(run.DepartureWorldX,world,0,"Pause advanced forward hero travel");
                    Near(run.BossExitCameraX,camera,0,"Pause advanced the cinematic camera");
                    Near(run.ExitTravelProgress,progress,0,"Pause advanced exit progress");
                    Near(run.BossElapsed,clock,0,"Pause advanced the boss clock");
                    run.TogglePause();
                }
                previousWorld=world;previousCamera=camera;previousScreen=run.PlayerX;previousY=run.PlayerY;
            }
            Check(moved&&crossed&&paused&&!run.IsBossArena,"Forward exit never crossed the far gate and resumed the road");
            Near(run.PlayerX,RunnerSession.StartPlayerX,.02f,"Camera did not settle into the normal running lane");
            Near(run.PlayerY,0,.01f,"Forward exit resumed above the road");
            Check(!run.FlightDeviceCollected&&run.BossExitCameraX==0&&run.ExitTravelProgress==0,
                "The finished forward corridor leaked into ordinary running");
            run.Update(.02f);Check(run.Elapsed>elapsed&&run.Distance>distance,"Scored running failed to resume after the forward gate");
            run.ReturnToMenu();Check(run.BossExitCameraX==0&&run.ExitTravelProgress==0,"Menu retained a forward camera offset");
        }
        foreach(float startX in new[]{100f,260f,720f})
        {
            RunnerSession run=EnterBossFixture(2);ReachFighting(run);
            SetProperty(run,"PlayerX",startX);SetProperty(run,"PlayerY",RunnerSession.FlightMaximumY);
            SetProperty(run,"VelocityY",0f);run.CurrentBoss.Health=0;run.Update(1f/120);
            bool landed=false,crossed=false;float previousY=run.PlayerY,previousWorld=run.DepartureWorldX;
            for(int i=0;i<120*5&&run.IsBossArena;i++)
            {
                run.Update(1f/120);if(!run.IsBossArena)break;
                Check(run.DepartureWorldX>=previousWorld-.002f&&run.PlayerY<=previousY+.002f,
                    "Aerial exit reversed world travel or altitude during landing");
                Check(previousY-run.PlayerY<5,"Aerial exit snapped down to the road");
                if(!landed&&run.PlayerY<=.001f)
                {
                    landed=true;
                    Check(run.DepartureWorldX<RunnerSession.BossExitGateX&&!run.FlightDeviceCollected,
                        "Aerial rider did not land and put away the board before the far gate, startX="+startX);
                }
                if(run.DepartureWorldX>=RunnerSession.BossExitGateX)
                {
                    crossed=true;
                    Check(landed&&run.PlayerY==0&&!run.FlightDeviceCollected&&run.TransitionElevation==0,
                        "The rider flew through the ground exit instead of running through it");
                }
                previousY=run.PlayerY;previousWorld=run.DepartureWorldX;
            }
            Check(landed&&crossed&&!run.IsBossArena,"Aerial far-edge journey failed to land and pass the exit");
        }
    }
}
