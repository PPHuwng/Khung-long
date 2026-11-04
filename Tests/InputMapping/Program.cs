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
        public bool Jump, Duck, Shoot, Skill, Lift;
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
        Test("flight touch separates tap flap from continuous lift", FlightTouch);
        Test("flight supports simultaneous movement lift and shooting", MultitouchFlight);
        Test("flight touch retains scale letterbox and synthetic mouse protection", FlightPointerScale);
        Test("Space W and Up hold flight lift and release returns to falling", FlightKeyboardHold);
        Test("flight tap and horizontal movement remain independent of shooting", FlightKeyboardTap);
        Test("all three characters can shoot in the normal 20k aerial encounter", FlightCharacterFire);
        Test("scripted arena entrance ignores manual movement flight and combat", ScriptedTransitionInput);
        Test("pause freezes the scripted entrance and active flight", FlightAndTransitionPause);
        Test("practice command trials locked characters without changing normal inventory", PracticeCommand);
        Test("practice retry key replays its selected trial without starting a normal run", PracticeRetryKey);
        Test("pause and release clear held flight thrust before resuming", FlightPauseRelease);
        Test("enemy lunges keep player steering jumping flying shooting and skills available", BossChargeControls);
        Test("selected practice danger tier survives start and retry without consuming normal stock", PracticeTierRetry);
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
        EnterArena(run,BossKind.Tank);
        for(int i=0;i<600&&run.ArenaPhase!=BossPhase.Fighting;i++)run.Update(1f/120);
        Check(run.ArenaPhase==BossPhase.Fighting,"Arena intro did not complete");return run;
    }
    private static void EnterArena(RunnerSession run,BossKind boss)
    {
        int ordinal=boss==BossKind.Tank?0:boss==BossKind.Gunship?1:2;
        int milestone=(ordinal+1)*RunnerSession.BossScoreInterval;
        SetField(run,"<BossNumber>k__BackingField",ordinal);
        SetField(run,"<NextBossScore>k__BackingField",milestone);
        SetField(run,"distanceClock",milestone*20d-1d);SetField(run,"<Distance>k__BackingField",milestone*20f-1f);
        run.Update(.02f);
        Check(run.IsBossArena&&run.CurrentBoss.Kind==boss,"Normal milestone selected the wrong arena");
    }
    private static RunnerSession FlightArena(CharacterKind character=CharacterKind.Classic)
    {
        RunnerSession run=Session(character);EnterArena(run,BossKind.Gunship);
        for(int i=0;i<600&&run.ArenaPhase!=BossPhase.Fighting;i++)run.Update(1f/120);
        Check(run.FlightControlsEnabled&&run.FlightDeviceCollected,"20k encounter did not enable its collected flight device");
        // Input tests isolate routing, not the attack survival already checked
        // by core fixtures. Keep long held/released input samples alive.
        SetField(run,"<InvincibleRemaining>k__BackingField",1000f);return run;
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
        // Real forward departure, rather than only a phase flag: pressing left,
        // jump, fire or a skill cannot reverse the staged exit through the gate.
        foreach(BossKind boss in Enum.GetValues(typeof(BossKind)))
        {
            run=Session(CharacterKind.Raptor);EnterArena(run,boss);
            for(int i=0;i<600&&run.ArenaPhase!=BossPhase.Fighting;i++)run.Update(1f/120);
            run.CurrentBoss.Health=0;run.Update(.01f);
            for(int i=0;i<300&&run.ExitTravelProgress<=.02f;i++)run.Update(1f/120);
            Check(run.ArenaPhase==BossPhase.Victory&&run.ExitTravelProgress>.02f,"Real forward exit fixture did not start");
            controller=Controller(run);Keyboard.current=new Keyboard();Touchscreen.current=null;Mouse.current=null;
            Keyboard.current.aKey.isPressed=true;Keyboard.current.spaceKey.isPressed=true;Keyboard.current.spaceKey.wasPressedThisFrame=true;
            Keyboard.current.jKey.isPressed=true;Keyboard.current.eKey.wasPressedThisFrame=true;
            float worldX=run.DepartureWorldX,y=run.PlayerY;int score=run.Score;
            Frame(controller);
            Check(run.DepartureWorldX>worldX&&!run.FacingLeft&&run.Score==score&&run.SkillCooldownRemaining==0&&
                !run.BossProjectiles.Any(p=>p.Friendly&&!p.Removed),"Player commands interrupted or reversed forward departure");
            Check(run.PlayerY<=y+.001f,"Held jump/lift made the leaving character rise instead of following the landing scene");
        }
    }
    private static void ModalStartKeys()
    {
        foreach(string modal in new[]{"showCharacters","showShop","showHistory","showPractice"})
        {
            var run=new RunnerSession(new RunnerSave{OwnsTriceratops=true,SelectedCharacter=CharacterKind.Triceratops},3);
            var controller=Controller(run);SetField(controller,modal,true);
            Keyboard.current=new Keyboard();Keyboard.current.spaceKey.wasPressedThisFrame=true;Keyboard.current.eKey.wasPressedThisFrame=true;Keyboard.current.jKey.isPressed=true;
            Frame(controller);Check(run.State==RunState.Menu&&run.SkillCooldownRemaining==0,modal+" leaked menu keys into gameplay");
        }
    }
    private static void ModalRestartKeys()
    {
        foreach(string modal in new[]{"showCharacters","showShop","showHistory","showPractice"})
        {
            RunnerSession run=Session(CharacterKind.Classic);
            ((IList<WorldEntity>)typeof(RunnerSession).GetField("entities",PrivateInstance).GetValue(run)).Add(new WorldEntity{Kind=EntityKind.Barrier,X=run.PlayerX-80,Y=0,Width=160,Height=200});
            run.Update(1f/120);Check(run.State==RunState.GameOver,"Fatal contact fixture did not finish the run");
            var controller=Controller(run);SetField(controller,modal,true);Keyboard.current=new Keyboard();Keyboard.current.rKey.wasPressedThisFrame=true;
            Frame(controller);Check(run.State==RunState.GameOver,modal+" leaked R into restart");
        }
    }

    private static Inputs FlightPoint(float x,float y,bool down=true,bool held=true)
    {
        MethodInfo method=typeof(CityRunnerController).GetMethod("ApplyGameplayPointer",BindingFlags.Static|BindingFlags.NonPublic);
        Check(method!=null,"Shared gameplay pointer mapping missing");
        object[] args={new Vector2(x,y),down,held,true,true,true,0f,false,false,false,false,false};
        method.Invoke(null,args);
        return new Inputs{Horizontal=(float)args[6],Jump=(bool)args[7],Duck=(bool)args[8],Shoot=(bool)args[9],Skill=(bool)args[10],Lift=(bool)args[11]};
    }
    private static Inputs ReadFlight()
    {
        MethodInfo method=typeof(CityRunnerController).GetMethod("ReadGameplayPointers",BindingFlags.Static|BindingFlags.NonPublic);
        Check(method!=null,"Shared gameplay touch iteration missing");
        object[] args={true,true,true,0f,false,false,false,false,false};method.Invoke(null,args);
        return new Inputs{Horizontal=(float)args[3],Jump=(bool)args[4],Duck=(bool)args[5],Shoot=(bool)args[6],Skill=(bool)args[7],Lift=(bool)args[8]};
    }
    private static void FlightTouch()
    {
        foreach(Vector2 point in new[]{new Vector2(1182,67),new Vector2(500,320)})
        {
            Inputs tap=FlightPoint(point.x,point.y);
            Check(tap.Jump&&tap.Lift&&!tap.Duck&&!tap.Shoot&&!tap.Skill,"Flight press did not request one flap plus held lift");
            Inputs hold=FlightPoint(point.x,point.y,false,true);
            Check(!hold.Jump&&hold.Lift&&!hold.Duck,"Flight hold repeated a tap or lost continuous lift");
            Inputs release=FlightPoint(point.x,point.y,false,false);
            Check(!release.Jump&&!release.Lift&&!release.Duck,"Released flight pointer kept thrust active");
        }
        Inputs consumed=FlightPoint(1056,67);
        Check(!consumed.Jump&&!consumed.Lift&&!consumed.Duck,"Unused flight crouch region leaked to a flap");
        Inputs shoot=FlightPoint(784,67,false,true);
        Check(shoot.Shoot&&!shoot.Lift&&!shoot.Jump,"Fire region also activated the flight device");
    }
    private static void MultitouchFlight()
    {
        Touchscreen.current=new Touchscreen();
        Touchscreen.current.touches.Add(Touch(180,67,false,true));
        Touchscreen.current.touches.Add(Touch(1182,67,false,true));
        Touchscreen.current.touches.Add(Touch(784,67,false,true));
        Inputs held=ReadFlight();
        Check(held.Horizontal==1&&held.Lift&&held.Shoot&&!held.Jump&&!held.Duck,"Three held fingers could not steer fly and shoot together");
        Touchscreen.current.touches.Add(Touch(914,67));
        Check(ReadFlight().Skill,"Fourth skill finger is ignored during airborne movement/fire");
        Touchscreen.current.touches.RemoveAt(1);
        Check(!ReadFlight().Lift,"Lifting the flight finger failed to release thrust while other fingers remained");
    }
    private static void FlightPointerScale()
    {
        Screen.width=1920;Screen.height=1080;
        Check(FlightPoint(1773,100.5f,false,true).Lift,"1080p BAY region no longer maps to lift");
        Screen.height=720;
        Check(FlightPoint(1502,67,false,true).Lift,"Ultrawide BAY offset no longer maps to lift");
        Inputs margin=FlightPoint(100,320);
        Check(!margin.Jump&&!margin.Lift&&!margin.Shoot,"Pillarbox margin became a flight input");
        Screen.width=1280;Touchscreen.current=new Touchscreen();Touchscreen.current.touches.Add(Touch(784,67,false,true));
        Mouse.current=new Mouse{leftButton=new ButtonControl{wasPressedThisFrame=true,isPressed=true},position=new Vector2Control{TestValue=new Vector2(500,320)}};
        Inputs input=ReadFlight();Check(input.Shoot&&!input.Jump&&!input.Lift,"Synthetic mouse event duplicated a flight/combat touch");
    }
    private static void FlightKeyboardHold()
    {
        foreach(string key in new[]{"spaceKey","wKey","upArrowKey"})
        {
            RunnerSession run=FlightArena();var controller=Controller(run);float start=run.PlayerY;
            Keyboard.current=new Keyboard();var control=(ButtonControl)typeof(Keyboard).GetField(key).GetValue(Keyboard.current);control.isPressed=true;
            for(int i=0;i<45;i++)Frame(controller);
            Check(run.PlayerY>start+30&&!run.IsDucking,"Held "+key+" did not lift the airborne character");
            float high=run.PlayerY;control.isPressed=false;for(int i=0;i<70;i++)Frame(controller);
            Check(run.PlayerY<high-30&&run.PlayerY>=RunnerSession.FlightMinimumY,"Released "+key+" did not restore controlled falling");
        }
    }
    private static void FlightKeyboardTap()
    {
        RunnerSession run=FlightArena();var controller=Controller(run);float y=run.PlayerY,x=run.PlayerX;
        Keyboard.current=new Keyboard();Keyboard.current.spaceKey.wasPressedThisFrame=true;Keyboard.current.dKey.isPressed=true;Keyboard.current.jKey.isPressed=true;
        Frame(controller);
        Check(run.PlayerY>y&&run.PlayerX>x&&run.BossProjectiles.Any(p=>p.Friendly),"Space tap failed to flap while steering and firing");
        Keyboard.current.spaceKey.wasPressedThisFrame=false;Keyboard.current.sKey.isPressed=true;Frame(controller);
        Check(!run.IsDucking,"Ground crouch remained active in the aerial arena");
    }
    private static void FlightCharacterFire()
    {
        foreach(CharacterKind kind in Enum.GetValues(typeof(CharacterKind)))
        {
            RunnerSession run=FlightArena(kind);var controller=Controller(run);Keyboard.current=new Keyboard();Keyboard.current.jKey.isPressed=true;
            Keyboard.current.spaceKey.isPressed=true;Frame(controller);
            Check(run.CurrentBoss.Kind==BossKind.Gunship&&run.BossNumber==2&&run.BossProjectiles.Any(p=>p.Friendly),kind+" cannot fly and use its normal weapon at 20k");
        }
    }
    private static void ScriptedTransitionInput()
    {
        foreach(BossKind kind in Enum.GetValues(typeof(BossKind)))
        {
            RunnerSession input=Session(CharacterKind.Triceratops),automatic=Session(CharacterKind.Triceratops);
            EnterArena(input,kind);EnterArena(automatic,kind);
            var manualController=Controller(input);var automaticController=Controller(automatic);
            for(int i=0;i<180;i++)
            {
                Keyboard.current=new Keyboard();Keyboard.current.aKey.isPressed=true;Keyboard.current.spaceKey.isPressed=true;Keyboard.current.spaceKey.wasPressedThisFrame=true;
                Keyboard.current.sKey.isPressed=true;Keyboard.current.jKey.isPressed=true;Keyboard.current.eKey.wasPressedThisFrame=true;
                Touchscreen.current=new Touchscreen();Touchscreen.current.touches.Add(Touch(180,67));Touchscreen.current.touches.Add(Touch(1182,67));Touchscreen.current.touches.Add(Touch(784,67));
                Frame(manualController);Keyboard.current=new Keyboard();Touchscreen.current=null;Frame(automaticController);
                Check(Math.Abs(input.PlayerX-automatic.PlayerX)<.001&&Math.Abs(input.PlayerY-automatic.PlayerY)<.001,"Manual input displaced scripted "+kind+" approach/takeoff");
                Check(input.SkillCooldownRemaining==0&&!input.BossProjectiles.Any(p=>p.Friendly)&&!input.IsDucking,"Transition accepted manual skill fire or crouch");
            }
            Check(input.ArenaPhase==BossPhase.Intro,"Transition input fixture advanced past its 4.8 second cinematic");
        }
    }
    private static void FlightAndTransitionPause()
    {
        foreach(bool flight in new[]{false,true})
        {
            RunnerSession run=flight?FlightArena():Session(CharacterKind.Classic);
            if(!flight){EnterArena(run,BossKind.Gunship);run.Update(.8f);}
            var controller=Controller(run);Keyboard.current=new Keyboard();Keyboard.current.escapeKey.wasPressedThisFrame=true;Frame(controller);
            Check(run.State==RunState.Paused,"Escape did not pause "+(flight?"active flight":"takeoff"));
            float x=run.PlayerX,y=run.PlayerY,clock=run.BossElapsed,phase=run.BossPhaseRemaining;
            Keyboard.current.escapeKey.wasPressedThisFrame=false;Keyboard.current.spaceKey.isPressed=true;Keyboard.current.dKey.isPressed=true;Keyboard.current.jKey.isPressed=true;
            for(int i=0;i<60;i++)Frame(controller);
            Check(run.PlayerX==x&&run.PlayerY==y&&run.BossElapsed==clock&&run.BossPhaseRemaining==phase,"Paused flight/cinematic accepted movement or advanced its clock");
        }
    }
    private static void PracticeCommand()
    {
        var save=new RunnerSave{Wallet=4321,BestScore=7654,LastScore=1234,PurchasedHearts=2,PurchasedShield=true,PurchasedWaterReserve=true,PurchasedWarmReserve=true};
        var run=new RunnerSession(save,113);int saves=0;run.SaveChanged+=()=>saves++;
        var controller=Controller(run);SetField(controller,"showPractice",true);SetField(controller,"practiceBoss",BossKind.Gunship);
        SetField(controller,"practiceCharacter",CharacterKind.Raptor);SetField(controller,"practiceLearning",false);
        MethodInfo start=typeof(CityRunnerController).GetMethod("StartPractice",PrivateInstance);Check(start!=null,"Practice start command is missing");
        start.Invoke(controller,null);
        Check(run.IsPractice&&run.CurrentBoss.Kind==BossKind.Gunship&&run.CurrentCharacter==CharacterKind.Raptor&&!run.PracticeLearning,"Prepared practice selection did not reach the real practice session");
        Check(!save.OwnsRaptor&&!save.OwnsTriceratops&&save.SelectedCharacter==CharacterKind.Classic,"Practice trial unlocked or equipped a normal character");
        Check(save.Wallet==4321&&save.BestScore==7654&&save.LastScore==1234&&save.PurchasedHearts==2&&save.PurchasedShield&&save.PurchasedWaterReserve&&save.PurchasedWarmReserve&&saves==0,"Practice command spent inventory or saved normal progress");
        Check(!(bool)typeof(CityRunnerController).GetField("showPractice",PrivateInstance).GetValue(controller),"Starting practice left its modal covering play");
    }

    private static void PracticeRetryKey()
    {
        var save=new RunnerSave{Wallet=4321,BestScore=7654,LastScore=1234,PurchasedHearts=2,PurchasedShield=true,PurchasedWaterReserve=true,PurchasedWarmReserve=true};
        var run=new RunnerSession(save,14);int saves=0;run.SaveChanged+=()=>saves++;
        run.StartBossPractice(BossKind.Gunship,CharacterKind.Raptor,false);run.SkipPracticeIntro();
        typeof(RunnerSession).GetMethod("FinishPractice",PrivateInstance).Invoke(run,new object[]{false,"Input retry fixture"});
        var controller=Controller(run);Keyboard.current=new Keyboard();Keyboard.current.rKey.wasPressedThisFrame=true;Frame(controller);
        Check(run.State==RunState.Playing&&run.IsPractice&&!run.PracticeLearning&&run.CurrentBoss.Kind==BossKind.Gunship&&run.CurrentCharacter==CharacterKind.Raptor,"R started a normal run or lost prepared practice selections");
        Check(!run.PracticeFinished&&run.PracticeHits==0&&run.PracticeElapsed==0&&run.Lives==3,"Retry retained the preceding practice outcome");
        Check(save.Wallet==4321&&save.BestScore==7654&&save.LastScore==1234&&save.PurchasedHearts==2&&save.PurchasedShield&&save.PurchasedWaterReserve&&save.PurchasedWarmReserve&&saves==0,"Practice R spent purchased normal-run stock or recorded progress");
    }
    private static void FlightPauseRelease()
    {
        RunnerSession run=FlightArena();var controller=Controller(run);Keyboard.current=new Keyboard();Keyboard.current.spaceKey.isPressed=true;
        for(int i=0;i<15;i++)Frame(controller);
        Keyboard.current.escapeKey.wasPressedThisFrame=true;Frame(controller);Check(run.State==RunState.Paused,"Flight pause fixture failed");
        Keyboard.current.escapeKey.wasPressedThisFrame=false;Keyboard.current.spaceKey.isPressed=false;for(int i=0;i<10;i++)Frame(controller);
        Keyboard.current.escapeKey.wasPressedThisFrame=true;Frame(controller);Keyboard.current.escapeKey.wasPressedThisFrame=false;
        Check(run.State==RunState.Playing,"Escape failed to resume flight");
        float resumeY=run.PlayerY;for(int i=0;i<60;i++)Frame(controller);
        Check(run.PlayerY<resumeY-20&&run.VelocityY<=0,"Released lift remained stuck after pause/resume");
    }

    private static void BossChargeControls()
    {
        FieldInfo active=typeof(BossEncounter).GetField("ContactActive");
        Check(active!=null,"Boss charge contact model is missing");
        foreach(BossKind boss in Enum.GetValues(typeof(BossKind)))
        foreach(CharacterKind character in Enum.GetValues(typeof(CharacterKind)))
        {
            var run=new RunnerSession(new RunnerSave(),147);
            run.StartBossPractice(boss,character,false);run.SkipPracticeIntro();
            // This isolates player routing during enemy actions; survival and
            // real charge damage are checked separately in the core suite.
            SetField(run,"<InvincibleRemaining>k__BackingField",1000f);
            for(int i=0;i<120*60&&!(bool)active.GetValue(run.CurrentBoss);i++)run.Update(1f/120);
            Check((bool)active.GetValue(run.CurrentBoss),"No actual lunge reached its strike phase: "+boss);
            var controller=Controller(run);float x=run.PlayerX,y=run.PlayerY;
            Keyboard.current=new Keyboard();Keyboard.current.aKey.isPressed=true;
            Keyboard.current.spaceKey.wasPressedThisFrame=true;Keyboard.current.spaceKey.isPressed=true;
            Keyboard.current.jKey.isPressed=true;Keyboard.current.eKey.wasPressedThisFrame=true;
            Frame(controller);
            Check(run.PlayerX<x,"Enemy lunge locked player steering: "+boss+" / "+character);
            Check(run.PlayerY>y,"Enemy lunge locked jump/flap: "+boss+" / "+character);
            // A shot may immediately strike nearby scenery in this same
            // frame. The real firing cooldown proves a shot was emitted even
            // when there is no surviving friendly projectile to count.
            Check((bool)typeof(RunnerSession).GetField("shootingHeld",PrivateInstance).GetValue(run)&&
                (double)typeof(RunnerSession).GetField("shotCooldown",PrivateInstance).GetValue(run)>0,
                "Enemy lunge locked the common weapon: "+boss+" / "+character);
            Check((run.SkillCooldownRemaining>0)==(character!=CharacterKind.Classic),"Enemy lunge changed character skill routing");
        }
    }

    private static void PracticeTierRetry()
    {
        foreach(int tier in new[]{2,5,8})
        {
            var save=new RunnerSave{Wallet=4321,BestScore=7654,LastScore=1234,PurchasedHearts=2,
                PurchasedShield=true,PurchasedWaterReserve=true,PurchasedWarmReserve=true};
            var run=new RunnerSession(save,14);int saves=0;run.SaveChanged+=()=>saves++;
            var controller=Controller(run);SetField(controller,"showPractice",true);
            SetField(controller,"practiceBoss",BossKind.Gunship);SetField(controller,"practiceCharacter",CharacterKind.Raptor);
            SetField(controller,"practiceLearning",false);SetField(controller,"practiceTier",tier);
            typeof(CityRunnerController).GetMethod("StartPractice",PrivateInstance).Invoke(controller,null);
            Check(run.IsPractice&&run.BossTier==tier&&run.CurrentBoss.Tier==tier,"Selected danger tier did not reach production practice");
            float hp=run.CurrentBoss.MaxHealth;
            Check(hp>800,"Higher-tier practice kept first-round helicopter health");
            run.SkipPracticeIntro();
            typeof(RunnerSession).GetMethod("FinishPractice",PrivateInstance).Invoke(run,new object[]{false,"Tier retry input fixture"});
            Keyboard.current=new Keyboard();Keyboard.current.rKey.wasPressedThisFrame=true;Frame(controller);
            Check(run.State==RunState.Playing&&run.IsPractice&&run.BossTier==tier&&run.CurrentBoss.Tier==tier,
                "R retry silently reset danger tier");
            Check(run.CurrentBoss.Kind==BossKind.Gunship&&run.CurrentCharacter==CharacterKind.Raptor&&!run.PracticeLearning&&
                run.CurrentBoss.MaxHealth==hp,"Retry changed the chosen training round");
            Check(save.Wallet==4321&&save.BestScore==7654&&save.LastScore==1234&&save.PurchasedHearts==2&&
                save.PurchasedShield&&save.PurchasedWaterReserve&&save.PurchasedWarmReserve&&
                !save.OwnsRaptor&&!save.OwnsTriceratops&&save.SelectedCharacter==CharacterKind.Classic&&saves==0,
                "Training danger-tier input spent normal inventory or unlocked a trial character");
            Keyboard.current=null;
        }
    }

}
