using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.Breve.Beatmaps;
using osuTK;

namespace osu.Game.Rulesets.Breve.UI
{
    /// <summary>
    /// Sparse dotted lines across the screen where the scan line turns around, at the top and bottom of its sweeps.
    /// </summary>
    /// <remarks>
    /// The dots drift along in time with the scan line, one gap per sweep: right to left along the top, and left to right along the bottom.
    /// Each line bumps outwards, and lights up, when the scan line turns around on it.
    /// </remarks>
    public partial class BorderLines : CompositeDrawable
    {
        /// <summary>
        /// How far a line moves outwards when the scan line turns around on it.
        /// </summary>
        private const float bump_height = 6;

        private const double bump_duration = 200;

        private const float resting_alpha = 0.5f;

        private readonly PageTiming pageTiming;
        private readonly DottedLine top;
        private readonly DottedLine bottom;

        public BorderLines(PageTiming pageTiming)
        {
            this.pageTiming = pageTiming;

            RelativeSizeAxes = Axes.Both;

            InternalChildren = new Drawable[]
            {
                top = new DottedLine(),
                bottom = new DottedLine(),
            };
        }

        protected override void Update()
        {
            base.Update();

            Page page = pageTiming.PageAt(Time.Current);

            float drift = (float)((page.Index + page.ProgressAt(Time.Current)) * DottedLine.DOT_SPACING);
            top.Drift = -drift;
            bottom.Drift = drift;

            // The scan line turns around on the bottom line as each upward sweep starts, and on the top line as each downward one starts.
            float bump = bumpAt(Time.Current - page.StartTime);
            place(top, BrevePlayfield.ScanLineY(1), page.MovesUpwards ? 0 : -bump);
            place(bottom, BrevePlayfield.ScanLineY(0), page.MovesUpwards ? bump : 0);
        }

        private static void place(DottedLine line, float restingY, float bump)
        {
            line.Y = restingY + bump;
            line.Alpha = resting_alpha + (1 - resting_alpha) * Math.Abs(bump) / bump_height;
        }

        /// <summary>
        /// How far out a line is a time after the scan line turned around on it: out and back in in a quick arc.
        /// </summary>
        private static float bumpAt(double timeSinceTurn)
            => timeSinceTurn < bump_duration ? bump_height * (float)Math.Sin(Math.PI * timeSinceTurn / bump_duration) : 0;

        public partial class DottedLine : ScreenWideBand
        {
            public const float DOT_SPACING = 40;

            private const float dot_size = 4;

            /// <summary>
            /// How far the dots have drifted to the right, in playfield units. They drift to the left while it's negative.
            /// </summary>
            public float Drift { get; set; }

            public DottedLine()
            {
                Height = dot_size;
            }

            protected override void Update()
            {
                base.Update();

                // One more dot than fits across the screen, so that there's always one drifting in at the edge.
                int dotCount = (int)(DrawWidth / DOT_SPACING) + 2;

                if (dotCount != Count)
                {
                    Clear();

                    for (int i = 0; i < dotCount; i++)
                    {
                        Add(new FastCircle
                        {
                            Anchor = Anchor.CentreLeft,
                            Origin = Anchor.Centre,
                            Size = new Vector2(dot_size),
                        });
                    }
                }

                float offset = Drift - MathF.Floor(Drift / DOT_SPACING) * DOT_SPACING;

                for (int i = 0; i < Count; i++)
                    Children[i].X = offset + (i - 1) * DOT_SPACING;
            }
        }
    }
}
