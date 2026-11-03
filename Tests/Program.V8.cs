using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityDino;

internal static partial class Program
{
    private static void RegisterV8Tests()
    {
        Test("characters unlock permanently at exact prices and selection respects run ownership", CharacterShopV8);
        Test("character save migration keeps old progress and sanitizes locked selections", CharacterSaveV8);
        Test("guard and dash use real physical contact, cooldown and pause rules", CharacterSkillsV8);
        Test("character skills cannot rescue falls or thermal depletion", CharacterSkillLimitsV8);
        Test("boss thresholds use displayed boosted score and shooting is restricted to combat", BossThresholdsV8);
        Test("arena freezes running clocks and resources while combat skills still tick", BossFreezeV8);
        Test("boss death records one result and restart clears the whole encounter", BossDeathV8);
        Test("boss victories award stars once and return to a safe preserved run", BossRewardsV8);
        Test("all three characters can shoot and only armored character reflects boss shots", BossCharacterCombatV8);
        Test("boss milestones rotate after the first three and stop safely at the score limit", BossMilestoneSequenceV8);
        Test("low-resource boss returns preserve resources and offer a real replacement drink", BossSurvivalReturnV8);
        Test("boss attack pace is independent of the stored unlimited running speed", BossIndependentPaceV8);
        Test("boss attacks are telegraphed and become real shared damage objects", BossAttackSignalsV8);
        Test("free classic can defeat all three full-health bosses with real shots and evasive input", BossClassicVictoriesV8);
    }

    private static RunnerSession CharacterRun(CharacterKind character, District district=District.Day, int hearts=0, bool stockShield=false)
    {
        var save=new RunnerSave { OwnsTriceratops=true, OwnsRaptor=true, SelectedCharacter=character,
            PurchasedHearts=hearts, PurchasedShield=stockShield };
        var run=new RunnerSession(save,251); run.SetStartingDistrict(district); run.StartRun();
        World(run).Clear(); StopSpawns(run); return run;
    }

    private static void CharacterShopV8()
    {
        var run=new RunnerSession(new RunnerSave { Wallet=7499 },8); int saves=0;run.SaveChanged+=()=>saves++;
        Check(run.OwnsCharacter(CharacterKind.Classic)&&!run.OwnsCharacter(CharacterKind.Triceratops)&&!run.OwnsCharacter(CharacterKind.Raptor),"Default character ownership wrong");
        Check(!run.TryUseSkill()&&!run.SelectCharacter(CharacterKind.Raptor)&&!run.BuyCharacter((CharacterKind)99),"Locked or invalid character accepted");
        Check(run.BuyCharacter(CharacterKind.Triceratops)&&run.Save.Wallet==4999&&run.Save.OwnsTriceratops,"Triceratops purchase wrong");
        Check(run.CurrentCharacter==CharacterKind.Triceratops&&run.Save.SelectedCharacter==CharacterKind.Triceratops,"Purchase did not equip next character");
        Check(!run.BuyCharacter(CharacterKind.Triceratops)&&!run.BuyCharacter(CharacterKind.Raptor)&&run.Save.Wallet==4999,"Duplicate/unfunded character purchase changed wallet");
        run.Save.Wallet++;Check(run.BuyCharacter(CharacterKind.Raptor)&&run.Save.Wallet==0,"Exact Raptor price rejected");
        Check(run.SelectCharacter(CharacterKind.Classic)&&run.SelectCharacter(CharacterKind.Raptor),"Owned selection failed");
        run.StartRun();Check(run.CurrentCharacter==CharacterKind.Raptor,"Run did not use equipped character");
        Check(!run.SelectCharacter(CharacterKind.Classic)&&!run.BuyCharacter(CharacterKind.Triceratops),"Playing allowed character changes");
        run.TogglePause();Check(!run.SelectCharacter(CharacterKind.Classic),"Pause allowed character changes");
        run.TogglePause();Inject(run,EntityKind.House,0,140,150);run.Update(1f/120);
        Check(run.State==RunState.GameOver&&run.SelectCharacter(CharacterKind.Classic),"Game over failed to prepare next selection");
        Check(run.CurrentCharacter==CharacterKind.Raptor,"Next selection replaced frozen death character");
        run.StartRun();Check(run.CurrentCharacter==CharacterKind.Classic&&run.Save.OwnsRaptor&&run.Save.OwnsTriceratops,"Restart lost permanent ownership");
        Check(saves>=5,"Character purchases/selections failed to notify persistence");
    }

