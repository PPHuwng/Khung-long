using UnityEngine;

namespace CityDino
{
    /// <summary>Run volleys use the same live bottom-up geometry as their boss counterparts.</summary>
    public sealed partial class RunnerWorldView
    {
        private readonly BossProjectile runMissileVisual = new BossProjectile
        {
            Kind = BossAttackKind.CrossMissile
        };

        private void DrawRunMissileBody(int id, float x, float y, float width, float height,
            float velocityX, float radius, float clock)
        {
            Rect body = new Rect(x, GroundY - y - height, width, height);
            // The round metal hub is the exact damaging circle. The small nose,
            // fins and exhaust make the rocket readable without adding a hidden
            // rectangular damage volume around its transparent corners.
            float physicalRadius = radius > 0f ? radius : Mathf.Min(width, height) * .5f;
            Disc(new Rect(body.center.x - physicalRadius, body.center.y - physicalRadius,
                physicalRadius * 2f, physicalRadius * 2f), Hex(0x354e62));
            runMissileVisual.Id = id;
            runMissileVisual.X = x;
            runMissileVisual.Y = y;
            runMissileVisual.Width = width;
            runMissileVisual.Height = height;
            runMissileVisual.VelocityX = velocityX;
            runMissileVisual.VelocityY = 0f;
            runMissileVisual.Radius = physicalRadius;
            DrawDirectionalMissile(runMissileVisual, body, clock);
        }

        private void DrawRunMissileWarning(int count, float y, float height,
            float remaining, float duration, float clock)
        {
            Color amber = Hex(0xffc875);
            float centreY = GroundY - y - height * .5f;
            float progress = duration > 0f ? Mathf.Clamp01(1f - remaining / duration) : 1f;
            // A sparse low lane is only a preview. It never looks like the
            // opaque missile body which appears after the completed warning.
            Fill(new Rect(92f, GroundY - y - height, Width - 146f, height), WithAlpha(amber, .055f));
            float offset = Repeat(clock * 83f, 74f);
            for (float x = 112f + offset; x < Width - 88f; x += 74f)
            {
                Line(new Vector2(x + 7f, centreY - 4f), new Vector2(x, centreY), WithAlpha(amber, .42f), 1.7f);
                Line(new Vector2(x, centreY), new Vector2(x + 7f, centreY + 4f), WithAlpha(amber, .42f), 1.7f);
            }
            float edgeX = Width - 37f;
            float pulse = .19f + .07f * Mathf.Sin(clock * 9f);
            Disc(new Rect(edgeX - 24f, centreY - 24f, 48f, 48f), WithAlpha(amber, pulse), true);
            Line(new Vector2(edgeX + 9f, centreY - 10f), new Vector2(edgeX - 7f, centreY), amber, 3f);
            Line(new Vector2(edgeX - 7f, centreY), new Vector2(edgeX + 9f, centreY + 10f), amber, 3f);
            Fill(new Rect(edgeX - 22f, centreY + 20f, 44f, 3f), WithAlpha(amber, .16f));
            Fill(new Rect(edgeX - 22f, centreY + 20f, 44f * progress, 3f), WithAlpha(amber, .85f));
            int visibleCount = System.Math.Max(1, System.Math.Min(4, count));
            float first = edgeX - (visibleCount - 1) * 6f;
            for (int i = 0; i < visibleCount; i++)
                Disc(new Rect(first + i * 12f - 3f, centreY - 28f, 6f, 6f), amber);
            GUI.color = Color.white;
        }
    }
}
