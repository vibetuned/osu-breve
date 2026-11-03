using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;

namespace osu.Game.Rulesets.Breve.UI
{
    /// <summary>
    /// Keeps track of when each lane was last in use, for notes played by moving from one lane to another (flicks).
    /// </summary>
    /// <remarks>
    /// It's in front of the notes, so it sees every lane press before they do, and it never takes a press,
    /// so that it also knows about the presses notes take. Every handler sees releases.
    /// </remarks>
    public partial class LaneActivity : Component, IKeyBindingHandler<BreveAction>
    {
        private readonly bool[] held = new bool[BrevePlayfield.LANE_COUNT];
        private readonly double[] lastReleased = Enumerable.Repeat(double.NegativeInfinity, BrevePlayfield.LANE_COUNT).ToArray();

        /// <summary>
        /// The last time a lane was in use: now while its key is held or a finger is on it, and otherwise when it was let go of.
        /// </summary>
        public double LastUsed(int lane) => held[lane] ? Time.Current : lastReleased[lane];

        public bool OnPressed(KeyBindingPressEvent<BreveAction> e)
        {
            if (laneOf(e.Action) is int lane)
                held[lane] = true;

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<BreveAction> e)
        {
            if (laneOf(e.Action) is not int lane)
                return;

            held[lane] = false;
            lastReleased[lane] = Time.Current;
        }

        private static int? laneOf(BreveAction action) => action >= BreveAction.Lane1 ? action - BreveAction.Lane1 : null;
    }
}
