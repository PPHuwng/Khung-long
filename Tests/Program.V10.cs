using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityDino;

internal static partial class Program
{
    private static void RegisterV10Tests()
    {
        Test("practice trials preserve every saved field and purchased stock for all characters", PracticeIsolationV10);
        Test("practice loss and retry never record normal runs or grant ownership", PracticeLifecycleV10);
        Test("learning mode changes only training health, warning duration and pace", PracticeDifficultyV10);
        Test("boss entry scripts a continuous approach, pickup and gradual ascent", BossJourneyV10);
        Test("boss departure gradually lands and resumes the same running clocks", BossDepartureV10);
        Test("flight hold, release, flap, boundaries and pause use the actual flight integrator", FlightControlV10);
        Test("practice victory has its own result without stars, history or saved changes", PracticeVictoryV10);
        Test("boss-created obstacles have completed source warnings and finite lifetimes", BossObstacleSignalsV10);
        Test("breakable boss scenery intercepts real shots and leaves a clear route", BossObstacleDamageV10);
        Test("boss-created circles use true round contact and all scenery freezes on pause", BossObstacleContactsV10);
    }

    private static string SaveSnapshotV10(RunnerSave save)
    {
        return System.Text.Json.JsonSerializer.Serialize(save,new System.Text.Json.JsonSerializerOptions{IncludeFields=true});
    }
    private static RunnerSave StockedSaveV10()
    {
        return new RunnerSave {Wallet=9812,BestScore=40123,LastScore=22341,PurchasedHearts=3,
            PurchasedShield=true,PurchasedWaterReserve=true,PurchasedWarmReserve=true,
            ScoreBoostLevel=2,MagnetLevel=1,OwnsTriceratops=false,OwnsRaptor=false,
            SelectedCharacter=CharacterKind.Classic,History=new List<RunRecord>{new RunRecord{Score=40123,Stars=99,Distance=12345}}};
    }
    private static void PracticeIsolationV10()
    {
        foreach(BossKind boss in Enum.GetValues(typeof(BossKind)))
        foreach(CharacterKind character in Enum.GetValues(typeof(CharacterKind)))
        {
            var run=new RunnerSession(StockedSaveV10(),318);string save=SaveSnapshotV10(run.Save);int saves=0;run.SaveChanged+=()=>saves++;
            Check(run.StartBossPractice(boss,character),"Valid practice rejected");
            Check(run.IsPractice&&run.CurrentCharacter==character&&run.CurrentBoss.Kind==boss&&run.Lives==5,"Practice selection did not apply");
            Check(!run.ShieldReady&&!run.WaterReserveReady&&!run.WarmReserveReady,"Practice borrowed normal-run stocks");
            Check(run.SkipPracticeIntro()&&!run.SkipPracticeIntro(),"Practice intro skip did not limit itself to intro");
            CombatBotInput(run,true);run.Update(.2f);run.ReturnToMenu();
            Check(run.State==RunState.Menu&&!run.IsPractice,"Practice status leaked to menu");
            Check(run.CurrentCharacter==CharacterKind.Classic&&SaveSnapshotV10(run.Save)==save&&saves==0,"Training changed ownership, equipped profile or saved progression");
            run.StartRun();
            Check(run.CurrentCharacter==CharacterKind.Classic&&run.Lives==4&&run.ShieldReady&&run.WaterReserveReady&&run.WarmReserveReady,"Training consumed next-run purchases");
        }
        var invalid=new RunnerSession(null,1);
        Check(!invalid.StartBossPractice((BossKind)99,CharacterKind.Classic)&&!invalid.StartBossPractice(BossKind.Tank,(CharacterKind)99),"Invalid training choice accepted");
        invalid.StartRun();Check(!invalid.StartBossPractice(BossKind.Tank,CharacterKind.Classic)&&!invalid.SkipPracticeIntro(),"Normal run accepted training transition");
    }
    private static void PracticeLifecycleV10()
    {
        var run=new RunnerSession(StockedSaveV10(),3);string saved=SaveSnapshotV10(run.Save);int saves=0;run.SaveChanged+=()=>saves++;
        run.StartBossPractice(BossKind.Gunship,CharacterKind.Raptor,false);run.SkipPracticeIntro();
        for(int i=0;i<3;i++)
        {
            SetProperty(run,"InvincibleRemaining",0f);
            ArenaShots(run).Add(new BossProjectile{Id=900000+i,Kind=BossAttackKind.Cannon,X=run.PlayerX-40,Y=run.PlayerY,Width=100,Height=100,Lifetime=1});
            run.Update(1f/120);
        }
        Check(run.State==RunState.GameOver&&run.PracticeFinished&&!run.PracticeWon&&run.PracticeHits==3,"Training loss did not create a separate result");
        float clock=run.PracticeElapsed,y=run.PlayerY;run.Update(10);
        Check(run.PracticeElapsed==clock&&run.PlayerY==y&&SaveSnapshotV10(run.Save)==saved&&saves==0,"Training death advanced or recorded saved history");
        Check(run.StartBossPractice(BossKind.Sentinel,CharacterKind.Triceratops)&&run.Lives==5&&run.PracticeHits==0&&run.PracticeElapsed==0&&!run.PracticeFinished&&run.SkillCooldownRemaining==0,"Training retry retained prior damage, clock or cooldown");
        Check(SaveSnapshotV10(run.Save)==saved&&saves==0,"Retry changed saved stocks");
    }
    private static void PracticeDifficultyV10()
    {
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            var easy=new RunnerSession(null,12);var standard=new RunnerSession(null,12);
            easy.StartBossPractice(kind,CharacterKind.Classic,true);standard.StartBossPractice(kind,CharacterKind.Classic,false);
            Check(easy.Lives==5&&standard.Lives==3&&easy.CurrentBoss.MaxHealth==standard.CurrentBoss.MaxHealth*.65f,"Learning health/margins incorrect");
            easy.SkipPracticeIntro();standard.SkipPracticeIntro();
            Near(easy.BossTelegraphs[0].Duration,standard.BossTelegraphs[0].Duration*1.25f,.001f,"Learning warning not extended");
            Check(easy.BossPracticePace==.8f&&standard.BossPracticePace==1f,"Learning pace did not remain training-only");
        }
    }
    private static void BossJourneyV10()
    {
        foreach(int number in new[]{1,2,4})
        {
            RunnerSession run=CharacterRun(CharacterKind.Classic,District.Storm);
            SetProperty(run,"PlayerX",690f);run.Jump();run.Update(.2f);
            float beforeX=run.PlayerX,beforeY=run.PlayerY;
            SetProperty(run,"BossNumber",number-1);SetProperty(run,"NextBossScore",number*10000);SetProperty(run,"Distance",number*200000f-1);SetProperty(run,"Score",number*10000-1);run.Update(1f/120);
            Check(run.CurrentBoss.Kind==(NormalBossKindV13(number)),"Wrong themed milestone order");
            Near(run.PlayerX,beforeX,2,"Milestone teleported X");Near(run.PlayerY,beforeY,8,"Milestone teleported Y");
            int score=run.Score;float elapsed=run.Elapsed,stage=run.DistrictElapsed,dist=run.Distance,water=run.Water,warmth=run.Warmth;
            bool collect=false,takeoff=false,arrive=false;float previousX=run.PlayerX,previousY=run.PlayerY,blend=0;
            for(int i=0;i<120*5&&run.ArenaPhase==BossPhase.Intro;i++)
            {
                run.SetHorizontal(-1);run.SetDucking(true);run.SetFlightLift(true);
                Check(!run.Jump()&&!run.TryShoot()&&!run.TryUseSkill(),"Transition accepted combat input");
                run.Update(1f/120);
                Check(Math.Abs(run.PlayerX-previousX)<5&&Math.Abs(run.PlayerY-previousY)<5,"Scripted hero journey jumped between frames");
                Check(run.TransitionSceneBlend>=blend-.001f,"Entry scene blend reversed");
                previousX=run.PlayerX;previousY=run.PlayerY;blend=run.TransitionSceneBlend;
                collect|=run.TransitionStage==BossTransitionStage.Collect;takeoff|=run.TransitionStage==BossTransitionStage.Takeoff;arrive|=run.TransitionStage==BossTransitionStage.Arrive;
                Check(run.Score==score&&run.Elapsed==elapsed&&run.DistrictElapsed==stage&&run.Distance==dist&&run.Water==water&&run.Warmth==warmth,"Transition advanced running state");
            }
            Check(run.ArenaPhase==BossPhase.Fighting&&(number!=2||collect)&&arrive,"Journey missed visible stages");
            Near(run.PlayerX,260,.1f,"Incorrect arrival lane");
            if(number==2)Check(takeoff&&run.FlightDeviceCollected&&run.PlayerY>140&&run.FlightControlsEnabled,"Gunship did not collect and ascend with device");
            else Check(!takeoff&&!run.FlightDeviceCollected&&run.PlayerY==0&&!run.FlightControlsEnabled,"Ground boss used a flying arena");
        }
    }
    private static void BossDepartureV10()
    {
        RunnerSession run=EnterBossFixture(2);ReachFighting(run);float clock=run.Elapsed,dist=run.Distance,speed=run.Speed;int score=run.Score;
        run.CurrentBoss.Health=0;run.Update(1f/120);Check(run.ArenaPhase==BossPhase.Victory,"Defeated gunship did not start outro");
        float previous=run.PlayerY;bool land=false;float blend=1;
        for(int i=0;i<120*6&&run.IsBossArena;i++)
        {
            run.SetFlightLift(true);run.SetHorizontal(1);Check(!run.Jump()&&!run.TryShoot(),"Victory accepted fight input");run.Update(1f/120);
            Check(Math.Abs(run.PlayerY-previous)<3,"Gunship outro teleported to ground");previous=run.PlayerY;
            if(run.IsBossArena){land|=run.TransitionStage==BossTransitionStage.Land;Check(run.TransitionSceneBlend<=blend+.001f,"Landing blend reversed");blend=run.TransitionSceneBlend;}
            Check(run.Elapsed==clock&&run.Distance==dist&&run.Speed==speed&&run.Score==score,"Outro advanced running clock");
        }
        Check(land&&!run.IsBossArena&&run.PlayerY==0&&!run.FlightDeviceCollected,"Outro did not land before returning");
        run.Update(.02f);Check(run.Elapsed>clock&&run.Distance>dist,"Normal running failed to resume after landing");
    }
    private static void FlightControlV10()
    {
        var run=new RunnerSession(null,4);run.StartBossPractice(BossKind.Gunship,CharacterKind.Raptor,false);run.SkipPracticeIntro();
        // Isolate flight control from the encounter's unrelated damage rules.
        SetProperty(run,"InvincibleRemaining",100f);
        float y=run.PlayerY;run.SetFlightLift(true);run.Update(.25f);Check(run.PlayerY>y&&run.VelocityY>0,"Holding lift failed to ascend");
        run.SetFlightLift(false);run.Update(.6f);Check(run.VelocityY<0,"Release did not create gravity-driven descent");
        Check(run.Jump()&&run.VelocityY>=150&&!run.Jump(),"Space flap missing or cadence unbounded");
        run.SetDucking(true);run.Update(.02f);Check(!run.IsDucking,"Flight allowed ground crouch");
        run.SetHorizontal(1);float x=run.PlayerX;run.Update(.1f);Check(run.PlayerX>x,"Flight lost A/D steering");
        run.SetFlightLift(true);run.TogglePause();y=run.PlayerY;float vy=run.VelocityY,b=run.BossElapsed;run.Update(4);
        Check(run.PlayerY==y&&run.VelocityY==vy&&run.BossElapsed==b&&!run.FlightControlsEnabled,"Pause advanced flight");run.TogglePause();
        run.Update(.6f);Check(run.VelocityY<=0,"Pause retained a held lift after resuming");
        run.SetFlightLift(true);for(int i=0;i<120*3;i++)run.Update(1f/120);
        Near(run.PlayerY,RunnerSession.FlightMaximumY,.01f,"Flight upper bound failed");
        run.SetFlightLift(false);for(int i=0;i<120*3;i++)run.Update(1f/120);
        Near(run.PlayerY,RunnerSession.FlightMinimumY,.01f,"Flight lower bound failed");Check(run.Lives==3,"Flight boundaries damaged the player");
    }
    private static void PracticeVictoryV10()
    {
        var run=new RunnerSession(StockedSaveV10(),9);string save=SaveSnapshotV10(run.Save);int saves=0;run.SaveChanged+=()=>saves++;
        run.StartBossPractice(BossKind.Tank,CharacterKind.Classic);run.SkipPracticeIntro();run.CurrentBoss.Health=1;run.SetShooting(true);
        for(int i=0;i<120*20&&run.State==RunState.Playing;i++){ClearArenaThreats(run);run.Update(1f/120);}
        Check(run.State==RunState.GameOver&&run.PracticeFinished&&run.PracticeWon&&run.PracticeElapsed>0,"Training victory did not finish its dedicated result");
        Check(run.Save.Wallet==9812&&run.RunStars==0&&run.Score==0&&SaveSnapshotV10(run.Save)==save&&saves==0,"Training victory awarded progression or consumed stock");
    }
    private static void BossObstacleSignalsV10()
    {
        var allKinds=new HashSet<BossAttackKind>();int count=0;
        for(int number=1;number<=3;number++)
        {
            RunnerSession run=IsolatedBossPattern(number,.2f);var warned=new Dictionary<int,Tuple<float,float>>();var seen=new HashSet<int>();
            for(int i=0;i<120*40;i++)
            {
                foreach(BossTelegraph mark in run.BossTelegraphs)
                    if(mark.Visible&&!mark.Active&&!warned.ContainsKey(mark.Id))warned.Add(mark.Id,Tuple.Create(run.BossElapsed-(mark.Duration-mark.Remaining),mark.Duration));
                foreach(BossObstacle obstacle in run.BossObstacles)
                {
                    Check(obstacle.Remaining>0&&obstacle.Remaining<=12&&obstacle.Width>0&&obstacle.Height>0,"Invalid finite scenery geometry/lifetime");
                    if(!seen.Add(obstacle.Id))continue;
                    Check(warned.ContainsKey(obstacle.SourceTelegraphId),"Boss scenery had no visible source warning");var warning=warned[obstacle.SourceTelegraphId];
                    Check(run.BossElapsed-warning.Item1>=warning.Item2-.025f,"Boss scenery appeared before warning finished");allKinds.Add(obstacle.Kind);count++;
                }
                Check(run.BossObstacles.Count<=12,"Arena scenery accumulated without bounds");run.Update(1f/120);
            }
            run.ReturnToMenu();Check(run.BossObstacles.Count==0,"Menu retained boss scenery");
        }
        foreach(BossAttackKind kind in new[]{BossAttackKind.MineDrop,BossAttackKind.CrateDrop,BossAttackKind.ElectricPillar,BossAttackKind.ShieldDrone,BossAttackKind.AirGate,BossAttackKind.AirMine})
            Check(allKinds.Contains(kind),"Missing real boss-created obstacle "+kind);
        Check(count>=20,"Obstacle coverage missed sustained cycles");
    }
    private static IList<BossObstacle> ArenaObstacles(RunnerSession run){return (IList<BossObstacle>)run.BossObstacles;}
    private static void BossObstacleDamageV10()
    {
        RunnerSession run=EnterBossFixture();ReachFighting(run);ClearArenaThreats(run);
        // Isolate an actual breakable body and projectile path. Boss retains HP.
        var obstacle=new BossObstacle{Id=99000,Kind=BossAttackKind.CrateDrop,X=run.PlayerX+170,Y=0,Width=56,Height=68,Remaining=10,Health=32,MaxHealth=32};ArenaObstacles(run).Add(obstacle);
        float hp=run.CurrentBoss.Health;run.SetShooting(true);bool hit=false;
        for(int i=0;i<120*3&&!obstacle.Removed;i++){ClearArenaThreats(run);run.Update(1f/120);hit|=obstacle.Health<32;Check(run.CurrentBoss.Health==hp,"Shots passed through live breakable scenery");}
        Check(hit&&obstacle.Health==0&&obstacle.Removed,"Real shots could not destroy crate");
        bool bossHit=false;for(int i=0;i<120*4;i++){ClearArenaThreats(run);run.Update(1f/120);bossHit|=run.CurrentBoss.Health<hp;}
        Check(bossHit,"Destroyed body kept blocking boss shots");
    }
    private static void BossObstacleContactsV10()
    {
        RunnerSession run=EnterBossFixture(1,hearts:1);ReachFighting(run);ClearArenaThreats(run);
        var mine=new BossObstacle{Id=99001,Kind=BossAttackKind.MineDrop,X=run.PlayerX-20,Y=35,Width=32,Height=32,Radius=16,Remaining=10,Health=16,MaxHealth=16};ArenaObstacles(run).Add(mine);
        bool expected=RunnerGeometry.CircleTouchesPlayer(mine.X+16,mine.Y+16,16,run);run.Update(1f/120);
        Check(expected&&run.Lives==1&&mine.Removed,"Circular boss hazard did not use real contact");
        var crate=new BossObstacle{Id=99002,Kind=BossAttackKind.CrateDrop,X=800,Y=0,Width=56,Height=68,VelocityX=-140,Remaining=10,Health=32,MaxHealth=32};ArenaObstacles(run).Add(crate);
        run.TogglePause();float x=crate.X,life=crate.Remaining;run.Update(5);Check(crate.X==x&&crate.Remaining==life,"Pause advanced boss scenery");
        run.TogglePause();run.CurrentBoss.Health=0;run.Update(1f/120);Check(run.BossObstacles.Count==0,"Victory retained scenery");
    }

    private static bool NeedsObstacleAirExtensionV10(RunnerSession run)
    {
        if(run.PlayerY<=0||run.VelocityY>=0)return false;
        return run.BossObstacles.Any(o=>o.Active&&!o.Removed&&(o.Kind==BossAttackKind.MineDrop||o.Kind==BossAttackKind.CrateDrop)&&
            o.X+o.Width>run.PlayerX-55&&o.X<run.PlayerX+110&&run.PlayerY<o.Y+o.Height+35);
    }
    private static void FlightBotInputV10(RunnerSession run)
    {
        run.SetDucking(false);run.SetHorizontal(0);
        var forbidden=new List<Tuple<float,float>>();float target=145;
        void Forbid(float low,float high){forbidden.Add(Tuple.Create(low,high));}
        foreach(BossObstacle obstacle in run.BossObstacles)
        {
            if(obstacle.Removed||!obstacle.Active||obstacle.X+obstacle.Width<run.PlayerX-60)continue;
            float until=Math.Max(0,(obstacle.X-run.PlayerX-60)/Math.Max(1,-obstacle.VelocityX));
            if(obstacle.Kind==BossAttackKind.AirGate&&until<3)
                Forbid(obstacle.Y-RunnerSession.StandingHeight-8,obstacle.Y+obstacle.Height+8);
            else if(obstacle.Kind==BossAttackKind.AirMine&&until<2)
                Forbid(obstacle.Y-RunnerSession.StandingHeight-12,obstacle.Y+obstacle.Height+12);
        }
        foreach(BossTelegraph mark in run.BossTelegraphs)
        {
            if(!mark.Visible)continue;
            if(mark.Kind==BossAttackKind.AirGate)
            {Forbid(-1000,mark.GapY+8);Forbid(mark.GapY+mark.GapHeight-RunnerSession.StandingHeight-8,1000);}
            else if(mark.Kind==BossAttackKind.CrossMissile&&mark.GapHeight>0)
            {Forbid(-1000,mark.GapY+8);Forbid(mark.GapY+mark.GapHeight-RunnerSession.StandingHeight-8,1000);}
            else if(mark.Kind==BossAttackKind.AirLaser)
                Forbid(mark.Y-RunnerSession.StandingHeight-14,mark.Y+mark.Height+14);
            else if(mark.Kind==BossAttackKind.GunshipDive)
                Forbid(mark.Y-RunnerSession.StandingHeight-12,mark.Y+mark.Height+12);
            else if(mark.Kind==BossAttackKind.MissileRain&&mark.GapWidth>0)
            {
                float targetX=mark.GapX+mark.GapWidth*.5f;
                if(run.PlayerX<mark.GapX+65||run.PlayerX>mark.GapX+mark.GapWidth-65)
                    run.SetHorizontal(Math.Max(-1,Math.Min(1,(targetX-run.PlayerX)/(RunnerSession.HorizontalSpeed/120))));
            }
        }
        BossEncounter boss=run.CurrentBoss;
        if(boss.ContactActive&&boss.AttackKind==BossAttackKind.GunshipDive&&
            boss.ContactX+boss.ContactWidth>run.PlayerX-65&&boss.ContactX<run.PlayerX+650)
            Forbid(boss.ContactY-RunnerSession.StandingHeight-12,boss.ContactY+boss.ContactHeight+12);
        foreach(BossProjectile p in run.BossProjectiles)
        {
            if(p.Friendly||p.Removed||p.VelocityX==0)continue;
            float until=HorizontalThreatTimeV11(run,p);
            if(until>2.3f)continue;
            float y=p.Y+p.VelocityY*until;
            Forbid(y-RunnerSession.StandingHeight-8,y+p.Height+8);
        }
        float best=float.PositiveInfinity;
        for(float candidate=25;candidate<=255;candidate+=2)
        {
            if(forbidden.Any(d=>candidate>d.Item1&&candidate<d.Item2))continue;
            float cost=Math.Abs(candidate-run.PlayerY)+Math.Abs(candidate-145)*.06f;
            if(cost<best){best=cost;target=candidate;}
        }
        float desiredVelocity=Math.Max(-200,Math.Min(200,(target-run.PlayerY)*3.8f));
        run.SetFlightLift(run.VelocityY<desiredVelocity);
    }
}
