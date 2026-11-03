using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using osu.Game.Audio;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Objects.Legacy;
using osu.Game.Rulesets.Objects.Types;
using osuTK;

namespace osu.Game.Rulesets.Breve.Beatmaps
{
    /// <summary>
    /// Converts beatmaps into osu!breve notes. Each note goes in the lane nearest its source object's x position,
    /// and is placed vertically where the scan line will be at the moment it should be hit.
    /// </summary>
    /// <remarks>
    /// Sliders that come back become drag chains. Other sliders and spinners become holds when they're long enough, and shorter sliders zigzags across the playfield
    /// (see <see cref="Expert"/> for how long). With <see cref="Flicks"/>, sliders too short for either become flicks. Everything else becomes a tap.
    /// Taps, holds and flicks that would clash with another note are then moved to a nearby lane, and taps and holds with a finish hitsound, the mapper's accents, become doubles.
    /// </remarks>
    public class BreveBeatmapConverter : BeatmapConverter<BreveHitObject>
    {
        /// <summary>
        /// How far from a beat, as a proportion of a beat, a note can be and still count as being on it when choosing how to place doubles.
        /// </summary>
        private const double on_beat_leniency = 0.05;

        /// <summary>
        /// How many lanes away a note in the centre lane gets its partner, when it becomes a double.
        /// </summary>
        private const int centre_partner_distance = 3;

        /// <summary>
        /// The fewest lanes the two taps of a zigzag are apart.
        /// </summary>
        private const int min_zigzag_distance = 4;

        /// <summary>
        /// How long before a hold starts and after it ends its lane is kept free of other notes, in milliseconds,
        /// so that its key can be let go of and pressed again.
        /// </summary>
        private const double hold_lane_clearance = 100;

        private const int centre_lane = BrevePlayfield.LANE_COUNT / 2;

        /// <summary>
        /// The rightmost lane a flick can be in, as it sits between its lane and the next one.
        /// </summary>
        private const int last_flick_lane = BrevePlayfield.LANE_COUNT - 2;

        /// <summary>
        /// How far the links of a drag chain may move sideways for each unit they travel with the scan line.
        /// At 1, links never slope by more than 45° away from the direction the scan line moves in.
        /// </summary>
        public const float MAX_DRAG_SLOPE = 1;

        /// <summary>
        /// The closest that the nodes of a drag chain may be, in milliseconds.
        /// </summary>
        private const double min_drag_node_interval = 60;

        private PageTiming pageTiming = null!;

        /// <summary>
        /// The <see cref="BreveHitObject.Scale"/> of the beatmap's notes.
        /// </summary>
        private float noteScale;

        /// <summary>
        /// Whether to make the expert chart, which has many more zigzags.
        /// </summary>
        /// <remarks>
        /// The normal chart has holds for sliders longer than a beat, zigzags for sliders of about a beat, and taps for anything shorter.
        /// The expert chart has holds for sliders of a beat or more, and zigzags for sliders from half a beat, so that every run of short sliders swings across the playfield.
        /// </remarks>
        public bool Expert { get; set; }

        /// <summary>
        /// Whether the short sliders that would otherwise become taps become <see cref="Flick"/>s instead:
        /// those shorter than a beat in the normal chart, and shorter than half a beat in the expert chart, where the longer ones are zigzags.
        /// </summary>
        public bool Flicks { get; set; }

        /// <summary>
        /// How many beats a slider or spinner has to last to become a hold.
        /// </summary>
        private double minHoldBeats => Expert ? 1 : 1.25;

        /// <summary>
        /// How many beats a slider has to last to become a zigzag, if it's too short to become a hold.
        /// </summary>
        private double minZigzagBeats => Expert ? 0.5 : 1;

        public BreveBeatmapConverter(IBeatmap beatmap, Ruleset ruleset)
            : base(beatmap, ruleset)
        {
        }

        public override bool CanConvert() => Beatmap.HitObjects.All(h => h is IHasXPosition);

