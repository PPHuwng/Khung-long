using System;
using UnityEngine;

namespace CityDino
{
    /// <summary>Attack poses, moving mechanisms and energy effects, all on the combat clock.</summary>
    public sealed partial class RunnerWorldView
    {
        private static int BossActionFrame(BossEncounter boss)
        {
            switch (boss.Action)
            {
                case BossAction.Windup: return 1;
                case BossAction.Strike: return 2;
                case BossAction.Recovery: return 3;
                default: return 0;
            }
        }

        private void DrawBossRigAction(BossEncounter boss, Rect body, float clock)
        {
            float progress = Mathf.Clamp01(boss.ActionProgress);
            bool loading = boss.Action == BossAction.Windup;
            bool striking = boss.Action == BossAction.Strike;
            Color energy = boss.Kind == BossKind.Sentinel ? Hex(0x83e8ff) : Hex(0xffc36e);
            if (loading)
            {
                Vector2 hub = new Vector2(body.x + body.width * .28f, body.y + body.height * .47f);
                float radius = 10f + 9f * progress;
                Disc(new Rect(hub.x - radius * 2, hub.y - radius * 2, radius * 4, radius * 4),
                    WithAlpha(energy, .12f + progress * .19f), true);
                for (int i = 0; i < 5; i++)
                {
                    float angle = i * 1.256637f + clock * 2.5f;
                    float reach = 30f - progress * 10f;
                    Vector2 start = new Vector2(hub.x + Mathf.Cos(angle) * reach,
                        hub.y + Mathf.Sin(angle) * reach);
                    Vector2 end = new Vector2(hub.x + Mathf.Cos(angle) * (radius + 3),
                        hub.y + Mathf.Sin(angle) * (radius + 3));
                    Line(start, end, WithAlpha(energy, .45f + progress * .4f), 2f);
                }
            }

            if (boss.Kind == BossKind.Sentinel)
            {
                // The forearm hinge changes with the actual attack state. The
                // rest of the sprite stays inside its live physical body rect.
                Vector2 shoulder = new Vector2(body.x + body.width * .27f, body.y + body.height * .34f);
                Vector2 elbow = new Vector2(body.x + body.width * (loading ? .19f : striking ? .12f : .21f),
                    body.y + body.height * (loading ? .22f : striking ? .46f : .52f));
                Vector2 wrist = new Vector2(body.x + body.width * (striking ? .025f : loading ? .12f : .16f),
                    body.y + body.height * (loading ? .13f : striking ? .48f : .69f));
                if (art.BossActionAtlas == null)
                {
                    Line(shoulder, elbow, Hex(0x385267), 9f);
                    Line(elbow, wrist, Hex(0x7693a4), 12f);
                    Line(elbow, wrist, WithAlpha(energy, .68f), 2.5f);
                    Disc(new Rect(elbow.x - 6, elbow.y - 6, 12, 12), Hex(0x203a4b));
                    Disc(new Rect(elbow.x - 3, elbow.y - 3, 6, 6), energy);
                    if (loading || striking)
                        DrawElectricOrb(new Rect(wrist.x - 11f, wrist.y - 11f, 22f, 22f), clock, .9f);
                }
            }
            else if (boss.Kind == BossKind.Tank)
            {
                float piston = loading ? progress * 6f : striking ? -4f : Mathf.Sin(clock * 7f) * 1.2f;
                float y = body.y + body.height * .46f;
                if (art.BossActionAtlas == null)
                {
                    Line(new Vector2(body.x + body.width * .16f + piston, y),
                        new Vector2(body.x + body.width * .43f, y), Hex(0x314c5a), 8f);
                    Line(new Vector2(body.x + body.width * .18f + piston, y - 2),
                        new Vector2(body.x + body.width * .35f, y - 2), Hex(0x99b7bc), 2f);
                }
                if (striking)
                    for (int i = 0; i < 9; i++)
                    {
                        float age = Repeat(clock * 3.5f + i * .11f, 1f);
                        float x = body.xMax - 12f + age * 90f;
                        Disc(new Rect(x, body.yMax - 13f - age * 16f, 13f + age * 22f, 9f + age * 9f),
                            new Color(.75f, .75f, .62f, .32f * (1f - age)), true);
                    }
            }
            else
            {
                for (int i = 0; i < 2; i++)
                {
                    float x = body.x + body.width * (.35f + i * .36f);
                    float y = body.y + body.height * .75f;
                    float flame = (striking ? 35f : loading ? 18f : 10f) + Mathf.Sin(clock * 33f + i) * 4f;
                    Line(new Vector2(x, y), new Vector2(x + flame, y + (striking ? -9f : 4f)),
                        new Color(.25f, .78f, 1f, .34f), 9f);
                    Line(new Vector2(x, y), new Vector2(x + flame * .7f, y), Hex(0xd3faff), 3f);
                }
            }

            if (boss.ContactActive && boss.ContactWidth > 0f && boss.ContactHeight > 0f)
            {
                // This highlighted strike edge is the damaging shape. Arms,
                // exhaust and afterimages outside it remain decorative.
                Rect strike = new Rect(boss.ContactX, GroundY - boss.ContactY - boss.ContactHeight,
                    boss.ContactWidth, boss.ContactHeight);
                Fill(strike, WithAlpha(energy, .12f));
                Fill(new Rect(strike.x, strike.y, Mathf.Min(7f, strike.width), strike.height), WithAlpha(energy, .74f));
                Line(new Vector2(strike.x + 3, strike.y + 3), new Vector2(strike.x + 3, strike.yMax - 3),
                    Hex(0xf1ffff), 2f);
                for (int i = 0; i < 6; i++)
                {
                    float y = strike.y + (i + .5f) / 6f * strike.height;
                    float length = 18f + Mathf.Sin(clock * 28f + i * 2f) * 5f;
                    Line(new Vector2(strike.x + 4, y), new Vector2(strike.x + length, y - 3f),
                        WithAlpha(energy, .67f), 2.5f);
                }
            }
        }

