#!/usr/bin/env bash
# Check the pixelfx → Unity pipeline without Unity (for cloud sessions / CI):
#  1. compile Assets/PixelFx against Unity 2021.1 reference DLLs (NuGet package Unity3D.SDK)
#  2. drive the real sheet parser + frame timing through a 60 fps game loop for one exported sheet
#  3. composite the frames it picks into a stand-in game scene: OUT-strip.png and OUT.gif (needs python3 + Pillow)
# Needs the .NET 8 SDK. Usage: tools/pixelfx/verify/run.sh [sheet.json] [out-prefix]
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; ROOT="$(cd "$HERE/../../.." && pwd)"
JSON="${1:-$ROOT/Assets/PixelFx/Sheets/explosions/small-explosion.json}"; OUT="${2:-$(mktemp -d)/ingame}"
WORK="$(mktemp -d)"; trap 'rm -rf "$WORK"' EXIT
command -v dotnet >/dev/null || { echo "install the .NET 8 SDK (e.g. apt-get install -y dotnet-sdk-8.0)"; exit 1; }
CACHE="${XDG_CACHE_HOME:-$HOME/.cache}/pixelfx-verify/unity3d.sdk-2021.1.14.1"
if [ ! -f "$CACHE/lib/UnityEngine.dll" ]; then
  mkdir -p "$CACHE" && curl -sSL -o "$CACHE/sdk.nupkg" https://api.nuget.org/v3-flatcontainer/unity3d.sdk/2021.1.14.1/unity3d.sdk.2021.1.14.1.nupkg
  (cd "$CACHE" && unzip -oq sdk.nupkg 'lib/*')
fi
dotnet build "$HERE/compile/compile.csproj" -nologo -v q -p:UNITY_LIB="$CACHE/lib" -o "$WORK/c" | grep -E "error|warning|Build succeeded" | sort -u
dotnet run --project "$HERE/sim/sim.csproj" -v q -- "$JSON" "$WORK/ticks.txt"
python3 -I "$HERE/scene.py" "${JSON%.json}.png" "$WORK/ticks.txt" "$OUT" >/dev/null && echo "scene: $OUT-strip.png $OUT.gif"
dotnet run --project "$HERE/combat/combat.csproj" -v q
