using System;
using osu.Framework.Graphics;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// Runs an action on the first frame a condition holds, such as the gameplay clock reaching a time.
    /// Test steps run too far apart to press notes within their hit windows, so this checks every frame instead.
    /// </summary>
    public partial class TimedAction : Drawable
    {
        private readonly Func<bool> due;
        private readonly Action action;

        public TimedAction(Func<bool> due, Action action)
        {
            this.due = due;
            this.action = action;
        }

        protected override void Update()
        {
            base.Update();

            if (!due())
                return;

            action();
            Expire();
        }
    }
}
