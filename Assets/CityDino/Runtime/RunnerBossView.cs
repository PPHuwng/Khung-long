using System;
using UnityEngine;

namespace CityDino
{
    /// <summary>Combat scenery and shared live geometry. Arena effects never run the weather or scrolling world.</summary>
    public sealed partial class RunnerWorldView
    {
        private void DrawBossArena(RunnerSession session)
        {
            BossEncounter boss = session.CurrentBoss;
            BossKind kind = boss == null ? BossKind.Tank : boss.Kind;
            float clock = session.BossElapsed;
            DrawArenaBackdrop(kind, clock);
            gaps.Clear();
            foreach (BossTelegraph telegraph in session.BossTelegraphs)
                if (telegraph.Visible) DrawBossTelegraph(telegraph, clock);
            if (boss != null) DrawBossBody(boss, session.ArenaPhase, clock, session.BossPhaseRemaining);
            DrawDino(session, session.PlayerAnimationTime);
            foreach (BossProjectile projectile in session.BossProjectiles)
                if (!projectile.Removed) DrawBossProjectile(projectile);
            GUI.color = Color.white;
        }

        private void DrawArenaBackdrop(BossKind kind, float clock)
        {
            Color sky = kind == BossKind.Tank ? Hex(0x485c68)
                : kind == BossKind.Sentinel ? Hex(0x202c43) : Hex(0x19334d);
            Color horizon = kind == BossKind.Tank ? Hex(0xc6b694)
                : kind == BossKind.Sentinel ? Hex(0x46657a) : Hex(0x849bab);
            for (int i = 0; i < 25; i++)
                Fill(new Rect(0, i * GroundY / 25f, Width, GroundY / 25f + 1), Color.Lerp(sky, horizon, i / 24f));

            if (kind == BossKind.Tank)
            {
                // Stacked containers and fence posts are scenery behind the flat combat surface.
                for (int i = 0; i < 7; i++)
                {
                    float x = i * 214f - 30f;
                    float y = 382f + i % 2 * 29f;
                    Fill(new Rect(x, y, 189, 141), i % 2 == 0 ? Hex(0x52605c) : Hex(0x796e5d));
                    Fill(new Rect(x + 7, y + 8, 175, 4), Hex(0x8e998c));
                    for (int rib = 0; rib < 8; rib++)
                        Fill(new Rect(x + 12 + rib * 23, y + 18, 3, 114), new Color(0.1f, 0.18f, 0.18f, 0.3f));
                }
                for (int i = 0; i < 16; i++)
                {
                    float x = i * 88;
                    Fill(new Rect(x, 477, 4, 75), Hex(0x253c44));
                    Line(new Vector2(x, 486), new Vector2(x + 88, 516), new Color(0.65f, 0.74f, 0.69f, 0.35f), 1);
                }
            }
            else if (kind == BossKind.Sentinel)
            {
                for (int i = 0; i < 6; i++)
                {
                    float x = i * 252f - 30;
                    Fill(new Rect(x, 145, 28, 390), Hex(0x263d50));
                    Fill(new Rect(x + 4, 147, 5, 388), Hex(0x567389));
                    Fill(new Rect(x + 28, 182, 221, 18), Hex(0x29455d));
                    Fill(new Rect(x + 66, 235, 105, 168), Hex(0x25364c));
                    Fill(new Rect(x + 73, 241, 91, 5), Hex(0x538092));
                    for (int light = 0; light < 4; light++)
                        Fill(new Rect(x + 80 + light * 19, 270, 8, 4), WithAlpha(Hex(0x96d5db), 0.35f));
                }
                Fill(new Rect(0, 510, Width, 40), Hex(0x3a5367));
                for (int i = 0; i < 24; i++) Fill(new Rect(i * 58, 518, 38, 3), Hex(0x82969b));
            }
            else
            {
                for (int i = 0; i < 18; i++)
                {
                    float x = i * 81f;
                    float height = 96 + i * 47 % 150;
                    Fill(new Rect(x, 520 - height, 67, height), i % 2 == 0 ? Hex(0x3d5c72) : Hex(0x4a6b7b));
                    for (int y = 0; y < 4; y++)
                        Fill(new Rect(x + 11, 537 - height + y * 29, 8, 5), WithAlpha(Hex(0xffe2a3), 0.42f));
                }
                Fill(new Rect(0, 523, Width, 28), Hex(0x284354));
                for (int i = 0; i < 7; i++)
                {
                    float x = Repeat(i * 235f - clock * 30f, Width + 160f) - 80;
                    Line(new Vector2(x, 260 + i * 31), new Vector2(x + 72, 260 + i * 31),
                        new Color(0.79f, 0.9f, 0.97f, 0.08f), 1.5f);
                }
            }

            Fill(new Rect(0, GroundY, Width, Height - GroundY), Hex(0x182d3b));
            Fill(new Rect(0, GroundY, Width, 7), Hex(0xa7b6b3));
            Fill(new Rect(0, GroundY + 7, Width, 55), Hex(0x465b64));
            for (int i = 0; i < 22; i++)
                Fill(new Rect(i * 64, GroundY + 12, 38, 5), Hex(0xdbbd68));
            Fill(new Rect(0, GroundY + 58, Width, 3), Hex(0x70828a));
            if (kind == BossKind.Gunship)
            {
                Fill(new Rect(557, GroundY + 21, 7, 26), Hex(0xb3c7c9));
                Fill(new Rect(609, GroundY + 21, 7, 26), Hex(0xb3c7c9));
                Fill(new Rect(557, GroundY + 31, 59, 6), Hex(0xb3c7c9));
            }
        }

