using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Scoring;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// Watch autoplay play osu!'s test beatmap converted to osu!breve.
    /// </summary>
    public partial class TestSceneBreveAutoplay : PlayerTestScene
    {
        protected override Ruleset CreatePlayerRuleset() => new BreveRuleset();

        protected override bool Autoplay => true;

        [Test]
        public void TestAutoplayHitsPerfectly()
        {
            AddUntilStep("wait for 20 judgements", () => Player.ScoreProcessor.JudgedHits >= 20);
            AddAssert("all perfect", () => Player.ScoreProcessor.Statistics.GetValueOrDefault(HitResult.Perfect) == Player.ScoreProcessor.JudgedHits);
        }

        [Test]
        public void TestScanLineSpansTheScreen()
        {
            AddUntilStep("scan line and its glow as wide as the screen", () =>
            {
                var parts = Player.DrawableRuleset.ChildrenOfType<ScanLine>().Single().ChildrenOfType<Box>().Select(b => b.ScreenSpaceDrawQuad);
                var screen = Player.DrawableRuleset.ScreenSpaceDrawQuad;

                return parts.All(part => Precision.AlmostEquals(part.TopLeft.X, screen.TopLeft.X, 1) && Precision.AlmostEquals(part.TopRight.X, screen.TopRight.X, 1));
            });
        }
    }
}
