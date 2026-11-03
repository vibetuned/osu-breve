using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Input;

namespace osu.Game.Rulesets.Breve.UI
{
    /// <summary>
    /// A horizontal band that reaches out to the left and right edges of the screen, rather than only across the playfield.
    /// It stays in the playfield's space, so that its <see cref="Drawable.Y"/> lines up exactly with the notes.
    /// </summary>
    public partial class ScreenWideBand : Container
    {
        private InputManager? inputManager;

        public ScreenWideBand()
        {
            Origin = Anchor.CentreLeft;
        }

        protected override void Update()
        {
            base.Update();

            // The ruleset's input manager covers the whole screen.
            inputManager ??= GetContainingInputManager();

            if (inputManager == null || Parent == null)
                return;

            var screen = inputManager.ScreenSpaceDrawQuad;
            float left = Parent.ToLocalSpace(screen.TopLeft).X;
            float right = Parent.ToLocalSpace(screen.TopRight).X;

            X = left;
            Width = right - left;
        }
    }
}
