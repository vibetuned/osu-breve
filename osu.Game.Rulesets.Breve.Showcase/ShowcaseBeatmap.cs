using System.IO;
using System.Text;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;

namespace osu.Game.Rulesets.Breve.Showcase
{
    /// <summary>
    /// A short osu! beatmap at 150 BPM, so one measure (1600ms) per sweep, with every kind of note osu!breve's normal chart has:
    /// taps, holds, a long hold across two sweeps, a spinner's centre hold, zigzags, diagonal and straight drag chains, doubles and a double hold.
    /// </summary>
    public static class ShowcaseBeatmap
    {
        /// <summary>
        /// When the capture ends: a little after the last note, once it has been hit.
        /// </summary>
        public const double CAPTURE_END_TIME = 20800;

        private const string beatmap = @"osu file format v14

[Difficulty]
HPDrainRate:5
CircleSize:4
OverallDifficulty:5
ApproachRate:5
SliderMultiplier:1
SliderTickRate:1

[TimingPoints]
0,400,4,1,0,100,1,0

[HitObjects]
88,192,1600,5,0,0:0:0:0:
200,192,2000,1,0,0:0:0:0:
312,192,2400,1,0,0:0:0:0:
424,192,2800,1,0,0:0:0:0:
256,192,3200,5,4,0:0:0:0:
144,192,3600,1,0,0:0:0:0:
200,192,3800,1,0,0:0:0:0:
368,192,4000,2,0,L|368:392,1,200
144,192,4400,1,0,0:0:0:0:
88,192,4800,6,0,L|188:192,1,100
424,192,5600,2,0,L|324:192,1,100
88,192,6400,6,0,L|288:192,2,200
256,192,8000,5,4,0:0:0:0:
144,192,8400,1,4,0:0:0:0:
368,192,8800,1,4,0:0:0:0:
200,192,9200,1,0,0:0:0:0:
312,192,9400,1,0,0:0:0:0:
256,192,9600,6,0,L|256:392|256:0|256:100,1,600
88,192,10000,1,0,0:0:0:0:
424,192,10400,1,0,0:0:0:0:
144,192,10800,1,0,0:0:0:0:
368,192,11200,5,0,0:0:0:0:
88,192,11600,1,0,0:0:0:0:
424,192,12400,1,0,0:0:0:0:
256,192,12800,12,0,13600,0:0:0:0:
88,192,13600,5,4,0:0:0:0:
424,192,14000,1,0,0:0:0:0:
400,192,14400,6,0,L|450:192,4,50
144,192,15200,1,0,0:0:0:0:
200,192,15600,1,0,0:0:0:0:
32,192,16000,5,0,0:0:0:0:
88,192,16200,1,0,0:0:0:0:
144,192,16400,1,0,0:0:0:0:
200,192,16600,1,0,0:0:0:0:
256,192,16800,1,0,0:0:0:0:
312,192,17000,1,0,0:0:0:0:
368,192,17200,1,0,0:0:0:0:
424,192,17400,1,0,0:0:0:0:
480,192,17600,5,4,0:0:0:0:
144,192,18000,2,0,L|244:192,1,100
256,192,18800,1,4,0:0:0:0:
144,192,19200,6,4,L|144:392,1,200
";

        public static Beatmap Create(RulesetInfo ruleset)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(beatmap));
            using var reader = new LineBufferedReader(stream);

            var decoded = new LegacyBeatmapDecoder().Decode(reader);
            decoded.BeatmapInfo.Ruleset = ruleset;

            return decoded;
        }
    }
}
