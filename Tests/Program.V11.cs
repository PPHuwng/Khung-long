using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityDino;

internal static partial class Program
{
    private static void RegisterV11Tests()
    {
        Test("boss charge actions move actual bodies only after their completed sweep warning", ChargeMotionV11);
        Test("only the real leading charge weapon damages the dinosaur and recovery stays harmless", ChargeContactV11);
        Test("missile ultimates enter from both edges and above with completed individual signals", MissileDirectionsV11);
        Test("missile rain preserves one locked escape corridor across every delayed wave", MissileRainGapV11);
        Test("electric projectiles use round contact rather than their transparent rectangular corners", ElectricOrbShapeV11);
        Test("new boss actions and projectile effects freeze and cancel on every arena exit", SpecialLifecycleV11);
    }

    private static bool IsChargeV11(BossAttackKind kind)
    {
        return kind==BossAttackKind.TankRam||kind==BossAttackKind.RobotDash||kind==BossAttackKind.GunshipDive;
    }

    private static float HorizontalThreatTimeV11(RunnerSession run,BossProjectile p)
    {
        float relative=p.VelocityX-run.HorizontalVelocity;
        if(relative<0&&p.X+p.Width>=run.PlayerX-55)
            return Math.Max(0,(p.X-run.PlayerX-55)/-relative);
        if(relative>0&&p.X<=run.PlayerX+55)
            return Math.Max(0,(run.PlayerX-55-p.X-p.Width)/relative);
        return float.PositiveInfinity;
    }
    private static bool ChargeThreatV11(RunnerSession run)
    {
        BossEncounter b=run.CurrentBoss;
        return b.ContactActive&&b.ContactY<120&&b.ContactX+b.ContactWidth>run.PlayerX-55&&b.ContactX<run.PlayerX+235;
    }
    private static bool NeedsSpecialAirExtensionV11(RunnerSession run)
    {
        if(ChargeThreatV11(run))return true;
        return run.BossProjectiles.Any(p=>!p.Friendly&&!p.Removed&&
            (p.Kind==BossAttackKind.CrossMissile||(p.Kind==BossAttackKind.ElectricOrb&&p.Y<55))&&
            HorizontalThreatTimeV11(run,p)<.25f&&run.PlayerY<p.Y+p.Height+30);
    }

    private static void ChargeMotionV11()
    {
        foreach(int number in new[]{1,2,3})
        {
            // Protected output measurement across the complete attack selection;
            // the separate full-health Classic test uses ordinary damage rules.
            RunnerSession run=IsolatedBossPattern(number,.2f);
            var warned=new Dictionary<int,Tuple<float,BossTelegraph>>();
            bool windup=false,strike=false,recovery=false;float minX=2000,maxX=0;
            for(int i=0;i<120*45;i++)
            {
                foreach(BossTelegraph mark in run.BossTelegraphs)
                    if(mark.Visible&&!mark.Active&&IsChargeV11(mark.Kind)&&!warned.ContainsKey(mark.Id))
                        warned.Add(mark.Id,Tuple.Create(run.BossElapsed-(mark.Duration-mark.Remaining),mark));
                BossEncounter boss=run.CurrentBoss;
                if(IsChargeV11(boss.AttackKind))
                {
                    windup|=boss.Action==BossAction.Windup;recovery|=boss.Action==BossAction.Recovery;
                    Check(!boss.ContactActive||boss.Action==BossAction.Strike,"A windup/recovery body caused contact damage");
                    if(boss.ContactActive)
                    {
                        strike=true;minX=Math.Min(minX,boss.X);maxX=Math.Max(maxX,boss.X);
                        Check(warned.ContainsKey(boss.ChargeSourceTelegraphId),"Boss charge lacks its visible source signal");
                        var source=warned[boss.ChargeSourceTelegraphId];BossTelegraph mark=source.Item2;
                        Check(run.BossElapsed-source.Item1>=mark.Duration-.025f,"Charge struck before its warning elapsed");
                        Check(boss.ContactWidth>0&&boss.ContactHeight>0&&boss.ContactX>=mark.X-.05f&&
                            boss.ContactX+boss.ContactWidth<=mark.X+mark.Width+.05f&&boss.ContactY>=mark.Y-.05f&&
                            boss.ContactY+boss.ContactHeight<=mark.Y+mark.Height+.05f,"Live charge footprint exceeded its marked sweep");
                        Check(boss.ContactX>=boss.X-1&&boss.ContactX+boss.ContactWidth<=boss.X+boss.Width+1,"Charge contact floated away from actual boss body");
                    }
                }
                run.Update(1f/120);
            }
            Check(windup&&strike&&recovery&&maxX-minX>400,"Boss omitted physical load/strike/return action: "+run.CurrentBoss.Kind);
        }
    }

