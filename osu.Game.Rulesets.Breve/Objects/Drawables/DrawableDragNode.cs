using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osuTK;

namespace osu.Game.Rulesets.Breve.Objects.Drawables
{
    public partial class DrawableDragNode : DrawableBreveHitObject
    {
        /// <summary>
        /// How far from a node, compared to a tap's radius, a held tap still counts. It is generous, as chains are followed rather than aimed at.
        /// </summary>
        private const float hit_area_scale = 1.1f;

        /// <summary>
        /// The proportion of the preempt time over which the node fades in.
        /// </summary>
        private const double fade_in_proportion = 0.4;

        private const float link_alpha = 0.5f;

        public new DragNode HitObject => (DragNode)base.HitObject;

        private Box link = null!;
        private Circle circle = null!;
        private CircularContainer follower = null!;
        private Colour4 chainColour;

        public override bool HandlePositionalInput => true;

        public DrawableDragNode()
            : this(null)
        {
        }

        public DrawableDragNode(DragNode? node = null)
            : base(node)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal(new Drawable[]
            {
                link = new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.CentreLeft,
                    Height = 3,
                },
                circle = new Circle { RelativeSizeAxes = Axes.Both },
                // Slides along the link to this node while the scan line is between the previous node and this one,
                // lit up while the chain is being followed.
                follower = new CircularContainer
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Masking = true,
                    BorderThickness = 4,
                    BorderColour = Colour4.White,
                    Child = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Alpha = 0.4f,
                    },
                },
            });
        }

        protected override void OnApply()
        {
            base.OnApply();

            follower.Size = new Vector2(2 * BreveHitObject.OBJECT_RADIUS * HitObject.Scale * DragNode.HEAD_PROPORTION);
            follower.Alpha = 0;

            // Each node draws the link back to the previous one, so that links appear and disappear with the nodes they lead to.
            if (HitObject.PreviousNode is DragNode previous)
            {
                Vector2 offset = previous.Position - HitObject.Position;

                link.Alpha = link_alpha;
                link.Width = offset.Length;
                link.Rotation = MathHelper.RadiansToDegrees(MathF.Atan2(offset.Y, offset.X));
            }
            else
                link.Alpha = 0;
        }

        protected override void Update()
        {
            base.Update();

            if (HitObject.PreviousNode is not DragNode previous)
                return;

            if (Judged || Time.Current < previous.StartTime || Time.Current >= HitObject.StartTime)
            {
                follower.Alpha = 0;
                link.Alpha = link_alpha;
                return;
            }

            // The chain is being followed while either end of this link is held, as a glissando moves from one lane to the next.
            bool following = IsHeld || IsLaneHeld(previous.Lane);
            float progress = (float)((Time.Current - previous.StartTime) / (HitObject.StartTime - previous.StartTime));

            follower.Position = (previous.Position - HitObject.Position) * (1 - progress);
            follower.Alpha = 1;
            follower.Scale = new Vector2(following ? 1.25f : 1);
            follower.Colour = following ? Colour4.White : chainColour.Opacity(0.6f);
            link.Alpha = following ? 1 : link_alpha;
        }

        protected override void ApplyColour(Colour4 colour)
        {
            chainColour = colour;
            circle.Colour = colour;
            link.Colour = colour;
        }

        protected override void UpdateInitialTransforms()
        {
            base.UpdateInitialTransforms();

            this.FadeInFromZero(HitObject.TimePreempt * fade_in_proportion);
        }

        protected override void UpdateHitStateTransforms(ArmedState state)
        {
            base.UpdateHitStateTransforms(state);

            switch (state)
            {
                case ArmedState.Hit:
                    this.ScaleTo(1.6f, 150, Easing.OutQuint).FadeOut(150).Expire();
                    break;

                case ArmedState.Miss:
                    this.FadeColour(Colour4.Red, 100).FadeOut(250).Expire();
                    break;
            }
        }

        protected override void CheckForResult(bool userTriggered, double timeOffset)
        {
            // There's no press to time, so a node is hit as soon as it's held while the scan line is close enough to it.
            if (timeOffset >= -HitObject.HitWindows.WindowFor(HitResult.Perfect) && IsHeld)
                ApplyMaxResult();
            else if (!HitObject.HitWindows.CanBeHit(timeOffset))
                ApplyMinResult();
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            => Vector2.Distance(ToLocalSpace(screenSpacePos), DrawSize / 2) <= BreveHitObject.OBJECT_RADIUS * HitObject.Scale * hit_area_scale;
    }
}
