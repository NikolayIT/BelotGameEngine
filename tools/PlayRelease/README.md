# Belot Google Play release tools

Run commands from the repository root. These tools target `com.nksolutions.belot`. The API workflow is deliberately specific to the first **1.0 / version code 1** release; review and update its guards before preparing a later version.

## Build a signed AAB

Requirements: Windows, PowerShell 7, .NET 10 with the Android MAUI workload / API 36 SDK, and Java `keytool` and `jarsigner` on PATH. This release used .NET SDK **10.0.401**, Android pack **36.1.69**, MAUI **10.0.70** and JDK **21.0.2**. Keep the existing publisher upload keystore and its passwords outside Git.

Create two restricted, temporary UTF-8 password files outside the repository. The caller owns their cleanup; the build script reads existing files and never creates or deletes keys or password files. Pass paths, never password values:

```powershell
.\tools\PlayRelease\build-android.ps1 `
    -KeystorePath C:\Private\Belot\upload.keystore `
    -KeyAlias upload `
    -StorePasswordFile C:\Private\Belot\store-password.txt `
    -KeyPasswordFile C:\Private\Belot\key-password.txt `
    -VersionName 1.0 -VersionCode 1
```

The script refuses missing/empty signing inputs and Android Debug certificates, publishes Release with warnings treated as errors, and verifies the signature, selected certificate, package/version, target API 36 and both ABIs. Defaults produce:

- `artifacts/play-release-1.0/publish/com.nksolutions.belot-Signed.aab`
- `bundle-manifest.json`, `android-publish.log`, `upload-certificate.log`, `aab-signature.log` in its parent directory.

Use `-OutputDirectory` to choose another output folder. Do not pass a global `RuntimeIdentifiers` override: it propagates into the referenced `netstandard2.0` projects. The Android SDK defaults already produce arm64 and x64; the script checks the actual bundle contents. The `file:` signing-password mechanism is supported for AAB publishing; the `env:` prefix is not. See [Microsoft's Android publish guide](https://learn.microsoft.com/en-us/dotnet/maui/android/deployment/publish-cli?view=net-maui-10.0).

Remove the temporary password files in the caller's `finally` block after success or failure. Keep build logging at minimal verbosity; do not record a diagnostic/binlog build containing signing properties. Jarsigner's self-signed / no-timestamp diagnostics are distinct from compiler warnings; confirm the signature and expected certificate fingerprint.

## Validate before upload

```powershell
dotnet test src/Tests/Belot.Engine.Tests -c Release -p:TreatWarningsAsErrors=true
dotnet test src/Tests/Belot.AI.ClaudePlayer.Tests -c Release -p:TreatWarningsAsErrors=true
dotnet test src/Tests/Belot.UI.Tests -c Release -p:TreatWarningsAsErrors=true
dotnet build src/UI/Belot.UI/Belot.UI.csproj -c Release -f net10.0-windows10.0.19041.0 -p:TreatWarningsAsErrors=true
python -B -m unittest discover -s tools/StoreScreenshots -p test_verify.py -v
python tools/StoreScreenshots/verify.py --check-only
```

Run shared-project builds sequentially. Check the exact AAB with the SDK's `bundletool.jar`; for the recorded installation:

```powershell
$bundletool = 'C:\Program Files\dotnet\packs\Microsoft.Android.Sdk.Windows\36.1.69\tools\bundletool.jar'
$bundle = 'artifacts/play-release-1.0/publish/com.nksolutions.belot-Signed.aab'
java -jar $bundletool validate "--bundle=$bundle"
java -jar $bundletool dump manifest "--bundle=$bundle" --module=base
Get-FileHash -LiteralPath $bundle -Algorithm SHA256
```

Check every native library's ELF LOAD alignment and the derived APK with SDK 35+ `zipalign -c -P 16 4`. [Android documents these checks](https://developer.android.com/guide/practices/page-sizes#elf-alignment). The release's local `artifacts/play-release-1.0/check-native-bundle.py` records all segments from the fixed AAB hash and distinguishes safe whole-LOAD RELRO coverage from a protection range that overlaps mutable data. It is retained local evidence, not a general checked-in validator. A static pass does not replace execution on a 16 KB device.

For a native content check, use `bundletool build-apks` on this exact AAB and verify the derived APK hash/signature before installation. If using Test's existing debug key to preserve its data, record that distinction: this validates release content but does not test the production certificate or Play delivery. Use only the authorized **BlueStacks Test / Pie64_2 / 127.0.0.1:5575**; do not operate The Tower / `emulator-5554`.

## Listings and Play API

`play-release.mjs` uses Node.js with built-in `fetch` and no npm dependencies. Supply an existing Play service-account JSON via `--key`; the key remains outside the repository. It signs an OAuth assertion locally and sends it to Google's token endpoint; it never prints the key or access token.

Source files are under `store/google-play/`: `listings/{bg,en-US}/*.txt`, `graphics/*.png`, and eight ordered `screenshots/{bg,en-US}/0*.png` per locale. The script checks text limits and image counts before opening an edit. Read the [capture guide](../../store/google-play/README.md) and [graphics guide](../../store/google-play/graphics/README.md) before replacing assets.

```powershell
$playKey = 'C:\Private\Belot\play-service-account.json'

# Read current settings/tracks/bundles, then discard the temporary edit:
node tools/PlayRelease/play-release.mjs inspect --key $playKey

# Upload BG/EN text and images, verify hashes/order, then commit the edit:
node tools/PlayRelease/play-release.mjs listings --key $playKey

# Fresh read: compare both locales and all 20 image entries, then discard the edit:
node tools/PlayRelease/play-release.mjs verify --key $playKey

# Upload AAB, check returned hash/code, and prepare a draft production release:
node tools/PlayRelease/play-release.mjs upload --key $playKey --bundle $bundle

# Select 100% production for the prepared version 1, then commit:
node tools/PlayRelease/play-release.mjs complete --key $playKey

# Read back the resulting state:
node tools/PlayRelease/play-release.mjs inspect --key $playKey
```

`listings`, `upload` and `complete` change Google Play. Run them only within an authorized release. Upload refuses to replace an existing production release; complete requires exactly the prepared version 1. `inspect` and `verify` only read app data and delete their temporary edits. All commands write receipts to `artifacts/play-release-1.0` by default; use `--receipts <folder>` to override it.

Commit uses `changesInReviewBehavior=ERROR_IF_IN_REVIEW`: this application's API requires automatic review submission and rejects `changesNotSentForReview`. The tool fails rather than cancels an existing review. Do not retry a mutation blindly after a network error; inspect current state and the saved receipt first.

Console-only setup still includes country availability, app content / target audience / IARC declarations, privacy URL and the final review flow. A `completed` track means a full rollout configuration; confirm Console review status separately before claiming submission, approval or public availability. The user must supply any required legal consent; an age-group selection does not determine the eventual IARC rating.

The first release's artifact hashes, validation results, native-test scope and current publishing status are in [store/google-play/releases/1.0.md](../../store/google-play/releases/1.0.md).
