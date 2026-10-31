using System;

namespace CityDino
{
    public enum CharacterKind { Classic, Triceratops, Raptor }

    public sealed partial class RunnerSave
    {
        public bool OwnsTriceratops;
        public bool OwnsRaptor;
        public CharacterKind SelectedCharacter;
    }

    /// <summary>
    /// Permanent character ownership and active skills. Every character keeps
    /// the ordinary movement rules; boss shooting is shared by all three.
    /// </summary>
    public sealed partial class RunnerSession
    {
        public const int TriceratopsPrice = 2500;
        public const int RaptorPrice = 5000;
        public const float GuardDuration = 3f;
        public const float GuardCooldown = 30f;
        public const float DashDuration = 0.35f;
        public const float DashCooldown = 16f;
        public const float CharacterDashSpeed = 900f;

        private CharacterKind runCharacter;
        private float skillCooldownRemaining;
        private float guardRemaining;
        private float dashRemaining;
        private float dashDirection = 1f;

        // Changing the selection after death prepares the next run without
        // changing the frozen character displayed in the finished encounter.
        public CharacterKind CurrentCharacter
        {
            get { return State == RunState.Menu ? Save.SelectedCharacter : runCharacter; }
        }
        public float SkillCooldownRemaining { get { return skillCooldownRemaining; } }
        public float SkillActiveRemaining
        {
            get { return CurrentCharacter == CharacterKind.Triceratops ? guardRemaining : dashRemaining; }
        }
        public float SkillCooldown
        {
            get { return CurrentCharacter == CharacterKind.Triceratops ? GuardCooldown
                : CurrentCharacter == CharacterKind.Raptor ? DashCooldown : 0f; }
        }
        public bool IsDashing
        {
            get { return runCharacter == CharacterKind.Raptor && dashRemaining > 0f; }
        }
        public float DashVelocity
        {
            get { return IsDashing ? dashDirection * CharacterDashSpeed : 0f; }
        }
        public bool SkillReady
        {
            get { return State == RunState.Playing && CurrentCharacter != CharacterKind.Classic &&
                skillCooldownRemaining <= 0f && (!IsBossArena || ArenaPhase == BossPhase.Fighting); }
        }

        public static int CharacterPrice(CharacterKind character)
        {
            switch (character)
            {
                case CharacterKind.Triceratops: return TriceratopsPrice;
                case CharacterKind.Raptor: return RaptorPrice;
                default: return 0;
            }
        }

        public static string CharacterName(CharacterKind character)
        {
            switch (character)
            {
                case CharacterKind.Triceratops: return "Giáp Sừng";
                case CharacterKind.Raptor: return "Tốc Ảnh";
                default: return "Cổ Dài";
            }
        }

        public static string CharacterSkillDescription(CharacterKind character)
        {
            switch (character)
            {
                case CharacterKind.Triceratops:
                    return "Giáp 3 giây, đỡ một đòn. Đỡ đạn boss sẽ phản công. Hồi 30 giây.";
                case CharacterKind.Raptor:
                    return "Lướt theo hướng di chuyển, tránh va chạm trong 0,35 giây. Hồi 16 giây.";
                default:
                    return "Né và nhảy cơ bản. Bắn được khi gặp boss, không có kỹ năng riêng.";
            }
        }

        public bool OwnsCharacter(CharacterKind character)
        {
            switch (character)
            {
                case CharacterKind.Classic: return true;
                case CharacterKind.Triceratops: return Save.OwnsTriceratops;
                case CharacterKind.Raptor: return Save.OwnsRaptor;
                default: return false;
            }
        }

        public bool BuyCharacter(CharacterKind character)
        {
            if ((State != RunState.Menu && State != RunState.GameOver) ||
                !Enum.IsDefined(typeof(CharacterKind), character) || OwnsCharacter(character))
                return false;
            int price = CharacterPrice(character);
            if (price <= 0 || Save.Wallet < price) return false;
            Save.Wallet -= price;
            if (character == CharacterKind.Triceratops) Save.OwnsTriceratops = true;
            else Save.OwnsRaptor = true;
            Save.SelectedCharacter = character;
            NotifySaveChanged();
            return true;
        }

        public bool SelectCharacter(CharacterKind character)
        {
            if ((State != RunState.Menu && State != RunState.GameOver) || !OwnsCharacter(character))
                return false;
            if (Save.SelectedCharacter == character) return true;
            Save.SelectedCharacter = character;
            NotifySaveChanged();
            return true;
        }

        public bool TryUseSkill()
        {
            if (!SkillReady) return false;
            if (runCharacter == CharacterKind.Triceratops)
            {
                guardRemaining = GuardDuration;
                skillCooldownRemaining = GuardCooldown;
            }
            else if (runCharacter == CharacterKind.Raptor)
            {
                dashDirection = Math.Abs(horizontalAxis) > 0.001f
                    ? (horizontalAxis < 0f ? -1f : 1f) : FacingLeft ? -1f : 1f;
                dashRemaining = DashDuration;
                skillCooldownRemaining = DashCooldown;
            }
            else return false;
            return true;
        }

        private static void SanitizeCharacterSave(RunnerSave save)
        {
            bool selectedOwned = save.SelectedCharacter == CharacterKind.Classic ||
                (save.SelectedCharacter == CharacterKind.Triceratops && save.OwnsTriceratops) ||
                (save.SelectedCharacter == CharacterKind.Raptor && save.OwnsRaptor);
            if (!selectedOwned) save.SelectedCharacter = CharacterKind.Classic;
        }

        private void BeginCharacterRun()
        {
            SanitizeCharacterSave(Save);
            runCharacter = Save.SelectedCharacter;
            skillCooldownRemaining = guardRemaining = dashRemaining = 0f;
            dashDirection = 1f;
        }

        private void TickCharacter(float dt)
        {
            if (State != RunState.Playing || !IsFinite(dt) || dt <= 0f) return;
            skillCooldownRemaining = Math.Max(0f, skillCooldownRemaining - dt);
            guardRemaining = Math.Max(0f, guardRemaining - dt);
            dashRemaining = Math.Max(0f, dashRemaining - dt);
        }

        // Called only for real physical hazards/projectiles. Fall damage and
        // survival depletion deliberately bypass this interception path.
        private bool TryAbsorbCharacterHit(bool reflectable)
        {
            if (State != RunState.Playing) return false;
            if (IsDashing) return true;
            if (runCharacter != CharacterKind.Triceratops || guardRemaining <= 0f) return false;
            guardRemaining = 0f;
            InvincibleRemaining = Math.Max(InvincibleRemaining, 1f);
            if (reflectable && IsBossArena) ReflectBossAttack();
            return true;
        }
    }
}
