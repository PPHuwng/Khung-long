using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityDino;

internal static partial class Program
{
    private static bool NeedsLowAirExtensionV9(RunnerSession run)
    {
        if(run.PlayerY<=0||run.VelocityY>=0)return false;
        float velocity=run.VelocityY;
        float untilLow=(velocity+(float)Math.Sqrt(velocity*velocity+2*RunnerSession.Gravity*Math.Max(0,run.PlayerY-32)))/RunnerSession.Gravity;
        float untilLand=(velocity+(float)Math.Sqrt(velocity*velocity+2*RunnerSession.Gravity*run.PlayerY))/RunnerSession.Gravity;
        foreach(BossTelegraph mark in run.BossTelegraphs)
        {
            if(!mark.Visible||mark.Kind!=BossAttackKind.LaserLow)continue;
            float start=mark.Active?0:mark.Remaining;
            float end=mark.Active?mark.Remaining:mark.Remaining+.32f;
            if(untilLow<end+.015f&&untilLand>start-.03f)return true;
        }
        return false;
    }

    private static bool NeedsGroundAirExtensionV9(RunnerSession run)
    {
        if(run.PlayerY<=0||run.VelocityY>=0)return false;
        foreach(BossProjectile shot in run.BossProjectiles)
        {
            if(shot.Friendly||shot.Removed||(shot.Kind!=BossAttackKind.Cannon&&shot.Kind!=BossAttackKind.Shockwave&&shot.Kind!=BossAttackKind.DroneLow))continue;
            if(shot.X+shot.Width<=run.PlayerX-55||shot.X>=run.PlayerX+110)continue;
            float clearance=shot.Y+shot.Height+5f;
            float dy=Math.Max(0,run.PlayerY-clearance);
            float timeDown=(run.VelocityY+(float)Math.Sqrt(run.VelocityY*run.VelocityY+2*RunnerSession.Gravity*dy))/RunnerSession.Gravity;
            float relative=-shot.VelocityX+run.HorizontalVelocity;
            float timeClear=relative>1f?(shot.X+shot.Width-(run.PlayerX-55f))/relative:100f;
            if(timeClear>timeDown+.02f)return true;
        }
        return false;
    }

    private static void RegisterV9Tests()
    {
        Test("boss bodies enter, patrol and move without frame-partition drift",BossBodyMotionV9);
        Test("three boss rage levels shorten cycles and add actual attack waves",BossRageDensityV9);
        Test("boss hits and firing create timed feedback that freezes on pause and death",BossFeedbackV9);
        Test("victory and restart cancel delayed boss waves before they can respawn",BossPendingCancellationV9);
        Test("late enraged boss patterns have real evasive paths from both screen bounds",BossLatePathsV9);
    }

    // These isolated movement/pattern measurements deliberately protect the
    // player. They measure physical production output, not survival. The full
    // health Classic fights and both-bound late paths do not use protection.
    private static RunnerSession IsolatedBossPattern(int number,float fraction)
    {
        RunnerSession run=EnterBossFixture(number);
        run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*fraction;
        SetProperty(run,"InvincibleRemaining",10000f);
        ReachFighting(run);
        return run;
    }

    private static void BossBodyMotionV9()
    {
        foreach(int number in new[]{1,2,3})
        {
            RunnerSession run=EnterBossFixture(number);
            BossEncounter boss=run.CurrentBoss;float arrival=boss.X;
            run.Update(.2f);float during=boss.X;
            Check(arrival>RunnerSession.CanvasWidth&&during<arrival,"Boss did not visibly enter from outside: "+boss.Kind);
            SetProperty(run,"InvincibleRemaining",10000f);ReachFighting(run);
            float minX=boss.X,maxX=boss.X,minY=boss.Y,maxY=boss.Y;
            for(int i=0;i<120*20;i++)
            {
                run.Update(1f/120);
                minX=Math.Min(minX,boss.X);maxX=Math.Max(maxX,boss.X);
                minY=Math.Min(minY,boss.Y);maxY=Math.Max(maxY,boss.Y);
                bool charging=IsChargeV11(boss.AttackKind)&&(boss.Action==BossAction.Strike||boss.Action==BossAction.Recovery);
                float minimum = charging && boss.FullSweep ? boss.SweepEndX - .01f : charging ? 99.99f : 809.99f;
                Check(boss.X>=minimum&&boss.X+boss.Width<=RunnerSession.CanvasWidth-15.9f,
                    "Boss body invaded player lane or left screen: "+boss.Kind+"@"+boss.X);
                Check(boss.Y>=-.01f,"Boss body sank below the flat arena");
                if(boss.Kind==BossKind.Gunship&&!charging)Check(RunnerSession.GroundY-boss.Y-boss.Height>=214f,"Patrolling gunship body rose behind the combat HUD");
            }
            Check(maxX-minX>40f,"Boss still stands at one horizontal position: "+boss.Kind);
            if(boss.Kind==BossKind.Sentinel)Check(maxY-minY>40,"Sentinel never made a visible stomp jump");
            if(boss.Kind==BossKind.Gunship)Check(maxY-minY>60,"Gunship failed to sweep vertically");
            run.TogglePause();string snapshot=BossFeedbackSnapshot(run);run.Update(4);
            Check(BossFeedbackSnapshot(run)==snapshot,"Paused physical boss body moved");

            RunnerSession a=IsolatedBossPattern(number,.9f),b=IsolatedBossPattern(number,.9f);
            for(int i=0;i<50;i++)a.Update(.1f);
            for(int i=0;i<250;i++)b.Update(.02f);
            Near(a.CurrentBoss.X,b.CurrentBoss.X,.35f,"Body X drifted with frame partition "+boss.Kind);
            Near(a.CurrentBoss.Y,b.CurrentBoss.Y,.35f,"Body Y drifted with frame partition "+boss.Kind);
            Near(a.BossElapsed,b.BossElapsed,.01f,"Combat clock drifted with frame partition");
        }
    }

