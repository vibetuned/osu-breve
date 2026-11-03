using osu.Framework.Localisation;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Breve.Mods
{
    /// <summary>
    /// Shows dotted lines across the screen where the scan line turns around (see <see cref="BorderLines"/>), as Cytus II can.
    /// </summary>
    /// <remarks>
    /// It only helps to read the playfield, and changes neither the notes nor the score, so it's with the other visual mods rather than the difficulty reductions.
    /// </remarks>
    public class BreveModBorderLines : Mod, IApplicableToDrawableRuleset<BreveHitObject>
    {
        public override string Name => "Border Lines";

        public override string Acronym => "BL";

        public override LocalisableString Description => "Show where the scan line turns around.";

        public override ModType Type => ModType.Fun;

        public void ApplyToDrawableRuleset(DrawableRuleset<BreveHitObject> drawableRuleset) => ((BrevePlayfield)drawableRuleset.Playfield).ShowBorderLines = true;
    }
}
