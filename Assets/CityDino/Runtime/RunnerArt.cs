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
        public readonly Texture2D[] DinoFrames;
        public readonly Font Font;

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
            DinoFrames = new Texture2D[5];
            for (int i = 0; i < DinoFrames.Length; i++)
                DinoFrames[i] = Load("Dino" + (i + 1));

            Font = Resources.Load<Font>("CityDino/RunnerFont");
            if (Font == null)
                Font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
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
