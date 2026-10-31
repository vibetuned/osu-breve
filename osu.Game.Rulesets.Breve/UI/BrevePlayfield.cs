using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Breve.Beatmaps;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Breve.Objects.Drawables;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;
using osuTK;

namespace osu.Game.Rulesets.Breve.UI
{
    public partial class BrevePlayfield : Playfield
    {
        /// <summary>
        /// The size of the playfield, in the units hit object positions are given in.
        /// It matches osu!'s, so that x positions carry over from converted beatmaps unchanged.
        /// </summary>
        public static readonly Vector2 BASE_SIZE = new Vector2(512, 384);

        /// <summary>
        /// How far from the top and bottom edges the scan line turns around.
        /// </summary>
        public const float VERTICAL_MARGIN = 32;

        /// <summary>
        /// How far from the left and right edges the outermost lanes are: the radius of a tap at circle size 5,
        /// so that the outer lanes' notes stay on the playfield, or only just leave it at lower circle sizes.
        /// </summary>
        public const float HORIZONTAL_MARGIN = 32;

        /// <summary>
        /// The number of lanes notes are placed in. It is odd, so that there is a centre lane.
        /// </summary>
        public const int LANE_COUNT = 9;

        private readonly PageTiming pageTiming;

        public BrevePlayfield(PageTiming pageTiming)
        {
            this.pageTiming = pageTiming;
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal(new Drawable[]
            {
                new ScanLine(pageTiming),
                HitObjectContainer,
                new TouchLaneMapper(),
            });

            RegisterPool<Tap, DrawableTap>(20, 100);
            RegisterPool<Hold, DrawableHold>(10, 50);
            RegisterPool<DragNode, DrawableDragNode>(50, 300);
        }

        /// <summary>
        /// Converts a scan line position, from 0 at the bottom of its sweep to 1 at the top, into a y coordinate on the playfield.
        /// </summary>
        public static float ScanLineY(double position) => (float)(BASE_SIZE.Y - VERTICAL_MARGIN - position * (BASE_SIZE.Y - 2 * VERTICAL_MARGIN));

        /// <summary>
        /// The distance between the centres of neighbouring lanes. Lanes are spread evenly between the horizontal margins.
        /// </summary>
        public static float LaneWidth => (BASE_SIZE.X - 2 * HORIZONTAL_MARGIN) / (LANE_COUNT - 1);

        /// <summary>
        /// Retrieves the x coordinate of a lane's centre, counting lanes from the left.
        /// </summary>
        public static float LaneX(int lane) => HORIZONTAL_MARGIN + lane * LaneWidth;

        protected override HitObjectLifetimeEntry CreateLifetimeEntry(HitObject hitObject) => new BreveHitObjectLifetimeEntry(hitObject);

        private class BreveHitObjectLifetimeEntry : HitObjectLifetimeEntry
        {
            public BreveHitObjectLifetimeEntry(HitObject hitObject)
                : base(hitObject)
            {
                // Prevent past objects in idle states from remaining alive, as their end times are skipped in non-frame-stable contexts.
                LifetimeEnd = HitObject.GetEndTime() + HitObject.HitWindows.WindowFor(HitResult.Miss);
            }

            protected override double InitialLifetimeOffset => ((BreveHitObject)HitObject).TimePreempt;
        }
    }
}
