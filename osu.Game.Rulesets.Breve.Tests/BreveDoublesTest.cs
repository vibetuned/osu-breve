using System.Linq;
using NUnit.Framework;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
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

        private static BreveHitObject[] notesAt(double time)
        {
            var converted = (Beatmap<BreveHitObject>)new BreveRuleset().CreateBeatmapConverter(DoublesBeatmap.Create(new OsuRuleset().RulesetInfo)).Convert();
            return converted.HitObjects.Where(h => Precision.AlmostEquals(h.StartTime, time)).ToArray();
        }
    }
}
