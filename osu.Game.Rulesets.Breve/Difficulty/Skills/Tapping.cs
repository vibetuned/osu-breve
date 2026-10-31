using osu.Game.Rulesets.Breve.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Breve.Difficulty.Skills
{
    /// <summary>
    /// How hard a beatmap's notes are to keep up with: how densely they have to be pressed.
    /// Both notes of a double count, and notes count more while a hold is being held.
    /// </summary>
    public class Tapping : StrainDecaySkill
    {
        /// <summary>
        /// How much a drag node, which only has to be held, counts compared to a note that has to be pressed.
        /// </summary>
        private const double drag_node_weight = 0.25;

        /// <summary>
        /// How much more a note counts while a hold is being held, as there's one finger fewer to play it with.
        /// </summary>
        private const double during_hold_multiplier = 1.25;

        protected override double SkillMultiplier => 1;

        protected override double StrainDecayBase => 0.3;

        public Tapping(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var note = (BreveDifficultyHitObject)current;
            double value = note.IsPress ? 1 : drag_node_weight;

            if (note.DuringHold)
                value *= during_hold_multiplier;

            return value;
        }
    }
}
