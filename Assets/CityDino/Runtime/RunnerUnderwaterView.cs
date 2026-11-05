using System;
using UnityEngine;

namespace CityDino
{
    /// <summary>Water, diving equipment and naval attacks use the shared combat clock and geometry.</summary>
    public sealed partial class RunnerWorldView
    {
        private void DrawUnderwaterArenaBackdrop(float clock)
        {
            DrawUnderwaterWater(clock, 0f);
        }

        private void DrawUnderwaterWater(float clock, float surfaceY)
        {
            float top = Mathf.Clamp(surfaceY, 0f, Height);
            float visible = Height - top;
            if (visible <= 0f) return;
            GUI.color = Color.white;
            if (art.UnderwaterBackdrop != null)
                GUI.DrawTextureWithTexCoords(new Rect(0, top, Width, visible), art.UnderwaterBackdrop,
                    new Rect(0, 0, 1f, visible / Height), true);
            else
            {
                for (int i = 0; i < 30; i++)
                {
                    float y = top + i * visible / 30f;
                    Fill(new Rect(0, y, Width, visible / 30f + 1f),
                        Color.Lerp(Hex(0x176b86), Hex(0x06263d), (y + 1f) / Height));
                }
                // Decorative reef stays beneath the playable swimming region;
                // it is not a road or a floor collider.
                for (int i = 0; i < 17; i++)
                {
                    float x = i * 91f - 23f;
                    Fill(new Rect(x, 694f - i % 3 * 9f, 52f, 45f), Hex(0x103d4c));
                    Line(new Vector2(x + 23f, 711f), new Vector2(x + 17f, 654f - i % 4 * 11f), Hex(0x287c7a), 4f);
                    Line(new Vector2(x + 19f, 681f), new Vector2(x + 7f, 666f), Hex(0x408b85), 3f);
                }
            }
            // Light columns and particles move in the water, not with wall
            // time, so pausing freezes every visible ripple and bubble.
            for (int i = 0; i < 5; i++)
            {
                float x = 75f + i * 269f + Mathf.Sin(clock * .16f + i) * 24f;
                Line(new Vector2(x, Mathf.Max(top, 130f)), new Vector2(x - 118f, Height),
                    new Color(.45f, .91f, 1f, .034f), 43f + i % 2 * 24f);
            }
            for (int i = 0; i < 29; i++)
            {
                float age = Repeat(clock * (.038f + i % 4 * .009f) + i * .137f, 1f);
                float y = Height - age * (Height + 35f);
                if (y < top) continue;
                float x = Repeat(i * 143f + Mathf.Sin(age * 5f + i) * 16f, Width);
                float size = 2f + i % 4;
                Disc(new Rect(x, y, size, size), new Color(.72f, .95f, 1f, .11f + i % 3 * .035f));
            }
            if (surfaceY > -25f && surfaceY < Height)
            {
                float y = Mathf.Max(0f, surfaceY);
                Fill(new Rect(0, y, Width, 7f), new Color(.53f, .94f, 1f, .42f));
                for (int i = 0; i < 24; i++)
                {
                    float x = Repeat(i * 75f + clock * 17f, Width + 60f) - 30f;
                    Line(new Vector2(x, y + 3f + Mathf.Sin(clock * 2f + i) * 2f),
                        new Vector2(x + 27f, y + 3f), new Color(.82f, 1f, 1f, .64f), 2f);
                }
            }
        }