    private static void CharacterSaveV8()
    {
        var old=new RunnerSave { Version=3,Wallet=2381,BestScore=17000,LastScore=9000,PurchasedHearts=2,
            ScoreBoostLevel=2,MagnetLevel=1,History=new List<RunRecord>{new RunRecord{Score=17000,Stars=23,Distance=1234}} };
        var migrated=new RunnerSession(old,1);
        Check(migrated.Save.Version==4&&old.Wallet==2381&&old.BestScore==17000&&old.LastScore==9000&&old.PurchasedHearts==2&&old.History.Count==1,"V3 migration discarded or refunded existing progress");
        Check(migrated.CurrentCharacter==CharacterKind.Classic&&!old.OwnsRaptor&&!old.OwnsTriceratops,"Old save received locked character");
        var bad=new RunnerSession(new RunnerSave{SelectedCharacter=CharacterKind.Raptor},1);
        Check(bad.Save.SelectedCharacter==CharacterKind.Classic,"Locked saved character bypassed ownership");
        bad=new RunnerSession(new RunnerSave{SelectedCharacter=(CharacterKind)99,OwnsRaptor=true},1);
        Check(bad.Save.SelectedCharacter==CharacterKind.Classic&&bad.Save.OwnsRaptor,"Invalid selection destroyed valid ownership");
        var options=new System.Text.Json.JsonSerializerOptions{IncludeFields=true};
        old.OwnsRaptor=true;old.SelectedCharacter=CharacterKind.Raptor;
        string data=System.Text.Json.JsonSerializer.Serialize(old,options);
        var loaded=System.Text.Json.JsonSerializer.Deserialize<RunnerSave>(data,options);
        var reload=new RunnerSession(loaded,2);
        Check(reload.CurrentCharacter==CharacterKind.Raptor&&reload.Save.Wallet==2381&&reload.Save.History.Count==1,"Serializable ownership/selection fields failed a reload");
    }

    private static void CharacterSkillsV8()
    {
        RunnerSession normal=CharacterRun(CharacterKind.Classic);
        Check(!normal.SkillReady&&!normal.TryUseSkill()&&normal.SkillCooldown==0,"Free Classic received a private skill");
        RunnerSession guard=CharacterRun(CharacterKind.Triceratops,hearts:1,stockShield:true);
        Check(guard.TryUseSkill()&&!guard.TryUseSkill()&&guard.SkillActiveRemaining==3&&guard.SkillCooldownRemaining==30,"Guard did not activate/cap correctly");
        Inject(guard,EntityKind.House,0,140,150);guard.Update(1f/120);
        Check(guard.Lives==2&&guard.SkillActiveRemaining==0&&guard.ShieldReady&&guard.InvincibleRemaining>0,"Guard failed one-hit absorption or spent stock shield first");
        Advance(guard,1.1f,false);Inject(guard,EntityKind.House,0,140,150);guard.Update(1f/120);
        Check(guard.Lives==2&&!guard.ShieldReady,"Stock shield did not remain available after manual guard");
        Advance(guard,1.1f,false);Inject(guard,EntityKind.House,0,140,150);guard.Update(1f/120);
        Check(guard.Lives==1&&!guard.TryUseSkill(),"Consumed guard remained effective during cooldown");
        RunnerSession dash=CharacterRun(CharacterKind.Raptor,hearts:1);SetProperty(dash,"PlayerX",450f);dash.SetHorizontal(-1);
        Check(dash.TryUseSkill()&&dash.IsDashing&&dash.DashVelocity<0,"Raptor ignored chosen dash direction");
        float x=dash.PlayerX;dash.Update(.1f);Near(dash.PlayerX,x-90,1f,"Dash did not use real horizontal movement");
        Inject(dash,EntityKind.House,0,140,150);dash.Update(1f/120);
        Check(dash.Lives==2,"Dash failed to protect actual physical contact");
        dash.SetHorizontal(0);Advance(dash,.35f,false);Check(!dash.IsDashing&&dash.SkillActiveRemaining==0&&!dash.TryUseSkill(),"Dash did not expire or honor cooldown");
        dash.TogglePause();float cooldown=dash.SkillCooldownRemaining;float px=dash.PlayerX;dash.Update(5);
        Near(dash.SkillCooldownRemaining,cooldown,0,"Pause advanced skill cooldown");Near(dash.PlayerX,px,0,"Paused dash moved");
        dash.TogglePause();Advance(dash,cooldown+.1f,false);Check(dash.SkillReady&&dash.TryUseSkill(),"Skill did not become ready again");
        dash.ReturnToMenu();dash.StartRun();Check(dash.CurrentCharacter==CharacterKind.Raptor&&dash.SkillReady&&!dash.IsDashing,"Restart retained active skill/cooldown");
    }

    private static void CharacterSkillLimitsV8()
    {
        foreach(CharacterKind kind in new[]{CharacterKind.Triceratops,CharacterKind.Raptor})
        {
            RunnerSession fall=CharacterRun(kind);SetProperty(fall,"PlayerY",-170f);SetProperty(fall,"VelocityY",-200f);SetField(fall,"grounded",false);
            Check(fall.TryUseSkill(),"Fixture skill unavailable");
            Inject(fall,EntityKind.Gap,0,4000,0);Advance(fall,.15f,false);
            Check(fall.State==RunState.GameOver,"Character skill rescued falling for "+kind);
            RunnerSession cold=CharacterRun(kind,District.Snow,1);SetProperty(cold,"DistrictElapsed",3f);SetProperty(cold,"Warmth",.01f);
            Check(cold.TryUseSkill(),"Cold skill unavailable");cold.Update(.02f);
            Check(cold.Lives==1,"Character skill prevented thermal depletion for "+kind);
        }
    }

