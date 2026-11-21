#!/usr/bin/env bash
# Re-vendor pixelfx into third_party/pixelfx at the commit pinned in PIXELFX_VERSION.
# The gallery (and the Patch Pixel font in gallery/fonts, which is NOT MIT and must not ship) is excluded,
# along with the tools that need it (build-gallery.mjs, serve.mjs) and the README media.
set -euo pipefail
HERE="$(cd "$(dirname "$0")" && pwd)"; ROOT="$(cd "$HERE/../.." && pwd)"
COMMIT="$(sed -n 's/^commit=//p' "$HERE/PIXELFX_VERSION")"; VERSION="$(sed -n 's/^version=//p' "$HERE/PIXELFX_VERSION")"
TMP="$(mktemp -d)"; trap 'rm -rf "$TMP"' EXIT
git -C "$TMP" init -q && git -C "$TMP" remote add origin https://github.com/patch-gg/pixelfx.git
git -C "$TMP" fetch -q --depth 1 origin "$COMMIT" && git -C "$TMP" checkout -q FETCH_HEAD
test "$(git -C "$TMP" rev-parse HEAD)" = "$COMMIT"
DEST="$ROOT/third_party/pixelfx"
# Keep effects authored in this repo (files under src/fx that upstream doesn't have) across re-vendoring.
if [ -d "$DEST/src/fx" ]; then
  (cd "$DEST/src/fx" && find . -type f -name '*.js') | while read -r f; do
    if [ ! -e "$TMP/src/fx/$f" ]; then mkdir -p "$(dirname "$TMP/src/fx/$f")"; cp "$DEST/src/fx/$f" "$TMP/src/fx/$f"; echo "kept local effect src/fx/${f#./}"; fi
  done
fi
rm -rf "$DEST"; mkdir -p "$DEST/docs"
cp -R "$TMP/src" "$TMP/tools" "$DEST/"
cp "$TMP/LICENSE" "$TMP/NOTICE.md" "$TMP/package.json" "$TMP/README.md" "$DEST/"
cp "$TMP/docs/AUTHORING.md" "$DEST/docs/" && cp "$TMP/docs/CATALOG.md" "$DEST/docs/"
rm -f "$DEST/tools/build-gallery.mjs" "$DEST/tools/serve.mjs"
mkdir -p "$DEST/upstream-skill" && cp "$TMP/skills/pixelfx/SKILL.md" "$DEST/upstream-skill/SKILL.md"
printf 'pixelfx %s @ %s\nhttps://github.com/patch-gg/pixelfx\nVendored by tools/pixelfx/vendor.sh. Do not edit upstream files; effects authored here go in src/fx/<category>/ as new files (vendor.sh keeps them).\nExcluded: gallery/ (Patch Pixel font, not MIT), docs/readme, .github, build-gallery.mjs, serve.mjs.\n' "$VERSION" "$COMMIT" > "$DEST/VENDORED.txt"
# re-register effects authored here in the registry and catalog
(cd "$DEST" && node tools/index.mjs >/dev/null)
if grep -rqi "PatchPixel\|woff2" "$DEST"; then echo "Patch Pixel font reference found in vendored files" >&2; exit 1; fi
echo "vendored pixelfx $VERSION ($COMMIT) -> third_party/pixelfx"