        private void DrawBossBody(BossEncounter boss, BossPhase phase, float clock, float phaseRemaining)
        {
            Rect rect = new Rect(boss.X, GroundY - boss.Y - boss.Height, boss.Width, boss.Height);
            GroundShadow(boss.X, boss.Width);
            int rage = Math.Max(1, Math.Min(3, boss.RageLevel));
            bool defeated = phase == BossPhase.Victory;
            DrawBossMechanicalEffects(boss, rect, clock, defeated);
            float hit = Mathf.Clamp01(boss.HitFlashRemaining / .14f);
            // Only tint the one body image. Its rectangle and matrix always
            // remain the live physical body, including the core's recoil.
            Color tint = defeated ? new Color(0.48f, 0.51f, 0.53f, 0.75f)
                : Color.Lerp(Color.white, new Color(1f, .55f, .35f, 1f), hit);
            GUI.color = tint;
            if (art.BossAtlas != null)
                GUI.DrawTextureWithTexCoords(rect, art.BossAtlas, art.GetBossUV(boss.Kind), true);
            else DrawBossFallback(rect, boss.Kind);
            GUI.color = Color.white;
            if (defeated)
            {
                DrawBossDestruction(rect, clock, phaseRemaining);
                return;
            }

            DrawBossMovingDetails(boss, rect, clock, rage);
            DrawBossMuzzle(boss, rect, clock);
            if (hit > 0f) DrawBossImpact(rect, hit, clock);

            // Match auto-aim at body X centre and 55% of its bottom-up height.
            Vector2 coreCentre = new Vector2(rect.center.x, rect.y + rect.height * .45f);
            float size = Mathf.Min(29f, Mathf.Min(rect.width, rect.height) * 0.2f);
            Color core = boss.Vulnerable ? Hex(0xffd46b) : Hex(0x75b9d5);
            Disc(new Rect(coreCentre.x - size, coreCentre.y - size, size * 2, size * 2), WithAlpha(core, 0.24f), true);
            Disc(new Rect(coreCentre.x - size * .42f, coreCentre.y - size * .42f, size * .84f, size * .84f), core);
            if (!boss.Vulnerable)
            {
                Line(new Vector2(coreCentre.x - size, coreCentre.y - size * .7f),
                    new Vector2(coreCentre.x + size, coreCentre.y + size * .7f), Hex(0x334a62), 3);
                Line(new Vector2(coreCentre.x - size, coreCentre.y + size * .7f),
                    new Vector2(coreCentre.x + size, coreCentre.y - size * .7f), Hex(0x334a62), 3);
            }

            // Small armour lights communicate the escalating attack phase
            // without covering the arena with another notification.
            Color alarm = rage == 3 ? Hex(0xff6c58) : rage == 2 ? Hex(0xffbf65) : Hex(0x81cfd7);
            float pulse = .55f + .35f * Mathf.Sin(clock * (3f + rage * 2f));
            for (int i = 0; i < rage; i++)
                Fill(new Rect(rect.xMax - 18f - i * 10f, rect.y + 9f, 6f, 4f), WithAlpha(alarm, pulse));
        }

