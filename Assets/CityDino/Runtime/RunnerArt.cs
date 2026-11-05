using UnityEngine;

namespace CityDino
{
    /// <summary>Loads build-safe copies of the artwork supplied with the project.</summary>
    public sealed class RunnerArt
    {
        public readonly Texture2D Background;
        public readonly Texture2D CityBackdrop;
        public readonly Texture2D Road;
        public readonly Texture2D PlatformLong;
        public readonly Texture2D PlatformShort;
        public readonly Texture2D Star;
        public readonly Texture2D Heart;
        public readonly Texture2D House1;
        public readonly Texture2D House2;
        public readonly Texture2D House3;
        public readonly Texture2D Hydrant;
        public readonly Texture2D Plane;
        public readonly Texture2D Tank;
        public readonly Texture2D SkylineFar;
        public readonly Texture2D SkylineNear;
        public readonly Texture2D HazardAtlas;
        public readonly Texture2D DinoActionsAtlas;
        public readonly Texture2D DinoRunAtlas;
        public readonly Texture2D PickupAtlas;
        public readonly Texture2D BiomeHazardAtlas;
        public readonly Texture2D TriceratopsAtlas;
        public readonly Texture2D RaptorAtlas;
        public readonly Texture2D BossAtlas;
        public readonly Texture2D FlightDevice;
        public readonly Texture2D Hoverboard;
        public readonly Texture2D BossActionAtlas;
        public readonly Texture2D OrdnanceAtlas;
        public readonly Texture2D ArenaBackdropAtlas;
        public readonly Texture2D ArenaGateAtlas;
        public readonly Texture2D SubmarineAtlas;
        public readonly Texture2D DivingKitAtlas;
        public readonly Texture2D UnderwaterBackdrop;
        public readonly Rect HoverboardUV = new Rect(46f / 2172f, 161f / 724f, 2080f / 2172f, 378f / 724f);
        // The earlier pack crop remains available for old resource consumers;
        // the current cinematic and ride use the hoverboard above.
        public readonly Rect FlightDeviceUV = new Rect(0.067708333f, 0.173828125f, 0.819010417f, 0.658203125f);
        public readonly Rect SkylineFarUV = new Rect(0f, 0f, 1f, 0.7651934f);
        public readonly Rect SkylineNearUV = new Rect(0f, 0.0441989f, 1f, 0.730663f);
        public readonly Texture2D[] DinoFrames;
        public readonly Font Font;

        // Tight visible-content bounds in the generated 2172 x 724 atlas.
        // Sampling bounds trim transparent cell padding without modifying the artwork.
        private static readonly Rect[] HazardUvs =
        {
            new Rect(0.0520258f, 0.2444751f, 0.1500921f, 0.5013812f),
            new Rect(0.2587477f, 0.1685083f, 0.230663f, 0.6878453f),
            new Rect(0.5105893f, 0.25f, 0.2302026f, 0.4226519f),
            new Rect(0.7767035f, 0.2251381f, 0.1970534f, 0.5939227f)
        };

        // Crouch frames 5..7, then jump launch/tuck/landing frames 8..10.
        // Common-size, feet-aligned sampling rectangles preserve proportions.
        private static readonly Rect[] DinoActionUvs =
        {
            new Rect(0.012369792f, 0.083984375f, 0.322265625f, 0.181640625f),
            new Rect(0.346354167f, 0.082031250f, 0.322265625f, 0.181640625f),
            new Rect(0.673828125f, 0.082031250f, 0.322265625f, 0.181640625f),
            new Rect(0.020833333f, 0.430664062f, 0.292317708f, 0.435546875f),
            new Rect(0.350260417f, 0.549804688f, 0.292317708f, 0.435546875f),
            new Rect(0.684244792f, 0.440429688f, 0.292317708f, 0.435546875f),
        };

