using System;

namespace CityDino
{
    /// <summary>Each complete trio advances combat tier independently of the in-fight rage phase.</summary>
    public sealed partial class RunnerSession
    {
        public int BossTier
        {
            get { return IsPractice ? Math.Max(1, practiceBossTier) : 1 + (Math.Max(1, BossNumber) - 1) / 3; }
        }
        public float BossHealthScale { get { return 1f + .18f * (BossTier - 1); } }
        public float BossTierPace { get { return 1f + Math.Min(.65f, .11f * (float)Math.Log(BossTier, 2d)); } }
        public float BossTierTempo { get { return 1f + Math.Min(.35f, .07f * (float)Math.Log(BossTier, 2d)); } }
        public int BossRepeatCount { get { return Math.Min(4, BossTier); } }
        public int BossExtraWaves { get { return Math.Min(3, BossTier - 1); } }
        public float BossAirGapHeight { get { return Math.Max(174f, 190f - BossExtraWaves * 5f); } }

        // Each cue keeps its complete reaction time; tempo shortens the quiet
        // spacing between separate casts, never the visible warning itself.
        private float BossCastSpacing(float seconds) { return seconds / BossTierTempo; }
        private void ExtendBossCycle(float lastDelay, float warning, float clearance)
        {
            bossActionCycle = Math.Max(bossActionCycle,
                lastDelay / BossPracticePace + warning + clearance / BossPracticePace);
        }
    }
}
