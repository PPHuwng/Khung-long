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
        public readonly Texture2D PickupAtlas;
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
            PickupAtlas = Load("PickupAtlas");
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

        public Rect GetPickupUV(EntityKind kind)
        {
            int index = kind == EntityKind.Water ? 0 : kind == EntityKind.HotDrink ? 1
                : kind == EntityKind.Shield ? 2 : kind == EntityKind.Magnet ? 3 : 4;
            return PickupUvs[index];
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
