using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Breve.Objects.Drawables
{
    /// <summary>
    /// A flick: a diamond made of two triangles, pointing to either side, on the boundary between two lanes.
    /// </summary>
    public partial class DrawableFlick : DrawableBreveHitObject, IKeyBindingHandler<BreveAction>
    {
        /// <summary>
        /// How recently the other lane has to have been in use for a press in one of the flick's lanes to count as moving across it.
        /// </summary>
        private const double lane_change_window = 200;

        /// <summary>
        /// How much larger than the flick the area that accepts clicks is.
        /// </summary>
        private const float hit_area_scale = 1.25f;

        /// <summary>
        /// The proportion of the preempt time over which the flick fades in.
        /// </summary>
        private const double fade_in_proportion = 0.4;

        /// <summary>
        /// The gap between the two halves, in playfield units.
        /// </summary>
        private const float gap = 3;

        private const float body_alpha = 0.4f;
        private const float fill_alpha = 0.9f;

        public new Flick HitObject => (Flick)base.HitObject;

        [Resolved]
        private LaneActivity laneActivity { get; set; } = null!;

        private Container leftHalf = null!;
        private Container rightHalf = null!;
        private Triangle leftFill = null!;
        private Triangle rightFill = null!;

        /// <summary>
        /// The mouse button a click on the flick was made with, while it's held, and where across the playfield the cursor was then.
        /// </summary>
        private (BreveAction action, float x)? click;

        public override bool HandlePositionalInput => true;

        public DrawableFlick()
            : this(null)
        {
        }

        public DrawableFlick(Flick? flick = null)
            : base(flick)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Each half is a triangle pointing out to its side from the middle, with a fill that grows out from the middle as the scan line approaches.
            AddRangeInternal(new Drawable[]
            {
                leftHalf = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        createTriangle(-1, body_alpha).With(t => t.EdgeSmoothness = Vector2.One),
                        leftFill = createTriangle(-1, fill_alpha),
                    },
                },
                rightHalf = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        createTriangle(1, body_alpha).With(t => t.EdgeSmoothness = Vector2.One),
                        rightFill = createTriangle(1, fill_alpha),
                    },
                },
            });
        }

        /// <remarks>
        /// Only the triangles that are never scaled have their edges smoothed: the framework smooths edges over an area that grows as a triangle is scaled down,
        /// which would turn the growing fills into blurs far larger than the flick.
        /// </remarks>
        private static Triangle createTriangle(int side, float alpha) => new Triangle
        {
            // Triangles point up, from the middle of their bottom edge, so this one is turned to point to its side, from the middle of the flick.
            RelativeSizeAxes = Axes.Both,
            Size = new Vector2(Flick.HEIGHT_PROPORTION, 0.5f),
            Anchor = Anchor.Centre,
            Origin = Anchor.BottomCentre,
            X = side * gap / 2,
            Rotation = side * 90,
            Alpha = alpha,
        };

        protected override void OnApply()
        {
            base.OnApply();

            click = null;
        }

        protected override void ApplyColour(Colour4 colour)
        {
            leftHalf.Colour = colour;
            rightHalf.Colour = colour;
        }

        protected override void UpdateInitialTransforms()
        {
            base.UpdateInitialTransforms();

            this.FadeInFromZero(HitObject.TimePreempt * fade_in_proportion);

            // The flick fills up as the scan line approaches, and is full when it arrives.
            leftFill.ScaleTo(0).Then().ScaleTo(1, HitObject.TimePreempt);
            rightFill.ScaleTo(0).Then().ScaleTo(1, HitObject.TimePreempt);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            base.UpdateHitStateTransforms(state);

            switch (state)
            {
                case ArmedState.Hit:
                    // The halves fly apart, as if flicked away.
                    leftHalf.MoveToX(-DrawWidth / 4, 250, Easing.OutQuint);
                    rightHalf.MoveToX(DrawWidth / 4, 250, Easing.OutQuint);
                    this.FadeOut(250).Expire();
                    break;

                case ArmedState.Miss:
                    this.FadeColour(Colour4.Red, 100).FadeOut(250).Expire();
                    break;
            }
        }

        protected override void Update()
        {
            base.Update();

            if (click == null || Judged)
                return;

            if (!IsPressed(click.Value.action))
            {
                click = null;
                return;
            }

            // With the mouse, a flick is clicked and dragged across, at least half a lane to either side.
            if (Math.Abs(cursorX - click.Value.x) >= BrevePlayfield.LaneWidth / 2)
            {
                click = null;
                UpdateResult(true);
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
        {
            // Inside the diamond the two halves make up.
            Vector2 fromCentre = ToLocalSpace(screenSpacePos) - DrawSize / 2;
            return Math.Abs(fromCentre.X) / (DrawWidth / 2) + Math.Abs(fromCentre.Y) / (DrawWidth * Flick.HEIGHT_PROPORTION / 2) <= hit_area_scale;
        }

        public bool OnPressed(KeyBindingPressEvent<BreveAction> e)
        {
            if (e.Action is BreveAction.Tap1 or BreveAction.Tap2)
                return startClick(e.Action);

            int lane = e.Action - BreveAction.Lane1;

            if (lane != HitObject.Lane && lane != HitObject.Lane + 1)
                return false;

            // Moving across the flick presses one of its lanes just after the other was in use.
            int otherLane = lane == HitObject.Lane ? HitObject.Lane + 1 : HitObject.Lane;

            if (Time.Current - laneActivity.LastUsed(otherLane) > lane_change_window)
                return false;

            return UpdateResult(true);
        }

        public void OnReleased(KeyBindingReleaseEvent<BreveAction> e)
        {
        }

        private bool startClick(BreveAction action)
        {
            // A click too early to hit the flick goes through to whatever else is under the cursor.
            if (!IsHovered || Time.Current < HitObject.StartTime - HitObject.HitWindows.WindowFor(HitResult.Miss))
                return false;

            click = (action, cursorX);
            return true;
        }

        /// <summary>
        /// Where across the playfield the cursor is.
        /// </summary>
        private float cursorX
        {
            get
            {
                var inputManager = GetContainingInputManager();
                return inputManager == null || Parent == null ? 0 : Parent.ToLocalSpace(inputManager.CurrentState.Mouse.Position).X;
            }
        }
    }
}
