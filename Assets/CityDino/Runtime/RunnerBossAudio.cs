using System;
using System.Collections.Generic;
using UnityEngine;

namespace CityDino
{
    public enum BossSoundKind { Windup, Cannon, Missile, Electric, Laser, Charge, Impact, Defeat, Blast, Sonar, Torpedo, Splash, Swarm, UnderwaterDefeat }

    public struct BossSoundCue
    {
        public BossSoundKind Kind;
        public float Pan;
        public BossSoundCue(BossSoundKind kind, float pan = 0f) { Kind = kind; Pan = pan; }
    }

    /// <summary>Observes simulation identities, never render passes or wall time.</summary>
    public sealed class BossSoundTracker
    {
        private BossEncounter encounter;
        private readonly HashSet<int> warned = new HashSet<int>();
        private readonly HashSet<int> shots = new HashSet<int>();
        private readonly HashSet<int> obstacles = new HashSet<int>();
        private readonly HashSet<int> live = new HashSet<int>();
        private readonly List<BossSoundCue> cues = new List<BossSoundCue>();
        private readonly HashSet<int> frameGroups = new HashSet<int>();
        private readonly Predicate<int> expiredIdentity;
        private bool victory;
        private bool submerged, surfaced;
        private int charge;
        private float previousHealth, lastWindup = -10f;
        public int TrackedCount { get { return warned.Count + shots.Count + obstacles.Count; } }

        public BossSoundTracker() { expiredIdentity = IsExpiredIdentity; }
        private bool IsExpiredIdentity(int id) { return !live.Contains(id); }