        protected override Beatmap<BreveHitObject> ConvertBeatmap(IBeatmap original, CancellationToken cancellationToken)
        {
            // This is a copy of the source beatmap's timing points (see BeatmapConverter.Convert), which the converted beatmap and so the playfield's scan line also use.
            MeterDetection.CorrectTimeSignatures(original.ControlPointInfo, original.HitObjects);
            pageTiming = new PageTiming(original.ControlPointInfo);

            // Sized like osu!'s hit circles, which works out larger than lanes are wide. Neighbouring lanes' notes are kept apart by moveClashingNotes.
            noteScale = LegacyRulesetExtensions.CalculateScaleFromCircleSize(original.Difficulty.CircleSize, true);

            var beatmap = base.ConvertBeatmap(original, cancellationToken);
            moveClashingNotes(beatmap);
            addDoubles(beatmap);

            return beatmap;
        }

        protected override IEnumerable<BreveHitObject> ConvertHitObject(HitObject original, IBeatmap beatmap, CancellationToken cancellationToken)
        {
            double beatLength = beatmap.ControlPointInfo.TimingPointAt(original.StartTime).BeatLength;

            switch (original)
            {
                case IHasPathWithRepeats slider when slider.RepeatCount > 0:
                    foreach (var node in createDragChain(original, slider, beatLength))
                        yield return node;

                    break;

                // Shorter holds would be little more than taps, and their bodies would crowd the playfield.
                case IHasDuration hasDuration when lastsAtLeast(hasDuration.Duration, minHoldBeats, beatLength):
                    yield return createHold(original, hasDuration);

                    break;

                // Shorter sliders become zigzags instead, unless they're too short to jump across the playfield in.
                case IHasPathWithRepeats slider when lastsAtLeast(slider.Duration, minZigzagBeats, beatLength):
                    foreach (var tap in createZigzag(original, slider))
                        yield return tap;

                    break;

                // Sliders too short for anything else ask for a quick move across lanes, rather than just a tap.
                case IHasPathWithRepeats slider when Flicks:
                    yield return createFlick(original, slider);

                    break;

                default:
                    yield return place(new Tap { Samples = headSamples(original) }, original.StartTime, laneOf(original));

                    break;
            }
        }

        /// <summary>
        /// Retrieves the lane nearest to an x position on osu!'s playfield.
        /// </summary>
        /// <remarks>
        /// Rounding half to even (<see cref="Math.Round(double)"/>'s default) is symmetric around the centre lane, as there is an odd number of lanes.
        /// That way, positions mirrored across the middle of the playfield always land in mirrored lanes.
        /// </remarks>
        public static int LaneFor(float x)
        {
            int lane = (int)Math.Round(x / BrevePlayfield.BASE_SIZE.X * (BrevePlayfield.LANE_COUNT - 1));
            return Math.Clamp(lane, 0, BrevePlayfield.LANE_COUNT - 1);
        }

        /// <summary>
        /// Whether a duration is at least a number of beats long, allowing for slider durations computed from their length landing a fraction of a millisecond short.
        /// </summary>
        private static bool lastsAtLeast(double duration, double beats, double beatLength) => duration >= beats * beatLength - 1;

        /// <summary>
        /// Creates a zigzag from a slider: a tap at its head, and another at its tail on the other side of the playfield,
        /// so that a run of short sliders swings from side to side. Each tap keeps the hitsounds of the end of the slider it's at.
        /// </summary>
        private IEnumerable<Tap> createZigzag(HitObject original, IHasPathWithRepeats slider)
        {
            int headLane = laneOf(original);

            yield return place(new Tap { Samples = sliderNodeSamples(slider, 0) }, original.StartTime, headLane);
            yield return place(new Tap { Samples = sliderNodeSamples(slider, slider.SpanCount()) }, slider.EndTime, zigzagLane(headLane, pageTiming.PageAt(original.StartTime).MovesUpwards));
        }

        /// <summary>
        /// Retrieves the lane at the other end of a zigzag from a lane: the mirrored lane across the centre,
        /// or, where that's too close (near the centre), the lane <see cref="min_zigzag_distance"/> away on the other side.
        /// From the centre lane itself, a zigzag goes right on upward sweeps and left on downward ones.
        /// </summary>
        private static int zigzagLane(int lane, bool onUpwardPage)
        {
            int mirrored = BrevePlayfield.LANE_COUNT - 1 - lane;

            if (Math.Abs(mirrored - lane) >= min_zigzag_distance)
                return mirrored;

            int direction = lane < centre_lane ? 1 : lane > centre_lane ? -1 : onUpwardPage ? 1 : -1;
            return Math.Clamp(lane + direction * min_zigzag_distance, 0, BrevePlayfield.LANE_COUNT - 1);
        }

