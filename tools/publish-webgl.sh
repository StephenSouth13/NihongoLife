#!/usr/bin/env bash
# Checks a NihongoLife WebGL output folder and prints how to publish it.
# Does not build Unity and does not push. See docs/WEBGL_DEPLOY.md.
set -euo pipefail

DIR="Build/Release/WebGL"
CHECK=0

usage() {
  echo "Usage: tools/publish-webgl.sh [output-folder] [--check]"
  echo "Default folder: Build/Release/WebGL"
}

while [[ $# -gt 0 ]]; do
  case "$1" in
    --check) CHECK=1; shift ;;
    -h|--help) usage; exit 0 ;;
    *) DIR="$1"; shift ;;
  esac
done

if [[ ! -d "$DIR" ]]; then
  echo "Missing folder: $DIR"
  echo "Build with Unity -executeMethod NihongoLife.Editor.ReleaseBuild.BuildWebGL"
  exit 1
fi

fail=0
need() {
  if [[ ! -e "$DIR/$1" ]]; then
    echo "Missing $1"
    fail=1
  fi
}

need index.html
need TemplateData/style.css
need zones/index.json
need _headers
need vercel.json
need .htaccess

if ! compgen -G "$DIR/Build/*.wasm" > /dev/null && ! compgen -G "$DIR/Build/*.unityweb" > /dev/null; then
  echo "Missing Build/*.wasm or Build/*.unityweb"
  fail=1
fi

# Comments may mention the header. Only a real assignment is a failure.
if grep -n -E '^[[:space:]]*[^#].*Content-Encoding' "$DIR/_headers" "$DIR/.htaccess" 2>/dev/null \
  || grep -n '"Content-Encoding"' "$DIR/vercel.json" 2>/dev/null; then
  echo "A hosting file sets Content-Encoding. Brotli fallback is on: do not send Content-Encoding: br."
  fail=1
fi

if [[ "$fail" -ne 0 ]]; then
  exit 1
fi

echo "WebGL output looks complete: $DIR"
echo
echo "Do not send Content-Encoding: br. Unity inflates .unityweb itself."
echo "Publish the whole folder, including zones/."
echo
echo "Netlify:    netlify deploy --dir=\"$DIR\" --prod"
echo "Vercel:     cd \"$DIR\" && vercel --prod"
echo "GitHub Pages: upload this folder. Pages cannot set custom headers; fallback decompression covers that."
echo
echo "Manual check: loading bar, fullscreen, portrait hint, wasm content type, zones/index.json not cached stale."

if [[ "$CHECK" -eq 1 ]]; then
  exit 0
fi
