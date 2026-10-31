using System;
using System.Globalization;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using osu.Framework;
using osu.Framework.Platform;
using osu.Game.Tests.Visual;

namespace osu.Game.Rulesets.Breve.Showcase
{
    /// <summary>
    /// Plays <see cref="ShowcaseBeatmap"/> with autoplay in a window, and saves each frame as a PNG in a folder.
    /// scripts/make-media.sh turns the frames into the README's video, GIF and screenshots.
    /// </summary>
    /// <remarks>
    /// Usage: <c>osu.Game.Rulesets.Breve.Showcase &lt;output folder&gt; [start time] [end time]</c>, with the times in milliseconds,
    /// so that a short stretch can be captured to check a change.
    /// </remarks>
    public static class Program
    {
        [STAThread]
        public static int Main(string[] args)
        {
            if (args.Length == 0)
            {
                Console.Error.WriteLine("Usage: osu.Game.Rulesets.Breve.Showcase <output folder> [start time] [end time]");
                return 2;
            }

            // Capturing takes longer than the 10 seconds test steps are normally allowed to wait for.
            Environment.SetEnvironmentVariable("OSU_TESTS_NO_TIMEOUT", "1");

            string output = Path.GetFullPath(args[0]);
            double start = args.Length > 1 ? double.Parse(args[1], CultureInfo.InvariantCulture) : 0;
            double end = args.Length > 2 ? double.Parse(args[2], CultureInfo.InvariantCulture) : ShowcaseBeatmap.CAPTURE_END_TIME;

            int result = 0;
            string storagePath;

            using (DesktopGameHost host = Host.GetSuitableDesktopHost(@"osu-breve-showcase"))
            {
                var runner = new OsuTestScene.OsuTestSceneTestRunner();
                var capture = new ShowcaseCapture(output, start, end);

                // The window has to run on the main thread on macOS, so the capture is driven from another.
                Task.Run(() =>
                {
                    try
                    {
                        while (!runner.IsLoaded)
                            Thread.Sleep(10);

                        capture.AddCaptureSteps();
                        runner.RunTestBlocking(capture);
                    }
                    catch (Exception e)
                    {
                        Console.Error.WriteLine(e);
                        result = 1;
                    }
                    finally
                    {
                        host.Exit();
                    }
                });

                host.Run(runner);
                storagePath = host.Storage.GetFullPath(string.Empty);
            }

            // The game's files are only needed while capturing.
            try
            {
                Directory.Delete(storagePath, true);
            }
            catch (IOException e)
            {
                Console.Error.WriteLine($"Couldn't delete {storagePath}: {e.Message}");
            }

            return result;
        }
    }
}
