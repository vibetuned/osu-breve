using System.IO;
using System.Text;
using osu.Game.Beatmaps;
using osu.Game.Beatmaps.Formats;
using osu.Game.IO;

namespace osu.Game.Rulesets.Breve.Tests
{
    public static class LegacyTestBeatmap
    {
        /// <summary>
        /// Decodes a beatmap written in the .osu format.
        /// </summary>
        /// <remarks>
        /// The beatmap mustn't have a "Mode" line, as decoding it needs a ruleset store. Beatmaps without one are osu! beatmaps.
        /// </remarks>
        public static Beatmap Decode(string beatmap, RulesetInfo ruleset)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(beatmap));
            using var reader = new LineBufferedReader(stream);

            var decoded = new LegacyBeatmapDecoder().Decode(reader);
            decoded.BeatmapInfo.Ruleset = ruleset;

            return decoded;
        }
    }
}