        /// <summary>
        /// Creates a flick from a slider, at its head, on the boundary between the head's lane and the next one in the direction the slider moves.
        /// A slider that doesn't move sideways points towards the centre lane, or from the centre lane itself, right on upward sweeps and left on downward ones.
        /// </summary>
        private Flick createFlick(HitObject original, IHasPathWithRepeats slider)
        {
            int lane = laneOf(original);
            float travel = slider.CurvePositionAt(1).X;

            int direction = Math.Abs(travel) >= 1 ? Math.Sign(travel)
                : lane != centre_lane ? Math.Sign(centre_lane - lane)
                : pageTiming.PageAt(original.StartTime).MovesUpwards ? 1 : -1;

            // A flick's lane is the left one of the two it sits between.
            int leftLane = Math.Clamp(direction > 0 ? lane : lane - 1, 0, last_flick_lane);

            return place(new Flick { Samples = headSamples(original) }, original.StartTime, leftLane);
        }

        private Hold createHold(HitObject original, IHasDuration hasDuration)
        {
            var slider = original as IHasPathWithRepeats;

            return place(new Hold
            {
                Duration = hasDuration.Duration,
                // Like in osu!, sliders sound at their head when pressed and at their tail when completed, and spinners only when completed.
                PressSamples = slider?.NodeSamples.FirstOrDefault() ?? new List<HitSampleInfo>(),
                Samples = slider?.NodeSamples.LastOrDefault() ?? original.Samples,
                Segments = createHoldSegments(original.StartTime, hasDuration.EndTime),
            }, original.StartTime, laneOf(original));
        }

        /// <summary>
        /// Splits the body of a hold into the parts the scan line sweeps on each page.
        /// </summary>
        private List<HoldSegment> createHoldSegments(double startTime, double endTime)
        {
            var segments = new List<HoldSegment>();

            foreach (Page page in pageTiming.PagesBetween(startTime, endTime))
            {
                double from = Math.Max(startTime, page.StartTime);
                double to = Math.Min(endTime, page.EndTime);

                // A hold ending right at a turnaround may reach a sliver past it through rounding, which isn't worth a part of its own.
                if (to - from < 1 && segments.Count > 0)
                    continue;

                segments.Add(new HoldSegment(page.StartTime, page.EndTime, page.MovesUpwards, scanLineYAt(from), scanLineYAt(to)));
            }

            return segments;
        }

        /// <summary>
        /// Creates a drag chain that follows a slider. A chain is either diagonal all the way or straight all the way, never a mix of both.
        /// </summary>
        /// <remarks>
        /// When it can, the chain zigzags with the slider: each span of the slider (out, and back after each turnaround) becomes one diagonal
        /// towards the lane the span ends in, with every link stepping one lane. Steps are spread evenly over the span, fall on quarter beats
        /// (or a coarser division at tempos where that would crowd them), and only happen once the scan line has travelled far enough
        /// for the link not to slope by more than <see cref="MAX_DRAG_SLOPE"/>. A chain following a fast slider therefore pushes out as far as it can before coming back.
        /// When that's not possible, because the slider doesn't move across lanes or one of its spans is too short to step, the chain is a straight run
        /// along the head's lane instead, with a node on every quarter beat.
        /// The slider's head, turnarounds and tail always get a node, as they're where its hitsounds are.
        /// </remarks>
        private IEnumerable<DragNode> createDragChain(HitObject original, IHasPathWithRepeats slider, double beatLength)
        {
            double nodeInterval = beatLength / 4;
            while (nodeInterval < min_drag_node_interval)
                nodeInterval *= 2;

            int spanCount = slider.SpanCount();
            int nodesPerSpan = Math.Max(1, (int)Math.Round(slider.Duration / spanCount / nodeInterval));
            int lastIndex = spanCount * nodesPerSpan;
            float headX = ((IHasXPosition)original).X;

            return createDiagonalChain() ?? createStraightChain();

            List<DragNode>? createDiagonalChain()
            {
                var nodes = new List<DragNode> { createNode(0, LaneFor(headX), null) };

                for (int span = 0; span < spanCount; span++)
                {
                    int spanEndLane = LaneFor(headX + slider.CurvePositionAt((double)(span + 1) / spanCount).X);
                    int steps = Math.Abs(spanEndLane - nodes[^1].Lane);
                    int direction = Math.Sign(spanEndLane - nodes[^1].Lane);
                    float spanEndY = scanLineYAt(timeAt((span + 1) * nodesPerSpan));
                    int stepsTaken = 0;

                    if (steps == 0)
                        return null;

                    for (int j = 1; j <= nodesPerSpan; j++)
                    {
                        int index = span * nodesPerSpan + j;
                        float y = scanLineYAt(timeAt(index));
                        bool isSpanEnd = j == nodesPerSpan;

                        // The next step is due once this much of the span has passed: j / nodesPerSpan >= (stepsTaken + 1) / steps.
                        // It's held back if it would leave too little travel for the link into the span's last node to step as well.
                        bool stepDue = stepsTaken < steps && j * steps >= (stepsTaken + 1) * nodesPerSpan;
                        bool canStep = isSteepEnough(nodes[^1].Y, y) && (isSpanEnd || isSteepEnough(y, spanEndY));

                        if (stepDue && canStep)
                        {
                            nodes.Add(createNode(index, nodes[^1].Lane + direction, nodes[^1]));
                            stepsTaken++;
                        }
                        else if (isSpanEnd)
                        {
                            // The chain would have to run straight into the turnaround or tail.
                            return null;
                        }
                    }
                }

                return nodes;
            }

            List<DragNode> createStraightChain()
            {
                int lane = LaneFor(headX);
                var nodes = new List<DragNode>();

                for (int index = 0; index <= lastIndex; index++)
                    nodes.Add(createNode(index, lane, nodes.Count > 0 ? nodes[^1] : null));

                return nodes;
            }

            double timeAt(int index) => original.StartTime + (double)index / lastIndex * slider.Duration;

            DragNode createNode(int index, int lane, DragNode? previous) => place(new DragNode
            {
                // Only the slider's head, turnarounds and tail carry hitsounds.
                Samples = index % nodesPerSpan == 0 ? sliderNodeSamples(slider, index / nodesPerSpan) : new List<HitSampleInfo>(),
                PreviousNode = previous,
            }, timeAt(index), lane);
        }

