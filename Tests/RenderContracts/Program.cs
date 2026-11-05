using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Reflection;
using CityDino;
using UnityEngine;

internal static class Program
{
    private static int passed,failed;
    private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
    private static string assetDirectory;
    private static int Main()
    {
        SetUpResources();
        Test("rotating saw stays centred on collision geometry at every canvas scale",SawCentres);
        Test("line segments preserve endpoints under canvas scale and letterboxing",LineEndpoints);
        Test("every hazard is drawn and no floating action labels remain",VisibleHazards);
        Test("peak blackout ends the world pass with fully opaque pure black",FullBlackout);
        Test("storm lightning rendering follows the shared strike clock",LightningDraw);
        Test("storm rain remains visible above both darkness and lightning",StormRainOrder);
        Test("snow has a visible icy surface and falling flakes",SnowSurface);
        Test("snow road cuts match open gaps and collapsed floors",SnowGapCuts);
        Test("all survival and power-up pickups are drawn at their physical positions",PickupSprites);
        Test("biome hazard sprites stay at their physical bounds under canvas transforms",BiomeSprites);
        Test("biome warnings are visible and inactive vents never draw a damaging plume",BiomeWarnings);
        Test("vent plume drawing follows its shared active collision height",VentPlumeHeight);
        Test("paused biome warnings and moving debris do not advance with ambient time",PausedBiomeHazards);
        Test("paused snow and storm weather do not advance with ambient wall time",PausedWeather);
        Test("jump and crouch poses draw their own atlas frames in both directions",ActionSprites);
        Test("five running poses use their atlas frames and physical layout at every canvas scale",RunningSprites);
        Test("running art stays consistent in menu, pause, death and legacy fallback",RunningLifecycle);
        Test("boss arenas replace the scrolling world and all district weather",ArenaIsolation);
        Test("all four bosses stay at their shared physical bounds under canvas transforms",BossSprites);
        Test("boss projectiles and attack telegraphs use their exact live damage geometry",BossAttackGeometry);
        Test("boss weak point clearly changes between protected and vulnerable states",BossWeakPoint);
        Test("boss arena effects and dead encounter remain frozen with gameplay",ArenaLifecycle);
        Test("live boss entry, motion and recoil preserve exact physical sprites",DynamicBossBodies);
        Test("tank treads, robot servos and helicopter rotors animate within a fixed body",BossMechanicalAnimation);
        Test("actual boss hits and enemy fire trigger feedback that freezes with gameplay",BossHitAndFireFeedback);
        Test("defeated bosses show timed destruction that freezes when paused",BossVictoryFeedback);
        Test("queued boss attacks remain hidden until distinct harmless warnings begin",BossQueuedWarnings);
        Test("combat HUD identifies all rage phases and the active attack",BossRageHud);
        Test("purchased characters draw every pose and facing from their own atlases",CharacterSprites);
        Test("companion collision silhouettes follow independent opaque and clear PNG samples",CharacterAlphaContacts);
        Test("character previews use the same atlas pose and physical layout",CharacterPreviews);
        Test("combat HUD shows boss health and limits fire and skill controls",CombatHud);
        Test("character and shop modals disable every underlying action button",ModalButtonIsolation);
        Test("normal 20k encounter gradually reveals sky during collected-device takeoff",FlightJourneyRendering);
        Test("hoverboard remains visible below the feet with front deck and rear thrust at every canvas scale",FlightDeviceGeometry);
        Test("airborne combat has no road or helipad and all arenas have distinct themes",ThemedArenaRendering);
        Test("scripted transitions and landing freeze their world layers when paused",JourneyPauseRendering);
        Test("boss obstacles show exact active physical bodies and harmless visible warnings",BossObstacleRendering);
        Test("aerial gate warning displays both collision bars and its real safe gap",AirGateRendering);
        Test("practice preparation previews all bosses and trial characters without underlying actions",PracticeMenuRendering);
        Test("practice results expose retry preparation and menu without ordinary run rewards",PracticeResultsRendering);
        Test("boss actions select independently drawn poses without moving their physical image bounds",BossActionPoseRendering);
        Test("real charge strikes animate the body and highlight the exact live contact shape",BossChargeRendering);
        Test("robot electricity is a true circular energy sphere with a harmless soft halo",ElectricOrbRendering);
        Test("front rear and falling missiles point their metal nose along the actual velocity",DirectionalMissileRendering);
        Test("missile rain warns its physical strips while keeping the real escape corridor clear",MissileRainRendering);
        Test("ordnance uses separate source art and shared round damage cores instead of flying boxes",OrdnanceRenderingV12);
        Test("partial and full-left sweeps reveal distinct locked stop and screen-edge signals",SweepWarningsV12);
        Test("practice tier controls and combat HUD distinguish danger level from health phase",TierHudV12);
        Test("victory moves through the exit behind the boss while arena and street scroll forward",ForwardExitRenderingV12);
        Test("submarine journey equips a helmet before the jump and reveals a separate submerged arena",UnderwaterJourneyV13);
        Test("diving helmet glass exposes every character face in both directions and stays on its logical head",DivingHelmetV13);
        Test("naval attacks draw distinct fish torpedoes mines and fully active sonar geometry",NavalOrdnanceV13);
        Test("submarine victory surfaces before the far exit and keeps the destination street moving forward",UnderwaterExitV13);
        Test("normal running rocket waves show harmless cues then distinct physical missiles and freeze on pause",RunMissileRenderingV13);
        Test("underwater controls and four-boss preparation remain clear and isolated",UnderwaterHudV13);
        Console.WriteLine("Render coordinate contracts: "+passed+" passed, "+failed+" failed.");
        Console.WriteLine("Recorded production draw calls with affine API stubs; no Unity rasterization or Editor execution.");
        return failed==0?0:1;
    }
    private static void Test(string name,Action action)
    {
        try { action(); passed++; Console.WriteLine("PASS "+name); }
        catch(Exception e) { failed++; Console.WriteLine("FAIL "+name+": "+e.Message); }
    }
    private static void Check(bool condition,string message)
    { if(!condition) throw new InvalidOperationException(message); }
    private static void Near(float a,float b,string message)
    { Check(Math.Abs(a-b)<0.025f,message+" actual="+a+" expected="+b); }
    private static void Point(Vector3 a,Vector3 b,string message)
    { Near(a.x,b.x,message+" X"); Near(a.y,b.y,message+" Y"); }
    private static void SetUpResources()
    {
        DirectoryInfo root=new DirectoryInfo(Directory.GetCurrentDirectory());
        while(root!=null&&!Directory.Exists(Path.Combine(root.FullName,"Assets","CityDino"))) root=root.Parent;
        Check(root!=null,"Run from the project directory");
        assetDirectory=Path.Combine(root.FullName,"Assets","CityDino","Resources","CityDino");
        foreach(string file in Directory.GetFiles(assetDirectory,"*.png"))
        {
            using(var stream=File.OpenRead(file))
            {
                byte[] header=new byte[24]; stream.ReadExactly(header);
                int width=(header[16]<<24)|(header[17]<<16)|(header[18]<<8)|header[19];
                int height=(header[20]<<24)|(header[21]<<16)|(header[22]<<8)|header[23];
                string name=Path.GetFileNameWithoutExtension(file);
                Resources.TestAssets["CityDino/"+name]=new Texture2D(width,height,TextureFormat.RGBA32,false){name=name};
            }
        }
        Resources.TestAssets["CityDino/RunnerFont"]=new Font();
        Event.current=new Event { type=EventType.Repaint };
    }
    private static IList<WorldEntity> World(RunnerSession session)
    {
        return (IList<WorldEntity>)typeof(RunnerSession).GetField("entities",Private).GetValue(session);
    }
    private static RunnerSession New(District district=District.Day)
    {
        var run=new RunnerSession(new RunnerSave(),1); run.SetStartingDistrict(district); run.StartRun(); World(run).Clear(); return run;
    }
    private static Matrix4x4 Canvas(float scale,float x=0,float y=0)
    { return Matrix4x4.TRS(new Vector3(x,y,0),Quaternion.identity,Vector3.one*scale); }
    private static void Draw(RunnerWorldView view,RunnerSession run,Matrix4x4 canvas)
    {
        GUI.DrawCalls.Clear(); GUI.matrix=canvas; GUI.color=Color.white; view.Draw(run,0);
        Point(GUI.matrix.MultiplyPoint3x4(new Vector3(76,139,0)),canvas.MultiplyPoint3x4(new Vector3(76,139,0)),"World did not restore canvas matrix");
    }
    private static void SawCentres()
    {
        var art=new RunnerArt(); var run=New();
        var saw=new WorldEntity { Id=1,Kind=EntityKind.Saw,X=450,Y=60,BaseY=60,Width=76,Height=76 };
        World(run).Add(saw);
        using(var view=new RunnerWorldView(art))
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(1.5f),Canvas(0.68f,119,28),Canvas(1.5f,320,0),Canvas(1,0,140)})
        foreach(float angle in new[]{0f,35f,90f,180f,273f})
        {
            saw.Rotation=angle; Draw(view,run,canvas);
            RecordedDraw draw=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&d.Texture==art.HazardAtlas&&Math.Abs(d.UV.x-art.GetHazardUV(1).x)<0.00001f);
            Vector3 expected=canvas.MultiplyPoint3x4(new Vector3(saw.X+saw.Width/2,RunnerSession.GroundY-saw.Y-saw.Height/2,0));
            Point(draw.Centre,expected,"Saw moved away from physical centre at angle "+angle);
            Vector3 edge=draw.Matrix.MultiplyPoint3x4(new Vector3(draw.Position.x,draw.Position.center.y,0));
            Vector3 scaledEdge=canvas.MultiplyPoint3x4(new Vector3(saw.X,RunnerSession.GroundY-saw.Y-saw.Height/2,0));
            float actualRadius=(float)Math.Sqrt(Math.Pow(edge.x-expected.x,2)+Math.Pow(edge.y-expected.y,2));
            float expectedRadius=(float)Math.Sqrt(Math.Pow(scaledEdge.x-expected.x,2)+Math.Pow(scaledEdge.y-expected.y,2));
            Near(actualRadius,expectedRadius,"Rotation changed blade size");
        }
        Check(RunnerGeometry.SawTouchesPlayer(saw,450,0,false,false,0,0),"Visible saw fixture does not collide at its authored location");
    }
    private static void LineEndpoints()
    {
        MethodInfo line=typeof(RunnerWorldView).GetMethod("Line",BindingFlags.Static|BindingFlags.NonPublic);
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(1.5f,133,25),Canvas(0.68f,0,140)})
        {
            var from=new Vector2(340,270); var to=new Vector2(398,326);
            GUI.matrix=canvas; GUI.DrawCalls.Clear();
            line.Invoke(null,new object[]{from,to,Color.white,2f});
            RecordedDraw draw=GUI.DrawCalls.Single();
            Point(draw.Matrix.MultiplyPoint3x4(new Vector3(draw.Position.x,draw.Position.center.y,0)),canvas.MultiplyPoint3x4(from),"Line start displaced");
            Point(draw.Matrix.MultiplyPoint3x4(new Vector3(draw.Position.xMax,draw.Position.center.y,0)),canvas.MultiplyPoint3x4(to),"Line end displaced");
        }
    }
    private static void VisibleHazards()
    {
        var art=new RunnerArt(); var run=New();
        EntityKind[] kinds={EntityKind.Plane,EntityKind.Saw,EntityKind.Spike,EntityKind.Barrier,EntityKind.House,EntityKind.Hydrant};
        for(int i=0;i<kinds.Length;i++) World(run).Add(new WorldEntity { Id=i+1,Kind=kinds[i],X=300+i*130,Y=i==0?60:0,Width=64,Height=64,Rotation=51 });
        using(var view=new RunnerWorldView(art)) Draw(view,run,Canvas(1.5f,100,0));
        foreach(WorldEntity e in run.Entities)
            Check(GUI.DrawCalls.Any(d=>d.Texture!=null&&d.Texture!=Texture2D.whiteTexture&&d.Position.x==e.X&&d.Position.width==e.Width&&d.Position.height==e.Height),"Hazard not drawn: "+e.Kind);
        Check(!GUI.DrawCalls.Any(d=>d.Kind=="Label"&&(d.Text=="CÚI"||d.Text=="NHẢY")),"Floating action labels still exist");
    }
    private static void FullBlackout()
    {
        var art=new RunnerArt(); var run=New(District.Night);
        Set(run,"NextBlackoutAt",2f); Set(run,"BlackoutDuration",2.5f); Set(run,"DistrictElapsed",2.21f);
        Check(run.DarknessAmount==1,"Night fixture is not at fully dark plateau");
        World(run).Add(new WorldEntity { Kind=EntityKind.Saw,X=450,Y=60,Width=76,Height=76,Rotation=80 });
        World(run).Add(BiomeFixture(EntityKind.FallingIce));
        World(run).Add(BiomeFixture(EntityKind.HeatVent));
        World(run).Add(BiomeFixture(EntityKind.StormDebris));
        using(var view=new RunnerWorldView(art)) Draw(view,run,Canvas(1.5f));
        RecordedDraw last=GUI.DrawCalls.Last();
        Check(last.Kind=="Texture"&&last.Texture==Texture2D.whiteTexture&&last.Position.x==0&&last.Position.y==0&&last.Position.width==1280&&last.Position.height==720,"World was drawn over night blackout");
        Check(last.Color.r==0&&last.Color.g==0&&last.Color.b==0&&last.Color.a==1,"Night blackout is not opaque pure black");
        var storm=New(District.Storm); Set(storm,"NextBlackoutAt",2f); Set(storm,"BlackoutDuration",2.5f); Set(storm,"DistrictElapsed",2.21f);
        Check(storm.DarknessAmount==0,"Storm retained the long night blackout mechanism");
    }
    private static void Set(RunnerSession run,string property,float value)
    { typeof(RunnerSession).GetProperty(property).GetSetMethod(true).Invoke(run,new object[]{value}); }
    private static bool FullCanvas(RecordedDraw draw)
    {
        return draw.Kind=="Texture"&&draw.Texture==Texture2D.whiteTexture&&draw.Position.x==0&&draw.Position.y==0
            &&draw.Position.width==1280&&draw.Position.height==720;
    }
    private static void LightningDraw()
    {
        const float peak=1.16f;
        foreach(District district in new[]{District.Day,District.Snow,District.Sunset,District.Night,District.Skyway,District.Storm})
        {
            var art=new RunnerArt(); var run=New(district); Set(run,"DistrictElapsed",peak);
            using(var view=new RunnerWorldView(art)) Draw(view,run,Canvas(1.5f,150,0));
            var flashes=GUI.DrawCalls.Where(d=>FullCanvas(d)&&d.Color.r>0.5f&&d.Color.b>d.Color.r&&d.Color.a>0).ToList();
            Check(flashes.Count==(district==District.Storm?1:0),"Lightning appeared on the wrong map or pulse duplicated");
            if(district==District.Storm)
                Near(flashes[0].Color.a,RunnerWeather.GetLightning(district,peak).Amount*0.55f,"Flash uses a different clock from thunder");
        }
    }
    private static void StormRainOrder()
    {
        foreach(float time in new[]{1.16f,1.7f,2.5f,2.96f,3.9f})
        {
            var art=new RunnerArt();var run=New(District.Storm);Set(run,"DistrictElapsed",time);Set(run,"Elapsed",time);
            using(var view=new RunnerWorldView(art)) Draw(view,run,Canvas(1.5f,90,0));
            int overlay=GUI.DrawCalls.FindLastIndex(FullCanvas);
            Check(overlay>=0,"Storm is missing its darkness/lightning overlay");
            int drops=GUI.DrawCalls.Skip(overlay+1).Count(d=>d.Kind=="Texture"&&d.Texture==Texture2D.whiteTexture
                &&d.Position.height<=3&&d.Position.width>=12&&d.Color.r>0.65f&&d.Color.g>0.75f&&d.Color.b>0.85f&&d.Color.a>0.1f);
            Check(drops>=35,"Rain was covered by storm darkness/flash at "+time+"s (visible drops="+drops+")");
        }
    }
    private static void SnowGapCuts()
    {
        var art=new RunnerArt();var run=New(District.Snow);
        var gap=new WorldEntity{Kind=EntityKind.Gap,X=350,Width=160,Height=160};
        var bridge=new WorldEntity{Kind=EntityKind.CollapsingFloor,X=720,Width=180,Height=160,CollapseProgress=0.5f};
        World(run).Add(gap);World(run).Add(bridge);
        using(var view=new RunnerWorldView(art))
        {
            Draw(view,run,Canvas(1));
            var cuts=(IList<Vector2>)typeof(RunnerWorldView).GetField("gaps",Private).GetValue(view);
            Check(cuts.Count==1,"Uncollapsed bridge was rendered as an open hole");
            Near(cuts[0].x,350,"Gap left edge differs from physics");Near(cuts[0].y,510,"Gap right edge differs from physics");
            CheckSnowSurfaceAvoids(350,510);
            Check(SnowSurfaceBounds().Any(r=>r.x<800&&r.xMax>820),"The intact bridge lacks a visible supporting road surface");
            bridge.Collapsed=true;bridge.CollapseProgress=1;
            Draw(view,run,Canvas(1));
            Check(cuts.Count==2,"Collapsed floor still has road or snow over its opening");
            Near(cuts[1].x,720,"Collapsed floor left edge differs from physics");Near(cuts[1].y,900,"Collapsed floor right edge differs from physics");
            CheckSnowSurfaceAvoids(350,510);CheckSnowSurfaceAvoids(720,900);
        }
    }
    private static Rect VisibleBounds(RecordedDraw draw)
    {
        Vector3[] corners={
            draw.Matrix.MultiplyPoint3x4(new Vector3(draw.Position.x,draw.Position.y,0)),
            draw.Matrix.MultiplyPoint3x4(new Vector3(draw.Position.xMax,draw.Position.y,0)),
            draw.Matrix.MultiplyPoint3x4(new Vector3(draw.Position.x,draw.Position.yMax,0)),
            draw.Matrix.MultiplyPoint3x4(new Vector3(draw.Position.xMax,draw.Position.yMax,0))};
        float left=corners.Min(p=>p.x),right=corners.Max(p=>p.x),top=corners.Min(p=>p.y),bottom=corners.Max(p=>p.y);
        if(draw.Clip.HasValue)
        {
            Rect clip=draw.Clip.Value;
            left=Math.Max(left,clip.x);right=Math.Min(right,clip.xMax);
            top=Math.Max(top,clip.y);bottom=Math.Min(bottom,clip.yMax);
        }
        return new Rect(left,top,Math.Max(0,right-left),Math.Max(0,bottom-top));
    }
    private static List<Rect> SnowSurfaceBounds()
    {
        return GUI.DrawCalls.Where(d=>d.Kind=="Texture"&&d.Texture==Texture2D.whiteTexture&&d.Position.height==7
            &&d.Color.r>0.9f&&d.Color.g>0.95f&&d.Color.a>0.95f).Select(VisibleBounds)
            .Where(r=>r.width>0&&r.height>0&&r.y>=RunnerSession.GroundY-0.01f).ToList();
    }
    private static void CheckSnowSurfaceAvoids(float left,float right)
    {
        var snow=SnowSurfaceBounds();Check(snow.Count>0,"Snow road fixture was not rendered");
        Check(!snow.Any(r=>r.x<right-1&&r.xMax>left+1),"A snow/ice surface was painted over the physical hole "+left+".."+right);
    }
    private static void SnowSurface()
    {
        var art=new RunnerArt();var run=New(District.Snow);Set(run,"Elapsed",4f);
        using(var view=new RunnerWorldView(art)) Draw(view,run,Canvas(1));
        Check(SnowSurfaceBounds().Sum(r=>r.width)>1200,"Snow-covered road surface is missing");
        int flakes=GUI.DrawCalls.Count(d=>d.Kind=="Texture"&&d.Texture!=null&&d.Position.width<=8
            &&d.Position.height<=8&&d.Position.y<RunnerSession.GroundY&&d.Color.r>0.85f&&d.Color.b>0.9f&&d.Color.a>0.1f);
        Check(flakes>=40,"Snowfall is missing or not distinct from the storm rain (flakes="+flakes+")");
    }
    private static void PickupSprites()
    {
        var art=new RunnerArt();var run=New();
        Check(art.PickupAtlas!=null,"Survival/power-up atlas was not included in resources");
        var kinds=new[]{EntityKind.Water,EntityKind.HotDrink,EntityKind.DoubleScore,EntityKind.Magnet,EntityKind.Shield};
        for(int i=0;i<kinds.Length;i++)World(run).Add(new WorldEntity{Id=i+1,Kind=kinds[i],X=300+i*160,Y=10,Width=32,Height=32});
        using(var view=new RunnerWorldView(art)) Draw(view,run,Canvas(1.5f,80,0));
        foreach(WorldEntity pickup in run.Entities)
        {
            float y=RunnerSession.GroundY-pickup.Y-pickup.Height;
            RecordedDraw draw=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&d.Texture==art.PickupAtlas&&d.Position.x==pickup.X
                &&Math.Abs(d.Position.y-y)<0.01f&&d.Position.width==pickup.Width&&d.Position.height==pickup.Height&&d.Color.a>0);
            Rect expected=art.GetPickupUV(pickup.Kind);
            Check(expected.width>0&&expected.height>0&&expected.x>=0&&expected.y>=0&&expected.xMax<=1.001f&&expected.yMax<=1.001f,"Pickup has invalid source bounds: "+pickup.Kind);
            Near(draw.UV.x,expected.x,"Pickup sampled a different icon");Near(draw.UV.y,expected.y,"Pickup sampled a different icon");
        }
        Check(kinds.Select(kind=>art.GetPickupUV(kind).x+":"+art.GetPickupUV(kind).y).Distinct().Count()==kinds.Length,"Different pickups accidentally share one sprite");
    }
    private static void PausedWeather()
    {
        foreach(District district in new[]{District.Snow,District.Storm})
        {
            var art=new RunnerArt();var run=New(district);Set(run,"Elapsed",6f);Set(run,"DistrictElapsed",1.16f);
            run.TogglePause();
            using(var view=new RunnerWorldView(art))
            {
                Draw(view,run,Canvas(1));string first=DrawSnapshot();
                GUI.DrawCalls.Clear();view.Draw(run,50f);
                Check(DrawSnapshot()==first,"Snow/rain/lightning advanced while paused on "+district);
            }
        }
    }
    private static string DrawSnapshot()
    {
        return string.Join(";",GUI.DrawCalls.Select(d=>d.Kind+":"+d.Position.x+":"+d.Position.y+":"+
            d.Position.width+":"+d.Position.height+":"+d.Centre.x+":"+d.Centre.y+":"+
            d.Color.r+":"+d.Color.g+":"+d.Color.b+":"+d.Color.a+":"+
            d.UV.x+":"+d.UV.y+":"+d.UV.width+":"+d.UV.height+":"+(d.Texture==null?"":d.Texture.name)));
    }
    private static WorldEntity BiomeFixture(EntityKind kind,int id=101)
    {
        return new WorldEntity { Id=id,Kind=kind,X=420,Y=kind==EntityKind.HeatVent?0:120,
            Width=kind==EntityKind.StormDebris?104:48,Height=kind==EntityKind.StormDebris?54:100,
            HazardActive=true,HazardPhase=1,HazardHeight=100,HazardAge=2,HazardWarning=1,Rotation=37 };
    }
    private static void BiomeSprites()
    {
        var art=new RunnerArt();Check(art.BiomeHazardAtlas!=null,"Biome hazard atlas is missing from resources");
        var kinds=new[]{EntityKind.FallingIce,EntityKind.HeatVent,EntityKind.StormDebris};
        foreach(EntityKind kind in kinds)
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(0.68f,119,28),Canvas(1.5f,320,140)})
        {
            var run=New();var entity=BiomeFixture(kind);World(run).Add(entity);
            using(var view=new RunnerWorldView(art)) Draw(view,run,canvas);
            RecordedDraw draw=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&d.Texture==art.BiomeHazardAtlas);
            Rect uv=art.GetBiomeHazardUV(kind);
            Check(uv.width>0&&uv.height>0&&uv.x>=0&&uv.y>=0&&uv.xMax<=1.001f&&uv.yMax<=1.001f,"Invalid hazard atlas bounds for "+kind);
            Near(draw.UV.x,uv.x,"Hazard sampled wrong sprite X");Near(draw.UV.y,uv.y,"Hazard sampled wrong sprite Y");
            Near(draw.UV.width,uv.width,"Hazard sampled wrong sprite width");Near(draw.UV.height,uv.height,"Hazard sampled wrong sprite height");
            float height=kind==EntityKind.HeatVent?entity.HazardHeight:entity.Height;
            float top=RunnerSession.GroundY-entity.Y-height;
            Near(draw.Position.x,entity.X,"Hazard image X differs from physics");Near(draw.Position.y,top,"Hazard image Y differs from physics");
            Near(draw.Position.width,entity.Width,"Hazard image width differs from physics");Near(draw.Position.height,height,"Hazard image height differs from physics");
            Point(draw.Centre,canvas.MultiplyPoint3x4(new Vector3(entity.X+entity.Width/2,top+height/2,0)),"Hazard centre moved under letterboxing");
            Point(draw.Matrix.MultiplyPoint3x4(new Vector3(draw.Position.x,draw.Position.y,0)),canvas.MultiplyPoint3x4(new Vector3(entity.X,top,0)),"Hazard art rotated away from its collision rectangle");
            Check(!GUI.DrawCalls.Any(d=>d.Kind=="Label"&&(d.Text=="CÚI"||d.Text=="NHẢY")),"Biome hazard restored floating action words");
        }
        Check(kinds.Select(kind=>art.GetBiomeHazardUV(kind).x+":"+art.GetBiomeHazardUV(kind).y).Distinct().Count()==3,"Biome hazards share the same atlas icon");
    }
    private static void BiomeWarnings()
    {
        var art=new RunnerArt();
        foreach(EntityKind kind in new[]{EntityKind.FallingIce,EntityKind.HeatVent,EntityKind.StormDebris})
        {
            var run=New();var entity=BiomeFixture(kind);entity.HazardPhase=0;entity.HazardActive=false;entity.HazardWarning=0.7f;
            World(run).Add(entity);
            using(var view=new RunnerWorldView(art))
            {
                Draw(view,run,Canvas(1));
                Check(!RunnerBiomeHazards.IsDamaging(entity),"Warning fixture is physically damaging: "+kind);
                var warning=GUI.DrawCalls.Where(d=>d.Kind=="UV"&&d.Texture==art.BiomeHazardAtlas).ToList();
                if(kind==EntityKind.HeatVent)
                {
                    Check(warning.Count==0,"Inactive vent has a visible lethal plume");
                    Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&d.Position.x>=entity.X&&d.Position.xMax<=entity.X+entity.Width
                        &&d.Position.y<RunnerSession.GroundY&&d.Position.y>=RunnerSession.GroundY-16&&d.Position.height<=4&&d.Color.a>0.2f),
                        "Heating vent has no readable warning above the road before its plume");
                    entity.HazardPhase=2;Draw(view,run,Canvas(1));
                    Check(!GUI.DrawCalls.Any(d=>d.Kind=="UV"&&d.Texture==art.BiomeHazardAtlas),"Safe vent retained its lethal plume sprite");
                }
                else
                {
                    Check(warning.Count==1&&warning[0].Color.a>0,"Warning hazard has no readable visible object: "+kind);
                    float alpha=warning[0].Color.a;
                    entity.HazardPhase=1;entity.HazardActive=true;
                    Draw(view,run,Canvas(1));
                    var active=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&d.Texture==art.BiomeHazardAtlas);
                    Check(active.Color.a>alpha,"Ghost warning art is indistinguishable from its active damaging sprite: "+kind);
                }
            }
        }
    }
    private static void VentPlumeHeight()
    {
        var art=new RunnerArt();var run=New(District.Sunset);var entity=BiomeFixture(EntityKind.HeatVent);World(run).Add(entity);
        using(var view=new RunnerWorldView(art))
        foreach(float height in new[]{0f,6f,38f,95f,132f})
        {
            entity.HazardHeight=height;entity.HazardActive=height>0;entity.HazardPhase=height>0?1:2;
            Draw(view,run,Canvas(1.5f,90,25));
            var plume=GUI.DrawCalls.Where(d=>d.Kind=="UV"&&d.Texture==art.BiomeHazardAtlas).ToList();
            Check(plume.Count==(height>0?1:0),"Vent flame does not match its active/safe phase at height "+height);
            if(height>0)
            {
                Near(plume[0].Position.height,height,"Vent flame and collision height diverged");
                Near(plume[0].Position.yMax,RunnerSession.GroundY-entity.Y,"Vent flame floats away from its physical origin");
            }
        }
    }
    private static void PausedBiomeHazards()
    {
        var art=new RunnerArt();
        foreach(EntityKind kind in new[]{EntityKind.FallingIce,EntityKind.HeatVent,EntityKind.StormDebris})
        foreach(int phase in new[]{0,1,2})
        {
            var run=New();Set(run,"Elapsed",7);var entity=BiomeFixture(kind);entity.HazardPhase=phase;entity.HazardActive=phase==1;
            entity.HazardWarning=0.6f;World(run).Add(entity);run.TogglePause();
            using(var view=new RunnerWorldView(art))
            {
                Draw(view,run,Canvas(1));string first=DrawSnapshot();
                GUI.DrawCalls.Clear();view.Draw(run,90f);
                Check(DrawSnapshot()==first,"Biome art advanced on ambient wall time during pause: "+kind+" phase "+phase);
            }
        }
    }
    private static void ActionSprites()
    {
        var art=new RunnerArt(); Check(art.DinoActionsAtlas!=null,"Action atlas is missing from resources");
        foreach(bool left in new[]{false,true})
        for(int frame=5;frame<=10;frame++)
        {
            var run=New();
            bool duck=frame<8;
            Set(run,"Elapsed",duck?(frame-5+0.1f)/10f:0.5f);
            Set(run,"PlayerY",duck?0:80);
            Set(run,"AnimationVelocityY",frame==8?500:frame==10?-500:0);
            typeof(RunnerSession).GetProperty("IsDucking").GetSetMethod(true).Invoke(run,new object[]{duck});
            typeof(RunnerSession).GetProperty("FacingLeft").GetSetMethod(true).Invoke(run,new object[]{left});
            Check(RunnerGeometry.GetPlayerFrame(run)==frame,"Shared pose selection did not choose action frame "+frame);
            using(var view=new RunnerWorldView(art)) Draw(view,run,Canvas(1.5f,100,0));
            RecordedDraw draw=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&d.Texture==art.DinoActionsAtlas);
            Rect source=art.GetDinoActionUV(frame);
            Near(draw.UV.x,left?source.x+source.width:source.x,"Action frame source X mismatch");
            Near(draw.UV.y,source.y,"Action frame source Y mismatch");
            Near(draw.UV.width,left?-source.width:source.width,"Action frame was not mirrored correctly");
            Near(draw.Position.width,duck?109:99,"Action draw width differs from collision layout");
            Near(draw.Position.height,duck?44:91,"Action draw height differs from collision layout");
        }
    }

    private static RecordedDraw RunningDraw(RunnerArt art)
    {
        return GUI.DrawCalls.Single(d=>d.Kind=="UV"&&d.Texture==art.DinoRunAtlas);
    }

    private static RunnerSession RunningPose(int frame,bool left=false)
    {
        var run=New();
        float rate=9f+run.Speed/150f;
        Set(run,"Elapsed",(frame+0.35f)/rate);
        typeof(RunnerSession).GetProperty("FacingLeft").GetSetMethod(true).Invoke(run,new object[]{left});
        Check(RunnerGeometry.GetPlayerFrame(run)==frame,"Running fixture did not choose frame "+frame);
        return run;
    }

    private static void RunningSprites()
    {
        var art=new RunnerArt();
        Check(art.DinoRunAtlas!=null,"Running atlas is missing from build-safe resources");
        // Opaque bounds measured independently from the generated PNG. Crops
        // vary, while their common canvas and pixel scale must stay unchanged.
        int[][] bounds={new[]{18,199,431,548},new[]{441,199,851,549},new[]{852,199,1274,548},
            new[]{1288,206,1712,532},new[]{1724,201,2154,548}};
        var sources=Enumerable.Range(0,5).Select(art.GetDinoRunUV).ToList();
        Check(sources.Select(uv=>uv.x+":"+uv.y).Distinct().Count()==5,"Running poses share the same source frame");
        for(int frame=0;frame<5;frame++)
        {
            Rect source=sources[frame];int[] box=bounds[frame];
            Check(source.x>=0&&source.y>=0&&source.xMax<=1.000001f&&source.yMax<=1.000001f,
                "Running viewport leaves the atlas");
            Near(source.x*art.DinoRunAtlas.width,box[0],"Running crop samples adjacent frame pixels at left edge");
            Near(source.xMax*art.DinoRunAtlas.width,box[2],"Running crop samples adjacent frame pixels at right edge");
            Near((1-source.yMax)*art.DinoRunAtlas.height,box[1],"Running crop top differs from generated artwork");
            Near((1-source.y)*art.DinoRunAtlas.height,box[3],"Running crop bottom differs from generated artwork");
        }
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(0.68f,119,28),Canvas(1.5f,320,0)})
        foreach(bool left in new[]{false,true})
        for(int frame=0;frame<5;frame++)
        {
            RunnerSession run=RunningPose(frame,left);
            using(var view=new RunnerWorldView(art)) Draw(view,run,canvas);
            RecordedDraw draw=RunningDraw(art);
            Rect source=art.GetDinoRunUV(frame);
            Near(draw.UV.x,left?source.xMax:source.x,"Running frame source X mismatch");
            Near(draw.UV.y,source.y,"Running frame source Y mismatch");
            Near(draw.UV.width,left?-source.width:source.width,"Running frame was not mirrored correctly");
            Near(draw.UV.height,source.height,"Running frame source height mismatch");
            int[] box=bounds[frame];float pixelWidth=box[2]-box[0],pixelHeight=box[3]-box[1];
            Near(draw.Position.width/pixelWidth,RunnerGeometry.PlayerStandingWidth/434f,"Running horizontal pixel scale changes between poses");
            Near(draw.Position.height/pixelHeight,RunnerGeometry.PlayerStandingHeight/354f,"Running vertical pixel scale changes between poses");
            float feet=RunnerSession.GroundY+RunnerGeometry.GetPlayerBob(run.Elapsed,0,false,run.State);
            float leftPixel=left?2f:432f-pixelWidth;
            float targetX=run.PlayerX-RunnerGeometry.PlayerStandingWidth/2+leftPixel/434f*RunnerGeometry.PlayerStandingWidth;
            float targetY=feet-RunnerGeometry.PlayerStandingHeight+(box[1]-197f)/354f*RunnerGeometry.PlayerStandingHeight;
            Near(draw.Position.x,targetX,"Running crop lost its shared nose anchor when mirrored");
            Near(draw.Position.y,targetY,"Running crop differs from common feet baseline");
            Near(draw.Position.yMax,feet-(551f-box[3])/354f*RunnerGeometry.PlayerStandingHeight,
                "Running leg motion was incorrectly snapped to each crop's bottom");
            Point(draw.Centre,canvas.MultiplyPoint3x4(new Vector3(targetX+draw.Position.width/2,targetY+draw.Position.height/2,0)),
                "Running frame moved away from physical body under canvas transform");
            Check(!GUI.DrawCalls.Any(d=>art.DinoFrames.Contains(d.Texture)),"Legacy running frame was drawn over the new atlas");
        }
    }

    private static void RunningLifecycle()
    {
        var art=new RunnerArt();Check(art.DinoRunAtlas!=null,"Running atlas is missing");
        for(int frame=0;frame<5;frame++)
        {
            RunnerSession run=RunningPose(frame);
            using(var view=new RunnerWorldView(art))
            {
                Draw(view,run,Canvas(1));RecordedDraw before=RunningDraw(art);
                run.TogglePause();run.Update(5);
                GUI.DrawCalls.Clear();GUI.matrix=Canvas(1);view.Draw(run,1000);
                RecordedDraw paused=RunningDraw(art);
                Near(paused.UV.x,before.UV.x,"Pause changed the selected run frame");
                Near(paused.Position.y,before.Position.y,"Pause changed the running foot baseline");
                run.TogglePause();
                World(run).Add(new WorldEntity{Kind=EntityKind.Barrier,X=run.PlayerX-80,Y=0,Width=160,Height=200});
                run.Update(1f/120);
                Check(run.State==RunState.GameOver,"Fatal-contact fixture did not end the run");
                Draw(view,run,Canvas(1));RecordedDraw dead=RunningDraw(art);
                int deathFrame=RunnerGeometry.GetPlayerFrame(run);
                run.Update(5);GUI.DrawCalls.Clear();GUI.matrix=Canvas(1);view.Draw(run,2000);
                RecordedDraw frozen=RunningDraw(art);
                Near(dead.UV.x,art.GetDinoRunUV(deathFrame).x,"Death drew a different run pose than collision geometry");
                Near(frozen.UV.x,dead.UV.x,"Death animation continued running");
                Near(frozen.Position.y,dead.Position.y,"Dead run pose continued bobbing");
            }

            var controller=new CityRunnerController();
            typeof(CityRunnerController).GetField("art",Private).SetValue(controller,art);
            typeof(CityRunnerController).GetField("ambientTime",Private).SetValue(controller,(frame+0.35f)/7f);
            GUI.DrawCalls.Clear();GUI.matrix=Canvas(1.5f,100,0);GUI.color=Color.white;
            typeof(CityRunnerController).GetMethod("DrawMenuDino",Private).Invoke(controller,null);
            RecordedDraw menu=RunningDraw(art);Rect source=art.GetDinoRunUV(frame);
            Near(menu.UV.x,source.x,"Menu does not use the gameplay running atlas frame");
            Near(menu.UV.y,source.y,"Menu running source row differs");
            float worldPixelWidth=source.width*art.DinoRunAtlas.width;
            float worldPixelHeight=source.height*art.DinoRunAtlas.height;
            Near(menu.Position.width/worldPixelWidth,164f/434f,"Menu changes running horizontal pixel scale between frames");
            Near(menu.Position.height/worldPixelHeight,151f/354f,"Menu changes running vertical pixel scale between frames");
        }

        // Optional atlas fallback retains the supplied original frames if the
        // new resource is absent during an incremental Editor import.
        UnityEngine.Object atlas=Resources.TestAssets["CityDino/DinoRunAtlas"];
        try
        {
            Resources.TestAssets.Remove("CityDino/DinoRunAtlas");
            var legacy=new RunnerArt();RunnerSession run=RunningPose(3,true);
            using(var view=new RunnerWorldView(legacy)) Draw(view,run,Canvas(1));
            RecordedDraw draw=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&d.Texture==legacy.DinoFrames[3]);
            Near(draw.UV.x,1,"Legacy running fallback lost its facing");Near(draw.UV.width,-1,"Legacy running fallback is not mirrored");
        }
        finally { Resources.TestAssets["CityDino/DinoRunAtlas"]=atlas; }
    }
    private static void Field(object instance,string name,object value)
    {
        FieldInfo field=instance.GetType().GetField(name,Private);
        Check(field!=null,"Fixture field missing: "+name);field.SetValue(instance,value);
    }
    private static RunnerSession NewCharacter(CharacterKind character,District district=District.Day)
    {
        var run=new RunnerSession(new RunnerSave{OwnsTriceratops=true,OwnsRaptor=true,SelectedCharacter=character},18);
        run.SetStartingDistrict(district);run.StartRun();World(run).Clear();return run;
    }
    private static RunnerSession ArenaFixture(BossKind kind=BossKind.Tank,CharacterKind character=CharacterKind.Classic,District district=District.Day)
    {
        RunnerSession run=BeginArena(kind,character,district);
        for(int i=0;i<600&&run.ArenaPhase!=BossPhase.Fighting;i++)run.Update(1f/120);
        Check(run.ArenaPhase==BossPhase.Fighting&&run.CurrentBoss!=null,"Arena fixture did not reach fighting");
        BossEncounter boss=run.CurrentBoss;boss.X=970;boss.Y=kind==BossKind.Gunship?180:kind==BossKind.Submarine?135:0;
        boss.Width=kind==BossKind.Sentinel?158:kind==BossKind.Submarine?290:238;boss.Height=kind==BossKind.Sentinel?216:kind==BossKind.Submarine?140:128;
        boss.Vulnerable=true;return run;
    }
    private static RunnerSession BeginArena(BossKind kind,CharacterKind character=CharacterKind.Classic,District district=District.Day)
    {
        RunnerSession run=NewCharacter(character,district);
        Field(run,"distanceToPattern",float.MaxValue);Field(run,"nextHeartAt",float.MaxValue);
        int ordinal=kind==BossKind.Tank?0:kind==BossKind.Gunship?1:kind==BossKind.Submarine?2:3;
        int milestone=(ordinal+1)*RunnerSession.BossScoreInterval;
        Field(run,"<BossNumber>k__BackingField",ordinal);
        Field(run,"<NextBossScore>k__BackingField",milestone);
        Field(run,"distanceClock",milestone*20d-1d);Set(run,"Distance",milestone*20f-1f);run.Update(.02f);
        Check(run.IsBossArena&&run.CurrentBoss.Kind==kind,"Normal milestone fixture selected the wrong real boss");
        return run;
    }
    private static void ArenaIsolation()
    {
        var art=new RunnerArt();Check(art.BossAtlas!=null,"Boss atlas missing");
        foreach(District district in new[]{District.Night,District.Storm,District.Snow})
        {
            RunnerSession run=ArenaFixture(district:district);Set(run,"DistrictElapsed",1.16f);Set(run,"NextBlackoutAt",1f);Set(run,"BlackoutDuration",2.5f);
            World(run).Add(new WorldEntity{Kind=EntityKind.Saw,X=350,Y=0,Width=80,Height=80});
            World(run).Add(new WorldEntity{Kind=EntityKind.Gap,X=450,Y=0,Width=200,Height=160});
            using(var view=new RunnerWorldView(art))Draw(view,run,Canvas(1.5f,120,0));
            Check(GUI.DrawCalls.Any(d=>BossBodyArt(d,art)),"Boss disappeared behind ordinary map rendering");
            Check(!GUI.DrawCalls.Any(d=>d.Texture==art.HazardAtlas||d.Texture==art.Road||d.Texture==art.SkylineFar||d.Texture==art.SkylineNear),"Scrolling world leaked into boss arena");
            Check(!GUI.DrawCalls.Any(FullCanvas),"District blackout or lightning covered the arena");
            // Actual arena ground is continuous even when the retained run had a hole.
            Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&d.Position.x==0&&d.Position.y==RunnerSession.GroundY&&d.Position.width==1280&&d.Position.height>=100),"Arena lacks a continuous safe floor");
        }
    }
    private static void BossSprites()
    {
        var art=new RunnerArt();Check(art.BossAtlas!=null,"Boss atlas missing");
        var distinct=new HashSet<string>();
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            Rect uv=kind==BossKind.Submarine?art.GetSubmarineUV(0):art.GetBossUV(kind);distinct.Add(kind+":"+uv.x+":"+uv.y);
            Check(uv.width>0&&uv.height>0&&uv.x>=0&&uv.y>=0&&uv.xMax<=1.000001f&&uv.yMax<=1.000001f,"Boss UV is outside atlas");
            foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
            {
                RunnerSession run=ArenaFixture(kind);BossEncounter boss=run.CurrentBoss;
                using(var view=new RunnerWorldView(art))Draw(view,run,canvas);
                RecordedDraw draw=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&BossBodyArt(d,art));
                Near(draw.Position.x,boss.X,"Boss image moved away from physical X");Near(draw.Position.y,RunnerSession.GroundY-boss.Y-boss.Height,"Boss image moved away from physical height");
                Near(draw.Position.width,boss.Width,"Boss image width differs from collision body");Near(draw.Position.height,boss.Height,"Boss image height differs from collision body");
                Rect expectedUV=ExpectedBossBodyUV(art,boss);Near(draw.UV.x,expectedUV.x,"Boss selected another body sprite");Near(draw.UV.y,expectedUV.y,"Boss source row differs");
                Point(draw.Centre,canvas.MultiplyPoint3x4(new Vector3(boss.X+boss.Width/2,RunnerSession.GroundY-boss.Y-boss.Height/2,0)),"Boss centre drifts under canvas scale");
            }
        }
        Check(distinct.Count==4,"Four bosses reuse the same source image");
    }
    private static bool BossBodyArt(RecordedDraw draw,RunnerArt art)
    {
        return draw.Kind=="UV" && (draw.Texture==art.BossAtlas || draw.Texture!=null&&(draw.Texture.name=="BossActionAtlas"||draw.Texture.name=="SubmarineAtlas"));
    }
    private static Rect ExpectedBossBodyUV(RunnerArt art,BossEncounter boss)
    {
        return boss.Kind==BossKind.Submarine?art.GetSubmarineUV((int)boss.Action):art.BossActionAtlas!=null ? art.GetBossActionUV(boss.Kind,(int)boss.Action) : art.GetBossUV(boss.Kind);
    }
    private static bool SameRect(Rect a,Rect b)
    {return Math.Abs(a.x-b.x)<.025f&&Math.Abs(a.y-b.y)<.025f&&Math.Abs(a.width-b.width)<.025f&&Math.Abs(a.height-b.height)<.025f;}
    private static void BossAttackGeometry()
    {
        var art=new RunnerArt();RunnerSession run=ArenaFixture();
        var shots=(IList<BossProjectile>)run.BossProjectiles;shots.Clear();
        var signals=(IList<BossTelegraph>)run.BossTelegraphs;signals.Clear();
        var friendly=new BossProjectile{Id=100,Friendly=true,X=321,Y=123,Width=18,Height=8};
        var enemy=new BossProjectile{Id=101,Friendly=false,Kind=BossAttackKind.Cannon,X=564,Y=51,Width=30,Height=22,Radius=11,VelocityX=-425};
        var removed=new BossProjectile{Id=102,Friendly=false,X=631,Y=66,Width=31,Height=17,Removed=true};
        shots.Add(friendly);shots.Add(enemy);shots.Add(removed);
        var signal=new BossTelegraph{Id=103,Kind=BossAttackKind.Artillery,X=428,Y=0,Width=73,Height=113,Remaining=.4f,Duration=1,Active=false};signals.Add(signal);
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
        {
            using(var view=new RunnerWorldView(art))Draw(view,run,canvas);
            foreach(BossProjectile shot in new[]{friendly,enemy})
            {
                Rect physical=new Rect(shot.X,RunnerSession.GroundY-shot.Y-shot.Height,shot.Width,shot.Height);
                Rect damage=shot.Friendly?physical:new Rect(physical.center.x-shot.Radius,physical.center.y-shot.Radius,shot.Radius*2,shot.Radius*2);
                RecordedDraw draw=GUI.DrawCalls.Single(d=>d.Texture?.name=="CityDino disc"&&SameRect(d.Position,damage));
                Point(draw.Centre,canvas.MultiplyPoint3x4(physical.center),"Projectile left its physical position under letterbox");
                if(shot.Friendly)Check(draw.Color.b>draw.Color.r,"Friendly energy is not distinguished from enemy ordnance");
                else Check(GUI.DrawCalls.Any(d=>d.Texture==art.OrdnanceAtlas&&SamePoint(d.Centre,canvas.MultiplyPoint3x4(physical.center))),"Enemy shell lacks its centered source art");
            }
            Check(!GUI.DrawCalls.Any(d=>SameRect(d.Position,new Rect(removed.X,RunnerSession.GroundY-removed.Y-removed.Height,removed.Width,removed.Height))),"Removed projectile is still visible");
            Rect warning=new Rect(signal.X,RunnerSession.GroundY-signal.Y-signal.Height,signal.Width,signal.Height);
            RecordedDraw fill=GUI.DrawCalls.Single(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,warning));
            Check(fill.Color.a<.3f,"Harmless attack warning looks like a fully active hitbox");
            signal.Active=true;using(var view=new RunnerWorldView(art))Draw(view,run,canvas);
            RecordedDraw active=GUI.DrawCalls.Single(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,warning));
            Check(active.Color.a>=.6f&&active.Color.r>active.Color.b,"Active attack does not paint its whole physical area");
            Point(active.Centre,canvas.MultiplyPoint3x4(warning.center),"Telegraph hitbox shifted under scaling");signal.Active=false;
        }
    }
    private static void BossWeakPoint()
    {
        var art=new RunnerArt();RunnerSession run=ArenaFixture();BossEncounter boss=run.CurrentBoss;
        using(var view=new RunnerWorldView(art))
        {
            boss.Vulnerable=true;Draw(view,run,Canvas(1));
            float cx=boss.X+boss.Width/2,cy=RunnerSession.GroundY-boss.Y-boss.Height*.55f;
            var open=GUI.DrawCalls.Where(d=>Math.Abs(d.Position.center.x-cx)<.01&&Math.Abs(d.Position.center.y-cy)<.01&&!BossBodyArt(d,art)).ToList();
            Check(open.Any(d=>d.Color.r>.9&&d.Color.g>.7&&d.Color.b<.6),"Open weak point has no visible golden core");
            boss.Vulnerable=false;Draw(view,run,Canvas(1));
            var locked=GUI.DrawCalls.Where(d=>Math.Abs(d.Position.center.x-cx)<.01&&Math.Abs(d.Position.center.y-cy)<.01&&!BossBodyArt(d,art)).ToList();
            Check(locked.Any(d=>d.Color.b>d.Color.r&&d.Color.g>.5),"Protected weak point does not change to blue shielding");
            Check(GUI.DrawCalls.Count(d=>d.Texture==Texture2D.whiteTexture&&d.Position.height==3&&Math.Abs(d.Centre.x-cx)<.025f&&Math.Abs(d.Centre.y-cy)<.025f)>=2,"Protected core lacks crossed shield marks");
        }
    }
    private static string DrawSignature()
    {
        return string.Join(";",GUI.DrawCalls.Select(d=>d.Kind+":"+d.Text+":"+(d.Texture==null?"":d.Texture.name)+":"+d.Position.x+":"+d.Position.y+":"+d.Position.width+":"+d.Position.height+":"+d.UV.x+":"+d.UV.y+":"+d.UV.width+":"+d.Color.r+":"+d.Color.g+":"+d.Color.b+":"+d.Color.a));
    }
    private static void ArenaLifecycle()
    {
        var art=new RunnerArt();RunnerSession run=ArenaFixture(BossKind.Gunship);
        using(var view=new RunnerWorldView(art))
        {
            Draw(view,run,Canvas(1));run.TogglePause();GUI.DrawCalls.Clear();GUI.matrix=Canvas(1);view.Draw(run,1000);string paused=DrawSignature();
            run.Update(5);GUI.DrawCalls.Clear();GUI.matrix=Canvas(1);view.Draw(run,2000);
            Check(DrawSignature()==paused,"Boss arena rendering ran from ambient time while paused");
            run.TogglePause();
            typeof(RunnerSession).GetMethod("FinishRun",Private).Invoke(run,new object[]{"Fixture fatal boss contact"});
            Check(run.State==RunState.GameOver&&run.IsBossArena,"Dead boss encounter was discarded before drawing results");
            Draw(view,run,Canvas(1));string dead=DrawSignature();run.Update(5);GUI.DrawCalls.Clear();GUI.matrix=Canvas(1);view.Draw(run,3000);
            Check(DrawSignature()==dead,"Dead boss encounter continued to animate");
        }
    }
    private static RunnerSession CharacterPose(CharacterKind kind,int frame,bool left)
    {
        RunnerSession run=NewCharacter(kind);typeof(RunnerSession).GetProperty("FacingLeft").GetSetMethod(true).Invoke(run,new object[]{left});
        if(frame<5)Set(run,"Elapsed",(frame+.35f)/(9+run.Speed/150));
        else if(frame<8){Set(run,"Elapsed",(.2f+frame-5)/10);run.SetDucking(true);}
        else
        {
            Set(run,"PlayerY",40);Field(run,"grounded",false);
            typeof(RunnerSession).GetProperty("AnimationVelocityY").GetSetMethod(true).Invoke(run,new object[]{frame==8?500f:frame==9?0f:-500f});
        }
        Check(RunnerGeometry.GetPlayerFrame(run)==frame,"Character fixture selected wrong pose "+frame);return run;
    }
    private static void CharacterSprites()
    {
        var art=new RunnerArt();Check(art.TriceratopsAtlas!=null&&art.RaptorAtlas!=null,"Purchased character atlases are missing");
        foreach(CharacterKind kind in new[]{CharacterKind.Triceratops,CharacterKind.Raptor})
        {
            var uvCells=new HashSet<string>();
            foreach(int frame in new[]{0,1,2,5,8,10}){Rect uv=art.GetCharacterUV(kind,frame);uvCells.Add(uv.x+":"+uv.y);Check(uv.width>0&&uv.height>0&&uv.x>=0&&uv.y>=0&&uv.xMax<=1.000001f&&uv.yMax<=1.000001f,"Character UV leaves atlas");}
            Check(uvCells.Count==6,"Character atlas does not contain all six distinctive poses");
            foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
            foreach(bool left in new[]{false,true})
            for(int frame=0;frame<11;frame++)
            {
                RunnerSession run=CharacterPose(kind,frame,left);
                using(var view=new RunnerWorldView(art))Draw(view,run,canvas);
                Texture2D texture=art.GetCharacterTexture(kind,frame);RecordedDraw draw=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&d.Texture==texture);
                Rect uv=art.GetCharacterUV(kind,frame);Near(draw.UV.x,left?uv.xMax:uv.x,"Character source facing differs from body");Near(draw.UV.width,left?-uv.width:uv.width,"Character sprite not mirrored");Near(draw.UV.y,uv.y,"Character selected another source pose");
                bool duck=frame>=5&&frame<8;float w=duck?RunnerGeometry.PlayerDuckWidth:RunnerGeometry.PlayerStandingWidth,h=duck?RunnerGeometry.PlayerDuckHeight:RunnerGeometry.PlayerStandingHeight;
                float bob=RunnerGeometry.GetPlayerBob(run.PlayerAnimationTime,run.PlayerY,duck,run.State);
                Rect body=new Rect(run.PlayerX-w/2,RunnerSession.GroundY-run.PlayerY-h+bob,w,h);
                Rect expected=art.GetCharacterRect(kind,frame,body,left);
                Check(SameRect(draw.Position,expected),"Character crop or mirror differs from its physical pose");
                Point(draw.Centre,canvas.MultiplyPoint3x4(expected.center),"Character source moved away from the physical canvas");
                Check(!GUI.DrawCalls.Any(d=>d.Kind=="UV"&&(d.Texture==art.DinoRunAtlas||d.Texture==art.DinoActionsAtlas)),"Purchased character draws the classic dinosaur over itself");
            }
        }
    }
    private sealed class PngAlpha
    {
        internal int Width,Height;
        internal byte[] Pixels;
        internal int At(int x,int y)
        {return x<0||y<0||x>=Width||y>=Height?0:Pixels[y*Width+x];}
    }
    private static int BigEndian(byte[] bytes,int offset)
    {return (bytes[offset]<<24)|(bytes[offset+1]<<16)|(bytes[offset+2]<<8)|bytes[offset+3];}
    private static int Paeth(int a,int b,int c)
    {
        int p=a+b-c,da=Math.Abs(p-a),db=Math.Abs(p-b),dc=Math.Abs(p-c);
        return da<=db&&da<=dc?a:db<=dc?b:c;
    }
    private static PngAlpha ReadPngAlpha(string path)
    {
        // Independent test-only decoder for the generated non-interlaced RGBA8
        // source images. It does not read production collision bands or bake data.
        using(var file=File.OpenRead(path))
        using(var reader=new BinaryReader(file))
        using(var compressed=new MemoryStream())
        {
            byte[] signature=reader.ReadBytes(8);
            Check(signature.SequenceEqual(new byte[]{137,80,78,71,13,10,26,10}),"Invalid source PNG");
            var image=new PngAlpha();
            while(file.Position<file.Length)
            {
                byte[] length=reader.ReadBytes(4);int count=BigEndian(length,0);
                string kind=System.Text.Encoding.ASCII.GetString(reader.ReadBytes(4));
                byte[] data=reader.ReadBytes(count);Check(data.Length==count,"Truncated PNG data");
                Check(reader.ReadBytes(4).Length==4,"Truncated PNG checksum");
                if(kind=="IHDR")
                {
                    image.Width=BigEndian(data,0);image.Height=BigEndian(data,4);
                    Check(data[8]==8&&data[9]==6&&data[12]==0,"RGBA8 non-interlaced source PNG required");
                }
                else if(kind=="IDAT")compressed.Write(data,0,data.Length);
                else if(kind=="IEND")break;
            }
            Check(image.Width>0&&image.Height>0,"Source PNG has no dimensions");
            int stride=image.Width*4;byte[] raw=new byte[(stride+1)*image.Height];compressed.Position=0;
            using(var zlib=new ZLibStream(compressed,CompressionMode.Decompress,true))zlib.ReadExactly(raw);
            byte[] previous=new byte[stride],row=new byte[stride];image.Pixels=new byte[image.Width*image.Height];
            int position=0;
            for(int y=0;y<image.Height;y++)
            {
                int filter=raw[position++];Check(filter<=4,"Unsupported PNG scanline filter");
                for(int x=0;x<stride;x++)
                {
                    int a=x>=4?row[x-4]:0,b=previous[x],c=x>=4?previous[x-4]:0;
                    int prediction=filter==0?0:filter==1?a:filter==2?b:filter==3?(a+b)/2:Paeth(a,b,c);
                    row[x]=unchecked((byte)(raw[position++]+prediction));
                }
                for(int x=0;x<image.Width;x++)image.Pixels[y*image.Width+x]=row[x*4+3];
                byte[] old=previous;previous=row;row=old;
            }
            return image;
        }
    }
    private static int RenderedAlpha(PngAlpha png,Rect placed,Rect uv,bool left,float x,float screenY)
    {
        float u=(x-placed.x)/placed.width,v=(screenY-placed.y)/placed.height;
        if(u<0||u>=1||v<0||v>=1)return 0;
        float sourceX=(left?uv.xMax-u*uv.width:uv.x+u*uv.width)*png.Width;
        float sourceY=(1f-uv.y-(1f-v)*uv.height)*png.Height;
        return png.At((int)Math.Floor(sourceX),(int)Math.Floor(sourceY));
    }
    private static void CharacterAlphaContacts()
    {
        var art=new RunnerArt();int checkedOpaque=0,checkedClear=0;
        foreach(CharacterKind kind in new[]{CharacterKind.Triceratops,CharacterKind.Raptor})
        {
            Texture2D texture=art.GetCharacterTexture(kind,0);Check(texture!=null,"Companion source atlas is missing");
            PngAlpha png=ReadPngAlpha(Path.Combine(assetDirectory,texture.name+".png"));
            foreach(int frame in new[]{0,1,2,5,8,10})
            foreach(bool left in new[]{false,true})
            {
                RunnerSession run=CharacterPose(kind,frame,left);bool duck=run.IsDucking;
                float width=duck?RunnerGeometry.PlayerDuckWidth:RunnerGeometry.PlayerStandingWidth;
                float height=duck?RunnerGeometry.PlayerDuckHeight:RunnerGeometry.PlayerStandingHeight;
                float bob=RunnerGeometry.GetPlayerBob(run.PlayerAnimationTime,run.PlayerY,duck,run.State);
                float feet=run.PlayerY-bob;
                Rect body=new Rect(run.PlayerX-width/2,RunnerSession.GroundY-feet-height,width,height);
                Rect placed=art.GetCharacterRect(kind,frame,body,left),uv=art.GetCharacterUV(kind,frame);
                int opaque=0,clear=0;
                for(float y=5;y<height-4;y+=6)
                for(float x=-width/2+5;x<width/2-4;x+=6)
                {
                    float px=run.PlayerX+x,py=feet+y,screenY=RunnerSession.GroundY-py;
                    bool allOpaque=true,allClear=true;
                    // Stay beyond the forgiving one-pixel outline and the
                    // two-logical-row approximation used by production masks.
                    foreach(float dy in new[]{-3f,0f,3f})
                    foreach(float dx in new[]{-3f,0f,3f})
                    {
                        int alpha=RenderedAlpha(png,placed,uv,left,px+dx,screenY+dy);
                        allOpaque&=alpha>=128;allClear&=alpha<128;
                    }
                    if(!allOpaque&&!allClear)continue;
                    bool contact=RunnerGeometry.CircleTouchesPlayer(px,py,.6f,run);
                    Check(contact==allOpaque,"PNG opacity/contact differs for "+kind+" frame="+frame+" left="+left+" point="+x+","+y+" opaque="+allOpaque);
                    if(allOpaque)opaque++;else clear++;
                }
                Check(opaque>=5&&clear>=5,"Opacity sampling missed body or transparent space for "+kind+" frame="+frame);
                checkedOpaque+=opaque;checkedClear+=clear;
            }
        }
        Console.WriteLine("  PNG alpha independently checked "+checkedOpaque+" opaque and "+checkedClear+" clear character points.");
    }
    private static void CharacterPreviews()
    {
        var art=new RunnerArt();using(var view=new RunnerWorldView(art))
        foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        foreach(int frame in new[]{0,1,2,5,8,10})
        {
            GUI.DrawCalls.Clear();GUI.matrix=Canvas(1.5f,140,0);GUI.color=Color.white;
            Rect body=new Rect(350,220,198,182);view.DrawCharacterPreview(kind,frame,body,true);
            Texture2D texture=art.GetCharacterTexture(kind,frame);RecordedDraw draw=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&d.Texture==texture);
            Rect expected=art.GetCharacterRect(kind,frame,body,true),uv=art.GetCharacterUV(kind,frame);
            Check(SameRect(draw.Position,expected),"Menu preview differs from selected character body");Near(draw.UV.x,uv.xMax,"Menu preview lost facing");Near(draw.UV.width,-uv.width,"Menu preview did not mirror source");
        }
    }
    private static CityRunnerController Ui(RunnerSession run,RunnerArt art,RunnerWorldView view)
    {
        var controller=new CityRunnerController();Field(controller,"session",run);Field(controller,"art",art);Field(controller,"world",view);
        typeof(CityRunnerController).GetMethod("EnsureStyles",Private).Invoke(controller,null);return controller;
    }
    private static void DrawUi(CityRunnerController controller)
    {
        GUI.DrawCalls.Clear();GUI.matrix=Canvas(1);GUI.color=Color.white;GUI.enabled=true;
        typeof(CityRunnerController).GetMethod("OnGUI",Private).Invoke(controller,null);
    }
    private static void CombatHud()
    {
        var art=new RunnerArt();
        foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=NewCharacter(kind);var controller=Ui(run,art,view);DrawUi(controller);
            Check(!GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.StartsWith("BẮN")),"Shooting button appeared outside the arena");
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&Math.Abs(d.Position.x-854)<.01f&&Math.Abs(d.Position.width-120)<.01f)==(kind!=CharacterKind.Classic),"Skill button visibility differs from selected character");
            run=ArenaFixture(character:kind);run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*.5f;controller=Ui(run,art,view);DrawUi(controller);
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.StartsWith("BẮN")),kind+" lacks boss fire control");
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&Math.Abs(d.Position.x-854)<.01f&&Math.Abs(d.Position.width-120)<.01f)==(kind!=CharacterKind.Classic),"Boss skill button differs from selected character");
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.Contains("K-01")),"Boss HUD has no encounter name");
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text==run.Score.ToString("D5")),"Frozen running score is missing in combat");
            Check(!GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&(d.Text.StartsWith("THÂN NHIỆT")||d.Text.StartsWith("NƯỚC "))),"Survival HUD leaked into the arena");
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Box"&&SameRect(d.Position,new Rect(340,148,332,17))),"Half-health boss HUD does not show a half-width health bar");
        }
    }

    private static void ModalButtonIsolation()
    {
        var art=new RunnerArt();using(var view=new RunnerWorldView(art))
        foreach(string modal in new[]{"showCharacters","showShop","showHistory","showPractice"})
        foreach(bool dead in new[]{false,true})
        {
            var run=new RunnerSession(new RunnerSave{Wallet=10000},4);
            if(dead){run.StartRun();World(run).Clear();World(run).Add(new WorldEntity{Kind=EntityKind.Barrier,X=run.PlayerX-80,Y=0,Width=160,Height=200});run.Update(1f/120);Check(run.State==RunState.GameOver,"Fatal fixture remained alive");}
            var controller=Ui(run,art,view);Field(controller,modal,true);DrawUi(controller);
            var underlying=GUI.DrawCalls.Where(d=>d.Kind=="Button"&&d.Text!=null&&
                (d.Text.StartsWith("BẮT ĐẦU")||d.Text.StartsWith("CHẠY LẠI")||d.Text=="Cửa hàng"||d.Text=="Thành tích"||d.Text.StartsWith("Về menu"))).ToList();
            Check(underlying.Count>=2&&underlying.All(d=>!d.Enabled),modal+" leaves an underlying action button clickable");
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Button"&&d.Text=="×"&&d.Enabled),modal+" accidentally disables its own close button");
            Check(GUI.enabled,"Modal drawing left GUI disabled for the next frame");
        }
    }

    // V9 rendering checks isolate draw geometry/feedback; the arena fixture
    // suppresses player damage only so long animation sampling does not end it.
    // Full-health one-heart boss solutions are covered by the core suite.
    private static RunnerSession DynamicArenaFixture(BossKind kind)
    {
        // This legacy family choreography audit deliberately uses the selected
        // C1 practice pattern order. New normal C3/submarine fights and tier
        // growth have their own real-input core checks.
        RunnerSession run=new RunnerSession(new RunnerSave(),1);
        run.StartBossPractice(kind,CharacterKind.Classic,false,1);
        Set(run,"InvincibleRemaining",1000f);return run;
    }
    private static RunnerSession FamilyArena(BossKind kind=BossKind.Tank)
    {
        RunnerSession run=DynamicArenaFixture(kind);run.SkipPracticeIntro();run.Update(1f/120);return run;
    }
    private static void AssertBossBody(RunnerWorldView view,RunnerArt art,RunnerSession run,Matrix4x4 canvas)
    {
        Draw(view,run,canvas);BossEncounter boss=run.CurrentBoss;
        RecordedDraw image=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&BossBodyArt(d,art));
        Rect expected=new Rect(boss.X,RunnerSession.GroundY-boss.Y-boss.Height,boss.Width,boss.Height);
        Check(SameRect(image.Position,expected),"Animated/recoiling boss image left its live physical rectangle");
        Matrix4x4 scene=canvas;
        // Ground journey translates the entire arena layer as the old street
        // exits. This camera movement is shared by body, floor and entrance gate.
        if(boss.Kind!=BossKind.Gunship&&boss.Kind!=BossKind.Submarine&&run.ArenaPhase==BossPhase.Intro)
            scene=canvas*Matrix4x4.TRS(new Vector3(1280*(1-run.TransitionSceneBlend),0,0),Quaternion.identity,Vector3.one);
        Point(image.Matrix.MultiplyPoint3x4(new Vector3(expected.x,expected.y,0)),
            scene.MultiplyPoint3x4(new Vector3(expected.x,expected.y,0)),"Boss top-left differs from the shared arena camera");
        Point(image.Matrix.MultiplyPoint3x4(new Vector3(expected.xMax,expected.yMax,0)),
            scene.MultiplyPoint3x4(new Vector3(expected.xMax,expected.yMax,0)),"Boss lower corner acquired decorative rotation/scale");
    }
    private static void DynamicBossBodies()
    {
        var art=new RunnerArt();
        foreach(BossKind kind in new[]{BossKind.Tank,BossKind.Sentinel,BossKind.Gunship})
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=DynamicArenaFixture(kind);float firstX=run.CurrentBoss.X;
            if(kind==BossKind.Gunship)
            {
                Draw(view,run,Canvas(1));Check(!GUI.DrawCalls.Any(d=>BossBodyArt(d,art)&&d.Position.x<1280),"Gunship appeared on screen before takeoff");
            }
            else foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})AssertBossBody(view,art,run,canvas);
            Advance(run,3f);
            Check(Math.Abs(run.CurrentBoss.X-firstX)>30,"Boss body never moved into its themed arena: "+kind);
            foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})AssertBossBody(view,art,run,canvas);
            for(int i=0;i<600&&run.ArenaPhase!=BossPhase.Fighting;i++)run.Update(1f/120);
            Check(run.ArenaPhase==BossPhase.Fighting,"Dynamic boss intro never completed");
            float minX=float.PositiveInfinity,maxX=float.NegativeInfinity,minY=float.PositiveInfinity,maxY=float.NegativeInfinity;
            foreach(float fraction in new[]{1f,.5f,.25f})
            {
                run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*fraction;
                for(int i=0;i<56;i++)
                {
                    run.Update(.125f);BossEncounter boss=run.CurrentBoss;
                    Check(boss.RageLevel==(fraction>.65f?1:fraction>.35f?2:3),"Rendered rage state differs from health phase");
                    minX=Math.Min(minX,boss.X);maxX=Math.Max(maxX,boss.X);minY=Math.Min(minY,boss.Y);maxY=Math.Max(maxY,boss.Y);
                    foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})AssertBossBody(view,art,run,canvas);
                }
            }
            Check(maxX-minX>8,"Boss stayed horizontally static during real combat: "+kind);
            if(kind!=BossKind.Tank)Check(maxY-minY>8,"Boss flight/step/stomp stayed vertically static: "+kind);
        }
    }
    private static void DrawBossBodyOnly(RunnerWorldView view,RunnerSession run,float clock)
    {
        GUI.DrawCalls.Clear();GUI.color=Color.white;GUI.matrix=Canvas(1);
        typeof(RunnerWorldView).GetMethod("DrawBossBody",Private).Invoke(view,
            new object[]{run.CurrentBoss,run.ArenaPhase,clock,run.BossPhaseRemaining});
    }
    private static List<RecordedDraw> MechanicalMarks(BossEncounter boss)
    {
        Rect rect=new Rect(boss.X,RunnerSession.GroundY-boss.Y-boss.Height,boss.Width,boss.Height);
        return GUI.DrawCalls.Where(d=>d.Texture==Texture2D.whiteTexture&&
            (boss.Kind==BossKind.Tank
                ? d.Position.width==8&&d.Position.height<=3&&d.Position.y>rect.yMax-35&&d.Position.y<rect.yMax
                : boss.Kind==BossKind.Sentinel
                    ? d.Position.height==2&&d.Color.b>.75f&&d.Color.g>.7f&&d.Centre.y>rect.yMax-50&&d.Centre.y<rect.yMax
                    : d.Position.width>rect.width*.5f&&d.Position.height<=3&&d.Centre.y>rect.y&&d.Centre.y<rect.y+rect.height*.15f)).ToList();
    }
    private static string MarksSignature(IEnumerable<RecordedDraw> marks)
    {
        return string.Join(";",marks.Select(d=>d.Position.x+":"+d.Position.y+":"+d.Position.width+":"+d.Centre.x+":"+d.Centre.y));
    }
    private static void BossMechanicalAnimation()
    {
        var art=new RunnerArt();
        foreach(BossKind kind in new[]{BossKind.Tank,BossKind.Sentinel,BossKind.Gunship})
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=ArenaFixture(kind);BossEncounter boss=run.CurrentBoss;
            boss.HitFlashRemaining=boss.ShotFlashRemaining=boss.LandingFlashRemaining=0;boss.RageLevel=2;boss.Vulnerable=false;
            DrawBossBodyOnly(view,run,.21f);List<RecordedDraw> first=MechanicalMarks(boss);
            Check(first.Count>=(kind==BossKind.Tank?12:2),"Boss lacks visible tread/servo/rotor mechanics: "+kind);
            string before=MarksSignature(first);DrawBossBodyOnly(view,run,.68f);
            Check(MarksSignature(MechanicalMarks(boss))!=before,"Mechanical animation only moves background or body: "+kind);
            RecordedDraw body=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&BossBodyArt(d,art));
            Check(SameRect(body.Position,new Rect(boss.X,RunnerSession.GroundY-boss.Y-boss.Height,boss.Width,boss.Height)),"Mechanical overlay moved boss image away from body");
        }
    }
    private static void BossHitAndFireFeedback()
    {
        var art=new RunnerArt();
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=DynamicArenaFixture(kind);
            for(int i=0;i<600&&run.ArenaPhase!=BossPhase.Fighting;i++)run.Update(1f/120);
            BossEncounter boss=run.CurrentBoss;float initialHealth=boss.Health;
            run.SetShooting(true);
            for(int i=0;i<2400&&(boss.Health==initialHealth||boss.HitFlashRemaining<=0);i++)run.Update(1f/120);
            run.SetShooting(false);
            Check(boss.Health<initialHealth&&boss.HitFlashRemaining>0,"Real friendly shot did not trigger boss hit feedback: "+kind);
            float hit=boss.HitFlashRemaining;
            DrawBossBodyOnly(view,run,run.BossElapsed);RecordedDraw flashing=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&BossBodyArt(d,art));int impactCount=GUI.DrawCalls.Count;
            boss.HitFlashRemaining=0;DrawBossBodyOnly(view,run,run.BossElapsed);RecordedDraw neutral=GUI.DrawCalls.Single(d=>d.Kind=="UV"&&BossBodyArt(d,art));
            Check(Math.Abs(flashing.Color.g-neutral.Color.g)+Math.Abs(flashing.Color.b-neutral.Color.b)>.05f,"Hit flash does not visibly change the body tint");
            Check(impactCount>=GUI.DrawCalls.Count+8,"Real hit produces no visible impact sparks");boss.HitFlashRemaining=hit;
            for(int i=0;i<2400&&boss.ShotFlashRemaining<=0;i++)run.Update(1f/120);
            Check(boss.ShotFlashRemaining>0,"Real enemy release never triggered muzzle feedback: "+kind);
            float shot=boss.ShotFlashRemaining;DrawBossBodyOnly(view,run,run.BossElapsed);int firingCount=GUI.DrawCalls.Count;
            boss.ShotFlashRemaining=0;DrawBossBodyOnly(view,run,run.BossElapsed);
            Check(firingCount>=GUI.DrawCalls.Count+(kind==BossKind.Submarine?1:5),"Boss fire has no muzzle flash/smoke: "+kind);boss.ShotFlashRemaining=shot;
            if(kind==BossKind.Sentinel)
            {
                for(int i=0;i<3600&&boss.LandingFlashRemaining<=0;i++)run.Update(1f/120);
                Check(boss.LandingFlashRemaining>0,"Actual robot stomp never triggers landing feedback");
                float landing=boss.LandingFlashRemaining;DrawBossBodyOnly(view,run,run.BossElapsed);int landingCount=GUI.DrawCalls.Count;
                boss.LandingFlashRemaining=0;DrawBossBodyOnly(view,run,run.BossElapsed);
                Check(landingCount>=GUI.DrawCalls.Count+9,"Stomping robot has no visible ground dust/impact");boss.LandingFlashRemaining=landing;
            }
            Draw(view,run,Canvas(1));run.TogglePause();GUI.DrawCalls.Clear();view.Draw(run,1000);string paused=DrawSignature();
            float clock=run.BossElapsed,pausedHit=boss.HitFlashRemaining,pausedShot=boss.ShotFlashRemaining;run.Update(5);GUI.DrawCalls.Clear();view.Draw(run,2000);
            Check(DrawSignature()==paused&&run.BossElapsed==clock&&boss.HitFlashRemaining==pausedHit&&boss.ShotFlashRemaining==pausedShot,"Impact/fire feedback advanced while paused");
            run.TogglePause();typeof(RunnerSession).GetMethod("FinishRun",Private).Invoke(run,new object[]{"Fixture stops real impact feedback"});
            Draw(view,run,Canvas(1));string dead=DrawSignature();run.Update(5);GUI.DrawCalls.Clear();view.Draw(run,3000);
            Check(DrawSignature()==dead,"Hit/fire effects continued after fatal state");
        }
    }
    private static void BossQueuedWarnings()
    {
        var art=new RunnerArt();RunnerSession run=ArenaFixture();var signals=(IList<BossTelegraph>)run.BossTelegraphs;signals.Clear();
        var warning=new BossTelegraph{Id=9999,Kind=BossAttackKind.Artillery,X=399,Y=0,Width=91,Height=103,Remaining=.65f,Duration=.9f,DelayRemaining=.4f};
        using(var view=new RunnerWorldView(art))
        {
            Draw(view,run,Canvas(1));string baseline=DrawSignature();signals.Add(warning);Draw(view,run,Canvas(1));
            Check(!warning.Visible&&DrawSignature()==baseline,"Queued future attack appeared before its warning begins");
            warning.DelayRemaining=0;Draw(view,run,Canvas(1));
            Rect physical=new Rect(warning.X,RunnerSession.GroundY-warning.Y-warning.Height,warning.Width,warning.Height);
            RecordedDraw harmless=GUI.DrawCalls.Single(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,physical));
            Check(harmless.Color.a<.3f,"Visible queued warning looks immediately damaging");
            warning.Active=true;Draw(view,run,Canvas(.68f,119,28));
            RecordedDraw live=GUI.DrawCalls.Single(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,physical));
            Check(live.Color.a>=.6f,"Activated queued attack does not show its complete damage rectangle");
            Point(live.Centre,Canvas(.68f,119,28).MultiplyPoint3x4(physical.center),"Queued warning/live body drifts under letterbox");
            warning.Active=false;
            var motifs=new HashSet<string>();
            foreach(BossAttackKind kind in Enum.GetValues(typeof(BossAttackKind)))
            {
                warning.Kind=kind;Draw(view,run,Canvas(1));motifs.Add(DrawSignature());
            }
            Check(motifs.Count>=4,"Different attack families all share one unreadable warning motif");
            Check(Enum.IsDefined(typeof(BossAttackKind),"DroneLow"),"Ground-drone wave was omitted from warning rendering");
            var shots=(IList<BossProjectile>)run.BossProjectiles;shots.Clear();
            var drone=new BossProjectile{Id=10001,Kind=BossAttackKind.DroneLow,X=543,Y=8,Width=50,Height=26};shots.Add(drone);
            Draw(view,run,Canvas(1.5f,320,0));Rect droneBody=new Rect(drone.X,RunnerSession.GroundY-drone.Y-drone.Height,drone.Width,drone.Height);
            Vector3 droneCentre=Canvas(1.5f,320,0).MultiplyPoint3x4(droneBody.center);
            Check(GUI.DrawCalls.Any(d=>d.Texture?.name=="CityDino disc"&&Math.Abs(d.Centre.x-droneCentre.x)<.03&&Math.Abs(d.Centre.y-droneCentre.y)<droneBody.height*.5f),"Ground drone does not show its rounded physical body");
            Check(!GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,droneBody)&&d.Color.a>.6f),"Ground drone regressed to a flying plastic rectangle");
        }
    }
    private static void BossVictoryFeedback()
    {
        var art=new RunnerArt();
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=DynamicArenaFixture(kind);
            for(int i=0;i<600&&run.ArenaPhase!=BossPhase.Fighting;i++)run.Update(1f/120);
            // This isolates the visual victory transition. The core suite fights
            // untouched full-health bosses to validate actual combat solutions.
            run.CurrentBoss.Health=1f;run.SetShooting(true);
            for(int i=0;i<2400&&run.ArenaPhase==BossPhase.Fighting;i++)run.Update(1f/120);
            Check(run.ArenaPhase==BossPhase.Victory,"A real friendly shot failed to start victory feedback");
            BossEncounter boss=run.CurrentBoss;float x=boss.X,y=boss.Y;
            DrawBossBodyOnly(view,run,run.BossElapsed);string first=DrawSignature();
            int effectCount=GUI.DrawCalls.Count(d=>!BossBodyArt(d,art));
            Check(effectCount>=15,"Defeated boss lacks destruction smoke and sparks");
            run.Update(.2f);DrawBossBodyOnly(view,run,run.BossElapsed);
            Check(DrawSignature()!=first,"Victory destruction did not advance with its short combat timer");
            Near(boss.X,x,"Defeated body kept walking during destruction");Near(boss.Y,y,"Defeated body kept flying during destruction");
            run.TogglePause();Draw(view,run,Canvas(1));string paused=DrawSignature();float remaining=run.BossPhaseRemaining;
            run.Update(5);GUI.DrawCalls.Clear();view.Draw(run,3000);
            Check(DrawSignature()==paused&&run.BossPhaseRemaining==remaining,"Paused victory kept animating or returned to running");
        }
    }
    private static void BossRageHud()
    {
        var art=new RunnerArt();RunnerSession run=ArenaFixture();BossEncounter boss=run.CurrentBoss;
        using(var view=new RunnerWorldView(art))
        {
            var controller=Ui(run,art,view);boss.AttackName="Fixture chained attack";boss.Vulnerable=true;boss.Health=boss.MaxHealth*.5f;
            for(int phase=1;phase<=3;phase++)
            {
                boss.RageLevel=phase;DrawUi(controller);
                Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.Contains("PHA "+phase)&&d.Text.Contains(boss.AttackName)),"Combat HUD hides current rage phase or attack name");
                Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text==run.Score.ToString("D5")),"Rage HUD replaces the preserved running score");
                RecordedDraw bar=GUI.DrawCalls.Single(d=>d.Kind=="Box"&&SameRect(d.Position,new Rect(340,148,332,17)));
                Check(phase==3?bar.Color.g<.45f:bar.Color.g>.5f,"Rage health bar does not distinguish the strongest phase");
                Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.Contains("CUỒNG NỘ"))==(phase==3),"Rage HUD overstates or hides frenzy");
            }
        }
    }

    private static void Advance(RunnerSession run,float seconds)
    {
        while(seconds>.0001f){float dt=Math.Min(seconds,1f/120);run.Update(dt);seconds-=dt;}
    }
    private static bool StreetTexture(RecordedDraw draw,RunnerArt art)
    {return draw.Texture==art.Road||draw.Texture==art.SkylineFar||draw.Texture==art.SkylineNear;}
    private static void FlightJourneyRendering()
    {
        var art=new RunnerArt();RunnerSession run=BeginArena(BossKind.Gunship,district:District.Sunset);
        int score=run.Score;float elapsed=run.Elapsed;
        using(var view=new RunnerWorldView(art))
        {
            Draw(view,run,Canvas(.68f,119,28));
            Check(GUI.DrawCalls.Any(d=>StreetTexture(d,art)),"20k entry instantly discarded the approach city");
            Check(!GUI.DrawCalls.Any(d=>BossBodyArt(d,art)&&d.Position.x<1280),"Gunship appeared on screen before the character reached the flight device");
            RecordedDraw pickup=GUI.DrawCalls.Single(d=>d.Texture==art.Hoverboard);
            Point(pickup.Centre,Canvas(.68f,119,28).MultiplyPoint3x4(new Vector3(run.FlightDeviceX,RunnerSession.GroundY-run.FlightDeviceY-12,0)),"Flight pickup does not sit at its physical approach location");
            Advance(run,1.4f);Draw(view,run,Canvas(.68f,119,28));
            Check(run.FlightDeviceCollected,"Flight equipment was not collected during its approach");
            RecordedDraw collected=GUI.DrawCalls.Single(d=>d.Texture==art.Hoverboard);
            Check(collected.Position.width<90&&Math.Abs(collected.Position.center.x-pickup.Position.center.x)<4,"Collected board instantly snapped from pickup to rider");
            Advance(run,.2f);Draw(view,run,Canvas(.68f,119,28));
            RecordedDraw mounting=GUI.DrawCalls.Single(d=>d.Texture==art.Hoverboard);
            Check(mounting.Position.width>90&&mounting.Position.width<145,"Board mounting skipped its intermediate glide/extension");
            Advance(run,.98f);Draw(view,run,Canvas(.68f,119,28));
            Check(run.FlightDeviceCollected&&run.PlayerY>0&&run.TransitionElevation>.1f&&run.TransitionElevation<.9f,"Device takeoff did not progress through intermediate altitude");
            var road=GUI.DrawCalls.First(d=>d.Texture==art.Road);
            Vector3 origin=road.Matrix.MultiplyPoint3x4(new Vector3(0,0,0));
            Check(origin.y>28+40*.68f,"Road and skyline did not fall away during gradual takeoff");
            Check(GUI.DrawCalls.Count(d=>d.Texture==art.Hoverboard)==1,"Collected pickup remained on the street alongside attached device");
            Advance(run,2.3f);Draw(view,run,Canvas(1));
            Check(run.ArenaPhase==BossPhase.Fighting&&!GUI.DrawCalls.Any(d=>StreetTexture(d,art)),"Full flight arena retains the old street");
            Check(run.Score==score&&run.Elapsed==elapsed,"Drawing/entry advanced the suspended running score");
        }
    }
    private static void FlightDeviceGeometry()
    {
        var art=new RunnerArt();Check(art.Hoverboard!=null,"Hoverboard PNG is missing");
        Check(art.HoverboardUV.x>0&&art.HoverboardUV.y>0&&art.HoverboardUV.width<1&&art.HoverboardUV.height<1,"Flight equipment still uses the uncropped image canvas");
        foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        foreach(bool left in new[]{false,true})
        foreach(float velocity in new[]{150f,0f,-150f})
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=ArenaFixture(BossKind.Gunship,kind);Set(run,"PlayerY",130);Set(run,"AnimationVelocityY",velocity);Field(run,"<FacingLeft>k__BackingField",left);
            int frame=RunnerGeometry.GetPlayerFrame(run);Check(frame==(velocity>80?8:velocity< -80?10:9),"Flight uses a running/crouching pose");
            foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
            {
                Draw(view,run,canvas);RecordedDraw pack=GUI.DrawCalls.Single(d=>d.Texture==art.Hoverboard);
                float width=RunnerGeometry.PlayerStandingWidth,height=RunnerGeometry.PlayerStandingHeight;
                Rect body=new Rect(run.PlayerX-width/2,RunnerSession.GroundY-run.PlayerY-height,width,height);
                Rect expected=new Rect(body.center.x-75,body.yMax-2,150,35);
                Check(SameRect(pack.Position,expected),"Flight pack drifted away from physical player: "+kind);
                Near(pack.UV.width,left?-art.HoverboardUV.width:art.HoverboardUV.width,"Flight pack did not mirror with character facing");
                Point(pack.Centre,canvas.MultiplyPoint3x4(expected.center),"Flight equipment acquired a second transform");
                Texture2D texture=art.GetCharacterTexture(kind,frame);RecordedDraw hero=GUI.DrawCalls.Single(d=>d.Texture==texture);
                Check(GUI.DrawCalls.IndexOf(pack)>GUI.DrawCalls.IndexOf(hero),"Hoverboard is hidden behind the character body");
                Check(pack.Position.y>=body.yMax-2&&pack.Position.yMax>body.yMax+30,"Hoverboard no longer sits visibly below the feet");
                Check(GUI.DrawCalls.Take(GUI.DrawCalls.IndexOf(hero)).Any(d=>d.Texture?.name=="CityDino soft glow"&&d.Position.y>=expected.yMax-6&&d.Position.width<=22),"Hoverboard lacks its rear-layer thrust glow");
                Check(!GUI.DrawCalls.Any(d=>d.Texture==art.FlightDevice),"Old hidden backpack is still drawn over the new rider");
                Check(SameRect(hero.Position,art.GetCharacterRect(kind,frame,body,left)),"Flying dinosaur pose left its normal collision layout");
            }
        }
    }
    private static void ThemedArenaRendering()
    {
        var art=new RunnerArt();var themes=new HashSet<string>();
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=ArenaFixture(kind);Draw(view,run,Canvas(1));themes.Add(DrawSignature());
            Check(!GUI.DrawCalls.Any(d=>StreetTexture(d,art)),"Old city appeared in the active themed arena");
            var floor=GUI.DrawCalls.Where(d=>d.Texture==Texture2D.whiteTexture&&d.Position.x==0&&d.Position.width==1280&&d.Position.y==RunnerSession.GroundY&&d.Position.height>7).ToList();
            if(kind==BossKind.Gunship||kind==BossKind.Submarine)
            {
                Check(floor.Count==0,"Air combat still paints a ground/helipad below the flying dinosaur");
                Check(!GUI.DrawCalls.Any(d=>SameRect(d.Position,new Rect(557,RunnerSession.GroundY+31,59,6))),"Old helipad H remains visible");
            }
            else Check(floor.Count>0,"Ground boss lacks a continuous combat floor");
        }
        Check(themes.Count==4,"The four bosses share a single arena theme");
    }
    private static void JourneyPauseRendering()
    {
        var art=new RunnerArt();
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        foreach(bool landing in new[]{false,true})
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=landing?ArenaFixture(kind):BeginArena(kind);
            if(landing){typeof(RunnerSession).GetMethod("BeginBossVictory",Private).Invoke(run,null);Advance(run,2.7f);}
            else Advance(run,2.6f);
            Check(run.ArenaPhase==(landing?BossPhase.Victory:BossPhase.Intro),"Transition fixture finished before pause");
            run.TogglePause();Draw(view,run,Canvas(1));string before=DrawSignature();float y=run.PlayerY;
            run.Update(5);GUI.DrawCalls.Clear();GUI.matrix=Canvas(1);view.Draw(run,9999);
            Check(DrawSignature()==before&&run.PlayerY==y,"Paused scripted "+kind+" journey kept moving its scene/hero");
        }
    }
    private static void BossObstacleRendering()
    {
        var art=new RunnerArt();RunnerSession run=ArenaFixture();var objects=(IList<BossObstacle>)run.BossObstacles;objects.Clear();
        foreach(BossAttackKind kind in new[]{BossAttackKind.MineDrop,BossAttackKind.CrateDrop,BossAttackKind.ElectricPillar,BossAttackKind.ShieldDrone,BossAttackKind.AirMine})
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
        using(var view=new RunnerWorldView(art))
        {
            var obstacle=new BossObstacle{Id=1123,Kind=kind,X=471,Y=83,Width=52,Height=48,Remaining=5,Radius=(kind==BossAttackKind.MineDrop||kind==BossAttackKind.AirMine)?24:0,Health=24,MaxHealth=24};
            objects.Clear();objects.Add(obstacle);Draw(view,run,canvas);
            Rect physical=new Rect(obstacle.X,RunnerSession.GroundY-obstacle.Y-obstacle.Height,obstacle.Width,obstacle.Height);
            if(obstacle.Radius>0)
                Check(GUI.DrawCalls.Any(d=>Math.Abs(d.Centre.x-canvas.MultiplyPoint3x4(physical.center).x)<.03&&Math.Abs(d.Centre.y-canvas.MultiplyPoint3x4(physical.center).y)<.03),"Circular obstacle left its live collision centre: "+kind);
            else Check(GUI.DrawCalls.Any(d=>SameRect(d.Position,physical)),"Obstacle does not draw its full physical body: "+kind);
            if(kind==BossAttackKind.ElectricPillar||kind==BossAttackKind.ShieldDrone)
            {
                GUI.DrawCalls.Clear();GUI.matrix=canvas;
                typeof(RunnerWorldView).GetMethod("DrawBossObstacle",Private).Invoke(view,new object[]{obstacle,.37f});
                Check(!GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,physical)&&d.Color.a>=.6f),"Robot energy scenery is still an opaque plastic box");
                Check(GUI.DrawCalls.Count(d=>d.Texture==Texture2D.whiteTexture&&d.Color.b>d.Color.r&&d.Color.a>.6f)>=10,"Robot energy scenery lacks luminous electric filaments");
                if(kind==BossAttackKind.ShieldDrone)Check(GUI.DrawCalls.Count(d=>d.Texture?.name=="CityDino disc")>=3,"Shield drone lacks its round turbine and plasma core");
            }
            objects.Clear();var warnings=(IList<BossTelegraph>)run.BossTelegraphs;warnings.Clear();
            var warning=new BossTelegraph{Id=obstacle.Id,Kind=kind,X=obstacle.X,Y=obstacle.Y,Width=obstacle.Width,Height=obstacle.Height,Duration=1,Remaining=.8f};warnings.Add(warning);
            Draw(view,run,canvas);RecordedDraw preview=GUI.DrawCalls.FirstOrDefault(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,physical));
            Check(preview!=null&&preview.Color.a<.3f,"Obstacle's visible harmless warning omits its eventual footprint: "+kind);
            Point(preview.Centre,canvas.MultiplyPoint3x4(physical.center),"Obstacle warning shifted away from live geometry");
            warnings.Clear();objects.Add(obstacle);obstacle.Removed=true;Draw(view,run,canvas);string removed=DrawSignature();objects.Clear();Draw(view,run,canvas);
            Check(DrawSignature()==removed,"Removed boss obstacle remains visible: "+kind);
        }
    }
    private static void AirGateRendering()
    {
        var art=new RunnerArt();RunnerSession run=ArenaFixture(BossKind.Gunship);var warnings=(IList<BossTelegraph>)run.BossTelegraphs;warnings.Clear();
        var warning=new BossTelegraph{Id=1823,Kind=BossAttackKind.AirGate,X=604,Y=0,Width=44,Height=360,GapY=87,GapHeight=190,Duration=1,Remaining=.8f};warnings.Add(warning);
        using(var view=new RunnerWorldView(art))
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
        {
            Draw(view,run,canvas);
            Rect gap=new Rect(warning.X,RunnerSession.GroundY-warning.GapY-warning.GapHeight,warning.Width,warning.GapHeight);
            Rect lower=new Rect(warning.X,RunnerSession.GroundY-warning.GapY,warning.Width,warning.GapY);
            Rect upper=new Rect(warning.X,RunnerSession.GroundY-360,warning.Width,360-warning.GapY-warning.GapHeight);
            foreach(Rect bar in new[]{lower,upper})
            {
                RecordedDraw fill=GUI.DrawCalls.FirstOrDefault(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,bar));
                Check(fill!=null&&fill.Color.a<.3f,"Gate preview omitted a harmless physical bar or hid safe opening");
                Point(fill.Centre,canvas.MultiplyPoint3x4(bar.center),"Gate bar warning left its physical position");
            }
            Check(!GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,new Rect(warning.X,RunnerSession.GroundY-360,warning.Width,360))),"Gate warning paints the safe gap as a collision wall");
        }
    }
    private static void PracticeMenuRendering()
    {
        var art=new RunnerArt();var run=new RunnerSession(new RunnerSave(),4);
        using(var view=new RunnerWorldView(art))
        {
            var controller=Ui(run,art,view);typeof(CityRunnerController).GetMethod("OpenPractice",Private).Invoke(controller,null);DrawUi(controller);
            var previews=GUI.DrawCalls.Where(d=>BossBodyArt(d,art)).ToList();
            Check(previews.Count==4&&previews.Select(d=>d.Texture.name+":"+d.UV.x+":"+d.UV.y).Distinct().Count()==4,"Preparation does not preview all four distinct bosses");
            foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
                Check(GUI.DrawCalls.Any(d=>d.Texture==art.GetCharacterTexture(kind,0)),"Practice omits a trial character preview: "+kind);
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Button"&&d.Enabled&&d.Text!=null&&d.Text.Contains("LÀM QUEN")),"Learning selector is missing");
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Button"&&d.Enabled&&d.Text!=null&&d.Text.Contains("CHUẨN")),"Standard selector is missing");
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.Contains("BẢN 13")),"Version footer is stale");
            Check(GUI.DrawCalls.Where(d=>d.Kind=="Button"&&d.Text!=null&&d.Text.StartsWith("BẮT ĐẦU")).All(d=>!d.Enabled),"Practice preparation leaves normal start active");
            Check(!run.Save.OwnsTriceratops&&!run.Save.OwnsRaptor&&run.State==RunState.Menu,"Previewing practice changed character ownership or began a run");
        }
    }
    private static void PracticeResultsRendering()
    {
        var art=new RunnerArt();
        foreach(bool won in new[]{false,true})
        using(var view=new RunnerWorldView(art))
        {
            var run=new RunnerSession(new RunnerSave{Wallet=4321,BestScore=7654,LastScore=1234},9);
            run.StartBossPractice(BossKind.Gunship,CharacterKind.Raptor);run.SkipPracticeIntro();
            typeof(RunnerSession).GetMethod("FinishPractice",Private).Invoke(run,new object[]{won,"Rendering fixture"});
            var controller=Ui(run,art,view);DrawUi(controller);
            foreach(string label in new[]{"TẬP LẠI","Đổi boss / nhân vật","Về menu"})
                Check(GUI.DrawCalls.Any(d=>d.Kind=="Button"&&d.Enabled&&d.Text.StartsWith(label)),"Practice result lacks action: "+label);
            Check(!GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&(d.Text.Contains("SAO NHẬN")||d.Text=="7654"||d.Text=="1234")),"Practice result shows normal run record/reward");
            Check(run.Save.Wallet==4321&&run.Save.BestScore==7654&&run.Save.LastScore==1234,"Drawing practice result changed normal progress");
        }
    }

    private static void BossActionPoseRendering()
    {
        var art=new RunnerArt();
        Check(art.BossActionAtlas!=null,"Action pose atlas is missing");
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        using(var view=new RunnerWorldView(art))
        {
            Texture2D texture=kind==BossKind.Submarine?art.SubmarineAtlas:art.BossActionAtlas;
            Check(texture!=null,"Boss action art missing: "+kind);
            PngAlpha png=ReadPngAlpha(Path.Combine(assetDirectory,texture.name+".png"));
            RunnerSession run=ArenaFixture(kind);BossEncounter boss=run.CurrentBoss;
            var poses=new HashSet<string>();var sourceMasks=new Dictionary<BossAction,bool[]>();
            foreach(BossAction action in Enum.GetValues(typeof(BossAction)))
            {
                boss.Action=action;boss.ActionProgress=.6f;
                Rect expected=ExpectedBossBodyUV(art,boss);
                Check(expected.width>0&&expected.height>0&&expected.x>=0&&expected.y>=0&&expected.xMax<=1.00001&&expected.yMax<=1.00001,"Action source crop leaves the PNG");
                poses.Add(expected.x+":"+expected.y+":"+expected.width+":"+expected.height);
                foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
                {
                    Draw(view,run,canvas);RecordedDraw image=GUI.DrawCalls.Single(d=>BossBodyArt(d,art));
                    Check(image.Texture==texture&&SameRect(image.UV,expected),"Attack state did not select its own drawn pose: "+kind+" "+action);
                    Rect body=new Rect(boss.X,RunnerSession.GroundY-boss.Y-boss.Height,boss.Width,boss.Height);
                    Check(SameRect(image.Position,body),"Action art changed the actual body rectangle");
                    Point(image.Centre,canvas.MultiplyPoint3x4(body.center),"Action image acquired a separate animation transform");
                }
                bool[] mask=new bool[64*48];Rect sourcePlacement=new Rect(0,0,64,48);
                for(int y=0;y<48;y++)for(int x=0;x<64;x++)mask[y*64+x]=RenderedAlpha(png,sourcePlacement,expected,false,x+.5f,y+.5f)>=128;
                Check(mask.Count(x=>x)>80,"Action crop contains too little opaque body art");sourceMasks[action]=mask;
            }
            Check(poses.Count==4,"Boss actions reuse one static frame: "+kind);
            int changed=sourceMasks[BossAction.Idle].Zip(sourceMasks[BossAction.Strike],(a,b)=>a!=b?1:0).Sum();
            Check(changed>=8,"Source PNG has no silhouette change between idle and strike: "+kind);
        }
        UnityEngine.Object original=Resources.TestAssets["CityDino/BossActionAtlas"];
        try
        {
            Resources.TestAssets.Remove("CityDino/BossActionAtlas");var fallback=new RunnerArt();
            using(var view=new RunnerWorldView(fallback))
            {
                RunnerSession run=ArenaFixture(BossKind.Sentinel);run.CurrentBoss.Action=BossAction.Strike;Draw(view,run,Canvas(1));
                Check(GUI.DrawCalls.Single(d=>BossBodyArt(d,fallback)).Texture==fallback.BossAtlas,"Missing action atlas discarded the original fallback boss");
            }
        }
        finally{Resources.TestAssets["CityDino/BossActionAtlas"]=original;}
    }
    private static void BossChargeRendering()
    {
        var art=new RunnerArt();
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=FamilyArena(kind);
            // Preserve attacks and motion while protecting this draw-contract
            // fixture; this is not a claim that the stationary player survives.
            bool warning=false,strike=false,recovery=false;float minX=run.CurrentBoss.X,maxX=minX;
            for(int i=0;i<120*65&&!recovery;i++)
            {
                BossEncounter boss=run.CurrentBoss;
                foreach(BossTelegraph mark in run.BossTelegraphs)
                    if(mark.Visible&&(mark.Kind==BossAttackKind.TankRam||mark.Kind==BossAttackKind.RobotDash||mark.Kind==BossAttackKind.GunshipDive||mark.Kind==BossAttackKind.SubmarineRam))
                    {
                        Draw(view,run,Canvas(.68f,119,28));Rect danger=new Rect(mark.X,RunnerSession.GroundY-mark.Y-mark.Height,mark.Width,mark.Height);
                        Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,danger)&&d.Color.a<.3f),"Lunge lacks its harmless lane warning");warning=true;
                    }
                if(boss.ContactActive)
                {
                    Check(warning&&boss.Action==BossAction.Strike,"Lunge image struck before its warning/state");strike=true;
                    minX=Math.Min(minX,boss.X);maxX=Math.Max(maxX,boss.X);
                    foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
                    {
                        Draw(view,run,canvas);Rect contact=new Rect(boss.ContactX,RunnerSession.GroundY-boss.ContactY-boss.ContactHeight,boss.ContactWidth,boss.ContactHeight);
                        RecordedDraw live=GUI.DrawCalls.Single(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,contact));
                        Check(live.Color.a>0&&live.Color.a<.3f,"Live strike shape is not clearly distinguished from its weapon-edge highlight");
                        Point(live.Centre,canvas.MultiplyPoint3x4(contact.center),"Charge contact visual drifted under letterbox");
                        RecordedDraw body=GUI.DrawCalls.Single(d=>BossBodyArt(d,art));
                        Check(SameRect(body.UV,ExpectedBossBodyUV(art,boss)),"Actual lunge retained the idle artwork");
                        Near(body.Position.x,boss.X,"Actual lunge body is still decorative at its old position");
                    }
                }
                if(strike&&boss.Action==BossAction.Recovery)
                {
                    recovery=true;Draw(view,run,Canvas(1));string before=DrawSignature();run.TogglePause();run.Update(5);GUI.DrawCalls.Clear();view.Draw(run,999);
                    Check(DrawSignature()==before,"Paused strike recovery kept animating");run.TogglePause();
                }
                run.Update(1f/120);
            }
            Check(warning&&strike&&recovery&&maxX-minX>300,"Boss only wiggled instead of completing a real lunge: "+kind);
        }
    }
    private static void DrawProjectileOnly(RunnerWorldView view,BossProjectile projectile,float clock,Matrix4x4 canvas)
    {
        GUI.DrawCalls.Clear();GUI.matrix=canvas;GUI.color=Color.white;
        typeof(RunnerWorldView).GetMethod("DrawBossProjectile",Private).Invoke(view,new object[]{projectile,clock});
    }
    private static void ElectricOrbRendering()
    {
        var art=new RunnerArt();using(var view=new RunnerWorldView(art))
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
        {
            var orb=new BossProjectile{Id=56,Kind=BossAttackKind.ElectricOrb,X=432,Y=83,Width=36,Height=36,Radius=18,VelocityX=-300};
            DrawProjectileOnly(view,orb,.37f,canvas);Rect physical=new Rect(orb.X,RunnerSession.GroundY-orb.Y-orb.Height,orb.Width,orb.Height);
            RecordedDraw disc=GUI.DrawCalls.Single(d=>d.Texture?.name=="CityDino disc"&&SameRect(d.Position,physical));
            Check(disc.Color.a>=.6f&&disc.Color.b>disc.Color.r,"Electricity is missing a solid cool-coloured energy sphere");
            Point(disc.Centre,canvas.MultiplyPoint3x4(physical.center),"Electric sphere left its actual circular contact centre");
            Check(!GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,physical)),"Electric sphere regressed to a plastic rectangle");
            Check(GUI.DrawCalls.Any(d=>d.Texture?.name=="CityDino soft glow"&&d.Position.width>physical.width&&d.Color.a<.3f),"Electricity lacks a softer harmless outer glow");
            Check(GUI.DrawCalls.Count(d=>d.Texture==Texture2D.whiteTexture&&d.Color.a>.7f)>=10,"Electricity has no animated branching arcs");
            string first=DrawSignature();DrawProjectileOnly(view,orb,.47f,canvas);Check(DrawSignature()!=first,"Electrical arcs are a static sticker");
        }
    }
    private static Vector3 LineEnd(RecordedDraw draw)
    {return draw.Matrix.MultiplyPoint3x4(new Vector3(draw.Position.xMax,draw.Position.center.y,0));}
    private static bool SamePoint(Vector3 a,Vector3 b)
    {return Math.Abs(a.x-b.x)<.025f&&Math.Abs(a.y-b.y)<.025f;}
    private static void DirectionalMissileRendering()
    {
        var art=new RunnerArt();using(var view=new RunnerWorldView(art))
        foreach(Vector2 velocity in new[]{new Vector2(320,0),new Vector2(-320,0),new Vector2(0,-335),new Vector2(0,335)})
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
        {
            bool horizontal=velocity.x!=0;var missile=new BossProjectile{Id=57,Kind=horizontal?BossAttackKind.CrossMissile:BossAttackKind.MissileRain,X=440,Y=120,Width=horizontal?40:26,Height=horizontal?24:50,VelocityX=velocity.x,VelocityY=velocity.y};
            DrawProjectileOnly(view,missile,.4f,canvas);Rect physical=new Rect(missile.X,RunnerSession.GroundY-missile.Y-missile.Height,missile.Width,missile.Height);
            Vector2 direction=new Vector2(velocity.x/velocity.magnitude,-velocity.y/velocity.magnitude);float length=horizontal?physical.width:physical.height;
            Vector3 expectedNose=canvas.MultiplyPoint3x4(physical.center+direction*(length*.46f));
            RecordedDraw nose=GUI.DrawCalls.Single(d=>d.Texture==Texture2D.whiteTexture&&d.Color.r>.85f&&d.Color.g<.6f&&d.Color.b<.5f&&d.Color.a>.9f);
            Point(LineEnd(nose),expectedNose,"Missile nose points against its physical velocity");
            RecordedDraw exhaust=GUI.DrawCalls.First();Vector3 tail=canvas.MultiplyPoint3x4(physical.center-direction*(length*.42f));
            Point(LineEnd(exhaust),tail,"Missile exhaust is not behind its metal body");
            if(horizontal)
            {
                var mark=new BossTelegraph{Id=58,Kind=BossAttackKind.CrossMissile,X=90,Y=18,Width=1000,Height=24,Duration=1,Remaining=.6f,OriginX=velocity.x>0?-30:1320,OriginY=18,VelocityX=velocity.x};
                GUI.DrawCalls.Clear();GUI.matrix=canvas;typeof(RunnerWorldView).GetMethod("DrawBossTelegraph",Private).Invoke(view,new object[]{mark,.3f,new BossEncounter()});
                float edgeX=velocity.x>0?25:1255,edgeY=RunnerSession.GroundY-30;
                Vector3 tip=canvas.MultiplyPoint3x4(new Vector2(edgeX+(velocity.x>0?7:-7),edgeY));
                Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SamePoint(LineEnd(d),tip)),"Missile warning has no correctly directed edge arrival indicator");
            }
        }
    }
    private static void MissileRainRendering()
    {
        var art=new RunnerArt();using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=FamilyArena();run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*.2f;
            for(int i=0;i<120*65&&!run.BossTelegraphs.Any(t=>t.Kind==BossAttackKind.MissileRain&&t.Visible);i++)run.Update(1f/120);
            var rain=run.BossTelegraphs.Where(t=>t.Kind==BossAttackKind.MissileRain&&t.Visible).ToList();Check(rain.Count>=3,"Real missile-rain pattern did not expose multiple warning strips");
            foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
            {
                Draw(view,run,canvas);
                foreach(BossTelegraph mark in rain)
                {
                    Check(mark.GapWidth>=200,"Rain warning leaves too little room for its escape corridor");
                    Check(mark.X+mark.Width<=mark.GapX||mark.X>=mark.GapX+mark.GapWidth,"Rain warning paints across its own escape corridor");
                    Rect danger=new Rect(mark.X,RunnerSession.GroundY-mark.Y-mark.Height,mark.Width,mark.Height);
                    Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,danger)&&d.Color.a<.3f),"Rain strip was not painted at its physical path");
                    foreach(float x in new[]{mark.GapX,mark.GapX+mark.GapWidth})
                    {
                        Vector3 bottom=canvas.MultiplyPoint3x4(new Vector2(x,RunnerSession.GroundY));
                        Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&d.Color.g>d.Color.r&&SamePoint(LineEnd(d),bottom)),"Rain escape edge lacks its green safe-corridor guide");
                    }
                }
            }
            for(int i=0;i<120*8&&!run.BossTelegraphs.Any(t=>t.Kind==BossAttackKind.MissileRain&&t.Active);i++)run.Update(1f/120);
            var impacts=run.BossTelegraphs.Where(t=>t.Kind==BossAttackKind.MissileRain&&t.Active).ToList();
            Check(impacts.Count>0,"Actual raining missiles never produced their ground-impact blast");
            foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
            {
                Draw(view,run,canvas);
                foreach(BossTelegraph blast in impacts)
                {
                    Rect live=new Rect(blast.X,RunnerSession.GroundY-blast.Y-blast.Height,blast.Width,blast.Height);
                    RecordedDraw paint=GUI.DrawCalls.Single(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,live));
                    Check(paint.Color.a>=.6f&&paint.Color.r>paint.Color.b,"A damaging rain impact still looks like a harmless warning");
                    Point(paint.Centre,canvas.MultiplyPoint3x4(live.center),"Rain impact differs from its active damage rectangle");
                }
            }
        }
    }

    private static void OrdnanceRenderingV12()
    {
        var art=new RunnerArt();Check(art.OrdnanceAtlas!=null,"New mechanical ordnance PNG is missing");
        PngAlpha png=ReadPngAlpha(Path.Combine(assetDirectory,"OrdnanceAtlas.png"));var crops=new HashSet<string>();
        for(int cell=0;cell<4;cell++)
        {
            Rect uv=art.GetOrdnanceUV(cell);Check(uv.x>=0&&uv.y>=0&&uv.width>0&&uv.height>0&&uv.xMax<=1&&uv.yMax<=1,"Ordnance crop leaves actual PNG");
            crops.Add(uv.x+":"+uv.y+":"+uv.width+":"+uv.height);
            int opaque=0,clear=0;
            for(int y=0;y<24;y++)for(int x=0;x<32;x++)
                if(RenderedAlpha(png,new Rect(0,0,32,24),uv,false,x+.5f,y+.5f)>=128)opaque++;else clear++;
            Check(opaque>70&&clear>15,"New source art is empty or has an opaque box backing: cell "+cell);
        }
        Check(crops.Count==4,"Four ordnance families reuse one sticker");
        using(var view=new RunnerWorldView(art))
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
        {
            foreach(BossAttackKind kind in new[]{BossAttackKind.Cannon,BossAttackKind.Artillery,BossAttackKind.AirSpread})
            {
                float radius=kind==BossAttackKind.Cannon?11:kind==BossAttackKind.Artillery?14:6;
                var shot=new BossProjectile{Id=128,Kind=kind,X=461,Y=82,Width=kind==BossAttackKind.Artillery?32:30,Height=kind==BossAttackKind.Artillery?32:22,Radius=radius,VelocityX=kind==BossAttackKind.Artillery?0:-325,VelocityY=kind==BossAttackKind.Artillery?-485:0};
                DrawProjectileOnly(view,shot,.27f,canvas);Rect body=new Rect(shot.X,RunnerSession.GroundY-shot.Y-shot.Height,shot.Width,shot.Height);
                Rect core=new Rect(body.center.x-radius,body.center.y-radius,radius*2,radius*2);
                RecordedDraw solid=GUI.DrawCalls.Single(d=>d.Texture?.name=="CityDino disc"&&SameRect(d.Position,core));
                Point(solid.Centre,canvas.MultiplyPoint3x4(body.center),"Round ordnance damage core drifted from projectile");
                Check(!GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,body)&&d.Color.a>.6f),"Ordnance is still an opaque flying box");
                if(kind!=BossAttackKind.AirSpread)
                {
                    RecordedDraw sprite=GUI.DrawCalls.Single(d=>d.Texture==art.OrdnanceAtlas);
                    Check(SameRect(sprite.UV,art.GetOrdnanceUV(0)),"Shell selects another mechanical source cell");
                    Point(sprite.Centre,canvas.MultiplyPoint3x4(body.center),"Velocity rotation moved shell art off its round contact core");
                    Check(sprite.Position.width/sprite.Position.height>2.4f,"Long shell source was squashed into a plastic block");
                }
                else Check(GUI.DrawCalls.Any(d=>d.Texture?.name=="CityDino soft glow"&&d.Color.a<.3f),"Air spread lacks plasma light and tail");
            }
            foreach(int cell in new[]{1,2})
            {
                var obstacle=new BossObstacle{Id=129,Kind=cell==1?BossAttackKind.CrateDrop:BossAttackKind.MineDrop,X=487,Y=40,Width=56,Height=56,Radius=28,Remaining=4,Health=32,MaxHealth=32};
                GUI.DrawCalls.Clear();GUI.matrix=canvas;typeof(RunnerWorldView).GetMethod("DrawBossObstacle",Private).Invoke(view,new object[]{obstacle,.43f});
                RecordedDraw sprite=GUI.DrawCalls.Single(d=>d.Texture==art.OrdnanceAtlas);
                Check(SameRect(sprite.UV,art.GetOrdnanceUV(cell)),"Mechanical obstacle selects wrong round source");
                Point(sprite.Centre,canvas.MultiplyPoint3x4(new Vector3(515,RunnerSession.GroundY-68,0)),"Mechanical obstacle drifted from live round core");
                Check(!GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,new Rect(487,RunnerSession.GroundY-96,56,56))&&d.Color.a>.6f),"Drone/mine regressed to an opaque cube");
            }
            var bar=new BossObstacle{Id=130,Kind=BossAttackKind.AirGate,X=620,Y=0,Width=44,Height=87,Remaining=4};
            GUI.DrawCalls.Clear();GUI.matrix=canvas;typeof(RunnerWorldView).GetMethod("DrawBossObstacle",Private).Invoke(view,new object[]{bar,.43f});
            var nodes=GUI.DrawCalls.Where(d=>d.Texture==art.OrdnanceAtlas).ToList();Check(nodes.Count==2,"Plasma gate lacks two actual emitter sprites");
            foreach(RecordedDraw node in nodes)Check(SameRect(node.UV,art.GetOrdnanceUV(3))&&node.Position.y>=RunnerSession.GroundY-87&&node.Position.yMax<=RunnerSession.GroundY+.01f,"Gate node leaves its damaging bar or uses another asset");
            Check(!GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,new Rect(620,RunnerSession.GroundY-87,44,87))&&d.Color.a>.6f),"Plasma gate remains an opaque plastic slab");
        }
    }
    private static void SweepWarningsV12()
    {
        var art=new RunnerArt();using(var view=new RunnerWorldView(art))
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
        {
            string partial="";
            foreach(bool full in new[]{false,true})
            {
                var cue=new BossTelegraph{Id=8000,Kind=BossAttackKind.TankRam,X=full?-310:90,Y=0,Width=full?1400:1000,Height=68,Duration=1.25f,Remaining=.5f,FullSweep=full,SweepEndX=full?-310:350,AimX=full?-310:350};
                GUI.DrawCalls.Clear();GUI.matrix=canvas;typeof(RunnerWorldView).GetMethod("DrawBossTelegraph",Private).Invoke(view,new object[]{cue,.45f,new BossEncounter()});
                Rect footprint=new Rect(cue.X,RunnerSession.GroundY-cue.Y-cue.Height,cue.Width,cue.Height);
                Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,footprint)&&d.Color.a<.3f),"Sweep cue no longer paints its exact harmless locked lane");
                float y=RunnerSession.GroundY-34;
                if(full)
                {
                    Vector3 tip=canvas.MultiplyPoint3x4(new Vector2(14,y));
                    Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SamePoint(LineEnd(d),tip)),"Full sweep does not show continuation through the left screen edge");
                    Check(DrawSignature()!=partial,"Random full/partial variants are visually indistinguishable before striking");
                }
                else
                {
                    Vector3 stop=canvas.MultiplyPoint3x4(new Vector2(358,y));
                    Check(GUI.DrawCalls.Any(d=>d.Texture?.name=="CityDino disc"&&SamePoint(d.Centre,stop)),"Partial sweep omits the locked stopping marker");partial=DrawSignature();
                }
            }
        }
    }
    private static void TierHudV12()
    {
        var art=new RunnerArt();var save=new RunnerSave{Wallet=1200,PurchasedHearts=2,SelectedCharacter=CharacterKind.Classic};var run=new RunnerSession(save,32);
        using(var view=new RunnerWorldView(art))
        {
            var controller=Ui(run,art,view);typeof(CityRunnerController).GetMethod("OpenPractice",Private).Invoke(controller,null);DrawUi(controller);
            foreach(int tier in new[]{1,2,3,5,8})Check(GUI.DrawCalls.Any(d=>d.Kind=="Button"&&d.Enabled&&d.Text!=null&&d.Text.EndsWith("CẤP "+tier)),"Practice preparation omits danger tier "+tier);
            run.StartBossPractice(BossKind.Sentinel,CharacterKind.Raptor,false,5);Field(controller,"showPractice",false);run.SkipPracticeIntro();run.CurrentBoss.Health=run.CurrentBoss.MaxHealth*.2f;run.Update(.01f);DrawUi(controller);
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.Contains("CẤP 5")&&d.Text.Contains("PHA 3")),"Danger tier is missing or confused with the boss health phase");
            Check(save.Wallet==1200&&save.PurchasedHearts==2&&!save.OwnsRaptor,"Rendering danger tier consumed inventory or opened a trial avatar");
        }
    }
    private static void ForwardExitRenderingV12()
    {
        var art=new RunnerArt();Check(art.ArenaGateAtlas!=null&&art.ArenaBackdropAtlas!=null,"Themed gate/backdrop source art missing");
        Check(!SameRect(art.GetArenaBackdropUV(BossKind.Tank),art.GetArenaBackdropUV(BossKind.Sentinel)),"Tank and robot use the same generated arena background");
        PngAlpha gatePng=ReadPngAlpha(Path.Combine(assetDirectory,"ArenaGateAtlas.png"));
        foreach(BossKind gateKind in new[]{BossKind.Tank,BossKind.Sentinel})
        {
            int transparentPassage=0,total=0;Rect uv=art.GetArenaGateUV(gateKind);
            for(int y=115;y<286;y+=10)for(int x=120;x<151;x+=10)
            {total++;if(RenderedAlpha(gatePng,new Rect(0,0,270,330),uv,false,x+.5f,y+.5f)<128)transparentPassage++;}
            Check(transparentPassage>total*.6f,"Generated gate has no real transparent passage behind its opening shutter");
        }
        foreach(BossKind kind in new[]{BossKind.Tank,BossKind.Sentinel,BossKind.Gunship})
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=ArenaFixture(kind);BossEncounter boss=run.CurrentBoss;
            Check(RunnerSession.BossExitGateX>boss.X+boss.Width,"Exit gate lies in front of the undefeated boss instead of behind it");
            typeof(RunnerSession).GetMethod("BeginBossVictory",Private).Invoke(run,null);
            int score=run.Score;float elapsed=run.Elapsed;Advance(run,RunnerSession.BossExplosionDuration+.12f);
            float previousGate=float.PositiveInfinity,previousRoad=float.PositiveInfinity,world=run.DepartureWorldX;bool crossed=false,gateShown=false,streetShown=false;
            for(int sample=0;sample<13&&run.ArenaPhase==BossPhase.Victory;sample++)
            {
                Draw(view,run,canvas);
                Check(run.DepartureWorldX>=world-.001f&&run.BossExitCameraX>=0&&!run.FacingLeft,"Victory backs toward the entrance instead of continuing forward");world=run.DepartureWorldX;
                float gateCentre=RunnerSession.BossExitGateX-run.BossExitCameraX;
                var gates=GUI.DrawCalls.Where(d=>d.Texture==art.ArenaGateAtlas&&Math.Abs(d.Position.width-270)<.03&&Math.Abs(d.Position.height-330)<.03).ToList();
                RecordedDraw gate=gates.FirstOrDefault(d=>SamePoint(d.Centre,canvas.MultiplyPoint3x4(new Vector3(gateCentre,RunnerSession.GroundY-165+run.TransitionElevation*680,0))));
                Check(gate!=null,"Forward exit gate is not drawn at its world position behind the defeated boss");gateShown=true;
                Near(gate.Centre.x,canvas.MultiplyPoint3x4(new Vector3(gateCentre,0,0)).x,"Exit gate has a different camera than the journey");
                Check(gate.Centre.x<=previousGate+.03f,"Exit gate moves right like the entrance being replayed");previousGate=gate.Centre.x;
                Check(SameRect(gate.UV,art.GetArenaGateUV(kind)),"Exit gate does not match this boss theme");
                var roads=GUI.DrawCalls.Where(d=>d.Texture==art.Road).ToList();
                Check(roads.Count>0,"Next running street never approaches from beyond the exit");
                float roadX=roads[0].Matrix.MultiplyPoint3x4(new Vector3(0,0,0)).x;
                float expectedX=canvas.MultiplyPoint3x4(new Vector3(RunnerSession.BossExitStreetX-run.BossExitCameraX,0,0)).x;
                Near(roadX,expectedX,"Next street is entering from the old left entrance");
                Check(roadX<=previousRoad+.03f,"Next street scrolls backward during departure");previousRoad=roadX;streetShown=true;
                if(kind==BossKind.Gunship&&run.TransitionElevation>.1f)
                {
                    float floorY=roads[0].Matrix.MultiplyPoint3x4(new Vector3(0,0,0)).y;
                    Check(floorY>canvas.MultiplyPoint3x4(new Vector3(0,RunnerSession.GroundY,0)).y+30,"A new street floor popped up inside the still airborne arena");
                }
                if(run.DepartureWorldX>=RunnerSession.BossExitGateX)
                {crossed=true;Check(run.PlayerY<.1f&&!run.FlightDeviceCollected,"Hero passed the exit gate before landing on its visible road");}
                Check(run.Score==score&&run.Elapsed==elapsed,"Cinematic exit advanced the suspended run score or clock");
                if(sample==5)
                {
                    string frozen=DrawSignature();Vector3 frozenGate=gate.Centre;
                    Vector3 frozenRoad=roads[0].Matrix.MultiplyPoint3x4(new Vector3(0,0,0));
                    run.TogglePause();run.Update(3);GUI.DrawCalls.Clear();GUI.matrix=canvas;view.Draw(run,9999);
                    Check(DrawSignature()==frozen,"Paused forward exit kept scrolling the gate or street");
                    RecordedDraw pausedGate=GUI.DrawCalls.Single(d=>d.Texture==art.ArenaGateAtlas&&Math.Abs(d.Position.width-270)<.03&&Math.Abs(d.Position.height-330)<.03);
                    Point(pausedGate.Centre,frozenGate,"Paused exit gate changed its camera transform with ambient time");
                    RecordedDraw pausedRoad=GUI.DrawCalls.First(d=>d.Texture==art.Road);
                    Point(pausedRoad.Matrix.MultiplyPoint3x4(new Vector3(0,0,0)),frozenRoad,"Paused destination street changed its camera transform");run.TogglePause();
                }
                Advance(run,.23f);
            }
            Check(gateShown&&streetShown&&crossed,"Journey failed to pass the far-side exit before returning to the run");
        }
    }

    private static void UnderwaterJourneyV13()
    {
        var art=new RunnerArt();Check(art.UnderwaterBackdrop!=null&&art.SubmarineAtlas!=null&&art.DivingKitAtlas!=null,"Underwater generated art missing");
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=BeginArena(BossKind.Submarine);bool equippedBeforeWater=false,jumped=false,deep=false;int score=run.Score;float elapsed=run.Elapsed;
            for(int i=0;i<120*5&&run.ArenaPhase==BossPhase.Intro;i++)
            {
                if(run.DivingHelmetEquipped&&run.WaterDepth<.01f)equippedBeforeWater=true;
                if(run.DivingHelmetEquipped&&run.WaterDepth<.2f&&run.PlayerY>20)jumped=true;
                if(i%12==0)
                {
                    Draw(view,run,Canvas(1));
                    if(run.DivingHelmetEquipped)Check(GUI.DrawCalls.Any(d=>d.Texture==art.DivingKitAtlas&&SameRect(d.UV,art.GetDivingKitUV(0))),"Equipped helmet missing from actual dive scene");
                    if(run.WaterDepth>.9f)deep=true;
                }
                Check(run.Score==score&&run.Elapsed==elapsed,"Dive scene advanced scored running time");run.Update(1f/120);
            }
            Check(equippedBeforeWater&&jumped&&deep&&run.SwimControlsEnabled,"Dive omitted equipment jump or actual submersion");
            Draw(view,run,Canvas(1));Check(GUI.DrawCalls.Any(d=>d.Texture==art.UnderwaterBackdrop),"Submerged encounter does not draw its ocean backdrop");
            Check(!GUI.DrawCalls.Any(d=>StreetTexture(d,art)||d.Texture==art.Hoverboard),"Submerged arena still shows street or flight board");
            string signature=DrawSignature();run.TogglePause();run.Update(3);GUI.DrawCalls.Clear();view.Draw(run,9999);
            Check(DrawSignature()==signature,"Paused underwater scene animates from wall time");
        }
    }
    private static void DivingHelmetV13()
    {
        var art=new RunnerArt();var helmetPng=ReadPngAlpha(Path.Combine(assetDirectory,"DivingKitAtlas.png"));
        var layout=typeof(RunnerWorldView).GetMethod("DivingHelmetRect",BindingFlags.Static|BindingFlags.NonPublic);
        Check(layout!=null,"Shared helmet placement missing");
        foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        foreach(bool left in new[]{false,true})
        foreach(float velocity in new[]{150f,0f,-150f})
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=ArenaFixture(BossKind.Submarine,kind);Set(run,"PlayerY",140f);Set(run,"AnimationVelocityY",velocity);Field(run,"<FacingLeft>k__BackingField",left);
            int frame=RunnerGeometry.GetPlayerFrame(run);Check(frame>=8,"Swimming still uses ground running/crouching poses");
            float width=RunnerGeometry.PlayerStandingWidth,height=RunnerGeometry.PlayerStandingHeight;
            Rect logical=new Rect(run.PlayerX-width*.5f,RunnerSession.GroundY-run.PlayerY-height,width,height);
            Rect helmet=(Rect)layout.Invoke(null,new object[]{run,logical});
            float faceX=helmet.x+helmet.width*(left?.34f:.66f),faceY=helmet.y+helmet.height*.49f;
            Check(faceX>logical.x-5&&faceX<logical.xMax+5&&faceY>logical.y-8&&faceY<logical.yMax+8,"Helmet has drifted away from the character head/body");
            int clear=0,total=0;
            for(float dy=-4;dy<=4;dy+=2)for(float dx=-4;dx<=4;dx+=2)
            {total++;if(RenderedAlpha(helmetPng,helmet,art.GetDivingKitUV(0),left,faceX+dx,faceY+dy)<128)clear++;}
            Check(clear>total*.65f,"Helmet glass covers the character's face with an opaque sprite");
            foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
            {
                Draw(view,run,canvas);RecordedDraw glass=GUI.DrawCalls.Single(d=>d.Texture==art.DivingKitAtlas&&Math.Abs(Math.Abs(d.UV.width)-art.GetDivingKitUV(0).width)<.00001f);
                Check(SameRect(glass.Position,helmet),"Helmet drawing and head layout diverge");Point(glass.Centre,canvas.MultiplyPoint3x4(helmet.center),"Helmet acquired an extra canvas transform");
                Near(glass.UV.width,left?-art.GetDivingKitUV(0).width:art.GetDivingKitUV(0).width,"Helmet did not mirror with facing");
                var hero=GUI.DrawCalls.Single(d=>d.Texture==art.GetCharacterTexture(kind,frame));
                Check(GUI.DrawCalls.IndexOf(glass)>GUI.DrawCalls.IndexOf(hero),"Helmet is hidden behind dinosaur");
                Check(!GUI.DrawCalls.Any(d=>d.Texture==art.Hoverboard),"Swimmer retained the aerial hoverboard");
            }
        }
    }
    private static void NavalOrdnanceV13()
    {
        var art=new RunnerArt();var run=ArenaFixture(BossKind.Submarine);var shots=(IList<BossProjectile>)run.BossProjectiles;var marks=(IList<BossTelegraph>)run.BossTelegraphs;var obstacles=(IList<BossObstacle>)run.BossObstacles;
        shots.Clear();marks.Clear();obstacles.Clear();
        using(var view=new RunnerWorldView(art))
        foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
        {
            foreach(var pair in new[]{Tuple.Create(BossAttackKind.Torpedo,2),Tuple.Create(BossAttackKind.FishShoal,1),Tuple.Create(BossAttackKind.DepthCharge,3)})
            {
                foreach(float direction in new[]{-1f,1f})
                {
                    shots.Clear();var shot=new BossProjectile{Id=99001,Kind=pair.Item1,X=542,Y=155,Width=pair.Item2==2?48:28,Height=pair.Item2==2?20:28,Radius=pair.Item2==2?10:13,VelocityX=pair.Item2==3?0:300*direction,VelocityY=pair.Item2==3?-200:0,Lifetime=4};shots.Add(shot);
                    Draw(view,run,canvas);Rect uv=art.GetDivingKitUV(pair.Item2);var sprite=GUI.DrawCalls.Single(d=>d.Texture==art.DivingKitAtlas&&SameRect(d.UV,uv));
                    Vector3 centre=canvas.MultiplyPoint3x4(new Vector3(shot.X+shot.Width*.5f,RunnerSession.GroundY-shot.Y-shot.Height*.5f,0));Point(sprite.Centre,centre,"Naval sprite left its true round contact centre");
                    Check(GUI.DrawCalls.Any(d=>d.Texture?.name=="CityDino disc"&&Math.Abs(d.Centre.x-centre.x)<.03f&&Math.Abs(d.Centre.y-centre.y)<.03f),"Naval round contact has no visible central body");
                }
            }
            shots.Clear();var mine=new BossObstacle{Id=99009,Kind=BossAttackKind.SeaMine,X=604,Y=180,Width=38,Height=38,Radius=19,Active=true,Health=24,MaxHealth=24,Remaining=5};obstacles.Add(mine);
            Draw(view,run,canvas);RecordedDraw body=GUI.DrawCalls.Single(d=>d.Texture==art.DivingKitAtlas&&SameRect(d.UV,art.GetDivingKitUV(3)));
            Point(body.Centre,canvas.MultiplyPoint3x4(new Vector3(623,RunnerSession.GroundY-199,0)),"Sea mine rotates away from its circular body");obstacles.Clear();
            var burst=new BossObstacle{Id=99010,Kind=BossAttackKind.DepthCharge,X=565,Y=100,Width=64,Height=64,Radius=32,Active=true,Remaining=.32f};obstacles.Add(burst);
            Draw(view,run,canvas);Rect circle=new Rect(565,RunnerSession.GroundY-164,64,64);
            Check(GUI.DrawCalls.Any(d=>d.Texture?.name=="CityDino disc"&&SameRect(d.Position,circle)&&d.Color.a>=.45f),"Depth-charge impact omits its actual circular damage area");
            Check(!GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,circle)&&d.Color.a>=.3f),"Underwater depth burst regressed to a plastic rectangular blast");obstacles.Clear();
            var warning=new BossTelegraph{Id=99011,Kind=BossAttackKind.SonarBeam,X=75,Y=175,Width=760,Height=14,Duration=1.2f,Remaining=.8f};marks.Add(warning);
            Draw(view,run,canvas);Rect beam=new Rect(75,RunnerSession.GroundY-189,760,14);
            Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,beam)&&d.Color.a<.3f),"Sonar warning does not preview its exact harmless lane");
            warning.Active=true;Draw(view,run,canvas);Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,beam)&&d.Color.a>=.6f),"Live sonar is still painted as a harmless warning");marks.Clear();
        }
    }
    private static void UnderwaterExitV13()
    {
        var art=new RunnerArt();
        foreach(float start in new[]{100f,720f})
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=ArenaFixture(BossKind.Submarine);Set(run,"PlayerX",start);Set(run,"PlayerY",230f);
            typeof(RunnerSession).GetMethod("BeginBossVictory",Private).Invoke(run,null);bool surfaced=false,crossed=false;float previous=run.DepartureWorldX;
            for(int i=0;i<120*5&&run.ArenaPhase==BossPhase.Victory;i++)
            {
                if(i%18==0)
                {
                    Draw(view,run,Canvas(.68f,119,28));Check(run.DepartureWorldX>=previous-.01f,"Swimmer turned back toward entrance");previous=run.DepartureWorldX;
                    if(run.WaterDepth<.001f){surfaced=true;Check(!run.DivingHelmetEquipped&&run.PlayerY<.1f,"Swimmer reached air but retained equipment or floating feet");}
                    if(run.DepartureWorldX>=RunnerSession.BossExitGateX)
                    {
                        crossed=true;Check(surfaced,"Diver crossed exit gate while still underwater");
                        Check(GUI.DrawCalls.Any(d=>d.Texture==art.ArenaGateAtlas)&&GUI.DrawCalls.Any(d=>d.Texture==art.Road),"Surface exit omits gate or onward running street");
                    }
                }
                run.Update(1f/120);
            }
            Check(surfaced&&crossed&&!run.IsBossArena&&run.PlayerY==0,"Submarine exit failed to surface and resume the forward street");
        }
    }
    private static void RunMissileRenderingV13()
    {
        var art=new RunnerArt();
        foreach(int count in new[]{1,2,3,4})
        using(var view=new RunnerWorldView(art))
        {
            RunnerSession run=New();Field(run,"distanceToPattern",float.MaxValue);var warnings=(IList<RunMissileWarning>)run.RunMissileWarnings;var shots=(IList<RunMissile>)run.RunMissiles;
            warnings.Clear();shots.Clear();var warning=new RunMissileWarning{Id=99100,VolleyId=910,Count=count,X=90,Y=14,Width=1190,Height=22,Duration=1.2f,Remaining=.8f};warnings.Add(warning);
            foreach(Matrix4x4 canvas in new[]{Canvas(1),Canvas(.68f,119,28),Canvas(1.5f,320,0)})
            {
                Draw(view,run,canvas);Check(GUI.DrawCalls.Any(d=>d.Texture==Texture2D.whiteTexture&&SameRect(d.Position,new Rect(92,524,1134,22))&&d.Color.a<.1f),"Normal rocket warning is absent or looks immediately damaging");
            }
            warnings.Clear();typeof(RunnerSession).GetMethod("LaunchRunMissileVolley",Private).Invoke(run,new object[]{warning});Check(shots.Count==count,"Live render fixture lost simultaneously spawned rocket count");
            for(int i=0;i<count;i++)shots[i].X=630+i*100;
            Draw(view,run,Canvas(.68f,119,28));
            foreach(RunMissile shot in shots)
            {
                Vector3 centre=Canvas(.68f,119,28).MultiplyPoint3x4(new Vector3(shot.X+20,RunnerSession.GroundY-shot.Y-11,0));
                Check(GUI.DrawCalls.Any(d=>d.Texture?.name=="CityDino disc"&&Math.Abs(d.Centre.x-centre.x)<.03f&&Math.Abs(d.Centre.y-centre.y)<.03f),"Running missile has no metal body at its damaging circle");
            }
            string drawn=DrawSignature();run.TogglePause();run.Update(3);GUI.DrawCalls.Clear();GUI.matrix=Canvas(.68f,119,28);view.Draw(run,9999);Check(DrawSignature()==drawn,"Paused normal rockets keep moving/drawing wall-time flames");
        }
    }
    private static void UnderwaterHudV13()
    {
        var art=new RunnerArt();using(var view=new RunnerWorldView(art))
        {
            var run=ArenaFixture(BossKind.Submarine);var controller=Ui(run,art,view);DrawUi(controller);
            foreach(string caption in new[]{"BƠI","LẶN","BẮN"})Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.StartsWith(caption)),"Underwater HUD lacks "+caption);
            Check(!GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.StartsWith("BAY")),"Underwater HUD still calls swimming flying");
            Check(GUI.DrawCalls.Any(d=>d.Kind=="Label"&&d.Text!=null&&d.Text.Contains("CẤP 3")),"Normal submarine lacks the requested initial C3 tier");
        }
    }

}