        // Tight, non-overlapping crops from the 2172 x 724 running sheet.
        // Their placement below preserves one common 434 x 354 body canvas;
        // sampling a full-width cell would pick up adjacent frame pixels.
        private static readonly Rect[] DinoRunUvs =
        {
            new Rect(0.008287293f, 0.243093923f, 0.190147330f, 0.482044199f),
            new Rect(0.203038674f, 0.241712707f, 0.188766114f, 0.483425414f),
            new Rect(0.392265193f, 0.243093923f, 0.194290976f, 0.482044199f),
            new Rect(0.593001842f, 0.265193370f, 0.195211786f, 0.450276243f),
            new Rect(0.793738490f, 0.243093923f, 0.197974217f, 0.479281768f),
        };
        private static readonly Rect[] DinoRunPlacement =
        {
            new Rect(0.043778802f, 0.005649718f, 0.951612903f, 0.985875706f),
            new Rect(0.050691244f, 0.005649718f, 0.944700461f, 0.988700565f),
            new Rect(0.023041475f, 0.005649718f, 0.972350230f, 0.985875706f),
            new Rect(0.018433180f, 0.025423729f, 0.976958525f, 0.920903955f),
            new Rect(0.004608295f, 0.011299435f, 0.990783410f, 0.980225989f),
        };

        // Water, hot drink, shield, magnet, double score, warming thermos.
        // Square alpha-bounded viewports preserve each icon's proportions.
        private static readonly Rect[] PickupUvs =
        {
            new Rect(0.044596354f, 0.505859375f, 0.287760417f, 0.431640625f),
            new Rect(0.352539062f, 0.505859375f, 0.293619792f, 0.440429688f),
            new Rect(0.690755208f, 0.507812500f, 0.253255208f, 0.379882812f),
            new Rect(0.060546875f, 0.069824219f, 0.249348958f, 0.374023438f),
            new Rect(0.367838542f, 0.079589844f, 0.259114583f, 0.388671875f),
            new Rect(0.694010417f, 0.050781250f, 0.287109375f, 0.430664062f),
        };
        public Rect WarmReserveUV { get { return PickupUvs[5]; } }

        // Falling ice, geothermal plume, windblown metal sign. Bounds are
        // sampled from the transparent atlas; physics owns each world rectangle.
        private static readonly Rect[] BiomeHazardUvs =
        {
            new Rect(0.091620626f, 0.058011050f, 0.125690608f, 0.908839779f),
            new Rect(0.357734807f, 0.034530387f, 0.209484346f, 0.932320442f),
            new Rect(0.664825046f, 0.244475138f, 0.312615101f, 0.363259669f),
        };

        // Three running poses, crouch, jump rise and jump landing. Tight alpha
        // crops retain natural proportions inside a shared canvas per character.
        private static readonly Rect[] TriceratopsUvs =
        {
            new Rect(0.016927083f, 0.516601562f, 0.313802083f, 0.304687500f),
            new Rect(0.360026042f, 0.521484375f, 0.292317708f, 0.303710938f),
            new Rect(0.685546875f, 0.521484375f, 0.291666667f, 0.299804688f),
            new Rect(0.018880208f, 0.103515625f, 0.311197917f, 0.224609375f),
            new Rect(0.361979167f, 0.149414062f, 0.279947917f, 0.278320312f),
            new Rect(0.678385417f, 0.103515625f, 0.305989583f, 0.286132812f),
        };
        private static readonly Rect[] RaptorUvs =
        {
            new Rect(0.040364583f, 0.559570312f, 0.269531250f, 0.281250000f),
            new Rect(0.368489583f, 0.559570312f, 0.253906250f, 0.281250000f),
            new Rect(0.686848958f, 0.559570312f, 0.271484375f, 0.281250000f),
            new Rect(0.039062500f, 0.147460938f, 0.287109375f, 0.182617188f),
            new Rect(0.383463542f, 0.166015625f, 0.239583333f, 0.254882812f),
            new Rect(0.704427083f, 0.131835938f, 0.257161458f, 0.263671875f),
        };
        private static readonly Rect[] TriceratopsPlacement =
        {
            new Rect(0.008163265f, 0.299334812f, 0.983673469f, 0.691796009f),
            new Rect(0.075510204f, 0.290465632f, 0.916326531f, 0.689578714f),
            new Rect(0.077551020f, 0.299334812f, 0.914285714f, 0.680709534f),
            new Rect(0.183050847f, 0.020920502f, 0.810169492f, 0.962343096f),
            new Rect(0.114285714f, 0.359201774f, 0.877551020f, 0.631929047f),
            new Rect(0.032653061f, 0.341463415f, 0.959183673f, 0.649667406f),
        };
        private static readonly Rect[] RaptorPlacement =
        {
            new Rect(0.016470588f, 0.253196931f, 0.974117647f, 0.736572890f),
            new Rect(0.072941176f, 0.253196931f, 0.917647059f, 0.736572890f),
            new Rect(0.009411765f, 0.253196931f, 0.981176471f, 0.736572890f),
            new Rect(0.080578512f, 0.025510204f, 0.911157025f, 0.954081633f),
            new Rect(0.124705882f, 0.322250639f, 0.865882353f, 0.667519182f),
            new Rect(0.061176471f, 0.299232737f, 0.929411765f, 0.690537084f),
        };
        private static readonly Rect[] BossUvs =
        {
            new Rect(0.007826888f, 0.128453039f, 0.346685083f, 0.575966851f),
            new Rect(0.356813996f, 0.128453039f, 0.241712707f, 0.697513812f),
            new Rect(0.618324125f, 0.175414365f, 0.373848987f, 0.494475138f),
        };

