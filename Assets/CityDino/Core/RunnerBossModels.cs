using System;

namespace CityDino
{
    public enum BossPhase { None, Intro, Fighting, Victory }
    public enum BossKind { Tank, Sentinel, Gunship, Submarine }
    public enum BossAttackKind { Cannon, Artillery, LaserLow, LaserHigh, Shockwave, Missile, Drone, DroneLow, MineDrop, CrateDrop, ElectricPillar, ShieldDrone, AirGate, AirSpread, AirLaser, AirMine, TankRam, RobotDash, GunshipDive, CrossMissile, MissileRain, ElectricOrb, Torpedo, FishShoal, SeaMine, SonarBeam, DepthCharge, SubmarineRam }
    public enum BossAction { Idle, Windup, Strike, Recovery }

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
        public BossAction Action;
        public float ActionProgress;
        public BossAttackKind AttackKind;
        public bool FacingLeft = true;
        public float ContactX, ContactY, ContactWidth, ContactHeight;
        public bool ContactActive;
        public int ChargeSourceTelegraphId;
        public bool FullSweep;
        public float SweepEndX;
        public int Tier = 1;
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
        public float Radius;
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
        public float GapY;
        public float GapHeight;
        public float GapX, GapWidth;
        public float AimX, AimY;
        public float OriginX, OriginY;
        public float VelocityX, VelocityY;
        // Delayed combo attacks take one final snapshot when their local cue
        // becomes visible. They never track the player after that snapshot.
        public bool LockOnReveal;
        public float AimOffsetX, AimOffsetY;
        public bool FullSweep;
        public float SweepEndX;
        public bool Visible { get { return DelayRemaining <= 0f; } }
    }
}
