using System.Collections.Generic;
using osu.Game.Audio;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.Breve.Objects
{
    /// <summary>
    /// A note that is pressed as the scan line passes over it, and held until <see cref="EndTime"/>.
    /// It is a long hold when it lasts past the end of its page.
    /// </summary>
    public class Hold : BreveHitObject, IHasDuration
    {
        public double Duration { get; set; }

        public double EndTime => StartTime + Duration;

        /// <summary>
        /// The samples played when the hold is pressed. <see cref="HitObject.Samples"/> are played when it is completed.
        /// </summary>
        public IList<HitSampleInfo> PressSamples { get; set; } = new List<HitSampleInfo>();

        /// <summary>
        /// The parts of the hold's body, one for each sweep of the scan line while the hold is held, in order.
        /// A hold that lasts past the end of its page has one on its own page and one on each page it continues onto.
        /// </summary>
        public IReadOnlyList<HoldSegment> Segments { get; set; } = new List<HoldSegment>();
    }
}