        private void DrawElectricOrb(Rect rect, float clock, float alpha = 1f)
        {
            Color cyan = Hex(0x66dfff), blue = Hex(0x5083ef), white = Hex(0xe4fcff);
            Rect halo = new Rect(rect.x - rect.width * .43f, rect.y - rect.height * .43f,
                rect.width * 1.86f, rect.height * 1.86f);
            Disc(halo, WithAlpha(cyan, .26f * alpha), true);
            // The solid disc has the same radius as the core's circle contact;
            // the softer halo conveys light without pretending to be metal.
            Disc(rect, WithAlpha(blue, .8f * alpha));
            Disc(new Rect(rect.x + rect.width * .14f, rect.y + rect.height * .14f,
                rect.width * .72f, rect.height * .72f), WithAlpha(cyan, .84f * alpha));
            Disc(new Rect(rect.center.x - rect.width * .19f, rect.center.y - rect.height * .19f,
                rect.width * .38f, rect.height * .38f), WithAlpha(white, alpha));
            float radius = rect.width * .45f;
            for (int i = 0; i < 6; i++)
            {
                float angle = i * 1.0471976f + clock * 3.5f;
                float jitter = Mathf.Sin(clock * 35f + i * 2.7f) * .22f;
                Vector2 a = new Vector2(rect.center.x + Mathf.Cos(angle) * radius,
                    rect.center.y + Mathf.Sin(angle) * radius);
                Vector2 b = new Vector2(rect.center.x + Mathf.Cos(angle + jitter + .35f) * radius * .63f,
                    rect.center.y + Mathf.Sin(angle + jitter + .35f) * radius * .63f);
                Vector2 c = new Vector2(rect.center.x + Mathf.Cos(angle + .13f) * radius * .24f,
                    rect.center.y + Mathf.Sin(angle + .13f) * radius * .24f);
                Line(a, b, WithAlpha(white, alpha), 1.8f);
                Line(b, c, WithAlpha(white, alpha * .88f), 1.4f);
            }
        }

