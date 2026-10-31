using osu.Game.Beatmaps;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// A small osu! beatmap at 120 BPM in 4/4, with accented (finish) notes in each of the places that decide how doubles are paired.
    /// Measures start every 2000ms.
    /// </summary>
    public static class DoublesBeatmap
    {
        /// <summary>
        /// An accented circle in lane 1 on a downbeat, which gets a partner mirrored around the centre.
        /// </summary>
        public const double DOWNBEAT_TIME = 2000;

        /// <summary>
        /// An accented circle in lane 7 on the second beat of a measure, which gets a partner in the centre lane.
        /// </summary>
        public const double BEAT_TIME = 2500;

        /// <summary>
        /// An accented circle in lane 1 off the beat, which gets a partner on the same side.
        /// </summary>
        public const double OFF_BEAT_TIME = 3250;

        /// <summary>
        /// An accented circle in the centre lane on a downbeat of an upward sweep, which gets a partner three lanes to the right.
        /// </summary>
        public const double CENTRE_TIME = 4000;

        /// <summary>
        /// A circle in lane 7 without an accent, which stays single.
        /// </summary>
        public const double UNACCENTED_TIME = 5000;

        /// <summary>
        /// A 2 beat slider in lane 2 with an accented head on a downbeat, which becomes a double hold.
        /// </summary>
        public const double HOLD_TIME = 6000;

        /// <summary>
        /// A 4 beat slider in lane 7, without an accent.
        /// </summary>
        public const double LONG_HOLD_TIME = 7500;

        /// <summary>
        /// An accented circle in lane 1 on a downbeat during the long hold, whose mirrored partner would be in the hold's lane.
        /// It gets a partner in the centre lane instead.
        /// </summary>
        public const double BLOCKED_TIME = 8000;

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
64,192,2000,1,4,0:0:0:0:
448,192,2500,1,4,0:0:0:0:
64,192,3250,1,4,0:0:0:0:
256,192,4000,1,4,0:0:0:0:
448,192,5000,1,0,0:0:0:0:
128,192,6000,2,0,L|128:392,1,200,4|0,0:0|0:0,0:0:0:0:
448,192,7500,2,0,L|448:592,1,400
64,192,8000,1,4,0:0:0:0:
";

        public static Beatmap Create(RulesetInfo ruleset) => LegacyTestBeatmap.Decode(beatmap, ruleset);
    }
}
