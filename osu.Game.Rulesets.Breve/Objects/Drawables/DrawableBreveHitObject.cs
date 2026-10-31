using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Game.Rulesets.Objects.Drawables;
using osuTK;

namespace osu.Game.Rulesets.Breve.Objects.Drawables
{
    public abstract partial class DrawableBreveHitObject : DrawableHitObject<BreveHitObject>
    {
        // Notes take the colour of the direction the scan line moves in when it reaches them,
        // which tells apart the notes of the current sweep and the next one while both are visible.
        private static readonly Colour4 upward_colour = Colour4.FromHex("66ccff");
        private static readonly Colour4 downward_colour = Colour4.FromHex("ff88cc");

        // Notes of the next sweep are also mixed with some grey until the scan line turns around onto it.
        private static readonly Colour4 upcoming_grey = Colour4.FromHex("8a8a8a");
        private const float upcoming_grey_amount = 0.45f;
        private const double page_start_fade_duration = 150;

        private BreveInputManager? inputManager;

        // Pooled drawables are created without a hit object, which the base constructor allows despite its non-nullable annotation.
        protected DrawableBreveHitObject(BreveHitObject? hitObject)
            : base(hitObject!)
        {
            Origin = Anchor.Centre;
        }

        protected override double InitialLifetimeOffset => HitObject.TimePreempt;

        /// <summary>
        /// Whether a pressed action is aimed at this object: its lane key, or a tap with the cursor over it.
        /// </summary>
        protected bool IsAimedAt(BreveAction action) => action is BreveAction.Tap1 or BreveAction.Tap2 ? IsHovered : action == BreveAction.Lane1 + HitObject.Lane;

        /// <summary>
        /// Whether an action aimed at this object is currently held.
        /// </summary>
        protected bool IsHeld => pressedActions.Any(IsAimedAt);

        /// <summary>
        /// Whether a lane's key is currently held, or a finger is on the lane.
        /// </summary>
        protected bool IsLaneHeld(int lane) => pressedActions.Contains(BreveAction.Lane1 + lane);

        private IEnumerable<BreveAction> pressedActions
        {
            get
            {
                inputManager ??= GetContainingInputManager() as BreveInputManager;
                return inputManager?.KeyBindingContainer.PressedActions ?? Enumerable.Empty<BreveAction>();
            }
        }

        protected override void OnApply()
        {
            base.OnApply();

            Position = HitObject.Position;
            Size = new Vector2(HitObject.Diameter);
        }

        protected override void Update()
        {
            base.Update();

            ApplyColour(ColourFor(HitObject.OnUpwardPage, HitObject.PageStartTime));
        }

        /// <summary>
        /// Colours the object's parts.
        /// </summary>
        /// <param name="colour">The colour of the direction the scan line moves in when it reaches the object, greyed while that sweep is yet to come.</param>
        protected abstract void ApplyColour(Colour4 colour);

        /// <summary>
        /// Retrieves the current colour of something on a sweep: the colour of the sweep's direction, greyed while the sweep is yet to come.
        /// </summary>
        protected Colour4 ColourFor(bool onUpwardPage, double pageStartTime)
        {
            Colour4 pageColour = onUpwardPage ? upward_colour : downward_colour;
            float grey = upcoming_grey_amount * (float)Math.Clamp((pageStartTime - Time.Current) / page_start_fade_duration, 0, 1);

            return new Colour4(
                pageColour.R + (upcoming_grey.R - pageColour.R) * grey,
                pageColour.G + (upcoming_grey.G - pageColour.G) * grey,
                pageColour.B + (upcoming_grey.B - pageColour.B) * grey,
                pageColour.A);
        }
    }
}