        private float scanLineYAt(double time) => BrevePlayfield.ScanLineY(pageTiming.PositionAt(time));

        /// <summary>
        /// Turns accented taps, holds and flicks, those with a finish hitsound, into doubles.
        /// </summary>
        /// <remarks>
        /// Where a note falls in the measure decides where its partner goes: on a downbeat it's mirrored around the centre lane,
        /// on any other beat it's in the centre lane, and off the beat it's two lanes away on the same side.
        /// A note already in the centre lane, which can be neither mirrored nor partnered with it, gets its partner three lanes away instead,
        /// on the right on upward sweeps and on the left on downward ones.
        /// Flicks sit between lanes, so they have no centre lane to pair with: on any beat their partner is mirrored, and off the beat it's two boundaries over on the same side
        /// (see <see cref="mirroredFlickLane"/> and <see cref="sameSideFlickLane"/>).
        /// If a partner's lane is taken by another note around that time, the next of those placements is tried, and the note stays single if none are free.
        /// </remarks>
        private void addDoubles(Beatmap<BreveHitObject> beatmap)
        {
            var partners = new List<BreveHitObject>();

            foreach (var note in beatmap.HitObjects)
            {
                if (note is not (Tap or Hold or Flick) || !isAccented(note))
                    continue;

                var timingPoint = beatmap.ControlPointInfo.TimingPointAt(note.StartTime);
                double beats = (note.StartTime - timingPoint.Time) / timingPoint.BeatLength;
                bool onBeat = Math.Abs(beats - Math.Round(beats)) < on_beat_leniency;
                bool onDownbeat = onBeat && (long)Math.Round(beats) % timingPoint.TimeSignature.Numerator == 0;

                int?[] placements;

                if (note is Flick)
                {
                    int? mirrored = mirroredFlickLane(note.Lane);
                    int? sameSide = sameSideFlickLane(note.Lane);

                    placements = onBeat ? new[] { mirrored, sameSide } : new[] { sameSide, mirrored };
                }
                else
                {
                    int? mirrored = note.Lane == centre_lane ? null : BrevePlayfield.LANE_COUNT - 1 - note.Lane;
                    int? withCentre = note.Lane == centre_lane ? centre_lane + (note.OnUpwardPage ? centre_partner_distance : -centre_partner_distance) : centre_lane;
                    int? sameSide = sameSideLane(note.Lane);

                    placements = onDownbeat ? new[] { mirrored, withCentre, sameSide }
                        : onBeat ? new[] { withCentre, mirrored, sameSide }
                        : new[] { sameSide, withCentre, mirrored };
                }

                // The partner is checked against the original note too, as it can be too close to it in a neighbouring lane.
                var partner = placements.Where(lane => lane != null)
                                        .Select(lane => createPartner(note, lane!.Value))
                                        .FirstOrDefault(p => !clashes(p, p.Lane, beatmap.HitObjects.Concat(partners)));

                if (partner != null)
                    partners.Add(partner);
            }

            beatmap.HitObjects = beatmap.HitObjects.Concat(partners).OrderBy(h => h.StartTime).ToList();
        }

