using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityDino
{
    /// <summary>
    /// Draws the simulation in a fixed 1280 x 720 presentation space. Physics and
    /// collision decisions belong to RunnerSession, never to the scrolling art.
    /// </summary>
    public sealed class RunnerWorldView : IDisposable
    {
        private const float Width = RunnerSession.CanvasWidth;
        private const float Height = RunnerSession.CanvasHeight;
        private const float GroundY = RunnerSession.GroundY;
        private const float DinoX = RunnerSession.PlayerX;

        private readonly RunnerArt art;
        private readonly Texture2D circle;
        private readonly Texture2D glow;
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
        }

        public void Draw(RunnerSession session, float ambientTime)
        {
            if (session == null || (Event.current != null && Event.current.type != EventType.Repaint))
                return;

            Color oldColor = GUI.color;
            Matrix4x4 oldMatrix = GUI.matrix;
            try
            {
                Palette palette = CurrentPalette(session.CurrentDistrict, ambientTime);
                float scroll = session.State == RunState.Menu ? ambientTime * 24f : session.Distance;
                DrawSky(palette, scroll, ambientTime);
                DrawCity(palette, scroll, ambientTime);
                ReadGaps(session);
                DrawRoad(palette, scroll);
                DrawEntities(session, palette, ambientTime);
                DrawDino(session, ambientTime);
                DrawWeather(palette, scroll, ambientTime);
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
                Sky = Hex(0x91d8e2), Horizon = Hex(0xe9f5dc), Backdrop = Color.white,
                FarCity = Hex(0x91bfc8), Foundation = Hex(0x192d40), Road = Color.white,
                Accent = Hex(0xf8cc66)
            };
            switch (district)
            {
                case District.Rain:
                    p.Sky = Hex(0x738eaa); p.Horizon = Hex(0xc0d0d4); p.Backdrop = Hex(0xa1b9cb);
                    p.FarCity = Hex(0x627d93); p.Road = Hex(0xb7c7d0); p.Accent = Hex(0x9eeced); p.Rain = 0.7f;
                    break;
                case District.Sunset:
                    p.Sky = Hex(0xdd8e9d); p.Horizon = Hex(0xffdf9c); p.Backdrop = Hex(0xffd0a7);
                    p.FarCity = Hex(0xab869e); p.Road = Hex(0xf4c6ad); p.Accent = Hex(0xffd372); p.Sunset = 1f;
                    break;
                case District.Night:
                    p.Sky = Hex(0x152542); p.Horizon = Hex(0x49547b); p.Backdrop = Hex(0x7b89b9);
                    p.FarCity = Hex(0x303c60); p.Foundation = Hex(0x111e32); p.Road = Hex(0x9ca8c6);
                    p.Accent = Hex(0x9fefd4); p.Night = 1f;
                    break;
                case District.Skyway:
                    p.Sky = Hex(0x8bbbe5); p.Horizon = Hex(0xe7f1f7); p.Backdrop = Hex(0xbddbed);
                    p.FarCity = Hex(0x92a9cd); p.Road = Hex(0xd8e8f4); p.Accent = Hex(0xffd26f); p.Skyway = 1f;
                    break;
                case District.Storm:
                    p.Sky = Hex(0x35445f); p.Horizon = Hex(0x8796aa); p.Backdrop = Hex(0x8b9cb6);
                    p.FarCity = Hex(0x536480); p.Foundation = Hex(0x152338); p.Road = Hex(0x91a5b9);
                    p.Accent = Hex(0xbee7f0); p.Rain = 1f; p.Night = 0.35f;
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
            for (int i = 0; i < 32; i++)
            {
                float fraction = i / 31f;
                Fill(new Rect(0f, i * Height / 32f, Width, Height / 32f + 1f), Color.Lerp(p.Sky, p.Horizon, fraction));
            }

            if (p.Night > 0.1f)
            {
                for (int i = 0; i < 48; i++)
                {
                    float x = Repeat(i * 173.71f + 73f, Width);
                    float y = 82f + Repeat(i * 53.13f, 235f);
                    float alpha = p.Night * (0.25f + 0.45f * (0.5f + Mathf.Sin(time * 1.1f + i) * 0.5f));
                    float size = i % 5 == 0 ? 3f : 2f;
                    Fill(new Rect(x, y, size, size), new Color(0.92f, 0.97f, 1f, alpha));
                }
            }

            float celestialY = Mathf.Lerp(165f, 235f, p.Sunset);
            Color sun = Color.Lerp(Hex(0xffe6a7), Hex(0xe7f5ff), p.Night);
            sun.a = Mathf.Lerp(0.95f, 0.2f, p.Rain);
            Disc(new Rect(1025f, celestialY - 32f, 144f, 144f), WithAlpha(sun, sun.a * 0.28f), true);
            Disc(new Rect(1060f, celestialY, 72f, 72f), sun);
            if (p.Night > 0.4f)
                Disc(new Rect(1079f, celestialY - 8f, 65f, 65f), WithAlpha(p.Sky, p.Night));

            for (int i = 0; i < 7; i++)
            {
                float x = Repeat(i * 247f - scroll * 0.055f + time * 2f, 1550f) - 160f;
                float y = 145f + (i % 3) * 65f;
                float size = 0.65f + (i % 3) * 0.2f;
                Cloud(x, y, size, new Color(1f, 1f, 1f, Mathf.Lerp(0.65f, 0.12f, p.Night) * (1f - p.Rain * 0.5f)));
            }
        }

        private void DrawCity(Palette p, float scroll, float time)
        {
            float skylineY = Mathf.Lerp(490f, 635f, p.Skyway);
            for (int i = -1; i < 19; i++)
            {
                int variant = (i + 100) % 9;
                float x = i * 82f - Repeat(scroll * 0.055f, 82f);
                float buildingHeight = 70f + (variant * 31 % 130);
                Color tint = p.FarCity;
                tint.a = 0.5f;
                Fill(new Rect(x, skylineY - buildingHeight, 55f + variant % 3 * 9f, buildingHeight), tint);
                Fill(new Rect(x + 19f, skylineY - buildingHeight - 10f, 16f, 10f), tint);
                if (p.Night > 0.2f)
                {
                    for (int row = 0; row < 5; row++)
                        for (int col = 0; col < 3; col++)
                            if ((i + row + col * 3) % 3 != 0)
                                Fill(new Rect(x + 9f + col * 14f, skylineY - buildingHeight + 18f + row * 22f, 5f, 7f), WithAlpha(p.Accent, p.Night * 0.5f));
                }
            }

            Texture2D city = art.CityBackdrop != null ? art.CityBackdrop : art.Background;
            if (city != null)
            {
                const float tileWidth = 1410f;
                float x = -Repeat(scroll * 0.11f, tileWidth);
                float y = Mathf.Lerp(125f, 345f, p.Skyway);
                float height = Mathf.Lerp(435f, 295f, p.Skyway);
                Color tint = p.Backdrop;
                tint.a = Mathf.Lerp(0.88f, 0.48f, p.Skyway);
                GUI.color = tint;
                // Exclude the road baked into the bottom of the supplied city image.
                Rect source = new Rect(0f, 0.145f, 1f, 0.855f);
                GUI.DrawTextureWithTexCoords(new Rect(x, y, tileWidth, height), city, source, true);
                GUI.DrawTextureWithTexCoords(new Rect(x + tileWidth, y, tileWidth, height), city, source, true);
                GUI.color = Color.white;
                // The thin atmospheric veil joins the art's upper edge to the sky.
                for (int i = 0; i < 10; i++)
                    Fill(new Rect(0f, y + i * 3f, Width, 3f), WithAlpha(p.Sky, (1f - i / 10f) * 0.55f));
            }

            if (p.Skyway > 0.1f)
            {
                for (int i = 0; i < 6; i++)
                {
                    float x = Repeat(i * 310f - scroll * 0.17f, 1680f) - 180f;
                    Cloud(x, 605f + (i % 2) * 45f, 1.35f, new Color(0.96f, 0.98f, 1f, p.Skyway * 0.68f));
                }
            }

            // Small local details give the skyline a readable foreground scale.
            float signX = 1010f - Repeat(scroll * 0.2f, 1580f);
            if (signX > -150f && p.Skyway < 0.5f)
            {
                Fill(new Rect(signX + 13f, 496f, 6f, 65f), Hex(0x43576c));
                Fill(new Rect(signX, 473f, 114f, 31f), Hex(0x23394c));
                Fill(new Rect(signX + 4f, 477f, 106f, 23f), Hex(0xc9e8d4));
                if (signStyle == null)
                {
                    signStyle = new GUIStyle { font = art.Font, fontSize = 13, alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
                    signStyle.normal.textColor = Hex(0x284b4e);
                }
                GUI.color = Color.white;
                GUI.Label(new Rect(signX + 3f, 477f, 108f, 23f), "DINO CITY", signStyle);
            }
        }

        private void ReadGaps(RunnerSession session)
        {
            gaps.Clear();
            foreach (WorldEntity entity in session.Entities)
            {
                if (!entity.Removed && entity.Kind == EntityKind.Gap && entity.X < Width && entity.X + entity.Width > 0f)
                    gaps.Add(new Vector2(Mathf.Max(0f, entity.X), Mathf.Min(Width, entity.X + entity.Width)));
            }
            gaps.Sort((a, b) => a.x.CompareTo(b.x));
        }

        private void DrawRoad(Palette p, float scroll)
        {
            float left = 0f;
            for (int i = 0; i < gaps.Count; i++)
            {
                Vector2 gap = gaps[i];
                if (gap.x > left)
                    DrawRoadSegment(left, gap.x, p, scroll);
                left = Mathf.Max(left, gap.y);
            }
            if (left < Width)
                DrawRoadSegment(left, Width, p, scroll);

            for (int i = 0; i < gaps.Count; i++)
            {
                Vector2 gap = gaps[i];
                DrawWarningEdge(gap.x - 14f, p.Accent);
                DrawWarningEdge(gap.y, p.Accent);
                // Downward marker is decorative; the open span remains empty.
                Line(new Vector2(gap.x - 2f, GroundY + 6f), new Vector2(gap.x - 2f, GroundY + 46f), WithAlpha(p.Foundation, 0.65f), 3f);
                Line(new Vector2(gap.y + 2f, GroundY + 6f), new Vector2(gap.y + 2f, GroundY + 46f), WithAlpha(p.Foundation, 0.65f), 3f);
            }
        }

        private void DrawRoadSegment(float left, float right, Palette p, float scroll)
        {
            float width = right - left;
            if (width <= 0f)
                return;
            GUI.BeginGroup(new Rect(left, GroundY, width, Height - GroundY));
            try
            {
                bool flying = p.Skyway > 0.5f || gaps.Count > 0;
                Texture2D texture = flying ? art.PlatformLong : art.Road;
                if (texture == null)
                    texture = art.PlatformLong;
                float tileWidth = flying ? 360f : 680f;
                float slabHeight = flying ? 105f : 114f;

                if (!flying)
                    Fill(new Rect(0f, 0f, width, Height - GroundY), p.Foundation);
                else
                    Fill(new Rect(0f, 0f, width, 39f), p.Foundation);

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
                Fill(new Rect(0f, 0f, width, 3f), Color.Lerp(Hex(0xc7e9e1), p.Accent, p.Night * 0.35f));
                Fill(new Rect(0f, 4f, width, 3f), WithAlpha(Hex(0x1c3446), 0.45f));

                if (p.Rain > 0.1f)
                {
                    for (int i = 0; i < 8; i++)
                    {
                        float x = Repeat(i * 183f - scroll, 1400f) - left;
                        Fill(new Rect(x, 21f + i % 3 * 13f, 43f + i % 2 * 20f, 2f), WithAlpha(p.Accent, p.Rain * 0.28f));
                    }
                }
            }
            finally
            {
                GUI.EndGroup();
            }
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
                if (entity.Removed || entity.Kind == EntityKind.Gap || entity.X > Width + 100f || entity.X + entity.Width < -100f)
                    continue;

                float bottom = GroundY - entity.Y;
                Rect rect = new Rect(entity.X, bottom - entity.Height, entity.Width, entity.Height);
                Texture2D texture = null;
                switch (entity.Kind)
                {
                    case EntityKind.House:
                        texture = entity.Variant % 3 == 0 ? art.House1 : entity.Variant % 3 == 1 ? art.House2 : art.House3;
                        Disc(new Rect(entity.X - 7f, GroundY - 4f, entity.Width + 14f, 10f), new Color(0.05f, 0.12f, 0.18f, 0.18f), true);
                        break;
                    case EntityKind.Hydrant:
                        texture = art.Hydrant;
                        Disc(new Rect(entity.X - 6f, GroundY - 4f, entity.Width + 12f, 10f), new Color(0.05f, 0.12f, 0.18f, 0.18f), true);
                        break;
                    case EntityKind.Plane:
                        texture = art.Plane;
                        // A leading dash makes the incoming airborne obstacle easier to read.
                        Line(new Vector2(rect.xMax + 6f, rect.center.y), new Vector2(rect.xMax + 34f, rect.center.y), WithAlpha(p.Horizon, 0.6f), 3f);
                        break;
                    case EntityKind.Star:
                        texture = art.Star;
                        rect.y += Mathf.Sin(time * 3f + entity.Id * 0.9f) * 3f;
                        Disc(new Rect(rect.center.x - rect.width, rect.center.y - rect.height, rect.width * 2f, rect.height * 2f), new Color(1f, 0.8f, 0.27f, 0.28f), true);
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
                if (texture != null)
                {
                    GUI.color = Color.Lerp(Color.white, p.Backdrop, 0.13f * p.Night);
                    GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
                }
                else
                {
                    Fill(rect, entity.Kind == EntityKind.Heart ? Hex(0xff657d) : entity.Kind == EntityKind.Star ? Hex(0xffd36f) : Hex(0x53758a));
                }
            }
        }

        private void DrawDino(RunnerSession session, float time)
        {
            float y = session.PlayerY;
            float shadowScale = Mathf.Clamp(1f - y / 450f, 0.5f, 1f);
            bool overGap = false;
            for (int i = 0; i < gaps.Count; i++)
                if (DinoX >= gaps[i].x && DinoX <= gaps[i].y)
                    overGap = true;
            if (!overGap && y > -30f)
                Disc(new Rect(DinoX - 39f * shadowScale, GroundY - 6f, 78f * shadowScale, 13f * shadowScale), new Color(0.03f, 0.09f, 0.16f, 0.22f), true);

            bool running = session.State == RunState.Playing || session.State == RunState.Menu;
            float clock = session.State == RunState.Menu ? time : session.Elapsed;
            float bob = running && y <= 1f && !session.IsDucking ? Mathf.Sin(clock * 21f) * 1.3f : 0f;
            float width = session.IsDucking ? 109f : 99f;
            float height = session.IsDucking ? 44f : 91f;
            Rect rect = new Rect(DinoX - width / 2f, GroundY - y - height + bob, width, height);

            if (session.InvincibleRemaining > 0f)
            {
                Disc(new Rect(rect.x - 14f, rect.y - 12f, rect.width + 28f, rect.height + 26f), new Color(0.65f, 1f, 0.87f, 0.3f), true);
                if (Mathf.FloorToInt(time * 12f) % 2 == 0)
                    GUI.color = new Color(1f, 1f, 1f, 0.42f);
                else
                    GUI.color = Color.white;
            }
            else
                GUI.color = Color.white;

            Texture2D texture = null;
            if (art.DinoFrames != null && art.DinoFrames.Length > 0)
            {
                int frame = y > 3f ? Mathf.Min(2, art.DinoFrames.Length - 1) : Mathf.FloorToInt(clock * (9f + session.Speed / 150f)) % art.DinoFrames.Length;
                if (!running && y <= 3f)
                    frame = 0;
                texture = art.DinoFrames[frame];
            }
            if (texture != null)
                GUI.DrawTexture(rect, texture, ScaleMode.StretchToFill, true);
            else
                Fill(rect, Hex(0x7fbd72));

            if (running && y <= 1f && session.Elapsed > 0.5f)
            {
                for (int i = 0; i < 3; i++)
                {
                    float phase = Repeat(clock * 3f + i * 0.33f, 1f);
                    float x = DinoX - 42f - phase * 44f;
                    Fill(new Rect(x, GroundY - 2f - phase * 6f, 5f - phase * 3f, 3f), new Color(0.89f, 0.92f, 0.87f, (1f - phase) * 0.5f));
                }
            }
            GUI.color = Color.white;
        }

        private void DrawWeather(Palette p, float scroll, float time)
        {
            if (p.Sunset > 0.1f)
                Disc(new Rect(855f, 170f, 550f, 550f), new Color(1f, 0.75f, 0.43f, 0.14f * p.Sunset), true);

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
                    if (phase < 0.3f)
                    {
                        bool aboveGap = false;
                        for (int j = 0; j < gaps.Count; j++)
                            if (x >= gaps[j].x && x <= gaps[j].y)
                                aboveGap = true;
                        if (!aboveGap)
                        {
                            float spread = phase * 20f;
                            Line(new Vector2(x - spread, GroundY - 1f), new Vector2(x - spread - 3f, GroundY - 5f), WithAlpha(p.Horizon, (0.3f - phase) * p.Rain), 1.5f);
                            Line(new Vector2(x + spread, GroundY - 1f), new Vector2(x + spread + 3f, GroundY - 5f), WithAlpha(p.Horizon, (0.3f - phase) * p.Rain), 1.5f);
                        }
                    }
                }
            }

            if (shownDistrict == District.Storm)
            {
                // A soft flash, rather than a full white frame, keeps hazards readable.
                float phase = Repeat(time, 11f);
                if (phase < 0.08f || (phase > 0.19f && phase < 0.23f))
                    Fill(new Rect(0f, 105f, Width, GroundY - 105f), new Color(0.87f, 0.94f, 1f, 0.08f));
            }
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
            if (delta.sqrMagnitude < 0.001f)
                return;
            Matrix4x4 previous = GUI.matrix;
            GUIUtility.RotateAroundPivot(Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, from);
            Fill(new Rect(from.x, from.y - thickness * 0.5f, delta.magnitude, thickness), color);
            GUI.matrix = previous;
        }

        private static Color Hex(uint rgb)
        {
            return new Color(((rgb >> 16) & 255) / 255f, ((rgb >> 8) & 255) / 255f, (rgb & 255) / 255f, 1f);
        }

        private static Color WithAlpha(Color color, float alpha)
        {
            color.a = alpha;
            return color;
        }

        private static float Repeat(float value, float length)
        {
            return value - Mathf.Floor(value / length) * length;
        }

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

        public void Dispose()
        {
            if (circle != null)
                UnityEngine.Object.Destroy(circle);
            if (glow != null)
                UnityEngine.Object.Destroy(glow);
        }
    }
}
