using System.Collections.Generic;
using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Scoring;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// Watch autoplay play each way of pairing doubles: mirrored on a downbeat, with the centre lane on a beat, on the same side off the beat,
    /// from the centre lane, as a double hold, and moved to the centre lane when the mirrored lane is taken by a hold.
    /// </summary>
    public partial class TestSceneBreveDoubles : PlayerTestScene
    {
        protected override Ruleset CreatePlayerRuleset() => new BreveRuleset();

        protected override bool Autoplay => true;

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => DoublesBeatmap.Create(ruleset);

        [Test]
        public void TestAutoplayHitsBothNotesOfEveryDoublePerfectly()
        {
            AddUntilStep("wait for all judgements", () => Player.ScoreProcessor.HasCompleted.Value);
            AddAssert("all perfect", () => Player.ScoreProcessor.Statistics.GetValueOrDefault(HitResult.Perfect) == Player.ScoreProcessor.JudgedHits);
        }
    }
}
