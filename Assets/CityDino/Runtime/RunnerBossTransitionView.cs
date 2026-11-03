using System;
using UnityEngine;

namespace CityDino
{
    /// <summary>Visible travel into themed arenas. All travel and attachment state comes from core.</summary>
    public sealed partial class RunnerWorldView
    {
        private void DrawBossTransition(RunnerSession session)
        {
            BossEncounter boss = session.CurrentBoss;
            BossKind kind = boss == null ? BossKind.Tank : boss.Kind;
            float blend = Mathf.Clamp01(session.TransitionSceneBlend);
            float clock = session.BossElapsed;
            gaps.Clear();
            if (kind == BossKind.Gunship)
            {
                DrawAerialJourney(session, blend, clock);
                if (!session.FlightDeviceCollected && session.ArenaPhase == BossPhase.Intro)
                    DrawFlightPickup(session, clock);
                if (boss != null)
                    DrawBossBody(boss, session.ArenaPhase, clock, session.BossPhaseRemaining);
            }
            else
            {
                Matrix4x4 canvas = GUI.matrix;
                try
                {
                    GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(-Width * blend, 0, 0), Quaternion.identity, Vector3.one);
                    DrawApproachStreet(session, clock);
                    GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(Width * (1f - blend), 0, 0), Quaternion.identity, Vector3.one);
                    DrawArenaBackdrop(kind, clock);
                    DrawArenaGate(kind, blend, clock);
                    if (boss != null) DrawBossBody(boss, session.ArenaPhase, clock, session.BossPhaseRemaining);
                }
                finally { GUI.matrix = canvas; }
            }
            DrawDino(session, session.PlayerAnimationTime);
            GUI.color = Color.white;
        }

        private void DrawApproachStreet(RunnerSession session, float clock)
        {
            Palette palette = ForDistrict(session.CurrentDistrict);
            float scroll = session.Distance + clock * 185f;
            DrawSky(palette, scroll, clock);
            DrawCity(palette, scroll, clock);
            // A clear cinematic corridor connects the old street and arena;
            // frozen gameplay obstacles are not painted beneath the walking hero.
            DrawRoad(palette, scroll, 1f);
        }

        private void DrawAerialJourney(RunnerSession session, float elevation, float clock)
        {
            Palette street = ForDistrict(session.CurrentDistrict);
            DrawSky(street, session.Distance + clock * 90f, clock);
            DrawAerialSky(clock, elevation);
            Matrix4x4 canvas = GUI.matrix;
            try
            {
                // The road and skyline fall away together while the hero's
                // actual altitude rises. At full elevation no street remains.
                GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(-clock * 42f * (1f - elevation), elevation * 680f, 0),
                    Quaternion.identity, Vector3.one);
                DrawCity(street, session.Distance + clock * 185f, clock);
                DrawRoad(street, session.Distance + clock * 185f, 1f);
            }
            finally { GUI.matrix = canvas; }
            if (elevation > .08f) DrawFlightClouds(clock, elevation);
        }

        private void DrawAerialArenaBackdrop(float clock)
        {
            DrawAerialSky(clock, 1f);
            DrawFlightClouds(clock, 1f);
        }

        private void DrawAerialSky(float clock, float strength)
        {
            Color top = Hex(0x315d83), bottom = Hex(0xc8e1ed);
            for (int i = 0; i < 32; i++)
                Fill(new Rect(0, i * Height / 32f, Width, Height / 32f + 1f), WithAlpha(Color.Lerp(top, bottom, i / 31f), strength));
            Disc(new Rect(1065, 110, 71, 71), new Color(.97f, .94f, .77f, .64f * strength));
            // Air streaks have no fixed horizon or painted platform: the entire
            // combat space is sky, with room above and below the flying hero.
            for (int i = 0; i < 14; i++)
            {
                float x = Repeat(i * 173f - clock * (58f + i % 3 * 19f), Width + 150f) - 75f;
                float y = 230f + i * 31f % 410f;
                Line(new Vector2(x, y), new Vector2(x + 45f + i % 4 * 13f, y),
                    new Color(.9f, .97f, 1f, .11f * strength), 1f);
            }
        }

        private void DrawFlightClouds(float clock, float strength)
        {
            for (int i = 0; i < 12; i++)
            {
                float x = Repeat(i * 247f - clock * (15f + i % 4 * 9f), Width + 310f) - 155f;
                float y = 190f + i * 79f % 480f;
                Cloud(x, y, .55f + i % 4 * .17f, new Color(1f, 1f, 1f, strength * (.17f + i % 3 * .07f)));
            }
        }

        private static void DrawArenaGate(BossKind kind, float progress, float clock)
        {
            Color metal = kind == BossKind.Tank ? Hex(0x40564f) : Hex(0x354b60);
            Fill(new Rect(20, GroundY - 290, 22, 290), metal);
            Fill(new Rect(218, GroundY - 290, 22, 290), metal);
            Fill(new Rect(20, GroundY - 290, 220, 25), Hex(0x263f4d));
            float open = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01((progress - .15f) / .65f));
            if (kind == BossKind.Sentinel)
            {
                float bottom = GroundY - open * 255f;
                Fill(new Rect(42, bottom - 255f, 176, 255f), Hex(0x536d7d));
                for (int i = 0; i < 8; i++)
                    Fill(new Rect(46, bottom - 246 + i * 31, 168, 4), Hex(0x2b4355));
            }
            else
            {
                float y = GroundY - 68f - open * 190f;
                Fill(new Rect(28, y, 200, 16), Hex(0xf0c373));
                for (int i = 0; i < 9; i++)
                    Fill(new Rect(32 + i * 23, y, 12, 16), Hex(0x314553));
            }
            Color lamp = open > .7f ? Hex(0x91e6bc) : Hex(0xffbc66);
            Fill(new Rect(24, GroundY - 282, 12, 10), lamp);
            Fill(new Rect(222, GroundY - 282, 12, 10), lamp);
        }

        private void DrawFlightPickup(RunnerSession session, float clock)
        {
            Rect rect = new Rect(session.FlightDeviceX - 43f,
                GroundY - session.FlightDeviceY - 44f, 86f, 44f);
            float pulse = .25f + .1f * Mathf.Sin(clock * 7f);
            Disc(new Rect(rect.x - 12, rect.y - 10, rect.width + 24, rect.height + 20),
                new Color(.52f, .87f, 1f, pulse), true);
            DrawFlightDeviceSprite(rect);
            Line(new Vector2(rect.center.x, rect.y - 19f), new Vector2(rect.center.x, rect.y - 8f),
                Hex(0xe6f9ff), 2f);
            Line(new Vector2(rect.center.x - 5f, rect.y - 13f), new Vector2(rect.center.x, rect.y - 8f),
                Hex(0xe6f9ff), 2f);
            Line(new Vector2(rect.center.x + 5f, rect.y - 13f), new Vector2(rect.center.x, rect.y - 8f),
                Hex(0xe6f9ff), 2f);
        }

        private void DrawAttachedFlightDevice(RunnerSession session, Rect body, float clock)
        {
            Rect pack = new Rect(body.center.x - 69f, body.y + body.height * .36f, 120f, 64f);
            if (session.FacingLeft) pack.x = body.center.x - 51f;
            DrawFlightDeviceSprite(pack, session.FacingLeft);
            float thrust = .55f + .22f * Mathf.Sin(clock * 27f) + Mathf.Clamp01(session.VelocityY / 230f) * .35f;
            for (int i = 0; i < 2; i++)
            {
                float x = pack.x + pack.width * (.28f + i * .43f);
                Fill(new Rect(x, pack.yMax - 5f, 8f, 12f + thrust * 15f), Hex(0x70e7ff));
                Fill(new Rect(x + 2f, pack.yMax - 4f, 4f, 8f + thrust * 12f), Hex(0xf1ffff));
            }
        }

        private void DrawFlightDeviceSprite(Rect rect, bool facingLeft = false)
        {
            GUI.color = Color.white;
            if (art.FlightDevice != null)
            {
                Rect uv = art.FlightDeviceUV;
                if (facingLeft) uv = new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height);
                GUI.DrawTextureWithTexCoords(rect, art.FlightDevice, uv, true);
            }
            else
            {
                // Import-safe fallback: the same silhouette is a visual tool,
                // not an extension of the dinosaur's collision body.
                Fill(new Rect(rect.x + rect.width * .39f, rect.y + rect.height * .15f, rect.width * .22f, rect.height * .7f), Hex(0x526e87));
                Fill(new Rect(rect.x, rect.y + rect.height * .3f, rect.width * .42f, rect.height * .27f), Hex(0x9ce9f1));
                Fill(new Rect(rect.x + rect.width * .58f, rect.y + rect.height * .3f, rect.width * .42f, rect.height * .27f), Hex(0x9ce9f1));
                Fill(new Rect(rect.x + rect.width * .44f, rect.y + rect.height * .24f, rect.width * .12f, rect.height * .25f), Hex(0xffd47c));
            }
        }
    }
}
