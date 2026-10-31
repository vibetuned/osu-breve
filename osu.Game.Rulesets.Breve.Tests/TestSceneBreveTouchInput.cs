using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Graphics;
using osu.Framework.Input;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Tests.Visual;
using osuTK;

namespace osu.Game.Rulesets.Breve.Tests
{
    public partial class TestSceneBreveTouchInput : PlayerTestScene
    {
        protected override Ruleset CreatePlayerRuleset() => new BreveRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => DoublesBeatmap.Create(ruleset);

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            InputManager.EndTouch(new Touch(TouchSource.Touch1, Vector2.Zero));
            InputManager.EndTouch(new Touch(TouchSource.Touch2, Vector2.Zero));
        });

        [Test]
        public void TestTouchPressesTheLaneUnderIt()
        {
            for (int i = 0; i < BrevePlayfield.LANE_COUNT; i++)
            {
                int lane = i;

                AddStep($"touch lane {lane}", () => InputManager.BeginTouch(touchAt(TouchSource.Touch1, lane)));
                AddAssert("lane pressed", () => pressedActions, () => Does.Contain(BreveAction.Lane1 + lane));

                AddStep($"release lane {lane}", () => InputManager.EndTouch(touchAt(TouchSource.Touch1, lane)));
                AddAssert("lane released", () => pressedActions, () => Does.Not.Contain(BreveAction.Lane1 + lane));
            }
        }

        [Test]
        public void TestTouchesDontClick()
        {
            AddStep("touch lane 4", () => InputManager.BeginTouch(touchAt(TouchSource.Touch1, 4)));
            AddAssert("no tap pressed", () => pressedActions, () => Does.Not.Contain(BreveAction.Tap1).And.Not.Contain(BreveAction.Tap2));
            AddStep("release", () => InputManager.EndTouch(touchAt(TouchSource.Touch1, 4)));
        }

        [Test]
        public void TestSlidingAcrossLanesMovesThePress()
        {
            AddStep("touch lane 2", () => InputManager.BeginTouch(touchAt(TouchSource.Touch1, 2)));
            AddAssert("lane 2 pressed", () => pressedActions, () => Does.Contain(BreveAction.Lane3));

            AddStep("slide to lane 3", () => InputManager.MoveTouchTo(touchAt(TouchSource.Touch1, 3)));
            AddAssert("lane 3 pressed", () => pressedActions, () => Does.Contain(BreveAction.Lane4));
            AddAssert("lane 2 released", () => pressedActions, () => Does.Not.Contain(BreveAction.Lane3));

            AddStep("release", () => InputManager.EndTouch(touchAt(TouchSource.Touch1, 3)));
            AddAssert("lane 3 released", () => pressedActions, () => Does.Not.Contain(BreveAction.Lane4));
        }

        [Test]
        public void TestTwoFingersInOneLane()
        {
            AddStep("touch lane 0", () => InputManager.BeginTouch(touchAt(TouchSource.Touch1, 0)));
            AddStep("touch lane 0 with another finger", () => InputManager.BeginTouch(touchAt(TouchSource.Touch2, 0)));
            AddAssert("lane pressed", () => pressedActions, () => Does.Contain(BreveAction.Lane1));

            AddStep("release first finger", () => InputManager.EndTouch(touchAt(TouchSource.Touch1, 0)));
            AddAssert("lane still pressed", () => pressedActions, () => Does.Contain(BreveAction.Lane1));

            AddStep("release second finger", () => InputManager.EndTouch(touchAt(TouchSource.Touch2, 0)));
            AddAssert("lane released", () => pressedActions, () => Does.Not.Contain(BreveAction.Lane1));
        }

        [Test]
        public void TestDoubleIsHitWithTwoFingers()
        {
            // The double on the first downbeat is in lanes 1 and 7.
            AddStep("touch both notes on time", () =>
            {
                atTime(DoublesBeatmap.DOWNBEAT_TIME - 10, () =>
                {
                    InputManager.BeginTouch(touchAt(TouchSource.Touch1, 1));
                    InputManager.BeginTouch(touchAt(TouchSource.Touch2, 7));
                });

                atTime(DoublesBeatmap.DOWNBEAT_TIME + 50, () =>
                {
                    InputManager.EndTouch(touchAt(TouchSource.Touch1, 1));
                    InputManager.EndTouch(touchAt(TouchSource.Touch2, 7));
                });
            });

            AddUntilStep("both judged", () => resultsAt(DoublesBeatmap.DOWNBEAT_TIME).Count() == 2);
            AddAssert("both hit", () => resultsAt(DoublesBeatmap.DOWNBEAT_TIME).All(r => r.IsHit));
        }

        [Test]
        public void TestDoubleHoldIsKeptWithTwoFingers()
        {
            // The double hold in lanes 2 and 6 lasts two beats.
            AddStep("hold both holds until the end", () =>
            {
                atTime(DoublesBeatmap.HOLD_TIME - 10, () =>
                {
                    InputManager.BeginTouch(touchAt(TouchSource.Touch1, 2));
                    InputManager.BeginTouch(touchAt(TouchSource.Touch2, 6));
                });

                atTime(DoublesBeatmap.HOLD_TIME + 2 * NoteTypesBeatmap.BEAT_LENGTH + 50, () =>
                {
                    InputManager.EndTouch(touchAt(TouchSource.Touch1, 2));
                    InputManager.EndTouch(touchAt(TouchSource.Touch2, 6));
                });
            });

            AddUntilStep("both judged", () => resultsAt(DoublesBeatmap.HOLD_TIME).Count() == 2);
            AddAssert("both hit", () => resultsAt(DoublesBeatmap.HOLD_TIME).All(r => r.IsHit));
        }

        [Test]
        public void TestDoubleHoldLetGoOfEarlyIsMissed()
        {
            AddStep("let go of one hold half way", () =>
            {
                atTime(DoublesBeatmap.HOLD_TIME - 10, () =>
                {
                    InputManager.BeginTouch(touchAt(TouchSource.Touch1, 2));
                    InputManager.BeginTouch(touchAt(TouchSource.Touch2, 6));
                });

                atTime(DoublesBeatmap.HOLD_TIME + NoteTypesBeatmap.BEAT_LENGTH, () => InputManager.EndTouch(touchAt(TouchSource.Touch2, 6)));
                atTime(DoublesBeatmap.HOLD_TIME + 2 * NoteTypesBeatmap.BEAT_LENGTH + 50, () => InputManager.EndTouch(touchAt(TouchSource.Touch1, 2)));
            });

            AddUntilStep("both judged", () => resultsAt(DoublesBeatmap.HOLD_TIME).Count() == 2);
            AddAssert("only the kept hold is hit", () => resultsAt(DoublesBeatmap.HOLD_TIME).Count(r => r.IsHit), () => Is.EqualTo(1));
        }

        /// <summary>
        /// Runs an action on the first frame the gameplay clock reaches a time.
        /// Test steps run too far apart to touch notes within their hit windows, so this checks every frame instead.
        /// </summary>
        private void atTime(double time, Action action) => Add(new TimedAction(() => currentTime >= time, action));

        private double currentTime => Player.DrawableRuleset.FrameStableClock.CurrentTime;

        private IEnumerable<BreveAction> pressedActions => this.ChildrenOfType<BreveInputManager>().SelectMany(m => m.KeyBindingContainer.PressedActions);

        private IEnumerable<Rulesets.Judgements.JudgementResult> resultsAt(double time) => Player.Results.Where(r => Precision.AlmostEquals(r.HitObject.StartTime, time));

        private Touch touchAt(TouchSource source, int lane)
            => new Touch(source, Player.DrawableRuleset.Playfield.ToScreenSpace(new Vector2(BrevePlayfield.LaneX(lane), BrevePlayfield.BASE_SIZE.Y / 2)));

        private partial class TimedAction : Drawable
        {
            private readonly Func<bool> due;
            private readonly Action action;

            public TimedAction(Func<bool> due, Action action)
            {
                this.due = due;
                this.action = action;
            }

            protected override void Update()
            {
                base.Update();

                if (!due())
                    return;

                action();
                Expire();
            }
        }
    }
}