        public RunnerArt()
        {
            Background = Load("Background");
            CityBackdrop = Load("CityBackdrop");
            Road = Load("Road");
            PlatformLong = Load("PlatformLong");
            PlatformShort = Load("PlatformShort");
            Star = Load("Star");
            Heart = Load("Heart");
            House1 = Load("House1");
            House2 = Load("House2");
            House3 = Load("House3");
            Hydrant = Load("Hydrant");
            Plane = Load("Plane");
            Tank = Load("Tank");
            SkylineFar = Load("SkylineFar");
            SkylineNear = Load("SkylineNear");
            HazardAtlas = Load("HazardAtlas");
            DinoActionsAtlas = Load("DinoActionsAtlas");
            // Older copies of the project retain the five original frames as
            // a fallback while the new running sheet finishes importing.
            DinoRunAtlas = Resources.Load<Texture2D>("CityDino/DinoRunAtlas");
            PickupAtlas = Load("PickupAtlas");
            BiomeHazardAtlas = Load("BiomeHazardAtlas");
            TriceratopsAtlas = Resources.Load<Texture2D>("CityDino/TriceratopsAtlas");
            RaptorAtlas = Resources.Load<Texture2D>("CityDino/RaptorAtlas");
            BossAtlas = Resources.Load<Texture2D>("CityDino/BossAtlas");
            FlightDevice = Resources.Load<Texture2D>("CityDino/FlightDevice");
            Hoverboard = Resources.Load<Texture2D>("CityDino/Hoverboard");
            BossActionAtlas = Resources.Load<Texture2D>("CityDino/BossActionAtlas");
            OrdnanceAtlas = Resources.Load<Texture2D>("CityDino/OrdnanceAtlas");
            ArenaBackdropAtlas = Resources.Load<Texture2D>("CityDino/ArenaBackdropAtlas");
            ArenaGateAtlas = Resources.Load<Texture2D>("CityDino/ArenaGateAtlas");
            SubmarineAtlas = Resources.Load<Texture2D>("CityDino/SubmarineAtlas");
            DivingKitAtlas = Resources.Load<Texture2D>("CityDino/DivingKitAtlas");
            UnderwaterBackdrop = Resources.Load<Texture2D>("CityDino/UnderwaterBackdrop");
            DinoFrames = new Texture2D[5];
            for (int i = 0; i < DinoFrames.Length; i++)
                DinoFrames[i] = Load("Dino" + (i + 1));

            Font = Resources.Load<Font>("CityDino/RunnerFont");
            if (Font == null)
                Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        }

        public Rect GetHazardUV(int cell)
        {
            int index = System.Math.Max(0, System.Math.Min(HazardUvs.Length - 1, cell));
            return HazardUvs[index];
        }

        public Rect GetDinoActionUV(int frame)
        {
            int index = System.Math.Max(0, System.Math.Min(DinoActionUvs.Length - 1, frame - 5));
            return DinoActionUvs[index];
        }

