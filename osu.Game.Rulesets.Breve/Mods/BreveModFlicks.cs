using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Beatmaps;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Breve.Mods
{
    /// <summary>
    /// Turns short sliders into flicks, which are hit by moving quickly from one lane to the next (see <see cref="BreveBeatmapConverter.Flicks"/>).
    /// </summary>
    public class BreveModFlicks : Mod, IApplicableToBeatmapConverter
    {
        public override string Name => "Flicks";

        public override string Acronym => "FK";

        public override LocalisableString Description => "Short sliders become flicks: move quickly across them, from one lane to the next.";

        public override ModType Type => ModType.DifficultyIncrease;

        public void ApplyToBeatmapConverter(IBeatmapConverter beatmapConverter) => ((BreveBeatmapConverter)beatmapConverter).Flicks = true;
    }
}
