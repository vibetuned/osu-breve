using NUnit.Framework;
using osu.Game.Rulesets.Breve.Scoring;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Breve.Tests
{
    [TestFixture]
    public class BreveHitWindowsTest
    {
        [TestCase(0)]
        [TestCase(5)]
        [TestCase(10)]
        public void TestPressesTooEarlyForABadAreIgnored(double overallDifficulty)
        {
            var hitWindows = new BreveHitWindows();
            hitWindows.SetDifficulty(overallDifficulty);

            double bad = hitWindows.WindowFor(HitResult.Meh);

            Assert.That(hitWindows.ResultFor(-bad), Is.EqualTo(HitResult.Meh));
            Assert.That(hitWindows.ResultFor(-bad - 1), Is.EqualTo(HitResult.None));
        }

        [Test]
        public void TestWindowsStayWideAtHighOverallDifficulty()
        {
            var hitWindows = new BreveHitWindows();
            hitWindows.SetDifficulty(10);

            Assert.That(hitWindows.WindowFor(HitResult.Perfect), Is.GreaterThanOrEqualTo(55));
            Assert.That(hitWindows.WindowFor(HitResult.Good), Is.GreaterThanOrEqualTo(110));
            Assert.That(hitWindows.WindowFor(HitResult.Meh), Is.GreaterThanOrEqualTo(160));
        }
    }
}
