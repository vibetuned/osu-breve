using System;
using System.Collections.Generic;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Difficulty.Preprocessing;
using osu.Game.Rulesets.Objects;

namespace osu.Game.Rulesets.Breve.Difficulty.Preprocessing
{
    public class BreveDifficultyHitObject : DifficultyHitObject
    {
        public new BreveHitObject BaseObject => (BreveHitObject)base.BaseObject;

        /// <summary>
        /// Whether the note has to be pressed, rather than only held like a drag node.
        /// </summary>
        public bool IsPress => BaseObject is not DragNode;

        /// <summary>
        /// Whether the note is a flick, which is played with a quick move from one lane to the next.
        /// </summary>
        public bool IsFlick => BaseObject is Flick;

        /// <summary>
        /// Whether the note is played at the same time as the one before it, as the second note of a double.
        /// </summary>
        public bool IsDoublePartner => DeltaTime < 1;

        /// <summary>
        /// How many lanes away the note is from the last note played before it.
        /// </summary>
        public readonly int LaneDistance;

        /// <summary>
        /// The time since the last note played before this one, adjusted for clock rate.
        /// </summary>
        public readonly double MovementTime;

        /// <summary>
        /// Whether a hold is being held while this note is played.
        /// </summary>
        public readonly bool DuringHold;

        /// <param name="hitObject">The note.</param>
        /// <param name="lastObject">The note before it, which may be played at the same time.</param>
        /// <param name="lastPlayedObject">The last note played before this one, at an earlier time. Null if there is none.</param>
        /// <param name="duringHold">Whether a hold is being held while this note is played.</param>
        /// <param name="clockRate">The rate at which the beatmap is played.</param>
        /// <param name="objects">The list of difficulty objects this one is added to.</param>
        /// <param name="index">This object's index in <paramref name="objects"/>.</param>
        public BreveDifficultyHitObject(HitObject hitObject, HitObject lastObject, BreveHitObject? lastPlayedObject, bool duringHold, double clockRate,
                                            List<DifficultyHitObject> objects, int index)
            : base(hitObject, lastObject, clockRate, objects, index)
        {
            DuringHold = duringHold;

            if (lastPlayedObject == null)
                return;

            LaneDistance = Math.Abs(BaseObject.Lane - lastPlayedObject.Lane);
            MovementTime = (BaseObject.StartTime - lastPlayedObject.StartTime) / clockRate;
        }
    }
}
