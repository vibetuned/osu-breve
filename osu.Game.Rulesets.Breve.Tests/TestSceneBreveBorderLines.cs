using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Rulesets.Breve.Mods;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// Watch autoplay with the Border Lines mod, which shows dotted lines where the scan line turns around.
    /// </summary>
    public partial class TestSceneBreveBorderLines : PlayerTestScene
    {
        protected override Ruleset CreatePlayerRuleset() => new BreveRuleset();

        protected override bool Autoplay => true;

        [Test]
        public void TestNoBorderLinesWithoutTheMod()
        {
            AddAssert("border lines hidden", () => !Player.DrawableRuleset.ChildrenOfType<BorderLines>().Single().IsPresent);
        }

        [Test]
        public void TestBorderLinesMarkWhereTheScanLineTurns()
        {
            loadWithBorderLines();

            // The lines move out briefly when the scan line turns around on them, so this waits until both are at rest.
            AddUntilStep("one at the top of the sweeps and one at the bottom", () =>
                Precision.AlmostEquals(topLine.Y, BrevePlayfield.ScanLineY(1)) && Precision.AlmostEquals(bottomLine.Y, BrevePlayfield.ScanLineY(0)));
        }

        [Test]
        public void TestDotsDriftInOppositeDirections()
        {
            float topDrift = 0;
            float bottomDrift = 0;

            loadWithBorderLines();

            AddStep("note where the dots are", () =>
            {
                topDrift = topLine.Drift;
                bottomDrift = bottomLine.Drift;
            });

            AddUntilStep("top dots drift left and bottom dots right", () => topLine.Drift < topDrift && bottomLine.Drift > bottomDrift);
        }

        [Test]
        public void TestLinesBumpOutwardsWhenTheScanLineTurnsOnThem()
        {
            bool topBumped = false;
            bool bottomBumped = false;

            loadWithBorderLines();

            // A bump is over in a fifth of a second, quicker than test steps run, so it's watched for on every frame.
            AddStep("watch for bumps", () =>
            {
                Add(new TimedAction(() => topLine.Y < BrevePlayfield.ScanLineY(1) - 1, () => topBumped = true));
                Add(new TimedAction(() => bottomLine.Y > BrevePlayfield.ScanLineY(0) + 1, () => bottomBumped = true));
            });

            AddUntilStep("top line bumps up", () => topBumped);
            AddUntilStep("bottom line bumps down", () => bottomBumped);
        }

        [Test]
        public void TestBorderLinesSpanTheScreen()
        {
            loadWithBorderLines();

            AddUntilStep("as wide as the screen, and dotted all the way", () =>
            {
                var screen = Player.DrawableRuleset.ScreenSpaceDrawQuad;

                return dottedLines.All(line =>
                {
                    float[] dots = line.ChildrenOfType<FastCircle>().Select(d => d.ScreenSpaceDrawQuad.Centre.X).OrderBy(x => x).ToArray();

                    if (dots.Length < 2)
                        return false;

                    // No gap at either end is wider than the gaps between dots.
                    float spacing = dots[1] - dots[0];

                    return Precision.AlmostEquals(line.ScreenSpaceDrawQuad.TopLeft.X, screen.TopLeft.X, 1)
                           && Precision.AlmostEquals(line.ScreenSpaceDrawQuad.TopRight.X, screen.TopRight.X, 1)
                           && dots[0] - screen.TopLeft.X <= spacing
                           && screen.TopRight.X - dots[^1] <= spacing;
                });
            });
        }

        private BorderLines.DottedLine[] dottedLines => Player.DrawableRuleset.ChildrenOfType<BorderLines.DottedLine>().ToArray();

        private BorderLines.DottedLine topLine => dottedLines.MinBy(l => l.Y)!;

        private BorderLines.DottedLine bottomLine => dottedLines.MaxBy(l => l.Y)!;

        private void loadWithBorderLines()
        {
            AddStep("load with border lines", () => LoadPlayer(new Mod[] { new BreveModBorderLines() }));
            AddUntilStep("wait for player", () => Player.IsLoaded);
        }
    }
}