        public Rect GetDinoRunUV(int frame)
        {
            int index = System.Math.Max(0, System.Math.Min(DinoRunUvs.Length - 1, frame));
            return DinoRunUvs[index];
        }

        public Rect GetDinoRunRect(int frame, Rect body, bool facingLeft = false)
        {
            int index = System.Math.Max(0, System.Math.Min(DinoRunPlacement.Length - 1, frame));
            Rect placement = DinoRunPlacement[index];
            float left = facingLeft ? 1f - placement.x - placement.width : placement.x;
            return new Rect(body.x + left * body.width, body.y + placement.y * body.height,
                placement.width * body.width, placement.height * body.height);
        }

        public Rect GetPickupUV(EntityKind kind)
        {
            int index = kind == EntityKind.Water ? 0 : kind == EntityKind.HotDrink ? 1
                : kind == EntityKind.Shield ? 2 : kind == EntityKind.Magnet ? 3 : 4;
            return PickupUvs[index];
        }

        public Rect GetBiomeHazardUV(EntityKind kind)
        {
            int index = kind == EntityKind.FallingIce ? 0 : kind == EntityKind.HeatVent ? 1 : 2;
            return BiomeHazardUvs[index];
        }

        public static int GetCharacterPoseCell(int frame)
        {
            if (frame >= 10) return 5;
            if (frame >= 8) return 4;
            if (frame >= 5) return 3;
            return frame == 2 ? 2 : frame == 1 || frame == 3 ? 1 : 0;
        }

        public Texture2D GetCharacterTexture(CharacterKind kind, int frame = 0)
        {
            if (kind == CharacterKind.Triceratops && TriceratopsAtlas != null) return TriceratopsAtlas;
            if (kind == CharacterKind.Raptor && RaptorAtlas != null) return RaptorAtlas;
            if (frame >= 5 && DinoActionsAtlas != null) return DinoActionsAtlas;
            if (frame < 5 && DinoRunAtlas != null) return DinoRunAtlas;
            return DinoFrames == null || DinoFrames.Length == 0 ? null
                : DinoFrames[System.Math.Max(0, frame) % DinoFrames.Length];
        }

        public Rect GetCharacterUV(CharacterKind kind, int frame)
        {
            if (kind == CharacterKind.Triceratops && TriceratopsAtlas != null)
                return TriceratopsUvs[GetCharacterPoseCell(frame)];
            if (kind == CharacterKind.Raptor && RaptorAtlas != null)
                return RaptorUvs[GetCharacterPoseCell(frame)];
            if (frame >= 5 && DinoActionsAtlas != null) return GetDinoActionUV(frame);
            if (frame < 5 && DinoRunAtlas != null) return GetDinoRunUV(frame);
            return new Rect(0, 0, 1, 1);
        }

        public Rect GetCharacterRect(CharacterKind kind, int frame, Rect body, bool facingLeft = false)
        {
            Rect placement;
            if (kind == CharacterKind.Triceratops && TriceratopsAtlas != null)
                placement = TriceratopsPlacement[GetCharacterPoseCell(frame)];
            else if (kind == CharacterKind.Raptor && RaptorAtlas != null)
                placement = RaptorPlacement[GetCharacterPoseCell(frame)];
            else return frame < 5 && DinoRunAtlas != null ? GetDinoRunRect(frame, body, facingLeft) : body;
            float left = facingLeft ? 1f - placement.x - placement.width : placement.x;
            return new Rect(body.x + left * body.width, body.y + placement.y * body.height,
                placement.width * body.width, placement.height * body.height);
        }

