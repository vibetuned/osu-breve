#!/usr/bin/env bash
# Remakes the media in the README: plays the showcase beatmap with autoplay in a window, captures every frame,
# and turns the frames into media/showcase.mp4, media/showcase.gif, the screenshots, and the captures of each mod.
# Needs the .NET 10 SDK, ffmpeg and ImageMagick, and a display for the game's window.
set -euo pipefail

cd "$(dirname "$0")/.."

for tool in dotnet ffmpeg magick; do
  if ! command -v "$tool" > /dev/null; then
    echo "make-media.sh needs $tool." >&2
    exit 1
  fi
done

frames=$(mktemp -d)
trap 'rm -rf "$frames"' EXIT

# The rate the frames are captured at (ShowcaseCapture.FRAME_RATE).
fps=60

# Captures a stretch of the showcase into a folder of frames: capture <folder> [start time] [end time] [mods], with times in milliseconds.
capture() {
  if ! dotnet run --project osu.Game.Rulesets.Breve.Showcase -c Release --no-build -- "$frames/$1" "${@:2}" > "$frames/$1.log" 2>&1; then
    tail -n 40 "$frames/$1.log" >&2
    exit 1
  fi
}

# Saves a captured frame as a screenshot: screenshot <folder> <capture start time> <time> <name>.
screenshot() {
  local frame=$(( ($3 - $2) * fps / 1000 ))
  magick "$frames/$1/$(printf 'frame%05d.png' "$frame")" -resize 1280x720 -strip "media/$4.png"
}

# Turns a folder of frames into an MP4 at full size and 60 fps, a fraction of the size of a GIF, for sharing: mp4 <folder> <name>.
mp4() {
  ffmpeg -v error -y -framerate $fps -i "$frames/$1/frame%05d.png" \
    -c:v libx264 -preset slow -crf 22 -pix_fmt yuv420p -movflags +faststart \
    "media/$2.mp4"
}

# Turns a folder of frames into a GIF at 25 fps, which keeps it light enough for the README: gif <folder> <width> <name>.
gif() {
  ffmpeg -v error -y -framerate $fps -i "$frames/$1/frame%05d.png" \
    -vf "fps=25,scale=$2:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff:max_colors=128[p];[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle" \
    "media/$3.gif"
}

echo "Building the showcase..."
dotnet build osu.Game.Rulesets.Breve.Showcase -c Release -v:q -nologo > /dev/null

echo "Capturing the showcase..."
capture normal

echo "Capturing the mods..."
# The short sliders at the end of the showcase beatmap, where Expert and Flicks make a difference, and the first sweeps with Border Lines.
# The expert chart is played from the start, like the normal one, so that their combo and score compare.
capture expert 0 20800 EX
capture flicks 20300 24200 FK
capture border-lines 2900 6500 BL

echo "Encoding the videos and GIFs..."

mp4 normal showcase
mp4 flicks mod-flicks
mp4 border-lines mod-border-lines

gif normal 800 showcase
gif flicks 640 mod-flicks
gif border-lines 640 mod-border-lines

echo "Saving the screenshots..."

screenshot normal 0 10200 hold
screenshot normal 0 7400 drag-chain
screenshot normal 0 19467 double-hold

# The same moment in the normal and expert charts, as the short sliders start.
screenshot normal 0 20800 chart-normal
screenshot expert 0 20800 chart-expert

ls -l media
