# App Store listing and release – Belot (iPhone)

App Store Connect app **Belot: Bulgarian Card Game** · Apple ID `6831489651` · bundle
`com.nksolutions.belot` · SKU `belot-ios` · seller NK Solutions EOOD, developer name NK Games
Studio. The pipeline follows the Gods & Magic one (its `store/APP_STORE.md`), with the differences
a public repository and a MAUI app need. The Google Play counterpart is `../google-play`.

## Files

- `<locale>.json` – each locale's `name`, `subtitle`, `keywords`, `promotionalText` and
  `description`, plus `whatsNew` from the second version on (Apple refuses it on the first).
- `settings.json` – the app-wide answers: category, age rating, content rights, copyright, URLs,
  review notes, each locale's screenshot folder and order, and the excluded territories.
- `../../tools/AppStoreRelease/` – `listing.mjs` (checks the texts; `--apply` writes them and the
  settings), `screenshots.mjs` (uploads a set), `availability.mjs` (price and territories, set
  once), `release.mjs` (the version; `--submit` sends it to App Review), `setup-signing.mjs` (bundle
  ID, profile, GitHub environment and secrets; run it again once a year, before the profile
  expires), and the scripts the workflow runs on the Mac.
- `../../.github/workflows/ios.yml` – the simulator pass and the signed upload.

