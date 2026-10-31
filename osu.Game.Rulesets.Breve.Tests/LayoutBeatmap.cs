using osu.Game.Beatmaps;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// A small osu! beatmap at 120 BPM, with notes that would clash in the lanes they convert to.
    /// Sweeps last a measure (2000ms), and the first one starts at 0ms going upwards.
    /// </summary>
    public static class LayoutBeatmap
    {
        /// <summary>
        /// A circle in lane 1, just before the scan line turns around at the bottom.
        /// </summary>
        public const double BEFORE_TURNAROUND_TIME = 3900;

        /// <summary>
        /// A circle in lane 1 just after the scan line turns around, at the same height as the one before.
        /// Taps are wider than lanes, so it moves two lanes over rather than one, to the right as its sweep goes upwards.
        /// </summary>
        public const double AFTER_TURNAROUND_TIME = 4100;

        /// <summary>
        /// Three stacked circles in the centre lane, a quarter beat apart, which spread out two lanes apart.
        /// </summary>
        public const double STACK_TIME = 5000;

        /// <summary>
        /// A 4 beat slider in lane 7.
        /// </summary>
        public const double HOLD_TIME = 7500;

        /// <summary>
        /// A circle in lane 7 while the hold is held, at the same height as the hold's head.
        /// It moves two lanes left, as lane 8 would overlap the head.
        /// </summary>
        public const double DURING_HOLD_TIME = 8500;

        private const string beatmap = @"osu file format v14

[Difficulty]
HPDrainRate:5
CircleSize:4
OverallDifficulty:5
ApproachRate:5
SliderMultiplier:1
SliderTickRate:1

[TimingPoints]
0,500,4,1,0,100,1,0

[HitObjects]
64,192,3900,1,0,0:0:0:0:
64,192,4100,1,0,0:0:0:0:
256,192,5000,1,0,0:0:0:0:
256,192,5125,1,0,0:0:0:0:
256,192,5250,1,0,0:0:0:0:
448,192,7500,2,0,L|448:592,1,400
448,192,8500,1,0,0:0:0:0:
";

        public static Beatmap Create(RulesetInfo ruleset) => LegacyTestBeatmap.Decode(beatmap, ruleset);
    }
}