        private void DrawBossMechanicalEffects(BossEncounter boss, Rect rect, float clock, bool defeated)
        {
            if (defeated) return;
            int rage = Math.Max(1, Math.Min(3, boss.RageLevel));
            if (boss.Kind == BossKind.Tank)
            {
                for (int i = 0; i < 6; i++)
                {
                    float t = Repeat(clock * (.9f + rage * .14f) + i / 6f, 1f);
                    float x = rect.xMax - 20f + t * 40f;
                    float y = rect.yMax - 9f - t * 22f;
                    Disc(new Rect(x, y, 13f + t * 14f, 9f + t * 9f),
                        new Color(.65f, .67f, .6f, (1f - t) * .23f), true);
                }
            }
            else if (boss.Kind == BossKind.Sentinel)
            {
                if (boss.Y > 4f)
                {
                    float flame = 13f + Mathf.Sin(clock * 27f) * 4f;
                    for (int i = 0; i < 2; i++)
                    {
                        float x = rect.x + rect.width * (.25f + i * .49f);
                        Fill(new Rect(x, rect.yMax - 5f, 14f, flame), Hex(0x58d9e9));
                        Fill(new Rect(x + 4f, rect.yMax - 4f, 6f, flame + 8f), Hex(0xe5fdff));
                    }
                }
                float landing = Mathf.Clamp01(boss.LandingFlashRemaining / .3f);
                if (landing > 0f)
                {
                    float spread = (1f - landing) * 75f;
                    Disc(new Rect(rect.center.x - rect.width * .62f - spread, GroundY - 7f,
                        rect.width * 1.24f + spread * 2f, 18f),
                        new Color(.7f, .85f, .9f, landing * .35f), true);
                    for (int i = 0; i < 8; i++)
                    {
                        float side = i % 2 == 0 ? -1f : 1f;
                        float x = rect.center.x + side * (30f + spread + i * 5f);
                        Fill(new Rect(x, GroundY - 4f - (1f - landing) * (12f + i * 3f), 5f, 3f),
                            new Color(.86f, .91f, .85f, landing * .65f));
                    }
                }
            }
            else
            {
                // Rotor wash changes with the shared flight clock and remains
                // scenery, never an extra invisible collision rectangle.
                for (int i = 0; i < 9; i++)
                {
                    float t = Repeat(clock * .95f + i / 9f, 1f);
                    float x = rect.x + rect.width * (.16f + i % 3 * .27f) + t * 43f;
                    float y = rect.yMax + 8f + t * 72f;
                    Line(new Vector2(x, y), new Vector2(x + 28f + t * 21f, y),
                        new Color(.77f, .9f, .94f, (1f - t) * .16f), 1.5f);
                }
                float flicker = .65f + .35f * Mathf.Sin(clock * 30f);
                Disc(new Rect(rect.xMax - 34f, rect.center.y - 4f, 33f + flicker * 17f, 13f),
                    new Color(.36f, .85f, 1f, .3f + flicker * .2f), true);
            }
        }

        private static void DrawBossMovingDetails(BossEncounter boss, Rect rect, float clock, int rage)
        {
            if (boss.Kind == BossKind.Tank)
            {
                // Tread links slide within the track's existing opaque body.
                float stride = Repeat(clock * (64f + rage * 12f), 18f);
                for (int i = 0; i < 13; i++)
                {
                    float x = rect.x + 15f + Repeat(i * 18f + stride, Mathf.Max(20f, rect.width - 29f));
                    Fill(new Rect(x, rect.yMax - 10f, 8f, 3f), new Color(.72f, .81f, .8f, .7f));
                    Fill(new Rect(x, rect.yMax - 29f, 8f, 2f), new Color(.12f, .2f, .23f, .5f));
                }
            }
            else if (boss.Kind == BossKind.Sentinel)
            {
                float step = Mathf.Sin(clock * (5f + rage));
                for (int i = 0; i < 2; i++)
                {
                    float side = i == 0 ? step : -step;
                    float x = rect.x + rect.width * (.25f + i * .47f);
                    Line(new Vector2(x, rect.yMax - 39f), new Vector2(x + side * 8f, rect.yMax - 16f),
                        new Color(.55f, .84f, .9f, .5f), 2f);
                }
                if (!boss.Vulnerable)
                {
                    float x = rect.x + rect.width * .39f;
                    float y = rect.y + rect.height * .43f;
                    float charge = .4f + Mathf.Sin(clock * 19f) * .15f;
                    Line(new Vector2(x - 13f, y - 15f), new Vector2(x - 4f, y + 1f),
                        new Color(.36f, .85f, 1f, charge), 2f);
                    Line(new Vector2(x - 4f, y + 1f), new Vector2(x + 10f, y - 9f),
                        new Color(.81f, .98f, 1f, charge), 2f);
                }
            }
            else
            {
                float span = rect.width * (.66f + .28f * Mathf.Abs(Mathf.Sin(clock * 54f)));
                float x = rect.x + rect.width * .51f;
                float y = rect.y + rect.height * .055f;
                Line(new Vector2(x - span / 2f, y), new Vector2(x + span / 2f, y),
                    new Color(.8f, .93f, .96f, .73f), 2f);
                Line(new Vector2(x - span * .35f, y + 4f), new Vector2(x + span * .35f, y + 4f),
                    new Color(.6f, .82f, .91f, .26f), 3f);
            }
        }