        private void DrawUnderwaterJourney(RunnerSession session)
        {
            float clock = session.BossElapsed, depth = Mathf.Clamp01(session.WaterDepth);
            Matrix4x4 canvas = GUI.matrix;
            gaps.Clear();
            Palette palette = ForDistrict(session.CurrentDistrict);
            DrawSky(palette, session.Distance, session.Elapsed);
            try
            {
                GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(-depth * 470f, -depth * 680f, 0),
                    Quaternion.identity, Vector3.one);
                // The approach ends at a real pier edge. The scripted jump
                // reaches X520, clearly beyond this X500 end of the street.
                GUI.BeginGroup(new Rect(0f, 0f, 500f, Height));
                try { DrawApproachStreet(session, clock); }
                finally { GUI.EndGroup(); }
                Fill(new Rect(388f, GroundY, 112f, 23f), Hex(0x607b83));
                Fill(new Rect(402f, GroundY + 23f, 16f, 143f), Hex(0x34576a));
                Fill(new Rect(478f, GroundY + 23f, 16f, 143f), Hex(0x34576a));
                for (int i = 0; i < 4; i++)
                    Fill(new Rect(420f, GroundY + 38f + i * 23f, 59f, 5f), Hex(0x8ba9ad));
            }
            finally { GUI.matrix = canvas; }
            float surface = GroundY + 35f - depth * (GroundY + 190f);
            DrawUnderwaterWater(clock, surface);
            if (!session.DivingHelmetEquipped) DrawDivingHelmetPickup(session, clock);
            if (depth > .03f && depth < .85f)
            {
                float spread = 19f + depth * 42f;
                Disc(new Rect(session.PlayerX - spread, surface - 7f, spread * 2f, 16f),
                    new Color(.72f, .98f, 1f, .23f), true);
            }
            if (session.CurrentBoss != null)
            {
                // The vessel is revealed only after the descending camera has
                // brought its complete body below the visible water surface.
                float reveal = Mathf.Clamp01((depth - .44f) / .22f);
                DrawSubmarineBody(session.CurrentBoss, session.ArenaPhase, clock, session.BossPhaseRemaining, reveal);
            }
            DrawDino(session, session.PlayerAnimationTime);
            GUI.color = Color.white;
        }