        private void DrawRobotShockwave(Rect rect, float clock)
        {
            Color blue = Hex(0x78dfff);
            Fill(rect, WithAlpha(blue, .23f));
            Disc(new Rect(rect.x - 8, rect.y - 9, rect.width + 16, rect.height + 18),
                WithAlpha(blue, .23f), true);
            for (int i = 0; i < 3; i++)
            {
                float shift = Repeat(clock * 75f + i * rect.width / 3f, rect.width);
                float x = rect.x + shift;
                float height = Mathf.Min(rect.height - 3f, 12f + Mathf.Sin(clock * 19f + i * 2f) * 7f);
                Vector2 root = new Vector2(x, rect.yMax - 2f);
                Vector2 knee = new Vector2(Mathf.Max(rect.x + 1, x - 6), rect.yMax - height * .57f);
                Vector2 tip = new Vector2(Mathf.Min(rect.xMax - 1, x + 2), rect.yMax - height);
                Line(root, knee, WithAlpha(blue, .42f), 6f);
                Line(knee, tip, WithAlpha(blue, .42f), 6f);
                Line(root, knee, Hex(0xebffff), 1.8f);
                Line(knee, tip, Hex(0xebffff), 1.8f);
            }
            Line(new Vector2(rect.x, rect.yMax - 1), new Vector2(rect.xMax, rect.yMax - 1), blue, 2f);
            Line(new Vector2(rect.x, rect.y + 1), new Vector2(rect.xMax, rect.y + 1), WithAlpha(blue, .56f), 1f);
        }

        private void DrawEnergyPillar(Rect rect, float clock, float alpha, float flash)
        {
            Color cyan = Color.Lerp(Hex(0x7cbdff), Color.white, flash);
            Fill(rect, WithAlpha(cyan, .18f * alpha));
            float emitter = Mathf.Min(10f, rect.height * .12f);
            Fill(new Rect(rect.x, rect.yMax - emitter, rect.width, emitter), WithAlpha(Hex(0x334e63), alpha));
            for (int branch = 0; branch < 3; branch++)
            {
                Vector2 previous = new Vector2(rect.x + rect.width * (.2f + branch * .3f), rect.yMax - emitter);
                int segments = Math.Max(3, (int)(rect.height / 17f));
                for (int i = 1; i <= segments; i++)
                {
                    float y = rect.yMax - emitter - (rect.height - emitter) * i / segments;
                    float centre = rect.x + rect.width * (.2f + branch * .3f);
                    float x = Mathf.Clamp(centre + Mathf.Sin(clock * 27f + i * 3.7f + branch) * rect.width * .16f,
                        rect.x + 2f, rect.xMax - 2f);
                    Vector2 next = new Vector2(x, y);
                    Line(previous, next, WithAlpha(cyan, .35f * alpha), 7f);
                    Line(previous, next, WithAlpha(Hex(0xeafaff), .93f * alpha), 2f);
                    previous = next;
                }
            }
            Line(new Vector2(rect.x + 1, rect.y), new Vector2(rect.x + 1, rect.yMax), WithAlpha(cyan, .48f * alpha), 1f);
            Line(new Vector2(rect.xMax - 1, rect.y), new Vector2(rect.xMax - 1, rect.yMax), WithAlpha(cyan, .48f * alpha), 1f);
        }

