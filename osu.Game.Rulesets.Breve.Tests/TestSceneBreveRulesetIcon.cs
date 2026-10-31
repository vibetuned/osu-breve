using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Game.Graphics.Containers;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// The ruleset's icon at a few of the sizes it's shown at, scaled to fit the way the game shows ruleset icons.
    /// </summary>
    public partial class TestSceneBreveRulesetIcon : OsuTestScene
    {
        public TestSceneBreveRulesetIcon()
        {
            Child = new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Spacing = new Vector2(40),
                Children = new[] { 24f, 48f, 128f }.Select(size => new ConstrainedIconContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(size),
                    Icon = new BreveRuleset().CreateIcon(),
                }).ToArray(),
            };
        }
    }
}
