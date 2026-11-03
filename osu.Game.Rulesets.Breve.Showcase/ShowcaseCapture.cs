using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using osu.Framework.Allocation;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Platform;
using osu.Framework.Testing;
using osu.Game.Beatmaps;
using osu.Game.Rulesets.Mods;
using osu.Game.Screens.Play.HUD;
using osu.Game.Tests.Visual;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;

namespace osu.Game.Rulesets.Breve.Showcase
{
    /// <summary>
    /// Plays <see cref="ShowcaseBeatmap"/> with autoplay and saves every frame, stepping gameplay time by exactly one frame between screenshots,
    /// so that the frames are evenly spaced however long each screenshot takes.
    /// </summary>
    public partial class ShowcaseCapture : PlayerTestScene
    {
        public const double FRAME_RATE = 60;

        /// <summary>
        /// How many frames to wait once gameplay has reached a frame's time before taking its screenshot, so that the frame drawn is up to date.
        /// </summary>
        private const int settle_frames = 3;

        private readonly string outputDirectory;
        private readonly Mod[] mods;
        private readonly double startTime;
        private readonly double endTime;

        private readonly ConcurrentBag<Task> saves = new ConcurrentBag<Task>();

        private bool capturing;
        private bool captured;
        private double frameTime;
        private int frameIndex;
        private int settledFrames;
        private Task<Image<Rgba32>>? screenshot;

        [Resolved]
        private GameHost host { get; set; } = null!;

        public ShowcaseCapture(string outputDirectory, Mod[] mods, double startTime, double endTime)
        {
            this.outputDirectory = outputDirectory;
            this.mods = mods;
            this.startTime = startTime;
            this.endTime = endTime;
        }

        protected override Ruleset CreatePlayerRuleset() => new BreveRuleset();

        protected override IBeatmap CreateBeatmap(RulesetInfo ruleset) => ShowcaseBeatmap.Create(ruleset);

        protected override bool Autoplay => true;

        protected override bool AllowFail => true;

        protected override bool HasCustomSteps => true;

        protected override double TimePerAction => 20;

        [BackgroundDependencyLoader]
        private void load(FrameworkConfigManager frameworkConfig)
        {
            // 960x540 is 1920x1080 on high-DPI displays.
            frameworkConfig.SetValue(FrameworkSetting.WindowMode, WindowMode.Windowed);
            frameworkConfig.SetValue(FrameworkSetting.WindowedSize, new System.Drawing.Size(960, 540));
            frameworkConfig.SetValue(FrameworkSetting.Locale, "en");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // The test scene keeps a list of its steps on the left, which isn't part of the gameplay being captured.
            foreach (var part in ((Container)InternalChild).Children)
            {
                if (part is Container<Drawable> container && container.Padding.Left > 0)
                    container.Padding = new MarginPadding();
                else
                    part.Hide();
            }
        }

        public void AddCaptureSteps()
        {
            AddStep("load player", () => LoadPlayer(mods));
            AddUntilStep("player loaded", () => Player.IsLoaded && Player.Alpha == 1);
            // Gameplay waits too, so that no notes are played before the capture starts.
            AddStep("pause gameplay", () => Player.GameplayClockContainer.Stop());
            // Two seconds, for the window to resize and the background to finish dimming, as the player has only just appeared.
            AddWaitStep("let the window and background settle", (int)(2000 / TimePerAction));
            AddStep("start capturing", () =>
            {
                Directory.CreateDirectory(outputDirectory);

                frameTime = startTime;
                Player.GameplayClockContainer.Seek(frameTime);
                capturing = true;
            });
            AddUntilStep("capture every frame", () => captured);
            AddUntilStep("save every frame", () => saves.All(s => s.IsCompleted));
        }

        protected override void Update()
        {
            base.Update();

            if (!capturing)
                return;

            if (screenshot != null)
            {
                if (!screenshot.IsCompleted)
                    return;

                var image = screenshot.GetResultSafely();
                string path = Path.Combine(outputDirectory, $"frame{frameIndex:D5}.png");

                saves.Add(Task.Run(() =>
                {
                    using (image)
                        image.SaveAsPng(path, new PngEncoder { CompressionLevel = PngCompressionLevel.BestSpeed });
                }));

                screenshot = null;
                frameIndex++;
                frameTime = startTime + frameIndex * 1000 / FRAME_RATE;

                if (frameTime > endTime)
                {
                    capturing = false;
                    captured = true;
                    return;
                }

                Player.GameplayClockContainer.Seek(frameTime);
                settledFrames = 0;
                return;
            }

            if (Math.Abs(Player.DrawableRuleset.FrameStableClock.CurrentTime - frameTime) > 0.5)
            {
                settledFrames = 0;
                return;
            }

            if (++settledFrames == 1)
                tidyHud();

            if (settledFrames < settle_frames)
                return;

            screenshot = host.TakeScreenshotAsync();
        }

        /// <summary>
        /// Hides the parts of the HUD that are only clutter in a showcase: the key counter, the leaderboard, the pp counter, and the mods (autoplay).
        /// </summary>
        private void tidyHud()
        {
            var clutter = Player.ChildrenOfType<KeyCounterDisplay>().Cast<Drawable>()
                                .Concat(Player.ChildrenOfType<DrawableGameplayLeaderboard>())
                                .Concat(Player.ChildrenOfType<PerformancePointsCounter>())
                                .Concat(Player.ChildrenOfType<ModDisplay>());

            foreach (var drawable in clutter)
            {
                drawable.ClearTransforms();
                drawable.Alpha = 0;
            }
        }
    }
}