        public IReadOnlyList<BossSoundCue> Observe(RunnerSession session, bool silent)
        {
            cues.Clear(); frameGroups.Clear();
            BossEncounter boss = session == null ? null : session.CurrentBoss;
            if (!ReferenceEquals(boss, encounter))
            {
                encounter = boss; warned.Clear(); shots.Clear(); obstacles.Clear(); charge = 0;
                victory = submerged = surfaced = false; lastWindup = -10f;
                previousHealth = boss == null ? 0f : boss.Health;
            }
            if (session == null || boss == null || !session.IsBossArena) return cues;
            bool playing = session.State == RunState.Playing;
            if (session.IsUnderwaterBoss)
            {
                if (!submerged && session.WaterDepth > .02f)
                { submerged = true; Emit(BossSoundKind.Splash, 0f); }
                if (submerged && !surfaced && session.ArenaPhase == BossPhase.Victory && session.WaterDepth < .02f)
                { surfaced = true; Emit(BossSoundKind.Splash, 0f); }
            }
            if (session.ArenaPhase == BossPhase.Victory)
            {
                if (!victory) { victory = true; Emit(session.IsUnderwaterBoss ? BossSoundKind.UnderwaterDefeat : BossSoundKind.Defeat, 0f); }
                previousHealth = boss.Health;
            }
            else if (session.ArenaPhase == BossPhase.Fighting)
            {
                bool newWarning = false; float warningPan = 0f;
                live.Clear();
                for (int i = 0; i < session.BossTelegraphs.Count; i++)
                {
                    BossTelegraph mark = session.BossTelegraphs[i];
                    if (!mark.Visible || mark.Active) continue;
                    live.Add(mark.Id);
                    if (!warned.Add(mark.Id)) continue;
                    newWarning = true;
                    warningPan = mark.VelocityX > 0f ? -.65f : mark.VelocityX < 0f ? .65f : .2f;
                }
                warned.RemoveWhere(expiredIdentity);
                // A rain wave may have six warning strips. One charging sound
                // communicates that wave without stacking six loud clips.
                if (newWarning && session.BossElapsed - lastWindup >= .28f)
                { lastWindup = session.BossElapsed; Emit(session.IsUnderwaterBoss ? BossSoundKind.Sonar : BossSoundKind.Windup, warningPan); }
                live.Clear();
                for (int i = 0; i < session.BossProjectiles.Count; i++)
                {
                    BossProjectile shot = session.BossProjectiles[i];
                    if (shot.Removed || shot.Friendly) continue;
                    live.Add(shot.Id);
                    if (!shots.Add(shot.Id)) continue;
                    BossSoundKind kind = SoundForAttack(shot.Kind);
                    float pan = shot.VelocityX > 0f ? -.65f : shot.VelocityX < 0f ? .65f : 0f;
                    Emit(kind, pan);
                }
                for (int i = 0; i < session.BossTelegraphs.Count; i++)
                    if (session.BossTelegraphs[i].Active) live.Add(-session.BossTelegraphs[i].Id);
                shots.RemoveWhere(expiredIdentity);
                if (boss.ContactActive && boss.ChargeSourceTelegraphId != charge)
                {
                    charge = boss.ChargeSourceTelegraphId;
                    Emit(session.IsUnderwaterBoss ? BossSoundKind.Torpedo : BossSoundKind.Charge, .2f);
                }
                for (int i = 0; i < session.BossTelegraphs.Count; i++)
                {
                    BossTelegraph mark = session.BossTelegraphs[i];
                    if (mark.Active && shots.Add(-mark.Id))
                        Emit(mark.Kind == BossAttackKind.SonarBeam ? BossSoundKind.Sonar
                            : mark.Kind == BossAttackKind.DepthCharge ? BossSoundKind.Splash
                            : mark.Kind == BossAttackKind.Artillery || mark.Kind == BossAttackKind.Missile ||
                            mark.Kind == BossAttackKind.MissileRain ? BossSoundKind.Blast : BossSoundKind.Laser, .25f);
                }
                // Active beams have separate identities; keep them only while
                // live so the bookkeeping stays bounded during long practice.
                for (int i = 0; i < session.BossTelegraphs.Count; i++)
                    if (session.BossTelegraphs[i].Active) live.Add(-session.BossTelegraphs[i].Id);
                shots.RemoveWhere(expiredIdentity);
                if (boss.Health < previousHealth && boss.Health > 0f) Emit(BossSoundKind.Impact, .25f);
                live.Clear();
                for (int i = 0; i < session.BossObstacles.Count; i++)
                {
                    BossObstacle obstacle = session.BossObstacles[i];
                    if (obstacle.Removed || !obstacle.Active) continue;
                    live.Add(obstacle.Id);
                    if (!obstacles.Add(obstacle.Id)) continue;
                    Emit(obstacle.Kind == BossAttackKind.SeaMine ? BossSoundKind.Splash
                        : obstacle.Kind == BossAttackKind.ElectricPillar || obstacle.Kind == BossAttackKind.AirGate ||
                        obstacle.Kind == BossAttackKind.ShieldDrone ? BossSoundKind.Electric : BossSoundKind.Impact, .25f);
                }
                obstacles.RemoveWhere(expiredIdentity);
                previousHealth = boss.Health;
            }
            if (silent || !playing) cues.Clear();
            return cues;
        }

        private void Emit(BossSoundKind kind, float pan)
        {
            int side = pan < -.1f ? 0 : pan > .1f ? 2 : 1;
            int key = (int)kind * 3 + side;
            if (frameGroups.Add(key)) cues.Add(new BossSoundCue(kind, pan));
        }

        private static BossSoundKind SoundForAttack(BossAttackKind kind)
        {
            if (kind == BossAttackKind.Torpedo || kind == BossAttackKind.SubmarineRam) return BossSoundKind.Torpedo;
            if (kind == BossAttackKind.FishShoal) return BossSoundKind.Swarm;
            if (kind == BossAttackKind.DepthCharge || kind == BossAttackKind.SeaMine) return BossSoundKind.Splash;
            if (kind == BossAttackKind.SonarBeam) return BossSoundKind.Sonar;
            if (kind == BossAttackKind.ElectricOrb || kind == BossAttackKind.Shockwave) return BossSoundKind.Electric;
            if (kind == BossAttackKind.CrossMissile || kind == BossAttackKind.MissileRain || kind == BossAttackKind.Missile ||
                kind == BossAttackKind.AirSpread) return BossSoundKind.Missile;
            return BossSoundKind.Cannon;
        }
    }

