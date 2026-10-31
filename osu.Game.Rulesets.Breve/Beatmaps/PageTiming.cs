using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps.ControlPoints;

namespace osu.Game.Rulesets.Breve.Beatmaps
{
    /// <summary>
    /// Splits a beatmap's timeline into <see cref="Page"/>s, the sweeps of the scan line, following the tempo and meter of its timing points.
    /// </summary>
    /// <remarks>
    /// Every timing point starts a new page, so that sweeps begin on a downbeat like they do in hand-made charts.
    /// Within a timing point every page has the same musical length: a beat, half a measure or a whole number of measures,
    /// whichever lasts closest to <see cref="TARGET_PAGE_DURATION"/>.
    /// </remarks>
    public class PageTiming
    {
        /// <summary>
        /// The duration, in milliseconds, that a page's musical length is chosen to be closest to.
        /// </summary>
        public const double TARGET_PAGE_DURATION = 1800;

        private readonly List<Section> sections = new List<Section>();

        public PageTiming(ControlPointInfo controlPointInfo)
        {
            IReadOnlyList<TimingControlPoint> timingPoints = controlPointInfo.TimingPoints.Count > 0
                ? controlPointInfo.TimingPoints
                : new[] { TimingControlPoint.DEFAULT };

            var sectionStarts = new List<(double time, double pageDuration)>();

            for (int i = 0; i < timingPoints.Count; i++)
            {
                double pageDuration = ChoosePageDuration(timingPoints[i].BeatLength, timingPoints[i].TimeSignature.Numerator);

                // A timing point that is replaced again within half a page doesn't get pages of its own. Its time is absorbed into the previous section instead.
                if (i < timingPoints.Count - 1 && timingPoints[i + 1].Time - timingPoints[i].Time < pageDuration / 2)
                    continue;

                sectionStarts.Add((timingPoints[i].Time, pageDuration));
            }

            int firstPageIndex = 0;

            for (int i = 0; i < sectionStarts.Count; i++)
            {
                (double startTime, double pageDuration) = sectionStarts[i];

                if (i == sectionStarts.Count - 1)
                {
                    sections.Add(new Section(startTime, pageDuration, firstPageIndex, int.MaxValue, pageDuration));
                    break;
                }

                double length = sectionStarts[i + 1].time - startTime;
                int fullPages = (int)Math.Floor(length / pageDuration);
                double remainder = length - fullPages * pageDuration;

                // A section rarely lasts a whole number of pages, so its last page absorbs the leftover time:
                // a short leftover stretches the last full page, and a long one becomes a shorter page of its own.
                var section = fullPages > 0 && remainder < pageDuration / 2
                    ? new Section(startTime, pageDuration, firstPageIndex, fullPages, pageDuration + remainder)
                    : new Section(startTime, pageDuration, firstPageIndex, fullPages + 1, remainder);

                sections.Add(section);
                firstPageIndex += section.PageCount;
            }
        }

        /// <summary>
        /// Retrieves the page that is being swept at <paramref name="time"/>. The first timing point's pages extend backwards to cover any earlier time.
        /// </summary>
        public Page PageAt(double time)
        {
            for (int i = sections.Count - 1; i > 0; i--)
            {
                if (time >= sections[i].StartTime)
                    return sections[i].PageAt(time);
            }

            return sections[0].PageAt(time);
        }

        /// <summary>
        /// Retrieves the scan line's position at <paramref name="time"/>, from 0 at the bottom of its sweep to 1 at the top.
        /// </summary>
        public double PositionAt(double time)
        {
            Page page = PageAt(time);
            double progress = Math.Clamp(page.ProgressAt(time), 0, 1);

            return page.MovesUpwards ? progress : 1 - progress;
        }

        /// <summary>
        /// Retrieves the page that follows another.
        /// </summary>
        public Page PageAfter(Page page)
        {
            int index = page.Index + 1;

            for (int i = sections.Count - 1; i > 0; i--)
            {
                if (index >= sections[i].FirstPageIndex)
                    return sections[i].Page(index - sections[i].FirstPageIndex);
            }

            return sections[0].Page(index - sections[0].FirstPageIndex);
        }

        /// <summary>
        /// Retrieves the pages swept between two times, in order.
        /// </summary>
        public IEnumerable<Page> PagesBetween(double startTime, double endTime)
        {
            Page page = PageAt(startTime);
            yield return page;

            while (page.EndTime < endTime)
            {
                page = PageAfter(page);
                yield return page;
            }
        }

        /// <summary>
        /// Chooses how long pages last for a tempo and meter: a beat, half a measure (when the measure splits evenly into two),
        /// or a power-of-two number of measures, whichever is closest to <see cref="TARGET_PAGE_DURATION"/>.
        /// </summary>
        /// <param name="beatLength">The length of one beat, in milliseconds.</param>
        /// <param name="beatsPerMeasure">The number of beats in a measure.</param>
        /// <returns>The duration of a page, in milliseconds.</returns>
        public static double ChoosePageDuration(double beatLength, int beatsPerMeasure)
        {
            var beatsPerPage = new List<int> { 1 };

            if (beatsPerMeasure % 2 == 0)
                beatsPerPage.Add(beatsPerMeasure / 2);

            for (int measures = 1; measures <= 64; measures *= 2)
                beatsPerPage.Add(beatsPerMeasure * measures);

            // Compare ratios rather than differences, so that a page twice as long as the target is as far off as one half as long.
            return beatsPerPage.Select(beats => beats * beatLength)
                               .MinBy(duration => Math.Abs(Math.Log(duration / TARGET_PAGE_DURATION)));
        }

        /// <summary>
        /// The pages that follow one timing point, all sharing its tempo and meter.
        /// </summary>
        /// <param name="StartTime">The time of the timing point.</param>
        /// <param name="PageDuration">How long each page lasts, except the last.</param>
        /// <param name="FirstPageIndex">The index of the section's first page.</param>
        /// <param name="PageCount">How many pages the section has. The section of the last timing point never ends.</param>
        /// <param name="LastPageDuration">How long the last page lasts.</param>
        private readonly record struct Section(double StartTime, double PageDuration, int FirstPageIndex, int PageCount, double LastPageDuration)
        {
            public Page PageAt(double time) => Page((int)Math.Floor((time - StartTime) / PageDuration));

            /// <summary>
            /// Retrieves one of the section's pages, by its position in the section. Negative positions extend the section backwards.
            /// </summary>
            public Page Page(int index)
            {
                if (index >= PageCount - 1)
                    return new Page(FirstPageIndex + PageCount - 1, StartTime + (PageCount - 1) * PageDuration, LastPageDuration);

                return new Page(FirstPageIndex + index, StartTime + index * PageDuration, PageDuration);
            }
        }
    }
}
