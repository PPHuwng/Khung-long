using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityDino
{
    /// <summary>
    /// World art in the same virtual canvas as RunnerSession. Layers and effects
    /// never move collision geometry: every player/hazard position comes from core.
    /// </summary>
    public sealed class RunnerWorldView : IDisposable
    {
        private const float Width = RunnerSession.CanvasWidth;
        private const float Height = RunnerSession.CanvasHeight;
        private const float GroundY = RunnerSession.GroundY;

        private readonly RunnerArt art;
        private readonly Texture2D circle;
        private readonly Texture2D glow;
        private readonly Texture2D spike;
        private readonly Texture2D saw;
        private readonly List<Vector2> gaps = new List<Vector2>(12);
        private GUIStyle signStyle;
        private District shownDistrict = District.Day;
        private Palette previousPalette;
        private Palette nextPalette;
        private float transitionStarted = -10f;
        private bool initialized;

        private struct Palette
        {
            public Color Sky, Horizon, Backdrop, FarCity, Foundation, Road, Accent;
            public float Rain, Night, Skyway, Sunset;
        }

        public RunnerWorldView(RunnerArt art)
        {
            this.art = art ?? throw new ArgumentNullException(nameof(art));
            circle = MakeCircle(false);
            glow = MakeCircle(true);
            spike = MakeSpike();
            saw = MakeSaw();
        }

        public void Draw(RunnerSession session, float ambientTime)
        {
            if (session == null || (Event.current != null && Event.current.type != EventType.Repaint)) return;
            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            try
            {
                Palette palette = CurrentPalette(session.CurrentDistrict, ambientTime);
                bool menu = session.State == RunState.Menu;
                float scroll = menu ? ambientTime * 75f : session.Distance;
                float clock = menu ? ambientTime : session.Elapsed;
                DrawSky(palette, scroll, clock);
                DrawCity(palette, scroll, clock);
                DrawDistrictAtmosphere(session.CurrentDistrict, palette, scroll, clock,
                    session.Speed / RunnerSession.InitialSpeed);
                ReadGaps(session);
                DrawRoad(palette, scroll, session.Speed / RunnerSession.InitialSpeed);
                if (!menu)
                {
                    DrawEntities(session, palette, clock);
                    DrawDino(session, clock);
                }
                DrawWeather(palette, scroll, clock);
                if (!menu) DrawLightning(session);
                if (!menu) DrawDarkness(session);
            }
            finally
            {
                GUI.color = oldColor;
                GUI.matrix = oldMatrix;
            }
        }

        private Palette CurrentPalette(District district, float time)
        {
            if (!initialized)
            {
                shownDistrict = district;
                previousPalette = nextPalette = ForDistrict(district);
                initialized = true;
            }
            if (shownDistrict != district)
            {
                previousPalette = Lerp(previousPalette, nextPalette, Mathf.Clamp01((time - transitionStarted) / 2f));
                nextPalette = ForDistrict(district);
                shownDistrict = district;
                transitionStarted = time;
            }
            return Lerp(previousPalette, nextPalette, Mathf.SmoothStep(0f, 1f, (time - transitionStarted) / 2f));
        }

        private static Palette ForDistrict(District district)
        {
            Palette p = new Palette
            {
                Sky = Hex(0x86c9df), Horizon = Hex(0xf0ead4), Backdrop = Color.white,
                FarCity = Hex(0x8aaabf), Foundation = Hex(0x17283e), Road = Color.white,
                Accent = Hex(0x70e5d1)
            };
            switch (district)
            {
                case District.Rain:
                    p.Sky = Hex(0x5e7897); p.Horizon = Hex(0xb6c8d2); p.Backdrop = Hex(0xa1c4d7);
                    p.FarCity = Hex(0x5c7899); p.Road = Hex(0xb2c7d1); p.Accent = Hex(0x8df5f0); p.Rain = 0.7f;
                    break;
                case District.Sunset:
                    p.Sky = Hex(0x947db4); p.Horizon = Hex(0xffd5a0); p.Backdrop = Hex(0xffd8c4);
                    p.FarCity = Hex(0x9a85a7); p.Road = Hex(0xf7d1b4); p.Accent = Hex(0xffc680); p.Sunset = 1f;
                    break;
                case District.Night:
                    p.Sky = Hex(0x101b37); p.Horizon = Hex(0x3c466b); p.Backdrop = Hex(0x91a5d5);
                    p.FarCity = Hex(0x303c64); p.Foundation = Hex(0x101b2f); p.Road = Hex(0xabbada);
                    p.Accent = Hex(0x89ffe4); p.Night = 1f;
                    break;
                case District.Skyway:
                    p.Sky = Hex(0x7bb4df); p.Horizon = Hex(0xe7edf6); p.Backdrop = Hex(0xc9e8ff);
                    p.FarCity = Hex(0x8daccb); p.Road = Hex(0xe2edf9); p.Accent = Hex(0xffdc8d); p.Skyway = 1f;
                    break;
                case District.Storm:
                    p.Sky = Hex(0x29354f); p.Horizon = Hex(0x74899e); p.Backdrop = Hex(0x9caed2);
                    p.FarCity = Hex(0x4f6382); p.Foundation = Hex(0x142137); p.Road = Hex(0x94aec7);
                    p.Accent = Hex(0x94ecf6); p.Rain = 1f; p.Night = 0.4f;
                    break;
            }
            return p;
        }

        private static Palette Lerp(Palette a, Palette b, float t)
        {
            t = Mathf.Clamp01(t);
            return new Palette
            {
                Sky = Color.Lerp(a.Sky, b.Sky, t), Horizon = Color.Lerp(a.Horizon, b.Horizon, t),
                Backdrop = Color.Lerp(a.Backdrop, b.Backdrop, t), FarCity = Color.Lerp(a.FarCity, b.FarCity, t),
                Foundation = Color.Lerp(a.Foundation, b.Foundation, t), Road = Color.Lerp(a.Road, b.Road, t),
                Accent = Color.Lerp(a.Accent, b.Accent, t), Rain = Mathf.Lerp(a.Rain, b.Rain, t),
                Night = Mathf.Lerp(a.Night, b.Night, t), Skyway = Mathf.Lerp(a.Skyway, b.Skyway, t),
                Sunset = Mathf.Lerp(a.Sunset, b.Sunset, t)
            };
        }

        private void DrawSky(Palette p, float scroll, float time)
        {
            for (int i = 0; i < 40; i++)
            {
                float fraction = i / 39f;
                Fill(new Rect(0f, i * Height / 40f, Width, Height / 40f + 1f), Color.Lerp(p.Sky, p.Horizon, fraction));
            }
            if (p.Night > 0.1f)
            {
                for (int i = 0; i < 46; i++)
                {
                    float x = Repeat(i * 173.71f + 73f, Width);
                    float y = 93f + Repeat(i * 53.13f, 230f);
                    float alpha = p.Night * (0.28f + 0.42f * (0.5f + Mathf.Sin(time * 1.1f + i) * 0.5f));
                    float size = i % 5 == 0 ? 3f : 2f;
                    Fill(new Rect(x, y, size, size), new Color(0.92f, 0.97f, 1f, alpha));
                }
            }
            float celestialY = Mathf.Lerp(152f, 230f, p.Sunset);
            Color sun = Color.Lerp(Hex(0xffe8b0), Hex(0xe6f7ff), p.Night);
            sun.a = Mathf.Lerp(0.94f, 0.2f, p.Rain);
            Disc(new Rect(974f, celestialY - 55f, 225f, 225f), WithAlpha(sun, sun.a * 0.24f), true);
            Disc(new Rect(1040f, celestialY + 5f, 88f, 88f), sun);
            if (p.Night > 0.4f)
                Disc(new Rect(1063f, celestialY - 5f, 78f, 78f), WithAlpha(p.Sky, p.Night));
            for (int i = 0; i < 7; i++)
            {
                float x = Repeat(i * 247f - scroll * 0.035f + time * 2f, 1550f) - 160f;
                float y = 145f + (i % 3) * 52f;
                float size = 0.6f + (i % 3) * 0.18f;
                Cloud(x, y, size, new Color(1f, 1f, 1f, Mathf.Lerp(0.56f, 0.1f, p.Night) * (1f - p.Rain * 0.5f)));
            }
        }

        private void DrawCity(Palette p, float scroll, float time)
        {
            float baseY = Mathf.Lerp(552f, 700f, p.Skyway);
            // Sparse silhouettes stay furthest away; they never contain a painted sky or road.
            for (int i = -1; i < 21; i++)
            {
                int variant = (i + 99) % 9;
                float x = i * 79f - Repeat(scroll * 0.035f, 79f);
                float height = 76f + (variant * 37 % 164);
                Color tint = WithAlpha(p.FarCity, 0.3f);
                Fill(new Rect(x, baseY - height - 40f, 52f + variant % 3 * 11f, height), tint);
                Fill(new Rect(x + 20f, baseY - height - 57f, 15f, 17f), tint);
            }

            if (art.SkylineFar != null)
                DrawSkyline(art.SkylineFar, art.SkylineFarUV, Mathf.Lerp(335f, 295f, p.Skyway), baseY - 8f,
                    scroll * 0.075f, WithAlpha(Color.Lerp(Color.white, p.Backdrop, 0.45f), 0.65f));
            else
                DrawProceduralSkyline(p, scroll, baseY, false);

            // Haze sits BETWEEN layers rather than hiding the player/obstacles.
            for (int i = 0; i < 8; i++)
                Fill(new Rect(0f, baseY - 152f + i * 19f, Width, 20f), WithAlpha(p.Horizon, (i / 7f) * 0.1f));

            if (art.SkylineNear != null)
                DrawSkyline(art.SkylineNear, art.SkylineNearUV, Mathf.Lerp(285f, 240f, p.Skyway), baseY,
                    scroll * 0.17f, WithAlpha(Color.Lerp(Color.white, p.Backdrop, 0.28f), 0.72f));
            else
                DrawProceduralSkyline(p, scroll, baseY, true);

            // Tiny passing light strips reinforce movement without resembling hazards.
            float viaductY = Mathf.Lerp(512f, 667f, p.Skyway);
            Fill(new Rect(0f, viaductY, Width, 3f), WithAlpha(p.FarCity, 0.25f));
            for (int i = 0; i < 11; i++)
            {
                float x = Repeat(i * 153f - scroll * 0.32f, Width + 170f) - 100f;
                Fill(new Rect(x, viaductY - 3f, 42f, 2f), WithAlpha(p.Accent, 0.3f + p.Night * 0.18f));
            }
            if (p.Skyway > 0.1f)
            {
                for (int i = 0; i < 6; i++)
                {
                    float x = Repeat(i * 310f - scroll * 0.13f, 1680f) - 180f;
                    Cloud(x, 615f + i % 2 * 46f, 1.4f, new Color(0.96f, 0.98f, 1f, p.Skyway * 0.65f));
                }
            }
            if (p.Skyway < 0.5f)
            {
                float x = 1050f - Repeat(scroll * 0.3f, 1660f);
                if (x > -140f)
                {
                    Fill(new Rect(x + 10f, 502f, 4f, 58f), WithAlpha(Hex(0x344c60), 0.48f));
                    Fill(new Rect(x, 479f, 107f, 28f), WithAlpha(Hex(0x203648), 0.75f));
                    Fill(new Rect(x + 3f, 482f, 101f, 22f), WithAlpha(p.Accent, 0.9f));
                    Sign(new Rect(x + 4f, 483f, 99f, 20f), "DINO / CITY", Hex(0x203648), 12);
                }
            }
        }

        private static void DrawSkyline(Texture2D texture, Rect source, float height, float bottom, float scroll, Color tint)
        {
            // Preserve the buildings' proportions after trimming transparent sky/feet.
            float aspect = source.width * texture.width / Mathf.Max(1f, source.height * texture.height);
            float tileWidth = Mathf.Max(1f, aspect * height);
            float start = -Repeat(scroll, tileWidth);
            int tile = (int)Math.Floor(scroll / tileWidth);
            GUI.color = tint;
            for (float x = start; x < Width; x += tileWidth, tile++)
            {
                // Alternate reflections join identical edge pixels and avoid a hard panorama seam.
                Rect uv = (tile & 1) == 0 ? source : new Rect(source.x + source.width, source.y, -source.width, source.height);
                GUI.DrawTextureWithTexCoords(new Rect(x, bottom - height, tileWidth + 1f, height), texture, uv, true);
            }
        }

        private static void DrawProceduralSkyline(Palette p, float scroll, float bottom, bool near)
        {
            float spacing = near ? 118f : 86f;
            float parallax = near ? 0.16f : 0.07f;
            for (int i = -1; i < 16; i++)
            {
                int index = i + 60;
                float x = i * spacing - Repeat(scroll * parallax, spacing);
                float height = 78f + index * (near ? 39 : 31) % (near ? 170 : 210);
                Color tint = Color.Lerp(p.FarCity, p.Foundation, near ? 0.28f : 0.04f);
                tint.a = near ? 0.58f : 0.4f;
                float width = spacing - 16f - index % 3 * 9f;
                Fill(new Rect(x, bottom - height, width, height), tint);
                Fill(new Rect(x + width * 0.3f, bottom - height - 10f, width * 0.4f, 10f), tint);
                for (int row = 0; row < 6; row++)
                    for (int column = 0; column < 4; column++)
                        if ((index + row * 3 + column) % 4 != 0 && row * 22f + 20f < height)
                            Fill(new Rect(x + 12f + column * 16f, bottom - height + 17f + row * 22f, 4f, 7f),
                                WithAlpha(p.Night > 0.2f ? p.Accent : p.Horizon, near ? 0.36f : 0.2f));
            }
        }

        private void ReadGaps(RunnerSession session)
        {
            gaps.Clear();
            if (session.State == RunState.Menu) return;
            foreach (WorldEntity entity in session.Entities)
                if (!entity.Removed && entity.Kind == EntityKind.Gap && entity.X < Width && entity.X + entity.Width > 0f)
                    gaps.Add(new Vector2(Mathf.Max(0f, entity.X), Mathf.Min(Width, entity.X + entity.Width)));
            gaps.Sort((a, b) => a.x.CompareTo(b.x));
        }

        private void DrawRoad(Palette p, float scroll, float speedRatio)
        {
            float left = 0f;
            for (int i = 0; i < gaps.Count; i++)
            {
                Vector2 gap = gaps[i];
                if (gap.x > left) DrawRoadSegment(left, gap.x, p, scroll, speedRatio);
                left = Mathf.Max(left, gap.y);
            }
            if (left < Width) DrawRoadSegment(left, Width, p, scroll, speedRatio);
            for (int i = 0; i < gaps.Count; i++)
            {
                Vector2 gap = gaps[i];
                DrawWarningEdge(gap.x - 14f, p.Accent);
                DrawWarningEdge(gap.y, p.Accent);
                Line(new Vector2(gap.x - 2f, GroundY + 6f), new Vector2(gap.x - 2f, GroundY + 46f), WithAlpha(p.Foundation, 0.65f), 3f);
                Line(new Vector2(gap.y + 2f, GroundY + 6f), new Vector2(gap.y + 2f, GroundY + 46f), WithAlpha(p.Foundation, 0.65f), 3f);
            }
        }

        private void DrawRoadSegment(float left, float right, Palette p, float scroll, float speedRatio)
        {
            float width = right - left;
            if (width <= 0f) return;
            GUI.BeginGroup(new Rect(left, GroundY, width, Height - GroundY));
            try
            {
                bool flying = p.Skyway > 0.5f || gaps.Count > 0;
                Texture2D texture = flying ? art.PlatformLong : art.Road;
                if (texture == null) texture = art.PlatformLong;
                float tileWidth = flying ? 360f : 680f;
                float slabHeight = flying ? 105f : 114f;
                Fill(new Rect(0f, 0f, width, flying ? 39f : Height - GroundY), p.Foundation);
                float firstTile = Mathf.Floor((left + scroll) / tileWidth) * tileWidth - scroll - left;
                for (float x = firstTile; x < width; x += tileWidth)
                {
                    if (texture != null)
                    {
                        GUI.color = p.Road;
                        GUI.DrawTexture(new Rect(x, 0f, tileWidth + 1f, slabHeight), texture, ScaleMode.StretchToFill, true);
                    }
                    else
                    {
                        Fill(new Rect(x, 3f, tileWidth + 1f, 62f), Hex(0x40516c));
                        Fill(new Rect(x + 30f, 32f, 70f, 4f), p.Accent);
                    }
                }
                Fill(new Rect(0f, 0f, width, 3f), Color.Lerp(Hex(0xc7e9e1), p.Accent, p.Night * 0.5f));
                Fill(new Rect(0f, 4f, width, 3f), WithAlpha(Hex(0x172c42), 0.6f));
                // Road reflections move faster than the skyline, making acceleration visible.
                for (int i = 0; i < 12; i++)
                {
                    float x = Repeat(i * 129f - scroll * 1.12f, 1500f) - left;
                    float y = 16f + i % 3 * 16f;
                    float streak = Mathf.Lerp(25f, 68f, Mathf.Clamp01(speedRatio - 1f));
                    Fill(new Rect(x, y, streak, i % 3 == 0 ? 2f : 1f), WithAlpha(p.Accent, 0.2f + 0.12f * p.Night + 0.1f * p.Rain));
                }
                if (!flying)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float x = Repeat(i * 191f - scroll * 1.35f, 1480f) - left;
                        Fill(new Rect(x, 112f + i % 2 * 17f, 44f + speedRatio * 12f, 2f), WithAlpha(p.Accent, 0.13f));
                    }
                    if (p.Rain > 0.1f)
                    {
                        for (int i = 0; i < 7; i++)
                        {
                            float x = Repeat(i * 223f - scroll, 1530f) - left;
                            Disc(new Rect(x, 9f + i % 3 * 10f, 80f + i % 3 * 18f, 7f), WithAlpha(p.Accent, p.Rain * 0.2f));
                        }
                    }
                }
            }
            finally { GUI.EndGroup(); }
        }

        private static void DrawWarningEdge(float x, Color color)
        {
            Fill(new Rect(x, GroundY, 14f, 10f), color);
            Fill(new Rect(x + 2f, GroundY + 2f, 3f, 6f), Hex(0x263249));
            Fill(new Rect(x + 9f, GroundY + 2f, 3f, 6f), Hex(0x263249));
        }

        private void DrawEntities(RunnerSession session, Palette p, float time)
        {
            foreach (WorldEntity entity in session.Entities)
            {
                if (entity.Removed || entity.Kind == EntityKind.Gap || entity.X > Width + 100f || entity.X + entity.Width < -100f) continue;
                float bottom = GroundY - entity.Y;
                Rect rect = new Rect(entity.X, bottom - entity.Height, entity.Width, entity.Height);
                Texture2D texture = null;
                bool drawn = false;
                switch (entity.Kind)
                {
                    case EntityKind.House:
                        texture = entity.Variant % 3 == 0 ? art.House1 : entity.Variant % 3 == 1 ? art.House2 : art.House3;
                        GroundShadow(entity.X, entity.Width);
                        break;
                    case EntityKind.Hydrant:
                        texture = art.Hydrant;
                        GroundShadow(entity.X, entity.Width);
                        break;
                    case EntityKind.Plane:
                        texture = art.Plane;
                        Line(new Vector2(rect.xMax + 6f, rect.center.y), new Vector2(rect.xMax + 34f, rect.center.y), WithAlpha(p.Horizon, 0.65f), 3f);
                        break;
                    case EntityKind.Spike:
                        GroundShadow(entity.X, entity.Width);
                        GUI.color = Color.white;
                        if (!DrawAtlas(rect, 0)) GUI.DrawTexture(rect, spike, ScaleMode.StretchToFill, true);
                        drawn = true;
                        break;
                    case EntityKind.Saw:
                        Disc(new Rect(rect.x - 9f, rect.y - 9f, rect.width + 18f, rect.height + 18f), new Color(1f, 0.38f, 0.24f, 0.22f), true);
                        Matrix4x4 before = GUI.matrix;
                        try
                        {
                            RotateLocal(entity.Rotation, rect.center);
                            GUI.color = Color.white;
                            if (!DrawAtlas(rect, 1)) GUI.DrawTexture(rect, saw, ScaleMode.StretchToFill, true);
                        }
                        finally { GUI.matrix = before; }
                        drawn = true;
                        break;
                    case EntityKind.Barrier:
                        GroundShadow(entity.X, entity.Width);
                        GUI.color = Color.white;
                        if (!DrawAtlas(rect, entity.Variant % 2 == 0 ? 2 : 3)) DrawBarrier(rect, entity.Variant, p, time);
                        drawn = true;
                        break;
                    case EntityKind.Star:
                        texture = art.Star;
                        rect.y += Mathf.Sin(time * 3f + entity.Id * 0.9f) * 3f;
                        Disc(new Rect(rect.center.x - rect.width, rect.center.y - rect.height, rect.width * 2f, rect.height * 2f), new Color(1f, 0.8f, 0.27f, 0.3f), true);
                        if (entity.Id % 3 == 0)
                        {
                            float spark = 0.5f + Mathf.Sin(time * 4f + entity.Id) * 0.5f;
                            Fill(new Rect(rect.xMax + 3f, rect.y + 3f, 3f, 3f), new Color(1f, 0.95f, 0.7f, spark));
                        }
                        break;
                    case EntityKind.Heart:
                        texture = art.Heart;
                        rect.y += Mathf.Sin(time * 2.6f + entity.Id) * 4f;
                        float pulse = 1.8f + Mathf.Sin(time * 4f) * 0.15f;
                        Disc(new Rect(rect.center.x - rect.width * pulse / 2f, rect.center.y - rect.height * pulse / 2f, rect.width * pulse, rect.height * pulse), new Color(1f, 0.33f, 0.46f, 0.32f), true);
                        break;
                }
                if (drawn) continue;
                if (texture != null)
                {
                    GUI.color = Color.Lerp(Color.white, p.Backdrop, 0.1f * p.Night);
                    GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
                }
                else Fill(rect, entity.Kind == EntityKind.Heart ? Hex(0xff657d) : entity.Kind == EntityKind.Star ? Hex(0xffd36f) : Hex(0x53758a));
            }
            GUI.color = Color.white;
        }

        private bool DrawAtlas(Rect target, int cell)
        {
            if (art.HazardAtlas == null) return false;
            // The catalog records tight UV bounds after the generated atlas is inspected.
            // Drawing those bounds keeps visible triangle/circle edges aligned to collisions.
            Rect uv = art.GetHazardUV(cell);
            GUI.DrawTextureWithTexCoords(target, art.HazardAtlas, uv, true);
            return true;
        }

        private void DrawBarrier(Rect rect, int variant, Palette p, float time)
        {
            Fill(rect, Hex(0x17263d));
            if (variant % 2 == 0)
            {
                Fill(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f), Hex(0xf5b851));
                GUI.BeginGroup(new Rect(rect.x + 4f, rect.y + 4f, rect.width - 8f, rect.height - 8f));
                try
                {
                    for (float x = -rect.height; x < rect.width; x += 24f)
                        Line(new Vector2(x, rect.height), new Vector2(x + rect.height, 0f), Hex(0x2b3545), 9f);
                }
                finally { GUI.EndGroup(); }
            }
            else
            {
                Fill(new Rect(rect.x + 5f, rect.y + 5f, rect.width - 10f, rect.height - 10f), Hex(0x25526a));
                Color light = Color.Lerp(Hex(0x7bf3ee), p.Accent, 0.4f);
                Fill(new Rect(rect.x + 8f, rect.y + 8f, 4f, rect.height - 16f), light);
                Fill(new Rect(rect.xMax - 12f, rect.y + 8f, 4f, rect.height - 16f), light);
                float pulse = 0.4f + 0.25f * Mathf.Sin(time * 4f);
                for (float y = rect.y + 12f; y < rect.yMax - 8f; y += 12f)
                    Fill(new Rect(rect.x + 16f, y, rect.width - 32f, 2f), WithAlpha(light, pulse));
            }
        }

        private void GroundShadow(float x, float width)
        {
            Disc(new Rect(x - 7f, GroundY - 4f, width + 14f, 10f), new Color(0.03f, 0.09f, 0.15f, 0.2f), true);
        }

        private void DrawDino(RunnerSession session, float time)
        {
            float x = session.PlayerX;
            float y = session.PlayerY;
            float shadowScale = Mathf.Clamp(1f - y / 450f, 0.5f, 1f);
            bool overGap = false;
            for (int i = 0; i < gaps.Count; i++) if (x >= gaps[i].x && x <= gaps[i].y) overGap = true;
            if (!overGap && y > -30f)
                Disc(new Rect(x - 39f * shadowScale, GroundY - 6f, 78f * shadowScale, 13f * shadowScale), new Color(0.03f, 0.09f, 0.16f, 0.24f), true);
            bool running = session.State == RunState.Playing;
            bool facingLeft = session.FacingLeft;
            float bob = RunnerGeometry.GetPlayerBob(time, y, session.IsDucking, session.State);
            float width = session.IsDucking ? RunnerGeometry.PlayerDuckWidth : RunnerGeometry.PlayerStandingWidth;
            float height = session.IsDucking ? RunnerGeometry.PlayerDuckHeight : RunnerGeometry.PlayerStandingHeight;
            Rect rect = new Rect(x - width / 2f, GroundY - y - height + bob, width, height);
            if (session.InvincibleRemaining > 0f)
            {
                Disc(new Rect(rect.x - 14f, rect.y - 12f, rect.width + 28f, rect.height + 26f), new Color(0.65f, 1f, 0.87f, 0.3f), true);
                GUI.color = Mathf.FloorToInt(time * 12f) % 2 == 0 ? new Color(1f, 1f, 1f, 0.42f) : Color.white;
            }
            else GUI.color = Color.white;
            int frame = RunnerGeometry.GetPlayerFrame(session);
            Texture2D texture = null;
            Rect uv = new Rect(0f, 0f, 1f, 1f);
            if (frame >= 5 && art.DinoActionsAtlas != null)
            {
                texture = art.DinoActionsAtlas;
                uv = art.GetDinoActionUV(frame);
            }
            else if (art.DinoFrames != null && art.DinoFrames.Length > 0)
            {
                int runningFrame = frame < 5 ? frame : RunnerGeometry.GetPlayerFrame(time, session.Speed, y, session.State);
                texture = art.DinoFrames[runningFrame % art.DinoFrames.Length];
            }
            if (facingLeft) uv = new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height);
            if (texture != null)
                GUI.DrawTextureWithTexCoords(rect, texture, uv, true);
            else Fill(rect, Hex(0x7fbd72));
            if (running && y <= 1f && session.Elapsed > 0.5f)
            {
                for (int i = 0; i < 4; i++)
                {
                    float phase = Repeat(time * 4f + i * 0.25f, 1f);
                    float particleX = facingLeft ? x + 42f + phase * 58f : x - 42f - phase * 58f;
                    Fill(new Rect(particleX, GroundY - 2f - phase * 9f, 5f - phase * 3f, 3f), new Color(0.89f, 0.94f, 0.91f, (1f - phase) * 0.55f));
                }
            }
            GUI.color = Color.white;
        }

        private void DrawDistrictAtmosphere(District district, Palette p, float scroll, float time, float speedRatio)
        {
            // These effects sit behind the road/player/hazards. Stronger late-run
            // wind reinforces speed without shifting any collision geometry.
            float rush = 1f + Mathf.Sqrt(Mathf.Max(0f, speedRatio - 1f)) * 0.45f;
            switch (district)
            {
                case District.Day:
                    for (int i = 0; i < 14; i++)
                    {
                        float x = Repeat(i * 137.3f - scroll * 0.35f - time * 54f * rush, Width + 90f) - 45f;
                        float y = 275f + Repeat(i * 49.7f, 240f) + Mathf.Sin(time * 1.7f + i) * 11f;
                        Color leaf = WithAlpha(i % 3 == 0 ? Hex(0xb98e54) : Hex(0x9fc47b), 0.42f);
                        Line(new Vector2(x, y), new Vector2(x + 7f, y - 3f), leaf, 3f);
                        if (i % 3 == 0)
                            Line(new Vector2(x + 17f, y + 5f), new Vector2(x + 52f * rush, y + 3f), WithAlpha(p.Horizon, 0.18f), 1f);
                    }
                    break;
                case District.Sunset:
                    // Heat is suggested by wavering highlights in the backdrop,
                    // never by warping the foreground or moving a hitbox.
                    for (int i = 0; i < 7; i++)
                    {
                        float y = 431f + i * 14f;
                        float offset = Mathf.Sin(time * 1.2f + i * 0.8f) * 24f;
                        Fill(new Rect(offset - 30f, y, Width + 60f, 2f), WithAlpha(Hex(0xffd7aa), 0.045f + i * 0.006f));
                    }
                    for (int i = 0; i < 18; i++)
                    {
                        float phase = Repeat(time * (0.11f + i % 4 * 0.02f) + i * 0.171f, 1f);
                        float x = Repeat(i * 91.7f - scroll * 0.12f - phase * 95f, Width);
                        float y = 539f - phase * 270f;
                        float alpha = Mathf.Sin(phase * 3.1415927f) * 0.38f;
                        Fill(new Rect(x, y, 2f + i % 2, 3f), WithAlpha(Hex(0xffd28b), alpha));
                    }
                    break;
                case District.Night:
                    for (int i = 0; i < 7; i++)
                    {
                        float x = Repeat(i * 223f - scroll * 0.17f, Width + 210f) - 100f;
                        float y = 259f + i % 3 * 67f;
                        float pulse = 0.5f + Mathf.Sin(time * 1.25f + i * 1.9f) * 0.18f;
                        Color neon = i % 2 == 0 ? Hex(0x73f8db) : Hex(0xb4a3ff);
                        Disc(new Rect(x - 15f, y - 15f, 120f, 51f), WithAlpha(neon, pulse * 0.14f), true);
                        Fill(new Rect(x, y, 78f, 2f), WithAlpha(neon, pulse * 0.5f));
                    }
                    break;
                case District.Skyway:
                    for (int i = 0; i < 11; i++)
                    {
                        float x = Repeat(i * 181.3f - time * 150f * rush - scroll * 0.08f, Width + 300f) - 150f;
                        float y = 163f + Repeat(i * 57f, 315f);
                        float span = (70f + i % 3 * 24f) * Mathf.Min(rush, 2.5f);
                        Line(new Vector2(x, y), new Vector2(x + span, y - 5f), WithAlpha(Hex(0xf4fcff), 0.22f), 2f);
                        Line(new Vector2(x + 15f, y + 9f), new Vector2(x + span * 0.65f, y + 5f), WithAlpha(p.Horizon, 0.14f), 1f);
                    }
                    break;
                case District.Storm:
                    for (int i = 0; i < 10; i++)
                    {
                        float x = Repeat(i * 173f - time * 190f * rush, Width + 180f) - 90f;
                        float y = 180f + Repeat(i * 43f, 290f);
                        Line(new Vector2(x, y), new Vector2(x + 64f, y - 14f), WithAlpha(p.Horizon, 0.13f), 2f);
                    }
                    break;
            }
        }

        private void DrawWeather(Palette p, float scroll, float time)
        {
            if (p.Sunset > 0.1f)
                Disc(new Rect(855f, 170f, 550f, 550f), new Color(1f, 0.75f, 0.43f, 0.12f * p.Sunset), true);
            if (p.Rain > 0.05f)
            {
                int count = Mathf.RoundToInt(67f * p.Rain);
                for (int i = 0; i < count; i++)
                {
                    float x = Repeat(i * 87.7f - time * 110f, Width + 80f) - 40f;
                    float y = Repeat(i * 63.3f + time * (440f + i % 4 * 23f), 600f) - 30f;
                    float length = 14f + i % 4 * 5f;
                    Line(new Vector2(x, y), new Vector2(x - 6f, y + length), new Color(0.83f, 0.94f, 1f, 0.23f + i % 3 * 0.05f), i % 4 == 0 ? 2f : 1f);
                }
                for (int i = 0; i < 12; i++)
                {
                    float x = Repeat(i * 111f - scroll, Width);
                    float phase = Repeat(time * 1.7f + i * 0.17f, 1f);
                    if (phase >= 0.3f) continue;
                    bool aboveGap = false;
                    for (int j = 0; j < gaps.Count; j++) if (x >= gaps[j].x && x <= gaps[j].y) aboveGap = true;
                    if (!aboveGap)
                    {
                        float spread = phase * 20f;
                        Line(new Vector2(x - spread, GroundY - 1f), new Vector2(x - spread - 3f, GroundY - 5f), WithAlpha(p.Horizon, (0.3f - phase) * p.Rain), 1.5f);
                        Line(new Vector2(x + spread, GroundY - 1f), new Vector2(x + spread + 3f, GroundY - 5f), WithAlpha(p.Horizon, (0.3f - phase) * p.Rain), 1.5f);
                    }
                }
            }
            if (p.Skyway > 0.1f)
            {
                for (int i = 0; i < 8; i++)
                {
                    float x = Repeat(i * 191f - time * 155f, Width + 220f) - 110f;
                    float y = 205f + i % 3 * 53f;
                    Line(new Vector2(x, y), new Vector2(x + 64f, y - 7f), WithAlpha(p.Horizon, p.Skyway * 0.24f), 2f);
                }
            }
        }

        private static void DrawLightning(RunnerSession session)
        {
            var lightning = RunnerWeather.GetLightning(session.CurrentDistrict, session.DistrictElapsed);
            float strength = Mathf.Clamp01(lightning.Amount);
            if (strength <= 0f) return;
            // A single soft pulse per strike: the foreground remains readable,
            // and the later blackout pass can still cover this completely.
            Fill(new Rect(0f, 0f, Width, Height), new Color(0.75f, 0.87f, 1f, strength * 0.23f));
            float boltX = Mathf.Clamp(lightning.BoltX, 100f, Width - 100f);
            Vector2 from = new Vector2(boltX, 124f);
            Color halo = new Color(0.56f, 0.77f, 1f, strength * 0.35f);
            Color core = new Color(0.92f, 0.97f, 1f, strength * 0.9f);
            for (int i = 1; i <= 6; i++)
            {
                float zigzag = ((lightning.StrikeId + i * 17) % 5 - 2) * 15f;
                Vector2 to = new Vector2(boltX + zigzag, 124f + i * 39f);
                Line(from, to, halo, 9f);
                Line(from, to, core, 2.5f);
                if (i == 3 || i == 5)
                {
                    float side = i == 3 ? -1f : 1f;
                    Vector2 branch = new Vector2(to.x + side * 38f, to.y + 20f);
                    Vector2 tip = new Vector2(branch.x - side * 11f, branch.y + 29f);
                    Line(to, branch, halo, 5f);
                    Line(to, branch, core, 1.5f);
                    Line(branch, tip, core, 1.5f);
                }
                from = to;
            }
        }

        private void DrawDarkness(RunnerSession session)
        {
            float amount = Mathf.Clamp01(session.DarknessAmount);
            if (amount > 0f)
            {
                // The blackout is intentionally fully opaque at its peak. Draw
                // nothing from the world above it; HUD/controls are a later pass.
                Fill(new Rect(0f, 0f, Width, Height), new Color(0f, 0f, 0f, amount));
                return;
            }
            if (session.State != RunState.Playing) return;
            if (session.BlackoutWarning)
            {
                Fill(new Rect(399f, 130f, 482f, 39f), new Color(0.06f, 0.10f, 0.16f, 0.93f));
                Sign(new Rect(409f, 133f, 462f, 31f), "SẮP TỐI NGẮN · GHI NHỚ VỊ TRÍ BẪY", Hex(0xffcf8b), 15);
            }
        }

        private void Sign(Rect rect, string text, Color color, int size)
        {
            if (signStyle == null) signStyle = new GUIStyle { font = art.Font, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
            signStyle.fontSize = size;
            signStyle.normal.textColor = color;
            GUI.color = Color.white;
            GUI.Label(rect, text, signStyle);
        }

        private void Cloud(float x, float y, float scale, Color color)
        {
            Disc(new Rect(x, y + 17f * scale, 106f * scale, 34f * scale), color);
            Disc(new Rect(x + 20f * scale, y, 55f * scale, 55f * scale), color);
            Disc(new Rect(x + 60f * scale, y + 10f * scale, 43f * scale, 43f * scale), color);
        }

        private void Disc(Rect rect, Color color, bool soft = false)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, soft ? glow : circle, ScaleMode.StretchToFill, true);
        }

        private static void Fill(Rect rect, Color color)
        {
            GUI.color = color;
            GUI.DrawTexture(rect, Texture2D.whiteTexture);
        }

        private static void Line(Vector2 from, Vector2 to, Color color, float thickness)
        {
            Vector2 delta = to - from;
            if (delta.sqrMagnitude < 0.001f) return;
            Matrix4x4 previous = GUI.matrix;
            try
            {
                RotateLocal(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
                Fill(new Rect(from.x, from.y - thickness * 0.5f, delta.magnitude, thickness), color);
            }
            finally { GUI.matrix = previous; }
        }

        private static void RotateLocal(float angle, Vector2 pivot)
        {
            // Compose in virtual coordinates BEFORE the canvas-to-screen
            // transform. GUIUtility.RotateAroundPivot prepends a screen-space
            // rotation, which displaces the saw when the Game view is scaled.
            GUI.matrix = GUI.matrix
                * Matrix4x4.TRS(new Vector3(pivot.x, pivot.y, 0f), Quaternion.Euler(0f, 0f, angle), Vector3.one)
                * Matrix4x4.TRS(new Vector3(-pivot.x, -pivot.y, 0f), Quaternion.identity, Vector3.one);
        }

        private static Color Hex(uint rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        private static Color WithAlpha(Color color, float alpha) { color.a = alpha; return color; }
        private static float Repeat(float value, float length) { return value - Mathf.Floor(value / length) * length; }

        private static Texture2D MakeCircle(bool soft)
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = soft ? "CityDino soft glow" : "CityDino disc";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = (x + 0.5f - size / 2f) / (size / 2f);
                    float dy = (y + 0.5f - size / 2f) / (size / 2f);
                    float distance = Mathf.Sqrt(dx * dx + dy * dy);
                    float alpha = soft ? Mathf.Pow(Mathf.Clamp01(1f - distance), 1.8f) : Mathf.Clamp01((1f - distance) * 22f);
                    pixels[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeSpike()
        {
            const int size = 64;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "CityDino spike fallback";
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float py = (y + 0.5f) / size;
                    float px = Mathf.Abs((x + 0.5f) / size - 0.5f);
                    float half = (1f - py) * 0.5f;
                    Color color = Color.clear;
                    if (px <= half)
                    {
                        bool edge = half - px < 0.055f || py < 0.065f;
                        color = edge ? Hex(0x203249) : Color.Lerp(Hex(0xff7359), Hex(0xffe5a1), py);
                    }
                    pixels[y * size + x] = color;
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        private static Texture2D MakeSaw()
        {
            const int size = 128;
            Texture2D texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.name = "CityDino saw fallback";
            texture.filterMode = FilterMode.Bilinear;
            texture.wrapMode = TextureWrapMode.Clamp;
            Color[] pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    double dx = (x + 0.5 - size / 2.0) / (size / 2.0);
                    double dy = (y + 0.5 - size / 2.0) / (size / 2.0);
                    double radius = Math.Sqrt(dx * dx + dy * dy);
                    double edge = 0.83 + 0.14 * Math.Cos(Math.Atan2(dy, dx) * 12);
                    Color color = Color.clear;
                    if (radius <= edge)
                    {
                        color = edge - radius < 0.055 ? Hex(0x26354b) : Hex(0xc4dae0);
                        if (radius < 0.63) color = Hex(0x738c9e);
                        if (radius < 0.47) color = Hex(0xffa85f);
                        if (radius < 0.27) color = Hex(0x26354b);
                        if (radius < 0.13) color = Hex(0x8df3ec);
                    }
                    pixels[y * size + x] = color;
                }
            texture.SetPixels(pixels);
            texture.Apply(false, true);
            return texture;
        }

        public void Dispose()
        {
            if (circle != null) UnityEngine.Object.Destroy(circle);
            if (glow != null) UnityEngine.Object.Destroy(glow);
            if (spike != null) UnityEngine.Object.Destroy(spike);
            if (saw != null) UnityEngine.Object.Destroy(saw);
        }
    }
}
