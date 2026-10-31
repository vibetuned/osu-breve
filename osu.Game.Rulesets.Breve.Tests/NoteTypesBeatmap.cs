using osu.Game.Beatmaps;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// A small osu! beatmap at 120 BPM, with one of each kind of object that osu!breve converts differently.
    /// </summary>
    public static class NoteTypesBeatmap
    {
        public const double BEAT_LENGTH = 500;

        /// <summary>
        /// A circle, which becomes a tap.
        /// </summary>
        public const double CIRCLE_TIME = 1000;

        /// <summary>
        /// A 4 beat slider from x = 64 to x = 464, which becomes a hold.
        /// </summary>
        public const double SLIDER_TIME = 2000;

        /// <summary>
        /// The same slider coming back once, which becomes a drag chain.
        /// </summary>
        public const double REVERSE_SLIDER_TIME = 5000;

        /// <summary>
        /// A slider lasting a fifth of a beat, which becomes a tap.
        /// </summary>
        public const double SHORT_SLIDER_TIME = 10000;

        /// <summary>
        /// A 2 beat spinner, which becomes a hold in the centre lane.
        /// </summary>
        public const double SPINNER_TIME = 11000;

        /// <summary>
        /// A slider sweeping across the playfield every beat and coming back twice, faster than a drag chain can follow.
        /// </summary>
        public const double FAST_REVERSE_SLIDER_TIME = 13000;

        /// <summary>
        /// A slider coming back after a quarter beat, too soon for a diagonal step, which becomes a straight drag chain.
        /// </summary>
        public const double QUICK_REVERSE_SLIDER_TIME = 15000;

        /// <summary>
        /// A half beat slider in lane 1, which becomes a zigzag to the mirrored lane, 7, in the expert chart, and a tap in the normal one.
        /// </summary>
        public const double ZIGZAG_TIME = 17500;

        /// <summary>
        /// A half beat slider in the centre lane on a downward sweep, which becomes a zigzag to the left in the expert chart.
        /// </summary>
        public const double CENTRE_ZIGZAG_TIME = 19500;

        /// <summary>
        /// A half beat slider in lane 3, whose mirrored lane is too close, so it becomes a zigzag four lanes over, to lane 7, in the expert chart.
        /// </summary>
        public const double NEAR_CENTRE_ZIGZAG_TIME = 21500;

        /// <summary>
        /// A 1 beat slider in lane 1, which becomes a zigzag to lane 7 in the normal chart, and a hold in the expert chart.
        /// </summary>
        public const double BEAT_SLIDER_TIME = 23500;

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
13000,-25,4,1,0,100,0,0
16000,-100,4,1,0,100,0,0

[HitObjects]
64,192,1000,1,0,0:0:0:0:
64,192,2000,2,0,L|464:192,1,400
64,192,5000,2,0,L|464:192,2,400
256,192,10000,2,0,L|276:192,1,20
256,192,11000,12,0,12000,0:0:0:0:
64,192,13000,2,0,L|464:192,3,400
64,192,15000,2,0,L|164:192,2,100
64,192,17500,2,0,L|114:192,1,50
256,192,19500,2,0,L|306:192,1,50
192,192,21500,2,0,L|242:192,1,50
64,192,23500,2,0,L|164:192,1,100
";

        public static Beatmap Create(RulesetInfo ruleset) => LegacyTestBeatmap.Decode(beatmap, ruleset);
    }
}
