using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using CityDino;
using UnityEngine;

internal static class Program
{
    private static int passed,failed;
    private static readonly BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
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
        foreach(string file in Directory.GetFiles(Path.Combine(root.FullName,"Assets","CityDino","Resources","CityDino"),"*.png"))
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
}