    // Milestone fixtures place the runner just before a real displayed-score
    // threshold. Boss health, movement, attacks and projectiles remain production.
    private static RunnerSession EnterBossFixture(int number=1,CharacterKind character=CharacterKind.Classic,int hearts=0,District district=District.Day,float startX=RunnerSession.StartPlayerX)
    {
        RunnerSession run=CharacterRun(character,district,hearts);
        SetProperty(run,"PlayerX",startX);
        SetProperty(run,"BossNumber",number-1);SetProperty(run,"NextBossScore",number*10000);
        SetProperty(run,"Distance",number*200000f-1f);SetProperty(run,"Score",number*10000-1);
        run.Update(1f/120);
        Check(run.IsBossArena&&run.ArenaPhase==BossPhase.Intro,"Real score crossing did not enter boss intro at "+number);
        return run;
    }
    private static void ReachFighting(RunnerSession run)
    {
        for(int i=0;i<120*5&&run.ArenaPhase==BossPhase.Intro;i++)run.Update(1f/120);
        Check(run.ArenaPhase==BossPhase.Fighting,"Boss intro did not enter combat");
    }
    private static IList<BossProjectile> ArenaShots(RunnerSession run){return (IList<BossProjectile>)run.BossProjectiles;}
    private static IList<BossTelegraph> ArenaMarks(RunnerSession run){return (IList<BossTelegraph>)run.BossTelegraphs;}
    private static void ClearArenaThreats(RunnerSession run)
    {
        // Only clock/reward fixture isolation calls this; real boss victory and
        // signal/shape coverage below do not disable attacks or grant immunity.
        foreach(BossProjectile shot in run.BossProjectiles)if(!shot.Friendly)shot.Removed=true;
        for(int i=ArenaMarks(run).Count-1;i>=0;i--)ArenaMarks(run).RemoveAt(i);
    }
    private static BossProjectile EnemyAtPlayer(RunnerSession run)
    {
        return new BossProjectile { Id=900001,Friendly=false,Kind=BossAttackKind.Cannon,X=run.PlayerX-70,Y=0,
            Width=140,Height=150,VelocityX=0,VelocityY=0,Lifetime=1 };
    }

    private static void BossThresholdsV8()
    {
        RunnerSession outside=CharacterRun(CharacterKind.Classic);outside.SetShooting(true);
        Check(!outside.TryShoot(),"Normal running accepted shooting");outside.Update(.1f);
        Check(outside.BossProjectiles.Count==0&&!outside.IsBossArena,"Outside shooting created combat objects");
        RunnerSession run=EnterBossFixture();Check(run.CurrentBoss.Kind==BossKind.Tank&&run.BossNumber==1,"First milestone selected wrong boss");
        int score=run.Score;float distance=run.Distance;
        run.SetShooting(true);Check(!run.TryShoot(),"Intro accepted shooting");run.Update(.1f);
        Check(!run.BossProjectiles.Any(p=>p.Friendly)&&run.Score==score&&run.Distance==distance,"Intro fired shots or advanced running");
        ReachFighting(run);Check(run.TryShoot(),"Free Classic could not shoot boss");
        Check(run.BossProjectiles.Any(p=>p.Friendly)&&!run.TryShoot(),"Shot rate limit missing or no projectile created");
        RunnerSession boost=CharacterRun(CharacterKind.Classic);Inject(boost,EntityKind.DoubleScore,10,30,30);boost.Update(1f/120);
        SetProperty(boost,"Distance",199980f);SetProperty(boost,"Score",9999);SetField(boost,"bonusScoreDistance",17d);
        boost.Update(1f/120);
        Check(boost.IsBossArena&&boost.BossNumber==1,"Displayed x2 threshold failed to trigger boss");
        Check(boost.Distance<200000f,"Boss entry waited for unboosted distance instead of displayed boosted score");
    }

    private static void BossFreezeV8()
    {
        RunnerSession run=CharacterRun(CharacterKind.Triceratops,District.Sunset,2);
        SetProperty(run,"Water",42f);Inject(run,EntityKind.Water,10,32,36);Inject(run,EntityKind.DoubleScore,10,30,30);Inject(run,EntityKind.Magnet,10,30,30);run.Update(1f/120);
        Check(run.TryUseSkill(),"Pre-arena skill unavailable");
        SetProperty(run,"Distance",199999f);SetProperty(run,"Score",9999);run.Update(1f/120);
        Check(run.IsBossArena,"Freeze fixture did not enter arena");
        int score=run.Score,lives=run.Lives;float elapsed=run.Elapsed,distance=run.Distance,speed=run.Speed,stage=run.DistrictElapsed;
        float water=run.Water,warmth=run.Warmth,queue=run.WaterRecoveryRemaining,boost=run.ScoreBoostRemaining,magnet=run.MagnetRemaining,cd=run.SkillCooldownRemaining;
        for(int i=0;i<120*3;i++){ClearArenaThreats(run);run.Update(1f/120);}
        Check(run.State==RunState.Playing&&run.Lives==lives,"Isolated arena fixture unexpectedly lost a life");
        Check(run.Score==score&&run.Distance==distance&&run.Speed==speed&&run.Elapsed==elapsed&&run.DistrictElapsed==stage,"Arena advanced a running clock or score");
        Check(run.Water==water&&run.Warmth==warmth&&run.WaterRecoveryRemaining==queue&&run.ScoreBoostRemaining==boost&&run.MagnetRemaining==magnet,"Arena consumed resources or running power-ups");
        Check(run.SkillCooldownRemaining<cd-2.9f,"Combat time did not cool character skill");
        run.TogglePause();float boss=run.BossElapsed;cd=run.SkillCooldownRemaining;float phase=run.BossPhaseRemaining;
        run.Update(20);Check(run.BossElapsed==boss&&run.SkillCooldownRemaining==cd&&run.BossPhaseRemaining==phase,"Pause advanced encounter or skill");
    }

