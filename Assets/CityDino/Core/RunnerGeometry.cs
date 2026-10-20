using System;

namespace CityDino
{
    /// <summary>
    /// Shared sprite layout and saw contact geometry. The five compact silhouettes
    /// below come from the supplied Dino1..5 PNGs, not an enclosing player box.
    /// Keeping this in core avoids requiring readable textures or Unity in physics.
    /// </summary>
    public static class RunnerGeometry
    {
        public const float PlayerStandingWidth = 99f;
        public const float PlayerStandingHeight = 91f;
        public const float PlayerDuckWidth = 109f;
        public const float PlayerDuckHeight = 44f;
        public const float SawRadiusRatio = 0.49f;

        private const float SourceWidth = 123f;
        private const float SourceHeight = 103f;
        private const float ContactInset = 1f;

        public static int GetPlayerFrame(float elapsed, float speed, float playerY, RunState state)
        {
            if (playerY > 3f) return 2;
            // Paused/game-over elapsed is frozen. Preserve the contact pose so
            // stopping a run cannot suddenly move the visible head or feet.
            if (state == RunState.Menu) return 0;
            return (int)Math.Floor(elapsed * (9f + speed / 150f)) % Silhouettes.Length;
        }

        public static float GetPlayerBob(float elapsed, float playerY, bool ducking, RunState state)
        {
            return state != RunState.Menu && playerY <= 1f && !ducking
                ? (float)Math.Sin(elapsed * 24f) * 1.3f : 0f;
        }

        /// <summary>
        /// Tests a slightly inset blade circle against the visible dinosaur body.
        /// Each band retains separate opaque runs, including the gap between legs.
        /// Facing, duck stretching, animation and bob use the same values as drawing.
        /// The tooth tips and one pixel of dinosaur outline remain forgiving.
        /// </summary>
        public static bool SawTouchesPlayer(WorldEntity saw, float playerX, float playerY,
            bool ducking, bool facingLeft, int frame, float bob)
        {
            float radius = Math.Min(saw.Width, saw.Height) * SawRadiusRatio;
            if (radius <= 0f) return false;
            float centreX = saw.X + saw.Width * 0.5f;
            float centreY = saw.Y + saw.Height * 0.5f;
            float width = ducking ? PlayerDuckWidth : PlayerStandingWidth;
            float height = ducking ? PlayerDuckHeight : PlayerStandingHeight;
            float feet = playerY - bob;
            if (centreX + radius <= playerX - width * 0.5f ||
                centreX - radius >= playerX + width * 0.5f ||
                centreY + radius <= feet || centreY - radius >= feet + height)
                return false;

            byte[] bands = Silhouettes[Math.Max(0, Math.Min(Silhouettes.Length - 1, frame))];
            float scaleX = width / SourceWidth;
            float scaleY = height / SourceHeight;
            float radiusSquared = radius * radius;
            for (int i = 0; i < bands.Length; i += 4)
            {
                float left = (bands[i] - SourceWidth * 0.5f) * scaleX;
                float right = (bands[i + 2] - SourceWidth * 0.5f) * scaleX;
                if (facingLeft)
                {
                    float oldLeft = left;
                    left = -right;
                    right = -oldLeft;
                }
                left += playerX + ContactInset;
                right += playerX - ContactInset;
                if (left >= right) continue;
                float bottom = feet + (SourceHeight - bands[i + 3]) * scaleY;
                float top = feet + (SourceHeight - bands[i + 1]) * scaleY;
                // Clip only the outside top/feet, not every band, so adjacent bands
                // remain contiguous and do not form artificial horizontal holes.
                bottom = Math.Max(bottom, feet + ContactInset);
                top = Math.Min(top, feet + height - ContactInset);
                if (bottom >= top) continue;
                float nearestX = Math.Max(left, Math.Min(right, centreX));
                float nearestY = Math.Max(bottom, Math.Min(top, centreY));
                float dx = centreX - nearestX;
                float dy = centreY - nearestY;
                if (dx * dx + dy * dy < radiusSquared) return true;
            }
            return false;
        }

