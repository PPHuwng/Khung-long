using System;

namespace CityDino
{
    /// <summary>
    /// Shared sprite layout and contact geometry. The silhouettes follow
    /// the supplied running frames and generated jump/crouch animation atlas.
    /// Keeping this in core avoids requiring readable textures or Unity in physics.
    /// </summary>
    public static class RunnerGeometry
    {
        public const float PlayerStandingWidth = 99f;
        public const float PlayerStandingHeight = 91f;
        public const float PlayerDuckWidth = 109f;
        public const float PlayerDuckHeight = 44f;
        public const float SawRadiusRatio = 0.49f;
        private const int RunFrameCount = 5;

        private const float SourceWidth = 123f;
        private const float SourceHeight = 103f;
        private const float ContactInset = 1f;

        public static int GetPlayerFrame(float elapsed, float speed, float playerY, RunState state)
        {
            if (playerY > 3f) return 2;
            // Paused/game-over elapsed is frozen. Preserve the contact pose so
            // stopping a run cannot suddenly move the visible head or feet.
            if (state == RunState.Menu) return 0;
            // Only running frames belong in this cycle, even after additional
            // action silhouettes are added. Bound animation rate, not run speed.
            double rate = Math.Min(20d, 9d + speed / 150d);
            return (int)(Math.Floor((double)elapsed * rate) % RunFrameCount);
        }

