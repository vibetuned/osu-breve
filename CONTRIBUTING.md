# Contributing to osu!breve

## Building and testing

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download).

```sh
dotnet build                                          # builds the ruleset, its tests and the showcase
dotnet test                                           # runs the tests
dotnet run --project osu.Game.Rulesets.Breve.Tests    # opens osu!'s test browser, to play the test scenes
```

The DLL is built to `osu.Game.Rulesets.Breve/bin/Debug/net10.0/`. To try a change, either copy it into osu!'s `rulesets` folder as in the [README](README.md#installing), or open `OsuGame` in the test browser: it runs the full game with osu!breve, and you can import beatmaps by dragging `.osz` files onto the window.

osu!breve is built against the osu! release set by `OsuVersion` in [Directory.Build.props](Directory.Build.props). When osu! changes its ruleset API and stops loading osu!breve, set it to the new osu! release and fix whatever no longer builds.

## Playing on iOS and Android

osu! on phones and tablets can't load rulesets from files, so osu!breve has to be built into the app, from a checkout of [osu!](https://github.com/ppy/osu):

1. Point osu!breve at the checkout, with an untracked `osu.local.props` at the root of this repository. The path is absolute, or relative to this repository:

   ```xml
   <Project>
     <PropertyGroup>
       <OsuRepository>../osu</OsuRepository>
     </PropertyGroup>
   </Project>
   ```

   osu!breve then builds against osu! from source, rather than its published packages, so that the app has a single copy of osu!.

2. In the checkout, add osu!breve to the app in the untracked `osu.Android/osu.Android.csproj.user`, or `osu.iOS/osu.iOS.csproj.user`:

   ```xml
   <Project>
     <Import Project="path/to/osu-breve/osu.Game.Rulesets.Breve/BundleBreve.targets" />
   </Project>
   ```

3. Build and install the app as osu!'s own instructions say. If osu!breve isn't pointed at that checkout, the build stops and says so.

Several rulesets can be built into the same app this way, each with its own import.

## Code style

osu!breve follows osu!'s code style and analysis rules, from osu!'s [.editorconfig](.editorconfig) and [CodeAnalysis](CodeAnalysis) folder, so that it stays ready to be offered to osu! itself. The build reports anything that breaks them.

## How the code is laid out

osu!breve plugs into osu! like any ruleset. Its beatmap converter turns each osu! beatmap into notes when it's played, and its drawable ruleset shows and judges them. The difficulty calculator rates the converted notes, and replays record the lanes and taps pressed.

The idea everything else builds on is the *page*: one sweep of the scan line across the playfield. [PageTiming](osu.Game.Rulesets.Breve/Beatmaps/PageTiming.cs) splits a beatmap's timeline into pages from its tempo and meter. A note's height is where the scan line will be on its page when the note should be hit.