        // Opaque alpha >=128, grouped in two source-pixel rows. Entries are
        // left, top, right-exclusive, bottom-exclusive in the original 123x103 PNG.
        // Separate horizontal runs preserve transparent space around neck/legs.
        private static readonly byte[][] Silhouettes =
        {
            // Dino1
            new byte[]
            {
                94, 0, 104, 2, 89, 2, 106, 4, 88, 4, 108, 6, 86, 6, 108, 8,
                84, 8, 110, 10, 83, 10, 112, 12, 82, 12, 113, 14, 82, 14, 113, 16,
                82, 16, 113, 18, 82, 18, 111, 20, 82, 20, 109, 22, 81, 22, 98, 24,
                81, 24, 96, 26, 80, 26, 95, 28, 80, 28, 95, 30, 80, 30, 95, 32,
                80, 32, 94, 34, 79, 34, 93, 36, 78, 36, 93, 38, 76, 38, 93, 40,
                76, 40, 93, 42, 74, 42, 93, 44, 50, 44, 63, 46, 70, 44, 93, 46,
                44, 46, 91, 48, 41, 48, 91, 50, 39, 50, 91, 52, 37, 52, 91, 54,
                35, 54, 89, 56, 33, 56, 89, 58, 32, 58, 88, 60, 30, 60, 87, 62,
                27, 62, 87, 64, 24, 64, 85, 66, 21, 66, 83, 68, 18, 68, 82, 70,
                5, 70, 7, 72, 14, 70, 83, 72, 5, 72, 85, 74, 5, 74, 86, 76,
                8, 76, 35, 78, 37, 76, 88, 78, 12, 78, 29, 80, 37, 78, 61, 80,
                68, 78, 88, 80, 16, 80, 22, 82, 36, 80, 53, 82, 70, 80, 88, 82,
                35, 82, 54, 84, 70, 82, 89, 84, 33, 84, 54, 86, 72, 84, 89, 86,
                33, 86, 45, 88, 47, 86, 52, 88, 73, 86, 88, 88, 31, 88, 43, 90,
                74, 88, 90, 90, 31, 90, 41, 92, 75, 90, 90, 92, 31, 92, 40, 94,
                75, 92, 87, 94, 31, 94, 42, 96, 77, 94, 84, 96, 32, 96, 42, 98,
                79, 96, 82, 98, 33, 98, 42, 100,
            },
            // Dino2
            new byte[]
            {
                97, 0, 108, 2, 92, 2, 109, 4, 91, 4, 111, 6, 89, 6, 111, 8,
                88, 8, 113, 10, 86, 10, 116, 12, 85, 12, 116, 14, 85, 14, 116, 16,
                85, 16, 116, 18, 85, 18, 114, 20, 85, 20, 112, 22, 84, 22, 101, 24,
                84, 24, 99, 26, 83, 26, 98, 28, 83, 28, 98, 30, 83, 30, 98, 32,
                83, 32, 97, 34, 82, 34, 96, 36, 81, 36, 96, 38, 79, 38, 96, 40,
                79, 40, 96, 42, 77, 42, 96, 44, 52, 44, 67, 46, 73, 44, 96, 46,
                47, 46, 94, 48, 44, 48, 94, 50, 41, 50, 94, 52, 40, 52, 94, 54,
                38, 54, 92, 56, 36, 56, 92, 58, 34, 58, 92, 60, 32, 60, 90, 62,
                30, 62, 90, 64, 27, 64, 88, 66, 23, 66, 86, 68, 21, 68, 86, 70,
                8, 70, 10, 72, 17, 70, 87, 72, 8, 72, 88, 74, 8, 74, 89, 76,
                11, 76, 69, 78, 70, 76, 90, 78, 15, 78, 32, 80, 39, 78, 64, 80,
                71, 78, 91, 80, 19, 80, 25, 82, 38, 80, 57, 82, 72, 80, 91, 82,
                38, 82, 57, 84, 73, 82, 92, 84, 36, 84, 57, 86, 75, 84, 92, 86,
                36, 86, 56, 88, 76, 86, 91, 88, 34, 88, 45, 90, 77, 88, 93, 90,
                34, 90, 44, 92, 78, 90, 93, 92, 34, 92, 43, 94, 79, 92, 90, 94,
                34, 94, 45, 96, 80, 94, 87, 96, 35, 96, 45, 98, 83, 96, 85, 98,
                36, 98, 45, 100,
            },
            // Dino3
            new byte[]
            {
                102, 2, 114, 4, 98, 4, 116, 6, 96, 6, 118, 8, 94, 8, 118, 10,
                93, 10, 120, 12, 92, 12, 122, 14, 92, 14, 122, 16, 92, 16, 122, 18,
                92, 18, 122, 20, 92, 20, 119, 22, 92, 22, 118, 24, 91, 24, 107, 26,
                91, 26, 105, 28, 90, 28, 105, 30, 90, 30, 105, 32, 90, 32, 105, 34,
                90, 34, 103, 36, 88, 36, 103, 38, 88, 38, 103, 40, 87, 40, 103, 42,
                86, 42, 103, 44, 83, 44, 103, 46, 55, 46, 73, 48, 81, 46, 103, 48,
                51, 48, 103, 50, 48, 50, 101, 52, 45, 52, 101, 54, 43, 54, 101, 56,
                41, 56, 101, 58, 38, 58, 99, 60, 36, 60, 99, 62, 33, 62, 97, 64,
                29, 64, 97, 66, 13, 66, 16, 68, 22, 66, 95, 68, 13, 68, 93, 70,
                13, 70, 93, 72, 15, 72, 91, 74, 18, 74, 41, 76, 42, 74, 89, 76,
                24, 76, 31, 78, 43, 76, 86, 78, 89, 76, 94, 78, 43, 78, 56, 80,
                57, 78, 72, 80, 74, 78, 86, 80, 89, 78, 95, 80, 41, 80, 56, 82,
                58, 80, 71, 82, 74, 80, 96, 82, 40, 82, 55, 84, 58, 82, 60, 84,
                75, 82, 97, 84, 39, 84, 54, 86, 56, 84, 60, 86, 77, 84, 97, 86,
                37, 86, 53, 88, 54, 86, 60, 88, 77, 86, 97, 88, 36, 88, 48, 90,
                53, 88, 57, 90, 79, 88, 96, 90, 34, 90, 47, 92, 53, 90, 55, 92,
                80, 90, 94, 92, 34, 92, 46, 94, 81, 92, 97, 94, 34, 94, 44, 96,
                82, 94, 97, 96, 34, 96, 44, 98, 84, 96, 95, 98, 34, 98, 45, 100,
                85, 98, 91, 100, 37, 100, 43, 102, 86, 100, 91, 102,
            },
            // Dino4
            new byte[]
            {
                94, 6, 106, 8, 90, 8, 108, 10, 88, 10, 110, 12, 86, 12, 110, 14,
                85, 14, 113, 16, 84, 16, 114, 18, 84, 18, 114, 20, 84, 20, 114, 22,
                84, 22, 114, 24, 83, 24, 113, 26, 83, 26, 110, 28, 82, 28, 98, 30,
                81, 30, 97, 32, 81, 32, 96, 34, 81, 34, 96, 36, 79, 36, 95, 38,
                79, 38, 95, 40, 77, 40, 94, 42, 76, 42, 94, 44, 75, 44, 93, 46,
                53, 46, 65, 48, 72, 46, 93, 48, 50, 48, 93, 50, 46, 50, 92, 52,
                44, 52, 91, 54, 41, 54, 91, 56, 39, 56, 91, 58, 37, 58, 89, 60,
                36, 60, 89, 62, 33, 62, 88, 64, 32, 64, 87, 66, 30, 66, 85, 68,
                27, 68, 85, 70, 26, 70, 86, 72, 23, 72, 88, 74, 19, 74, 89, 76,
                15, 76, 90, 78, 11, 78, 67, 80, 70, 78, 91, 80, 11, 80, 64, 82,
                71, 80, 91, 82, 13, 82, 63, 84, 73, 82, 90, 84, 15, 84, 25, 86,
                30, 84, 63, 86, 75, 84, 89, 86, 30, 86, 42, 88, 44, 86, 63, 88,
                77, 86, 90, 88, 30, 88, 40, 90, 44, 88, 63, 90, 78, 88, 90, 90,
                33, 90, 38, 92, 46, 90, 62, 92, 79, 90, 88, 92, 46, 92, 63, 94,
                80, 92, 85, 94, 46, 94, 63, 96, 47, 96, 60, 98, 49, 98, 58, 100,
            },
            // Dino5
            new byte[]
            {
                85, 4, 96, 6, 83, 6, 98, 8, 79, 8, 99, 10, 77, 10, 100, 12,
                76, 12, 101, 14, 74, 14, 103, 16, 74, 16, 104, 18, 74, 18, 104, 20,
                74, 20, 104, 22, 74, 22, 103, 24, 74, 24, 100, 26, 73, 26, 89, 28,
                73, 28, 87, 30, 73, 30, 87, 32, 73, 32, 87, 34, 73, 34, 87, 36,
                71, 36, 85, 38, 71, 38, 85, 40, 70, 40, 85, 42, 69, 42, 85, 44,
                68, 44, 85, 46, 66, 46, 84, 48, 44, 48, 55, 50, 63, 48, 83, 50,
                40, 50, 83, 52, 37, 52, 83, 54, 35, 54, 83, 56, 32, 56, 81, 58,
                29, 58, 81, 60, 27, 60, 81, 62, 26, 62, 80, 64, 23, 64, 79, 66,
                21, 66, 78, 68, 20, 68, 77, 70, 17, 70, 75, 72, 13, 72, 75, 74,
                8, 74, 73, 76, 1, 76, 71, 78, 1, 78, 68, 80, 72, 78, 77, 80,
                1, 80, 30, 82, 31, 80, 68, 82, 71, 80, 77, 82, 4, 82, 24, 84,
                32, 82, 46, 84, 48, 82, 68, 84, 71, 82, 77, 84, 8, 84, 17, 86,
                30, 84, 46, 86, 57, 84, 77, 86, 28, 86, 50, 88, 58, 86, 76, 88,
                28, 88, 50, 90, 59, 88, 74, 90, 26, 90, 49, 92, 59, 90, 73, 92,
                26, 92, 47, 94, 59, 92, 71, 94, 26, 94, 40, 96, 59, 94, 71, 96,
                26, 96, 35, 98, 61, 96, 68, 98, 26, 98, 36, 100, 28, 100, 31, 102,
                33, 100, 35, 102,
            },
        };
    }
}
