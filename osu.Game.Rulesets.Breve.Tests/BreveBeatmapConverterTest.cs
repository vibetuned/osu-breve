using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Beatmaps;
using osu.Game.Rulesets.Breve.Mods;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Osu;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Tests.Beatmaps;

namespace osu.Game.Rulesets.Breve.Tests
{
    [TestFixture]
    public class BreveBeatmapConverterTest
    {
        [Test]
        public void TestNotesSitInTheirLaneOnTheScanLine()
        {
            var source = new TestBeatmap(new OsuRuleset().RulesetInfo);
            var converted = convert(source);
            var pageTiming = new PageTiming(source.ControlPointInfo);

            foreach (var note in converted.HitObjects)
            {
                Assert.That(note.X, Is.EqualTo(BrevePlayfield.LaneX(note.Lane)));
                Assert.That(note.Y, Is.EqualTo(BrevePlayfield.ScanLineY(pageTiming.PositionAt(note.StartTime))), $"The note at {note.StartTime}ms isn't on the scan line");
            }
        }

        [Test]
        public void TestTapsAreAsLargeAsOsuHitCircles()
        {
            var source = new TestBeatmap(new OsuRuleset().RulesetInfo);
            var osuBeatmap = new OsuRuleset().CreateBeatmapConverter(source).Convert();
            var osuCircle = osuBeatmap.HitObjects.OfType<HitCircle>().First();
            osuCircle.ApplyDefaults(osuBeatmap.ControlPointInfo, osuBeatmap.Difficulty);
            double osuCircleDiameter = 2 * osuCircle.Radius;

            var converted = convert(source);

            Assert.That(converted.HitObjects.OfType<Tap>().Select(t => (double)t.Diameter), Is.All.EqualTo(osuCircleDiameter).Within(0.01));
            Assert.That(converted.HitObjects.OfType<DragNode>().Select(n => (double)n.Diameter), Is.All.LessThan(osuCircleDiameter));
        }