        private bool DrawBossSpecialWarning(BossTelegraph mark, float clock, Color color)
        {
            if (mark.Active) return false;
            Rect rect = new Rect(mark.X, GroundY - mark.Y - mark.Height, mark.Width, mark.Height);
            if (mark.Kind == BossAttackKind.TankRam || mark.Kind == BossAttackKind.RobotDash || mark.Kind == BossAttackKind.GunshipDive)
            {
                if (mark.FullSweep) color = Hex(0xff9879);
                DrawBossWarningRect(rect, color, mark);
                float offset = Repeat(clock * 155f, 74f);
                for (float x = rect.x + 12f + offset; x < rect.xMax - 22f; x += 74f)
                {
                    float y = rect.center.y;
                    Line(new Vector2(x + 12f, y - 9f), new Vector2(x, y), WithAlpha(color, .8f), 3f);
                    Line(new Vector2(x, y), new Vector2(x + 12f, y + 9f), WithAlpha(color, .8f), 3f);
                    if (mark.FullSweep)
                    {
                        Line(new Vector2(x + 23f, y - 9f), new Vector2(x + 11f, y), WithAlpha(color, .65f), 2f);
                        Line(new Vector2(x + 11f, y), new Vector2(x + 23f, y + 9f), WithAlpha(color, .65f), 2f);
                    }
                }
                // A stop bracket means a partial lunge. A double arrow at the
                // screen edge means the body will cross the entire left side;
                // the random destination is already locked in this cue.
                float stopX = Mathf.Clamp(mark.SweepEndX + 8f, 24f, Width - 24f);
                float centreY = rect.center.y;
                if (mark.FullSweep)
                {
                    Line(new Vector2(58f, centreY), new Vector2(14f, centreY), color, 4f);
                    for (int i = 0; i < 2; i++)
                    {
                        float x = 14f + i * 14f;
                        Line(new Vector2(x + 10f, centreY - 12f), new Vector2(x, centreY), color, 3f);
                        Line(new Vector2(x, centreY), new Vector2(x + 10f, centreY + 12f), color, 3f);
                    }
                }
                else
                {
                    float half = Mathf.Min(19f, rect.height * .36f);
                    Line(new Vector2(stopX, centreY - half), new Vector2(stopX, centreY + half), color, 3f);
                    Line(new Vector2(stopX, centreY - half), new Vector2(stopX + 11f, centreY - half), color, 3f);
                    Line(new Vector2(stopX, centreY + half), new Vector2(stopX + 11f, centreY + half), color, 3f);
                    Disc(new Rect(stopX - 4f, centreY - 4f, 8f, 8f), WithAlpha(color, .65f));
                }
                return true;
            }
            if (mark.Kind == BossAttackKind.CrossMissile)
            {
                DrawBossWarningRect(rect, color, mark);
                float sign = mark.VelocityX < 0f ? -1f : 1f;
                float offset = Repeat(clock * 115f, 80f);
                for (float x = rect.x + 24f + offset; x < rect.xMax - 24f; x += 80f)
                {
                    Vector2 tip = new Vector2(x + sign * 10f, rect.center.y);
                    Line(new Vector2(x - sign * 7f, tip.y - 6f), tip, WithAlpha(color, .88f), 2f);
                    Line(new Vector2(x - sign * 7f, tip.y + 6f), tip, WithAlpha(color, .88f), 2f);
                }
                float edgeX = Mathf.Clamp(mark.OriginX, 25f, Width - 25f);
                float edgeY = GroundY - mark.OriginY - mark.Height * .5f;
                Disc(new Rect(edgeX - 15f, edgeY - 15f, 30f, 30f), WithAlpha(color, .35f), true);
                Line(new Vector2(edgeX - sign * 8f, edgeY - 9f), new Vector2(edgeX + sign * 7f, edgeY), color, 3f);
                Line(new Vector2(edgeX - sign * 8f, edgeY + 9f), new Vector2(edgeX + sign * 7f, edgeY), color, 3f);
                return true;
            }
            if (mark.Kind == BossAttackKind.MissileRain)
            {
                DrawBossWarningRect(rect, color, mark);
                float progress = mark.Duration <= 0f ? 0f : Mathf.Clamp01(1f - mark.Remaining / mark.Duration);
                // Each missile owns this actual vertical strip. The common
                // green corridor is marked only along its edges, never filled
                // with a danger colour or covered by an invisible hitbox.
                if (mark.GapWidth > 0f)
                {
                    float top = Mathf.Max(211f, rect.y);
                    Color safe = new Color(.55f, 1f, .81f, .5f);
                    Line(new Vector2(mark.GapX, top), new Vector2(mark.GapX, GroundY), safe, 1.5f);
                    Line(new Vector2(mark.GapX + mark.GapWidth, top), new Vector2(mark.GapX + mark.GapWidth, GroundY), safe, 1.5f);
                }
                float offset = Repeat(clock * 85f, 76f);
                for (float y = rect.y + 19f + offset; y < rect.yMax - 18f; y += 76f)
                {
                    float x = rect.center.x;
                    Line(new Vector2(x - 8f, y), new Vector2(x, y + 10f), WithAlpha(color, .63f + progress * .3f), 2f);
                    Line(new Vector2(x + 8f, y), new Vector2(x, y + 10f), WithAlpha(color, .63f + progress * .3f), 2f);
                }
                return true;
            }
            if (mark.Kind == BossAttackKind.ElectricOrb)
            {
                Color electric = Hex(0x92eaff);
                DrawBossWarningRect(rect, electric, mark);
                for (float x = rect.x + 16f; x < rect.xMax - 21f; x += 83f)
                {
                    Vector2 a = new Vector2(x + 11f, rect.center.y - 9f);
                    Vector2 b = new Vector2(x + 3f, rect.center.y + 1f);
                    Vector2 c = new Vector2(x + 13f, rect.center.y - 1f);
                    Vector2 d = new Vector2(x + 5f, rect.center.y + 9f);
                    Line(a, b, electric, 2f); Line(b, c, electric, 2f); Line(c, d, electric, 2f);
                }
                return true;
            }
            return false;
        }

