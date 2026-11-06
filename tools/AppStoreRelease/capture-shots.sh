#!/usr/bin/env bash
# Runs the store-shots build (Platforms/iOS/StoreShotsDriver.cs) on a 6.9" iPhone simulator and
# takes the screenshots it asks for: English and Bulgarian, plus a check of the language a
# Bulgarian iPhone gets on its own.
#
#   capture-shots.sh <path to the .app> <output folder>
set -euo pipefail

app="$1"
out="$2"
bundle="com.nksolutions.belot"
mkdir -p "$out"

udid="$(xcrun simctl list devices available -j | python3 -c '
import json, re, sys
devices = json.load(sys.stdin)["devices"]
best = None
for runtime, items in devices.items():
    if "iOS" not in runtime:
        continue
    version = [int(x) for x in re.findall(r"\d+", runtime.split("iOS")[-1])]
    for item in items:
        if "Pro Max" in item["name"]:
            key = (version, item["name"])
            if best is None or key > best[0]:
                best = (key, item["udid"], item["name"], runtime)
print(best[1])
print(best[2], best[3], file=sys.stderr)
')"
echo "Simulator: $udid"
xcrun simctl boot "$udid" || true
xcrun simctl bootstatus "$udid" -b

# Runs one pass of the driver: run <name> <language> <games> <scenes> [launch arguments...]
run() {
  local name="$1" language="$2" games="$3" scenes="$4"
  shift 4
  local folder="$out/$name"
  mkdir -p "$folder"

  xcrun simctl uninstall "$udid" "$bundle" || true
  xcrun simctl install "$udid" "$app"
  local data
  data="$(xcrun simctl get_app_container "$udid" "$bundle" data)/Documents"
  mkdir -p "$data"

  echo "== $name: language '$language', $games game(s) per level"
  SIMCTL_CHILD_BELOT_SHOT_LANGUAGE="$language" \
  SIMCTL_CHILD_BELOT_SHOT_GAMES="$games" \
  SIMCTL_CHILD_BELOT_SHOT_SCENES="$scenes" \
    xcrun simctl launch "$udid" "$bundle" "$@"

  local deadline=$((SECONDS + 2400)) failed=0 polls=0
  while [ ! -f "$data/shots-done.txt" ]; do
    if [ -f "$data/shot-request.txt" ]; then
      local shot
      shot="$(cat "$data/shot-request.txt")"
      xcrun simctl io "$udid" screenshot --type=png "$folder/$shot.png" 2> /dev/null
      rm -f "$data/shot-request.txt"
      echo "  took $shot"
    fi
    if [ "$SECONDS" -gt "$deadline" ]; then
      echo "::error::The $name pass never finished."
      failed=1
      break
    fi
    # Now and then, as a spawn is slow and a stalled one holds up the screenshots. Not grep -q:
    # closing the pipe early kills launchctl, which pipefail reports as a failure.
    polls=$((polls + 1))
    if [ $((polls % 50)) = 0 ] && ! xcrun simctl spawn "$udid" launchctl list 2> /dev/null | grep "UIKitApplication:$bundle" > /dev/null; then
      sleep 2
      if [ ! -f "$data/shots-done.txt" ]; then
        echo "::error::The app stopped during the $name pass."
        failed=1
        break
      fi
    fi
    sleep 0.2
  done

  cp "$data/shots-log.txt" "$folder/log.txt" 2> /dev/null || true
  cat "$folder/log.txt" 2> /dev/null || true
  if [ -f "$data/shots-error.txt" ]; then
    cp "$data/shots-error.txt" "$folder/error.txt"
    echo "::error::The $name pass failed: $(head -3 "$folder/error.txt")"
    failed=1
  fi
  if [ "$failed" = 1 ]; then
    mkdir -p "$out/crashes"
    cp -R "$HOME/Library/Logs/DiagnosticReports/." "$out/crashes/" 2> /dev/null || true
    xcrun simctl spawn "$udid" log show --last 10m --style compact --predicate "process CONTAINS 'Belot'" > "$out/crashes/$name-log.txt" 2> /dev/null || true
    exit 1
  fi
  xcrun simctl terminate "$udid" "$bundle" || true
}

# A clean status bar, as on Apple's own product pages.
xcrun simctl status_bar "$udid" override --time "9:41" --dataNetwork wifi --wifiMode active --wifiBars 3 \
  --cellularMode active --cellularBars 4 --batteryState charged --batteryLevel 100

store_scenes="01-gameplay,02-bidding,03-hint,04-declarations,05-result,06-setup,07-statistics,08-rules,09-settings"
run en en "${BELOT_SHOT_GAMES:-3}" "$store_scenes"
run bg bg "${BELOT_SHOT_GAMES:-3}" "$store_scenes"

# No language chosen in the app, a Bulgarian iPhone: the app must start in Bulgarian.
run device-bg "" 0 "" -AppleLanguages "(bg)" -AppleLocale "bg_BG"
grep -q 'Language without a saved choice: bg' "$out/device-bg/log.txt" || { echo "::error::A Bulgarian iPhone did not get Bulgarian."; exit 1; }

# App Store Connect refuses images with an alpha channel, and simctl's all have one (fully opaque).
python3 -m venv "$out/.venv"
"$out/.venv/bin/pip" install --quiet pillow==11.3.0
"$out/.venv/bin/python" - "$out" <<'PY'
import pathlib, sys
from PIL import Image
problems = 0
for path in sorted(pathlib.Path(sys.argv[1]).glob("[!.]*/*.png")):
    with Image.open(path) as image:
        image.load()
        size = image.size
        alpha = image.getchannel("A").getextrema() if image.mode == "RGBA" else (255, 255)
        flat = image.convert("RGB")
    flat.save(path, optimize=True)
    print(path.relative_to(sys.argv[1]), size, "alpha", alpha)
    if size != (1320, 2868) or alpha != (255, 255):
        problems += 1
sys.exit(1 if problems else 0)
PY
rm -rf "$out/.venv"
