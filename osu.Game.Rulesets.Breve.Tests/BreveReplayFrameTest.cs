using NUnit.Framework;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Replays;
using osuTK;

namespace osu.Game.Rulesets.Breve.Tests
{
    [TestFixture]
    public class BreveReplayFrameTest
    {
        [Test]
        public void TestActionsSurviveLegacyConversion()
        {
            var beatmap = new Beatmap();
            var frame = new BreveReplayFrame(1234, new Vector2(100, 200), BreveAction.Tap1, BreveAction.Lane1, BreveAction.Lane5, BreveAction.Lane9);

            var restored = new BreveReplayFrame();
            restored.FromLegacy(frame.ToLegacy(beatmap), beatmap);

            Assert.That(restored.Position, Is.EqualTo(frame.Position));
            Assert.That(restored.Actions, Is.EquivalentTo(frame.Actions));
        }
    }
}
