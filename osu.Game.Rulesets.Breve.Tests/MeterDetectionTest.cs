using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Beatmaps;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Breve.Tests
{
    [TestFixture]
    public class MeterDetectionTest
    {
        [Test]
        public void TestDupleTimeStaysInFour()
        {
            // Accents on every 4th beat, and finishes every 8th, like a 4/4 song.
            Assert.That(convertedBeatsPerMeasure(circleEveryBeat(newComboEvery: 4, finishEvery: 8)), Is.EqualTo(4));
        }

        [Test]
        public void TestTripleTimeLeftAt4Over4BecomesThree()
        {
            // Accents on every 3rd beat, and finishes every 6th, like a 3/4 waltz mapped without changing the time signature.
            Assert.That(convertedBeatsPerMeasure(circleEveryBeat(newComboEvery: 3, finishEvery: 6)), Is.EqualTo(3));
        }

        [Test]
        public void TestCompoundTimeLeftAt4Over4BecomesThree()
        {
            // A 6/8 song timed in eighth notes: accents every 6 beats, and finishes every 12.
            Assert.That(convertedBeatsPerMeasure(circleEveryBeat(newComboEvery: 6, finishEvery: 12)), Is.EqualTo(3));
        }

        [Test]
        public void TestNoAccentsStaysInFour()
        {
            Assert.That(convertedBeatsPerMeasure(circleEveryBeat(newComboEvery: 0, finishEvery: 0)), Is.EqualTo(4));
        }

        [Test]
        public void TestTimeSignatureSetByTheMapperIsKept()
        {
            // The mapper chose 7/4, even though the accents are in threes.
            Assert.That(convertedBeatsPerMeasure(circleEveryBeat(newComboEvery: 3, finishEvery: 6, beatsPerMeasure: 7)), Is.EqualTo(7));
        }

        [Test]
        public void TestOsuTestBeatmapStaysInFour()
        {
            Assert.That(convertedBeatsPerMeasure(new TestBeatmap(new OsuRuleset().RulesetInfo)), Is.EqualTo(4));
        }

        [Test]
        public void TestPagesFollowTheDetectedMeter()
        {
            var converted = convert(circleEveryBeat(newComboEvery: 3, finishEvery: 6));
            var pageTiming = new PageTiming(converted.ControlPointInfo);

            // At 120 BPM, a measure of three beats lasts 1500ms, which is the closest musical length to the target.
            Assert.That(pageTiming.PageAt(5000).Duration, Is.EqualTo(1500));
        }

        [Test]
        public void TestSourceBeatmapIsNotChanged()
        {
            var source = circleEveryBeat(newComboEvery: 3, finishEvery: 6);
            convert(source);

            Assert.That(source.ControlPointInfo.TimingPoints[0].TimeSignature.Numerator, Is.EqualTo(4));
        }

        private static int convertedBeatsPerMeasure(IBeatmap beatmap) => convert(beatmap).ControlPointInfo.TimingPoints[0].TimeSignature.Numerator;

        private static Beatmap<BreveHitObject> convert(IBeatmap beatmap) => (Beatmap<BreveHitObject>)new BreveRuleset().CreateBeatmapConverter(beatmap).Convert();

        /// <summary>
        /// Creates an osu! beatmap at 120 BPM with a circle on each of 96 beats, starting new combos and playing finishes every so many beats.
        /// </summary>
        private static IBeatmap circleEveryBeat(int newComboEvery, int finishEvery, int beatsPerMeasure = 4)
        {
            var beatmap = new StringBuilder($@"osu file format v14

[Difficulty]
HPDrainRate:5
CircleSize:4
OverallDifficulty:5
ApproachRate:5
SliderMultiplier:1
SliderTickRate:1

[TimingPoints]
0,500,{beatsPerMeasure},1,0,100,1,0

[HitObjects]
");

            foreach (int beat in Enumerable.Range(0, 96))
            {
                int type = newComboEvery > 0 && beat % newComboEvery == 0 ? 5 : 1;
                int hitsound = finishEvery > 0 && beat % finishEvery == 0 ? 4 : 0;

                beatmap.AppendLine($"{(beat % 2 == 0 ? 128 : 384)},192,{beat * 500},{type},{hitsound},0:0:0:0:");
            }

            return LegacyTestBeatmap.Decode(beatmap.ToString(), new OsuRuleset().RulesetInfo);
        }
    }
}