    /// <summary>One warning and one launch sound per running volley, including silent frames.</summary>
    public sealed class RunMissileSoundTracker
    {
        private RunnerSession trackedSession;
        private float previousElapsed;
        private readonly HashSet<int> warnings = new HashSet<int>();
        private readonly HashSet<int> launched = new HashSet<int>();
        private readonly HashSet<int> live = new HashSet<int>();
        private readonly List<BossSoundCue> cues = new List<BossSoundCue>();
        private readonly Predicate<int> expiredIdentity;
        public int TrackedCount { get { return warnings.Count + launched.Count; } }

        public RunMissileSoundTracker() { expiredIdentity = IsExpiredIdentity; }
        private bool IsExpiredIdentity(int id) { return !live.Contains(id); }

        public IReadOnlyList<BossSoundCue> Observe(RunnerSession session, bool silent)
        {
            cues.Clear();
            if (!ReferenceEquals(session, trackedSession) ||
                session != null && session.Elapsed < previousElapsed - .001f)
            {
                warnings.Clear(); launched.Clear();
                trackedSession = session;
            }
            previousElapsed = session == null ? 0f : session.Elapsed;
            if (session == null || session.IsBossArena || session.IsPractice || session.State == RunState.Menu)
            {
                warnings.Clear(); launched.Clear();
                return cues;
            }

            bool newWarning = false, newLaunch = false;
            float launchPan = .65f;
            live.Clear();
            for (int i = 0; i < session.RunMissileWarnings.Count; i++)
            {
                RunMissileWarning warning = session.RunMissileWarnings[i];
                live.Add(warning.VolleyId);
                if (warnings.Add(warning.VolleyId)) newWarning = true;
            }
            warnings.RemoveWhere(expiredIdentity);
            live.Clear();
            for (int i = 0; i < session.RunMissiles.Count; i++)
            {
                RunMissile missile = session.RunMissiles[i];
                if (missile.Removed) continue;
                live.Add(missile.VolleyId);
                if (!launched.Add(missile.VolleyId)) continue;
                newLaunch = true;
                launchPan = missile.VelocityX > 0f ? -.65f : .65f;
            }
            launched.RemoveWhere(expiredIdentity);
            if (!silent && session.State == RunState.Playing)
            {
                if (newWarning) cues.Add(new BossSoundCue(BossSoundKind.Windup, .65f));
                if (newLaunch) cues.Add(new BossSoundCue(BossSoundKind.Missile, launchPan));
            }
            return cues;
        }
    }

    public sealed partial class CityRunnerController
    {
        private BossSoundTracker bossSounds;
        private RunMissileSoundTracker runMissileSounds;
        private AudioSource bossLeft, bossCentre, bossRight, bossMovement, boardMovement;
        private AudioClip[] bossClips;
        private AudioClip tankLoop, robotLoop, rotorLoop, hoverLoop, underwaterLoop, breathingLoop, footstepSound;
        private BossEncounter audioEncounter;
        private double nextFootstep;

