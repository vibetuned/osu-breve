using osu.Game.Rulesets.Breve.UI;

namespace osu.Game.Rulesets.Breve.Objects
{
    /// <summary>
    /// A note between two lanes, hit by moving quickly from one of them to the other as the scan line passes over it:
    /// sliding a finger across it, or pressing one of its lane keys just after the other.
    /// </summary>
    /// <remarks>
    /// Its <see cref="BreveHitObject.Lane"/> is the left one of the two, and it sits on the boundary with the lane to its right.
    /// </remarks>
    public class Flick : BreveHitObject
    {
        /// <summary>
        /// How tall a flick is compared to its width, its <see cref="BreveHitObject.Diameter"/>.
        /// </summary>
        public const float HEIGHT_PROPORTION = 0.6f;

        public override float XInLane(int lane) => BrevePlayfield.LaneX(lane) + BrevePlayfield.LaneWidth / 2;
    }
}
