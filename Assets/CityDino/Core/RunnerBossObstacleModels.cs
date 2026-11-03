namespace CityDino
{
    /// <summary>Physical boss-created scenery in the same bottom-up space as the player.</summary>
    public sealed class BossObstacle
    {
        public int Id;
        public int GroupId;
        public int SourceTelegraphId;
        public BossAttackKind Kind;
        public float X, Y, Width, Height;
        public float VelocityX, VelocityY;
        public float Remaining;
        public float Health, MaxHealth;
        public float WarningRemaining;
        public float Radius;
        public float BaseY, MotionAmplitude, MotionSpeed;
        public float HitFlashRemaining;
        public bool Removed;
        public bool Active = true;
        public bool Destructible { get { return MaxHealth > 0f; } }
    }
}