```
osu-breve/
├── osu.Game.Rulesets.Breve/                  The ruleset, built into the DLL players install
│   ├── BreveRuleset.cs                       What osu! loads: the name, icon, mods, key bindings and judgement names
│   ├── BreveRulesetIcon.cs                   The crossed arrows icon, drawn into a texture
│   ├── BreveInputManager.cs                  The actions notes respond to: two taps aimed with the cursor, and nine lanes
│   ├── BundleBreve.targets                   Builds osu!breve into osu!'s mobile apps
│   ├── Beatmaps/
│   │   ├── BreveBeatmapConverter.cs          Turns osu! objects into notes, places them in lanes, and makes doubles
│   │   ├── PageTiming.cs                     Splits the timeline into pages
│   │   ├── Page.cs                           One page: when it starts, how long it lasts, which way the line moves
│   │   └── MeterDetection.cs                 Recognises songs in 3/4 or 6/8 that their beatmap labels 4/4
│   ├── Objects/
│   │   ├── BreveHitObject.cs                 What every note has: its lane, position, size, and the page it's on
│   │   ├── Tap.cs                            A note to tap. A zigzag is two taps.
│   │   ├── Hold.cs                           A note to press and keep held
│   │   ├── HoldSegment.cs                    The part of a hold's body on one page
│   │   ├── DragNode.cs                       One node of a drag chain
│   │   ├── Flick.cs                          A note between two lanes, to move quickly across
│   │   └── Drawables/                        How notes look, and how they're judged
│   │       ├── DrawableBreveHitObject.cs     Shared by every note: its colour, and which presses are aimed at it
│   │       ├── DrawableTap.cs
│   │       ├── DrawableHold.cs               Also the hold's body, which the scan line eats up, and its progress ring
│   │       ├── DrawableDragNode.cs
│   │       └── DrawableFlick.cs              Also how a flick is told apart from a single press
│   ├── UI/
│   │   ├── DrawableBreveRuleset.cs           Brings the playfield, input and replays together
│   │   ├── BrevePlayfield.cs                 The playfield: its lanes, and where the scan line is during a page
│   │   ├── BrevePlayfieldAdjustmentContainer.cs   Scales the playfield to fit the screen
│   │   ├── ScanLine.cs                       The glowing line sweeping across the screen
│   │   ├── BorderLines.cs                    Dotted lines where the scan line turns around, for the Border Lines mod
│   │   ├── LaneActivity.cs                   When each lane was last in use, for flicks
│   │   ├── ScreenWideBand.cs                 Stretches the scan line and border lines to the edges of the screen
│   │   └── TouchLaneMapper.cs                Turns fingers on a touchscreen into lane presses
│   ├── Scoring/
│   │   └── BreveHitWindows.cs                The timing windows, forgiving like Cytus's
│   ├── Difficulty/                           The star rating
│   │   ├── BreveDifficultyCalculator.cs      Combines the skills, calibrated so the Expert chart rates like osu!
│   │   ├── BreveDifficultyAttributes.cs
│   │   ├── Preprocessing/
│   │   │   └── BreveDifficultyHitObject.cs   A note, with what the skills need to know about the notes before it
│   │   └── Skills/
│   │       ├── Tapping.cs                    How densely notes have to be pressed
│   │       └── Movement.cs                   How far and how fast hands move across the lanes
│   ├── Mods/                                 No Fail, Half Time, Double Time, Expert, Flicks, Autoplay and Border Lines
│   └── Replays/
│       ├── BreveAutoGenerator.cs             Autoplay's perfect play
│       ├── BreveReplayFrame.cs               How presses are stored in replays
│       └── BreveFramedReplayInputHandler.cs  Plays replays back
├── osu.Game.Rulesets.Breve.Tests/            The tests, and scenes to watch in the test browser
│   ├── *Test.cs                              Tests of conversion, pages, meter, layout, doubles, difficulty, hit windows,
│   │                                           replays and key bindings
│   ├── TestScene*.cs                         Autoplay, each note type, doubles, flicks, border lines, touch input,
│   │                                           playing with the mouse, the icon, and the full game
│   ├── NoteTypesBeatmap.cs                   Small .osu beatmaps the tests convert:
│   ├── DoublesBeatmap.cs                       one of each note type, each way doubles pair up,
│   ├── LayoutBeatmap.cs                        and notes that would clash in their lanes
│   ├── LegacyTestBeatmap.cs                  Decodes those beatmaps
│   ├── TimedAction.cs                        Acts on an exact gameplay frame, for tests that press notes on time
│   └── VisualTestRunner.cs                   Opens the test browser
├── osu.Game.Rulesets.Breve.Showcase/         Captures the media in the README
│   ├── ShowcaseBeatmap.cs                    A 24 second beatmap with every kind of note, ending with short sliders for the mods
│   ├── ShowcaseCapture.cs                    Plays it with autoplay, and any mods, and saves every frame
│   └── Program.cs                            Runs the capture in a window
├── scripts/
│   └── make-media.sh                         Captures the showcase and turns the frames into media/
├── media/                                    The README's GIFs, video and screenshots
├── .github/workflows/release.yml             Builds and publishes the DLL for each tag
├── CodeAnalysis/                             osu!'s analyser rules and banned APIs
├── Directory.Build.props                     Settings every project shares, including the osu! version
├── .editorconfig                             osu!'s code style
├── .gitattributes                            Line endings, as in osu!
├── .gitignore
├── app.manifest                              Windows settings for the test browser's and showcase's windows
├── global.json                               The .NET SDK version
├── osu.Game.Rulesets.Breve.sln
├── README.md                                 For players: what osu!breve is, and how to install it
├── CONTRIBUTING.md                           This file
└── LICENSE                                   The MIT License
```

## Remaking the media

The GIFs, video and screenshots in [media](media) are made by [scripts/make-media.sh](scripts/make-media.sh). It plays the showcase beatmap with autoplay in a window, on its own and with each of osu!breve's mods, captures every frame at 60 fps, and turns the frames into the video, the GIFs and the screenshots. Run it again after changing how osu!breve looks. It takes a few minutes, and needs [ffmpeg](https://ffmpeg.org) and [ImageMagick](https://imagemagick.org).

To check a change without remaking everything, capture a stretch of the showcase, with times in milliseconds, and optionally mods by their acronyms:

```sh
dotnet run --project osu.Game.Rulesets.Breve.Showcase -- frames 7000 8000
dotnet run --project osu.Game.Rulesets.Breve.Showcase -- frames 20300 24200 FK,BL
```

## Releasing

Push a tag named after the version:

```sh
git tag v2026.1031.0
git push origin v2026.1031.0
```

The [release workflow](.github/workflows/release.yml) runs the tests, builds the DLL with that version, and publishes it as a GitHub release for the tag.