        public static int GetPlayerFrame(RunnerSession session)
        {
            if (session == null) return 0;
            if (session.IsDucking)
                return 5 + (int)(Math.Floor((double)session.Elapsed * 10d) % 3d);
            if (session.PlayerY > 3f)
                return session.AnimationVelocityY > 220f ? 8
                    : session.AnimationVelocityY < -220f ? 10 : 9;
            return GetPlayerFrame(session.Elapsed, session.Speed, session.PlayerY, session.State);
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

            byte[] bands = frame >= RunFrameCount
                ? ActionSilhouettes[Math.Min(ActionSilhouettes.Length - 1, frame - RunFrameCount)]
                : Silhouettes[Math.Max(0, Math.Min(RunFrameCount - 1, frame))];
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

        /// <summary>
        /// Biome traps touch the same visible silhouette as the saw, including
        /// head, tail, pose and facing. Transparent neck/leg gaps stay harmless.
        /// </summary>
        public static bool BiomeTouchesPlayer(WorldEntity hazard, RunnerSession session)
        {
            if (session == null || !RunnerBiomeHazards.IsDamaging(hazard)) return false;
            bool ducking = session.IsDucking;
            float width = ducking ? PlayerDuckWidth : PlayerStandingWidth;
            float height = ducking ? PlayerDuckHeight : PlayerStandingHeight;
            float feet = session.PlayerY - GetPlayerBob(session.Elapsed, session.PlayerY, ducking, session.State);
            float hazardHeight = hazard.Kind == EntityKind.HeatVent ? hazard.HazardHeight : hazard.Height;
            if (hazard.X + hazard.Width <= session.PlayerX - width * 0.5f ||
                hazard.X >= session.PlayerX + width * 0.5f ||
                hazard.Y + hazardHeight <= feet || hazard.Y >= feet + height)
                return false;
            int frame = GetPlayerFrame(session);
            byte[] bands = frame >= RunFrameCount
                ? ActionSilhouettes[Math.Min(ActionSilhouettes.Length - 1, frame - RunFrameCount)]
                : Silhouettes[Math.Max(0, Math.Min(RunFrameCount - 1, frame))];
            float scaleX = width / SourceWidth;
            float scaleY = height / SourceHeight;
            for (int i = 0; i < bands.Length; i += 4)
            {
                float left = (bands[i] - SourceWidth * 0.5f) * scaleX;
                float right = (bands[i + 2] - SourceWidth * 0.5f) * scaleX;
                if (session.FacingLeft)
                {
                    float oldLeft = left;
                    left = -right;
                    right = -oldLeft;
                }
                left += session.PlayerX + ContactInset;
                right += session.PlayerX - ContactInset;
                float bottom = Math.Max(feet + (SourceHeight - bands[i + 3]) * scaleY, feet + ContactInset);
                float top = Math.Min(feet + (SourceHeight - bands[i + 1]) * scaleY, feet + height - ContactInset);
                if (left < right && bottom < top && RunnerBiomeHazards.TouchesPlayer(hazard, left, right, bottom, top))
                    return true;
            }
            return false;
        }

        // Generated action atlas alpha >=128, expressed in the same 123x103
        // logical sampling grid as the running frames. UV viewports use a
        // common size and feet baseline within each row to prevent scale jitter.
        // Atlas SHA256: b92a5b9974054e53ce8b1f160969bedec15a5f0b069e7cfe90302a87d2257c77
        private static readonly byte[][] ActionSilhouettes =
        {
            // Crouch left-foot lead; atlas viewport (19, 752, 514, 938)
            new byte[]
            {
                46, 0, 55, 2, 46, 2, 56, 4, 41, 4, 61, 6, 41, 6, 61, 8,
                36, 8, 64, 10, 34, 10, 67, 12, 33, 12, 67, 14, 31, 14, 69, 16,
                27, 16, 72, 18, 104, 16, 112, 18, 1, 18, 6, 20, 24, 18, 75, 20,
                100, 18, 116, 20, 0, 20, 9, 22, 24, 20, 75, 22, 100, 20, 116, 22,
                0, 22, 9, 24, 19, 22, 78, 24, 97, 22, 117, 24, 0, 24, 78, 26,
                97, 24, 118, 26, 0, 26, 86, 28, 94, 26, 119, 28, 0, 28, 120, 30,
                1, 30, 120, 32, 1, 32, 120, 34, 3, 34, 120, 36, 3, 36, 120, 38,
                4, 38, 122, 40, 5, 40, 122, 42, 5, 42, 123, 44, 6, 44, 123, 46,
                8, 46, 123, 48, 8, 48, 123, 50, 10, 50, 123, 52, 10, 52, 123, 54,
                13, 54, 123, 56, 19, 56, 123, 58, 19, 58, 122, 60, 21, 60, 122, 62,
                21, 62, 121, 64, 19, 64, 94, 66, 100, 64, 120, 66, 19, 66, 87, 68,
                104, 66, 118, 68, 18, 68, 90, 70, 104, 68, 118, 70, 18, 70, 94, 72,
                18, 72, 46, 74, 50, 72, 95, 74, 16, 74, 45, 76, 51, 74, 95, 76,
                16, 76, 44, 78, 59, 76, 96, 78, 16, 78, 44, 80, 59, 78, 96, 80,
                16, 80, 41, 82, 59, 80, 96, 82, 16, 82, 41, 84, 61, 82, 96, 84,
                16, 84, 40, 86, 62, 84, 96, 86, 16, 86, 40, 88, 62, 86, 96, 88,
                16, 88, 41, 90, 64, 88, 95, 90, 17, 90, 41, 92, 65, 90, 95, 92,
                18, 92, 41, 94, 65, 92, 79, 94, 81, 92, 94, 94, 18, 94, 41, 96,
                67, 94, 78, 96, 83, 94, 92, 96, 19, 96, 41, 98, 67, 96, 78, 98,
                83, 96, 92, 98, 25, 98, 41, 100, 69, 98, 76, 100, 85, 98, 91, 100,
                26, 100, 39, 102, 69, 100, 76, 102, 86, 100, 89, 102, 26, 102, 39, 103,
            },
            // Crouch right-foot lead; atlas viewport (532, 754, 1027, 940)
            new byte[]
            {
                48, 0, 58, 2, 41, 2, 63, 4, 102, 2, 111, 4, 41, 4, 63, 6,
                102, 4, 111, 6, 37, 6, 65, 8, 99, 6, 115, 8, 34, 8, 67, 10,
                96, 8, 116, 10, 34, 10, 67, 12, 96, 10, 116, 12, 2, 12, 6, 14,
                31, 12, 70, 14, 94, 12, 117, 14, 2, 14, 6, 16, 28, 14, 73, 16,
                94, 14, 117, 16, 1, 16, 10, 18, 23, 16, 76, 18, 91, 16, 119, 18,
                1, 18, 10, 20, 23, 18, 76, 20, 90, 18, 119, 20, 1, 20, 80, 22,
                87, 20, 119, 22, 1, 22, 80, 24, 87, 22, 119, 24, 1, 24, 119, 26,
                2, 26, 121, 28, 2, 28, 121, 30, 4, 30, 122, 32, 4, 32, 122, 34,
                5, 34, 122, 36, 6, 36, 122, 38, 7, 38, 122, 40, 8, 40, 122, 42,
                9, 42, 122, 44, 10, 44, 121, 46, 11, 46, 120, 48, 14, 48, 119, 50,
                14, 50, 119, 52, 18, 52, 97, 54, 102, 52, 117, 54, 18, 54, 96, 56,
                102, 54, 117, 56, 22, 56, 93, 58, 22, 58, 90, 60, 20, 60, 90, 62,
                19, 62, 87, 64, 19, 64, 86, 66, 17, 66, 87, 68, 17, 68, 87, 70,
                16, 70, 40, 72, 41, 70, 89, 72, 15, 72, 38, 74, 42, 72, 91, 74,
                14, 74, 38, 76, 41, 74, 92, 76, 14, 76, 34, 78, 41, 76, 56, 78,
                57, 76, 93, 78, 13, 78, 32, 80, 41, 78, 58, 80, 66, 78, 95, 80,
                13, 80, 31, 82, 41, 80, 58, 82, 66, 80, 95, 82, 13, 82, 28, 84,
                41, 82, 59, 84, 67, 82, 95, 84, 13, 84, 27, 86, 41, 84, 59, 86,
                67, 84, 95, 86, 13, 86, 26, 88, 42, 86, 61, 88, 69, 86, 95, 88,
                13, 88, 26, 90, 42, 88, 61, 90, 69, 88, 76, 90, 77, 88, 95, 90,
                14, 90, 26, 92, 44, 90, 61, 92, 79, 90, 93, 92, 16, 92, 25, 94,
                44, 92, 61, 94, 79, 92, 93, 94, 16, 94, 25, 96, 44, 94, 61, 96,
                80, 94, 91, 96, 18, 96, 23, 98, 46, 96, 61, 98, 81, 96, 91, 98,
                18, 98, 23, 100, 46, 98, 61, 100, 82, 98, 89, 100, 47, 100, 59, 102,
                47, 102, 59, 103,
            },
            // Crouch in-between; atlas viewport (1035, 754, 1530, 940)
            new byte[]
            {
                44, 0, 53, 2, 40, 2, 57, 4, 40, 4, 57, 6, 35, 6, 61, 8,
                32, 8, 64, 10, 32, 10, 64, 12, 29, 12, 66, 14, 1, 14, 5, 16,
                29, 14, 66, 16, 104, 14, 113, 16, 0, 16, 7, 18, 23, 16, 70, 18,
                100, 16, 116, 18, 0, 18, 7, 20, 23, 18, 70, 20, 100, 18, 116, 20,
                0, 20, 10, 22, 17, 20, 73, 22, 97, 20, 117, 22, 0, 22, 77, 24,
                94, 22, 118, 24, 0, 24, 77, 26, 94, 24, 119, 26, 1, 26, 81, 28,
                90, 26, 120, 28, 1, 28, 120, 30, 1, 30, 120, 32, 2, 32, 120, 34,
                3, 34, 120, 36, 3, 36, 122, 38, 5, 38, 122, 40, 5, 40, 123, 42,
                7, 42, 123, 44, 7, 44, 123, 46, 8, 46, 123, 48, 10, 48, 123, 50,
                11, 50, 123, 52, 14, 52, 123, 54, 14, 54, 122, 56, 18, 56, 122, 58,
                22, 58, 121, 60, 23, 60, 121, 62, 23, 62, 93, 64, 101, 62, 119, 64,
                22, 64, 85, 66, 105, 64, 118, 66, 22, 66, 85, 68, 105, 66, 118, 68,
                22, 68, 81, 70, 22, 70, 81, 72, 22, 72, 78, 74, 22, 74, 79, 76,
                22, 76, 49, 78, 52, 76, 79, 78, 22, 78, 49, 80, 52, 78, 82, 80,
                24, 80, 47, 82, 62, 80, 83, 82, 24, 82, 45, 84, 62, 82, 83, 84,
                25, 84, 45, 86, 63, 84, 85, 86, 25, 86, 43, 88, 64, 86, 85, 88,
                25, 88, 44, 90, 65, 88, 86, 90, 25, 90, 44, 92, 67, 90, 86, 92,
                27, 92, 44, 94, 67, 92, 86, 94, 27, 94, 44, 96, 69, 94, 86, 96,
                27, 96, 44, 98, 69, 96, 86, 98, 29, 98, 42, 100, 71, 98, 86, 100,
                34, 100, 41, 102, 72, 100, 84, 102, 34, 102, 41, 103, 73, 102, 84, 103,
            },
            // Jump launch; atlas viewport (32, 137, 481, 583)
            new byte[]
            {
                96, 0, 114, 2, 93, 2, 117, 4, 92, 4, 119, 6, 89, 6, 119, 8,
                89, 8, 121, 10, 89, 10, 123, 12, 87, 12, 123, 14, 86, 14, 123, 16,
                86, 16, 121, 18, 86, 18, 121, 20, 86, 20, 117, 22, 87, 22, 107, 24,
                87, 24, 106, 26, 87, 26, 106, 28, 85, 28, 106, 30, 85, 30, 104, 32,
                85, 32, 104, 34, 84, 34, 104, 36, 84, 36, 104, 38, 82, 38, 104, 40,
                82, 40, 104, 42, 79, 42, 103, 44, 77, 44, 103, 46, 72, 46, 103, 48,
                67, 48, 101, 50, 61, 50, 101, 52, 57, 52, 99, 54, 54, 54, 99, 56,
                51, 56, 98, 58, 3, 58, 7, 60, 48, 58, 98, 60, 1, 60, 9, 62,
                43, 60, 107, 62, 1, 62, 13, 64, 41, 62, 107, 64, 1, 64, 18, 66,
                35, 64, 110, 66, 3, 66, 25, 68, 31, 66, 110, 68, 4, 68, 110, 70,
                6, 70, 110, 72, 8, 72, 108, 74, 9, 74, 106, 76, 12, 76, 89, 78,
                94, 76, 104, 78, 14, 78, 84, 80, 96, 78, 102, 80, 19, 80, 82, 82,
                23, 82, 79, 84, 28, 84, 76, 86, 32, 86, 75, 88, 30, 88, 75, 90,
                28, 90, 73, 92, 27, 92, 71, 94, 27, 94, 68, 96, 27, 96, 65, 98,
                27, 98, 45, 100, 46, 98, 63, 100, 28, 100, 45, 102, 47, 100, 63, 102,
                30, 102, 42, 103, 50, 102, 62, 103,
            },
            // Jump tuck; atlas viewport (538, 15, 987, 461)
            new byte[]
            {
                98, 16, 108, 18, 93, 18, 112, 20, 89, 20, 115, 22, 87, 22, 117, 24,
                87, 24, 117, 26, 86, 26, 119, 28, 85, 28, 120, 30, 84, 30, 120, 32,
                84, 32, 120, 34, 84, 34, 119, 36, 84, 36, 117, 38, 84, 38, 106, 40,
                84, 40, 103, 42, 84, 42, 103, 44, 84, 44, 103, 46, 86, 46, 104, 48,
                86, 48, 105, 50, 86, 50, 105, 52, 84, 52, 105, 54, 60, 54, 72, 56,
                84, 54, 105, 56, 55, 56, 78, 58, 82, 56, 105, 58, 48, 58, 105, 60,
                44, 60, 105, 62, 3, 62, 11, 64, 38, 62, 105, 64, 3, 64, 15, 66,
                34, 64, 103, 66, 3, 66, 103, 68, 4, 68, 103, 70, 6, 70, 101, 72,
                7, 72, 101, 74, 10, 74, 99, 76, 12, 76, 99, 78, 14, 78, 98, 80,
                18, 80, 95, 82, 22, 82, 96, 84, 32, 84, 98, 86, 36, 86, 98, 88,
                34, 88, 98, 90, 32, 90, 96, 92, 32, 92, 96, 94, 32, 94, 65, 96,
                69, 94, 93, 96, 32, 96, 63, 98, 69, 96, 91, 98, 34, 98, 61, 100,
                71, 98, 88, 100, 38, 100, 56, 102, 73, 100, 83, 102, 40, 102, 45, 103,
                46, 102, 52, 103, 75, 102, 81, 103,
            },
            // Jump landing; atlas viewport (1051, 127, 1500, 573)
            new byte[]
            {
                101, 6, 111, 8, 97, 8, 115, 10, 93, 10, 118, 12, 91, 12, 120, 14,
                89, 14, 120, 16, 89, 16, 122, 18, 87, 18, 123, 20, 87, 20, 123, 22,
                86, 22, 123, 24, 86, 24, 123, 26, 86, 26, 120, 28, 84, 28, 118, 30,
                84, 30, 107, 32, 84, 32, 105, 34, 84, 34, 104, 36, 83, 36, 104, 38,
                83, 38, 104, 40, 83, 40, 102, 42, 81, 42, 102, 44, 1, 44, 4, 46,
                81, 44, 102, 46, 0, 46, 9, 48, 58, 46, 69, 48, 80, 46, 102, 48,
                0, 48, 14, 50, 46, 48, 73, 50, 77, 48, 102, 50, 0, 50, 19, 52,
                43, 50, 102, 52, 1, 52, 102, 54, 3, 54, 102, 56, 4, 56, 100, 58,
                6, 58, 100, 60, 8, 60, 100, 62, 9, 62, 99, 64, 13, 64, 97, 66,
                15, 66, 96, 68, 21, 68, 95, 70, 26, 70, 94, 72, 31, 72, 92, 74,
                36, 74, 92, 76, 34, 76, 94, 78, 31, 78, 95, 80, 29, 80, 97, 82,
                29, 82, 97, 84, 29, 84, 53, 86, 69, 84, 99, 86, 29, 86, 52, 88,
                70, 86, 100, 88, 31, 88, 52, 90, 72, 88, 102, 90, 31, 90, 52, 92,
                72, 90, 102, 92, 35, 92, 51, 94, 74, 92, 102, 94, 39, 94, 49, 96,
                75, 94, 102, 96, 75, 96, 100, 98, 77, 98, 93, 100, 77, 100, 92, 102,
                79, 102, 87, 103,
            },
        };

        // DinoRunAtlas alpha >=128 sampled in the shared 434x354 pixel canvas,
        // then grouped in two logical rows (123x103). Tight UVs are placed at a
        // common nose/feet anchor by RunnerArt; no source image was modified.
        // Atlas SHA256: 0903b7491d88d136f8f383549a3decb70da6dee36affe7a0ff6bfc0967d1a222
        private static readonly byte[][] Silhouettes =
        {
            // Run 0: atlas bounds (18, 199, 431, 548)
            new byte[]
            {
                100, 0, 112, 2, 96, 2, 115, 4, 94, 4, 117, 6, 92, 6, 118, 8,
                90, 8, 120, 10, 89, 10, 121, 12, 88, 12, 122, 14, 88, 14, 122, 16,
                87, 16, 122, 18, 87, 18, 122, 20, 87, 20, 122, 22, 87, 22, 120, 24,
                87, 24, 118, 26, 87, 26, 108, 28, 88, 28, 107, 30, 88, 30, 106, 32,
                88, 32, 105, 34, 87, 34, 105, 36, 87, 36, 105, 38, 86, 38, 105, 40,
                85, 40, 105, 42, 84, 42, 105, 44, 58, 44, 76, 46, 82, 44, 105, 46,
                6, 46, 10, 48, 53, 46, 105, 48, 5, 48, 13, 50, 48, 48, 105, 50,
                5, 50, 18, 52, 45, 50, 104, 52, 6, 52, 21, 54, 41, 52, 104, 54,
                7, 54, 103, 56, 9, 56, 102, 58, 9, 58, 101, 60, 10, 60, 101, 62,
                12, 62, 100, 64, 14, 64, 99, 66, 16, 66, 97, 68, 18, 68, 96, 70,
                22, 70, 94, 72, 25, 72, 95, 74, 29, 74, 96, 76, 33, 76, 98, 78,
                29, 78, 100, 80, 27, 80, 101, 82, 25, 82, 57, 84, 75, 82, 102, 84,
                24, 84, 55, 86, 77, 84, 104, 86, 23, 86, 53, 88, 78, 86, 106, 88,
                22, 88, 51, 90, 79, 88, 108, 90, 21, 90, 51, 92, 81, 90, 109, 92,
                21, 92, 51, 94, 82, 92, 109, 94, 21, 94, 51, 96, 83, 94, 109, 96,
                21, 96, 51, 98, 85, 96, 109, 98, 22, 98, 49, 100, 87, 98, 107, 100,
                23, 100, 37, 102, 39, 100, 47, 102, 88, 100, 103, 102,
            },
            // Run 1: atlas bounds (441, 199, 851, 549)
            new byte[]
            {
                100, 0, 111, 2, 96, 2, 114, 4, 94, 4, 116, 6, 92, 6, 118, 8,
                90, 8, 119, 10, 89, 10, 120, 12, 88, 12, 121, 14, 88, 14, 122, 16,
                87, 16, 122, 18, 87, 18, 122, 20, 87, 20, 122, 22, 87, 22, 120, 24,
                87, 24, 118, 26, 87, 26, 109, 28, 87, 28, 107, 30, 88, 30, 106, 32,
                88, 32, 106, 34, 88, 34, 105, 36, 88, 36, 105, 38, 87, 38, 105, 40,
                86, 40, 105, 42, 86, 42, 105, 44, 60, 44, 76, 46, 84, 44, 105, 46,
                7, 46, 10, 48, 55, 46, 105, 48, 6, 48, 13, 50, 51, 48, 105, 50,
                6, 50, 17, 52, 47, 50, 105, 52, 6, 52, 21, 54, 43, 52, 105, 54,
                7, 54, 105, 56, 9, 56, 103, 58, 10, 58, 103, 60, 11, 60, 102, 62,
                12, 62, 102, 64, 14, 64, 101, 66, 16, 66, 99, 68, 19, 68, 98, 70,
                22, 70, 96, 72, 25, 72, 95, 74, 30, 74, 93, 76, 35, 76, 93, 78,
                35, 78, 93, 80, 34, 80, 93, 82, 32, 82, 93, 84, 32, 84, 65, 86,
                72, 84, 93, 86, 32, 86, 63, 88, 72, 86, 94, 88, 32, 88, 62, 90,
                72, 88, 96, 90, 34, 90, 61, 92, 72, 90, 98, 92, 35, 92, 60, 94,
                73, 92, 100, 94, 37, 94, 61, 96, 74, 94, 100, 96, 40, 96, 61, 98,
                76, 96, 100, 98, 45, 98, 61, 100, 78, 98, 99, 100, 48, 100, 59, 102,
                82, 100, 97, 102,
            },
            // Run 2: atlas bounds (852, 199, 1274, 548)
            new byte[]
            {
                101, 0, 112, 2, 96, 2, 115, 4, 94, 4, 117, 6, 92, 6, 118, 8,
                91, 8, 120, 10, 90, 10, 121, 12, 89, 12, 122, 14, 88, 14, 122, 16,
                88, 16, 122, 18, 87, 18, 122, 20, 87, 20, 122, 22, 87, 22, 120, 24,
                87, 24, 118, 26, 87, 26, 108, 28, 88, 28, 106, 30, 88, 30, 106, 32,
                88, 32, 106, 34, 88, 34, 105, 36, 88, 36, 105, 38, 87, 38, 105, 40,
                86, 40, 105, 42, 85, 42, 105, 44, 56, 44, 77, 46, 83, 44, 105, 46,
                52, 46, 105, 48, 3, 48, 9, 50, 48, 48, 105, 50, 3, 50, 12, 52,
                44, 50, 105, 52, 3, 52, 16, 54, 39, 52, 104, 54, 3, 54, 104, 56,
                5, 56, 103, 58, 6, 58, 102, 60, 7, 60, 101, 62, 9, 62, 100, 64,
                11, 64, 99, 66, 13, 66, 98, 68, 16, 68, 96, 70, 20, 70, 94, 72,
                23, 72, 96, 74, 25, 74, 99, 76, 23, 76, 104, 78, 20, 78, 106, 80,
                19, 80, 54, 82, 55, 80, 107, 82, 17, 82, 52, 84, 71, 82, 107, 84,
                16, 84, 48, 86, 70, 84, 107, 86, 16, 86, 46, 88, 70, 86, 105, 88,
                16, 88, 44, 90, 70, 88, 87, 90, 89, 88, 104, 90, 16, 90, 42, 92,
                71, 90, 86, 92, 91, 90, 102, 92, 17, 92, 42, 94, 72, 92, 85, 94,
                93, 92, 100, 94, 19, 94, 42, 96, 74, 94, 84, 96, 21, 96, 42, 98,
                76, 96, 81, 98, 25, 98, 42, 100, 26, 100, 40, 102,
            },
            // Run 3: atlas bounds (1288, 206, 1712, 532)
            new byte[]
            {
                101, 2, 111, 4, 97, 4, 115, 6, 95, 6, 116, 8, 92, 8, 119, 10,
                91, 10, 120, 12, 90, 12, 121, 14, 89, 14, 122, 16, 88, 16, 122, 18,
                87, 18, 122, 20, 87, 20, 122, 22, 87, 22, 122, 24, 87, 24, 120, 26,
                87, 26, 118, 28, 87, 28, 116, 30, 88, 30, 107, 32, 88, 32, 105, 34,
                88, 34, 105, 36, 87, 36, 105, 38, 87, 38, 104, 40, 86, 40, 104, 42,
                84, 42, 104, 44, 55, 44, 75, 46, 83, 44, 104, 46, 3, 46, 8, 48,
                50, 46, 105, 48, 2, 48, 11, 50, 45, 48, 104, 50, 2, 50, 15, 52,
                39, 50, 104, 52, 3, 52, 20, 54, 30, 52, 103, 54, 4, 54, 103, 56,
                6, 56, 102, 58, 7, 58, 102, 60, 8, 60, 101, 62, 10, 62, 99, 64,
                12, 64, 98, 66, 15, 66, 97, 68, 18, 68, 96, 70, 22, 70, 94, 72,
                26, 72, 94, 74, 32, 74, 95, 76, 30, 76, 96, 78, 29, 78, 97, 80,
                29, 80, 97, 82, 29, 82, 97, 84, 29, 84, 58, 86, 69, 84, 96, 86,
                30, 86, 57, 88, 67, 86, 95, 88, 31, 88, 55, 90, 67, 88, 94, 90,
                33, 90, 54, 92, 67, 90, 91, 92, 35, 92, 52, 94, 68, 92, 88, 94,
                44, 94, 50, 96, 70, 94, 82, 96, 71, 96, 79, 98,
            },
            // Run 4: atlas bounds (1724, 201, 2154, 548)
            new byte[]
            {
                101, 0, 110, 2, 97, 2, 114, 4, 95, 4, 116, 6, 93, 6, 118, 8,
                91, 8, 119, 10, 90, 10, 120, 12, 89, 12, 121, 14, 88, 14, 122, 16,
                88, 16, 122, 18, 86, 18, 122, 20, 86, 20, 122, 22, 86, 22, 120, 24,
                86, 24, 118, 26, 86, 26, 116, 28, 87, 28, 106, 30, 88, 30, 105, 32,
                88, 32, 105, 34, 87, 34, 104, 36, 86, 36, 104, 38, 86, 38, 104, 40,
                85, 40, 104, 42, 84, 42, 104, 44, 54, 44, 74, 46, 82, 44, 104, 46,
                2, 46, 5, 48, 49, 46, 104, 48, 1, 48, 8, 50, 46, 48, 104, 50,
                1, 50, 12, 52, 42, 50, 104, 52, 1, 52, 15, 54, 37, 52, 103, 54,
                2, 54, 103, 56, 3, 56, 102, 58, 5, 58, 102, 60, 6, 60, 101, 62,
                7, 62, 100, 64, 9, 64, 99, 66, 11, 66, 98, 68, 14, 68, 96, 70,
                17, 70, 94, 72, 20, 72, 95, 74, 26, 74, 99, 76, 31, 76, 104, 78,
                30, 78, 107, 80, 28, 80, 110, 82, 26, 82, 111, 84, 25, 84, 54, 86,
                73, 84, 111, 86, 24, 86, 52, 88, 74, 86, 111, 88, 24, 88, 49, 90,
                75, 88, 111, 90, 23, 90, 49, 92, 76, 90, 109, 92, 23, 92, 49, 94,
                78, 92, 107, 94, 23, 94, 49, 96, 79, 94, 95, 96, 96, 94, 105, 96,
                23, 96, 48, 98, 80, 96, 93, 98, 99, 96, 103, 98, 23, 98, 47, 100,
                83, 98, 91, 100, 25, 100, 40, 102,
            },
        };
    }
}
