#!/usr/bin/env bash
# Installs the MAUI iOS workload and selects the Xcode that its iOS SDK asks for, on a GitHub
# runner: .NET for iOS builds only with the Xcode it was made for (major.minor).
#
# The workload set is pinned: newer ones carry iOS SDK 27, which needs Xcode 27, and the newest
# hosted image, macos-26, stops at Xcode 26.6. Move it on when GitHub has a macos-27 image.
set -euo pipefail

dotnet workload install maui-ios --version 10.0.401
dotnet workload list

props="$(find "$(dirname "$(readlink -f "$(command -v dotnet)")")/packs" -path '*Microsoft.iOS.Sdk.net10.0_*' -name 'Microsoft.iOS.Sdk.Versions.props' | sort -V | tail -1)"
echo "iOS SDK: $props"
wanted="$(sed -n 's/.*<_RecommendedXcodeVersion>\([0-9.]*\)<.*/\1/p' "$props")"
echo "It recommends Xcode $wanted. On this runner:"
ls -d /Applications/Xcode*.app

for candidate in "/Applications/Xcode_$wanted.app" "/Applications/Xcode_$wanted.0.app"; do
  if [ -d "$candidate" ]; then
    sudo xcode-select -s "$candidate/Contents/Developer"
    break
  fi
done

xcodebuild -version
xcrun simctl list runtimes | grep -i ios || true
