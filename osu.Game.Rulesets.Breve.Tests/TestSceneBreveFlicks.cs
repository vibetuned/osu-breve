using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Framework.Input;
using osu.Framework.Input.States;
using osu.Framework.Testing;
using osu.Framework.Utils;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Mods;
using osu.Game.Rulesets.Breve.Objects;
using osu.Game.Rulesets.Breve.Objects.Drawables;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Judgements;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Scoring;
using osu.Game.Tests.Visual;
using osuTK;
using osuTK.Input;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// Play a flick between lanes 1 and 2 by rolling from one lane key to the next, sliding a finger across it, or dragging the mouse across it,
    /// and then a double flick with two fingers.
    /// </summary>
    public partial class TestSceneBreveFlicks : PlayerTestScene
    {
        private const double flick_time = 2000;

        private const double double_flick_time = 6000;

        // Half beat sliders moving to the right from lane 1, which become flicks between lanes 1 and 2.
        // The second is accented, on a downbeat, so it becomes a double flick with a mirrored partner between lanes 6 and 7.
        private const string beatmap = @"osu file format v14

[Difficulty]
CircleSize:4
OverallDifficulty:5
SliderMultiplier:1

[TimingPoints]
0,500,4,1,0,100,1,0

[HitObjects]
64,192,2000,2,0,L|114:192,1,50
64,192,6000,2,4,L|114:192,1,50
";

        protected override Ruleset CreatePlayerRuleset() => new BreveRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => LegacyTestBeatmap.Decode(beatmap, ruleset);

        [SetUp]
        public void SetUp() => Schedule(() =>
        {
            InputManager.EndTouch(new Touch(TouchSource.Touch1, Vector2.Zero));
            InputManager.EndTouch(new Touch(TouchSource.Touch2, Vector2.Zero));
            InputManager.ReleaseButton(MouseButton.Left);
        });

        [Test]
        public void TestRollingAcrossLanesHitsTheFlick()
        {
            loadWithFlicks();

            AddStep("press lane 1, then lane 2 on time", () =>
            {
                atTime(flick_time - 40, () => press(1));
                atTime(flick_time, () => press(2));
                atTime(flick_time + 50, () =>
                {
                    release(1);
                    release(2);
                });
            });

            assertHit();
        }

        [Test]
        public void TestSlidingAFingerAcrossHitsTheFlick()
        {
            loadWithFlicks();

            AddStep("slide from lane 1 to lane 2 on time", () =>
            {
                atTime(flick_time - 40, () => InputManager.BeginTouch(touchAt(BrevePlayfield.LaneX(1))));
                atTime(flick_time, () => InputManager.MoveTouchTo(touchAt(BrevePlayfield.LaneX(2))));
                atTime(flick_time + 50, () => InputManager.EndTouch(touchAt(BrevePlayfield.LaneX(2))));
            });

            assertHit();
        }

        [Test]
        public void TestDraggingTheMouseAcrossHitsTheFlick()
        {
            loadWithFlicks();

            AddStep("click the flick and drag across it on time", () =>
            {
                atTime(flick_time - 80, () => InputManager.MoveMouseTo(screenPosition(flick.X)));

                // Each move waits for the one before to have gone through, as hovering is only updated once input has been handled,
                // and a slow frame could otherwise bunch them up into one.
                Add(new TimedAction(() => currentTime >= flick_time - 40 && drawableFlick?.IsHovered == true, () => InputManager.PressButton(MouseButton.Left)));
                Add(new TimedAction(() => currentTime >= flick_time && inputManager.KeyBindingContainer.PressedActions.Contains(BreveAction.Tap1),
                    () => InputManager.MoveMouseTo(screenPosition(flick.X + BrevePlayfield.LaneWidth))));
                Add(new TimedAction(() => result != null, () => InputManager.ReleaseButton(MouseButton.Left)));
            });

            assertHit();
        }

        [Test]
        public void TestPressingOneLaneMissesTheFlick()
        {
            loadWithFlicks();

            AddStep("press only lane 2 on time", () =>
            {
                atTime(flick_time, () => press(2));
                atTime(flick_time + 50, () => release(2));
            });

            assertMissed();
        }

        [Test]
        public void TestChangingLanesTooSlowlyMissesTheFlick()
        {
            loadWithFlicks();

            AddStep("press lane 1 well before lane 2", () =>
            {
                atTime(flick_time - 800, () => press(1));
                atTime(flick_time - 750, () => release(1));
                atTime(flick_time, () => press(2));
                atTime(flick_time + 50, () => release(2));
            });

            assertMissed();
        }

        [Test]
        public void TestDoubleFlickIsHitWithTwoFingers()
        {
            loadWithFlicks();

            AddAssert("double flick between lanes 1 and 2, and 6 and 7", () => flicksAt(double_flick_time).Select(f => f.Lane), () => Is.EquivalentTo(new[] { 1, 6 }));

            AddStep("slide two fingers across on time", () =>
            {
                atTime(double_flick_time - 40, () =>
                {
                    InputManager.BeginTouch(new Touch(TouchSource.Touch1, screenPosition(BrevePlayfield.LaneX(1), double_flick_time)));
                    InputManager.BeginTouch(new Touch(TouchSource.Touch2, screenPosition(BrevePlayfield.LaneX(7), double_flick_time)));
                });
                atTime(double_flick_time, () =>
                {
                    InputManager.MoveTouchTo(new Touch(TouchSource.Touch1, screenPosition(BrevePlayfield.LaneX(2), double_flick_time)));
                    InputManager.MoveTouchTo(new Touch(TouchSource.Touch2, screenPosition(BrevePlayfield.LaneX(6), double_flick_time)));
                });
                atTime(double_flick_time + 50, () =>
                {
                    InputManager.EndTouch(new Touch(TouchSource.Touch1, screenPosition(BrevePlayfield.LaneX(2), double_flick_time)));
                    InputManager.EndTouch(new Touch(TouchSource.Touch2, screenPosition(BrevePlayfield.LaneX(6), double_flick_time)));
                });
            });

            AddUntilStep("both judged", () => resultsAt(double_flick_time).Count() == 2);
            AddAssert("both hit", () => resultsAt(double_flick_time).All(r => r.IsHit));
        }

        [Test]
        public void TestAutoplayHitsSingleAndDoubleFlicks()
        {
            AddStep("load with flicks and autoplay", () => LoadPlayer(new Mod[] { new BreveModFlicks(), new BreveModAutoplay() }));
            AddUntilStep("wait for player", () => Player.IsLoaded);
            AddUntilStep("wait for all judgements", () => Player.ScoreProcessor.HasCompleted.Value);
            AddAssert("three flicks hit", () => Player.Results.Count(r => r.HitObject is Flick && r.IsHit), () => Is.EqualTo(3));
        }

        private void loadWithFlicks()
        {
            AddStep("load with flicks", () => LoadPlayer(new Mod[] { new BreveModFlicks() }));
            AddUntilStep("wait for player", () => Player.IsLoaded);
            AddAssert("flick between lanes 1 and 2", () => flick.Lane, () => Is.EqualTo(1));
        }

        // These tests play in real time, where a slow frame can make a press late, so they check that the flick is hit rather than how well.
        private void assertHit()
        {
            AddUntilStep("flick judged", () => result != null);
            AddAssert("flick hit", () => result!.IsHit);
        }

        private void assertMissed()
        {
            AddUntilStep("flick judged", () => result != null);
            AddAssert("flick missed", () => result!.Type, () => Is.EqualTo(HitResult.Miss));
        }

        private Flick flick => flicksAt(flick_time).Single();

        private Flick[] flicksAt(double time) => Player.DrawableRuleset.Objects.OfType<Flick>().Where(f => Precision.AlmostEquals(f.StartTime, time)).ToArray();

        private JudgementResult? result => resultsAt(flick_time).SingleOrDefault();

        private IEnumerable<JudgementResult> resultsAt(double time) => Player.Results.Where(r => Precision.AlmostEquals(r.HitObject.StartTime, time));

        private DrawableFlick? drawableFlick => Player.DrawableRuleset.ChildrenOfType<DrawableFlick>().SingleOrDefault(d => d.HitObject is Flick f && Precision.AlmostEquals(f.StartTime, flick_time));

        private double currentTime => Player.DrawableRuleset.FrameStableClock.CurrentTime;

        private void atTime(double time, Action action) => Add(new TimedAction(() => currentTime >= time, action));

        private BreveInputManager inputManager => this.ChildrenOfType<BreveInputManager>().Single();

        private void press(int lane) => inputManager.KeyBindingContainer.TriggerPressed(BreveAction.Lane1 + lane);

        private void release(int lane) => inputManager.KeyBindingContainer.TriggerReleased(BreveAction.Lane1 + lane);

        private Vector2 screenPosition(float x) => Player.DrawableRuleset.Playfield.ToScreenSpace(new Vector2(x, flick.Y));

        private Vector2 screenPosition(float x, double time) => Player.DrawableRuleset.Playfield.ToScreenSpace(new Vector2(x, flicksAt(time)[0].Y));

        private Touch touchAt(float x) => new Touch(TouchSource.Touch1, screenPosition(x));
    }
}
