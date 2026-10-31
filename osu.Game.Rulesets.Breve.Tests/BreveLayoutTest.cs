using System;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Mods;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Breve.Tests
{
    [TestFixture]
    public class BreveLayoutTest
    {
        // Lanes count from 0 here, so the centre lane is 4.
        [TestCase(LayoutBeatmap.BEFORE_TURNAROUND_TIME, 1)]
        [TestCase(LayoutBeatmap.AFTER_TURNAROUND_TIME, 3)]
        [TestCase(LayoutBeatmap.STACK_TIME, 4)]
        [TestCase(LayoutBeatmap.STACK_TIME + 125, 6)]
        [TestCase(LayoutBeatmap.STACK_TIME + 250, 2)]
        [TestCase(LayoutBeatmap.HOLD_TIME, 7)]
        [TestCase(LayoutBeatmap.DURING_HOLD_TIME, 5)]
        public void TestClashingNotesMoveToTheNearestFreeLane(double time, int expectedLane)
        {
            var converted = convert(LayoutBeatmap.Create(new OsuRuleset().RulesetInfo));
            var note = converted.HitObjects.Single(h => Precision.AlmostEquals(h.StartTime, time));

            Assert.That(note.Lane, Is.EqualTo(expectedLane));
        }

        [Test]
        public void TestNoNotesOverlapOrLandInAHeldLane()
        {
            var ruleset = new OsuRuleset().RulesetInfo;

            foreach (var beatmap in new IBeatmap[] { LayoutBeatmap.Create(ruleset), NoteTypesBeatmap.Create(ruleset), DoublesBeatmap.Create(ruleset) })
                assertNoOverlaps(beatmap, allowedOverlap: 0);
        }

        [TestCase(false)]
        [TestCase(true)]
        public void TestDensePassagesOnlyOverlapSlightly(bool expert)
        {
            // osu!'s test beatmap has passages too dense for every note to find a clear lane, where notes go where they overlap least instead.
            assertNoOverlaps(new TestBeatmap(new OsuRuleset().RulesetInfo), allowedOverlap: 0.25f, expert);
        }

        /// <summary>
        /// Asserts that no two notes on screen at the same time overlap by more than a proportion of the smaller one's diameter,
        /// and that no note is in a lane while a hold in it is held.
        /// </summary>
        private static void assertNoOverlaps(IBeatmap beatmap, float allowedOverlap, bool expert = false)
        {
            var converter = new BreveRuleset().CreateBeatmapConverter(beatmap);

            if (expert)
                new BreveModExpert().ApplyToBeatmapConverter(converter);

            var notes = ((Beatmap<BreveHitObject>)converter.Convert()).HitObjects;

            for (int i = 0; i < notes.Count; i++)
            {
                for (int j = i + 1; j < notes.Count; j++)
                {
                    var a = notes[i];
                    var b = notes[j];

                    // Drag chains keep their shape, so they're allowed to cross themselves at turnarounds.
                    if (a is DragNode && b is DragNode)
                        continue;

                    // Taps are wider than lanes, so notes in neighbouring lanes can overlap too.
                    bool onScreenTogether = a.StartTime - a.TimePreempt < b.GetEndTime() && b.StartTime - b.TimePreempt < a.GetEndTime();
                    float horizontalDistance = Math.Abs(a.Lane - b.Lane) * BrevePlayfield.LaneWidth;
                    float verticalDistance = a.Y - b.Y;
                    float overlap = (a.Diameter + b.Diameter) / 2 - MathF.Sqrt(horizontalDistance * horizontalDistance + verticalDistance * verticalDistance);

                    Assert.That(onScreenTogether && overlap > allowedOverlap * Math.Min(a.Diameter, b.Diameter), Is.False,
                        $"The notes at {a.StartTime}ms (lane {a.Lane}) and {b.StartTime}ms (lane {b.Lane}) overlap by {overlap:F0}");

                    if (a.Lane != b.Lane)
                        continue;

                    bool duringHold = (a is Hold && b.StartTime <= a.GetEndTime()) || (b is Hold && a.GetEndTime() >= b.StartTime);

                    Assert.That(duringHold, Is.False, $"The notes at {a.StartTime}ms and {b.StartTime}ms are in lane {a.Lane} while one of them is held");
                }
            }
        }

        private static Beatmap<BreveHitObject> convert(IBeatmap beatmap) => (Beatmap<BreveHitObject>)new BreveRuleset().CreateBeatmapConverter(beatmap).Convert();
    }
}
