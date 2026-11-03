using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Mods;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// Watch autoplay play one of each note type: a tap, a hold, a drag chain, a short slider that becomes a tap, a spinner that becomes a centre hold,
    /// a drag chain following a slider too fast for it to keep up with, and zigzags from short sliders.
    /// </summary>
    public partial class TestSceneBreveNoteTypes : PlayerTestScene
    {
        protected override Ruleset CreatePlayerRuleset() => new BreveRuleset();

        protected override bool Autoplay => true;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => NoteTypesBeatmap.Create(ruleset);

        [Test]
        public void TestAutoplayHitsEveryNoteTypePerfectly()
        {
            AddUntilStep("wait for all judgements", () => Player.ScoreProcessor.HasCompleted.Value);
            AddAssert("all perfect", () => Player.ScoreProcessor.Statistics.GetValueOrDefault(HitResult.Perfect) == Player.ScoreProcessor.JudgedHits);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestAutoplayHitsFlicksPerfectly(bool expert)
        {
            AddStep("load with flicks", () => LoadPlayer(expert ? new Mod[] { new BreveModFlicks(), new BreveModExpert() } : new Mod[] { new BreveModFlicks() }));
            AddUntilStep("wait for player", () => Player.IsLoaded);
            AddAssert("has flicks", () => Player.DrawableRuleset.Objects.OfType<Flick>().Any());
            AddUntilStep("wait for all judgements", () => Player.ScoreProcessor.HasCompleted.Value);
            AddAssert("all perfect", () => Player.ScoreProcessor.Statistics.GetValueOrDefault(HitResult.Perfect) == Player.ScoreProcessor.JudgedHits);
        }

        [Test]
        public void TestAutoplayHitsEveryNoteTypePerfectlyInTheExpertChart()
        {
            AddStep("load the expert chart", () => LoadPlayer(new Mod[] { new BreveModExpert() }));
            AddUntilStep("wait for player", () => Player.IsLoaded);
            AddUntilStep("wait for all judgements", () => Player.ScoreProcessor.HasCompleted.Value);
            AddAssert("all perfect", () => Player.ScoreProcessor.Statistics.GetValueOrDefault(HitResult.Perfect) == Player.ScoreProcessor.JudgedHits);
        }
    }
}