        private void DrawBossMuzzle(BossEncounter boss, Rect rect, float clock)
        {
            float shot = Mathf.Clamp01(boss.ShotFlashRemaining / .18f);
            if (shot <= 0f) return;
            float x = rect.x + (boss.Kind == BossKind.Gunship ? rect.width * .17f : 3f);
            float y = rect.y + rect.height * (boss.Kind == BossKind.Gunship ? .73f : .44f);
            Disc(new Rect(x - 27f, y - 19f, 47f, 38f), new Color(1f, .53f, .15f, shot * .48f), true);
            Line(new Vector2(x - 29f * shot, y), new Vector2(x + 6f, y),
                new Color(1f, .94f, .63f, shot), 4f);
            Line(new Vector2(x - 17f, y - 12f * shot), new Vector2(x - 8f, y + 10f * shot),
                new Color(1f, .76f, .29f, shot), 2f);
            for (int i = 0; i < 3; i++)
            {
                float t = Repeat(clock * 1.7f + i * .27f, 1f);
                Disc(new Rect(x - 16f - t * 22f, y - 8f - t * 23f, 13f + t * 14f, 12f + t * 9f),
                    new Color(.62f, .67f, .68f, shot * (1f - t) * .26f), true);
            }
        }

        private static void DrawBossImpact(Rect rect, float hit, float clock)
        {
            float age = 1f - hit;
            Vector2 origin = new Vector2(rect.x + rect.width * .14f, rect.y + rect.height * .48f);
            for (int i = 0; i < 8; i++)
            {
                float angle = i * .7853982f + .15f;
                float span = 7f + age * (15f + i % 3 * 6f);
                Vector2 a = new Vector2(origin.x + Mathf.Cos(angle) * span, origin.y + Mathf.Sin(angle) * span);
                Vector2 b = new Vector2(a.x + Mathf.Cos(angle) * 7f, a.y + Mathf.Sin(angle) * 7f);
                Line(a, b, new Color(1f, .87f, .46f, hit), 2f);
            }
        }

        private void DrawBossDestruction(Rect rect, float clock, float remaining)
        {
            float progress = Mathf.Clamp01(1f - remaining / RunnerSession.BossVictoryDuration);
            for (int i = 0; i < 7; i++)
            {
                float t = Repeat(progress * .8f + i * .137f, 1f);
                float x = rect.x + rect.width * (.1f + i % 4 * .23f);
                float y = rect.y + rect.height * (.3f + i % 3 * .18f) - t * 43f;
                Disc(new Rect(x, y, 22f + t * 43f, 20f + t * 34f),
                    new Color(.21f, .26f, .29f, .28f * (1f - t)), true);
                if (i % 2 == 0)
                    Disc(new Rect(x - 3f, y + 11f, 18f, 17f),
                        new Color(1f, .56f, .19f, (1f - progress) * .6f), true);
            }
            for (int i = 0; i < 10; i++)
            {
                float t = Repeat(progress + i * .073f, 1f);
                float x = rect.x + rect.width * .5f + (i - 4.5f) * t * 21f;
                float y = rect.y + rect.height * .45f - Mathf.Sin(t * 3.1415927f) * 49f + t * 65f;
                Fill(new Rect(x, y, 4f, 3f), new Color(1f, .8f, .37f, 1f - progress));
            }
        }