    private static void BossDeathV8()
    {
        RunnerSession run=EnterBossFixture();ReachFighting(run);ArenaShots(run).Add(EnemyAtPlayer(run));run.Update(1f/120);
        Check(run.State==RunState.GameOver&&run.Lives==0&&run.Save.History.Count==1,"Fatal boss projectile did not finish once");
        int score=run.Score;float clock=run.BossElapsed;run.Update(4);Check(run.Score==score&&run.BossElapsed==clock&&run.Save.History.Count==1&&!run.TryShoot()&&!run.TryUseSkill(),"Dead arena continued or recorded twice");
        run.StartRun();Check(!run.IsBossArena&&run.CurrentBoss==null&&run.BossProjectiles.Count==0&&run.BossTelegraphs.Count==0&&run.NextBossScore==10000&&run.BossNumber==0,"Restart retained encounter/milestone state");
    }

    private static void BossRewardsV8()
    {
        RunnerSession run=EnterBossFixture(1,hearts:2);ReachFighting(run);
        int wallet=run.Save.Wallet,stars=run.RunStars,lives=run.Lives,score=run.Score;float elapsed=run.Elapsed,speed=run.Speed,water=run.Water,stage=run.DistrictElapsed;
        // This fixture isolates one-shot reward/return bookkeeping. The separate
        // ClassicVictory tests below fight untouched full-health bosses.
        run.CurrentBoss.Health=1;run.SetShooting(true);
        for(int i=0;i<120*20&&run.ArenaPhase==BossPhase.Fighting;i++){ClearArenaThreats(run);run.Update(1f/120);}
        Check(run.ArenaPhase==BossPhase.Victory,"Actual friendly shot did not end reward fixture");
        Check(run.Save.Wallet==wallet+300&&run.RunStars==stars+300&&run.NextBossScore==20000,"Boss reward/milestone did not credit exactly once");
        for(int i=0;i<120*5&&run.IsBossArena;i++)run.Update(1f/120);
        Check(!run.IsBossArena&&run.Lives==lives&&run.Score==score&&run.Elapsed==elapsed&&run.Speed==speed&&run.DistrictElapsed==stage&&run.Water==water,"Victory return changed preserved run state or healed hearts");
        Check(run.InvincibleRemaining>0&&run.Entities.All(e=>!Hazard(e)||e.X>run.PlayerX+300),"Victory returned into an immediate hazard");
        run.SetShooting(false);for(int i=0;i<30;i++)run.Update(1f/120);
        Check(run.Save.Wallet==wallet+300&&run.BossNumber==1&&!run.IsBossArena,"Victory reward or arena retriggered before next threshold");
        SetProperty(run,"Distance",399999f);SetProperty(run,"Score",19999);run.Update(1f/120);
        Check(run.IsBossArena&&run.BossNumber==2&&run.CurrentBoss.Kind==BossKind.Gunship,"Second real milestone did not choose gunship");
    }

    private static void BossCharacterCombatV8()
    {
        foreach(CharacterKind character in Enum.GetValues(typeof(CharacterKind)))
        {
            RunnerSession run=EnterBossFixture(1,character);ReachFighting(run);
            Check(run.TryShoot()&&run.BossProjectiles.Any(p=>p.Friendly),"Character cannot shoot boss: "+character);
            if(character==CharacterKind.Classic)Check(!run.TryUseSkill(),"Classic received a separate skill");
            else Check(run.TryUseSkill(),"Purchased character lacks skill in arena: "+character);
        }
        RunnerSession guard=EnterBossFixture(1,CharacterKind.Triceratops);ReachFighting(guard);Check(guard.TryUseSkill(),"Combat guard unavailable");
        float hp=guard.CurrentBoss.Health;ArenaShots(guard).Add(EnemyAtPlayer(guard));guard.Update(1f/120);
        Check(guard.Lives==1&&guard.SkillActiveRemaining==0&&guard.CurrentBoss.Health<hp,"Guard did not reflect one enemy shot");
        hp=guard.CurrentBoss.Health;ArenaShots(guard).Add(EnemyAtPlayer(guard));guard.Update(1f/120);
        Near(guard.CurrentBoss.Health,hp,0,"Consumed guard reflected a second projectile");
    }

    private static float EscapeDirection(float x,float left,float width)
    {
        if(left+width+65>=RunnerSession.MaximumPlayerX)return -1;
        if(left-65<=RunnerSession.MinimumPlayerX)return 1;
        return x<left+width*.5f?-1:1;
    }

    private static void WinBookkeepingFixture(RunnerSession run)
    {
        // Isolate sequence/transition bookkeeping with a real friendly shot.
        // Full-health Classic victories below retain every production attack.
        ReachFighting(run);run.CurrentBoss.Health=1;run.SetShooting(true);
        for(int i=0;i<120*20&&run.ArenaPhase==BossPhase.Fighting;i++)
        {ClearArenaThreats(run);run.Update(1f/120);}
        Check(run.ArenaPhase==BossPhase.Victory,"Bookkeeping fixture failed to hit boss with a real shot");
        for(int i=0;i<120*5&&run.IsBossArena;i++)run.Update(1f/120);
        Check(!run.IsBossArena,"Victory never restored normal running");
        run.SetShooting(false);
    }

