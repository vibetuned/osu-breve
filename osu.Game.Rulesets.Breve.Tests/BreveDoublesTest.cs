using System.Linq;
using NUnit.Framework;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Mods;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Osu;

namespace osu.Game.Rulesets.Breve.Tests
{
    [TestFixture]
    public class BreveDoublesTest
    {
        // Lanes count from 0 here, so the centre lane is 4.
        [TestCase(DoublesBeatmap.DOWNBEAT_TIME, 1, 7)]
        [TestCase(DoublesBeatmap.BEAT_TIME, 7, 4)]
        [TestCase(DoublesBeatmap.OFF_BEAT_TIME, 1, 3)]
        [TestCase(DoublesBeatmap.CENTRE_TIME, 4, 7)]
        [TestCase(DoublesBeatmap.BLOCKED_TIME, 1, 4)]
        public void TestAccentedTapsBecomeDoubles(double time, int lane, int partnerLane)
        {
            var notes = notesAt(time);

            Assert.That(notes.Select(n => n.Lane), Is.EquivalentTo(new[] { lane, partnerLane }));
            Assert.That(notes, Is.All.TypeOf<Tap>());
        }

        [Test]
        public void TestAccentedHoldsBecomeDoubleHolds()
        {
            var notes = notesAt(DoublesBeatmap.HOLD_TIME);

            Assert.That(notes.Select(n => n.Lane), Is.EquivalentTo(new[] { 2, 6 }));
            Assert.That(notes, Is.All.TypeOf<Hold>());
            Assert.That(notes.Cast<Hold>().Select(h => h.Duration).Distinct(), Has.Exactly(1).Items);
        }

        [TestCase(DoublesBeatmap.UNACCENTED_TIME)]
        [TestCase(DoublesBeatmap.LONG_HOLD_TIME)]
        public void TestUnaccentedNotesStaySingle(double time)
        {
            Assert.That(notesAt(time), Has.Exactly(1).Items);
        }

        [Test]
        public void TestOnlyOneNoteOfADoublePlaysTheHitsound()
        {
            foreach (double time in new[] { DoublesBeatmap.DOWNBEAT_TIME, DoublesBeatmap.HOLD_TIME })
            {
                var notes = notesAt(time);
                Assert.That(notes.Count(n => (n is Hold hold ? hold.PressSamples : n.Samples).Any()), Is.EqualTo(1));
            }
        }

        // A flick's lane is the left one of the two it sits between, so flicks are in lanes 0 to 7, and mirror around the middle of the centre lane.
        [TestCase(2000, 1, 6)]
        [TestCase(4500, 3, 1)]
        [TestCase(6250, 1, 3)]
        public void TestAccentedFlicksBecomeDoubleFlicks(double time, int lane, int partnerLane)
        {
            var notes = flicksAt(time);

            Assert.That(notes.Select(n => n.Lane), Is.EquivalentTo(new[] { lane, partnerLane }));
            Assert.That(notes, Is.All.TypeOf<Flick>());
            Assert.That(notes.Count(n => n.Samples.Any()), Is.EqualTo(1), "only one flick plays the hitsound");
        }

        [Test]
        public void TestUnaccentedFlicksStaySingle()
        {
            Assert.That(flicksAt(8000), Has.Exactly(1).Items);
        }

        /// <summary>
        /// Converts, with flicks, an osu! beatmap at 120 BPM in 4/4 with a half beat slider moving right in each of the places that decide how double flicks are paired:
        /// accented on a downbeat (from lane 1), on a beat next to the centre lane (from lane 3, whose mirror would share the centre lane), off the beat (from lane 1),
        /// and unaccented.
        /// </summary>
        private static BreveHitObject[] flicksAt(double time)
        {
            const string beatmap = @"osu file format v14

[Difficulty]
CircleSize:4
SliderMultiplier:1

[TimingPoints]
0,500,4,1,0,100,1,0

[HitObjects]
88,192,2000,2,4,L|138:192,1,50
200,192,4500,2,4,L|250:192,1,50
88,192,6250,2,4,L|138:192,1,50
88,192,8000,2,0,L|138:192,1,50
";

            var converter = new BreveRuleset().CreateBeatmapConverter(LegacyTestBeatmap.Decode(beatmap, new OsuRuleset().RulesetInfo));
            new BreveModFlicks().ApplyToBeatmapConverter(converter);

            var converted = (Beatmap<BreveHitObject>)converter.Convert();
            return converted.HitObjects.Where(h => Precision.AlmostEquals(h.StartTime, time)).ToArray();
        }

        private static BreveHitObject[] notesAt(double time)
        {
            var converted = (Beatmap<BreveHitObject>)new BreveRuleset().CreateBeatmapConverter(DoublesBeatmap.Create(new OsuRuleset().RulesetInfo)).Convert();
            return converted.HitObjects.Where(h => Precision.AlmostEquals(h.StartTime, time)).ToArray();
        }
    }
}