        private void InitializeBossAudio()
        {
            bossSounds = new BossSoundTracker();
            runMissileSounds = new RunMissileSoundTracker();
            bossLeft = NewBossSource(.17f, -.65f);
            bossCentre = NewBossSource(.2f, 0f);
            bossRight = NewBossSource(.17f, .65f);
            bossMovement = NewBossSource(.045f, .22f);
            boardMovement = NewBossSource(.025f, 0f);
            bossMovement.loop = boardMovement.loop = true;
            bossClips = new AudioClip[Enum.GetValues(typeof(BossSoundKind)).Length];
            for (int i = 0; i < bossClips.Length; i++)
                bossClips[i] = MakeBossAudio((BossSoundKind)i, i == (int)BossSoundKind.Defeat || i == (int)BossSoundKind.UnderwaterDefeat ? 1.65f :
                    i == (int)BossSoundKind.Sonar ? .65f : i == (int)BossSoundKind.Splash ? .42f :
                    i == (int)BossSoundKind.Charge ? .52f : i == (int)BossSoundKind.Impact ? .09f : .3f);
            tankLoop = MakeMechanismLoop("Tracks", 0);
            robotLoop = MakeMechanismLoop("Servo", 1);
            rotorLoop = MakeMechanismLoop("Rotor", 2);
            hoverLoop = MakeMechanismLoop("Hoverboard", 3);
            underwaterLoop = MakeWaterLoop("Submarine engine", false);
            breathingLoop = MakeWaterLoop("Diving bubbles", true);
            footstepSound = MakeTone("Footstep", 130f, 67f, .055f);
        }

        private AudioSource NewBossSource(float volume, float pan)
        {
            AudioSource source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false; source.volume = volume; source.panStereo = pan;
            return source;
        }

        private void UpdateBossAudio()
        {
            if (bossSounds == null || session == null) return;
            bool active = session.State == RunState.Playing && !muted && !ModalOpen;
            IReadOnlyList<BossSoundCue> cues = bossSounds.Observe(session, !active);
            IReadOnlyList<BossSoundCue> runCues = runMissileSounds == null ? null : runMissileSounds.Observe(session, !active);
            if (!active)
            { StopBossAudio(session.State == RunState.Paused || muted); return; }
            if (!ReferenceEquals(audioEncounter, session.CurrentBoss))
            {
                StopBossAudio(false);
                audioEncounter = session.CurrentBoss;
            }
            for (int i = 0; i < cues.Count; i++)
            {
                BossSoundCue cue = cues[i];
                if (cue.Kind == BossSoundKind.Defeat || cue.Kind == BossSoundKind.UnderwaterDefeat)
                {
                    StopBossAudio(false);
                    bossCentre.PlayOneShot(bossClips[(int)cue.Kind]);
                    continue;
                }
                AudioSource target = cue.Pan < -.1f ? bossLeft : cue.Pan > .1f ? bossRight : bossCentre;
                target.PlayOneShot(bossClips[(int)cue.Kind]);
            }
            if (runCues != null)
                for (int i = 0; i < runCues.Count; i++)
                {
                    BossSoundCue cue = runCues[i];
                    AudioSource target = cue.Pan < -.1f ? bossLeft : cue.Pan > .1f ? bossRight : bossCentre;
                    target.PlayOneShot(bossClips[(int)cue.Kind]);
                }
            bool fight = session.IsBossArena && session.ArenaPhase != BossPhase.Victory;
            AudioClip movement = fight && session.CurrentBoss != null ? session.CurrentBoss.Kind == BossKind.Tank ? tankLoop
                : session.CurrentBoss.Kind == BossKind.Sentinel ? robotLoop
                : session.CurrentBoss.Kind == BossKind.Submarine ? underwaterLoop : rotorLoop : null;
            if (session.CurrentBoss != null)
                bossMovement.volume = session.CurrentBoss.Action == BossAction.Strike ? .055f : .035f;
            SetBossLoop(bossMovement, movement);
            bool flying = session.IsFlyingBoss && session.FlightDeviceCollected;
            bool submerged = session.IsUnderwaterBoss && session.DivingHelmetEquipped && session.WaterDepth > .02f;
            boardMovement.volume = submerged ? .024f : .021f + Mathf.Clamp01(session.VelocityY / 230f) * .018f;
            SetBossLoop(boardMovement, submerged ? breathingLoop : flying ? hoverLoop : null);
            // Quiet footsteps follow simulated progress, never ambient time.
            double clock = session.IsBossArena ? session.BossElapsed : session.Elapsed;
            if ((!session.IsFlyingBoss || !session.FlightDeviceCollected) &&
                (!session.IsUnderwaterBoss || session.WaterDepth <= .001f) && session.PlayerY >= -.5f &&
                session.PlayerY <= 1f && !session.IsDucking && clock >= nextFootstep &&
                (!session.IsBossArena || session.ArenaPhase == BossPhase.Intro && session.TransitionStage != BossTransitionStage.Arrive ||
                 session.ArenaPhase == BossPhase.Fighting && Math.Abs(session.HorizontalVelocity) > 20f && !session.IsDashing ||
                 session.ArenaPhase == BossPhase.Victory && session.ExitTravelProgress > 0f))
            {
                nextFootstep = clock + .22d;
                bossCentre.PlayOneShot(footstepSound);
            }
            if (clock + 1d < nextFootstep) nextFootstep = clock + .22d;
        }

