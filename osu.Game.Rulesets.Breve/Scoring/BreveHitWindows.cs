using osu.Game.Beatmaps;
using osu.Game.Rulesets.Scoring;

namespace osu.Game.Rulesets.Breve.Scoring
{
    /// <summary>
    /// Timing windows in the spirit of Cytus, which is forgiving: they're wide, and only narrow a little at higher overall difficulties.
    /// </summary>
    /// <remarks>
    /// There's no window for misses beyond the one for bads, so a press that's too early for a bad is ignored rather than missing the note.
    /// A note is only missed by not being hit in time.
    /// </remarks>
    public class BreveHitWindows : HitWindows
    {
        public static readonly DifficultyRange PERFECT_WINDOW_RANGE = new DifficultyRange(80, 70, 55);
        public static readonly DifficultyRange GOOD_WINDOW_RANGE = new DifficultyRange(150, 130, 110);
        public static readonly DifficultyRange MEH_WINDOW_RANGE = new DifficultyRange(220, 190, 160);

        private double perfect;
        private double good;
        private double meh;

        public override bool IsHitResultAllowed(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                case HitResult.Good:
                case HitResult.Meh:
                case HitResult.Miss:
                    return true;
            }

            return false;
        }

        public override void SetDifficulty(double difficulty)
        {
            perfect = IBeatmapDifficultyInfo.DifficultyRange(difficulty, PERFECT_WINDOW_RANGE);
            good = IBeatmapDifficultyInfo.DifficultyRange(difficulty, GOOD_WINDOW_RANGE);
            meh = IBeatmapDifficultyInfo.DifficultyRange(difficulty, MEH_WINDOW_RANGE);
        }

        public override double WindowFor(HitResult result)
        {
            switch (result)
            {
                case HitResult.Perfect:
                    return perfect;

                case HitResult.Good:
                    return good;

                case HitResult.Meh:
                case HitResult.Miss:
                    return meh;

                // Results that aren't used get an empty window rather than an exception, as difficulty calculation asks every ruleset for its Great window.
                default:
                    return 0;
            }
        }
    }
}
