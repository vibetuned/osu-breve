using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input;
using osu.Game.Rulesets.Breve.Beatmaps;

namespace osu.Game.Rulesets.Breve.UI
{
    /// <summary>
    /// The line that sweeps up and down the playfield, reaching each note at the moment it should be hit.
    /// It stretches across the whole width of the screen rather than only the playfield.
    /// </summary>
    /// <remarks>
    /// Like Cytus's, it's a broad band: a solid middle, which is what reaches the notes, in a glow that fades out above and below it.
    /// </remarks>
    public partial class ScanLine : CompositeDrawable
    {
        private const float core_thickness = 6;

        private const float core_alpha = 0.5f;

        /// <summary>
        /// How far the glow reaches out on either side of the solid middle.
        /// </summary>
        private const float glow_reach = 14;

        /// <summary>
        /// How opaque the glow is next to the solid middle, from where it fades out.
        /// </summary>
        private const float glow_alpha = 0.25f;

        private readonly PageTiming pageTiming;
        private readonly Container line;

        private InputManager? inputManager;

        public ScanLine(PageTiming pageTiming)
        {
            this.pageTiming = pageTiming;

            RelativeSizeAxes = Axes.Both;
            InternalChild = line = new Container
            {
                Origin = Anchor.CentreLeft,
                Height = core_thickness + 2 * glow_reach,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Height = 0.5f,
                        Colour = ColourInfo.GradientVertical(Colour4.White.Opacity(0), Colour4.White.Opacity(glow_alpha)),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Height = 0.5f,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Colour = ColourInfo.GradientVertical(Colour4.White.Opacity(glow_alpha), Colour4.White.Opacity(0)),
                    },
                    new Box
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = core_thickness,
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Alpha = core_alpha,
                    },
                },
            };
        }

        protected override void Update()
        {
            base.Update();

            line.Y = BrevePlayfield.ScanLineY(pageTiming.PositionAt(Time.Current));

            // The line stays in the playfield's space so that it lines up exactly with the notes, but reaches out to the edges of the screen,
            // which the ruleset's input manager covers.
            inputManager ??= GetContainingInputManager();

            if (inputManager == null)
                return;

            var screen = inputManager.ScreenSpaceDrawQuad;
            float left = ToLocalSpace(screen.TopLeft).X;
            float right = ToLocalSpace(screen.TopRight).X;

            line.X = left;
            line.Width = right - left;
        }
    }
}