    private static void BossMilestoneSequenceV8()
    {
        RunnerSession run=CharacterRun(CharacterKind.Classic);
        var kinds=new List<BossKind>();
        for(int number=1;number<=12;number++)
        {
            SetProperty(run,"Distance",number*200000f-1f);SetProperty(run,"Score",number*10000-1);
            run.Update(1f/120);
            Check(run.IsBossArena&&run.BossNumber==number&&run.NextBossScore==(number+1)*10000,
                "Milestone skipped/retriggered at boss "+number);
            kinds.Add(run.CurrentBoss.Kind);
            if(number<=3)Check(run.CurrentBoss.Kind==(number==1?BossKind.Tank:number==2?BossKind.Gunship:BossKind.Sentinel),"First three boss order changed");
            WinBookkeepingFixture(run);
            run.Update(1f/120);
            Check(!run.IsBossArena&&run.BossNumber==number,"Cleared milestone immediately entered again");
        }
        for(int i=1;i<kinds.Count;i++)Check(kinds[i]!=kinds[i-1],"Boss bag repeated the previous boss at "+i);
        for(int i=3;i<12;i+=3)Check(kinds.Skip(i).Take(3).Distinct().Count()==3,"Boss bag repeated before covering all three kinds");

        RunnerSession limit=CharacterRun(CharacterKind.Classic);
        SetProperty(limit,"NextBossScore",int.MaxValue-1);
        SetProperty(limit,"Distance",(float)((double)int.MaxValue*20d+1024d));
        SetProperty(limit,"Score",int.MaxValue-1);limit.Update(1f/120);
        Check(limit.IsBossArena&&limit.NextBossScore>0,"Final milestone overflowed its score counter");
        WinBookkeepingFixture(limit);
        for(int i=0;i<12;i++)limit.Update(1f/120);
        Check(!limit.IsBossArena&&limit.BossNumber==1,"Maximum score retriggered a completed final milestone");
    }

    private static void BossSurvivalReturnV8()
    {
        foreach(District district in new[]{District.Snow,District.Sunset})
        {
            bool cold=district==District.Snow;
            RunnerSession run=CharacterRun(CharacterKind.Classic,district);
            SetProperty(run,"DistrictElapsed",3f);SetProperty(run,cold?"Warmth":"Water",0f);
            SetProperty(run,"Distance",199999f);SetProperty(run,"Score",9999);run.Update(1f/120);
            Check(run.IsBossArena&&(cold?run.Warmth:run.Water)==0f,"Boss entry healed or depleted the paused resource");
            WinBookkeepingFixture(run);
            Check((cold?run.Warmth:run.Water)==0f&&run.Lives==1,"Boss return manufactured a resource/heart heal");
            EntityKind drink=cold?EntityKind.HotDrink:EntityKind.Water;
            WorldEntity replacement=run.Entities.Single(e=>!e.Removed&&e.Kind==drink);
            Check(replacement.X>run.PlayerX+40&&replacement.Y==10,"Return drink spawned on top of the player or outside its lane");
            run.Update(.04f);
            Check(run.Lives==1&&(cold?run.Warmth:run.Water)==0f,
                "Return invulnerability silently refilled depleted resource to 45");
            for(int i=0;i<120&& !replacement.Removed;i++)run.Update(1f/120);
            Check(replacement.Removed&&(cold?run.WarmthRecoveryRemaining:run.WaterRecoveryRemaining)>0,
                "Return drink was not collected through real pickup contact");
            for(int i=0;i<120;i++)run.Update(1f/120);
            Check(run.State==RunState.Playing&&run.Lives==1&&(cold?run.Warmth:run.Water)>0,
                "Real gradual return supply failed to restore a sustainable positive resource");
        }
    }

    private static float[] CaptureBossPace(float runningElapsed)
    {
        RunnerSession run=CharacterRun(CharacterKind.Classic);
        SetProperty(run,"Elapsed",runningElapsed);SetProperty(run,"Speed",RunnerSession.InitialSpeed+runningElapsed*RunnerSession.SpeedAcceleration);
        SetProperty(run,"Distance",199999f);SetProperty(run,"Score",9999);run.Update(1f/120);ReachFighting(run);
        // Output-only timing measurement; survival is covered by full fights.
        SetProperty(run,"InvincibleRemaining",1000f);
        var starts=new List<float>();var seen=new HashSet<int>();float velocity=0,warning=0;
        for(int i=0;i<120*12;i++)
        {
            foreach(BossTelegraph mark in run.BossTelegraphs)
                if(mark.Visible&&!mark.Active&&seen.Add(mark.Id)){starts.Add(run.BossElapsed);warning=mark.Duration;}
            foreach(BossProjectile shot in run.BossProjectiles)
                if(!shot.Friendly&&shot.Kind==BossAttackKind.Cannon)velocity=shot.VelocityX;
            CombatBotInput(run,false);run.Update(1f/120);
            Check(run.State==RunState.Playing,"Pace audit unexpectedly stopped at stored run age "+runningElapsed);
        }
        Check(starts.Count>=3&&velocity<0,"Pace audit omitted actual attack cycles or cannon projectiles");
        return new[]{warning,velocity,starts[1]-starts[0],starts[2]-starts[1]};
    }