        /// <summary>
        /// Moves taps, holds and flicks that clash with an earlier note, or with any drag chain, to the nearest lane where they don't.
        /// One lane over is tried first, to the right on upward sweeps and to the left on downward ones, then two lanes over, and so on.
        /// Drag chains are never moved, so that they keep their shape.
        /// In passages too dense for any lane to be clear, a note goes where it overlaps other notes the least, but never into a lane held for a hold.
        /// </summary>
        private static void moveClashingNotes(Beatmap<BreveHitObject> beatmap)
        {
            var placed = beatmap.HitObjects.OfType<DragNode>().Cast<BreveHitObject>().ToList();

            foreach (var note in beatmap.HitObjects)
            {
                if (note is DragNode)
                    continue;

                int lane = note.Lane;
                float leastOverlap = float.PositiveInfinity;

                foreach (int candidate in lanesByPreference(note))
                {
                    float overlap = overlapIn(note, candidate, placed);

                    if (overlap < leastOverlap)
                    {
                        lane = candidate;
                        leastOverlap = overlap;
                    }

                    if (overlap == 0)
                        break;
                }

                if (lane != note.Lane)
                {
                    note.Lane = lane;
                    note.Position = new Vector2(note.XInLane(lane), note.Y);
                }

                placed.Add(note);
            }
        }

        /// <summary>
        /// Retrieves the lanes to try a note in when it clashes with another: its own, then one lane over on each side, then two lanes over, and so on.
        /// </summary>
        private static IEnumerable<int> lanesByPreference(BreveHitObject note)
        {
            yield return note.Lane;

            int direction = note.OnUpwardPage ? 1 : -1;
            int lastLane = note is Flick ? last_flick_lane : BrevePlayfield.LANE_COUNT - 1;

            for (int distance = 1; distance < BrevePlayfield.LANE_COUNT; distance++)
            {
                foreach (int lane in new[] { note.Lane + direction * distance, note.Lane - direction * distance })
                {
                    if (lane >= 0 && lane <= lastLane)
                        yield return lane;
                }
            }
        }

        /// <summary>
        /// Whether a note would clash with any of some other notes if it were in a lane.
        /// </summary>
        private static bool clashes(BreveHitObject note, int lane, IEnumerable<BreveHitObject> others) => overlapIn(note, lane, others) > 0;

        /// <summary>
        /// How badly a note would clash with some other notes if it were in a lane:
        /// by how far, in playfield units, it would overlap the one it overlaps most of those on screen at the same time
        /// (taps are wider than lanes, so that includes notes in neighbouring lanes), and infinitely if one of its lanes' keys is held down for a hold then.
        /// 0 if it doesn't clash at all.
        /// </summary>
        private static float overlapIn(BreveHitObject note, int lane, IEnumerable<BreveHitObject> others)
        {
            float deepestOverlap = 0;

            foreach (var other in others)
            {
                if (other == note)
                    continue;

                float horizontalDistance = Math.Abs(other.X - note.XInLane(lane));
                float minDistance = (note.Diameter + other.Diameter) / 2;

                if (horizontalDistance >= minDistance)
                    continue;

                if (shareALane(note, lane, other, other.Lane) && ((other is Hold && duringHold(note, other)) || (note is Hold && duringHold(other, note))))
                    return float.PositiveInfinity;

                bool onScreenTogether = note.StartTime - note.TimePreempt < other.GetEndTime() && other.StartTime - other.TimePreempt < note.GetEndTime();

                if (!onScreenTogether)
                    continue;

                float verticalDistance = note.Y - other.Y;
                float distance = MathF.Sqrt(horizontalDistance * horizontalDistance + verticalDistance * verticalDistance);

                deepestOverlap = Math.Max(deepestOverlap, minDistance - distance);
            }

            return deepestOverlap;

            static bool duringHold(BreveHitObject note, BreveHitObject hold)
                => note.StartTime <= hold.GetEndTime() + hold_lane_clearance && note.GetEndTime() >= hold.StartTime - hold_lane_clearance;
        }