    private static void MissileDirectionsV11()
    {
        int fronts=0,rears=0,above=0,orbs=0;
        foreach(int number in new[]{1,2,3})
        {
            RunnerSession run=IsolatedBossPattern(number,.2f);var sources=new Dictionary<int,Tuple<float,float>>();var seen=new HashSet<int>();
            for(int i=0;i<120*65;i++)
            {
                foreach(BossTelegraph mark in run.BossTelegraphs)
                    if(mark.Visible&&!mark.Active&&!sources.ContainsKey(mark.Id))
                        sources.Add(mark.Id,Tuple.Create(run.BossElapsed-(mark.Duration-mark.Remaining),mark.Duration));
                foreach(BossProjectile p in run.BossProjectiles)
                {
                    if(p.Friendly||p.Removed||!seen.Add(p.Id)||
                        (p.Kind!=BossAttackKind.CrossMissile&&p.Kind!=BossAttackKind.MissileRain&&p.Kind!=BossAttackKind.ElectricOrb))continue;
                    Check(sources.ContainsKey(p.SourceTelegraphId),"Special projectile appeared without its own source warning");
                    var signal=sources[p.SourceTelegraphId];
                    Check(signal.Item2>=.7f&&run.BossElapsed-signal.Item1>=signal.Item2-.025f,"Special projectile skipped its visible reaction time");
                    Check(p.Lifetime>0&&p.Lifetime<=12&&p.Width>0&&p.Height>0,"Special projectile is not finite physical geometry");
                    if(p.Kind==BossAttackKind.CrossMissile){if(p.VelocityX<0)fronts++;if(p.VelocityX>0)rears++;}
                    if(p.Kind==BossAttackKind.MissileRain&&p.VelocityY<0)above++;
                    if(p.Kind==BossAttackKind.ElectricOrb){orbs++;Check(p.Radius>0,"Electric orb retained box-shaped collision");}
                }
                Check(run.BossProjectiles.Count<90&&run.BossObstacles.Count<20,"New ultimate accumulated unbounded objects");
                run.Update(1f/120);
            }
        }
        Check(fronts>0&&rears>0&&above>0&&orbs>0,"Pattern coverage missed a direction or electrical projectile");
        Console.WriteLine("  V11 special audit: "+fronts+" front, "+rears+" rear, "+above+" falling missiles, "+orbs+" real circular electric orbs.");
    }

    private static void PlaceInChargeV11(RunnerSession run)
    {
        BossEncounter b=run.CurrentBoss;
        SetProperty(run,"PlayerX",b.ContactX+b.ContactWidth*.5f);
        SetProperty(run,"PlayerY",b.ContactY+5);
        SetProperty(run,"VelocityY",0f);SetProperty(run,"AnimationVelocityY",0f);
        SetField(run,"grounded",false);SetProperty(run,"InvincibleRemaining",0f);
        Check(RunnerGeometry.RectTouchesPlayer(b.ContactX,b.ContactY,b.ContactWidth,b.ContactHeight,run),"Charge fixture missed the visible opaque body");
    }
    private static void ChargeContactV11()
    {
        foreach(int number in new[]{1,2,3})
        {
            // Explicit contact fixtures place the actor inside a real strike;
            // they are not survival proofs or commands used by the fight bot.
            RunnerSession hit=IsolatedBossPattern(number,.2f);bool found=false;
            for(int i=0;i<120*45;i++)
            {
                if(hit.CurrentBoss.ContactActive&&hit.CurrentBoss.ContactX>120&&hit.CurrentBoss.ContactX<650){found=true;break;}
                hit.Update(1f/120);
            }
            Check(found,"Contact test missed physical charge crossing the playable area");
            ClearArenaThreats(hit);ArenaObstacles(hit).Clear();PlaceInChargeV11(hit);hit.Update(.0001f);
            Check(hit.Lives==0&&hit.State==RunState.GameOver,"Actual visible leading weapon did not inflict a real hit");
            int records=hit.Save.History.Count;string dead=SpecialSnapshotV11(hit);hit.Update(3);
            Check(hit.Save.History.Count==records&&SpecialSnapshotV11(hit)==dead,"Fatal charge advanced or recorded more than once");

            RunnerSession recover=IsolatedBossPattern(number,.2f);found=false;
            for(int i=0;i<120*45;i++)
            {
                if(IsChargeV11(recover.CurrentBoss.AttackKind)&&recover.CurrentBoss.Action==BossAction.Recovery){found=true;break;}
                recover.Update(1f/120);
            }
            Check(found&&!recover.CurrentBoss.ContactActive,"Charge never entered harmless recovery");
            ClearArenaThreats(recover);ArenaObstacles(recover).Clear();PlaceInChargeV11(recover);recover.Update(.0001f);
            Check(recover.Lives==1&&recover.State==RunState.Playing,"Returning boss caused an unmarked body hit "+recover.CurrentBoss.Kind+" "+ArenaDescription(recover)+" action="+recover.CurrentBoss.Action+" contact="+recover.CurrentBoss.ContactActive);
        }
    }

