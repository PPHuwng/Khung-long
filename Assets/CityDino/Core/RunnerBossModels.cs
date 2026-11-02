using System;

namespace CityDino
{
    public enum BossPhase { None, Intro, Fighting, Victory }
    public enum BossKind { Tank, Sentinel, Gunship }
    public enum BossAttackKind { Cannon, Artillery, LaserLow, LaserHigh, Shockwave, Missile, Drone, DroneLow }

    /// <summary>Visible body and combat state; all coordinates use the runner's bottom-up world space.</summary>
    public sealed class BossEncounter
    {
        public BossKind Kind;
        public float Health;
        public float MaxHealth;
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public bool Vulnerable;
        public string AttackName = "";
        public float AttackProgress;
        public int RageLevel = 1;
        public float HitFlashRemaining;
        public float ShotFlashRemaining;
        public float LandingFlashRemaining;
        public float Recoil;
    }

    public sealed class BossProjectile
    {
        public int Id;
        public bool Friendly;
        public BossAttackKind Kind;
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float VelocityX;
        public float VelocityY;
        public bool Removed;
        public float Lifetime = 8f;
        public int SourceTelegraphId;
    }

    /// <summary>Warning first, then an optional live beam or ground blast in this exact rectangle.</summary>
    public sealed class BossTelegraph
    {
        public int Id;
        public BossAttackKind Kind;
        public float X;
        public float Y;
        public float Width;
        public float Height;
        public float Remaining;
        public float Duration;
        public bool Active;
        public float DelayRemaining;
        public int SourceTelegraphId;
        public bool Visible { get { return DelayRemaining <= 0f; } }
    }
}
