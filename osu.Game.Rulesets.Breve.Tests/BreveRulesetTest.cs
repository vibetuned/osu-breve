using System.Linq;
using NUnit.Framework;
using osu.Framework.Input.Bindings;
using osu.Game.Rulesets.Breve.UI;

namespace osu.Game.Rulesets.Breve.Tests
{
    [TestFixture]
    public class BreveRulesetTest
    {
        [Test]
        public void TestEachLaneHasAKeyAndAMidiKey()
        {
            var bindings = new BreveRuleset().GetDefaultKeyBindings().ToList();

            for (int lane = 0; lane < BrevePlayfield.LANE_COUNT; lane++)
            {
                var keys = bindings.Where(b => (BreveAction)b.Action == BreveAction.Lane1 + lane).Select(b => b.KeyCombination.Keys.Single()).ToList();

                Assert.That(keys, Has.Count.EqualTo(2), $"Lane {lane} doesn't have two bindings");
                Assert.That(keys.Count(isMidiKey), Is.EqualTo(1), $"Lane {lane} doesn't have exactly one MIDI binding");
            }
        }

        [Test]
        public void TestLaneMidiKeysGoUpTheKeyboard()
        {
            var midiKeys = new BreveRuleset().GetDefaultKeyBindings()
                                                 .Where(b => isMidiKey(b.KeyCombination.Keys.Single()))
                                                 .OrderBy(b => (int)b.Action)
                                                 .Select(b => b.KeyCombination.Keys.Single())
                                                 .ToList();

            Assert.That(midiKeys, Is.Ordered);
            Assert.That(midiKeys, Is.Unique);
        }

        private static bool isMidiKey(InputKey key) => key.ToString().StartsWith("Midi", System.StringComparison.Ordinal);
    }
}
