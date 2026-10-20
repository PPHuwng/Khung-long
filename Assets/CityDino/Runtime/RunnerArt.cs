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

        private static Texture2D Load(string name)
        {
            Texture2D texture = Resources.Load<Texture2D>("CityDino/" + name);
            if (texture == null)
                Debug.LogError("Missing City Dino artwork: Resources/CityDino/" + name);
            return texture;
        }
    }
}
