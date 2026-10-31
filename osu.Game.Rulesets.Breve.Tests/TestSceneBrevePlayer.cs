using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// Play osu!'s test beatmap converted to osu!breve, clicking notes with the mouse.
    /// </summary>
    public partial class TestSceneBrevePlayer : PlayerTestScene
    {
        protected override Ruleset CreatePlayerRuleset() => new BreveRuleset();
    }
}