    private static void BossIndependentPaceV8()
    {
        float[] normal=CaptureBossPace(0),fast=CaptureBossPace(3600);
        Near(normal[0],fast[0],.001f,"Stored run speed shortened boss warning");
        Near(normal[1],fast[1],.001f,"Stored run speed accelerated boss projectiles");
        Near(normal[2],fast[2],.025f,"Stored run speed accelerated first visible attack-wave interval");
        Near(normal[3],fast[3],.025f,"Stored run speed accelerated subsequent visible attack-wave interval");
    }

    private static void CombatBotInput(RunnerSession run, bool shoot, bool earlyArtillery=false)
    {
        run.SetShooting(shoot);
        if(run.IsFlyingBoss){FlightBotInputV10(run);return;}
        bool high=run.BossTelegraphs.Any(m=>m.Visible&&m.Kind==BossAttackKind.LaserHigh&&(m.Active||m.Remaining<.65f));
        bool low=run.BossTelegraphs.Any(m=>m.Visible&&m.Kind==BossAttackKind.LaserLow&&(m.Active||m.Remaining<.32f));
        bool drone=run.BossProjectiles.Any(p=>!p.Friendly&&!p.Removed&&(p.Kind==BossAttackKind.Drone||(p.Kind==BossAttackKind.ElectricOrb&&p.Y>=55))&&
            p.X+p.Width>run.PlayerX-55&&p.X<run.PlayerX+125)||
            run.BossObstacles.Any(o=>o.Active&&!o.Removed&&o.Kind==BossAttackKind.ShieldDrone&&o.X+o.Width>run.PlayerX-55&&o.X<run.PlayerX+150);
        // Select a genuinely clear interval across the whole visible salvo.
        // Choosing the last marker's escape side can oscillate between two
        // simultaneous artillery zones even though either outer lane is safe.
        var danger = new List<Tuple<float,float>>();
        float threatTime=float.PositiveInfinity;
        float bodyTop=run.PlayerY+(run.IsDucking?RunnerSession.DuckHeight:RunnerSession.StandingHeight);
        void AddDanger(float left,float right,float seconds)
        {
            if(run.CurrentBoss.Kind==BossKind.Gunship&&seconds>.3f)return;
            danger.Add(Tuple.Create(left,right));
            if(run.PlayerX>left&&run.PlayerX<right)threatTime=Math.Min(threatTime,seconds);
        }
        foreach(BossTelegraph mark in run.BossTelegraphs)
            if(mark.Visible&&(mark.Kind==BossAttackKind.Artillery||mark.Kind==BossAttackKind.Missile||mark.Kind==BossAttackKind.MissileRain)&&
                (mark.Kind==BossAttackKind.MissileRain||run.CurrentBoss.Kind==BossKind.Gunship||mark.Active||mark.Remaining<(earlyArtillery?mark.Duration+.01f:.4f)))
            {
                float fall=mark.Kind==BossAttackKind.Artillery?485f:mark.Kind==BossAttackKind.MissileRain?335f:620f;
                float pace=1f+Math.Min(.3f,((run.BossNumber-1)/3)*.04f)+(run.CurrentBoss.RageLevel-1)*.08f;
                float until=mark.Active?0:mark.Remaining+Math.Max(0,430f-bodyTop)/(fall*pace);
                AddDanger(mark.X-60f,mark.X+mark.Width+60f,until);
            }
        foreach(BossProjectile p in run.BossProjectiles)
            if(!p.Friendly&&!p.Removed&&(p.Kind==BossAttackKind.Artillery||p.Kind==BossAttackKind.Missile||p.Kind==BossAttackKind.MissileRain))
            {
                float blastWidth=p.Kind==BossAttackKind.Artillery?100f:p.Kind==BossAttackKind.MissileRain?62f:78f;
                float centre=p.X+p.Width*.5f;
                float until=Math.Max(0,p.Y-bodyTop)/Math.Max(1f,-p.VelocityY);
                AddDanger(centre-blastWidth*.5f-60f,centre+blastWidth*.5f+60f,until);
            }
        foreach(BossTelegraph mark in run.BossTelegraphs)
            if(mark.Visible&&mark.Kind==BossAttackKind.ElectricPillar) AddDanger(mark.X-65,mark.X+mark.Width+65,mark.Remaining);
        foreach(BossObstacle obstacle in run.BossObstacles)
            if(!obstacle.Removed&&obstacle.Kind==BossAttackKind.ElectricPillar)AddDanger(obstacle.X-65,obstacle.X+obstacle.Width+65,0);
        float direction=0,escapeTime=0;
        if(danger.Any(d=>run.PlayerX>d.Item1&&run.PlayerX<d.Item2))
        {
            float best=float.PositiveInfinity,target=run.PlayerX;
            for(float candidate=RunnerSession.MinimumPlayerX;candidate<=RunnerSession.MaximumPlayerX;candidate+=5f)
            {
                if(danger.Any(d=>candidate>d.Item1-4&&candidate<d.Item2+4))continue;
                float cost=Math.Abs(candidate-run.PlayerX);
                if(cost<best){best=cost;target=candidate;}
            }
            if(float.IsInfinity(best))
                target=run.PlayerX<410?RunnerSession.MinimumPlayerX:RunnerSession.MaximumPlayerX;
            direction=target<run.PlayerX?-1:target>run.PlayerX?1:0;
            escapeTime=Math.Abs(target-run.PlayerX)/RunnerSession.HorizontalSpeed;
        }
        // Prioritize the next real intersection, not every future marker at
        // once. A far-future missile must not force crossing a current blast.
        bool droneWave=run.BossProjectiles.Any(p=>!p.Friendly&&!p.Removed&&
            (p.Kind==BossAttackKind.Drone||p.Kind==BossAttackKind.DroneLow)&&
            p.X+p.Width>run.PlayerX-55&&p.X<run.PlayerX+500);
        if(droneWave&&threatTime>escapeTime+.22f)direction=0;
        // A forward combat lane also meets stomp waves sooner, before the
        // next full-width laser sequence. Move there using ordinary A/D input
        // instead of assuming the far-left corner is safe for every combo.
        if(danger.Count==0&&run.CurrentBoss.Kind==BossKind.Sentinel)
            direction=run.PlayerX<594?1:run.PlayerX>606?-1:0;
        run.SetHorizontal(direction);
        run.SetDucking(high||drone);
        bool groundThreat=run.BossProjectiles.Any(p=>!p.Friendly&&!p.Removed&&
            (p.Kind==BossAttackKind.Cannon||p.Kind==BossAttackKind.Shockwave||p.Kind==BossAttackKind.DroneLow)&&
            p.X+p.Width>run.PlayerX-55&&p.X<run.PlayerX+110)||
            run.BossProjectiles.Any(p=>!p.Friendly&&!p.Removed&&
                (p.Kind==BossAttackKind.CrossMissile||(p.Kind==BossAttackKind.ElectricOrb&&p.Y<55))&&
                HorizontalThreatTimeV11(run,p)<.25f)||
            ChargeThreatV11(run)||
            run.BossObstacles.Any(o=>o.Active&&!o.Removed&&(o.Kind==BossAttackKind.MineDrop||o.Kind==BossAttackKind.CrateDrop)&&o.X+o.Width>run.PlayerX-55&&o.X<run.PlayerX+170);
        // If a trailing high drone catches the descending part of a jump,
        // use the available second jump to remain above its visible lane.
        // Grounded players still crouch; this does not invent an air-crouch.
        if(drone&&run.PlayerY>90&&run.PlayerY<150&&run.VelocityY<0)run.Jump();
        if(!high&&!drone&&(low||groundThreat))
        {
            if(run.PlayerY<=.01f)run.Jump();
            else if((NeedsGroundAirExtensionV9(run)||NeedsLowAirExtensionV9(run)||NeedsObstacleAirExtensionV10(run)||NeedsSpecialAirExtensionV11(run))&&run.VelocityY<0&&run.PlayerY<145)run.Jump();
        }
    }

