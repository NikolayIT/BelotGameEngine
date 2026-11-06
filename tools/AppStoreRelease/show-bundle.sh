#!/usr/bin/env bash
# Prints what App Review and the App Store read from an app bundle, and fails when the settings
# they depend on are missing.
set -euo pipefail

app="$1"
plist="$app/Info.plist"
buddy() { /usr/libexec/PlistBuddy -c "Print :$1" "$plist" 2>/dev/null || echo "(missing)"; }

du -sh "$app"
echo "Largest files:"
find "$app" -type f -exec du -k {} + | sort -rn | head -14 | sed "s|$app/||"
for key in CFBundleIdentifier CFBundleName CFBundleDisplayName CFBundleShortVersionString CFBundleVersion \
  MinimumOSVersion UIDeviceFamily UISupportedInterfaceOrientations UIUserInterfaceStyle UIStatusBarStyle \
  ITSAppUsesNonExemptEncryption CFBundleLocalizations UILaunchStoryboardName CFBundleIconName; do
  echo "$key: $(buddy "$key" | tr '\n' ' ')"
done

ls -d "$app"/*.lproj
ls "$app" | grep -i -E 'PrivacyInfo|AppIcon|Assets.car' || true

test -f "$app/PrivacyInfo.xcprivacy"
test -f "$app/bg.lproj/InfoPlist.strings"
test "$(buddy CFBundleIdentifier)" = "com.nksolutions.belot"
test "$(buddy ITSAppUsesNonExemptEncryption)" = "false"
echo "Bundle checks pass."
