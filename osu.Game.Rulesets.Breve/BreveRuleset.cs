using System;
using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Input.Bindings;
using osu.Framework.Localisation;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Breve.Beatmaps;
using osu.Game.Rulesets.Breve.Difficulty;
using osu.Game.Rulesets.Breve.Mods;
using osu.Game.Rulesets.Breve.Replays;
using osu.Game.Rulesets.Breve.UI;
using osu.Game.Rulesets.Difficulty;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Replays.Types;
using osu.Game.Rulesets.Scoring;
using osu.Game.Rulesets.UI;

namespace osu.Game.Rulesets.Breve
{
    public class BreveRuleset : Ruleset
    {
        public const string SHORT_NAME = "breve";

        // The lane keys sit on the home row, with the centre lane on the space bar.
        // Keys are bound by their physical position, so on an AZERTY keyboard these are Q S D F, space, J K L M.
        private static readonly InputKey[] lane_keys =
        {
            InputKey.A, InputKey.S, InputKey.D, InputKey.F,
            InputKey.Space,
            InputKey.J, InputKey.K, InputKey.L, InputKey.Semicolon,
        };

        // Each lane also has a MIDI key, the white keys from middle C, so that the lanes play like a keyboard (and a drag chain like a glissando on it).
        // It's a second binding, which also gives each lane a second column to rebind in the key binding settings.
        private static readonly InputKey[] lane_midi_keys =
        {
            InputKey.MidiC4, InputKey.MidiD4, InputKey.MidiE4, InputKey.MidiF4,
            InputKey.MidiG4,
            InputKey.MidiA4, InputKey.MidiB4, InputKey.MidiC5, InputKey.MidiD5,
        };

        public override string Description => "osu!breve";

        public override string ShortName => SHORT_NAME;

        public override DrawableRuleset CreateDrawableRulesetWith(IBeatmap beatmap, IReadOnlyList<Mod>? mods = null) => new DrawableBreveRuleset(this, beatmap, mods);

        public override IBeatmapConverter CreateBeatmapConverter(IBeatmap beatmap) => new BreveBeatmapConverter(beatmap, this);

        public override DifficultyCalculator CreateDifficultyCalculator(IWorkingBeatmap beatmap) => new BreveDifficultyCalculator(RulesetInfo, beatmap);

        public override IConvertibleReplayFrame CreateConvertibleReplayFrame() => new BreveReplayFrame();

        public override IEnumerable<Mod> GetModsFor(ModType type)
        {
            switch (type)
            {
                case ModType.DifficultyReduction:
                    return new Mod[] { new BreveModNoFail(), new BreveModHalfTime() };

                case ModType.DifficultyIncrease:
                    return new Mod[] { new BreveModDoubleTime(), new BreveModExpert() };

                case ModType.Automation:
                    return new Mod[] { new BreveModAutoplay() };

                default:
                    return Array.Empty<Mod>();
            }
        }

        public override IEnumerable<KeyBinding> GetDefaultKeyBindings(int variant = 0)
        {
            var bindings = new List<KeyBinding>
            {
                new KeyBinding(InputKey.MouseLeft, BreveAction.Tap1),
                new KeyBinding(InputKey.MouseRight, BreveAction.Tap2),
            };

            for (int lane = 0; lane < BrevePlayfield.LANE_COUNT; lane++)
            {
                bindings.Add(new KeyBinding(lane_keys[lane], BreveAction.Lane1 + lane));
                bindings.Add(new KeyBinding(lane_midi_keys[lane], BreveAction.Lane1 + lane));
            }

            return bindings;
        }

        public override IEnumerable<HitResult> GetValidHitResults() => new[]
        {
            HitResult.Perfect,
            HitResult.Good,
            HitResult.Meh,
            HitResult.Miss,
        };

        public override LocalisableString GetDisplayNameForHitResult(HitResult result)
        {
            switch (result)
            {
                // Cytus calls its lowest successful judgement "bad".
                case HitResult.Meh:
                    return "bad";
            }

            return base.GetDisplayNameForHitResult(result);
        }

        public override Drawable CreateIcon() => new BreveRulesetIcon();

        // Leave this line intact. It will bake the correct version into the ruleset on each build/release.
        public override string RulesetAPIVersionSupported => CURRENT_RULESET_API_VERSION;
    }
}
