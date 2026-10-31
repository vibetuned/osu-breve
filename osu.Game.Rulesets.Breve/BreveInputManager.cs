using System.ComponentModel;
using osu.Framework.Input.Bindings;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Breve
{
    public partial class BreveInputManager : RulesetInputManager<BreveAction>
    {
        // Touches press the lane they're in (see TouchLaneMapper), so they mustn't also click the note under them as a mouse would.
        protected override bool MapMouseToLatestTouch => false;

        public BreveInputManager(RulesetInfo ruleset)
            : base(ruleset, 0, SimultaneousBindingMode.Unique)
        {
        }
    }

    public enum BreveAction
    {
        // Taps hit the note under the cursor.
        [Description("Tap 1")]
        Tap1,

        [Description("Tap 2")]
        Tap2,

        // Lane keys hit the next note in their lane, wherever the cursor is.
        [Description("Lane 1")]
        Lane1,

        [Description("Lane 2")]
        Lane2,

        [Description("Lane 3")]
        Lane3,

        [Description("Lane 4")]
        Lane4,

        [Description("Lane 5 (centre)")]
        Lane5,

        [Description("Lane 6")]
        Lane6,

        [Description("Lane 7")]
        Lane7,

        [Description("Lane 8")]
        Lane8,

        [Description("Lane 9")]
        Lane9,
    }
}
