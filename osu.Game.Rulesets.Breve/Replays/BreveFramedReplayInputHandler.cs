using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.StateChanges;
using osu.Framework.Utils;
using osu.Game.Replays;
using osu.Game.Rulesets.Replays;

namespace osu.Game.Rulesets.Breve.Replays
{
    public class BreveFramedReplayInputHandler : FramedReplayInputHandler<BreveReplayFrame>
    {
        public BreveFramedReplayInputHandler(Replay replay)
            : base(replay)
        {
        }

        protected override bool IsImportant(BreveReplayFrame frame) => frame.Actions.Any();

        protected override void CollectReplayInputs(List<IInput> inputs)
        {
            var position = Interpolation.ValueAt(CurrentTime, StartFrame.Position, EndFrame.Position, StartFrame.Time, EndFrame.Time);

            inputs.Add(new MousePositionAbsoluteInput { Position = GamefieldToScreenSpace(position) });
            inputs.Add(new ReplayState<BreveAction> { PressedActions = CurrentFrame?.Actions ?? new List<BreveAction>() });
        }
    }
}
