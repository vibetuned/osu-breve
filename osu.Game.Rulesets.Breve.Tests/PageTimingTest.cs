using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Breve.Beatmaps;

namespace osu.Game.Rulesets.Breve.Tests
{
    [TestFixture]
    public class PageTimingTest
    {
        [TestCase(120, 4, 4 * 60000.0 / 120)] // one measure
        [TestCase(180, 4, 4 * 60000.0 / 180)] // one measure
        [TestCase(90, 4, 2 * 60000.0 / 90)] // half a measure
        [TestCase(240, 4, 8 * 60000.0 / 240)] // two measures
        [TestCase(150, 3, 6 * 60000.0 / 150)] // two measures, as 3/4 can't be split in half
        public void TestPageLengthFollowsTempoAndMeter(double bpm, int beatsPerMeasure, double expectedDuration)
        {
            Assert.That(PageTiming.ChoosePageDuration(60000 / bpm, beatsPerMeasure), Is.EqualTo(expectedDuration).Within(0.001));
        }

        [Test]
        public void TestPagesStartOnTimingPoints()
        {
            var pageTiming = new PageTiming(createControlPoints((0, 120, 4), (7000, 180, 4)));

            Assert.That(pageTiming.PageAt(7000).StartTime, Is.EqualTo(7000));
            Assert.That(pageTiming.PageAt(7001).Duration, Is.EqualTo(4 * 60000.0 / 180).Within(0.001));
        }

        [Test]
        public void TestDirectionAlternates()
        {
            var pageTiming = new PageTiming(createControlPoints((0, 120, 4)));

            Assert.That(pageTiming.PositionAt(0), Is.EqualTo(0));
            Assert.That(pageTiming.PositionAt(1000), Is.EqualTo(0.5));
            Assert.That(pageTiming.PositionAt(2000), Is.EqualTo(1));
            Assert.That(pageTiming.PositionAt(3000), Is.EqualTo(0.5));
            Assert.That(pageTiming.PositionAt(4000), Is.EqualTo(0));
        }

        [Test]
        public void TestShortLeftoverStretchesLastPage()
        {
            // 3¼ measures until the next timing point.
            var pageTiming = new PageTiming(createControlPoints((0, 120, 4), (6500, 120, 4)));
            Page lastPage = pageTiming.PageAt(6400);

            Assert.That(lastPage.StartTime, Is.EqualTo(4000));
            Assert.That(lastPage.Duration, Is.EqualTo(2500));
        }

        [Test]
        public void TestLongLeftoverBecomesItsOwnPage()
        {
            // 3¾ measures until the next timing point.
            var pageTiming = new PageTiming(createControlPoints((0, 120, 4), (7500, 120, 4)));
            Page lastPage = pageTiming.PageAt(7400);

            Assert.That(lastPage.StartTime, Is.EqualTo(6000));
            Assert.That(lastPage.Duration, Is.EqualTo(1500));
        }

        [Test]
        public void TestBriefTimingPointDoesNotGetItsOwnPages()
        {
            // The 200 BPM timing point lasts 100ms, far less than half of one of its pages.
            var pageTiming = new PageTiming(createControlPoints((0, 120, 4), (4000, 200, 4), (4100, 120, 4)));

            Assert.That(pageTiming.PageAt(4050).StartTime, Is.EqualTo(2000));
            Assert.That(pageTiming.PageAt(4100).StartTime, Is.EqualTo(4100));
        }

        [Test]
        public void TestPagesExtendBackwardsBeforeFirstTimingPoint()
        {
            var pageTiming = new PageTiming(createControlPoints((1000, 120, 4)));
            Page page = pageTiming.PageAt(0);

            Assert.That(page.Index, Is.EqualTo(-1));
            Assert.That(page.StartTime, Is.EqualTo(-1000));
            Assert.That(page.MovesUpwards, Is.False);
        }

        [Test]
        public void TestPageAfterCrossesTimingPoints()
        {
            // The first timing point's last page is stretched to end at the second one.
            var pageTiming = new PageTiming(createControlPoints((1000, 120, 4), (7500, 120, 4)));
            var startTimes = new List<double>();
            Page page = pageTiming.PageAt(0);

            for (int i = 0; i < 6; i++)
            {
                startTimes.Add(page.StartTime);
                page = pageTiming.PageAfter(page);
            }

            Assert.That(startTimes, Is.EqualTo(new double[] { -1000, 1000, 3000, 5000, 7500, 9500 }));
        }

        [Test]
        public void TestPagesBetween()
        {
            var pageTiming = new PageTiming(createControlPoints((0, 120, 4)));

            Assert.That(pageTiming.PagesBetween(500, 1500).Select(p => p.Index), Is.EqualTo(new[] { 0 }));
            Assert.That(pageTiming.PagesBetween(1500, 4500).Select(p => p.Index), Is.EqualTo(new[] { 0, 1, 2 }));
            Assert.That(pageTiming.PagesBetween(1500, 4000).Select(p => p.Index), Is.EqualTo(new[] { 0, 1 }), "A page ending exactly when the range does isn't followed by the next");
        }

        [Test]
        public void TestScanLineNeverJumps()
        {
            // Timing points landing mid-page, a tempo change and a change of meter.
            var pageTiming = new PageTiming(createControlPoints((0, 120, 4), (5300, 175, 4), (11000, 140, 3)));
            double previous = pageTiming.PositionAt(-1000);

            for (double time = -999; time < 20000; time++)
            {
                double position = pageTiming.PositionAt(time);
                Assert.That(Math.Abs(position - previous), Is.LessThan(0.01), $"The scan line jumped at {time}ms");
                previous = position;
            }
        }

        private static ControlPointInfo createControlPoints(params (double time, double bpm, int beatsPerMeasure)[] timingPoints)
        {
            var controlPointInfo = new ControlPointInfo();

            foreach ((double time, double bpm, int beatsPerMeasure) in timingPoints)
                controlPointInfo.Add(time, new TimingControlPoint { BeatLength = 60000 / bpm, TimeSignature = new TimeSignature(beatsPerMeasure) });

            return controlPointInfo;
        }
    }
}
