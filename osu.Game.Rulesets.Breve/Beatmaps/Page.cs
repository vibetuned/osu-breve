namespace osu.Game.Rulesets.Breve.Beatmaps
{
    /// <summary>
    /// One sweep of the scan line across the playfield.
    /// </summary>
    /// <param name="Index">The page's position in the beatmap. Page 0 starts at the first timing point, and earlier pages have negative indices.</param>
    /// <param name="StartTime">The time at which the sweep starts.</param>
    /// <param name="Duration">How long the sweep lasts.</param>
    public readonly record struct Page(int Index, double StartTime, double Duration)
    {
        public double EndTime => StartTime + Duration;

        /// <summary>
        /// Whether the scan line moves upwards during this page. Pages alternate direction, and page 0 moves upwards.
        /// </summary>
        public bool MovesUpwards => Index % 2 == 0;

        /// <summary>
        /// How far through this page <paramref name="time"/> is, from 0 at its start to 1 at its end.
        /// </summary>
        public double ProgressAt(double time) => (time - StartTime) / Duration;
    }
}
