using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Difficulty.Preprocessing;
using osu.Game.Rulesets.Breve.Difficulty.Skills;
using osu.Game.Rulesets.Breve.Mods;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Difficulty.Skills;
using osu.Game.Rulesets.Mods;
using osu.Game.Utils;

namespace osu.Game.Rulesets.Breve.Difficulty
{
    /// <summary>
    /// Rates beatmaps by how hard their notes are to keep up with (<see cref="Tapping"/>) and to reach (<see cref="Movement"/>).
    /// </summary>
    /// <remarks>
    /// The multipliers are calibrated against osu! on the expert chart of its test beatmap: tapping matches osu!'s speed rating, movement its aim rating,
    /// and the star rating osu!'s own, so that a converted beatmap's expert chart has roughly as many stars as it does in osu!.
    /// The normal chart, with fewer zigzags, rates lower.
    /// </remarks>
    public class BreveDifficultyCalculator : DifficultyCalculator
    {
        private const double tapping_multiplier = 0.01766;
        private const double movement_multiplier = 0.002588;
        private const double star_rating_multiplier = 1.154;

        /// <summary>
        /// The power the skills are combined with. Above 1, a beatmap that's hard in both ways is rated higher than one that's hard in only one.
        /// </summary>
        private const double skill_combination_power = 1.1;

        public override int Version => 20261031;

        public BreveDifficultyCalculator(IRulesetInfo ruleset, IWorkingBeatmap beatmap)
            : base(ruleset, beatmap)
        {
        }

        protected override DifficultyAttributes CreateDifficultyAttributes(IBeatmap beatmap, Mod[] mods, Skill[] skills)
        {
            if (beatmap.HitObjects.Count == 0)
                return new BreveDifficultyAttributes { Mods = mods };

            double tapping = skills.OfType<Tapping>().Single().DifficultyValue() * tapping_multiplier;
            double movement = skills.OfType<Movement>().Single().DifficultyValue() * movement_multiplier;
            double combined = Math.Pow(Math.Pow(tapping, skill_combination_power) + Math.Pow(movement, skill_combination_power), 1 / skill_combination_power);

            return new BreveDifficultyAttributes
            {
                StarRating = combined * star_rating_multiplier,
                TappingDifficulty = tapping,
                MovementDifficulty = movement,
                MaxCombo = beatmap.HitObjects.Count,
                Mods = mods,
            };
        }

        protected override IEnumerable<DifficultyHitObject> CreateDifficultyHitObjects(IBeatmap beatmap, Mod[] mods)
        {
            double clockRate = ModUtils.CalculateRateWithMods(mods);
            var notes = beatmap.HitObjects.Cast<BreveHitObject>().ToList();
            var objects = new List<DifficultyHitObject>(notes.Count);
            var holds = new List<Hold>();

            BreveHitObject? lastPlayed = null;

            for (int i = 1; i < notes.Count; i++)
            {
                var current = notes[i];
                var last = notes[i - 1];

                // The note before this one is only played before it if it's not this note's partner in a double.
                if (last.StartTime < current.StartTime - 1)
                    lastPlayed = last;

                if (last is Hold hold)
                    holds.Add(hold);

                holds.RemoveAll(h => h.EndTime <= current.StartTime);
                bool duringHold = holds.Any(h => h.StartTime < current.StartTime - 1);

                objects.Add(new BreveDifficultyHitObject(current, last, lastPlayed, duringHold, clockRate, objects, objects.Count));
            }

            return objects;
        }

        protected override Skill[] CreateSkills(IBeatmap beatmap, Mod[] mods) => new Skill[]
        {
            new Tapping(mods),
            new Movement(mods),
        };

        protected override Mod[] DifficultyAdjustmentMods => new Mod[]
        {
            new BreveModDoubleTime(),
            new BreveModHalfTime(),
            new BreveModExpert(),
        };
    }
}
