using System;

namespace CityDino
{
    /// <summary>Scored runs begin at tier three; complete groups of four raise that tier independently of the in-fight rage phase.</summary>
    public sealed partial class RunnerSession
    {
        public int BossTier
        {
            get { return IsPractice ? Math.Max(1, practiceBossTier) : 3 + (Math.Max(1, BossNumber) - 1) / 4; }
        }
        public float BossHealthScale { get { return 1f + .18f * (BossTier - 1); } }
        public float BossTierPace { get { return 1f + Math.Min(.85f, .16f * (float)Math.Log(BossTier, 2d)); } }
        public float BossTierTempo { get { return 1f + Math.Min(.45f, .10f * (float)Math.Log(BossTier, 2d)); } }
        // More volleys remain meaningful after the initial C3/C4 rounds. C6
        // gains a fifth cast and C8 a sixth without an unbounded object wall.
        public int BossRepeatCount { get { return BossTier <= 4 ? BossTier : Math.Min(6, 4 + (BossTier - 4) / 2); } }
        public int BossExtraWaves { get { return BossRepeatCount - 1; } }
        // Density increases through additional casts, not by closing the
        // opening below the already established movement margin.
        public float BossAirGapHeight { get { return 190f - Math.Min(3, BossExtraWaves) * 5f; } }

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