        // Generated source remains unchanged. Four separate action poses in
        // each row: tank, sentinel, gunship; per-tile alpha bounds exclude gutters.
        private static readonly Rect[] BossActionUvs =
        {
            new Rect(46f / 1536f, 736f / 1024f, 319f / 1536f, 204f / 1024f),
            new Rect(424f / 1536f, 736f / 1024f, 343f / 1536f, 231f / 1024f),
            new Rect(769f / 1536f, 736f / 1024f, 359f / 1536f, 188f / 1024f),
            new Rect(1175f / 1536f, 736f / 1024f, 333f / 1536f, 214f / 1024f),
            new Rect(54f / 1536f, 390f / 1024f, 270f / 1536f, 280f / 1024f),
            new Rect(433f / 1536f, 393f / 1024f, 334f / 1536f, 225f / 1024f),
            new Rect(769f / 1536f, 395f / 1024f, 358f / 1536f, 223f / 1024f),
            new Rect(1178f / 1536f, 392f / 1024f, 323f / 1536f, 210f / 1024f),
            new Rect(31f / 1536f, 103f / 1024f, 344f / 1536f, 171f / 1024f),
            new Rect(419f / 1536f, 75f / 1024f, 348f / 1536f, 226f / 1024f),
            new Rect(769f / 1536f, 41f / 1024f, 351f / 1536f, 277f / 1024f),
            new Rect(1183f / 1536f, 97f / 1024f, 325f / 1536f, 192f / 1024f),
        };

        public Rect GetBossActionUV(BossKind kind, int pose)
        {
            int row = kind == BossKind.Tank ? 0 : kind == BossKind.Sentinel ? 1 : 2;
            return BossActionUvs[row * 4 + System.Math.Max(0, System.Math.Min(3, pose))];
        }

        // Visible alpha bounds from the unchanged generated sheet: shell,
        // assault drone, circular mine and plasma emitter node.
        private static readonly Rect[] OrdnanceUvs =
        {
            new Rect(79f / 1536f, 670f / 1024f, 593f / 1536f, 211f / 1024f),
            new Rect(896f / 1536f, 552f / 1024f, 511f / 1536f, 448f / 1024f),
            new Rect(155f / 1536f, 58f / 1024f, 467f / 1536f, 443f / 1024f),
            new Rect(931f / 1536f, 80f / 1024f, 416f / 1536f, 400f / 1024f),
        };

        public Rect GetOrdnanceUV(int cell)
        {
            return OrdnanceUvs[System.Math.Max(0, System.Math.Min(3, cell))];
        }

        private static readonly Rect[] ArenaGateUvs =
        {
            new Rect(12f/1774f, 35f/887f, 864f/1774f, 811f/887f),
            new Rect(899f/1774f, 34f/887f, 862f/1774f, 836f/887f),
        };

        public Rect GetArenaGateUV(BossKind kind)
        {
            return ArenaGateUvs[kind == BossKind.Tank ? 0 : 1];
        }
        public Rect GetArenaBackdropUV(BossKind kind)
        {
            return new Rect(0f, kind == BossKind.Tank ? .5f : 0f, 1f, .5f);
        }

        // Tight alpha sampling bounds from unchanged naval artwork.
        private static readonly Rect[] SubmarineUvs =
        {
            new Rect(16f/2172f, 231f/724f, 511f/2172f, 297f/724f),
            new Rect(554f/2172f, 231f/724f, 515f/2172f, 316f/724f),
            new Rect(1101f/2172f, 168f/724f, 512f/2172f, 341f/724f),
            new Rect(1647f/2172f, 231f/724f, 511f/2172f, 294f/724f),
        };

        public Rect GetSubmarineUV(int cell)
        {
            return SubmarineUvs[System.Math.Max(0, System.Math.Min(3, cell))];
        }

        private static readonly Rect[] DivingKitUvs =
        {
            new Rect(15f/1262f, 681f/1246f, 549f/1262f, 520f/1246f),
            new Rect(653f/1262f, 732f/1246f, 592f/1262f, 388f/1246f),
            new Rect(22f/1262f, 180f/1246f, 575f/1262f, 273f/1246f),
            new Rect(653f/1262f, 47f/1246f, 583f/1262f, 585f/1246f),
        };

        public Rect GetDivingKitUV(int cell)
        {
            return DivingKitUvs[System.Math.Max(0, System.Math.Min(3, cell))];
        }

        public Rect GetBossUV(BossKind kind)
        {
            return BossUvs[System.Math.Max(0, System.Math.Min(2, (int)kind))];
        }

        private static Texture2D Load(string name)
        {
            Texture2D texture = Resources.Load<Texture2D>("CityDino/" + name);
            if (texture == null)
                Debug.LogError("Missing City Dino artwork: Resources/CityDino/" + name);
            return texture;
        }
    }
}
