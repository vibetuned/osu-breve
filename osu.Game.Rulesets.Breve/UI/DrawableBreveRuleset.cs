using System.Collections.Generic;
using osu.Framework.Input;
using osu.Game.Beatmaps;
using osu.Game.Input.Handlers;
using osu.Game.Replays;
using osu.Game.Rulesets.Breve.Beatmaps;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Breve.Replays;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Breve.UI
{
    public partial class DrawableBreveRuleset : DrawableRuleset<BreveHitObject>
    {
        public DrawableBreveRuleset(BreveRuleset ruleset, IBeatmap beatmap, IReadOnlyList<Mod>? mods = null)
            : base(ruleset, beatmap, mods)
        {
        }

        public override PlayfieldAdjustmentContainer CreatePlayfieldAdjustmentContainer() => new BrevePlayfieldAdjustmentContainer();

        protected override Playfield CreatePlayfield() => new BrevePlayfield(new PageTiming(Beatmap.ControlPointInfo));

        protected override ReplayInputHandler CreateReplayInputHandler(Replay replay) => new BreveFramedReplayInputHandler(replay);

        // Hit objects are pooled by the playfield instead.
        public override DrawableHitObject<BreveHitObject>? CreateDrawableRepresentation(BreveHitObject h) => null;

        protected override PassThroughInputManager CreateInputManager() => new BreveInputManager(Ruleset.RulesetInfo);
    }
}
