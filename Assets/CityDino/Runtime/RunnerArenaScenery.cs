using System;
using UnityEngine;

namespace CityDino
{
    /// <summary>Themed layered arenas and openable gates share the journey camera.</summary>
    public sealed partial class RunnerWorldView
    {
        private void DrawArenaBackdrop(BossKind kind, float clock, bool showExitGate = true)
        {
            if (kind == BossKind.Gunship)
            {
                DrawAerialArenaBackdrop(clock);
                return;
            }
            GUI.color = Color.white;
            if (art.ArenaBackdropAtlas != null)
                GUI.DrawTextureWithTexCoords(new Rect(0, 0, Width, GroundY), art.ArenaBackdropAtlas,
                    art.GetArenaBackdropUV(kind), true);
            else
            {
                Color top = kind == BossKind.Tank ? Hex(0x465967) : Hex(0x19243f);
                Color bottom = kind == BossKind.Tank ? Hex(0xc69b69) : Hex(0x496480);
                for (int i = 0; i < 24; i++)
                    Fill(new Rect(0, i * GroundY / 24f, Width, GroundY / 24f + 1f), Color.Lerp(top, bottom, i / 23f));
            }
            DrawArenaAtmosphere(kind, clock);
            DrawArenaFloor(kind, 0f, Width);
            if (showExitGate) DrawArenaGate(kind, 0f, clock, RunnerSession.BossExitGateX);
        }

        private void DrawArenaAtmosphere(BossKind kind, float clock)
        {
            if (kind == BossKind.Tank)
            {
                for (int i = 0; i < 3; i++)
                {
                    float x = 320f + i * 425f;
                    float target = x + Mathf.Sin(clock * .21f + i * 1.7f) * 250f;
                    Line(new Vector2(x, 111f + i % 2 * 12f), new Vector2(target, 385f),
                        new Color(1f, .86f, .56f, .055f), 38f);
                }
                for (int i = 0; i < 17; i++)
                {
                    float x = Repeat(i * 137f - clock * (8f + i % 4 * 4f), Width + 50f) - 25f;
                    float y = 280f + i * 47f % 265f + Mathf.Sin(clock * .7f + i) * 4f;
                    Fill(new Rect(x, y, 2f, 2f), new Color(1f, .81f, .47f, .18f));
                }
            }
            else
            {
                for (int i = 0; i < 11; i++)
                {
                    float age = Repeat(clock * .15f + i * .17f, 1f);
                    float x = 104f + i * 119f + Mathf.Sin(age * 3f + i) * 17f;
                    float y = 500f - age * 130f;
                    Disc(new Rect(x, y, 22f + age * 25f, 11f + age * 16f),
                        new Color(.65f, .9f, 1f, (1f - age) * .075f), true);
                }
                float pulse = .045f + Mathf.Sin(clock * 1.8f) * .018f;
                Disc(new Rect(68f, 140f, 223f, 330f), new Color(.16f, .88f, 1f, pulse), true);
                for (int i = 0; i < 8; i++)
                {
                    float age = Repeat(clock * .8f + i * .29f, 1f);
                    Fill(new Rect(465f + i % 3 * 95f + age * 18f, 220f + age * 90f,
                        2f, 4f), new Color(1f, .64f, .24f, (1f - age) * .38f));
                }
            }
        }

        private static void DrawArenaFloor(BossKind kind, float left, float width)
        {
            Color baseColor = kind == BossKind.Tank ? Hex(0x283d49) : Hex(0x1d3348);
            Fill(new Rect(left, GroundY, width, Height - GroundY), baseColor);
            Fill(new Rect(left, GroundY, width, 8f), Hex(0xc0d2d0));
            Fill(new Rect(left, GroundY + 8f, width, 4f), Hex(0x556e7a));
            Fill(new Rect(left, GroundY + 16f, width, 3f), Hex(0x162e40));
            for (float x = left + 14f; x < left + width; x += 92f)
            {
                Fill(new Rect(x, GroundY + 26f, 69f, 5f), kind == BossKind.Tank ? Hex(0xad9465) : Hex(0x579098));
                Fill(new Rect(x + 8f, GroundY + 71f, 2f, 54f), new Color(.6f, .75f, .79f, .14f));
                Fill(new Rect(x + 43f, GroundY + 104f, 4f, 4f), Hex(0x718791));
            }
            Fill(new Rect(left, Height - 18f, width, 3f), Hex(0x456075));
        }

        private void DrawArenaGate(BossKind kind, float opening, float clock, float centreX = 130f)
        {
            opening = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(opening));
            Rect frame = new Rect(centreX - 135f, GroundY - 330f, 270f, 330f);
            Color metal = kind == BossKind.Tank ? Hex(0x43596b) : Hex(0x293e5d);
            float bottom = GroundY - opening * 310f;
            // Shutter is behind the alpha-cut arch; the empty passage reveals
            // the destination street as soon as the mechanism opens.
            float doorTop = frame.y + 63f;
            if (bottom > doorTop)
            {
                // Clip the rolling shutter to its passage. The retracted leaf
                // disappears into the lintel instead of floating above the arch.
                Fill(new Rect(centreX - 84f, doorTop, 168f, bottom - doorTop), metal);
                for (int i = 0; i < 9; i++)
                {
                    float y = bottom - 253f + i * 28f;
                    float top = Mathf.Max(doorTop, y), end = Mathf.Min(bottom, y + 5f);
                    if (end > top) Fill(new Rect(centreX - 80f, top, 160f, end - top), Hex(0x192f46));
                    top = Mathf.Max(doorTop, y + 5f); end = Mathf.Min(bottom, y + 7f);
                    if (end > top) Fill(new Rect(centreX - 79f, top, 158f, end - top), Hex(0x657e91));
                }
                Fill(new Rect(centreX - 5f, doorTop, 10f, bottom - doorTop), Hex(0x1c354c));
            }
            GUI.color = Color.white;
            if (art.ArenaGateAtlas != null)
                GUI.DrawTextureWithTexCoords(frame, art.ArenaGateAtlas, art.GetArenaGateUV(kind), true);
            else
            {
                Fill(new Rect(frame.x, frame.y, 38f, frame.height), metal);
                Fill(new Rect(frame.xMax - 38f, frame.y, 38f, frame.height), metal);
                Fill(new Rect(frame.x, frame.y, frame.width, 40f), Hex(0x698292));
            }
            Color lamp = opening > .85f ? Hex(0x9dffd1) : Hex(0xffb34f);
            for (int i = 0; i < 2; i++)
            {
                float x = frame.x + (i == 0 ? 27f : frame.width - 27f);
                Disc(new Rect(x - 14f, frame.y + 35f, 28f, 28f), WithAlpha(lamp, .13f + .04f * Mathf.Sin(clock * 4f)), true);
                Fill(new Rect(x - 5f, frame.y + 44f, 10f, 6f), lamp);
            }
            if (opening > .85f)
                for (int i = 0; i < 3; i++)
                {
                    float x = centreX - 28f + i * 21f;
                    Line(new Vector2(x - 5f, GroundY - 18f), new Vector2(x + 3f, GroundY - 13f), WithAlpha(lamp, .7f), 2f);
                    Line(new Vector2(x + 3f, GroundY - 13f), new Vector2(x - 5f, GroundY - 8f), WithAlpha(lamp, .7f), 2f);
                }
        }
    }
}