        private bool DrawOrdnanceSprite(Rect rect, int cell, Color tint, float angle = 0f)
        {
            if (art.OrdnanceAtlas == null) return false;
            Color previousColor = GUI.color;
            Matrix4x4 canvas = GUI.matrix;
            try
            {
                GUI.color = tint;
                if (Mathf.Abs(angle) > .01f)
                {
                    GUI.matrix = canvas * Matrix4x4.TRS(new Vector3(rect.center.x, rect.center.y, 0f),
                        Quaternion.Euler(0f, 0f, angle), Vector3.one);
                    rect = new Rect(-rect.width * .5f, -rect.height * .5f, rect.width, rect.height);
                }
                GUI.DrawTextureWithTexCoords(rect, art.OrdnanceAtlas, art.GetOrdnanceUV(cell), true);
            }
            finally { GUI.matrix = canvas; GUI.color = previousColor; }
            return true;
        }

        private static Vector2 ProjectileDirection(BossProjectile projectile)
        {
            float length = Mathf.Sqrt(projectile.VelocityX * projectile.VelocityX + projectile.VelocityY * projectile.VelocityY);
            return length > .01f ? new Vector2(projectile.VelocityX / length, -projectile.VelocityY / length)
                : new Vector2(-1f, 0f);
        }

        private void DrawArmoredShell(BossProjectile projectile, Rect rect, float clock)
        {
            Vector2 direction = ProjectileDirection(projectile);
            float radius = projectile.Radius > 0f ? projectile.Radius : Mathf.Min(rect.width, rect.height) * .45f;
            Rect core = new Rect(rect.center.x - radius, rect.center.y - radius, radius * 2f, radius * 2f);
            Vector2 tail = rect.center - direction * radius * .8f;
            float trail = 15f + Mathf.Sin(clock * 31f + projectile.Id) * 4f;
            Line(tail - direction * trail, tail, new Color(1f, .47f, .19f, .32f), radius * .8f);
            Line(tail - direction * trail * .65f, tail, Hex(0xffecc0), Mathf.Max(1f, radius * .2f));
            Disc(core, Hex(0x5d7683));
            // The collision is a round metal core. Nose, fins and the trail
            // outside this disc are deliberately forgiving decoration.
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 180f;
            // Use the shell's long side profile rather than stretching it to
            // the old box. Only the clearly drawn circular metal core hits;
            // the pointed nose, fins and exhaust are forgiving decoration.
            Rect shell = new Rect(rect.center.x - radius * 2.05f, rect.center.y - radius * .74f,
                radius * 4.1f, radius * 1.48f);
            if (!DrawOrdnanceSprite(shell, 0, Color.white, angle))
                DrawDirectionalMissile(projectile, rect, clock);
            Disc(new Rect(rect.center.x - radius * .32f, rect.center.y - radius * .32f,
                radius * .64f, radius * .64f), Hex(0xffbe6f));
        }

        private void DrawPlasmaBolt(BossProjectile projectile, Rect rect, float clock)
        {
            Vector2 direction = ProjectileDirection(projectile);
            float radius = projectile.Radius > 0f ? projectile.Radius : Mathf.Min(rect.width, rect.height) * .5f;
            Rect core = new Rect(rect.center.x - radius, rect.center.y - radius, radius * 2f, radius * 2f);
            float pulse = .8f + .16f * Mathf.Sin(clock * 32f + projectile.Id);
            Vector2 tail = rect.center - direction * radius;
            Line(tail - direction * (16f + pulse * 8f), tail, new Color(.25f, .75f, 1f, .27f), radius * 1.6f);
            Line(tail - direction * 15f, tail, Hex(0xb3f4ff), Mathf.Max(1f, radius * .36f));
            Disc(new Rect(core.x - radius, core.y - radius, core.width + radius * 2f, core.height + radius * 2f),
                new Color(.3f, .79f, 1f, .23f), true);
            Disc(core, Hex(0x489bd8));
            Disc(new Rect(rect.center.x - radius * .58f, rect.center.y - radius * .58f,
                radius * 1.16f, radius * 1.16f), Hex(0xbef7ff));
            Disc(new Rect(rect.center.x - radius * .26f, rect.center.y - radius * .26f,
                radius * .52f, radius * .52f), Color.white);
        }

