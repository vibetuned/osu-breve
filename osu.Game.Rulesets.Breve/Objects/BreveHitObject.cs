using osu.Game.Rulesets.Breve.Beatmaps;
using osu.Game.Rulesets.Breve.Scoring;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Breve.Objects
{
    public abstract class BreveHitObject : HitObject, IHasPosition
    {
        /// <summary>
        /// The radius of a tap or hold at a <see cref="Scale"/> of 1, the same as osu!'s hit circles.
        /// </summary>
        public const float OBJECT_RADIUS = 64;

        /// <summary>
        /// How large the object is compared to <see cref="OBJECT_RADIUS"/>.
        /// The converter sets it from the beatmap's circle size the same way osu! does, so that taps are as large as osu!'s hit circles.
        /// </summary>
        public float Scale { get; set; } = 1;

        /// <summary>
        /// The diameter of the object, in playfield units.
        /// </summary>
        public virtual float Diameter => 2 * OBJECT_RADIUS * Scale;

        /// <summary>
        /// How long before <see cref="HitObject.StartTime"/> the object appears.
        /// The converter sets this to the duration of the object's page, so that notes show up one sweep ahead of the scan line.
        /// </summary>
        public double TimePreempt { get; set; } = PageTiming.TARGET_PAGE_DURATION;

        /// <summary>
        /// Whether the scan line is moving upwards when it reaches this object.
        /// </summary>
        public bool OnUpwardPage { get; set; }

        /// <summary>
        /// When the page this object is in starts. Until then, the scan line is still on the sweep before.
        /// </summary>
        public double PageStartTime { get; set; }

        /// <summary>
        /// The lane the object is in, counting from the left. Its x position is the lane's centre.
        /// </summary>
        public int Lane { get; set; }

        public Vector2 Position { get; set; }

        public float X
        {
            get => Position.X;
            set => Position = new Vector2(value, Y);
        }

        public float Y
        {
            get => Position.Y;
            set => Position = new Vector2(X, value);
        }

        protected override HitWindows CreateHitWindows() => new BreveHitWindows();
    }
}