        private static void DrawBossFallback(Rect rect, BossKind kind)
        {
            Fill(rect, Hex(0x3c5363));
            Fill(new Rect(rect.x + 5, rect.y + 5, rect.width - 10, rect.height - 10), Hex(0x72868b));
            if (kind == BossKind.Tank)
            {
                Fill(new Rect(rect.x, rect.yMax - 25, rect.width, 25), Hex(0x203742));
                Fill(new Rect(rect.x, rect.y + rect.height * .35f, rect.width * .7f, 12), Hex(0x354c5b));
            }
            else if (kind == BossKind.Sentinel)
            {
                Fill(new Rect(rect.x + rect.width * .2f, rect.y + 9, rect.width * .6f, 17), Hex(0x263b4c));
                Fill(new Rect(rect.x + rect.width * .28f, rect.y + 15, rect.width * .44f, 5), Hex(0xeab26d));
            }
            else
                Fill(new Rect(rect.x + rect.width * .12f, rect.y + 3, rect.width * .76f, 5), Hex(0xc7d0ce));
        }

        private void DrawBossTelegraph(BossTelegraph telegraph, float clock)
        {
            Rect rect = new Rect(telegraph.X, GroundY - telegraph.Y - telegraph.Height,
                telegraph.Width, telegraph.Height);
            bool active = telegraph.Active;
            bool highLaser = telegraph.Kind == BossAttackKind.LaserHigh;
            Color color = active ? (highLaser ? Hex(0xd480ff) : Hex(0xff765b))
                : highLaser ? Hex(0xd3a2ff) : Hex(0xffce73);
            if (active)
            {
                // The complete live rectangle is drawn, including beam edges.
                Fill(rect, WithAlpha(color, 0.7f));
                if (rect.height <= 35f)
                    Fill(new Rect(rect.x, rect.y + rect.height * .37f, rect.width, Mathf.Max(2, rect.height * .26f)), Hex(0xfff2d1));
                else
                    Fill(new Rect(rect.x + rect.width * .38f, rect.y, Mathf.Max(2, rect.width * .24f), rect.height), Hex(0xfff2d1));
                return;
            }
            float progress = telegraph.Duration <= 0 ? 0 : Mathf.Clamp01(1 - telegraph.Remaining / telegraph.Duration);
            Fill(rect, WithAlpha(color, .06f + progress * .12f));
            float edge = 1.5f + progress;
            Fill(new Rect(rect.x, rect.y, rect.width, edge), WithAlpha(color, .78f));
            Fill(new Rect(rect.x, rect.yMax - edge, rect.width, edge), WithAlpha(color, .78f));
            Fill(new Rect(rect.x, rect.y, edge, rect.height), WithAlpha(color, .78f));
            Fill(new Rect(rect.xMax - edge, rect.y, edge, rect.height), WithAlpha(color, .78f));
            DrawBossWarningGlyphs(telegraph.Kind, rect, color, progress, clock);
        }

