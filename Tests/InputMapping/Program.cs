// Executes production pointer hit-testing and touch iteration against the small
// Vector2/Rect and fake-device surface in UnityStubs. This is not a Unity event,
// Android/iOS, rendering, or Play Mode test.
using System;
using System.Reflection;
using System.Collections.Generic;
using System.Linq;
using CityDino;
using UnityEngine;
using UnityEngine.InputSystem;

internal static class Program
{
    private static int passed, failed;
    private static readonly MethodInfo Apply = typeof(CityRunnerController).GetMethod("ApplyPointer", BindingFlags.NonPublic | BindingFlags.Static);
    private static readonly MethodInfo Read = typeof(CityRunnerController).GetMethod("ReadPointers", BindingFlags.NonPublic | BindingFlags.Static);

    private struct Inputs
    {
        public float Horizontal;
        public bool Jump, Duck, Shoot, Skill;
    }

    private static int Main()
    {
        Check(Apply != null && Read != null, "Production pointer entry points were not found");
        Test("left touch moves without jumping", Left);
        Test("right touch moves without jumping", Right);
        Test("released move button does not become a jump tap", ReleasedMove);
        Test("left and right touches cancel each other", OppositeTouches);
        Test("duck touch does not jump", Duck);
        Test("world tap jumps once and holding does not repeat", WorldTap);
        Test("HUD and bottom margin do not trigger gameplay input", HudAndBottom);
        Test("1920x1080 coordinates map to the same move button", ScaledScreen);
        Test("ultrawide offset maps to the same move button", UltrawideScreen);
        Test("ultrawide pillarbox touches are ignored", Pillarbox);
        Test("two live touch slots can move and jump together", MultitouchMoveJump);
        Test("active touch suppresses duplicate synthetic mouse input", SyntheticMouse);
        Test("inactive touch slots permit real mouse input", MouseFallback);
        Test("ignored pointer preserves an existing keyboard intent", ExistingKeyboardIntent);
        Test("held boss fire touch does not jump", BossFireTouch);
        Test("skill touch triggers once without jumping", SkillTouch);
        Test("hidden combat controls do not request shooting or skills", HiddenCombatControls);
        Test("three touches can move jump and shoot together", MultitouchCombat);
        Test("combat touch suppresses duplicate synthetic mouse input", CombatSyntheticMouse);
        Test("combat controls preserve scale and pillarbox mapping", CombatScale);
        Test("every character fires with J during boss combat", BossKeyboardFire);
        Test("J cannot fire during ordinary running", RunningKeyboardFire);
        Test("E is a special skill for the two purchased characters", CharacterSkillKey);
        Test("pause blocks boss fire and character skills", PausedCombatKeys);
        Test("arena transition controls consume touches without firing or jumping", ArenaTransitionControls);
        Test("menu modals block starting and special action keys", ModalStartKeys);
        Test("game over modals block restart keys", ModalRestartKeys);
        Console.WriteLine();
        Console.WriteLine("Input mapping: " + passed + " passed, " + failed + " failed.");
        Console.WriteLine("Production mapping ran against math/device stubs; Unity event delivery and device hardware were not executed.");
        return failed == 0 ? 0 : 1;
    }

    private static void Check(bool ok, string message)
    {
        if (!ok) throw new InvalidOperationException(message);
    }

    private static void Test(string name, Action body)
    {
        Screen.width = 1280; Screen.height = 720;
        Touchscreen.current = null; Mouse.current = null; Keyboard.current = null;
        Time.unscaledDeltaTime=0.016f; Time.unscaledTime=0; GUI.enabled=true;
        try { body(); passed++; Console.WriteLine("PASS " + name); }
        catch (Exception error) { failed++; Console.WriteLine("FAIL " + name + ": " + error.GetBaseException().Message); }
    }

    private static Inputs Point(float x, float y, bool down = true, bool held = true, Inputs initial = default(Inputs))
    {
        object[] args = { new Vector2(x, y), down, held, initial.Horizontal, initial.Jump, initial.Duck };
        Apply.Invoke(null, args);
        return new Inputs { Horizontal = (float)args[3], Jump = (bool)args[4], Duck = (bool)args[5] };
    }

    private static Inputs ReadDevices()
    {
        object[] args = { 0f, false, false };
        Read.Invoke(null, args);
        return new Inputs { Horizontal = (float)args[0], Jump = (bool)args[1], Duck = (bool)args[2] };
    }

    private static TouchControl Touch(float x, float y, bool down = true, bool held = true)
    {
        return new TouchControl
        {
            press = new ButtonControl { wasPressedThisFrame = down, isPressed = held },
            position = new Vector2Control { TestValue = new Vector2(x, y) }
        };
    }

