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
                DrawBossTelegraph(telegraph, clock);
            if (boss != null) DrawBossBody(boss, session.ArenaPhase);
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

        private void DrawBossBody(BossEncounter boss, BossPhase phase)
        {
            Rect rect = new Rect(boss.X, GroundY - boss.Y - boss.Height, boss.Width, boss.Height);
            GroundShadow(boss.X, boss.Width);
            Color tint = phase == BossPhase.Victory ? new Color(0.48f, 0.51f, 0.53f, 0.75f) : Color.white;
            GUI.color = tint;
            if (art.BossAtlas != null)
                GUI.DrawTextureWithTexCoords(rect, art.BossAtlas, art.GetBossUV(boss.Kind), true);
            else DrawBossFallback(rect, boss.Kind);
            GUI.color = Color.white;
            if (phase == BossPhase.Victory) return;

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

        private static void DrawBossTelegraph(BossTelegraph telegraph, float clock)
        {
            Rect rect = new Rect(telegraph.X, GroundY - telegraph.Y - telegraph.Height,
                telegraph.Width, telegraph.Height);
            bool active = telegraph.Active;
            Color color = active ? Hex(0xff765b) : Hex(0xffce73);
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
            if (rect.width < 200f)
            {
                float half = Mathf.Min(10f, rect.width * .2f);
                Line(new Vector2(rect.center.x - half, rect.yMax - 9), new Vector2(rect.center.x + half, rect.yMax - 9), color, 2);
                Line(new Vector2(rect.center.x, rect.yMax - 9 - half), new Vector2(rect.center.x, rect.yMax - 9 + half), color, 2);
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
        }
    }
}