        /// <summary>
        /// Whether two notes, each in a lane, are played with any of the same lane keys. A flick is played with both lanes it sits between.
        /// </summary>
        private static bool shareALane(BreveHitObject first, int firstLane, BreveHitObject second, int secondLane)
        {
            int firstLastLane = first is Flick ? firstLane + 1 : firstLane;
            int secondLastLane = second is Flick ? secondLane + 1 : secondLane;

            return firstLane <= secondLastLane && secondLane <= firstLastLane;
        }

        /// <summary>
        /// Creates the partner of a note in a double. It's silent, as the original note already plays the hitsound.
        /// </summary>
        private BreveHitObject createPartner(BreveHitObject note, int lane)
        {
            BreveHitObject partner = note switch
            {
                Hold hold => new Hold { Duration = hold.Duration, Segments = hold.Segments },
                Flick => new Flick(),
                _ => new Tap(),
            };

            return place(partner, note.StartTime, lane);
        }

        /// <summary>
        /// Retrieves the lane of the flick mirroring a flick around the centre lane. Null for the two flicks either side of the centre lane,
        /// whose mirrors are each other: they would share the centre lane, so they can't be played together.
        /// </summary>
        private static int? mirroredFlickLane(int lane)
        {
            int mirrored = last_flick_lane - lane;
            return Math.Abs(mirrored - lane) >= 2 ? mirrored : null;
        }

        /// <summary>
        /// Retrieves the lane of the flick two boundaries away from a flick on the same side of the centre: further out if there's room, and further in otherwise.
        /// Flicks two boundaries apart don't share a lane, so they can be played together.
        /// </summary>
        private static int sameSideFlickLane(int lane)
        {
            if (lane < centre_lane)
                return lane >= 2 ? lane - 2 : lane + 2;

            return lane <= last_flick_lane - 2 ? lane + 2 : lane - 2;
        }

        /// <summary>
        /// Retrieves the lane two away from a lane on the same side of the centre: further out if there's room, and further in otherwise.
        /// Null for the centre lane, which has no side.
        /// </summary>
        private static int? sameSideLane(int lane)
        {
            if (lane < centre_lane)
                return lane >= 2 ? lane - 2 : lane + 2;

            if (lane > centre_lane)
                return lane <= BrevePlayfield.LANE_COUNT - 3 ? lane + 2 : lane - 2;

            return null;
        }

        private static bool isAccented(BreveHitObject note) => (note is Hold hold ? hold.PressSamples : note.Samples).Any(s => s.Name == HitSampleInfo.HIT_FINISH);

        /// <summary>
        /// Whether a drag chain link between two heights is steep enough to step into the next lane without sloping by more than <see cref="MAX_DRAG_SLOPE"/>.
        /// </summary>
        private static bool isSteepEnough(float fromY, float toY) => Math.Abs(toY - fromY) * MAX_DRAG_SLOPE >= BrevePlayfield.LaneWidth;

        private static IList<HitSampleInfo> sliderNodeSamples(IHasPathWithRepeats slider, int node) => node < slider.NodeSamples.Count ? slider.NodeSamples[node] : new List<HitSampleInfo>();

        /// <summary>
        /// Places a hit object at a time and in a lane, on the scan line.
        /// </summary>
        private T place<T>(T hitObject, double time, int lane)
            where T : BreveHitObject
        {
            Page page = pageTiming.PageAt(time);

            hitObject.StartTime = time;
            hitObject.Lane = lane;
            hitObject.Position = new Vector2(hitObject.XInLane(lane), BrevePlayfield.ScanLineY(pageTiming.PositionAt(time)));
            hitObject.TimePreempt = page.Duration;
            hitObject.OnUpwardPage = page.MovesUpwards;
            hitObject.PageStartTime = page.StartTime;
            hitObject.Scale = noteScale;

            return hitObject;
        }

        private static int laneOf(HitObject original) => LaneFor(((IHasXPosition)original).X);

        private static IList<HitSampleInfo> headSamples(HitObject original) => (original as IHasPathWithRepeats)?.NodeSamples.FirstOrDefault() ?? original.Samples;
    }
}
