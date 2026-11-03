using System;

namespace CityDino
{
    /// <summary>Attack choreography drives the live boss body and its visible leading weapon.</summary>
    public sealed partial class RunnerSession
    {
        private bool bossChargeActive;
        private double bossChargeStartedAt;
        private float bossChargeStartX, bossChargeStartY, bossChargeAimX, bossChargeAimY;
        private float bossChargeLaneY, bossChargeLaneHeight;
        private BossAttackKind bossChargeKind;
        private int bossChargeSourceId;

        private static bool IsBossCharge(BossAttackKind kind)
        {
            return kind == BossAttackKind.TankRam || kind == BossAttackKind.RobotDash || kind == BossAttackKind.GunshipDive;
        }

        private static float BossMotionEase(float value)
        {
            value = Math.Max(0f, Math.Min(1f, value));
            return value * value * (3f - 2f * value);
        }

        private void ResetBossMotion()
        {
            bossChargeActive = false;
            bossChargeStartedAt = 0d;
            bossChargeSourceId = 0;
            if (CurrentBoss == null) return;
            CurrentBoss.ContactActive = false;
            CurrentBoss.ChargeSourceTelegraphId = 0;
            CurrentBoss.Action = BossAction.Idle;
            CurrentBoss.ActionProgress = 0f;
            CurrentBoss.FacingLeft = true;
        }

        private void BeginBossCharge(BossTelegraph source)
        {
            if (CurrentBoss == null || source == null || !IsBossCharge(source.Kind) ||
                State != RunState.Playing || ArenaPhase != BossPhase.Fighting) return;
            bossChargeActive = true;
            bossChargeStartedAt = bossClock;
            bossChargeStartX = CurrentBoss.X;
            bossChargeStartY = CurrentBoss.Y;
            bossChargeAimX = Math.Max(100f, Math.Min(500f, source.AimX));
            bossChargeLaneY = source.Y;
            bossChargeLaneHeight = source.Height;
            bossChargeAimY = source.Kind == BossAttackKind.GunshipDive
                ? Math.Max(20f, Math.Min(250f, source.Y - 20f)) : 0f;
            bossChargeKind = source.Kind;
            bossChargeSourceId = source.SourceTelegraphId > 0 ? source.SourceTelegraphId : source.Id;
            CurrentBoss.ChargeSourceTelegraphId = bossChargeSourceId;
            CurrentBoss.AttackKind = bossChargeKind;
            CurrentBoss.Action = BossAction.Strike;
            CurrentBoss.ActionProgress = 0f;
            CurrentBoss.ContactActive = false;
            CurrentBoss.ShotFlashRemaining = .22f;
        }

        private bool ApplyBossCharge(float homeX, float homeY)
        {
            if (!bossChargeActive || ArenaPhase != BossPhase.Fighting) return false;
            float strikeDuration = 1.1f / BossPracticePace;
            float recoveryDuration = .85f / BossPracticePace;
            float age = (float)(bossClock - bossChargeStartedAt);
            if (age >= strikeDuration + recoveryDuration)
            {
                bossChargeActive = false;
                CurrentBoss.ContactActive = false;
                CurrentBoss.Action = BossAction.Idle;
                CurrentBoss.ActionProgress = 0f;
                return false;
            }
            CurrentBoss.AttackKind = bossChargeKind;
            CurrentBoss.FacingLeft = true;
            if (age < strikeDuration)
            {
                float progress = Math.Max(0f, age / strikeDuration);
                // A constant horizontal sweep makes both the warning and the
                // time to contact readable; recovery eases back into patrol.
                CurrentBoss.X = bossChargeStartX + (bossChargeAimX - bossChargeStartX) * progress;
                CurrentBoss.Y = bossChargeStartY + (bossChargeAimY - bossChargeStartY) * BossMotionEase(progress * 4f);
                CurrentBoss.Action = BossAction.Strike;
                CurrentBoss.ActionProgress = progress;
                CurrentBoss.ContactActive = bossChargeKind != BossAttackKind.GunshipDive || progress >= .25f;
            }
            else
            {
                float progress = Math.Max(0f, (age - strikeDuration) / recoveryDuration);
                float eased = BossMotionEase(progress);
                CurrentBoss.X = bossChargeAimX + (homeX - bossChargeAimX) * eased;
                CurrentBoss.Y = bossChargeAimY + (homeY - bossChargeAimY) * eased;
                CurrentBoss.Action = BossAction.Recovery;
                CurrentBoss.ActionProgress = progress;
                CurrentBoss.ContactActive = false;
            }
            CurrentBoss.ContactX = CurrentBoss.X + 8f;
            CurrentBoss.ContactY = bossChargeLaneY;
            CurrentBoss.ContactWidth = bossChargeKind == BossAttackKind.TankRam ? 68f
                : bossChargeKind == BossAttackKind.RobotDash ? 76f : 96f;
            CurrentBoss.ContactHeight = bossChargeLaneHeight;
            CurrentBoss.ChargeSourceTelegraphId = bossChargeSourceId;
            return true;
        }

        private void UpdateBossAction()
        {
            if (CurrentBoss == null || ArenaPhase != BossPhase.Fighting || bossChargeActive) return;
            CurrentBoss.AttackKind = primaryBossAttack;
            CurrentBoss.FacingLeft = true;
            CurrentBoss.ContactActive = false;
            BossTelegraph next = null;
            for (int i = 0; i < bossTelegraphs.Count; i++)
            {
                BossTelegraph mark = bossTelegraphs[i];
                if (!mark.Visible || mark.Active || mark.Remaining <= 0f) continue;
                if (next == null || mark.Remaining < next.Remaining) next = mark;
            }
            // A staged second warning must not hide the firing pose of the
            // first volley. A charge warning still owns its full windup.
            if (next != null && (CurrentBoss.ShotFlashRemaining <= 0f || IsBossCharge(next.Kind)))
            {
                CurrentBoss.AttackKind = next.Kind;
                CurrentBoss.Action = BossAction.Windup;
                CurrentBoss.ActionProgress = Math.Max(0f, Math.Min(1f, 1f - next.Remaining / Math.Max(.001f, next.Duration)));
                if (IsBossCharge(next.Kind)) CurrentBoss.ChargeSourceTelegraphId = next.SourceTelegraphId;
            }
            else if (CurrentBoss.ShotFlashRemaining > 0f)
            {
                CurrentBoss.Action = BossAction.Strike;
                CurrentBoss.ActionProgress = 1f - Math.Min(1f, CurrentBoss.ShotFlashRemaining / .22f);
            }
            else if (CurrentBoss.Recoil > .01f)
            {
                CurrentBoss.Action = BossAction.Recovery;
                CurrentBoss.ActionProgress = 1f - Math.Min(1f, CurrentBoss.Recoil);
            }
            else
            {
                CurrentBoss.Action = BossAction.Idle;
                CurrentBoss.ActionProgress = CurrentBoss.AttackProgress;
            }
        }

        private void TickBossChargeContact()
        {
            if (CurrentBoss == null || !CurrentBoss.ContactActive || !bossChargeActive ||
                State != RunState.Playing || ArenaPhase != BossPhase.Fighting || InvincibleRemaining > 0f) return;
            if (RunnerGeometry.RectTouchesPlayer(CurrentBoss.ContactX, CurrentBoss.ContactY,
                CurrentBoss.ContactWidth, CurrentBoss.ContactHeight, this) && !TryAbsorbHazard(false))
                TakeHit("Trúng " + AttackName(bossChargeKind));
        }
    }
}
