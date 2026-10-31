using System;
using osu.Framework;
using osu.Framework.Platform;
using osu.Game.Tests;

namespace osu.Game.Rulesets.Breve.Tests
{
    /// <summary>
    /// Opens osu!'s test browser, to play osu!breve's test scenes by hand.
    /// </summary>
    public static class VisualTestRunner
    {
        [STAThread]
        public static int Main(string[] args)
        {
            // Keeps its files apart from the installed osu!'s, as osu!'s own test runner does.
            using (DesktopGameHost host = Host.GetSuitableDesktopHost(@"osu-development"))
            {
                host.Run(new OsuTestBrowser());
                return 0;
            }
        }
    }
}