    private static void NoInput(Inputs input)
    {
        Check(input.Horizontal == 0 && !input.Jump && !input.Duck, "Unexpected gameplay input outside the active area");
    }

    private static void Left()
    {
        Inputs input = Point(82, 67);
        Check(input.Horizontal == -1 && !input.Jump && !input.Duck, "Left button must only request left movement");
    }
    private static void Right()
    {
        Inputs input = Point(180, 67);
        Check(input.Horizontal == 1 && !input.Jump && !input.Duck, "Right button must only request right movement");
    }
    private static void ReleasedMove() { NoInput(Point(82, 67, true, false)); }
    private static void OppositeTouches()
    {
        Inputs input = Point(82, 67);
        input = Point(180, 67, true, true, input);
        NoInput(input);
    }
    private static void Duck()
    {
        Inputs input = Point(1056, 67);
        Check(input.Duck && !input.Jump && input.Horizontal == 0, "Duck button also triggered jump/movement");
    }
    private static void WorldTap()
    {
        Check(Point(500, 320).Jump, "World tap did not request jump");
        NoInput(Point(500, 320, false, true));
    }
    private static void HudAndBottom()
    {
        NoInput(Point(1100, 640));
        NoInput(Point(500, 0));
    }
    private static void ScaledScreen()
    {
        Screen.width = 1920; Screen.height = 1080;
        Inputs input = Point(123, 100.5f);
        Check(input.Horizontal == -1 && !input.Jump, "Scaled screen did not preserve move-button hit box");
    }
    private static void UltrawideScreen()
    {
        Screen.width = 1920; Screen.height = 720;
        Inputs input = Point(402, 67);
        Check(input.Horizontal == -1 && !input.Jump, "Pillarbox offset was not removed before hit testing");
    }
    private static void Pillarbox()
    {
        Screen.width = 1920; Screen.height = 720;
        NoInput(Point(100, 320));
        NoInput(Point(1900, 320));
    }
    private static void MultitouchMoveJump()
    {
        Touchscreen.current = new Touchscreen();
        Touchscreen.current.touches.Add(Touch(180, 67, false, true));
        Touchscreen.current.touches.Add(Touch(1182, 67));
        Inputs input = ReadDevices();
        Check(input.Horizontal == 1 && input.Jump && !input.Duck, "The second finger was ignored or movement stopped while jumping");
    }
    private static void SyntheticMouse()
    {
        Touchscreen.current = new Touchscreen();
        Touchscreen.current.touches.Add(Touch(180, 67));
        Mouse.current = new Mouse { leftButton = new ButtonControl { wasPressedThisFrame = true, isPressed = true }, position = new Vector2Control { TestValue = new Vector2(500, 320) } };
        Inputs input = ReadDevices();
        Check(input.Horizontal == 1 && !input.Jump && !input.Duck, "A touch was double-counted as a mouse jump");
    }
    private static void MouseFallback()
    {
        Touchscreen.current = new Touchscreen();
        Touchscreen.current.touches.Add(Touch(180, 67, false, false));
        Mouse.current = new Mouse { leftButton = new ButtonControl { wasPressedThisFrame = true, isPressed = true }, position = new Vector2Control { TestValue = new Vector2(500, 320) } };
        Inputs input = ReadDevices();
        Check(input.Horizontal == 0 && input.Jump && !input.Duck, "Inactive touchscreen prevented real mouse input");
    }
    private static void ExistingKeyboardIntent()
    {
        Inputs input = Point(1100, 640, true, true, new Inputs { Horizontal = -1, Jump = true });
        Check(input.Horizontal == -1 && input.Jump && !input.Duck, "Ignored HUD pointer overwrote keyboard intent");
    }
    private static Inputs CombatPoint(float x,float y,bool canShoot=true,bool canSkill=true,bool down=true,bool held=true)
    {
        MethodInfo apply=typeof(CityRunnerController).GetMethod("ApplyCombatPointer",BindingFlags.NonPublic|BindingFlags.Static);
        Check(apply!=null,"Production combat pointer entry point is missing");
        object[] args={new Vector2(x,y),down,held,canShoot,canSkill,0f,false,false,false,false};
        apply.Invoke(null,args);
        return new Inputs{Horizontal=(float)args[5],Jump=(bool)args[6],Duck=(bool)args[7],Shoot=(bool)args[8],Skill=(bool)args[9]};
    }
    private static Inputs ReadCombat(bool canShoot=true,bool canSkill=true)
    {
        MethodInfo read=typeof(CityRunnerController).GetMethod("ReadCombatPointers",BindingFlags.NonPublic|BindingFlags.Static);
        Check(read!=null,"Production combat touch reader is missing");
        object[] args={canShoot,canSkill,0f,false,false,false,false};read.Invoke(null,args);
        return new Inputs{Horizontal=(float)args[2],Jump=(bool)args[3],Duck=(bool)args[4],Shoot=(bool)args[5],Skill=(bool)args[6]};
    }
    private static void BossFireTouch()
    {
        foreach(bool down in new[]{true,false})
        {
            Inputs input=CombatPoint(784,67,down:down);
            Check(input.Shoot&&!input.Skill&&!input.Jump&&!input.Duck&&input.Horizontal==0,"Held boss fire leaked to jumping or another action");
        }
    }
    private static void SkillTouch()
    {
        Inputs down=CombatPoint(914,67);
        Check(down.Skill&&!down.Shoot&&!down.Jump,"Skill button leaked to jump or fire");
        Inputs held=CombatPoint(914,67,down:false);
        Check(!held.Skill&&!held.Shoot&&!held.Jump,"Holding skill requested repeated activations or a jump");
    }
    private static void HiddenCombatControls()
    {
        foreach(float x in new[]{784f,914f})
        {
            Inputs input=CombatPoint(x,67,false,false);
            Check(!input.Shoot&&!input.Skill,"Hidden combat button still requested a special action");
        }
    }
    private static void MultitouchCombat()
    {
        Touchscreen.current=new Touchscreen();
        Touchscreen.current.touches.Add(Touch(180,67,false,true));
        Touchscreen.current.touches.Add(Touch(1182,67));
        Touchscreen.current.touches.Add(Touch(784,67,false,true));
        Inputs input=ReadCombat();
        Check(input.Horizontal==1&&input.Jump&&input.Shoot&&!input.Skill,"Move+jump+shoot could not be held with separate fingers");
        Touchscreen.current.touches.Clear();
        Touchscreen.current.touches.Add(Touch(82,67,false,true));Touchscreen.current.touches.Add(Touch(914,67));
        input=ReadCombat(false,true);
        Check(input.Horizontal==-1&&input.Skill&&!input.Jump&&!input.Shoot,"Move+skill touch was ignored or leaked to jump");
    }
    private static void CombatSyntheticMouse()
    {
        Touchscreen.current=new Touchscreen();Touchscreen.current.touches.Add(Touch(784,67));
        Mouse.current=new Mouse{leftButton=new ButtonControl{wasPressedThisFrame=true,isPressed=true},position=new Vector2Control{TestValue=new Vector2(500,320)}};
        Inputs input=ReadCombat();Check(input.Shoot&&!input.Jump,"Boss touch was double-counted as a synthetic mouse jump");
    }
    private static void CombatScale()
    {
        Screen.width=1920;Screen.height=1080;
        Check(CombatPoint(1176,100.5f).Shoot,"Scaled boss fire box moved");
        Screen.width=1920;Screen.height=720;
        Check(CombatPoint(1104,67).Shoot,"Letterboxed boss fire box moved");
        Inputs margin=CombatPoint(100,320);Check(!margin.Shoot&&!margin.Skill&&!margin.Jump,"Pillarbox margin requested combat input");
    }
    private static readonly BindingFlags PrivateInstance=BindingFlags.Instance|BindingFlags.NonPublic;
    private static void SetField(object instance,string name,object value)
    {
        FieldInfo field=instance.GetType().GetField(name,PrivateInstance);
        Check(field!=null,"Fixture field missing: "+name);field.SetValue(instance,value);
    }
    private static RunnerSession Session(CharacterKind kind)
    {
        var run=new RunnerSession(new RunnerSave{OwnsTriceratops=true,OwnsRaptor=true,SelectedCharacter=kind},413);
        run.SetStartingDistrict(District.Day);run.StartRun();
        ((IList<WorldEntity>)typeof(RunnerSession).GetField("entities",PrivateInstance).GetValue(run)).Clear();
        SetField(run,"distanceToPattern",float.MaxValue);SetField(run,"nextHeartAt",float.MaxValue);
        return run;
    }
    private static RunnerSession Arena(CharacterKind kind)
    {
        RunnerSession run=Session(kind);
        SetField(run,"distanceClock",199999d);SetField(run,"<Distance>k__BackingField",199999f);
        run.Update(0.02f);Check(run.IsBossArena,"Milestone fixture did not enter the arena");
        for(int i=0;i<600&&run.ArenaPhase!=BossPhase.Fighting;i++)run.Update(1f/120);
        Check(run.ArenaPhase==BossPhase.Fighting,"Arena intro did not complete");return run;
    }
    private static CityRunnerController Controller(RunnerSession run)
    {
        var controller=new CityRunnerController();SetField(controller,"session",run);return controller;
    }
    private static void Frame(CityRunnerController controller)
    { typeof(CityRunnerController).GetMethod("Update",PrivateInstance).Invoke(controller,null); }
    private static void BossKeyboardFire()
    {
        foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        {
            RunnerSession run=Arena(kind);var controller=Controller(run);Keyboard.current=new Keyboard();Keyboard.current.jKey.isPressed=true;
            Frame(controller);Check(run.BossProjectiles.Any(p=>p.Friendly&&!p.Removed),kind+" cannot fire with J in boss combat");
        }
    }
    private static void RunningKeyboardFire()
    {
        foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        {
            RunnerSession run=Session(kind);var controller=Controller(run);Keyboard.current=new Keyboard();Keyboard.current.jKey.isPressed=true;
            Frame(controller);Check(!run.IsBossArena&&!run.BossProjectiles.Any(p=>p.Friendly&&!p.Removed),kind+" fires outside boss combat");
        }
    }
    private static void CharacterSkillKey()
    {
        foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        {
            RunnerSession run=Session(kind);var controller=Controller(run);Keyboard.current=new Keyboard();Keyboard.current.eKey.wasPressedThisFrame=true;
            Frame(controller);
            Check((run.SkillCooldownRemaining>0)==(kind!=CharacterKind.Classic),"E skill ownership differs for "+kind);
        }
    }
    private static void PausedCombatKeys()
    {
        RunnerSession run=Arena(CharacterKind.Triceratops);run.TogglePause();var controller=Controller(run);
        Keyboard.current=new Keyboard();Keyboard.current.jKey.isPressed=true;Keyboard.current.eKey.wasPressedThisFrame=true;
        Frame(controller);Check(run.State==RunState.Paused&&run.SkillCooldownRemaining==0&&!run.BossProjectiles.Any(p=>p.Friendly&&!p.Removed),"Pause accepted boss shooting or skills");
    }
    private static void ArenaTransitionControls()
    {
        RunnerSession run=Session(CharacterKind.Triceratops);
        SetField(run,"distanceClock",199999d);SetField(run,"<Distance>k__BackingField",199999f);run.Update(.02f);
        Check(run.ArenaPhase==BossPhase.Intro,"Intro fixture skipped transition");
        var controller=Controller(run);Keyboard.current=new Keyboard();Keyboard.current.jKey.isPressed=true;Keyboard.current.eKey.wasPressedThisFrame=true;
        Touchscreen.current=new Touchscreen();Touchscreen.current.touches.Add(Touch(784,67));Touchscreen.current.touches.Add(Touch(914,67));
        Frame(controller);Check(!run.BossProjectiles.Any(p=>p.Friendly&&!p.Removed)&&run.SkillCooldownRemaining==0&&run.PlayerY==0,
            "Intro fire/skill controls leaked to shooting, a skill or tap-jump");
        for(int i=0;i<600&&run.ArenaPhase!=BossPhase.Fighting;i++)run.Update(1f/120);
        SetField(run,"<ArenaPhase>k__BackingField",BossPhase.Victory);SetField(run,"<BossPhaseRemaining>k__BackingField",1f);
        ((IList<BossProjectile>)run.BossProjectiles).Clear();
        Frame(controller);Check(run.ArenaPhase==BossPhase.Victory&&!run.BossProjectiles.Any(p=>p.Friendly&&!p.Removed)&&run.SkillCooldownRemaining==0&&run.PlayerY==0,
            "Victory fire/skill controls leaked to shooting, a skill or tap-jump");
    }
    private static void ModalStartKeys()
    {
        foreach(string modal in new[]{"showCharacters","showShop","showHistory"})
        {
            var run=new RunnerSession(new RunnerSave{OwnsTriceratops=true,SelectedCharacter=CharacterKind.Triceratops},3);
            var controller=Controller(run);SetField(controller,modal,true);
            Keyboard.current=new Keyboard();Keyboard.current.spaceKey.wasPressedThisFrame=true;Keyboard.current.eKey.wasPressedThisFrame=true;Keyboard.current.jKey.isPressed=true;
            Frame(controller);Check(run.State==RunState.Menu&&run.SkillCooldownRemaining==0,modal+" leaked menu keys into gameplay");
        }
    }
    private static void ModalRestartKeys()
    {
        foreach(string modal in new[]{"showCharacters","showShop","showHistory"})
        {
            RunnerSession run=Session(CharacterKind.Classic);
            ((IList<WorldEntity>)typeof(RunnerSession).GetField("entities",PrivateInstance).GetValue(run)).Add(new WorldEntity{Kind=EntityKind.Barrier,X=run.PlayerX-80,Y=0,Width=160,Height=200});
            run.Update(1f/120);Check(run.State==RunState.GameOver,"Fatal contact fixture did not finish the run");
            var controller=Controller(run);SetField(controller,modal,true);Keyboard.current=new Keyboard();Keyboard.current.rKey.wasPressedThisFrame=true;
            Frame(controller);Check(run.State==RunState.GameOver,modal+" leaked R into restart");
        }
    }

}
