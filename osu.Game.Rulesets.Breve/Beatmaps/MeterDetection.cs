using System;
using System.Collections.Generic;
using System.Linq;
using osu.Game.Audio;
using osu.Game.Beatmaps.ControlPoints;
using osu.Game.Beatmaps.Timing;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Types;

namespace osu.Game.Rulesets.Breve.Beatmaps
{
    /// <summary>
    /// Works out whether a beatmap's music is in triple time (3/4, 6/8) or duple time (4/4) from the rhythm of its notes.
    /// osu! beatmaps usually leave their time signature at the default 4/4, even when the song is in 3/4 or 6/8, which would make pages cut across its measures.
    /// </summary>
    public static class MeterDetection
    {
        /// <summary>
        /// How close to a beat, as a proportion of a beat, a note has to be to count towards it.
        /// </summary>
        private const double on_beat_leniency = 0.125;

        /// <summary>
        /// The fewest beats a timing point has to last for its rhythm to be worth analysing.
        /// </summary>
        private const int min_beats = 24;

        /// <summary>
        /// How much more strongly the accents have to repeat every three beats than every four for the music to count as being in triple time.
        /// </summary>
        private const double triple_time_margin = 0.3;

        /// <summary>
        /// Sets the time signature of timing points left at 4/4 to 3/4 where their notes are in triple time.
        /// Other time signatures were chosen on purpose by the mapper, so they're left as they are.
        /// </summary>
        public static void CorrectTimeSignatures(ControlPointInfo controlPointInfo, IReadOnlyList<HitObject> hitObjects)
        {
            var timingPoints = controlPointInfo.TimingPoints;
            double lastTime = hitObjects.Count > 0 ? hitObjects.Max(h => h.GetEndTime()) : 0;

            for (int i = 0; i < timingPoints.Count; i++)
            {
                var timingPoint = timingPoints[i];

                if (timingPoint.TimeSignature.Numerator != 4)
                    continue;

                double endTime = i + 1 < timingPoints.Count ? timingPoints[i + 1].Time : lastTime;

                if (IsInTripleTime(timingPoint, endTime, hitObjects))
                    timingPoint.TimeSignature = TimeSignature.SimpleTriple;
            }
        }

        /// <summary>
        /// Whether the notes between a timing point and <paramref name="endTime"/> are in triple time.
        /// </summary>
        /// <remarks>
        /// Each beat gets an accent from the notes that start on it, where new combos and finishes (which mappers put on strong beats) count more than other notes.
        /// The music is in triple time if those accents repeat clearly more strongly every three and six beats than every four and eight.
        /// Without enough notes or a clear difference, it's taken to be in duple time like most music.
        /// </remarks>
        public static bool IsInTripleTime(TimingControlPoint timingPoint, double endTime, IReadOnlyList<HitObject> hitObjects)
        {
            int beatCount = (int)Math.Floor((endTime - timingPoint.Time) / timingPoint.BeatLength) + 1;

            if (beatCount < min_beats)
                return false;

            double[] accents = new double[beatCount];

            foreach (var hitObject in hitObjects)
            {
                double beats = (hitObject.StartTime - timingPoint.Time) / timingPoint.BeatLength;
                int beat = (int)Math.Round(beats);

                if (beat < 0 || beat >= beatCount || Math.Abs(beats - beat) > on_beat_leniency)
                    continue;

                accents[beat] += accentOf(hitObject);
            }

            double triple = autocorrelation(accents, 3) + autocorrelation(accents, 6);
            double duple = autocorrelation(accents, 4) + autocorrelation(accents, 8);

            return triple - duple > triple_time_margin;
        }

        private static double accentOf(HitObject hitObject)
        {
            double accent = 1;

            if (hitObject is IHasCombo { NewCombo: true })
                accent += 2;

            var samples = (hitObject as IHasPathWithRepeats)?.NodeSamples.FirstOrDefault() ?? hitObject.Samples;

            if (samples.Any(s => s.Name == HitSampleInfo.HIT_FINISH))
                accent += 2;

            return accent;
        }

        /// <summary>
        /// How alike the accents are to themselves shifted by a number of beats, from -1 (opposite) to 1 (identical).
        /// It's high when the accents repeat every that many beats.
        /// </summary>
        private static double autocorrelation(double[] accents, int lag)
        {
            if (accents.Length <= lag)
                return 0;

            double mean = accents.Average();
            double variance = accents.Sum(a => (a - mean) * (a - mean)) / accents.Length;

            if (variance < 1e-9)
                return 0;

            double covariance = 0;

            for (int i = 0; i + lag < accents.Length; i++)
                covariance += (accents[i] - mean) * (accents[i + lag] - mean);

            return covariance / (accents.Length - lag) / variance;
        }
    }
}
