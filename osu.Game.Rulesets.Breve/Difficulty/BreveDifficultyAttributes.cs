using Newtonsoft.Json;
using osu.Game.Rulesets.Difficulty;

namespace osu.Game.Rulesets.Breve.Difficulty
{
    public class BreveDifficultyAttributes : DifficultyAttributes
    {
        /// <summary>
        /// How hard the beatmap's notes are to keep up with.
        /// </summary>
        [JsonProperty("tapping_difficulty")]
        public double TappingDifficulty { get; set; }

        /// <summary>
        /// How hard the beatmap's notes are to reach.
        /// </summary>
        [JsonProperty("movement_difficulty")]
        public double MovementDifficulty { get; set; }
    }
}
