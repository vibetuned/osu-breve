using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Input.Events;
using osuTK;

namespace osu.Game.Rulesets.Breve.UI
{
    /// <summary>
    /// Turns touches into lane presses, so that each finger plays like a lane key:
    /// it presses the lane it's over, keeps it held while it stays there, and moves the press along as it slides across lanes, like a glissando.
    /// </summary>
    /// <remarks>
    /// Lane presses are what keyboard play uses too, so holds, drag chains and doubles all work with several fingers, and touch plays are recorded in replays.
    /// </remarks>
    public partial class TouchLaneMapper : Drawable
    {
        private readonly Dictionary<TouchSource, BreveAction> touchedLanes = new Dictionary<TouchSource, BreveAction>();

        private BreveInputManager? inputManager;

        public TouchLaneMapper()
        {
            RelativeSizeAxes = Axes.Both;
        }

        // Touches outside the playfield count too, so that the outer lanes are easy to reach.
        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => true;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            inputManager = GetContainingInputManager() as BreveInputManager;
        }

        protected override bool OnTouchDown(TouchDownEvent e)
        {
            press(e.Touch.Source, laneActionAt(e.ScreenSpaceTouchDownPosition));
            return true;
        }

        protected override void OnTouchMove(TouchMoveEvent e)
        {
            base.OnTouchMove(e);

            if (!touchedLanes.TryGetValue(e.Touch.Source, out var currentAction))
                return;

            var action = laneActionAt(e.ScreenSpaceTouch.Position);

            if (action == currentAction)
                return;

            release(e.Touch.Source);
            press(e.Touch.Source, action);
        }

        protected override void OnTouchUp(TouchUpEvent e)
        {
            release(e.Touch.Source);
            base.OnTouchUp(e);
        }

        private BreveAction laneActionAt(Vector2 screenSpacePosition)
        {
            // This fills the playfield, so its local space is in playfield units.
            float x = ToLocalSpace(screenSpacePosition).X;
            int lane = (int)Math.Round((x - BrevePlayfield.HORIZONTAL_MARGIN) / BrevePlayfield.LaneWidth);

            return BreveAction.Lane1 + Math.Clamp(lane, 0, BrevePlayfield.LANE_COUNT - 1);
        }

        private void press(TouchSource source, BreveAction action)
        {
            // A lane touched by more than one finger is only pressed once, and stays pressed until the last of them leaves it.
            bool alreadyPressed = touchedLanes.ContainsValue(action);
            touchedLanes[source] = action;

            if (!alreadyPressed)
                inputManager?.KeyBindingContainer.TriggerPressed(action);
        }

        private void release(TouchSource source)
        {
            if (!touchedLanes.Remove(source, out var action))
                return;

            if (!touchedLanes.ContainsValue(action))
                inputManager?.KeyBindingContainer.TriggerReleased(action);
        }
    }
}
