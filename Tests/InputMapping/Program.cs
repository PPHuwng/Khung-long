// Executes production pointer hit-testing and touch iteration against the small
// Vector2/Rect and fake-device surface in UnityStubs. This is not a Unity event,
// Android/iOS, rendering, or Play Mode test.
using System;
using System.Reflection;
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
        public bool Jump, Duck;
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
        Touchscreen.current = null; Mouse.current = null;
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
}
