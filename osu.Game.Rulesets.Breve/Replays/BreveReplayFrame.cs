using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Replays.Legacy;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.Replays.Types;
using osuTK;

namespace osu.Game.Rulesets.Breve.Replays
{
    public class BreveReplayFrame : ReplayFrame, IConvertibleReplayFrame
    {
        /// <summary>
        /// Lane keys don't fit in the legacy replay format's mouse buttons, so they are stored as extra button flags above <see cref="ReplayButtonState.Smoke"/>.
        /// </summary>
        private const int lane_flags_offset = 5;

        public Vector2 Position;
        public List<BreveAction> Actions = new List<BreveAction>();

        public BreveReplayFrame()
        {
        }

        public BreveReplayFrame(double time, Vector2 position, params BreveAction[] actions)
            : base(time)
        {
            Position = position;
            Actions.AddRange(actions);
        }

        public void FromLegacy(LegacyReplayFrame currentFrame, IBeatmap beatmap, ReplayFrame? lastFrame = null)
        {
            Position = currentFrame.Position;
            if (currentFrame.MouseLeft) Actions.Add(BreveAction.Tap1);
            if (currentFrame.MouseRight) Actions.Add(BreveAction.Tap2);

            int laneFlags = (int)currentFrame.ButtonState >> lane_flags_offset;

            for (int lane = 0; lane < BrevePlayfield.LANE_COUNT; lane++)
            {
                if ((laneFlags & (1 << lane)) != 0)
                    Actions.Add(BreveAction.Lane1 + lane);
            }
        }

        public LegacyReplayFrame ToLegacy(IBeatmap beatmap)
        {
            ReplayButtonState state = ReplayButtonState.None;

            foreach (var action in Actions)
            {
                switch (action)
                {
                    case BreveAction.Tap1:
                        state |= ReplayButtonState.Left1;
                        break;

                    case BreveAction.Tap2:
                        state |= ReplayButtonState.Right1;
                        break;

                    default:
                        state |= (ReplayButtonState)(1 << (lane_flags_offset + (action - BreveAction.Lane1)));
                        break;
                }
            }

            return new LegacyReplayFrame(Time, Position.X, Position.Y, state);
        }

        public override bool IsEquivalentTo(ReplayFrame other)
            => other is BreveReplayFrame frame && Time == frame.Time && Position == frame.Position && Actions.SequenceEqual(frame.Actions);
    }
}
