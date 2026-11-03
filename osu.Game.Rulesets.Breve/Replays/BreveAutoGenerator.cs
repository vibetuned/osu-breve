using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Replays;
using osuTK;

namespace osu.Game.Rulesets.Breve.Replays
{
    public class BreveAutoGenerator : AutoGenerator<BreveReplayFrame>
    {
        /// <summary>
        /// How long taps and drag nodes are pressed for, unless the same action is needed again sooner.
        /// </summary>
        private const double press_duration = 50;

        /// <summary>
        /// How long before a drag node its lane key is pressed. Nodes are hit as soon as they're held close enough to the scan line.
        /// </summary>
        private const double drag_lead = 10;

        /// <summary>
        /// How long before a flick the lane it's flicked from is pressed.
        /// </summary>
        private const double flick_lead = 30;

        /// <summary>
        /// How long before pressing an action again it is let go of, so that the new press registers.
        /// </summary>
        private const double release_gap = 5;

        public new Beatmap<BreveHitObject> Beatmap => (Beatmap<BreveHitObject>)base.Beatmap;

        public BreveAutoGenerator(IBeatmap beatmap)
            : base(beatmap)
        {
        }

        protected override void GenerateFrames()
        {
            var presses = new List<Press>();
            var cursor = new List<(double time, Vector2 position)> { (-100000, BrevePlayfield.BASE_SIZE / 2) };
            int tapCount = 0;
            var hitObjects = Beatmap.HitObjects;

            for (int i = 0; i < hitObjects.Count; i++)
            {
                BreveHitObject hitObject = hitObjects[i];
                BreveAction laneAction = BreveAction.Lane1 + hitObject.Lane;
                cursor.Add((hitObject.StartTime, hitObject.Position));

                bool isDouble = (i > 0 && Precision.AlmostEquals(hitObjects[i - 1].StartTime, hitObject.StartTime))
                                || (i + 1 < hitObjects.Count && Precision.AlmostEquals(hitObjects[i + 1].StartTime, hitObject.StartTime));

                switch (hitObject)
                {
                    // Alternate between tapping taps and pressing their lane keys, so that watching autoplay (and the autoplay tests) covers both ways of playing.
                    // Doubles, holds and drag chains use lane keys, as a single cursor can't be on two notes at once, or stay on a hold while tapping elsewhere.
                    case Tap:
                        bool useCursor = !isDouble && tapCount++ % 2 == 0;
                        presses.Add(new Press(hitObject.StartTime, useCursor ? BreveAction.Tap1 : laneAction, true) { End = hitObject.StartTime + press_duration });
                        break;

                    case Hold hold:
                        presses.Add(new Press(hold.StartTime, laneAction, true) { End = hold.EndTime });
                        break;

                    case DragNode:
                        presses.Add(new Press(hitObject.StartTime - drag_lead, laneAction, false) { End = hitObject.StartTime + press_duration });
                        break;

                    // Flicks are played by moving from one of their lanes to the other: rightwards on upward sweeps, and leftwards on downward ones.
                    case Flick:
                        int fromLane = hitObject.OnUpwardPage ? hitObject.Lane : hitObject.Lane + 1;
                        int toLane = hitObject.OnUpwardPage ? hitObject.Lane + 1 : hitObject.Lane;

                        presses.Add(new Press(hitObject.StartTime - flick_lead, BreveAction.Lane1 + fromLane, false) { End = hitObject.StartTime });
                        presses.Add(new Press(hitObject.StartTime, BreveAction.Lane1 + toLane, true) { End = hitObject.StartTime + press_duration });
                        break;
                }
            }

            Frames.Add(new BreveReplayFrame(cursor[0].time, cursor[0].position));

            var byStart = resolveOverlaps(presses).Where(p => p.End > p.Start).OrderBy(p => p.Start).ToList();
            var active = new List<Press>();
            int nextPress = 0;
            int cursorIndex = 0;

            foreach (double time in byStart.SelectMany(p => new[] { p.Start, p.End }).Distinct().OrderBy(t => t))
            {
                active.RemoveAll(p => p.End <= time);

                while (nextPress < byStart.Count && byStart[nextPress].Start <= time)
                    active.Add(byStart[nextPress++]);

                while (cursorIndex + 1 < cursor.Count && cursor[cursorIndex + 1].time <= time)
                    cursorIndex++;

                Frames.Add(new BreveReplayFrame(time, cursor[cursorIndex].position, active.Select(p => p.Action).ToArray()));
            }
        }

        /// <summary>
        /// Makes presses of the same action that overlap playable: drag nodes keep the action held on, and anything else lets go of it just before.
        /// </summary>
        private static List<Press> resolveOverlaps(List<Press> presses)
        {
            var resolved = new List<Press>();

            foreach (var group in presses.GroupBy(p => p.Action))
            {
                Press? current = null;

                foreach (var press in group.OrderBy(p => p.Start))
                {
                    if (current != null && current.End > press.Start - release_gap)
                    {
                        if (!press.NeedsNewPress)
                        {
                            current.End = Math.Max(current.End, press.End);
                            continue;
                        }

                        current.End = Math.Max(current.Start, press.Start - release_gap);
                    }

                    if (current != null)
                        resolved.Add(current);

                    current = press;
                }

                if (current != null)
                    resolved.Add(current);
            }

            return resolved;
        }

        /// <summary>
        /// An action pressed from <see cref="Start"/> until <see cref="End"/>.
        /// </summary>
        /// <param name="Start">When the action is pressed.</param>
        /// <param name="Action">The action.</param>
        /// <param name="NeedsNewPress">Whether the note this press is for has to be pressed, rather than just held.</param>
        private record Press(double Start, BreveAction Action, bool NeedsNewPress)
        {
            public double End { get; set; }
        }
    }
}