**Keys.** This repository is public, so nothing secret enters it. The scripts read the team's key
from the folder `BELOT_APPLE_KEYS` names, by default `../GodsAndMagic/keys/apple` beside this
repository (`asc.json`, `AuthKey_<id>.p8`, and the team's one Apple Distribution certificate, which
Gods & Magic's `setup-signing.mjs` issues and renews). `.gitignore` refuses `keys/`, `*.p8`, `*.p12`
and `*.mobileprovision`. The workflow gets them from the secrets of the GitHub environment
`app-store`, open only to `master` and only after its reviewer (the owner) approves the run:
other accounts can write to this repository, and the key is an Admin key.

## Releasing a version

Bump `ApplicationDisplayVersion` in `Belot.UI.csproj` (`ApplicationVersion` is Android's; iOS takes
the workflow's run number).

```bash
gh workflow run ios.yml --ref master -f task=screenshots   # when the screens changed; artifact app-store-screenshots
gh workflow run ios.yml --ref master -f task=upload        # approve it under the run's "Review deployments"
node tools/AppStoreRelease/release.mjs                     # the build shows here within minutes
node tools/AppStoreRelease/release.mjs --create            # the version, released to everyone once approved
# bring whatsNew up to date in every <locale>.json, then:
node tools/AppStoreRelease/listing.mjs --apply
node tools/AppStoreRelease/screenshots.mjs <artifact folder>
node tools/AppStoreRelease/release.mjs --submit            # attach the build and send it to App Review
```

A version's release type is `AFTER_APPROVAL` with no phased release, so everybody gets it as soon
as Apple approves it; `--submit` refuses anything else.

## What is set

- **Locales.** The App Store has no Bulgarian, and Apple's table gives the Bulgarian storefront
  **English (U.K.)** alone. So `en-GB` carries the Bulgarian listing and Bulgarian screenshots, with
  an English paragraph at the end for the other storefronts that read it (the U.K., and the
  countries without a language of their own), and `en-US`, the primary language, the English
  listing and screenshots. The app itself declares both languages (`CFBundleLocalizations` and the
  `.lproj` folders), so the page lists them.
- **Name.** `Belot` and `Белот` are taken on the App Store, which wants names unique, hence the
  longer store names. The home screen says Belot or Белот, by the phone's language.
- **iPhone only**, portrait, iOS 15 or newer, as Gods & Magic: on an iPad the app runs as an iPhone
  app, and no iPad screenshots are needed. The table's layout is a phone's, so iPad support would be
  a design change more than a build setting.
- **Category:** Games, subcategories Card and Strategy.
- **Age rating:** every answer none or no, as the game has no betting, money, chat or user content
  anyone else sees. Custom names stay on the device. Links leave for Safari
  (`Pages/ExternalBrowser.cs`), so there is no open web access in the app.
- **Price and availability:** free; every territory except China mainland, which requires a
  government approval number for games; new territories are added automatically.
- **App Privacy:** Data Not Collected. It has no API: App Store Connect → App Privacy. Keep it in
  step with the privacy policy.
- **App Review:** the contact is read from the team's other app and is never in this repository;
  the notes come from `settings.json`.

## The iOS build

`Platforms/iOS` holds the app delegate, `Info.plist` and the privacy manifest. On a Mac the project
targets `net10.0-ios` alone. On Windows, `BelotIos=true` in the environment gives a compile check:

```powershell
$env:BelotIos='true'
dotnet restore src/UI/Belot.UI/Belot.UI.csproj --force -p:StoreShots=true
dotnet msbuild src/UI/Belot.UI/Belot.UI.csproj -t:Compile -p:TargetFramework=net10.0-ios -p:Configuration=Release -p:StoreShots=true -p:IsMacEnabled=false
$env:BelotIos=$null
dotnet restore src/UI/Belot.UI/Belot.UI.csproj --force    # back to the normal targets
```

Differences from Android:

- **No Back button.** Every page has its own, but iOS's swipe from the edge pops a page without
  asking, so `AppShell.OnNavigating` refuses it on the table; the Menu button asks first as ever.
  The simulator pass checks that the Shell refuses it.
- **Dark always** (`UIUserInterfaceStyle`): every page is dark felt, so native pickers and the
  keyboard match it, and the status bar stays light.
- **Ahead of time.** iOS has no JIT. Release builds compile everything ahead of time and keep the
  interpreter for what the compiler could not foresee (`MtouchInterpreter=-all`), rather than crash.
  Measured on `macos-26` (November 2026), for a device: 59 MB and 23.4 MB zipped with it, 52 MB and
  20.8 MB without, both in about 8 minutes. The simulator is another matter, keeping every assembly
  whole (476 MB and 47 minutes with it, 107 MB and 8 minutes without), so the simulator pass runs
  without it, which is also the stricter check.
- **The felt covers the whole screen** (`Pages/PageBackground.cs`): a gradient on a page's content
  would end in a hard edge at the safe area, and result screens darken the page around their dimming.
- **Text size.** `AppDelegate` follows Dynamic Type as `MainActivity` follows the font scale.
- **The workload is pinned** to set `10.0.401` (iOS SDK 26.5, Xcode 26.6) in `select-xcode.sh`:
  newer sets carry iOS SDK 27, which needs Xcode 27, and GitHub's newest image, `macos-26`, stops at
  26.6. Move it when GitHub has `macos-27`. App Store Connect accepts Xcode 26 builds as of November
  2026; building with the iOS 27.1 SDK would be what earns the page the iPhone Duo badge.

## Screenshots and the simulator pass

`Platforms/iOS/StoreShotsDriver.cs`, built only with `-p:StoreShots=true` (which also targets the
simulator, so it cannot reach a store build), plays the real app on the newest 6.9" iPhone
simulator on an Apple Silicon runner, where the build runs ahead-of-time compiled like a device's:

1. three whole games against each of the five levels, through the table's own commands, following
   the app's own hints at no pace, so the AI's code paths run and the statistics hold real games;
2. the eight store scenes on the real pages, in each language: it writes a scene's name to a file
   in the app's Documents, and `capture-shots.sh` takes it with `simctl io` (1320×2868, the 6.9"
   iPhone's size, status bar set to Apple's usual 9:41, then flattened, since Apple refuses an alpha
   channel), with `09-settings` for checking only;
3. a run with no language chosen on a Bulgarian iPhone, which must start in Bulgarian.

The artifact holds the shots, each pass's `log.txt` and, after a failure, the crash reports.

**This is a simulator, not a device.** Nothing here has run on a real iPhone. App Review's device
is the first; install it from TestFlight on a phone before a release that changes the iOS layer.

## When the app goes live

The first approved version is released automatically. Once its page answers at
https://apps.apple.com/app/id6831489651:

- add the App Store link beside Google Play on the publisher site
  (`nksolutions.com` repository, `public/belot/index.html`), which still says "for Android";
- add it to the repository's README and to ednaigra.com's Belot page if that links the app.

## Gotchas

- Creating an app has no API (`POST /v1/apps` answers 403): App Store Connect → Apps → New App,
  with the bundle ID `setup-signing.mjs` registered.
- Availability must name every territory, with `available: false` for the excluded ones; a list that
  leaves them out gets a 409.
- A new app's first version already exists as `1.0`; `release.mjs --create` renames it.
- Creating a locale's app info localization also creates its version localization.
- `screenshots.mjs` waits five minutes per locale for Apple to process its images; read
  `assetDeliveryState` before retrying, since a retry deletes the set and starts over.
- App Store text: no emoji, no em dash, and no other platform or store named (`listing.mjs` checks).
