using System;
using osu.Game.Rulesets.Breve.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Rulesets.Breve.Difficulty.Skills
{
    /// <summary>
    /// How hard a beatmap's notes are to reach: how far across the lanes, and how quickly, the hands have to move from note to note.
    /// </summary>
    public class Movement : StrainDecaySkill
    {
        /// <summary>
        /// The shortest time a move is measured over, so that notes very close together don't count as impossibly fast moves.
        /// </summary>
        private const double min_movement_time = 50;

        protected override double SkillMultiplier => 1;

        protected override double StrainDecayBase => 0.15;

        public Movement(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var note = (BreveDifficultyHitObject)current;

            // A double's second note is reached at the same time as the first, by the other hand.
            if (note.IsDoublePartner)
                return 0;

            // In lanes per second.
            return note.LaneDistance / Math.Max(note.MovementTime, min_movement_time) * 1000;
        }
    }
}
