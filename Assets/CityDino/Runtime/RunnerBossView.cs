using System;
using UnityEngine;

namespace CityDino
{
    /// <summary>Combat scenery and shared live geometry. Arena effects never run the weather or scrolling world.</summary>
    public sealed partial class RunnerWorldView
    {
        private void DrawBossArena(RunnerSession session)
        {
            if (session.ArenaPhase == BossPhase.Intro || session.ArenaPhase == BossPhase.Victory)
            {
                DrawBossTransition(session);
                return;
            }
            BossEncounter boss = session.CurrentBoss;
            BossKind kind = boss == null ? BossKind.Tank : boss.Kind;
            float clock = session.BossElapsed;
            DrawArenaBackdrop(kind, clock);
            gaps.Clear();
            foreach (BossTelegraph telegraph in session.BossTelegraphs)
                if (telegraph.Visible) DrawBossTelegraph(telegraph, clock, boss);
            foreach (BossObstacle obstacle in session.BossObstacles)
                if (!obstacle.Removed) DrawBossObstacle(obstacle, clock);
            if (boss != null) DrawBossBody(boss, session.ArenaPhase, clock, session.BossPhaseRemaining);
            DrawDino(session, session.PlayerAnimationTime);
            foreach (BossProjectile projectile in session.BossProjectiles)
                if (!projectile.Removed) DrawBossProjectile(projectile, clock);
            GUI.color = Color.white;
        }