    private sealed class BossWarningObservation
    {
        public float Began,Duration,X,Width;
        public BossAttackKind Kind;
    }
    private static void BossAttackSignalsV8()
    {
        int projectiles=0,beams=0;
        for(int number=1;number<=3;number++)
        {
            RunnerSession run=EnterBossFixture(number);ReachFighting(run);
            // Trace every wave through forced HP stages; this is a signal audit,
            // not a survival proof. Full Classic fights retain normal damage.
            SetProperty(run,"InvincibleRemaining",1000f);
            var warned=new Dictionary<int,BossWarningObservation>();var appeared=new HashSet<int>();var kinds=new HashSet<BossAttackKind>();
            // Hold fire for pattern coverage; health staging explicitly tests
            // all three attack phases, not a full-health victory.
            for(int i=0;i<120*70;i++)
            {
                if(i==120*22)run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*.5f;
                if(i==120*44)run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*.2f;
                foreach(BossTelegraph mark in run.BossTelegraphs)
                {
                    if(!mark.Visible)continue;
                    if(!mark.Active)
                    {
                        Check(mark.Duration>=.65f&&mark.Remaining<=mark.Duration+.02f,"Boss attack warning too short or invalid");
                        if(!warned.ContainsKey(mark.Id))warned.Add(mark.Id,new BossWarningObservation {
                            Began=run.BossElapsed-(mark.Duration-mark.Remaining),Duration=mark.Duration,
                            X=mark.X,Width=mark.Width,Kind=mark.Kind });
                        kinds.Add(mark.Kind);
                    }
                    else if(appeared.Add(mark.Id))
                    {
                        BossWarningObservation signal;
                        Check(mark.SourceTelegraphId>0&&warned.TryGetValue(mark.SourceTelegraphId,out signal),
                            "Active beam/blast appeared without its own visible warning: "+mark.Kind);
                        signal=warned[mark.SourceTelegraphId];
                        Check(signal.Kind==mark.Kind&&run.BossElapsed-signal.Began>=signal.Duration-.025f,
                            "Live beam/blast skipped its source warning duration");
                        if(mark.Kind==BossAttackKind.Artillery||mark.Kind==BossAttackKind.Missile)
                            Check(mark.X>=signal.X-.01f&&mark.X+mark.Width<=signal.X+signal.Width+.01f,
                                "Actual explosion extends beyond its marked ground footprint");
                        Check(mark.Width>0&&mark.Height>0&&mark.Remaining>0,"Active beam has invalid physical shape");beams++;
                    }
                }
                foreach(BossProjectile shot in run.BossProjectiles.Where(p=>!p.Friendly&&!p.Removed))
                    if(appeared.Add(shot.Id))
                    {
                        BossWarningObservation signal;
                        Check(shot.SourceTelegraphId>0&&warned.TryGetValue(shot.SourceTelegraphId,out signal),
                            "Enemy wave lacks its own visible telegraph: "+shot.Kind);
                        signal=warned[shot.SourceTelegraphId];
                        Check(signal.Kind==shot.Kind&&run.BossElapsed-signal.Began>=signal.Duration-.025f,
                            "Enemy wave activated before its own warning completed");
                        Check(shot.Width>0&&shot.Height>0&&Math.Abs(shot.VelocityX)+Math.Abs(shot.VelocityY)>0,"Enemy projectile never moves");
                        projectiles++;
                    }
                CombatBotInput(run,false);run.Update(1f/120);
                Check(run.State==RunState.Playing,"Pattern trace stopped on "+run.CurrentBoss.Kind+" at "+run.BossElapsed+"s "+ArenaDescription(run));
            }
            BossAttackKind[] expected=run.CurrentBoss.Kind==BossKind.Tank?new[]{BossAttackKind.Cannon,BossAttackKind.Artillery}:
                run.CurrentBoss.Kind==BossKind.Sentinel?new[]{BossAttackKind.LaserLow,BossAttackKind.LaserHigh,BossAttackKind.Shockwave}:new[]{BossAttackKind.AirGate,BossAttackKind.AirSpread,BossAttackKind.AirMine,BossAttackKind.AirLaser};
            foreach(BossAttackKind kind in expected)Check(kinds.Contains(kind),"Boss omitted its promised attack: "+kind);
        }
        Check(projectiles>=25&&beams>=10,"Boss warning audit omitted attack cycles");
        Console.WriteLine("  Boss pattern audit: "+projectiles+" actual enemy shots and "+beams+" active beams/blasts each linked to their own completed visible warning.");
    }

