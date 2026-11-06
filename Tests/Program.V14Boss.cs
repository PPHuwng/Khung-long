using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using CityDino;

internal static partial class Program
{
    private static void RegisterV14BossTests()
    {
        Test("advanced boss formations combine skills and approach angles with seeded variety", BossComboVarietyV14);
        Test("delayed boss combo aim locks when its local cue appears and never homes afterwards", BossComboAimV14);
        Test("advanced boss projectiles travel diagonally and preserve real contact damage", BossComboContactV14);
        Test("advanced boss cue durations are concise while every attack completes its own signal", BossComboSignalsV14);
        Test("higher boss tiers add mixed waves without unbounded objects or unused charge downtime", BossComboPressureV14);
        Test("short airborne and naval beams leave a real escape at both vertical limits despite momentum", BossBeamEscapeV14);
    }

    private static RunnerSession ComboFixtureV14(BossKind kind,int tier=3,int seed=617)
    {
        var run=new RunnerSession(null,seed);
        Check(run.StartBossPractice(kind,CharacterKind.Classic,false,tier),"Combo fixture rejected");
        Check(run.SkipPracticeIntro(),"Combo fixture intro rejected");
        return run;
    }

    private static string ComboPlanV14(RunnerSession run)
    {
        return string.Join("|",run.BossTelegraphs.Select(m=>m.Kind+":"+m.DelayRemaining.ToString("F3")+":"+
            m.OriginX.ToString("F1")+","+m.OriginY.ToString("F1")+":"+m.VelocityX.ToString("F1")+","+m.VelocityY.ToString("F1")));
    }