        private static void SetBossLoop(AudioSource source, AudioClip clip)
        {
            if (source == null) return;
            if (source.clip != clip) { source.Stop(); source.clip = clip; }
            if (clip != null && !source.isPlaying) source.Play();
            else if (clip == null && source.isPlaying) source.Stop();
        }

        private void StopBossAudio(bool includeInterface)
        {
            if (bossLeft != null) bossLeft.Stop();
            if (bossCentre != null) bossCentre.Stop();
            if (bossRight != null) bossRight.Stop();
            if (bossMovement != null) bossMovement.Stop();
            if (boardMovement != null) boardMovement.Stop();
            if (includeInterface && audioSource != null) audioSource.Stop();
        }

        private void DisposeBossAudio()
        {
            StopBossAudio(true);
            if (bossClips != null) for (int i = 0; i < bossClips.Length; i++) if (bossClips[i] != null) Destroy(bossClips[i]);
            if (tankLoop != null) Destroy(tankLoop);
            if (robotLoop != null) Destroy(robotLoop);
            if (rotorLoop != null) Destroy(rotorLoop);
            if (hoverLoop != null) Destroy(hoverLoop);
            if (underwaterLoop != null) Destroy(underwaterLoop);
            if (breathingLoop != null) Destroy(breathingLoop);
            if (footstepSound != null) Destroy(footstepSound);
            if (bossLeft != null) Destroy(bossLeft);
            if (bossCentre != null) Destroy(bossCentre);
            if (bossRight != null) Destroy(bossRight);
            if (bossMovement != null) Destroy(bossMovement);
            if (boardMovement != null) Destroy(boardMovement);
            bossLeft = bossCentre = bossRight = bossMovement = boardMovement = null;
            tankLoop = robotLoop = rotorLoop = hoverLoop = underwaterLoop = breathingLoop = footstepSound = null;
            bossClips = null;
            bossSounds = null;
            runMissileSounds = null;
        }