    private static string ArenaDescription(RunnerSession run)
    {
        return "player="+run.PlayerX+","+run.PlayerY+" vy="+run.VelocityY+" duck="+run.IsDucking+
            " threats="+string.Join(";",run.BossProjectiles.Where(p=>!p.Friendly).Select(p=>p.Kind+"@"+p.X+","+p.Y))+
            " obstacles="+string.Join(";",run.BossObstacles.Select(o=>o.Kind+"@"+o.X+","+o.Y))+" reason="+run.EndReason+
            " marks="+string.Join(";",run.BossTelegraphs.Select(m=>m.Kind+"@"+m.X+","+m.Y+" remaining="+m.Remaining+" active="+m.Active));
    }

    private static void BossClassicVictoriesV8()
    {
        float longest=0;
        for(int number=1;number<=3;number++)
        {
            RunnerSession run=EnterBossFixture(number);ReachFighting(run);
            Check(run.CurrentCharacter==CharacterKind.Classic&&run.Lives==1&&run.CurrentBoss.Health==run.CurrentBoss.MaxHealth,"Classic victory fixture altered health or granted advantages");
            float start=run.BossElapsed;int score=run.Score;float full=run.CurrentBoss.MaxHealth;int shots=0;
            var recent=new Queue<string>();var seenRage=new HashSet<int>();
            for(int i=0;i<120*100&&run.ArenaPhase==BossPhase.Fighting&&run.State==RunState.Playing;i++)
            {
                seenRage.Add(run.CurrentBoss.RageLevel);shots=Math.Max(shots,run.BossProjectiles.Count(p=>p.Friendly));
                if(i%30==0){recent.Enqueue(run.BossElapsed.ToString("0.00")+" "+ArenaDescription(run));while(recent.Count>16)recent.Dequeue();}
                CombatBotInput(run,true);run.Update(1f/120);
                Check(run.State==RunState.Playing&&run.Lives==1,"Free Classic hit during full boss fight #"+number+" at "+run.BossElapsed+" "+ArenaDescription(run)+"\nRecent: "+string.Join("\n",recent));
                Check(run.Score==score,"Shooting boss farmed running points");
            }
            Check(run.ArenaPhase==BossPhase.Victory&&run.CurrentBoss.Health==0&&shots>0,"Free Classic failed to defeat full-health "+run.CurrentBoss.Kind);
            Check(seenRage.SetEquals(new[]{1,2,3}),"Full-health fight skipped a real rage phase");
            float time=run.BossElapsed-start;longest=Math.Max(longest,time);
            Check(time>=15&&time<90&&full>=500,"Boss ended instantly or became an excessively long damage sponge");
            Console.WriteLine("  Classic defeated "+run.CurrentBoss.Kind+" from "+full+" HP in "+time.ToString("0.0")+" combat seconds with real shots, no damage or skill.");
        }
        Check(longest>25,"Boss coverage did not exercise sustained combat");
    }
}