    private static void BossComboVarietyV14()
    {
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            var variants=new HashSet<string>();bool diagonal=false,rear=false,front=false;
            foreach(int seed in new[]{17,31,81,117,317,617,991,1317})
            {
                RunnerSession run=ComboFixtureV14(kind,3,seed),same=ComboFixtureV14(kind,3,seed);
                Check(ComboPlanV14(run)==ComboPlanV14(same),"Same seed changed mixed boss plan "+kind);
                variants.Add(run.CurrentBoss.AttackName);
                Check(run.BossTelegraphs.Select(m=>m.Kind).Distinct().Count()>=2,"Advanced plan still repeats one skill family "+kind);
                diagonal|=run.BossTelegraphs.Any(m=>Math.Abs(m.VelocityX)>20&&Math.Abs(m.VelocityY)>20);
                rear|=run.BossTelegraphs.Any(m=>m.OriginX<0&&m.VelocityX>0);
                front|=run.BossTelegraphs.Any(m=>m.OriginX>RunnerSession.CanvasWidth&&m.VelocityX<0);
            }
            Check(variants.Count>=3&&diagonal&&rear&&front,"Advanced boss omitted seeded formations or attack angles "+kind);
            var drill=ComboFixtureV14(kind,1,617);
            Check(!drill.BossTelegraphs.Any(m=>m.LockOnReveal),"Beginner practice unexpectedly enabled advanced combos");
        }
    }

    private static void BossComboAimV14()
    {
        int checkedCues=0;
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            RunnerSession run=null;BossTelegraph cue=null;
            foreach(int seed in new[]{17,31,81,117,317,617,991})
            {
                var candidate=ComboFixtureV14(kind,3,seed);
                var delayed=candidate.BossTelegraphs.FirstOrDefault(m=>m.LockOnReveal&&m.DelayRemaining>.6f);
                if(delayed!=null){run=candidate;cue=delayed;break;}
            }
            Check(cue!=null,"No delayed tracking snapshot found "+kind);
            SetProperty(run,"InvincibleRemaining",1000f); // signal timing audit, not a survival proof
            float originalAim=cue.AimX;
            while(!cue.Visible)
            {
                run.SetHorizontal(1);run.SetFlightLift(true);run.SetSwimLift(true);
                run.Update(1f/240);
            }
            // Run one physical step to lock a cue whose delay expired exactly
            // at the prior external frame boundary.
            run.Update(1f/240);
            Check(!cue.LockOnReveal,"Visible cue failed to lock its aim "+kind);
            Near(cue.AimX,Math.Max(120,Math.Min(700,run.PlayerX+cue.AimOffsetX)),5,"Cue used a stale cycle-start X");
            float aimX=cue.AimX,aimY=cue.AimY,x=cue.X,y=cue.Y,vx=cue.VelocityX,vy=cue.VelocityY;
            Check(Math.Abs(run.PlayerX-260)>50,"Aim test never actually relocated the dinosaur");
            for(int i=0;i<24;i++){run.SetHorizontal(-1);run.SetFlightLift(false);run.SetSwimDive(true);run.Update(1f/240);}
            Near(cue.AimX,aimX,0,"Cue followed X after becoming visible");
            Near(cue.AimY,aimY,0,"Cue followed depth after becoming visible");
            Near(cue.X,x,0,"Visible attack rectangle secretly moved");Near(cue.Y,y,0,"Visible beam secretly moved");
            Near(cue.VelocityX,vx,0,"Visible projectile direction changed");Near(cue.VelocityY,vy,0,"Visible projectile slope changed");
            checkedCues++;
        }
        Check(checkedCues==4,"Aim-lock contract omitted a boss");
    }

    private static void BossComboContactV14()
    {
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            RunnerSession run=null;BossTelegraph cue=null;
            foreach(int seed in new[]{17,31,81,117,317,617,991})
            {
                var candidate=ComboFixtureV14(kind,3,seed);
                var diagonal=candidate.BossTelegraphs.FirstOrDefault(m=>Math.Abs(m.VelocityX)>20&&Math.Abs(m.VelocityY)>20);
                if(diagonal!=null){run=candidate;cue=diagonal;break;}
            }
            Check(cue!=null,"No diagonal production attack "+kind);
            SetProperty(run,"InvincibleRemaining",1000f); // wait for the real warned shot without incidental damage
            while(!run.BossProjectiles.Any(p=>!p.Friendly&&p.SourceTelegraphId==cue.Id))run.Update(1f/240);
            var shot=run.BossProjectiles.First(p=>!p.Friendly&&p.SourceTelegraphId==cue.Id);
            float oldX=shot.X,oldY=shot.Y;
            run.Update(.025f);
            Check(Math.Abs(shot.X-oldX)>1&&Math.Abs(shot.Y-oldY)>1,"Diagonal shot only changed its artwork "+kind);
            // Isolate contact geometry from the other combo skills only for
            // this unit fixture, then put the opaque torso in its actual path.
            foreach(var other in run.BossProjectiles)if(other!=shot)other.Removed=true;
            ((IList<BossTelegraph>)run.BossTelegraphs).Clear();
            ((IList<BossObstacle>)run.BossObstacles).Clear();
            shot.X=run.PlayerX+20;shot.Y=run.PlayerY+45;
            shot.VelocityX=shot.VelocityY=0;
            SetProperty(run,"InvincibleRemaining",0f);int before=run.Lives;
            run.Update(1f/240);
            Check(run.Lives==before-1,"Actual diagonal projectile torso contact caused no damage "+kind);
        }
    }

    private static void BossComboSignalsV14()
    {
        int shots=0,beams=0;
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            var run=ComboFixtureV14(kind,8,81);SetProperty(run,"InvincibleRemaining",1000f);
            var cues=new Dictionary<int,Tuple<float,float>>();var observed=new HashSet<int>();
            for(int i=0;i<120*35;i++)
            {
                foreach(var cue in run.BossTelegraphs.Where(m=>m.Visible))
                {
                    if(!cue.Active&&!cues.ContainsKey(cue.Id))
                    {
                        Check(cue.Duration>=.45f&&cue.Duration<=1.051f,"Advanced cue retained a long old banner interval "+cue.Kind);
                        cues.Add(cue.Id,Tuple.Create(run.BossElapsed-cue.Duration+cue.Remaining,cue.Duration));
                    }
                    if(cue.Active&&observed.Add(cue.Id))
                    {
                        Check(cues.ContainsKey(cue.SourceTelegraphId),"Active beam lacks visible source");
                        var source=cues[cue.SourceTelegraphId];Check(run.BossElapsed-source.Item1>=source.Item2-.025f,"Beam activated early");beams++;
                    }
                }
                foreach(var p in run.BossProjectiles.Where(p=>!p.Friendly&&!p.Removed))
                    if(observed.Add(p.Id))
                    {
                        Check(cues.ContainsKey(p.SourceTelegraphId),"Combo projectile lost local source cue "+p.Kind);
                        var source=cues[p.SourceTelegraphId];Check(run.BossElapsed-source.Item1>=source.Item2-.025f,"Combo fired before local cue completed");shots++;
                    }
                Check(run.BossTelegraphs.Count<100&&run.BossProjectiles.Count<100&&run.BossObstacles.Count<30,"Mixed skills accumulated indefinitely");
                run.Update(1f/120);
            }
            run.TogglePause();string paused=SpecialSnapshotV11(run);run.Update(2);
            Check(paused==SpecialSnapshotV11(run),"Paused mixed skills advanced");run.TogglePause();
            run.CurrentBoss.Health=0;run.Update(.01f);
            Check(run.BossTelegraphs.Count==0&&run.BossProjectiles.Count==0&&run.BossObstacles.Count==0,"Victory retained delayed combo attacks");
        }
        Check(shots>50&&beams>0,"Signal audit omitted physical mixed shots or beams");
        Console.WriteLine("  V14 combo source audit: "+shots+" real shots, "+beams+" beams; every local cue completed.");
    }

    private static void BossComboPressureV14()
    {
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            int low=0,high=0;
            foreach(int seed in new[]{17,31,81,117,317,617,991})
            {
                var a=ComboFixtureV14(kind,3,seed);var b=ComboFixtureV14(kind,8,seed);
                Check(a.CurrentBoss.AttackName==b.CurrentBoss.AttackName,"Tier changed the seeded formation rather than density");
                int aCount=a.BossTelegraphs.Count,bCount=b.BossTelegraphs.Count;
                low+=aCount;high+=bCount;
                Check(bCount>=aCount,"Higher tier reduced the same formation's attack count");
                if(a.CurrentBoss.AttackName.Contains("Xung phong")||a.CurrentBoss.AttackName.Contains("Quyền chớp")||
                    a.CurrentBoss.AttackName.Contains("Bổ nhào")||a.CurrentBoss.AttackName.Contains("Đột kích sâu"))
                    Check((float)typeof(RunnerSession).GetField("bossActionCycle",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).GetValue(b)<8f,
                        "High-tier charge waited through unused wave slots");
            }
            Check(high>low,"High tier still repeats the same small formation "+kind);
        }
    }

    private static void BossBeamEscapeV14()
    {
        int cases=0;
        foreach(BossKind kind in new[]{BossKind.Gunship,BossKind.Submarine})
        foreach(float y in new[]{20f,60f,140f,220f,260f})
        foreach(float velocity in new[]{-220f,0f,200f})
        {
            bool escaped=false;
            foreach(int direction in new[]{-1,1})
            {
                var run=ComboFixtureV14(kind,8,81);
                ClearArenaThreats(run);ArenaObstacles(run).Clear();
                SetField(run,"bossActionCycle",10000f);SetField(run,"attackClock",0d);
                SetProperty(run,"Lives",1);SetProperty(run,"PlayerY",y);SetProperty(run,"VelocityY",velocity);
                run.SetFlightLift(velocity>0);run.SetSwimLift(velocity>0);run.SetSwimDive(velocity<0);
                BossAttackKind beam=kind==BossKind.Submarine?BossAttackKind.SonarBeam:BossAttackKind.AirLaser;
                typeof(RunnerSession).GetMethod("AddComboBeam",BindingFlags.Instance|BindingFlags.NonPublic)
                    .Invoke(run,new object[]{beam,0f,42f});
                // Keep the existing motion for a human reaction interval,
                // then perform a held direction through the real live beam.
                for(int i=0;i<240*2&&run.State==RunState.Playing;i++)
                {
                    if(i>=29)
                    {
                        run.SetFlightLift(direction>0);run.SetSwimLift(direction>0);run.SetSwimDive(direction<0);
                    }
                    run.Update(1f/240);
                }
                if(run.State==RunState.Playing&&run.Lives==1)escaped=true;
            }
            Check(escaped,"Short beam has no physical vertical escape: "+kind+" y="+y+" vy="+velocity);
            cases++;
        }
        Console.WriteLine("  Fast-beam live escape audit: "+cases+" depth/momentum cases, 120ms reaction, no damage protection.");
    }
}