        private static AudioClip MakeBossAudio(BossSoundKind kind, float seconds)
        {
            const int rate = 22050;
            var data = new float[Mathf.CeilToInt(seconds * rate)];
            uint seed = 317u + (uint)kind * 97u;
            double phase = 0d;
            float filtered = 0f;
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate, u = (float)i / data.Length;
                seed = seed * 1664525u + 1013904223u;
                float noise = (seed >> 8) / 8388608f - 1f;
                filtered += (noise - filtered) * .16f;
                float envelope = Mathf.Min(1f, t * 60f) * (1f - u) * (1f - u);
                float frequency = kind == BossSoundKind.Windup ? 150f + u * 720f
                    : kind == BossSoundKind.Laser ? 1200f - u * 900f
                    : kind == BossSoundKind.Electric ? 130f + 85f * Mathf.Sin(t * 95f)
                    : kind == BossSoundKind.Impact ? 940f - u * 430f
                    : kind == BossSoundKind.Charge ? 260f - u * 190f : 110f - u * 75f;
                phase += 2d * Math.PI * frequency / rate;
                float sine = (float)Math.Sin(phase);
                float sample;
                if (kind == BossSoundKind.Defeat || kind == BossSoundKind.UnderwaterDefeat)
                {
                    float burst = 0f;
                    foreach (float start in DefeatBursts)
                    { float age = t - start; if (age >= 0f) burst += (float)Math.Exp(-age * 9f); }
                    sample = (kind == BossSoundKind.UnderwaterDefeat ? .9f * filtered + .13f * sine : .7f * filtered + .2f * sine)
                        * Math.Min(1.5f, burst) * (1f - u);
                }
                else if (kind == BossSoundKind.Sonar)
                    sample = (float)Math.Sin(2d * Math.PI * (720f - u * 90f) * t) * envelope * .4f;
                else if (kind == BossSoundKind.Splash)
                    sample = (filtered * .65f + (float)Math.Sin(2d * Math.PI * (230f - u * 160f) * t) * .22f)
                        * envelope * (.65f + .35f * Mathf.Sin(t * 73f));
                else if (kind == BossSoundKind.Swarm)
                    sample = ((float)Math.Sin(2d * Math.PI * (280f + Mathf.Sin(t * 28f) * 75f) * t) * .25f + filtered * .4f) * envelope;
                else if (kind == BossSoundKind.Torpedo)
                    sample = (filtered * .8f + (float)Math.Sin(2d * Math.PI * (100f + u * 65f) * t) * .15f) * envelope;
                else if (kind == BossSoundKind.Electric)
                    sample = (.45f * sine * Mathf.Sin(t * 340f) + .2f * noise) * envelope;
                else if (kind == BossSoundKind.Missile || kind == BossSoundKind.Charge)
                    sample = (filtered * .85f + sine * .15f) * envelope;
                else if (kind == BossSoundKind.Cannon || kind == BossSoundKind.Blast)
                    sample = (filtered * .7f + sine * .38f) * envelope;
                else sample = sine * envelope * .44f;
                data[i] = Math.Max(-.8f, Math.Min(.8f, sample));
            }
            AudioClip clip = AudioClip.Create("CityDino Boss " + kind, data.Length, 1, rate, false);
            clip.SetData(data, 0); return clip;
        }

        private static readonly float[] DefeatBursts = { 0f, .23f, .56f, .92f };

        private static AudioClip MakeWaterLoop(string name, bool breathing)
        {
            const int rate = 22050;
            var data = new float[rate * 2];
            for (int i = 0; i < data.Length; i++)
            {
                double t = (double)i / rate;
                float slow = .5f + .5f * (float)Math.Sin(t * Math.PI * (breathing ? 1d : 3d));
                float low = (float)Math.Sin(t * Math.PI * (breathing ? 220d : 112d));
                float bubble = (float)Math.Sin(t * Math.PI * 340d + Math.Sin(t * Math.PI * 8d) * 3d);
                data[i] = breathing ? (.12f * low + .055f * bubble) * slow * slow
                    : (.19f * low + .045f * bubble) * (.65f + .35f * slow);
            }
            AudioClip clip = AudioClip.Create("CityDino " + name, data.Length, 1, rate, false);
            clip.SetData(data, 0); return clip;
        }

        private static AudioClip MakeMechanismLoop(string name, int mechanism)
        {
            const int rate = 22050;
            var data = new float[rate];
            for (int i = 0; i < data.Length; i++)
            {
                float t = (float)i / rate;
                float pulse = .5f + .5f * Mathf.Sin(t * (mechanism == 2 ? 18f : mechanism == 0 ? 12f : 7f) * (float)(2d * Math.PI));
                float low = Mathf.Sin(t * (mechanism == 0 ? 65f : mechanism == 1 ? 170f : mechanism == 2 ? 72f : 210f) * (float)(2d * Math.PI));
                float high = Mathf.Sin(t * (mechanism == 0 ? 183f : mechanism == 1 ? 440f : mechanism == 2 ? 216f : 420f) * (float)(2d * Math.PI));
                data[i] = (low * .23f + high * .08f) * (.3f + pulse * .7f);
            }
            AudioClip clip = AudioClip.Create("CityDino " + name, data.Length, 1, rate, false);
            clip.SetData(data, 0); return clip;
        }
    }
}
