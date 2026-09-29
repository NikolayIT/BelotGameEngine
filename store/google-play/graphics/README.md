# Google Play graphics — Belot 1.0

| Upload field | File | Size |
| --- | --- | --- |
| App icon, both languages | [icon.png](icon.png) | 512 × 512 |
| Bulgarian feature graphic | [feature-bg.png](feature-bg.png) | 1024 × 500 |
| English (US) feature graphic | [feature-en-US.png](feature-en-US.png) | 1024 × 500 |

All three files are opaque 24-bit RGB PNGs. Upload only the PNGs. File hashes and the renderer version are recorded in [manifest.json](manifest.json).

The icon combines the application's existing `appicon.svg` background and `appiconfg.svg` foreground without changing the design. The feature graphics use the application's king, queen, and jack of spades, its Open Sans fonts, and its green felt and gold palette. These are promotional compositions; the separate `screenshots/` folder contains the untouched native gameplay captures.

The copy describes the shipped five difficulty levels, hints, and offline play. It makes no ranking, award, or superiority claim.

## Reproduce

From the repository root, using Windows, PowerShell 7, and Microsoft Edge:

```powershell
pwsh -NoProfile -File tools/StoreScreenshots/render_graphics.ps1
```

For another Chromium installation, pass `-BrowserPath` with the executable's absolute path. The script embeds the repository resources in temporary HTML under ignored `artifacts/store-graphics/`, renders at the exact final dimensions with device scale 1, and writes RGB PNGs without resizing. It does not operate an emulator, a signed-in browser, or Play Console. Rebuilding also refreshes `manifest.json`.

## Validation — 2026-09-29

All three final PNGs were visually reviewed in full size. Text, card corners, and the complete fan remain visible in both languages. Independent checks passed for dimensions, 8-bit RGB color type, every PNG chunk CRC, full pixel decoding, and the manifest's SHA-256 hashes. The icon is 90,887 bytes; both feature graphics are below 331 KB.
