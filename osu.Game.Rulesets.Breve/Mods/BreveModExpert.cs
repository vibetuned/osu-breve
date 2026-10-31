using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Beatmaps;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Breve.Mods
{
    /// <summary>
    /// Plays the expert chart, which zigzags across the playfield for every short slider (see <see cref="BreveBeatmapConverter.Expert"/>).
    /// </summary>
    public class BreveModExpert : Mod, IApplicableToBeatmapConverter
    {
        public override string Name => "Expert";

        public override string Acronym => "EX";

        public override LocalisableString Description => "Zigzag across the playfield for every short slider.";

        public override ModType Type => ModType.DifficultyIncrease;

        public void ApplyToBeatmapConverter(IBeatmapConverter beatmapConverter) => ((BreveBeatmapConverter)beatmapConverter).Expert = true;
    }
}
