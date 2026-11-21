#!/usr/bin/env bash
# Run a boss arena headless with the game's real C# scripts and render it: OUT/<boss>.mp4 + OUT/<boss>-storyboard.png
#   tools/arena-sim/run.sh [serena|child|all] [out-dir]
# Needs the .NET 8 SDK, python3 + Pillow, ffmpeg and a CJK font (fonts-wqy-zenhei).
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; ROOT="$(cd "$HERE/../.." && pwd)"
WHICH="${1:-all}"; OUT="${2:-$ROOT/records/arena}"; mkdir -p "$OUT"
FONT="$(fc-list :lang=ko file 2>/dev/null | head -1 | cut -d: -f1)"; FONT="${FONT:-/usr/share/fonts/truetype/wqy/wqy-zenhei.ttc}"
dotnet build "$HERE/arena-sim.csproj" -c Release -nologo -v q | grep -E "error|Build succeeded" | sort -u
for b in $([ "$WHICH" = all ] && echo "serena child" || echo "$WHICH"); do
  dotnet "$HERE/bin/Release/net8.0/arena-sim.dll" "$b" "$ROOT" 2>"$OUT/$b.log" | python3 -I -W ignore "$HERE/overlay.py" 368 208 "$OUT/$b" "$FONT"
  cat "$OUT/$b.log"
done
