#!/usr/bin/env bash
# Remakes the media in the README: plays the showcase beatmap with autoplay in a window, captures every frame,
# and turns the frames into media/showcase.mp4, media/showcase.gif and the screenshots.
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

echo "Capturing the showcase..."

if ! dotnet run --project osu.Game.Rulesets.Breve.Showcase -c Release -- "$frames/png" > "$frames/capture.log" 2>&1; then
  tail -n 40 "$frames/capture.log" >&2
  exit 1
fi

echo "Encoding the video and GIF..."

ffmpeg -v error -y -framerate $fps -i "$frames/png/frame%05d.png" \
  -c:v libx264 -preset slow -crf 22 -pix_fmt yuv420p -movflags +faststart \
  media/showcase.mp4

# 800 pixels wide at 25 fps keeps the GIF to a few megabytes, light enough for the README.
ffmpeg -v error -y -framerate $fps -i "$frames/png/frame%05d.png" \
  -vf "fps=25,scale=800:-1:flags=lanczos,split[a][b];[a]palettegen=stats_mode=diff:max_colors=128[p];[b][p]paletteuse=dither=bayer:bayer_scale=4:diff_mode=rectangle" \
  media/showcase.gif

echo "Saving the screenshots..."

# Saves the frame at a time in the showcase beatmap, in milliseconds, as a screenshot.
screenshot() {
  local frame=$(( $2 * fps / 1000 ))
  magick "$frames/png/$(printf 'frame%05d.png' "$frame")" -resize 1280x720 -strip "media/$1.png"
}

screenshot hold 10200
screenshot drag-chain 7400
screenshot double-hold 19467

ls -l media