        // Lanes count from 0 here, so the centre lane is 4.
        [TestCase(NoteTypesBeatmap.ZIGZAG_TIME, 1, 7)]
        [TestCase(NoteTypesBeatmap.CENTRE_ZIGZAG_TIME, 4, 0)]
        [TestCase(NoteTypesBeatmap.NEAR_CENTRE_ZIGZAG_TIME, 3, 7)]
        public void TestExpertTurnsHalfBeatSlidersIntoZigzags(double time, int headLane, int tailLane)
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo), expert: true);
            var head = noteAt(converted, time);
            var tail = noteAt(converted, time + NoteTypesBeatmap.BEAT_LENGTH / 2);

            Assert.That(head, Is.TypeOf<Tap>());
            Assert.That(tail, Is.TypeOf<Tap>());
            Assert.That(head.Lane, Is.EqualTo(headLane));
            Assert.That(tail.Lane, Is.EqualTo(tailLane));
        }

        [Test]
        public void TestCirclesAndShortSlidersBecomeTaps()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo));

            Assert.That(noteAt(converted, NoteTypesBeatmap.CIRCLE_TIME), Is.TypeOf<Tap>());
            Assert.That(noteAt(converted, NoteTypesBeatmap.SHORT_SLIDER_TIME), Is.TypeOf<Tap>());
        }

        [Test]
        public void TestSlidersThatDontComeBackBecomeHolds()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo));
            var hold = (Hold)noteAt(converted, NoteTypesBeatmap.SLIDER_TIME);

            Assert.That(hold.Duration, Is.EqualTo(4 * NoteTypesBeatmap.BEAT_LENGTH).Within(0.01));
            Assert.That(hold.Lane, Is.EqualTo(BreveBeatmapConverter.LaneFor(64)));
        }

        [Test]
        public void TestSpinnersBecomeHoldsInTheCentreLane()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo));
            var hold = (Hold)noteAt(converted, NoteTypesBeatmap.SPINNER_TIME);

            Assert.That(hold.Duration, Is.EqualTo(2 * NoteTypesBeatmap.BEAT_LENGTH));
            Assert.That(hold.Lane, Is.EqualTo(BrevePlayfield.LANE_COUNT / 2));
        }

        [Test]
        public void TestHoldWithinOneSweepHasOneBodyPart()
        {
            var source = NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo);
            var pageTiming = new PageTiming(source.ControlPointInfo);

            // The slider lasts exactly one downward sweep.
            var hold = (Hold)noteAt(convert(source), NoteTypesBeatmap.SLIDER_TIME);

            Assert.That(hold.Segments, Has.Count.EqualTo(1));
            Assert.That(hold.Segments[0].OnUpwardPage, Is.False);
            Assert.That(hold.Segments[0].FromY, Is.EqualTo(hold.Y));
            Assert.That(hold.Segments[0].ToY, Is.EqualTo(BrevePlayfield.ScanLineY(pageTiming.PositionAt(hold.EndTime))));
        }

        [Test]
        public void TestHoldContinuingOntoTheNextSweepIsSplit()
        {
            var source = DoublesBeatmap.Create(new OsuRuleset().RulesetInfo);
            var pageTiming = new PageTiming(source.ControlPointInfo);

            // The hold starts on a downward sweep and continues past the scan line turning around at the bottom.
            var hold = (Hold)noteAt(convert(source), DoublesBeatmap.LONG_HOLD_TIME);
            float bottom = BrevePlayfield.ScanLineY(0);

            Assert.That(hold.Segments, Has.Count.EqualTo(2));

            Assert.That(hold.Segments[0].OnUpwardPage, Is.False);
            Assert.That(hold.Segments[0].FromY, Is.EqualTo(hold.Y));
            Assert.That(hold.Segments[0].ToY, Is.EqualTo(bottom));

            Assert.That(hold.Segments[1].OnUpwardPage, Is.True);
            Assert.That(hold.Segments[1].PageStartTime, Is.EqualTo(hold.Segments[0].PageEndTime));
            Assert.That(hold.Segments[1].FromY, Is.EqualTo(bottom));
            Assert.That(hold.Segments[1].ToY, Is.EqualTo(BrevePlayfield.ScanLineY(pageTiming.PositionAt(hold.EndTime))));
        }

        [Test]
        public void TestSlidersThatComeBackBecomeDragChains()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo));
            double startTime = NoteTypesBeatmap.REVERSE_SLIDER_TIME;
            double spanDuration = 4 * NoteTypesBeatmap.BEAT_LENGTH;
            var nodes = converted.HitObjects.OfType<DragNode>().Where(n => n.StartTime <= startTime + 2 * spanDuration + 1).ToList();

            Assert.That(nodes[0].StartTime, Is.EqualTo(startTime));
            Assert.That(nodes[0].IsChainHead);
            Assert.That(nodes[0].Lane, Is.EqualTo(BreveBeatmapConverter.LaneFor(64)));
            Assert.That(nodes[^1].StartTime, Is.EqualTo(startTime + 2 * spanDuration).Within(0.01));

            // The chain pushes out towards the slider's end, and comes back.
            var turnaround = nodes.Single(n => Precision.AlmostEquals(n.StartTime, startTime + spanDuration));

            Assert.That(turnaround.Lane, Is.GreaterThan(nodes[0].Lane));
            Assert.That(nodes[^1].Lane, Is.LessThan(turnaround.Lane));

            for (int i = 1; i < nodes.Count; i++)
            {
                double quarterBeats = (nodes[i].StartTime - startTime) / (NoteTypesBeatmap.BEAT_LENGTH / 4);

                Assert.That(nodes[i].IsChainHead, Is.False);
                Assert.That(nodes[i].PreviousNode, Is.SameAs(nodes[i - 1]));
                Assert.That(quarterBeats, Is.EqualTo(Math.Round(quarterBeats)).Within(1e-6), $"The node at {nodes[i].StartTime}ms isn't on a quarter beat");
            }
        }

        [Test]
        public void TestDragChainsAreEitherAllDiagonalOrAllStraight()
        {
            var beatmaps = new IBeatmap[]
            {
                NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo),
                new TestBeatmap(new OsuRuleset().RulesetInfo),
            };

            foreach (var beatmap in beatmaps)
            {
                foreach (var chain in chainsIn(convert(beatmap)))
                {
                    var laneChanges = chain.Zip(chain.Skip(1), (previous, node) => Math.Abs(node.Lane - previous.Lane)).Distinct().ToList();

                    Assert.That(laneChanges, Is.EquivalentTo(new[] { 1 }).Or.EquivalentTo(new[] { 0 }).Or.Empty,
                        $"The chain starting at {chain[0].StartTime}ms mixes straight and diagonal links");
                }
            }
        }

        [Test]
        public void TestDragChainsZigzagWithTheirSlider()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo));
            var nodes = converted.HitObjects.OfType<DragNode>().ToList();

            // The 4 beat slider's chain moves right on its way out and left on its way back.
            double startTime = NoteTypesBeatmap.REVERSE_SLIDER_TIME;
            double spanDuration = 4 * NoteTypesBeatmap.BEAT_LENGTH;
            var slowChain = nodes.Where(n => n.StartTime <= startTime + 2 * spanDuration + 1).ToList();

            for (int i = 1; i < slowChain.Count; i++)
            {
                int expectedChange = slowChain[i].StartTime <= startTime + spanDuration + 1 ? 1 : -1;
                Assert.That(slowChain[i].Lane - slowChain[i - 1].Lane, Is.EqualTo(expectedChange), $"The chain goes the wrong way at {slowChain[i].StartTime}ms");
            }

            // The fast slider's chain can only move one lane each way, so it zigzags with the slider one lane at a time.
            var fastChain = nodes.Where(n => n.StartTime >= NoteTypesBeatmap.FAST_REVERSE_SLIDER_TIME && n.StartTime < NoteTypesBeatmap.QUICK_REVERSE_SLIDER_TIME).ToList();

            for (int i = 1; i < fastChain.Count; i++)
                Assert.That(Math.Abs(fastChain[i].Lane - fastChain[i - 1].Lane), Is.EqualTo(1), $"The fast chain runs straight at {fastChain[i].StartTime}ms");

            for (int i = 2; i < fastChain.Count; i++)
            {
                int change = fastChain[i].Lane - fastChain[i - 1].Lane;
                int previousChange = fastChain[i - 1].Lane - fastChain[i - 2].Lane;

                Assert.That(change, Is.EqualTo(-previousChange), $"The fast chain doesn't zigzag at {fastChain[i].StartTime}ms");
            }
        }

        [Test]
        public void TestSlidersComingBackTooSoonForADiagonalBecomeStraightChains()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo));
            var chain = converted.HitObjects.OfType<DragNode>().Where(n => n.StartTime >= NoteTypesBeatmap.QUICK_REVERSE_SLIDER_TIME).ToList();

            // Head, turnaround and tail, a quarter beat apart, all in the head's lane.
            Assert.That(chain.Select(n => n.StartTime), Is.EqualTo(new[] { 0.0, 0.25, 0.5 }.Select(b => NoteTypesBeatmap.QUICK_REVERSE_SLIDER_TIME + b * NoteTypesBeatmap.BEAT_LENGTH)).Within(0.01));
            Assert.That(chain.Select(n => n.Lane), Is.All.EqualTo(BreveBeatmapConverter.LaneFor(64)));
        }

        private static List<List<DragNode>> chainsIn(Beatmap<BreveHitObject> beatmap)
        {
            var chains = new List<List<DragNode>>();

            foreach (var node in beatmap.HitObjects.OfType<DragNode>())
            {
                if (node.IsChainHead)
                    chains.Add(new List<DragNode>());

                chains[^1].Add(node);
            }

            return chains;
        }

        [Test]
        public void TestDragChainsMoveOneLaneAtATimeAndNeverFlattenOut()
        {
            var beatmaps = new IBeatmap[]
            {
                NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo),
                new TestBeatmap(new OsuRuleset().RulesetInfo),
            };

            foreach (var beatmap in beatmaps)
            {
                var nodes = convert(beatmap).HitObjects.OfType<DragNode>().ToList();

                for (int i = 1; i < nodes.Count; i++)
                {
                    if (nodes[i].IsChainHead)
                        continue;

                    int laneChange = Math.Abs(nodes[i].Lane - nodes[i - 1].Lane);
                    float verticalTravel = Math.Abs(nodes[i].Y - nodes[i - 1].Y);

                    Assert.That(laneChange, Is.LessThanOrEqualTo(1), $"The chain jumped more than one lane at {nodes[i].StartTime}ms");

                    if (laneChange == 1)
                        Assert.That(verticalTravel * BreveBeatmapConverter.MAX_DRAG_SLOPE, Is.GreaterThanOrEqualTo(BrevePlayfield.LaneWidth - 0.01f), $"The link to the node at {nodes[i].StartTime}ms is too flat");
                }
            }
        }

        [TestCase(0, 0)]
        [TestCase(256, BrevePlayfield.LANE_COUNT / 2)]
        [TestCase(512, BrevePlayfield.LANE_COUNT - 1)]
        [TestCase(-40, 0)]
        [TestCase(600, BrevePlayfield.LANE_COUNT - 1)]
        public void TestLaneForPosition(float x, int expectedLane)
        {
            Assert.That(BreveBeatmapConverter.LaneFor(x), Is.EqualTo(expectedLane));
        }

        [Test]
        public void TestMirroredPositionsLandInMirroredLanes()
        {
            for (int x = 0; x <= 512; x++)
            {
                int lane = BreveBeatmapConverter.LaneFor(x);
                int mirroredLane = BreveBeatmapConverter.LaneFor(512 - x);

                Assert.That(lane + mirroredLane, Is.EqualTo(BrevePlayfield.LANE_COUNT - 1), $"x = {x} and x = {512 - x} aren't in mirrored lanes");
            }
        }

        [Test]
        public void TestSourceObjectsAreConvertedInOrder()
        {
            var source = new TestBeatmap(new OsuRuleset().RulesetInfo);
            var converted = convert(source);

            Assert.That(converted.HitObjects.Select(h => h.StartTime), Is.Ordered);
            Assert.That(converted.HitObjects.Count, Is.GreaterThanOrEqualTo(source.HitObjects.Count));
            Assert.That(source.HitObjects.All(h => converted.HitObjects.Any(n => Precision.AlmostEquals(n.StartTime, h.StartTime))), "Every source object has a note at its start time");
        }

        [Test]
        public void TestHoldsKeepTheirSourceHitsounds()
        {
            var source = NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo);
            var converted = convert(source);

            var slider = (IHasPathWithRepeats)source.HitObjects.Single(h => h.StartTime == NoteTypesBeatmap.SLIDER_TIME);
            var sliderHold = (Hold)noteAt(converted, NoteTypesBeatmap.SLIDER_TIME);

            Assert.That(sliderHold.PressSamples, Is.EqualTo(slider.NodeSamples[0]));
            Assert.That(sliderHold.Samples, Is.EqualTo(slider.NodeSamples[^1]));

            var spinner = source.HitObjects.Single(h => h.StartTime == NoteTypesBeatmap.SPINNER_TIME);
            var spinnerHold = (Hold)noteAt(converted, NoteTypesBeatmap.SPINNER_TIME);

            Assert.That(spinnerHold.PressSamples, Is.Empty);
            Assert.That(spinnerHold.Samples, Is.EqualTo(spinner.Samples));
        }

        [Test]
        public void TestFlicksTurnShortSlidersIntoFlicks()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo), flicks: true);

            // Both sliders move to the right, so their flicks are between their head's lane and the next one.
            assertFlickBetween(noteAt(converted, NoteTypesBeatmap.ZIGZAG_TIME), 1);
            assertFlickBetween(noteAt(converted, NoteTypesBeatmap.SHORT_SLIDER_TIME), 4);

            Assert.That(noteAt(converted, NoteTypesBeatmap.CIRCLE_TIME), Is.TypeOf<Tap>(), "circles stay taps");
            Assert.That(noteAt(converted, NoteTypesBeatmap.BEAT_SLIDER_TIME), Is.TypeOf<Tap>(), "sliders long enough to zigzag still do");
        }

        [Test]
        public void TestExpertKeepsZigzagsWithFlicks()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo), expert: true, flicks: true);

            Assert.That(noteAt(converted, NoteTypesBeatmap.ZIGZAG_TIME), Is.TypeOf<Tap>(), "half beat sliders zigzag in the expert chart");
            assertFlickBetween(noteAt(converted, NoteTypesBeatmap.SHORT_SLIDER_TIME), 4);
        }

        [Test]
        public void TestNoFlicksWithoutTheMod()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo));

            Assert.That(converted.HitObjects.OfType<Flick>(), Is.Empty);
        }

        [Test]
        public void TestFlicksPointTheWayTheirSliderMoves()
        {
            const string beatmap = @"osu file format v14

[Difficulty]
CircleSize:4
SliderMultiplier:1

[TimingPoints]
0,500,4,1,0,100,1,0

[HitObjects]
400,192,1000,2,0,L|350:192,1,50
88,192,3000,2,0,L|88:242,1,50
424,192,5000,2,0,L|424:242,1,50
32,192,7000,2,0,L|0:192,1,32
";

            var converted = convert(LegacyTestBeatmap.Decode(beatmap, new OsuRuleset().RulesetInfo), flicks: true);

            assertFlickBetween(noteAt(converted, 1000), 5, "a slider moving left from lane 6 flicks between lanes 5 and 6");
            assertFlickBetween(noteAt(converted, 3000), 1, "a slider that doesn't move sideways flicks towards the centre");
            assertFlickBetween(noteAt(converted, 5000), 6, "a slider that doesn't move sideways flicks towards the centre");
            assertFlickBetween(noteAt(converted, 7000), 0, "a slider moving left from the leftmost lane flicks into the next lane over");
        }

        private static void assertFlickBetween(BreveHitObject note, int leftLane, string? message = null)
        {
            Assert.That(note, Is.TypeOf<Flick>(), message);
            Assert.That(note.Lane, Is.EqualTo(leftLane), message);
            Assert.That(note.X, Is.EqualTo((BrevePlayfield.LaneX(leftLane) + BrevePlayfield.LaneX(leftLane + 1)) / 2), message);
        }

        private static BreveHitObject noteAt(Beatmap<BreveHitObject> beatmap, double time) => beatmap.HitObjects.Single(h => Precision.AlmostEquals(h.StartTime, time));

        [Test]
        public void TestNormalTurnsHalfBeatSlidersIntoTaps()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo));

            Assert.That(noteAt(converted, NoteTypesBeatmap.ZIGZAG_TIME), Is.TypeOf<Tap>());
            Assert.That(converted.HitObjects.Any(h => Precision.AlmostEquals(h.StartTime, NoteTypesBeatmap.ZIGZAG_TIME + NoteTypesBeatmap.BEAT_LENGTH / 2)), Is.False);
        }

        [Test]
        public void TestNormalTurnsBeatSlidersIntoZigzags()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo));
            var head = noteAt(converted, NoteTypesBeatmap.BEAT_SLIDER_TIME);
            var tail = noteAt(converted, NoteTypesBeatmap.BEAT_SLIDER_TIME + NoteTypesBeatmap.BEAT_LENGTH);

            Assert.That(head, Is.TypeOf<Tap>());
            Assert.That(tail, Is.TypeOf<Tap>());
            Assert.That(head.Lane, Is.EqualTo(1));
            Assert.That(tail.Lane, Is.EqualTo(7));
        }

        [Test]
        public void TestExpertTurnsBeatSlidersIntoHolds()
        {
            var converted = convert(NoteTypesBeatmap.Create(new OsuRuleset().RulesetInfo), expert: true);

            Assert.That(noteAt(converted, NoteTypesBeatmap.BEAT_SLIDER_TIME), Is.TypeOf<Hold>());
        }

        private static Beatmap<BreveHitObject> convert(IBeatmap beatmap, bool expert = false, bool flicks = false)
        {
            var converter = new BreveRuleset().CreateBeatmapConverter(beatmap);

            if (expert)
                new BreveModExpert().ApplyToBeatmapConverter(converter);

            if (flicks)
                new BreveModFlicks().ApplyToBeatmapConverter(converter);

            return (Beatmap<BreveHitObject>)converter.Convert();
        }
    }
}
