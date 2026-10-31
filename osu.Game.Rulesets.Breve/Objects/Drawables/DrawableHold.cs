using System;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Game.Audio;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Scoring;
using osu.Game.Skinning;
using osuTK;

namespace osu.Game.Rulesets.Breve.Objects.Drawables
{
    public partial class DrawableHold : DrawableBreveHitObject, IKeyBindingHandler<BreveAction>
    {
        /// <summary>
        /// How close to its end a hold can be let go of and still count as completed.
        /// </summary>
        private const double release_leniency = 150;

        /// <summary>
        /// How much larger than the note the area that accepts taps is.
        /// </summary>
        private const float hit_area_scale = 1.25f;

        /// <summary>
        /// The proportion of the preempt time over which the note fades in.
        /// </summary>
        private const double fade_in_proportion = 0.4;

        /// <summary>
        /// How wide the body is compared to the head.
        /// </summary>
        private const float body_width_proportion = 0.55f;

        private const float body_alpha = 0.5f;

        /// <summary>
        /// How opaque the body is while the hold is held, so that holding it lights it up.
        /// </summary>
        private const float held_body_alpha = 0.85f;

        public new Hold HitObject => (Hold)base.HitObject;

        /// <summary>
        /// The result of the press that started the hold, or null if it hasn't been started.
        /// It becomes the hold's result if the hold is kept until the end.
        /// </summary>
        private HitResult? pressResult;

        private Container<Box> bodyParts = null!;
        private Circle endCap = null!;
        private Circle fill = null!;
        private CircularContainer ring = null!;
        private CircularContainer holdingRing = null!;
        private CircularProgress progressRing = null!;
        private PausableSkinnableSound pressSamples = null!;

        public override bool HandlePositionalInput => true;

        public DrawableHold()
            : this(null)
        {
        }

