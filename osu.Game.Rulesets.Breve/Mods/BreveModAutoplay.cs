using System.Collections.Generic;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Replays;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Breve.Mods
{
    public class BreveModAutoplay : ModAutoplay
    {
        public override ModReplayData CreateReplayData(IBeatmap beatmap, IReadOnlyList<Mod> mods)
            => new ModReplayData(new BreveAutoGenerator(beatmap).Generate(), new ModCreatedUser { Username = "Autoplay" });
    }
}
