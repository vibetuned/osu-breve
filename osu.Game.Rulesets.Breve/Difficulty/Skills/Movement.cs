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

        /// <summary>
        /// How long the quick move across a flick, one lane over, is taken to last.
        /// </summary>
        private const double flick_movement_time = 150;

        protected override double SkillMultiplier => 1;

        protected override double StrainDecayBase => 0.15;

        public Movement(Mod[] mods)
            : base(mods)
        {
        }

        protected override double StrainValueOf(DifficultyHitObject current)
        {
            var note = (BreveDifficultyHitObject)current;

            // In lanes per second. A flick is a move of its own, across it, on top of the move to reach it.
            double flickSpeed = note.IsFlick ? 1 / flick_movement_time * 1000 : 0;

            // A double's second note is reached at the same time as the first, by the other hand, though a flick still has to be flicked.
            if (note.IsDoublePartner)
                return flickSpeed;

            return note.LaneDistance / Math.Max(note.MovementTime, min_movement_time) * 1000 + flickSpeed;
        }
    }
}
