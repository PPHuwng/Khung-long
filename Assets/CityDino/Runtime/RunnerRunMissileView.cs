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

        private void DrawRunMissileWarning(RunMissileWarning warning, float clock)
        {
            Color amber = Hex(0xffc875);
            int visibleCount = System.Math.Max(1, System.Math.Min(4, warning.Count));
            float edgeX = Width - 27f;
            float progress = warning.Duration > 0f ? Mathf.Clamp01(1f - warning.Remaining / warning.Duration) : 1f;
            // Only the actual entry points are marked. No stripe, moving lane
            // or safe-corridor paint covers the playfield or tells when to jump.
            for (int i = 0; i < visibleCount; i++)
            {
                float centreY = GroundY - warning.MissileY(i) - 11f;
                Disc(new Rect(edgeX - 14f, centreY - 14f, 28f, 28f), WithAlpha(Hex(0x172b38), .92f));
                Sign(new Rect(edgeX - 11f, centreY - 13f, 22f, 24f), "!", amber, 19);
                Fill(new Rect(edgeX - 11f, centreY + 12f, 22f * progress, 2f), amber);
            }
            GUI.color = Color.white;
        }
    }
}
