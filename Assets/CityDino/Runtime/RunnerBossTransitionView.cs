using System;
using UnityEngine;

namespace CityDino
{
    /// <summary>Visible travel into themed arenas. All travel and attachment state comes from core.</summary>
    public sealed partial class RunnerWorldView
    {
        private void DrawBossTransition(RunnerSession session)
        {
            if (session.ArenaPhase == BossPhase.Victory)
            {
                DrawBossForwardDeparture(session);
                return;
            }
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

        private void DrawBossForwardDeparture(RunnerSession session)
        {
            BossEncounter boss = session.CurrentBoss;
            BossKind kind = boss == null ? BossKind.Tank : boss.Kind;
            float clock = session.BossElapsed;
            float camera = session.BossExitCameraX;
            float elevation = session.TransitionElevation;
            float progress = session.ExitTravelProgress;
            Matrix4x4 canvas = GUI.matrix;
            gaps.Clear();
            try
            {
                if (kind == BossKind.Gunship) DrawAerialArenaBackdrop(clock);
                GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(-camera, kind == BossKind.Gunship ? elevation * 680f : 0f, 0f),
                    Quaternion.identity, Vector3.one);
                if (kind != BossKind.Gunship)
                {
                    DrawArenaBackdrop(kind, clock, false);
                    DrawArenaFloor(kind, Width, RunnerSession.BossExitStreetX - Width);
                    if (art.ArenaBackdropAtlas != null)
                    {
                        Rect endUv = art.GetArenaBackdropUV(kind);
                        endUv.x += endUv.width * .8f;
                        endUv.width *= .2f;
                        GUI.color = Color.white;
                        GUI.DrawTextureWithTexCoords(new Rect(Width, 0f, RunnerSession.BossExitStreetX - Width, GroundY),
                            art.ArenaBackdropAtlas, endUv, true);
                    }
                    else Fill(new Rect(Width, 0f, RunnerSession.BossExitStreetX - Width, GroundY),
                        kind == BossKind.Tank ? Hex(0x6e756c) : Hex(0x2e4661));
                }
                else
                {
                    // The landing apron rises from below as the rider descends,
                    // reaching the street before the hero crosses the exit gate.
                    DrawArenaFloor(BossKind.Sentinel, 1110f, RunnerSession.BossExitStreetX - 1110f);
                    Fill(new Rect(1110f, GroundY - 34f, RunnerSession.BossExitStreetX - 1110f, 28f), Hex(0x426c86));
                }
                DrawArenaGate(kind, Mathf.Clamp01((progress - .08f) / .46f), clock, RunnerSession.BossExitGateX);
                GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(RunnerSession.BossExitStreetX - camera,
                    kind == BossKind.Gunship ? elevation * 680f : 0f, 0f), Quaternion.identity, Vector3.one);
                DrawDestinationStreet(session);
                GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(-camera, 0f, 0f), Quaternion.identity, Vector3.one);
                if (boss != null) DrawBossBody(boss, session.ArenaPhase, clock, session.BossPhaseRemaining);
            }
            finally { GUI.matrix = canvas; }
            DrawDino(session, session.PlayerAnimationTime);
            GUI.color = Color.white;
        }

        private void DrawDestinationStreet(RunnerSession session)
        {
            // Match the exact frozen run clock and scroll at the final frame;
            // switching back to normal play must not rewind the city/road.
            Palette palette = ForDistrict(session.CurrentDistrict);
            // Settle any palette blend held over from before the boss so the
            // normal renderer uses this exact destination on the next frame.
            initialized = true;
            shownDistrict = session.CurrentDistrict;
            previousPalette = nextPalette = palette;
            DrawSky(palette, session.Distance, session.Elapsed);
            DrawCity(palette, session.Distance, session.Elapsed);
            DrawDistrictAtmosphere(session.CurrentDistrict, palette, session.Distance, session.Elapsed,
                session.Speed / RunnerSession.InitialSpeed);
            DrawRoad(palette, session.Distance, session.Speed / RunnerSession.InitialSpeed);
            DrawWeather(palette, session.Distance, session.Elapsed);
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

        private void DrawFlightPickup(RunnerSession session, float clock)
        {
            Rect rect = new Rect(session.FlightDeviceX - 43f,
                GroundY - session.FlightDeviceY - 24f, 86f, 24f);
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

        private static Rect FlightBoardRect(RunnerSession session, Rect body)
        {
            Rect target = new Rect(body.center.x - 75f, body.yMax - 2f, 150f, 35f);
            if (session.ArenaPhase != BossPhase.Intro) return target;
            float progress = Mathf.Clamp01((session.BossTransitionProgress * RunnerSession.BossIntroDuration - 1.4f) / .5f);
            progress = progress * progress * (3f - 2f * progress);
            Rect pickup = new Rect(session.FlightDeviceX - 43f,
                GroundY - session.FlightDeviceY - 24f, 86f, 24f);
            return new Rect(Mathf.Lerp(pickup.x, target.x, progress), Mathf.Lerp(pickup.y, target.y, progress),
                Mathf.Lerp(pickup.width, target.width, progress), Mathf.Lerp(pickup.height, target.height, progress));
        }

        private void DrawAttachedFlightDevice(RunnerSession session, Rect body, float clock)
        {
            // Only the exhaust is behind the rider. The deck is below the feet
            // and drawn afterwards, so the dinosaur cannot hide the equipment.
            Rect board = FlightBoardRect(session, body);
            float thrust = .55f + .13f * Mathf.Sin(clock * 27f) + Mathf.Clamp01(session.VelocityY / 230f) * .4f;
            for (int i = 0; i < 2; i++)
            {
                float fraction = .30f + i * .42f;
                if (session.FacingLeft) fraction = 1f - fraction;
                float x = board.x + board.width * fraction;
                float y = board.yMax - 3f;
                float length = 13f + thrust * 19f;
                Disc(new Rect(x - 10f, y - 2f, 20f, length + 9f), new Color(.25f, .8f, 1f, .14f), true);
                Line(new Vector2(x, y), new Vector2(x + Mathf.Sin(clock * 21f + i) * 2f, y + length),
                    Hex(0x5edfff), 6f);
                Line(new Vector2(x, y), new Vector2(x, y + length * .7f), Hex(0xe9ffff), 2.5f);
                for (int particle = 0; particle < 3; particle++)
                {
                    float phase = Repeat(clock * 2.4f + particle / 3f + i * .17f, 1f);
                    Fill(new Rect(x - 2f + Mathf.Sin(phase * 8f + i) * phase * 6f,
                        y + length + phase * 18f, 3f, 3f), new Color(.58f, .92f, 1f, (1f - phase) * .5f));
                }
            }
        }

        private void DrawFlightDeviceFront(RunnerSession session, Rect body)
        {
            DrawFlightDeviceSprite(FlightBoardRect(session, body), session.FacingLeft);
        }

        private void DrawFlightDeviceSprite(Rect rect, bool facingLeft = false)
        {
            GUI.color = Color.white;
            if (art.Hoverboard != null)
            {
                Rect uv = art.HoverboardUV;
                if (facingLeft) uv = new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height);
                GUI.DrawTextureWithTexCoords(rect, art.Hoverboard, uv, true);
            }
            else
            {
                // Import-safe board fallback; the equipment does not extend
                // the dinosaur's physical collision silhouette.
                Fill(new Rect(rect.x, rect.y + rect.height * .18f, rect.width, rect.height * .28f), Hex(0xece4cc));
                Fill(new Rect(rect.x + 2f, rect.y + rect.height * .46f, rect.width - 4f, rect.height * .13f), Hex(0xc5a66a));
                for (int i = 0; i < 2; i++)
                {
                    float x = rect.x + rect.width * (.22f + i * .42f);
                    Disc(new Rect(x, rect.y + rect.height * .52f, rect.width * .17f, rect.height * .46f), Hex(0x337a89));
                }
            }
        }
    }
}
