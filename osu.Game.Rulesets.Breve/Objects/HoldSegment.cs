namespace osu.Game.Rulesets.Breve.Objects
{
    /// <summary>
    /// The part of a hold's body on one sweep of the scan line.
    /// </summary>
    /// <param name="PageStartTime">When the sweep starts.</param>
    /// <param name="PageEndTime">When the sweep ends.</param>
    /// <param name="OnUpwardPage">Whether the scan line moves upwards during the sweep.</param>
    /// <param name="FromY">Where the scan line is when this part of the hold starts, as a y coordinate.</param>
    /// <param name="ToY">Where the scan line is when this part of the hold ends, as a y coordinate.</param>
    public readonly record struct HoldSegment(double PageStartTime, double PageEndTime, bool OnUpwardPage, float FromY, float ToY);
}