        public DrawableHold(Hold? hold = null)
            : base(hold)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            AddRangeInternal(new Drawable[]
            {
                // Positioned relative to the head's centre.
                bodyParts = new Container<Box>
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                endCap = new Circle
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                },
                fill = new Circle
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0.6f,
                },
                ring = createRing(5),
                // A white ring and dot in the middle of the head, like a target, which taps don't have.
                createRing(3).With(r =>
                {
                    r.Anchor = Anchor.Centre;
                    r.Origin = Anchor.Centre;
                    r.Size = new Vector2(0.6f);
                    r.BorderColour = Colour4.White;
                }),
                new Circle
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Size = new Vector2(0.25f),
                },
                // While the hold is held, a ring around the head fills up as it goes, and is full when the hold ends.
                holdingRing = createRing(3).With(r =>
                {
                    r.Anchor = Anchor.Centre;
                    r.Origin = Anchor.Centre;
                    r.Scale = new Vector2(1.3f);
                    r.Alpha = 0;
                }),
                progressRing = new CircularProgress
                {
                    RelativeSizeAxes = Axes.Both,
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Scale = new Vector2(1.3f),
                    InnerRadius = 0.12f,
                    Alpha = 0,
                },
                pressSamples = new PausableSkinnableSound(),
            });
        }

        protected override void OnApply()
        {
            base.OnApply();

            pressResult = null;
            pressSamples.Samples = HitObject.PressSamples.Cast<ISampleInfo>().ToArray();

            // The body has a part for each sweep of the hold, covering where the scan line travels on it, and a cap where the hold ends.
            float bodyWidth = HitObject.Diameter * body_width_proportion;

            bodyParts.Clear();

            foreach (var _ in HitObject.Segments)
            {
                bodyParts.Add(new Box
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.TopCentre,
                    Width = bodyWidth,
                });
            }

            endCap.Size = new Vector2(bodyWidth * 1.6f);
            endCap.Y = HitObject.Segments[^1].ToY - HitObject.Y;
        }

        protected override void OnFree()
        {
            base.OnFree();

            pressSamples.ClearSamples();
        }

        public override void StopAllSamples()
        {
            base.StopAllSamples();

            pressSamples.Stop();
        }

        protected override void Update()
        {
            base.Update();

            bool holding = pressResult != null && !Judged;

            holdingRing.Alpha = holding ? 1 : 0;
            progressRing.Alpha = holding ? 1 : 0;
            progressRing.Progress = Math.Clamp((Time.Current - HitObject.StartTime) / HitObject.Duration, 0, 1);

            for (int i = 0; i < bodyParts.Count; i++)
            {
                var segment = HitObject.Segments[i];
                var part = bodyParts[i];

                // Each part of the body shows up once its sweep is the next one, and takes that sweep's colour.
                double sweepDuration = segment.PageEndTime - segment.PageStartTime;
                bool visible = Time.Current >= segment.PageStartTime - sweepDuration && Time.Current < segment.PageEndTime;

                // The stretch the scan line has already passed is used up, so the body shrinks towards where the hold ends.
                double partStartTime = Math.Max(HitObject.StartTime, segment.PageStartTime);
                double partEndTime = Math.Min(HitObject.EndTime, segment.PageEndTime);
                float swept = partEndTime > partStartTime ? (float)Math.Clamp((Time.Current - partStartTime) / (partEndTime - partStartTime), 0, 1) : 1;
                float fromY = segment.FromY + (segment.ToY - segment.FromY) * swept;

                part.Y = Math.Min(fromY, segment.ToY) - HitObject.Y;
                part.Height = Math.Abs(segment.ToY - fromY);
                part.Alpha = visible ? holding ? held_body_alpha : body_alpha : 0;
                part.Colour = ColourFor(segment.OnUpwardPage, segment.PageStartTime);

                if (i == bodyParts.Count - 1)
                {
                    endCap.Alpha = visible ? 1 : 0;
                    endCap.Colour = part.Colour;
                }
            }
        }

        protected override void ApplyColour(Colour4 colour)
        {
            fill.Colour = colour;
            ring.BorderColour = colour;
            holdingRing.BorderColour = colour;
            progressRing.Colour = colour;
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
            double pressOffset = Time.Current - HitObject.StartTime;

            if (pressResult == null)
            {
                if (!userTriggered)
                {
                    if (!HitObject.HitWindows.CanBeHit(pressOffset))
                        ApplyMinResult();

                    return;
                }

                HitResult result = HitObject.HitWindows.ResultFor(pressOffset);

                if (result == HitResult.None)
                    return;

                if (result == HitResult.Miss)
                {
                    ApplyMinResult();
                    return;
                }

                pressResult = result;
                pressSamples.Play();
                return;
            }

            // Once started, the hold takes the result of the press that started it if it is kept until the end, and misses if it is let go of too early.
            if (timeOffset >= 0)
                ApplyResult(pressResult.Value);
            else if (!IsHeld)
                ApplyResult(timeOffset >= -release_leniency ? pressResult.Value : HitResult.Miss);
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            => Vector2.Distance(ToLocalSpace(screenSpacePos), DrawSize / 2) <= DrawWidth / 2 * hit_area_scale;

        public bool OnPressed(KeyBindingPressEvent<BreveAction> e)
        {
            if (pressResult != null || !IsAimedAt(e.Action))
                return false;

            UpdateResult(true);

            // The press is used up if it started the hold, or missed it by coming too early.
            return pressResult != null || Judged;
        }

        public void OnReleased(KeyBindingReleaseEvent<BreveAction> e)
        {
        }

        private static CircularContainer createRing(float thickness) => new CircularContainer
        {
            RelativeSizeAxes = Axes.Both,
            Masking = true,
            BorderThickness = thickness,
            Child = new Box
            {
                RelativeSizeAxes = Axes.Both,
                Alpha = 0,
                AlwaysPresent = true,
            },
        };
    }
}