    private sealed class PatternCount
    {
        public int Dangers,Warnings,HiddenWarnings;
        public float MeanCycle;
    }
    private static PatternCount MeasureBossPattern(int number,float fraction,int expectedRage)
    {
        RunnerSession run=IsolatedBossPattern(number,fraction);
        var live=new HashSet<int>();var warnings=new HashSet<int>();var hidden=new HashSet<int>();
        var cycles=new List<float>();float previous=run.CurrentBoss.AttackProgress,last=run.BossElapsed;
        var regularCycles=new List<float>();
        for(int i=0;i<120*70;i++)
        {
            foreach(BossTelegraph mark in run.BossTelegraphs)
            {
                if(!mark.Visible){hidden.Add(mark.Id);continue;}
                if(mark.Active)live.Add(mark.Id);
                else {warnings.Add(mark.Id);Check(mark.Duration>=.65f,"Rage shortened warning below reaction floor");}
            }
            foreach(BossProjectile p in run.BossProjectiles)if(!p.Friendly&&!p.Removed)live.Add(p.Id);
            foreach(BossObstacle o in run.BossObstacles)if(o.Active&&!o.Removed)live.Add(o.Id);
            if(run.CurrentBoss.ContactActive)live.Add(run.CurrentBoss.ChargeSourceTelegraphId);
            if((int)run.CurrentBoss.AttackKind<(int)BossAttackKind.TankRam)
                regularCycles.Add((float)typeof(RunnerSession).GetField("bossActionCycle",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(run));
            run.Update(1f/120);
            Check(run.CurrentBoss.RageLevel==expectedRage,"Wrong shared health phase for "+run.CurrentBoss.Kind+" at "+fraction);
            float progress=run.CurrentBoss.AttackProgress;
            if(previous>.8f&&progress<.2f){cycles.Add(run.BossElapsed-last);last=run.BossElapsed;}
            previous=progress;
        }
        Check(cycles.Count>=3&&live.Count>=3,"Pattern measurement did not observe sustained combat");
        return new PatternCount {Dangers=live.Count,Warnings=warnings.Count,HiddenWarnings=hidden.Count,
            MeanCycle=regularCycles.Average()};
    }
    private static void BossRageDensityV9()
    {
        foreach(int number in new[]{1,2,3})
        {
            PatternCount easy=MeasureBossPattern(number,.9f,1),middle=MeasureBossPattern(number,.5f,2),hard=MeasureBossPattern(number,.2f,3);
            Check(easy.MeanCycle>middle.MeanCycle+.2f&&middle.MeanCycle>hard.MeanCycle+.2f,
                "Rage levels did not make attack cycles faster for boss "+number);
            Check(middle.Dangers>=easy.Dangers&&hard.Dangers>easy.Dangers,
                "Later rage does not add actual danger waves for boss "+number+": "+easy.Dangers+"/"+middle.Dangers+"/"+hard.Dangers);
            Check(hard.Warnings>easy.Warnings&&hard.HiddenWarnings>0,
                "A stronger combination lost its separate delayed warnings");
            Console.WriteLine("  Boss "+number+" rage1/2/3: "+easy.Dangers+"/"+middle.Dangers+"/"+hard.Dangers+
                " live hazards over70s, regular cycles "+easy.MeanCycle.ToString("0.00")+"/"+middle.MeanCycle.ToString("0.00")+"/"+hard.MeanCycle.ToString("0.00")+"s (special attacks retain longer recovery).");
        }
    }

    private static string BossFeedbackSnapshot(RunnerSession run)
    {
        BossEncounter b=run.CurrentBoss;
        return b==null?"none":string.Join("|",run.ArenaPhase,run.BossElapsed,b.X,b.Y,b.RageLevel,b.HitFlashRemaining,
            b.ShotFlashRemaining,b.LandingFlashRemaining,b.Recoil,b.AttackProgress,b.Health,
            string.Join(";",run.BossTelegraphs.Select(m=>m.Id+":"+m.Remaining+":"+m.DelayRemaining+":"+m.Active)));
    }
    private static void BossFeedbackV9()
    {
        RunnerSession run=IsolatedBossPattern(1,.9f);run.SetShooting(true);
        bool fired=false,hit=false,recoil=false;
        for(int i=0;i<120*8;i++)
        {
            run.Update(1f/120);BossEncounter b=run.CurrentBoss;
            fired|=b.ShotFlashRemaining>0;hit|=b.HitFlashRemaining>0;recoil|=b.Recoil>.1f;
            if(fired&&hit&&recoil)break;
        }
        Check(fired&&hit&&recoil,"Real shots/hits do not produce firing, impact and recoil feedback");
        run.TogglePause();string paused=BossFeedbackSnapshot(run);run.Update(5);
        Check(BossFeedbackSnapshot(run)==paused,"Pause advanced boss impact/muzzle feedback");
        run.TogglePause();
        typeof(RunnerSession).GetMethod("FinishRun",BindingFlags.NonPublic|BindingFlags.Instance).Invoke(run,new object[]{"Feedback death fixture"});
        string dead=BossFeedbackSnapshot(run);run.Update(5);
        Check(BossFeedbackSnapshot(run)==dead,"Death allowed a body/effect clock to continue");
        run.StartRun();Check(!run.IsBossArena&&run.CurrentBoss==null,"Restart retained dead animation state");
    }

    private static void BossPendingCancellationV9()
    {
        RunnerSession run=IsolatedBossPattern(3,.2f);
        bool pending=false;
        for(int i=0;i<120*7;i++)
        {
            if(run.BossTelegraphs.Any(m=>!m.Visible)){pending=true;break;}
            run.Update(1f/120);
        }
        Check(pending,"Fixture did not capture a real staged multi-wave attack");
        run.CurrentBoss.Health=1;run.SetShooting(true);
        for(int i=0;i<120*12&&run.ArenaPhase==BossPhase.Fighting;i++)run.Update(1f/120);
        Check(run.ArenaPhase==BossPhase.Victory,"Real player shot failed to end staged-wave fixture");
        int wallet=run.Save.Wallet;float x=run.CurrentBoss.X,y=run.CurrentBoss.Y;
        for(int i=0;i<120;i++)
        {
            run.Update(1f/120);
            Check(run.BossProjectiles.Count==0&&run.BossTelegraphs.Count==0,"A cancelled delayed wave respawned during victory");
            Near(run.CurrentBoss.X,x,0,"Dead boss body continued its patrol");Near(run.CurrentBoss.Y,y,0,"Dead boss body continued hovering");
        }
        for(int i=0;i<120*5&&run.IsBossArena;i++)run.Update(1f/120);
        Check(!run.IsBossArena&&run.Save.Wallet==wallet&&run.BossProjectiles.Count==0&&run.BossTelegraphs.Count==0,
            "Return preserved delayed combat objects or duplicated reward");
        run.StartRun();Check(run.BossProjectiles.Count==0&&run.BossTelegraphs.Count==0&&!run.IsBossArena,"New run retained a staged attack");
    }

    private static void BossLatePathsV9()
    {
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        foreach(float x in new[]{RunnerSession.MinimumPlayerX,RunnerSession.MaximumPlayerX})
        {
            RunnerSession run=EnterBossFixture(kind==BossKind.Tank?7:kind==BossKind.Gunship?8:9,startX:x);
            run.CurrentBoss.Kind=kind;
            run.CurrentBoss.Width=kind==BossKind.Sentinel?172:kind==BossKind.Gunship?270:250;
            run.CurrentBoss.Height=kind==BossKind.Sentinel?225:kind==BossKind.Gunship?130:150;
            run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*.2f;
            // V10 scripts the approach; combat begins in its actual arrival lane.
            // Testing either initial edge here checks that scripted travel lands safely.
            Near(run.PreBossPlayerX,x,.001f,"Milestone did not capture initial edge"); ReachFighting(run);
            for(int i=0;i<120*15;i++)
            {
                CombatBotInput(run,run.BossObstacles.Any(o=>o.Destructible&&o.X>run.PlayerX&&o.X<run.PlayerX+500),true);run.Update(1f/120);
                Check(run.State==RunState.Playing&&run.Lives==1,"Late route took damage "+kind+" edge="+x+" t="+run.BossElapsed+" "+ArenaDescription(run));
            }
        }
    }
}
