using System;
using System.Collections.Generic;
using System.Linq;
using CityDino;

internal static partial class Program
{
    private static readonly BossKind[] LegacyBossKindsV13={BossKind.Tank,BossKind.Gunship,BossKind.Sentinel};
    private static BossKind LegacyBossKindV13(int number){return number%3==1?BossKind.Tank:number%3==2?BossKind.Gunship:BossKind.Sentinel;}
    private static BossKind NormalBossKindV13(int number){return number==1?BossKind.Tank:number==2?BossKind.Gunship:number==3?BossKind.Submarine:BossKind.Sentinel;}
    private static float BossBaseHpV13(BossKind kind){return kind==BossKind.Tank?750:kind==BossKind.Gunship?800:kind==BossKind.Sentinel?840:900;}
    private static void RegisterV13Tests()
    {
        Test("normal milestones start at C3 and four-boss rounds scale while practice remains exact",NormalDifficultyV13);
        Test("run missile columns launch simultaneously with variable locked vertical gaps",RunMissileSignalsV13);
        Test("run missile collisions use real circles and respect shield guard dash and fatal recording",RunMissileContactsV13);
        Test("run missile state freezes on pause and death and clears for arenas with return grace",RunMissileLifecycleV13);
        Test("run missile corridors drain terrain while preserving natural supplies and avoid night blackout",RunMissileCorridorsV13);
        Test("vertical missile gaps have real timed jump paths for all characters at low and late speed",RunMissilePathsV13);
        Test("submarine entry equips a helmet then jumps and dives without running clocks",UnderwaterJourneyV13);
        Test("swimming uses held rise dive and stroke while pause and death freeze the physical pose",SwimControlV13);
        Test("submarine attacks have completed signals, round mines and finite lifecycle",UnderwaterSignalsV13);
        Test("submarine practice and surface exit preserve every saved field and next-run stock",UnderwaterPracticeV13);
        Test("naval mines use round real contact and ordinary shots can destroy the obstruction",UnderwaterMineContactV13);
    }
    private static void NormalDifficultyV13()
    {
        foreach(int number in new[]{1,2,3,4,5,8,9,12})
        {
            var run=EnterBossFixture(number);int tier=3+(number-1)/4;
            Check(run.BossTier==tier&&run.CurrentBoss.Tier==tier,"Normal boss did not begin at C3 / progress by four encounters");
            if(number<=4)Check(run.CurrentBoss.Kind==NormalBossKindV13(number),"Submarine was not inserted between aircraft and robot");
            Near(run.CurrentBoss.MaxHealth/BossBaseHpV13(run.CurrentBoss.Kind),1+.18f*(tier-1),.001f,"Actual normal HP did not scale with C3 start");
        }
        foreach(int tier in new[]{1,2,3,4,6,8})
        {
            var run=TierPracticeV12(BossKind.Submarine,tier);Check(run.BossTier==tier,"Practice requested tier was offset by normal C3 start");
            Check(run.BossRepeatCount==(tier<=4?tier:tier==6?5:6),"Later tier repeat budget stopped growing beyond C4");
        }
    }
    private static void RunMissileBotV13(RunnerSession run,float timingOffset=0f)
    {
        if(run.IsBossArena)return;
        var near=run.RunMissiles.Where(m=>!m.Removed&&m.X+m.Width>run.PlayerX-60).OrderBy(m=>m.X).FirstOrDefault();
        if(near==null)return;
        if(near.GapHeight>0)
        {
            float untilCentre=(near.X+near.Width*.5f-run.PlayerX)/Math.Max(1,-near.VelocityX+run.HorizontalVelocity);
            float lead=(near.GapY<70?.18f:near.GapY<100?.27f:.35f)+timingOffset;
            run.SetDucking(false);
            if(untilCentre<lead&&run.PlayerY<=.01f)run.Jump();
            return;
        }
        float until=(near.X-run.PlayerX-48)/Math.Max(1,-near.VelocityX+run.HorizontalVelocity);
        if(until<.26f&&run.PlayerY<=.01f){run.SetDucking(false);run.Jump();}
        else if(run.VelocityY<0&&run.PlayerY<70&&until<.22f){run.SetDucking(false);run.Jump();}
    }
    private static void SwimBotInputV13(RunnerSession run)
    {
        run.SetDucking(false);
        float targetX=run.PlayerX,targetY=145;
        var forbidden=new List<Tuple<float,float>>();
        void Forbid(float low,float high){forbidden.Add(Tuple.Create(low,high));}
        BossTelegraph gap=run.BossTelegraphs.Where(m=>m.Visible&&!m.Active&&m.GapWidth>0).OrderBy(m=>m.Remaining).FirstOrDefault();
        if(gap!=null)targetX=gap.GapX+gap.GapWidth*.5f;
        foreach(var mark in run.BossTelegraphs)
        {
            if(!mark.Visible)continue;
            if((mark.Kind==BossAttackKind.Torpedo||mark.Kind==BossAttackKind.FishShoal)&&mark.GapHeight>0)
            {Forbid(-1000,mark.GapY+8);Forbid(mark.GapY+mark.GapHeight-RunnerSession.StandingHeight-8,1000);}
            if(mark.Kind==BossAttackKind.SonarBeam&&(mark.Active||mark.Remaining<1.4f))Forbid(mark.Y-RunnerSession.StandingHeight-12,mark.Y+mark.Height+12);
            if(mark.Kind==BossAttackKind.SubmarineRam)Forbid(mark.Y-RunnerSession.StandingHeight-14,mark.Y+mark.Height+14);
        }
        foreach(var obstacle in run.BossObstacles)
        {
            if(obstacle.Removed||!obstacle.Active||obstacle.Kind!=BossAttackKind.SeaMine)continue;
            if(Math.Abs(obstacle.X+obstacle.Width*.5f-targetX)<75)
                Forbid(obstacle.Y-RunnerSession.StandingHeight-22,obstacle.Y+obstacle.Height+22);
        }
        BossEncounter boss=run.CurrentBoss;
        if(boss.ContactActive&&boss.ContactX+boss.ContactWidth>run.PlayerX-60&&boss.ContactX<run.PlayerX+650)
            Forbid(boss.ContactY-RunnerSession.StandingHeight-14,boss.ContactY+boss.ContactHeight+14);
        foreach(var p in run.BossProjectiles)
        {
            if(p.Friendly||p.Removed)continue;
            if(p.VelocityX!=0)
            {
                float until=HorizontalThreatTimeV11(run,p);if(until>2.5f)continue;
                float y=p.Y+p.VelocityY*until;Forbid(y-RunnerSession.StandingHeight-10,y+p.Height+10);
            }
            else if(p.Kind==BossAttackKind.DepthCharge&&Math.Abs(p.X+p.Width*.5f-targetX)<80)
            {
                // A falling charge leaves its column unchanged. Move to a
                // genuine gap rather than inventing protection from above.
                targetX=run.PlayerX<p.X?Math.Max(100,p.X-95):Math.Min(720,p.X+95);
            }
        }
        float best=float.PositiveInfinity;
        for(float y=25;y<=255;y+=2)
        {
            if(forbidden.Any(f=>y>f.Item1&&y<f.Item2))continue;
            float cost=Math.Abs(y-run.PlayerY)+Math.Abs(y-145)*.06f;
            if(cost<best){best=cost;targetY=y;}
        }
        float dx=targetX-run.PlayerX;
        run.SetHorizontal(Math.Max(-1,Math.Min(1,dx/(RunnerSession.HorizontalSpeed/120))));
        float desired=Math.Max(-175,Math.Min(185,(targetY-run.PlayerY)*4f));
        run.SetSwimLift(run.VelocityY<desired);
        run.SetSwimDive(run.VelocityY>desired+25&&targetY<run.PlayerY-5);
        if(targetY>run.PlayerY+45&&run.VelocityY<30)run.Jump();
    }
    private static RunnerSession MissileFixtureV13(int seed=42,float age=0,CharacterKind character=CharacterKind.Classic,int hearts=0,bool shield=false)
    {
        var run=CharacterRun(character,District.Day,hearts,shield);
        SetProperty(run,"NextBossScore",int.MaxValue);
        SetProperty(run,"Elapsed",age);SetProperty(run,"Speed",RunnerSession.InitialSpeed+age*RunnerSession.SpeedAcceleration);
        SetField(run,"nextRunMissileAt",(double)age);
        return run;
    }
    private static void RunMissileSignalsV13()
    {
        var counts=new HashSet<int>();var gaps=new HashSet<float>();var trace=new List<string>();
        for(int seed=0;seed<12;seed++)
        {
            var run=new RunnerSession(null,seed);run.SetStartingDistrict(District.Day);run.StartRun();
            World(run).Clear();SetField(run,"distanceToPattern",1000000f);SetProperty(run,"NextBossScore",int.MaxValue);
            // Protected output measurement records count/cadence only. Real
            // unprotected trajectories are checked separately below.
            SetProperty(run,"InvincibleRemaining",1000f);
            var warned=new Dictionary<int,Tuple<float,int>>();var appeared=new HashSet<int>();
            for(int i=0;i<120*110;i++)
            {
                foreach(var mark in run.RunMissileWarnings)
                {
                    Check(mark.Count>=3&&mark.Count<=4&&mark.Duration==RunnerSession.RunMissileWarningDuration,"Invalid missile count or cue duration");
                    Check(mark.GapHeight==180&&mark.GapY>=55&&mark.GapY<=115,"Vertical column omitted a reachable locked opening");gaps.Add(mark.GapY);
                    if(!warned.ContainsKey(mark.VolleyId))warned.Add(mark.VolleyId,Tuple.Create(run.Elapsed-(mark.Duration-mark.Remaining),mark.Count));
                    Check(run.RunMissiles.All(m=>m.VolleyId!=mark.VolleyId),"Warning and damage overlapped before expiry");
                }
                foreach(var missile in run.RunMissiles)
                {
                    if(!appeared.Add(missile.VolleyId))continue;
                    Check(warned.ContainsKey(missile.VolleyId),"Missile volley lacked a visible warning");var mark=warned[missile.VolleyId];
                    Check(run.Elapsed-mark.Item1>=RunnerSession.RunMissileWarningDuration-.025f,"Missile fired before warning duration completed");
                    var wave=run.RunMissiles.Where(m=>m.VolleyId==missile.VolleyId).OrderBy(m=>m.Y).ToList();
                    Check(wave.Count==mark.Item2,"Volley did not launch its entire selected count simultaneously");counts.Add(wave.Count);
                    for(int n=0;n<wave.Count;n++)
                    {
                        Check(wave[n].Radius==11&&wave[n].VelocityX<0&&wave[n].Lifetime>0,"Missile live geometry invalid");
                        Near(wave[n].X,wave[0].X,.01f,"Volley is still a horizontal train instead of a simultaneous column");
                        Check(wave[n].Y+22<=wave[n].GapY||wave[n].Y>=wave[n].GapY+wave[n].GapHeight,"Missile occupied its locked escape gap");
                        if(n>0)Check(wave[n].Y>wave[n-1].Y,"Vertical rocket rows overlapped");
                    }
                    trace.Add(seed+":"+run.Elapsed.ToString("0.0")+":"+wave.Count);
                }
                Check(run.RunMissiles.Count<=4&&run.RunMissileWarnings.Count<=1,"Running missiles accumulated without bounds");run.Update(1f/120);
            }
        }
        Check(counts.SetEquals(new[]{3,4})&&gaps.SetEquals(new[]{55f,85f,115f}),"Natural seeded columns missed count/gap variation");
        Console.WriteLine("  Run missile output audit: "+trace.Count+" real vertical columns covered 3/4 rockets and all three locked gap heights.");
    }
    private static IList<RunMissile> MissilesV13(RunnerSession run){return (IList<RunMissile>)run.RunMissiles;}
    private static void AddMissileContactV13(RunnerSession run)
    {MissilesV13(run).Add(new RunMissile{Id=990001,VolleyId=1,X=run.PlayerX+10,Y=50,Width=40,Height=22,Radius=11,VelocityX=-10,Lifetime=2});}
    private static void RunMissileContactsV13()
    {
        var fatal=MissileFixtureV13();AddMissileContactV13(fatal);fatal.Update(.001f);Check(fatal.Lives==0&&fatal.State==RunState.GameOver&&fatal.Save.History.Count==1,"Real missile contact failed to record fatal hit");
        fatal.Update(2);Check(fatal.Save.History.Count==1,"Fatal missile recorded twice");
        var shield=MissileFixtureV13(shield:true);AddMissileContactV13(shield);shield.Update(.001f);Check(shield.Lives==1&&!shield.ShieldReady,"Missile ignored or failed to spend one starting shield");
        foreach(var character in new[]{CharacterKind.Triceratops,CharacterKind.Raptor})
        {var run=MissileFixtureV13(character:character);Check(run.TryUseSkill(),"Skill unavailable");AddMissileContactV13(run);run.Update(.001f);Check(run.Lives==1,"Character physical protection did not absorb missile");}
        var fast=MissileFixtureV13(age:3600,hearts:1);SetProperty(fast,"InvincibleRemaining",0f);
        MissilesV13(fast).Add(new RunMissile{Id=990002,VolleyId=1,X=fast.PlayerX+90,Y=50,Width=40,Height=22,Radius=11,VelocityX=-20000,Lifetime=1});
        fast.Update(.02f);Check(fast.Lives==1,"Adaptive collision tunneled a thin fast missile through actor");
    }
    private static void RunMissileLifecycleV13()
    {
        var run=MissileFixtureV13();run.Update(.01f);Check(run.RunMissileWarnings.Count==1,"Fixture cue missing");
        float cue=run.RunMissileWarnings[0].Remaining,clock=run.Elapsed;run.TogglePause();run.Update(3);Check(run.Elapsed==clock&&run.RunMissileWarnings[0].Remaining==cue,"Pause advanced a missile cue");run.TogglePause();
        SetProperty(run,"Distance",199999f);SetProperty(run,"Score",9999);SetProperty(run,"NextBossScore",10000);run.Update(1f/120);
        Check(run.IsBossArena&&run.RunMissiles.Count==0&&run.RunMissileWarnings.Count==0,"Boss entry retained a running rocket or warning");
        WinBookkeepingFixture(run);float start=run.Elapsed;
        for(int i=0;i<120*4;i++){run.Update(1f/120);Check(run.RunMissiles.Count==0&&run.RunMissileWarnings.Count==0,"Return grace launched an unseen missile");}
        Check(run.Elapsed>=start+3.9f,"Return grace fixture failed to advance normal time");run.ReturnToMenu();Check(run.RunMissiles.Count==0&&run.RunMissileWarnings.Count==0,"Menu kept run missile state");
        run.StartRun();Check(run.RunMissileWarnings.Count==0&&run.RunMissiles.Count==0,"Restart kept missile state");
    }
    private static void RunMissileCorridorsV13()
    {
        var run=MissileFixtureV13();var obstacle=new WorldEntity{Id=990005,Kind=EntityKind.Plane,X=700,Y=56,Width=100,Height=42};World(run).Add(obstacle);
        run.Update(.01f);Check(run.RunMissileCorridorReserved&&run.RunMissileWarnings.Count==0,"Rocket cue formed an impossible forced-duck combination");
        for(int i=0;i<120*5&&run.RunMissileWarnings.Count==0;i++){run.SetDucking(true);run.Update(1f/120);}
        Check(run.RunMissileWarnings.Count==1,"Missile corridor failed to start after old hazards naturally left");
        Check(run.Entities.All(e=>!Hazard(e)||e.Removed||e.X+e.Width<RunnerSession.MinimumPlayerX-100),"Live terrain remained in reserved missile corridor");
        var night=MissileFixtureV13();SetProperty(night,"CurrentDistrict",District.Night);SetProperty(night,"NextBlackoutAt",night.DistrictElapsed+1f);
        night.Update(.02f);Check(night.RunMissileWarnings.Count==0,"Missiles launched into a known approaching blackout");
        foreach(var district in new[]{District.Snow,District.Sunset})
        {
            var fed=MissileFixtureV13(hearts:1);SetProperty(fed,"CurrentDistrict",district);SetProperty(fed,"DistrictElapsed",3f);SetField(fed,"nextSurvivalSupplyAt",0d);fed.Update(.01f);
            Check(fed.RunMissileCorridorReserved&&fed.Entities.Any(e=>e.Kind==(district==District.Snow?EntityKind.HotDrink:EntityKind.Water)),"Missile corridor suppressed the required real survival supply");
            // A forced volley jump may miss an older drink. The replacement
            // waits for an actual landing and must still travel into the actor;
            // this fixture never credits resources or protection directly.
            var recovered=MissileFixtureV13();SetProperty(recovered,"CurrentDistrict",district);SetProperty(recovered,"DistrictElapsed",3f);
            SetProperty(recovered,district==District.Snow?"Warmth":"Water",35f);
            SetProperty(recovered,"PlayerY",80f);SetProperty(recovered,"VelocityY",0f);SetField(recovered,"grounded",false);
            MissilesV13(recovered).Add(new RunMissile{Id=990090,VolleyId=1,X=-150,Y=14,Width=40,Height=22,Radius=11,VelocityX=-530,Lifetime=1});
            recovered.Update(.02f);EntityKind drink=district==District.Snow?EntityKind.HotDrink:EntityKind.Water;
            Check(!recovered.Entities.Any(e=>e.Kind==drink)&&recovered.Lives==1&&recovered.InvincibleRemaining==0&&
                (district==District.Snow?recovered.Warmth:recovered.Water)<35f,"Post-volley opportunity manufactured a heal/protection before landing");
            WorldEntity replacement=null;
            for(int n=0;n<120&&replacement==null;n++){recovered.Update(1f/120);replacement=recovered.Entities.FirstOrDefault(e=>e.Kind==drink&&!e.Removed);}
            Check(replacement!=null&&replacement.X>recovered.PlayerX+65&&replacement.Y==10&&recovered.PlayerY==0,"Post-volley drink was not a real reachable pickup after landing");
            float resource=district==District.Snow?recovered.Warmth:recovered.Water;
            for(int n=0;n<120&&!replacement.Removed;n++)recovered.Update(1f/120);
            Check(replacement.Removed&&(district==District.Snow?recovered.WarmthRecoveryRemaining:recovered.WaterRecoveryRemaining)>0&&
                (district==District.Snow?recovered.Warmth:recovered.Water)<resource+1f,"Replacement did not require real pickup or instantly refilled the bar");
        }
    }
    private static void RunMissilePathsV13()
    {
        int cases=0;
        foreach(var character in new[]{CharacterKind.Classic,CharacterKind.Triceratops,CharacterKind.Raptor})
        foreach(int count in new[]{3,4})foreach(float gap in new[]{55f,85f,115f})
        foreach(float age in new[]{0f,600f,3600f})foreach(float x in new[]{100f,720f})
        foreach(float timingOffset in new[]{-.025f,0f,.025f})
        {
            var run=MissileFixtureV13(age:age,character:character);SetProperty(run,"PlayerX",x);SetField(run,"nextRunMissileAt",double.MaxValue);
            var warning=new RunMissileWarning{Id=990010,VolleyId=1,Count=count,GapY=gap,GapHeight=180};
            typeof(RunnerSession).GetMethod("LaunchRunMissileVolley",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(run,new object[]{warning});
            for(int i=0;i<120*5&&run.RunMissiles.Count>0;i++){RunMissileBotV13(run,timingOffset);run.Update(1f/120);Check(run.State==RunState.Playing&&run.Lives==1,"Missile column lacks real input path count="+count+" gap="+gap+" age="+age+" X="+x+" y="+run.PlayerY+" character="+character+" timing="+timingOffset);}
            Check(run.RunMissiles.Count==0,"Real missile path did not wait through the full volley");cases++;
        }
        Console.WriteLine("  Running vertical missile real one-heart timed-jump paths: "+cases+" character/gap/speed/position cases.");
    }
    private static void UnderwaterJourneyV13()
    {
        foreach(var character in Enum.GetValues(typeof(CharacterKind)).Cast<CharacterKind>())
        {
            var run=new RunnerSession(new RunnerSave{OwnsRaptor=true,OwnsTriceratops=true,SelectedCharacter=character},2);run.StartBossPractice(BossKind.Submarine,character,false,3);
            float elapsed=run.Elapsed,water=run.Water,warm=run.Warmth;bool helmet=false,dove=false,jumped=false;float lastY=run.PlayerY;
            for(int i=0;i<120*5&&run.ArenaPhase==BossPhase.Intro;i++)
            {run.Update(1f/120);helmet|=run.DivingHelmetEquipped;jumped|=run.DivingHelmetEquipped&&run.PlayerY>2&&run.WaterDepth<.05f;dove|=run.WaterDepth>.1f;Check(Math.Abs(run.PlayerY-lastY)<6,"Diver entrance teleported body");lastY=run.PlayerY;Check(run.Elapsed==elapsed&&run.Water==water&&run.Warmth==warm,"Diving entrance advanced normal clocks/resources");}
            Check(helmet&&jumped&&dove&&run.DivingHelmetEquipped&&run.WaterDepth==1&&run.SwimControlsEnabled&&!run.FlightControlsEnabled,"Diver did not equip, jump and enter actual underwater combat");
        }
    }
    private static void SwimControlV13()
    {
        var run=TierPracticeV12(BossKind.Submarine,3,protect:true);Check(run.DivingHelmetEquipped&&run.PlayerY>100,"Practice skip did not equip diving kit");float y=run.PlayerY;
        run.SetSwimLift(true);for(int i=0;i<40;i++)run.Update(1f/120);Check(run.PlayerY>y,"Held swim rise failed");
        run.SetSwimLift(false);run.SetSwimDive(true);y=run.PlayerY;for(int i=0;i<80;i++)run.Update(1f/120);Check(run.PlayerY<y,"Held dive failed");
        Check(run.Jump(),"Tap stroke failed");Check(!run.Jump(),"Stroke cooldown missing");run.TogglePause();y=run.PlayerY;float clock=run.BossElapsed;run.Update(3);Check(run.PlayerY==y&&run.BossElapsed==clock,"Pause advanced swimming");
        run.TogglePause();
        SetProperty(run,"PlayerY",180f);SetProperty(run,"VelocityY",0f);
        run.SetSwimLift(true);run.SetSwimDive(true);for(int i=0;i<60;i++)run.Update(1f/120);
        Check(run.PlayerY<180f,"Explicit downward dive did not take priority over held lift");
        run.SetSwimDive(false);run.SetSwimLift(true);for(int i=0;i<120*4;i++)run.Update(1f/120);Check(run.PlayerY<=RunnerSession.FlightMaximumY&&run.PlayerY>=RunnerSession.FlightMinimumY,"Swim bounds failed");
    }
    private static void UnderwaterSignalsV13()
    {
        var run=TierPracticeV12(BossKind.Submarine,3,protect:true);var warnings=new Dictionary<int,Tuple<float,float>>();var seen=new HashSet<int>();var kinds=new HashSet<BossAttackKind>();
        for(int i=0;i<120*160;i++)
        {
            foreach(var m in run.BossTelegraphs)if(m.Visible&&!m.Active&&!warnings.ContainsKey(m.Id)){warnings.Add(m.Id,Tuple.Create(run.BossElapsed-(m.Duration-m.Remaining),m.Duration));kinds.Add(m.Kind);}
            foreach(var p in run.BossProjectiles.Where(p=>!p.Friendly&&!p.Removed))if(seen.Add(p.Id)){Check(warnings.ContainsKey(p.SourceTelegraphId),"Naval shot has no visible signal");var w=warnings[p.SourceTelegraphId];Check(w.Item2>=.44&&run.BossElapsed-w.Item1>=w.Item2-.03,"Naval shot skipped cue");Check(p.Lifetime>0&&p.Radius>0,"Naval projectile omitted finite circular body");}
            foreach(var o in run.BossObstacles.Where(o=>!o.Removed))if(seen.Add(o.Id)){Check(warnings.ContainsKey(o.SourceTelegraphId)&&o.Radius>0&&o.Remaining>0,"Sea mine omitted signal/circular body/lifetime");}
            Check(run.BossProjectiles.Count<100&&run.BossObstacles.Count<30,"Naval attack count accumulated without limits");run.Update(1f/120);
        }
        foreach(var kind in new[]{BossAttackKind.Torpedo,BossAttackKind.FishShoal,BossAttackKind.SeaMine,BossAttackKind.SonarBeam,BossAttackKind.DepthCharge,BossAttackKind.SubmarineRam})Check(kinds.Contains(kind),"Submarine omitted "+kind);
        run.ReturnToMenu();Check(run.BossProjectiles.Count==0&&run.BossObstacles.Count==0&&!run.DivingHelmetEquipped,"Leaving water retained threats or diving kit");
    }
    private static void UnderwaterMineContactV13()
    {
        var hit=TierPracticeV12(BossKind.Submarine,3);ClearArenaThreats(hit);ArenaObstacles(hit).Clear();
        var mine=new BossObstacle{Id=990100,Kind=BossAttackKind.SeaMine,X=hit.PlayerX+20,Y=hit.PlayerY+45,
            Width=38,Height=38,Radius=19,Remaining=10,Health=24,MaxHealth=24};
        Check(RunnerGeometry.CircleTouchesPlayer(mine.X+19,mine.Y+19,19,hit),"Mine fixture missed opaque head/body");
        ArenaObstacles(hit).Add(mine);int lives=hit.Lives,wallet=hit.Save.Wallet;hit.Update(.0001f);
        Check(hit.Lives==lives-1&&mine.Removed&&hit.Save.Wallet==wallet,"Physical naval mine failed damage/removal or manufactured stars");
        var firing=TierPracticeV12(BossKind.Submarine,3);ClearArenaThreats(firing);ArenaObstacles(firing).Clear();
        var target=new BossObstacle{Id=990101,Kind=BossAttackKind.SeaMine,X=firing.PlayerX+180,Y=firing.PlayerY+35,
            Width=38,Height=38,Radius=19,Remaining=10,Health=24,MaxHealth=24};ArenaObstacles(firing).Add(target);
        float bossHp=firing.CurrentBoss.Health;firing.SetShooting(true);bool damaged=false;
        for(int i=0;i<120*4&&!target.Removed;i++)
        {ClearArenaThreats(firing);firing.Update(1f/120);damaged|=target.Health<24;Check(firing.CurrentBoss.Health==bossHp,"A shot passed through a live mine to hit the submarine");}
        Check(damaged&&target.Removed&&target.Health==0,"Actual common gun could not destroy a naval mine");
    }

    private static void UnderwaterPracticeV13()
    {
        foreach(float x in new[]{100f,260f,720f})
        {
            var run=new RunnerSession(StockedSaveV10(),24);string saved=SaveSnapshotV10(run.Save);int saves=0;run.SaveChanged+=()=>saves++;
            run.StartBossPractice(BossKind.Submarine,CharacterKind.Raptor,false,8);run.SkipPracticeIntro();SetProperty(run,"PlayerX",x);SetProperty(run,"PlayerY",260f);
            // Isolate victory/surface bookkeeping, not a full fight proof.
            run.CurrentBoss.Health=0;run.Update(1f/120);bool crossed=false;
            for(int i=0;i<120*6&&run.State==RunState.Playing;i++)
            {run.Update(1f/120);if(run.DepartureWorldX>=RunnerSession.BossExitGateX){crossed=true;Check(run.PlayerY==0&&run.WaterDepth==0&&!run.DivingHelmetEquipped,"Submarine victory reached far gate without surfacing/removing helmet");}}
            Check(crossed&&run.PracticeWon&&run.PracticeFinished&&SaveSnapshotV10(run.Save)==saved&&saves==0,"Diving practice victory changed saved progress/stock");
        }
    }
}