        private void DrawUnderwaterForwardDeparture(RunnerSession session)
        {
            float clock = session.BossElapsed, depth = Mathf.Clamp01(session.WaterDepth);
            float camera = session.BossExitCameraX, progress = session.ExitTravelProgress;
            Matrix4x4 canvas = GUI.matrix;
            gaps.Clear();
            Palette palette = ForDistrict(session.CurrentDistrict);
            DrawSky(palette, session.Distance, session.Elapsed);
            DrawUnderwaterWater(clock, GroundY + 35f - depth * (GroundY + 190f));
            try
            {
                // The surface-side quay comes into view as the diver rises.
                // By the gate crossing WaterDepth is zero and both feet and
                // the quay have reached the ordinary running baseline.
                GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(-camera, depth * 680f, 0),
                    Quaternion.identity, Vector3.one);
                DrawArenaFloor(BossKind.Sentinel, 1080f, RunnerSession.BossExitStreetX - 1080f);
                Fill(new Rect(1080f, GroundY - 31f, RunnerSession.BossExitStreetX - 1080f, 27f), Hex(0x4e7c8b));
                DrawArenaGate(BossKind.Sentinel, Mathf.Clamp01((progress - .08f) / .46f), clock, RunnerSession.BossExitGateX);
                GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(RunnerSession.BossExitStreetX - camera,
                    depth * 680f, 0), Quaternion.identity, Vector3.one);
                DrawDestinationStreet(session);
                // The defeated vessel remains below the diver as the camera
                // rises to the surface, rather than floating above the quay.
                GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(-camera, (1f - depth) * 680f, 0), Quaternion.identity, Vector3.one);
                if (session.CurrentBoss != null)
                    DrawBossBody(session.CurrentBoss, session.ArenaPhase, clock, session.BossPhaseRemaining);
            }
            finally { GUI.matrix = canvas; }
            DrawDino(session, session.PlayerAnimationTime);
            GUI.color = Color.white;
        }

        private void DrawDivingHelmetPickup(RunnerSession session, float clock)
        {
            Rect rect = new Rect(session.DivingHelmetX - 26f, GroundY - session.DivingHelmetY - 52f, 52f, 52f);
            Disc(new Rect(rect.x - 9f, rect.y - 8f, 70f, 69f), new Color(.56f, .9f, 1f, .19f + .05f * Mathf.Sin(clock * 6f)), true);
            DrawDivingSprite(rect, 0, Color.white);
            Line(new Vector2(rect.center.x, rect.y - 19f), new Vector2(rect.center.x, rect.y - 6f), Hex(0xe1fcff), 2f);
            Line(new Vector2(rect.center.x - 5f, rect.y - 11f), new Vector2(rect.center.x, rect.y - 6f), Hex(0xe1fcff), 2f);
            Line(new Vector2(rect.center.x + 5f, rect.y - 11f), new Vector2(rect.center.x, rect.y - 6f), Hex(0xe1fcff), 2f);
        }

        private static Rect DivingHelmetRect(RunnerSession session, Rect body)
        {
            int frame = RunnerGeometry.GetPlayerFrame(session);
            bool swimmingPose = frame >= 8;
            float faceX = .89f;
            float faceY = frame == 9 ? .29f : frame == 10 ? .30f : .12f;
            float width = .55f, height = .62f;
            if (session.CurrentCharacter == CharacterKind.Triceratops)
            {
                // These head positions include the character atlas placement
                // in the shared logical body, rather than only its UV cell.
                faceX = swimmingPose ? .85f : .86f;
                faceY = swimmingPose ? .63f : .58f;
                width = .72f; height = .76f;
            }
            else if (session.CurrentCharacter == CharacterKind.Raptor)
            {
                faceX = .86f; faceY = swimmingPose ? .44f : .38f;
            }
            if (session.FacingLeft) faceX = 1f - faceX;
            // Measured clear-glass centroid in the unchanged generated sprite
            // is (.66,.49); the rear oxygen hose is outside this opening.
            float glassX = session.FacingLeft ? .34f : .66f;
            return new Rect(body.x + body.width * (faceX - width * glassX),
                body.y + body.height * (faceY - height * .49f), body.width * width, body.height * height);
        }

        private void DrawDivingHelmet(RunnerSession session, Rect logicalBody, float clock)
        {
            if (!session.DivingHelmetEquipped) return;
            Rect rect = DivingHelmetRect(session, logicalBody);
            // A very light glass tint keeps the original face fully readable.
            float glassX = session.FacingLeft ? .34f : .66f;
            Disc(new Rect(rect.x + rect.width * (glassX - .30f), rect.y + rect.height * .185f,
                rect.width * .60f, rect.height * .61f), new Color(.43f, .86f, 1f, .06f));
            DrawDivingSprite(rect, 0, Color.white, 0f, session.FacingLeft);
            if (session.WaterDepth > .1f)
            {
                for (int i = 0; i < 3; i++)
                {
                    float age = Repeat(clock * .9f + i / 3f, 1f);
                    float direction = session.FacingLeft ? -1f : 1f;
                    float x = rect.center.x + direction * (rect.width * .38f + age * 13f);
                    float y = rect.center.y - age * 31f;
                    Disc(new Rect(x, y, 3f + age * 2f, 3f + age * 2f), new Color(.78f, .98f, 1f, (1f - age) * .5f));
                }
            }
        }

        private void DrawSubmarineBody(BossEncounter boss, BossPhase phase, float clock, float remaining, float reveal = 1f)
        {
            if (reveal <= 0f) return;
            reveal = Mathf.Clamp01(reveal);
            Rect body = new Rect(boss.X, GroundY - boss.Y - boss.Height, boss.Width, boss.Height);
            float hit = Mathf.Clamp01(boss.HitFlashRemaining / .14f);
            bool defeated = phase == BossPhase.Victory;
            Color tint = defeated ? new Color(.45f, .62f, .67f, .75f) : Color.Lerp(Color.white, new Color(1f, .65f, .4f), hit);
            tint.a *= reveal;
            GUI.color = tint;
            if (art.SubmarineAtlas != null)
                GUI.DrawTextureWithTexCoords(body, art.SubmarineAtlas, art.GetSubmarineUV(BossActionFrame(boss)), true);
            else
            {
                Disc(body, WithAlpha(Hex(0x38566a), reveal));
                Fill(new Rect(body.x + body.width * .42f, body.y - 6f, body.width * .18f, 23f), WithAlpha(Hex(0x527a8b), reveal));
                for (int i = 0; i < 5; i++) Disc(new Rect(body.x + 38f + i * 34f, body.center.y - 10f, 13f, 13f), WithAlpha(Hex(0x77e0e5), reveal));
            }
            GUI.color = Color.white;
            if (reveal < .995f) return;
            for (int i = 0; i < 12; i++)
            {
                float age = Repeat(clock * (defeated ? .2f : .6f) + i * .137f, 1f);
                float x = body.xMax - 14f + age * (defeated ? 60f : 82f);
                float y = body.center.y + (i % 3 - 1) * 13f - age * (defeated ? 85f : 18f);
                float size = 3f + age * 6f;
                Disc(new Rect(x, y, size, size), new Color(.76f, .96f, 1f, (1f - age) * .38f));
            }
            if (defeated)
            {
                float progress = Mathf.Clamp01(1f - Mathf.Max(0f, remaining - RunnerSession.BossOutroDuration) / RunnerSession.BossExplosionDuration);
                for (int i = 0; i < 6; i++)
                {
                    float size = 14f + Repeat(progress + i * .17f, 1f) * 31f;
                    Disc(new Rect(body.x + body.width * (.12f + i % 3 * .3f), body.y + body.height * (.2f + i % 2 * .3f), size, size),
                        new Color(.44f, .93f, 1f, (1f - progress) * .26f), true);
                }
                return;
            }
            float pulse = .5f + Mathf.Sin(clock * 7f) * .15f;
            Color core = boss.Vulnerable ? Hex(0xffd982) : Hex(0x7ce7eb);
            Vector2 centre = new Vector2(body.center.x, body.y + body.height * .45f);
            Disc(new Rect(centre.x - 23f, centre.y - 23f, 46f, 46f), WithAlpha(core, pulse * .28f), true);
            Disc(new Rect(centre.x - 8f, centre.y - 8f, 16f, 16f), core);
            if (!boss.Vulnerable)
            {
                Line(new Vector2(centre.x - 13f, centre.y - 13f), new Vector2(centre.x + 13f, centre.y + 13f), Hex(0x264c63), 3f);
                Line(new Vector2(centre.x - 13f, centre.y + 13f), new Vector2(centre.x + 13f, centre.y - 13f), Hex(0x264c63), 3f);
            }
            if (boss.ShotFlashRemaining > 0f)
                Disc(new Rect(body.x - 21f, body.center.y - 15f, 37f, 28f), new Color(.52f, .94f, 1f, boss.ShotFlashRemaining * 2f), true);
            if (hit > 0f) DrawBossImpact(body, hit, clock);
            if (boss.ContactActive)
            {
                Rect strike = new Rect(boss.ContactX, GroundY - boss.ContactY - boss.ContactHeight, boss.ContactWidth, boss.ContactHeight);
                Fill(strike, new Color(.58f, .96f, 1f, .16f));
                Fill(new Rect(strike.x, strike.y, 5f, strike.height), new Color(.8f, 1f, 1f, .8f));
            }
        }

        private bool DrawUnderwaterProjectile(BossProjectile projectile, float clock)
        {
            int cell;
            if (projectile.Kind == BossAttackKind.Torpedo) cell = 2;
            else if (projectile.Kind == BossAttackKind.FishShoal) cell = 1;
            else if (projectile.Kind == BossAttackKind.DepthCharge) cell = 3;
            else return false;
            Rect hit = new Rect(projectile.X, GroundY - projectile.Y - projectile.Height, projectile.Width, projectile.Height);
            if (projectile.Radius > 0f)
                Disc(new Rect(hit.center.x - projectile.Radius, hit.center.y - projectile.Radius, projectile.Radius * 2f, projectile.Radius * 2f),
                    new Color(.44f, .9f, 1f, .16f));
            float angle = (float)(Math.Atan2(-projectile.VelocityY, projectile.VelocityX) * 180d / Math.PI) - 180f;
            Rect visual = cell == 2 ? new Rect(hit.center.x - hit.width * .65f, hit.center.y - hit.height * .5f, hit.width * 1.3f, hit.height) : hit;
            DrawDivingSprite(visual, cell, Color.white, angle);
            float dx = -projectile.VelocityX, dy = projectile.VelocityY;
            float length = Mathf.Max(1f, Mathf.Sqrt(dx * dx + dy * dy)); dx /= length; dy /= length;
            for (int i = 0; i < 5; i++)
            {
                float age = Repeat(clock * 2f + i * .2f, 1f);
                float distance = 13f + age * 29f;
                float size = 2f + age * 4f;
                Disc(new Rect(hit.center.x + dx * distance, hit.center.y + dy * distance, size, size), new Color(.67f, .96f, 1f, (1f - age) * .38f));
            }
            return true;
        }

        private bool DrawUnderwaterObstacle(BossObstacle obstacle, float clock)
        {
            if (obstacle.Kind != BossAttackKind.SeaMine && obstacle.Kind != BossAttackKind.DepthCharge) return false;
            Rect rect = new Rect(obstacle.X, GroundY - obstacle.Y - obstacle.Height, obstacle.Width, obstacle.Height);
            if (obstacle.Kind == BossAttackKind.DepthCharge)
            {
                float radius = obstacle.Radius > 0f ? obstacle.Radius : Mathf.Min(rect.width, rect.height) * .5f;
                Rect burst = new Rect(rect.center.x - radius, rect.center.y - radius, radius * 2f, radius * 2f);
                Disc(burst, new Color(.52f, .94f, 1f, .50f));
                Disc(new Rect(burst.x + radius * .18f, burst.y + radius * .18f, radius * 1.64f, radius * 1.64f),
                    new Color(.86f, 1f, 1f, .45f));
                for (int i = 0; i < 10; i++)
                {
                    float angle = i * .6283185f + clock * 2f;
                    float x = burst.center.x + Mathf.Cos(angle) * radius * .72f;
                    float y = burst.center.y + Mathf.Sin(angle) * radius * .72f;
                    Disc(new Rect(x - 3f, y - 3f, 6f, 6f), new Color(.92f, 1f, 1f, .84f));
                }
                return true;
            }
            float alpha = obstacle.Active ? 1f : .25f;
            DrawDivingSprite(rect, 3, WithAlpha(Color.white, alpha), clock * 17f + obstacle.Id * 7f);
            Disc(new Rect(rect.center.x - 4f, rect.center.y - 4f, 8f, 8f), new Color(1f, .45f, .3f, alpha));
            Line(new Vector2(rect.center.x, rect.yMax), new Vector2(rect.center.x + Mathf.Sin(clock * 1.5f + obstacle.Id) * 5f, rect.yMax + 21f),
                new Color(.45f, .7f, .75f, .4f), 1f);
            if (obstacle.Destructible && obstacle.Health < obstacle.MaxHealth)
            {
                Fill(new Rect(rect.x, rect.y - 5f, rect.width, 3f), Hex(0x123646));
                Fill(new Rect(rect.x, rect.y - 5f, rect.width * Mathf.Clamp01(obstacle.Health / obstacle.MaxHealth), 3f), Hex(0x91f4dc));
            }
            return true;
        }

        private bool DrawUnderwaterTelegraph(BossTelegraph mark, float clock)
        {
            if (mark.Kind != BossAttackKind.Torpedo && mark.Kind != BossAttackKind.FishShoal &&
                mark.Kind != BossAttackKind.SeaMine && mark.Kind != BossAttackKind.SonarBeam &&
                mark.Kind != BossAttackKind.DepthCharge && mark.Kind != BossAttackKind.SubmarineRam) return false;
            Rect rect = new Rect(mark.X, GroundY - mark.Y - mark.Height, mark.Width, mark.Height);
            Color light = mark.Kind == BossAttackKind.DepthCharge ? Hex(0xffce80) : Hex(0x9af2ed);
            if (mark.Active)
            {
                Fill(rect, WithAlpha(light, .66f));
                for (float x = rect.x + 4f; x < rect.xMax - 3f; x += 28f)
                    Line(new Vector2(x, rect.center.y - rect.height * .3f), new Vector2(x + 17f, rect.center.y + rect.height * .3f), Hex(0xe4ffff), 2f);
                return true;
            }
            if (mark.GapHeight > 0f && mark.Height > mark.GapHeight)
            {
                float lower = Mathf.Clamp(mark.GapY, mark.Y, mark.Y + mark.Height);
                float upper = Mathf.Clamp(mark.GapY + mark.GapHeight, lower, mark.Y + mark.Height);
                DrawBossWarningRect(new Rect(mark.X, GroundY - lower, mark.Width, lower - mark.Y), light, mark);
                DrawBossWarningRect(new Rect(mark.X, GroundY - mark.Y - mark.Height, mark.Width, mark.Y + mark.Height - upper), light, mark);
            }
            else DrawBossWarningRect(rect, light, mark);
            if (mark.GapWidth > 0f)
            {
                Line(new Vector2(mark.GapX, Mathf.Max(rect.y, 210f)), new Vector2(mark.GapX, GroundY), new Color(.56f, 1f, .8f, .6f), 1.5f);
                Line(new Vector2(mark.GapX + mark.GapWidth, Mathf.Max(rect.y, 210f)), new Vector2(mark.GapX + mark.GapWidth, GroundY), new Color(.56f, 1f, .8f, .6f), 1.5f);
            }
            if (mark.Kind == BossAttackKind.SeaMine || mark.Kind == BossAttackKind.DepthCharge)
            {
                float sign = mark.Kind == BossAttackKind.SeaMine ? -1f : 1f;
                for (float y = rect.y + 15f; y < rect.yMax - 12f; y += 52f)
                {
                    Vector2 tip = new Vector2(rect.center.x, y + sign * 7f);
                    Line(new Vector2(tip.x - 7f, y - sign * 3f), tip, light, 2f);
                    Line(new Vector2(tip.x + 7f, y - sign * 3f), tip, light, 2f);
                }
            }
            else
            {
                float direction = mark.VelocityX > 0f ? 1f : -1f;
                for (float x = rect.x + 20f; x < rect.xMax - 20f; x += 74f)
                {
                    float y = rect.center.y;
                    Line(new Vector2(x - direction * 8f, y - 7f), new Vector2(x + direction * 7f, y), light, 2f);
                    Line(new Vector2(x - direction * 8f, y + 7f), new Vector2(x + direction * 7f, y), light, 2f);
                }
            }
            return true;
        }

        private bool DrawDivingSprite(Rect rect, int cell, Color color, float angle = 0f, bool flip = false)
        {
            Color previous = GUI.color; Matrix4x4 matrix = GUI.matrix;
            GUI.color = color;
            try
            {
                if (angle != 0f)
                    GUI.matrix = matrix * Matrix4x4.TRS(new Vector3(rect.center.x, rect.center.y, 0),
                        Quaternion.Euler(0, 0, angle), Vector3.one) *
                        Matrix4x4.TRS(new Vector3(-rect.center.x, -rect.center.y, 0), Quaternion.identity, Vector3.one);
                if (art.DivingKitAtlas != null)
                {
                    Rect uv = art.GetDivingKitUV(cell);
                    if (flip) uv = new Rect(uv.x + uv.width, uv.y, -uv.width, uv.height);
                    GUI.DrawTextureWithTexCoords(rect, art.DivingKitAtlas, uv, true);
                    return true;
                }
                if (cell == 0)
                {
                    // Preserve a genuinely clear centre while artwork imports.
                    // Drawing a solid outer disc first would hide the actor's
                    // face even if a translucent disc were placed over it.
                    float centreX = rect.x + rect.width * (flip ? .34f : .66f);
                    float centreY = rect.y + rect.height * .49f;
                    float radiusX = rect.width * .31f, radiusY = rect.height * .32f;
                    Disc(new Rect(centreX - radiusX, centreY - radiusY, radiusX * 2f, radiusY * 2f),
                        new Color(.35f, .78f, .93f, .08f));
                    for (int i = 0; i < 32; i++)
                    {
                        float a = i * (float)(Math.PI * 2d / 32d);
                        float b = (i + 1) * (float)(Math.PI * 2d / 32d);
                        Line(new Vector2(centreX + Mathf.Cos(a) * radiusX, centreY + Mathf.Sin(a) * radiusY),
                            new Vector2(centreX + Mathf.Cos(b) * radiusX, centreY + Mathf.Sin(b) * radiusY),
                            Hex(0xe3bf78), 3f);
                    }
                    Fill(new Rect(centreX - radiusX * .7f, centreY + radiusY * .87f,
                        radiusX * 1.4f, rect.height * .1f), Hex(0x477a86));
                    Line(new Vector2(centreX - radiusX * .55f, centreY - radiusY * .72f),
                        new Vector2(centreX + radiusX * .40f, centreY - radiusY * .83f), Hex(0xffefc8), 2f);
                }
                else Disc(rect, cell == 1 ? Hex(0x65dbe5) : Hex(0x698799));
                return false;
            }
            finally { GUI.color = previous; GUI.matrix = matrix; }
        }
    }
}