        private void DrawBossWarningGlyphs(BossAttackKind kind, Rect rect, Color color, float progress, float clock)
        {
            if (kind == BossAttackKind.Artillery)
            {
                float half = Mathf.Min(10f, rect.width * .2f);
                Line(new Vector2(rect.center.x - half, rect.yMax - 9), new Vector2(rect.center.x + half, rect.yMax - 9), color, 2);
                Line(new Vector2(rect.center.x, rect.yMax - 9 - half), new Vector2(rect.center.x, rect.yMax - 9 + half), color, 2);
                Disc(new Rect(rect.center.x - half * 1.6f, rect.yMax - 14f, half * 3.2f, 9f),
                    WithAlpha(color, .15f + progress * .17f));
            }
            else if (kind == BossAttackKind.Missile)
            {
                float y = rect.y - 21f - (1f - progress) * 7f;
                float x = rect.center.x;
                Line(new Vector2(x - 8f, y), new Vector2(x, y + 11f), color, 2.5f);
                Line(new Vector2(x + 8f, y), new Vector2(x, y + 11f), color, 2.5f);
                Line(new Vector2(x, y - 7f), new Vector2(x, y + 8f), color, 2f);
                Fill(new Rect(rect.x + 5f, rect.yMax - 7f, Mathf.Max(1f, (rect.width - 10f) * progress), 2f), color);
            }
            else if (kind == BossAttackKind.LaserLow || kind == BossAttackKind.LaserHigh)
            {
                float offset = Repeat(clock * 65f, 37f);
                for (float x = rect.x + 7f + offset; x < rect.xMax - 20f; x += 37f)
                {
                    Fill(new Rect(x, rect.center.y - 1f, 14f, 2f), WithAlpha(color, .5f + progress * .3f));
                    Line(new Vector2(x + 6f, rect.y + 2f), new Vector2(x + 11f, rect.yMax - 2f),
                        WithAlpha(color, .6f), 1.5f);
                }
            }
            else if (kind == BossAttackKind.Shockwave)
            {
                for (float x = rect.x + 6f; x < rect.xMax - 24f; x += 30f)
                {
                    float amplitude = Mathf.Min(10f, rect.height * .4f) * (.45f + progress * .55f);
                    Line(new Vector2(x, rect.yMax - 3f), new Vector2(x + 8f, rect.yMax - 3f - amplitude), color, 1.5f);
                    Line(new Vector2(x + 8f, rect.yMax - 3f - amplitude), new Vector2(x + 16f, rect.yMax - 3f), color, 1.5f);
                }
            }
            else if (kind == BossAttackKind.Drone || kind == BossAttackKind.DroneLow)
            {
                float offset = Repeat(clock * 49f, 91f);
                for (float x = rect.x + 12f + offset; x < rect.xMax - 32f; x += 91f)
                {
                    float y = rect.center.y;
                    Fill(new Rect(x, y - 5f, 20f, 10f), WithAlpha(color, .26f + progress * .25f));
                    Fill(new Rect(x + 5f, y - 2f, 8f, 4f), WithAlpha(color, .8f));
                    Line(new Vector2(x - 3f, y - 8f), new Vector2(x + 23f, y - 8f), color, 1.5f);
                }
            }
            else
            {
                // Low shell lanes show a leftward stream before the first shot.
                float offset = Repeat(clock * 82f, 46f);
                for (float x = rect.x + 9f + offset; x < rect.xMax - 21f; x += 46f)
                {
                    float y = rect.center.y;
                    Line(new Vector2(x + 9f, y - 5f), new Vector2(x, y), color, 2f);
                    Line(new Vector2(x, y), new Vector2(x + 9f, y + 5f), color, 2f);
                }
            }
        }

        private static void DrawBossProjectile(BossProjectile projectile)
        {
            Rect rect = new Rect(projectile.X, GroundY - projectile.Y - projectile.Height,
                projectile.Width, projectile.Height);
            Color edge = projectile.Friendly ? Hex(0x8ce7f2) : Hex(0xff8b66);
            Fill(rect, edge);
            if (rect.width > 4 && rect.height > 4)
                Fill(new Rect(rect.x + 2, rect.y + 2, rect.width - 4, rect.height - 4),
                    projectile.Friendly ? Hex(0xe8ffff) : Hex(0xffe3ae));
            if (!projectile.Friendly && (projectile.Kind == BossAttackKind.Drone || projectile.Kind == BossAttackKind.DroneLow))
            {
                float y = rect.y + rect.height * .25f;
                Fill(new Rect(rect.x + 4f, y, rect.width - 8f, 3f), Hex(0x38495a));
                Fill(new Rect(rect.x + rect.width * .22f, rect.y + rect.height * .43f, rect.width * .24f, 4f), Hex(0xff5f61));
                Fill(new Rect(rect.x + rect.width * .58f, rect.y + rect.height * .43f, rect.width * .22f, 4f), Hex(0xffe9bc));
            }
            else if (!projectile.Friendly && (projectile.Kind == BossAttackKind.Missile || projectile.Kind == BossAttackKind.Artillery))
            {
                Fill(new Rect(rect.x + 3f, rect.y + 3f, rect.width - 6f, Mathf.Min(8f, rect.height * .23f)), Hex(0xffecaf));
                Line(new Vector2(rect.x + 4f, rect.yMax - 9f), new Vector2(rect.center.x, rect.yMax - 3f), Hex(0xb34f45), 2f);
                Line(new Vector2(rect.center.x, rect.yMax - 3f), new Vector2(rect.xMax - 4f, rect.yMax - 9f), Hex(0xb34f45), 2f);
            }
        }
    }
}