        private void DrawBossBody(BossEncounter boss, BossPhase phase, float clock, float phaseRemaining)
        {
            if (boss.Kind == BossKind.Submarine)
            {
                DrawSubmarineBody(boss, phase, clock, phaseRemaining);
                return;
            }
            Rect rect = new Rect(boss.X, GroundY - boss.Y - boss.Height, boss.Width, boss.Height);
            if (boss.Kind != BossKind.Gunship) GroundShadow(boss.X, boss.Width);
            int rage = Math.Max(1, Math.Min(3, boss.RageLevel));
            bool defeated = phase == BossPhase.Victory;
            DrawBossMechanicalEffects(boss, rect, clock, defeated);
            float hit = Mathf.Clamp01(boss.HitFlashRemaining / .14f);
            // Only tint the one body image. Its rectangle and matrix always
            // remain the live physical body, including the core's recoil.
            Color tint = defeated ? new Color(0.48f, 0.51f, 0.53f, 0.75f)
                : Color.Lerp(Color.white, new Color(1f, .55f, .35f, 1f), hit);
            GUI.color = tint;
            if (art.BossActionAtlas != null)
                GUI.DrawTextureWithTexCoords(rect, art.BossActionAtlas, art.GetBossActionUV(boss.Kind, BossActionFrame(boss)), true);
            else if (art.BossAtlas != null)
                GUI.DrawTextureWithTexCoords(rect, art.BossAtlas, art.GetBossUV(boss.Kind), true);
            else DrawBossFallback(rect, boss.Kind);
            GUI.color = Color.white;
            if (defeated)
            {
                DrawBossDestruction(rect, clock, phaseRemaining);
                return;
            }

            DrawBossMovingDetails(boss, rect, clock, rage);
            DrawBossRigAction(boss, rect, clock);
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
            float defeatRemaining = Mathf.Max(0f, remaining - (RunnerSession.BossVictoryDuration - 1.6f));
            float progress = Mathf.Clamp01(1f - defeatRemaining / 1.6f);
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

        private void DrawBossObstacle(BossObstacle obstacle, float clock)
        {
            if (DrawUnderwaterObstacle(obstacle, clock)) return;
            Rect rect = new Rect(obstacle.X, GroundY - obstacle.Y - obstacle.Height,
                obstacle.Width, obstacle.Height);
            float flash = Mathf.Clamp01(obstacle.HitFlashRemaining / .12f);
            float alpha = obstacle.Active ? 1f : .25f;
            Color metal = Color.Lerp(Hex(0x365065), Hex(0xffecb8), flash);
            Color light = Color.Lerp(Hex(0x8fe5f4), Color.white, flash);
            if (obstacle.Kind == BossAttackKind.CrateDrop)
                DrawAssaultDrone(obstacle, rect, clock, alpha, flash);
            else if (obstacle.Radius > 0f)
            {
                // Round mines use the same full disc as CircleTouchesPlayer;
                // decorative lights stay inside that physical silhouette.
                Disc(rect, WithAlpha(metal, alpha));
                float spin = clock * (obstacle.Kind == BossAttackKind.MineDrop ? 140f : 75f) + obstacle.Id * 11f;
                bool hasMineArt = DrawOrdnanceSprite(rect, 2,
                    WithAlpha(Color.Lerp(Color.white, Hex(0xffecb8), flash), alpha), spin);
                if (!hasMineArt)
                {
                    Rect core = new Rect(rect.x + rect.width * .16f, rect.y + rect.height * .16f,
                        rect.width * .68f, rect.height * .68f);
                    Disc(core, WithAlpha(Hex(0x93b2bb), alpha));
                }
                Disc(new Rect(rect.center.x - 5f, rect.center.y - 5f, 10f, 10f),
                    WithAlpha(Hex(0xff9765), alpha));
                for (int i = 0; i < 4; i++)
                {
                    float angle = clock * 3f + i * 1.5707963f;
                    float x = rect.center.x + Mathf.Cos(angle) * rect.width * .32f;
                    float y = rect.center.y + Mathf.Sin(angle) * rect.height * .32f;
                    Disc(new Rect(x - 2f, y - 2f, 4f, 4f), WithAlpha(light, alpha));
                }
            }
            else if (obstacle.Kind == BossAttackKind.ShieldDrone)
            {
                // A suspended turbine and plasma shield, rather than a flat
                // plastic crate. The faint complete field still marks its
                // rectangular contact footprint honestly.
                Fill(rect, WithAlpha(light, .12f * alpha));
                Disc(new Rect(rect.x + 3f, rect.y + 4f, rect.width - 6f, rect.height - 8f), WithAlpha(metal, alpha));
                float size = Mathf.Min(rect.height - 10f, rect.width * .57f);
                DrawElectricOrb(new Rect(rect.center.x - size * .5f, rect.center.y - size * .5f, size, size), clock, alpha);
                for (int i = 0; i < 2; i++)
                {
                    float x = i == 0 ? rect.x + 2f : rect.xMax - 2f;
                    Line(new Vector2(x, rect.y + 7f), new Vector2(x, rect.yMax - 7f), WithAlpha(light, .8f * alpha), 3f);
                }
            }
            else if (obstacle.Kind == BossAttackKind.ElectricPillar)
                DrawEnergyPillar(rect, clock, alpha, flash);
            else if (obstacle.Kind == BossAttackKind.AirGate)
                DrawPlasmaGate(rect, clock, alpha, flash);
            else
            {
                // Electric pillars and gate bars fill the complete live
                // rectangle. There are no damaging invisible outer borders.
                Fill(rect, WithAlpha(metal, alpha));
                float inset = Mathf.Min(6f, rect.width * .15f);
                Fill(new Rect(rect.x + inset, rect.y, Mathf.Max(1f, rect.width - inset * 2f), rect.height),
                    WithAlpha(obstacle.Kind == BossAttackKind.AirGate ? Hex(0x70dbe8) : Hex(0xa6b5ff), alpha * .75f));
                for (float y = rect.y + 6f; y < rect.yMax - 4f; y += 21f)
                {
                    float wave = Mathf.Sin(clock * 12f + y * .13f) * 3f;
                    Line(new Vector2(rect.x + inset + 2f, y),
                        new Vector2(rect.xMax - inset - 2f, Mathf.Min(rect.yMax - 2f, y + 9f + wave)),
                        WithAlpha(Hex(0xe4fcff), alpha * .8f), 2f);
                }
                Fill(new Rect(rect.x, rect.y, rect.width, Mathf.Min(5f, rect.height)), WithAlpha(light, alpha));
                Fill(new Rect(rect.x, rect.yMax - Mathf.Min(5f, rect.height), rect.width, Mathf.Min(5f, rect.height)), WithAlpha(light, alpha));
            }
            if (obstacle.Destructible && obstacle.Health < obstacle.MaxHealth)
            {
                Fill(new Rect(rect.x, rect.y - 6f, rect.width, 3f), Hex(0x213b4c));
                Fill(new Rect(rect.x, rect.y - 6f, rect.width * Mathf.Clamp01(obstacle.Health / obstacle.MaxHealth), 3f), Hex(0xffd285));
            }
        }

        private void DrawBossTelegraph(BossTelegraph telegraph, float clock, BossEncounter boss)
        {
            if (DrawUnderwaterTelegraph(telegraph, clock)) return;
            Rect rect = new Rect(telegraph.X, GroundY - telegraph.Y - telegraph.Height,
                telegraph.Width, telegraph.Height);
            bool highLaser = telegraph.Kind == BossAttackKind.LaserHigh;
            Color color = telegraph.Active ? (highLaser ? Hex(0xd480ff) : Hex(0xff765b))
                : highLaser ? Hex(0xd3a2ff) : Hex(0xffce73);
            if (DrawBossSpecialWarning(telegraph, clock, color, boss)) return;
            if (telegraph.Active)
            {
                // Active beams/blasts retain their complete real damage geometry.
                // A short launch icon must never replace a live hitbox's artwork.
                Fill(rect, WithAlpha(color, .7f));
                if (rect.height <= 35f)
                    Fill(new Rect(rect.x, rect.y + rect.height * .37f, rect.width, Mathf.Max(2, rect.height * .26f)), Hex(0xfff2d1));
                else
                    Fill(new Rect(rect.x + rect.width * .38f, rect.y, Mathf.Max(2, rect.width * .24f), rect.height), Hex(0xfff2d1));
                return;
            }
            if (telegraph.Kind == BossAttackKind.LaserLow || telegraph.Kind == BossAttackKind.LaserHigh ||
                telegraph.Kind == BossAttackKind.AirLaser)
            {
                DrawBossLaserWarning(rect, color, telegraph);
                return;
            }
            if (telegraph.Kind == BossAttackKind.AirGate)
            {
                // Small source lamps identify the two emitters without drawing
                // their complete future route or outlining the safe opening.
                float lower = Mathf.Clamp(telegraph.GapY, telegraph.Y, telegraph.Y + telegraph.Height);
                float upper = Mathf.Clamp(telegraph.GapY + telegraph.GapHeight, lower, telegraph.Y + telegraph.Height);
                float sourceX = boss != null ? boss.X + 2f : rect.center.x;
                if (lower > telegraph.Y)
                    DrawBossLocalWarning(new Vector2(sourceX, GroundY - (telegraph.Y + lower) * .5f),
                        new Vector2(-1f, 0f), color, telegraph);
                if (upper < telegraph.Y + telegraph.Height)
                    DrawBossLocalWarning(new Vector2(sourceX, GroundY - (upper + telegraph.Y + telegraph.Height) * .5f),
                        new Vector2(-1f, 0f), color, telegraph);
                return;
            }
            Vector2 origin;
            Vector2 direction = new Vector2(-1f, 0f);
            if (telegraph.OriginX != 0f || telegraph.OriginY != 0f)
            {
                float halfWidth = telegraph.Kind == BossAttackKind.Cannon ? 15f : 9f;
                float halfHeight = telegraph.Kind == BossAttackKind.Cannon ? 11f : 6f;
                origin = new Vector2(telegraph.OriginX + halfWidth, GroundY - telegraph.OriginY - halfHeight);
                direction = new Vector2(telegraph.VelocityX, -telegraph.VelocityY);
            }
            else if (telegraph.Kind == BossAttackKind.Artillery || telegraph.Kind == BossAttackKind.Missile)
            {
                origin = new Vector2(rect.center.x, 150f);
                direction = new Vector2(0f, 1f);
            }
            else if (telegraph.Kind == BossAttackKind.ElectricPillar)
            {
                // Locally created hazards need a marker at their actual spawn,
                // rather than a misleading lamp on the distant boss body.
                origin = new Vector2(rect.center.x, rect.yMax - Mathf.Min(18f, rect.height * .5f));
                direction = new Vector2(0f, 1f);
            }
            else if (boss != null && (telegraph.Kind == BossAttackKind.MineDrop ||
                telegraph.Kind == BossAttackKind.CrateDrop || telegraph.Kind == BossAttackKind.ShieldDrone ||
                telegraph.Kind == BossAttackKind.AirMine))
            {
                float centreX = telegraph.Kind == BossAttackKind.MineDrop ? boss.X - 10f
                    : telegraph.Kind == BossAttackKind.CrateDrop ? boss.X
                    : telegraph.Kind == BossAttackKind.ShieldDrone ? boss.X + 5f : boss.X - 5f;
                origin = new Vector2(centreX, rect.center.y);
            }
            else if (boss != null && telegraph.Kind == BossAttackKind.Cannon)
                origin = new Vector2(boss.X - 15f, GroundY - 25f);
            else if (boss != null && telegraph.Kind == BossAttackKind.AirSpread)
                origin = new Vector2(boss.X - 11f, GroundY - boss.Y - boss.Height * .45f - 6f);
            else if (boss != null && telegraph.Kind == BossAttackKind.Shockwave)
                origin = new Vector2(boss.X + 1f, GroundY - 12.5f);
            else if (boss != null && (telegraph.Kind == BossAttackKind.Drone || telegraph.Kind == BossAttackKind.DroneLow))
                origin = new Vector2(boss.X - 5f, GroundY - (telegraph.Kind == BossAttackKind.Drone ? 72f : 21f));
            else if (boss != null)
                origin = new Vector2(boss.X + 7f, GroundY - boss.Y - boss.Height * .48f);
            else origin = new Vector2(rect.xMax - 18f, rect.center.y);
            DrawBossLocalWarning(origin, direction, color, telegraph);
        }

        private void DrawBossLaserWarning(Rect rect, Color color, BossTelegraph telegraph)
        {
            // A laser is instant when fired: keep a restrained aiming filament,
            // not a filled banner. It stops moving as soon as the shot locks.
            Fill(new Rect(rect.x, rect.center.y - .75f, rect.width, 1.5f), WithAlpha(color, .28f));
            DrawBossLocalWarning(new Vector2(rect.xMax - 18f, rect.center.y), new Vector2(-1f, 0f), color, telegraph);
        }

        private void DrawBossLocalWarning(Vector2 origin, Vector2 direction, Color color,
            BossTelegraph telegraph, bool doubleArrow = false)
        {
            origin.x = Mathf.Clamp(origin.x, 25f, Width - 25f);
            origin.y = Mathf.Clamp(origin.y, 150f, GroundY - 18f);
            float progress = telegraph.Duration <= 0f ? 0f : Mathf.Clamp01(1f - telegraph.Remaining / telegraph.Duration);
            Color ink = WithAlpha(color, .78f + progress * .18f);
            // A fixed compact caution sign is easier on the eyes than blinking
            // full-lane arrows. The exclamation point stays inside its source.
            Vector2 top = new Vector2(origin.x, origin.y - 14f);
            Vector2 left = new Vector2(origin.x - 13f, origin.y + 10f);
            Vector2 right = new Vector2(origin.x + 13f, origin.y + 10f);
            Line(top, left, ink, 2f); Line(left, right, ink, 2f); Line(right, top, ink, 2f);
            Fill(new Rect(origin.x - 1.5f, origin.y - 6f, 3f, 8f), ink);
            Fill(new Rect(origin.x - 1.5f, origin.y + 5f, 3f, 3f), ink);
            float magnitude = Mathf.Sqrt(direction.x * direction.x + direction.y * direction.y);
            if (magnitude < .01f) return;
            direction = direction * (1f / magnitude);
            Vector2 across = new Vector2(-direction.y, direction.x);
            int count = doubleArrow ? 2 : 1;
            for (int i = 0; i < count; i++)
            {
                Vector2 tip = origin + direction * (23f + i * 9f);
                Vector2 back = tip - direction * 7f;
                Line(back + across * 5f, tip, ink, 2f);
                Line(back - across * 5f, tip, ink, 2f);
            }
        }

        // Kept for compact, stationary hazard previews that use a real footprint.
        private static void DrawBossWarningRect(Rect rect, Color color, BossTelegraph telegraph)
        {
            if (rect.width <= 0f || rect.height <= 0f) return;
            float progress = telegraph.Duration <= 0 ? 0 : Mathf.Clamp01(1 - telegraph.Remaining / telegraph.Duration);
            Fill(rect, WithAlpha(color, .06f + progress * .12f));
            float edge = 1.5f + progress;
            Fill(new Rect(rect.x, rect.y, rect.width, edge), WithAlpha(color, .78f));
            Fill(new Rect(rect.x, rect.yMax - edge, rect.width, edge), WithAlpha(color, .78f));
            Fill(new Rect(rect.x, rect.y, edge, rect.height), WithAlpha(color, .78f));
            Fill(new Rect(rect.xMax - edge, rect.y, edge, rect.height), WithAlpha(color, .78f));
        }

        private void DrawBossProjectile(BossProjectile projectile, float clock)
        {
            if (!projectile.Friendly && DrawUnderwaterProjectile(projectile, clock)) return;
            Rect rect = new Rect(projectile.X, GroundY - projectile.Y - projectile.Height,
                projectile.Width, projectile.Height);
            if (projectile.Friendly)
            {
                DrawPulsePellet(rect, projectile, clock);
                return;
            }
            if (projectile.Kind == BossAttackKind.Cannon || projectile.Kind == BossAttackKind.Artillery)
            {
                DrawArmoredShell(projectile, rect, clock);
                return;
            }
            if (projectile.Kind == BossAttackKind.AirSpread)
            {
                DrawPlasmaBolt(projectile, rect, clock);
                return;
            }
            if (!projectile.Friendly && projectile.Kind == BossAttackKind.ElectricOrb)
            {
                DrawElectricOrb(rect, clock);
                return;
            }
            if (!projectile.Friendly && projectile.Kind == BossAttackKind.Shockwave)
            {
                DrawRobotShockwave(rect, clock);
                return;
            }
            if (!projectile.Friendly && (projectile.Kind == BossAttackKind.CrossMissile ||
                projectile.Kind == BossAttackKind.MissileRain || projectile.Kind == BossAttackKind.Missile))
            {
                DrawDirectionalMissile(projectile, rect, clock);
                return;
            }
            if (projectile.Kind == BossAttackKind.Drone || projectile.Kind == BossAttackKind.DroneLow)
            {
                DrawWingDrone(rect, clock);
                return;
            }
            DrawArmoredShell(projectile, rect, clock);
        }
    }
}