    private static void MissileRainGapV11()
    {
        foreach(int number in new[]{1,2})
        {
            RunnerSession run=IsolatedBossPattern(number,.2f);var marks=new Dictionary<int,BossTelegraph>();int count=0;
            for(int i=0;i<120*60;i++)
            {
                foreach(BossTelegraph mark in run.BossTelegraphs)
                    if(mark.Visible&&!mark.Active&&mark.Kind==BossAttackKind.MissileRain)
                    {
                        marks[mark.Id]=mark;Check(mark.GapWidth>=200,"Rain's claimed escape lane cannot fit a dinosaur");
                        Check(mark.X+mark.Width<=mark.GapX+.01f||mark.X>=mark.GapX+mark.GapWidth-.01f,"Rain cue paints its safe corridor as hazardous");
                    }
                foreach(BossProjectile p in run.BossProjectiles.Where(p=>!p.Friendly&&!p.Removed&&p.Kind==BossAttackKind.MissileRain))
                {
                    Check(marks.ContainsKey(p.SourceTelegraphId),"Rain missile lost its lane's source");BossTelegraph source=marks[p.SourceTelegraphId];
                    Check(p.X+p.Width<=source.GapX+.01f||p.X>=source.GapX+source.GapWidth-.01f,"Rain missile intrudes into the promised escape corridor");count++;
                }
                run.Update(1f/120);
            }
            Check(count>0,"Rain corridor test never observed falling missiles");
        }
    }

    private static void ElectricOrbShapeV11()
    {
        RunnerSession corner=EnterBossFixture(4);ReachFighting(corner);ClearArenaThreats(corner);
        float foundX=0,foundY=0;bool found=false;
        for(float x=corner.PlayerX-90;x<corner.PlayerX+90&&!found;x+=2)
        for(float y=0;y<105&&!found;y+=2)
            if(RunnerGeometry.RectTouchesPlayer(x,y,32,32,corner)&&!RunnerGeometry.CircleTouchesPlayer(x+16,y+16,16,corner))
            {foundX=x;foundY=y;found=true;}
        Check(found,"Could not construct independent transparent circle-corner case");
        ArenaShots(corner).Add(new BossProjectile{Id=999001,Kind=BossAttackKind.ElectricOrb,X=foundX,Y=foundY,Width=32,Height=32,Radius=16,Lifetime=1});
        corner.Update(.0001f);Check(corner.Lives==1&&corner.State==RunState.Playing,"Electric orb's transparent box corner inflicted damage");
        RunnerSession hit=EnterBossFixture(4);ReachFighting(hit);ClearArenaThreats(hit);
        ArenaShots(hit).Add(new BossProjectile{Id=999002,Kind=BossAttackKind.ElectricOrb,X=hit.PlayerX+20,Y=45,Width=32,Height=32,Radius=16,Lifetime=1});
        Check(RunnerGeometry.CircleTouchesPlayer(hit.PlayerX+36,61,16,hit),"Orb contact fixture missed opaque body");
        hit.Update(.0001f);Check(hit.State==RunState.GameOver&&hit.Lives==0,"Visible circular electrical contact did not cause real damage");
    }

    private static string SpecialSnapshotV11(RunnerSession run)
    {
        BossEncounter b=run.CurrentBoss;
        return BossFeedbackSnapshot(run)+"|"+string.Join(",",b.Action,b.ActionProgress,b.ContactActive,b.ContactX,b.ContactY,b.ChargeSourceTelegraphId,
            string.Join(";",run.BossProjectiles.Select(p=>p.Id+":"+p.X+":"+p.Y+":"+p.Lifetime)));
    }
    private static void SpecialLifecycleV11()
    {
        foreach(int number in new[]{1,2,3})
        {
            RunnerSession run=IsolatedBossPattern(number,.2f);bool strike=false;
            for(int i=0;i<120*45;i++)
            {
                if(run.CurrentBoss.ContactActive){strike=true;break;}run.Update(1f/120);
            }
            Check(strike,"Lifecycle fixture missed an actual charge");run.TogglePause();string paused=SpecialSnapshotV11(run);run.Update(5);
            Check(SpecialSnapshotV11(run)==paused,"Pause advanced charge collision, action pose or missile trails");run.TogglePause();
            run.CurrentBoss.Health=0;run.Update(1f/120);
            Check(run.ArenaPhase==BossPhase.Victory&&!run.CurrentBoss.ContactActive&&run.BossProjectiles.Count==0&&run.BossTelegraphs.Count==0,"Victory retained live charge or ultimate damage");
            run.ReturnToMenu();run.StartRun();Check(!run.IsBossArena&&run.CurrentBoss==null&&run.BossProjectiles.Count==0,"New run kept special attack state");
        }
    }
}
