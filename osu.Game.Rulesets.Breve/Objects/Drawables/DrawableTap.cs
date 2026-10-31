using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Breve.Objects.Drawables
{
    public partial class DrawableTap : DrawableBreveHitObject, IKeyBindingHandler<BreveAction>
    {
        /// <summary>
        /// How much larger than the note the area that accepts taps is.
        /// </summary>
        private const float hit_area_scale = 1.25f;

        /// <summary>
        /// The proportion of the preempt time over which the note fades in.
        /// </summary>
        private const double fade_in_proportion = 0.4;

        private Circle fill = null!;
        private CircularContainer ring = null!;

        public override bool HandlePositionalInput => true;

        public DrawableTap()
            : this(null)
        {
        }

        public DrawableTap(Tap? tap = null)
            : base(tap)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal(new Drawable[]
            {
                fill = new Circle
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0.6f,
                },
                ring = new CircularContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Masking = true,
                    BorderThickness = 5,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0,
                        AlwaysPresent = true,
                    },
                },
            });
        }

        protected override void ApplyColour(Colour4 colour)
        {
            fill.Colour = colour;
            ring.BorderColour = colour;
        }

        protected override void UpdateInitialTransforms()
        {
            base.UpdateInitialTransforms();

            this.FadeInFromZero(HitObject.TimePreempt * fade_in_proportion);

            // The note fills up as the scan line approaches, and is full when it arrives.
            fill.ScaleTo(0).Then().ScaleTo(1, HitObject.TimePreempt);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            base.UpdateHitStateTransforms(state);

            switch (state)
            {
                case ArmedState.Hit:
                    this.ScaleTo(1.4f, 250, Easing.OutQuint).FadeOut(250).Expire();
                    break;

                case ArmedState.Miss:
                    this.FadeColour(Colour4.Red, 100).FadeOut(250).Expire();
                    break;
            }
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            if (!userTriggered)
            {
                if (!HitObject.HitWindows.CanBeHit(timeOffset))
                    ApplyMinResult();

                return;
            }

            HitResult result = HitObject.HitWindows.ResultFor(timeOffset);

            if (result == HitResult.None)
                return;

            ApplyResult(result);
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            => Vector2.Distance(ToLocalSpace(screenSpacePos), DrawSize / 2) <= DrawWidth / 2 * hit_area_scale;

        public bool OnPressed(KeyBindingPressEvent<BreveAction> e) => IsAimedAt(e.Action) && UpdateResult(true);

        public void OnReleased(KeyBindingReleaseEvent<BreveAction> e)
        {
        }
    }
}
