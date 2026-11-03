using System.Linq;
using System.Text;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Difficulty;
using osu.Game.Rulesets.Breve.Mods;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Osu;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Breve.Tests
{
    [TestFixture]
    public class BreveDifficultyCalculatorTest
    {
        [Test]
        public void TestExpertStarRatingMatchesOsuOnItsTestBeatmap()
        {
            var beatmap = new TestBeatmap(new OsuRuleset().RulesetInfo);

            double osuStars = new OsuRuleset().CreateDifficultyCalculator(new TestWorkingBeatmap(beatmap)).Calculate().StarRating;
            double breveStars = calculate(beatmap, new BreveModExpert()).StarRating;

            Assert.That(breveStars, Is.EqualTo(osuStars).Within(5).Percent,
                "The difficulty calculator's multipliers are calibrated against osu! on the expert chart of its test beatmap, and need recalibrating after changes to conversion or difficulty");
        }

        [Test]
        public void TestFlicksAreHarder()
        {
            var beatmap = NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo);

            Assert.That(calculate(beatmap, new BreveModFlicks()).StarRating, Is.GreaterThan(calculate(beatmap).StarRating));
        }

        [Test]
        public void TestExpertIsHarderThanNormal()
        {
            var beatmap = new TestBeatmap(new OsuRuleset().RulesetInfo);

            Assert.That(calculate(beatmap, new BreveModExpert()).StarRating, Is.GreaterThan(calculate(beatmap).StarRating));
        }

        [Test]
        public void TestEmptyBeatmapHasNoDifficulty()
        {
            Assert.That(calculate(new Beatmap()).StarRating, Is.Zero);
        }

        [Test]
        public void TestRateAdjustmentsChangeDifficulty()
        {
            var beatmap = new TestBeatmap(new OsuRuleset().RulesetInfo);

            double normal = calculate(beatmap).StarRating;
            double doubleTime = calculate(beatmap, new BreveModDoubleTime()).StarRating;
            double halfTime = calculate(beatmap, new BreveModHalfTime()).StarRating;

            Assert.That(doubleTime, Is.GreaterThan(normal));
            Assert.That(halfTime, Is.LessThan(normal));
        }

        [Test]
        public void TestDenserNotesAreHarderToKeepUpWith()
        {
            var sparse = calculate(alternatingCircles(500, 192, 320));
            var dense = calculate(alternatingCircles(250, 192, 320));

            Assert.That(dense.TappingDifficulty, Is.GreaterThan(sparse.TappingDifficulty));
            Assert.That(dense.StarRating, Is.GreaterThan(sparse.StarRating));
        }

        [Test]
        public void TestWiderJumpsAreHarderToReach()
        {
            // Lanes 3 and 5 (counting from 0), against lanes 0 and 8, at the same rhythm.
            var narrow = calculate(alternatingCircles(250, 192, 320));
            var wide = calculate(alternatingCircles(250, 0, 512));

            Assert.That(wide.MovementDifficulty, Is.GreaterThan(narrow.MovementDifficulty));
            Assert.That(wide.StarRating, Is.GreaterThan(narrow.StarRating));
        }

        private static BreveDifficultyAttributes calculate(IBeatmap beatmap, params Mod[] mods)
            => (BreveDifficultyAttributes)new BreveRuleset().CreateDifficultyCalculator(new TestWorkingBeatmap(beatmap)).Calculate(mods);

        /// <summary>
        /// Creates an osu! beatmap at 120 BPM with 64 circles alternating between two x positions.
        /// </summary>
        private static IBeatmap alternatingCircles(double interval, params float[] xs)
        {
            var beatmap = new StringBuilder(@"osu file format v14

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
");

            foreach (int i in Enumerable.Range(0, 64))
                beatmap.AppendLine($"{xs[i % xs.Length]},192,{1000 + i * interval},1,0,0:0:0:0:");

            return LegacyTestBeatmap.Decode(beatmap.ToString(), new OsuRuleset().RulesetInfo);
        }
    }
}