        private void DrawPulsePellet(Rect rect, BossProjectile projectile, float clock)
        {
            Vector2 direction = ProjectileDirection(projectile);
            Line(rect.center - direction * 12f, rect.center, new Color(.47f, .89f, 1f, .35f), rect.height * .72f);
            Disc(rect, Hex(0x71e0f0));
            Disc(new Rect(rect.x + rect.width * .2f, rect.y + rect.height * .2f,
                rect.width * .6f, rect.height * .6f), Hex(0xf0ffff));
        }

        private void DrawWingDrone(Rect rect, float clock)
        {
            Color metal = Hex(0x426375), light = Hex(0x8ce6f8);
            Disc(new Rect(rect.x + rect.width * .08f, rect.y + rect.height * .23f,
                rect.width * .84f, rect.height * .65f), metal);
            for (int i = 0; i < 2; i++)
            {
                float x = rect.x + rect.width * (.2f + i * .6f);
                Disc(new Rect(x - rect.width * .17f, rect.y, rect.width * .34f, rect.height * .4f), Hex(0x243d4f));
                float blade = rect.width * (.13f + .035f * Mathf.Sin(clock * 45f + i));
                Line(new Vector2(x - blade, rect.y + rect.height * .16f),
                    new Vector2(x + blade, rect.y + rect.height * .16f), light, 2f);
                Line(new Vector2(x, rect.yMax - 4f), new Vector2(x, rect.yMax + 8f + Mathf.Sin(clock * 31f) * 2f),
                    new Color(.37f, .86f, 1f, .5f), 3f);
            }
            Disc(new Rect(rect.center.x - 5f, rect.center.y - 3f, 10f, 8f), Hex(0xffb874));
        }

        private void DrawAssaultDrone(BossObstacle obstacle, Rect rect, float clock, float alpha, float flash)
        {
            float radius = obstacle.Radius > 0f ? obstacle.Radius : Mathf.Min(rect.width, rect.height) * .5f;
            Rect shield = new Rect(rect.center.x - radius, rect.center.y - radius, radius * 2f, radius * 2f);
            Color tint = Color.Lerp(Color.white, Hex(0xffecc3), flash);
            Disc(shield, new Color(.23f, .62f, .8f, .19f * alpha));
            bool hasDroneArt = DrawOrdnanceSprite(rect, 1, WithAlpha(tint, alpha));
            if (!hasDroneArt)
            {
                // Rounded shield and articulated side engines replace the
                // former wooden crate even while the atlas is importing.
                Disc(new Rect(rect.x + rect.width * .12f, rect.y + rect.height * .17f,
                    rect.width * .76f, rect.height * .68f), WithAlpha(Hex(0x3d5b71), alpha));
                Disc(new Rect(rect.x + rect.width * .24f, rect.y + rect.height * .29f,
                    rect.width * .52f, rect.height * .42f), WithAlpha(Hex(0x7998a7), alpha));
                for (int i = 0; i < 2; i++)
                {
                    float x = rect.x + rect.width * (.12f + i * .76f);
                    Line(new Vector2(x, rect.y + rect.height * .35f), new Vector2(x, rect.y + rect.height * .71f),
                        WithAlpha(Hex(0x243e52), alpha), 8f);
                }
            }
            if (!hasDroneArt)
            {
                float eye = 5f + Mathf.Sin(clock * 7f + obstacle.Id) * .6f;
                Disc(new Rect(rect.center.x - eye, rect.y + rect.height * .37f - eye, eye * 2f, eye * 2f),
                    WithAlpha(Hex(0xffbc75), alpha));
            }
            for (int i = 0; i < 2; i++)
            {
                float x = rect.x + rect.width * (.2f + i * .6f);
                float exhaust = 9f + Mathf.Sin(clock * 28f + i) * 3f;
                Line(new Vector2(x, rect.yMax - 5f), new Vector2(x + 6f, rect.yMax - 5f + exhaust),
                    WithAlpha(Hex(0x76dbf6), alpha * .65f), 4f);
            }
            for (int i = 0; i < 6; i++)
            {
                float angle = clock * 2.2f + i * 1.0471976f;
                float x = rect.center.x + Mathf.Cos(angle) * radius * .87f;
                float y = rect.center.y + Mathf.Sin(angle) * radius * .87f;
                Disc(new Rect(x - 2f, y - 2f, 4f, 4f), WithAlpha(Hex(0x95e8ff), alpha * .75f));
            }
        }

        private void DrawPlasmaGate(Rect rect, float clock, float alpha, float flash)
        {
            Color cyan = Color.Lerp(Hex(0x6eddfa), Color.white, flash);
            // The pale field spans exactly the damaging rectangular bar; the
            // opening between the two bars is never painted or bridged.
            Fill(rect, WithAlpha(cyan, alpha * .13f));
            float nodeSize = Mathf.Min(rect.width * .74f, Mathf.Max(10f, rect.height * .38f));
            for (int i = 0; i < 2; i++)
            {
                float y = i == 0 ? rect.y : rect.yMax - nodeSize;
                Rect node = new Rect(rect.center.x - nodeSize * .5f, y, nodeSize, nodeSize);
                Disc(node, WithAlpha(Hex(0x29495c), alpha));
                DrawOrdnanceSprite(node, 3, WithAlpha(Color.white, alpha));
                Disc(new Rect(node.center.x - nodeSize * .19f, node.center.y - nodeSize * .19f,
                    nodeSize * .38f, nodeSize * .38f), WithAlpha(cyan, alpha));
            }
            for (int branch = 0; branch < 3; branch++)
            {
                Vector2 previous = new Vector2(rect.x + rect.width * (.18f + branch * .32f), rect.y);
                int count = Math.Max(2, (int)(rect.height / 13f));
                for (int segment = 1; segment <= count; segment++)
                {
                    float y = rect.y + rect.height * segment / count;
                    float x = rect.x + rect.width * (.18f + branch * .32f) +
                        Mathf.Sin(clock * 29f + segment * 3.2f + branch) * rect.width * .12f;
                    x = Mathf.Clamp(x, rect.x + 2f, rect.xMax - 2f);
                    Vector2 next = new Vector2(x, y);
                    Line(previous, next, WithAlpha(cyan, alpha * .29f), 7f);
                    Line(previous, next, WithAlpha(Hex(0xe3fcff), alpha * .91f), 1.8f);
                    previous = next;
                }
            }
            Line(new Vector2(rect.x + 1f, rect.y), new Vector2(rect.x + 1f, rect.yMax), WithAlpha(cyan, alpha * .6f), 1f);
            Line(new Vector2(rect.xMax - 1f, rect.y), new Vector2(rect.xMax - 1f, rect.yMax), WithAlpha(cyan, alpha * .6f), 1f);
        }

        private void DrawDirectionalMissile(BossProjectile missile, Rect rect, float clock)
        {
            float magnitude = Mathf.Sqrt(missile.VelocityX * missile.VelocityX + missile.VelocityY * missile.VelocityY);
            Vector2 direction = magnitude > 0f
                ? new Vector2(missile.VelocityX / magnitude, -missile.VelocityY / magnitude) : new Vector2(-1f, 0f);
            Vector2 side = new Vector2(-direction.y, direction.x);
            float length = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? rect.width : rect.height;
            float thickness = Mathf.Abs(direction.x) > Mathf.Abs(direction.y) ? rect.height : rect.width;
            Vector2 centre = rect.center;
            Vector2 nose = centre + direction * (length * .46f);
            Vector2 tail = centre - direction * (length * .42f);
            float flame = 13f + 5f * Mathf.Sin(clock * 39f + missile.Id);
            // Exhaust trails backwards for both front and rear attacks; the
            // metal nose always points along the simulated velocity.
            Line(tail - direction * flame, tail, new Color(1f, .4f, .14f, .43f), thickness * .75f);
            Line(tail - direction * flame * .6f, tail, Hex(0xfff1a8), thickness * .28f);
            Line(tail, nose - direction * (length * .19f), Hex(0x354e62), thickness * .74f);
            Line(tail + side * thickness * .13f, nose - direction * (length * .2f) + side * thickness * .13f,
                Hex(0xb3d1dc), thickness * .35f);
            Line(nose - direction * (length * .28f), nose, Hex(0xf47968), thickness * .55f);
            Line(tail + direction * length * .2f, tail - side * thickness * .46f, Hex(0x8aafc2), 3f);
            Line(tail + direction * length * .2f, tail + side * thickness * .46f, Hex(0x8aafc2), 3f);
        }
    }
}
