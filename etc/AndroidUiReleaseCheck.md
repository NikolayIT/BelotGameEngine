# Android 1.0 UI release check

September 29, 2026. Scope: the MAUI app in `src/UI/Belot.UI`; engine rules and AI weights are unchanged. App version remains **1.0 (build 1)**.

## Bugs and fixes

“Device” means reproduced on the authorized BlueStacks Test instance. Other entries cover deterministic test reproductions and source-identified layout constraints; coverage and device-check limits are listed separately.

| Reproduction / problem | Fix | Regression coverage |
| --- | --- | --- |
| Device: a long player name overlaps the Hint control. | Give the name a bounded, truncated column; place status/bid text below it and keep Hint in its own column. | `TableLayoutTests.LongNamesShouldHaveBoundedSpaceSeparateFromTheHintButton` |
| Device: changing Android font scale during a match returns to Start and loses the game. Home and resume alone preserve it. | Handle `ConfigChanges.FontScale` in the existing activity. After the base callback, refresh existing MAUI text handlers and invalidate layout without recreating the Shell or match. | `AndroidConfigurationTests` compiles the actual callback against platform doubles; native font/state verification is recorded below. |
| Device: large system text crowds the fixed table; a third bid row clips opponent backs. | Cap only live table text at 1.3x through `TableTextSize`, keeping card geometry fixed. Double/ReDouble share one slot in the NT/AT/Double-or-ReDouble/Pass row. Supporting pages and result overlays retain full system scaling. | `UiScaleTests`, `AndroidConfigurationTests`, table-text and shared-bid-row contracts in `TableLayoutTests`; native normal/large-text play and back stacks checked; Double/ReDouble not encountered natively |
| Three declaration choices can consume the middle row on a narrow table. | Scroll only the choices list, up to 90 table design units; keep title and Declare visible. One choice keeps its natural height. | Declaration-panel and hint-bound contracts in `TableLayoutTests`; one-choice native panels reviewed, three-choice scrolling covered by source contracts only |
| Device: requesting a hint shifts the whole hand by roughly four pixels. | Use a fixed three-unit overlay border inside the fixed-size card grid, with no layout-changing hint trigger. | `HintOutlinesShouldOverlayTheFixedCardWithoutChangingItsLayout`; native hand bounds verified identical before/after hint |
| Device: at 360 dp width, the table extends slightly beneath the status bar and screen edge. | Set explicit `SafeAreaEdges` and size the table from its content host. MAUI 10 otherwise permits edge-to-edge page content. Setup uses `All`, including keyboard avoidance; the table and other supporting pages respect the system container. | `TableLayoutTests.TableSizingShouldFollowTheContentHostInsideTheSystemBars`, safe-area page contracts and final device screenshots |
| User review: fifteen large level tiles and tablet magnification make the interface unnecessarily large; bidding obscures the table. | Replace the tiles with three native seat pickers. Cap page scale at 1.1 and table scale at 1.25; bound the table to 480 x 760 design units. Put decisions in an Auto row above the hand; only the declaration choices have a bounded scroll area. | `LineupSelectionTests`, `SupportingPageTests`, `UiScaleTests`, and `TableLayoutTests.BiddingShouldUseItsOwnCompactRowAboveTheHand` |
| Device/user report: illegal hand cards look blacked out and their faces cannot be read. | Remove the overlay entirely. Every card face stays fully visible in its normal position, with no positional illegal-card cue. Legal moves and `IsPlayable` are unchanged. Both the original alpha-colored `BoxView` and an attempted black overlay with explicit 20% opacity still produced black faces on Test. | Two whole-game `HandDimmingTests` cover model legality, clearing, rejected taps and assists on/off; `IllegalCardsShouldKeepUncoveredFacesWithoutMovingTheCards` guards the layout |
| User rejected numeric opponent counters, table scrolling and lowered illegal cards. | Restore `TableGrid` directly inside `TableHost`, with bounded `HeightRequest`, three expanded `Backs` stacks and the person's hand anchored in the bottom row. Remove the table `ScrollView` and illegal-card offsets. Keep the compact home, inline decisions, custom names and chronological card layers. | `TheTableShouldKeepTheHandInItsBottomRowInsteadOfScrollingWithTheContent`, `OpponentsShouldKeepTheirExpandedCardBacksInsteadOfNumericCounters`, and `IllegalCardsShouldKeepUncoveredFacesWithoutMovingTheCards` |
| Device: expanded side backs still clip during bidding on a 360 dp table. | Give each side seat Auto label rows and a remaining-height back-card row. `VerticalCardBackLayout` uses the actual arranged bounds: preserve 38 x 53 cards/13-unit step when possible, compress the step toward two units, then shrink proportionally. Keep every back inside its area; North and the person's hand are unchanged. | Seven `VerticalCardBackGeometryTests` cases cover 1-8 cards, widths/heights/scales and edge cases; two side-layout contracts; `release-backs-normal.png` and `release-backs-font2.png` show full stacks |
| Declarations appear twice and consume space needed by side cards. | Suppress the speech bubble once the persistent declaration summary is present; restore normal bid bubbles in the next auction. | Two `SeatPresentationTests` cases verify visibility and property-change notifications |
| User report: played cards stack in fixed seat order, so a later card can appear underneath an earlier one. | Bind current and last-trick cards to their chronological position; publish the layer before the card notification starts an animation; reset it between tricks/rounds/games. | `PlayedCardOrderTests`: two seeds, two whole games per seed, every leader, forced cards, collection, and mid-trick restart; four XAML binding cases |
| User request: AI difficulty is treated as a player name; other seats cannot be renamed. | Keep levels in setup selectors. Save a separate optional name for each bot; use localized seat roles when blank. All four table names are bounded and truncated. | `SeatNamesTests`, name persistence cases in `SettingsTests`, three entry wiring cases in `SupportingPageTests`, and table-name bounds |
| A picker is repopulated on language change, briefly yielding index -1. | Ignore temporary/cancelled selections; preserve each seat's existing saved level id independently. | `LineupSelectionTests` |
| Accepted bids/cards/declarations leave controls active until the queued session continuation. | Close the controls synchronously after an accepted action; keep them open for an invalid action. | `GameFlowRegressionTests.AcceptedMovesCloseTheirControlsBeforeTheSessionResumes` |
| Repeated Leave or a late command after leaving/disposal can navigate or start again. | Make leaving/disposal idempotent and reject commands from the closed table. | `GameFlowRegressionTests.RepeatedLeaveAndLateCommandsDoNotNavigateOrRestartAgain` |
| A turn-display failure leaves the session accepting actions after its error overlay. | Cancel and clear pending decisions before reporting the error; clear turn indicators. | `GameFlowRegressionTests.ASessionErrorClearsTheDecisionBeforeReportingIt` |
| Hint seed 76 selects a carre and an overlapping tierce; seed 41 highlights the wrong one of two declarations of the same kind. | Match the actual declaration and preserve the engine's first-nonconflicting selection order. | Two declaration-hint cases in `GameFlowRegressionTests` |
| Failed hints fault a discarded task; late hint completions/timers can update a disposed table. | Show a localized, recoverable notice for a current failure; discard stale results/failures and invalidate timers on disposal. | Retry and delayed success/failure cases in `GameFlowRegressionTests` |
| Repeated reset taps can open duplicate dialogs; a late confirmation can affect a page already left. | Share a `PageActions` gate for dialogs/navigation and bind confirmation to the current page visit. | `PageActionsTests` |
| Selecting the already-active default language does not persist the explicit choice; invalid saved speed shows no selected preset. | Persist explicit language choices and normalize invalid speeds to Normal. | `LocalizationTests`, `SettingsTests` |
| Older/damaged statistics can show impossible counts/streaks, lose a previous peak, or invent a result/date from a malformed history row. | Normalize counters, snapshot the old peak before updating, reject malformed history records and cap reads at 100 entries. | `PlayerRatingStoreTests`, `MatchHistoryTests` |
| Card/suit controls and icon-only navigation lack useful spoken names; table cards/counts and selected options are inaccessible. | Announce seat plus card for current/last tricks, and seat plus public count on expanded back stacks; exclude decorative backs. Add selection/legal/hint descriptions, including selected language/speed with language-change refresh. | `TableAccessibilityTests`, `SupportingPageTests`, `TableLayoutTests` |
| Large text crowds Settings choices and splits Statistics values. | Keep native controls in named grids (`LanguageChoicesGrid`, `SpeedChoicesGrid`, `MetricsGrid`), reducing columns as width/font scale requires. Subscribe only while the page is active. Preserve full text scaling. | Adaptive-grid and lifecycle cases in `SupportingPageTests`, column calculations in `UiScaleTests`; named native captures below verify Settings and Statistics; these pages were unchanged by the final home/side-back edits |
| At 2x text, the compact home header and paired name/level inputs become crowded. | `StartColumnsFor` moves the title above its actions and stacks each name above its native picker when needed. At normal phone text size, keep the existing compact two-column setup. Reposition existing controls without resetting entered names or saved selections; subscribe to scale changes only while visible. | Seventeen `StartPageLayoutTests` cases cover sizing thresholds, native controls and lifecycle/selection wiring; final normal/2x home captures and browser return verified |
| User request: open online Belot from the home screen. | Add a secondary button opening `https://ednaigra.com/play?game=belot` in the system browser. `OnlinePlay` shares the page action gate and shows a localized launch-failure message for the current visit. The local game engine gains no network/multiplayer integration. | `OnlinePlayTests` and home-button wiring in `SupportingPageTests`; URL returned HTTP 200 and local website `LobbyPage` routing confirms `game=belot`; native browser handoff and loaded Belot chooser verified in the captures below |
| Device: at 2x text, the Bulgarian Statistics title wraps its final letter onto another line. | When metrics stack, place the heading on a separate full-width row. Preserve the full font size and title text. | Heading/layout regression coverage; `final-statistics-font2.png` at 13:28 shows the intact title and values |

## Rejected candidates

A `FlexLayout` candidate made Settings buttons invisible on the native device and was reverted to explicit adaptive grids. It did not pass native review. Black illegal-card overlays, lowered cards, numeric opponent counters and whole-table scrolling were also removed; their earlier screenshots are not final release evidence.

## Validation status

- **272/272 UI tests passed**; baseline was 74. Engine **741** and AI **361** tests passed in the earlier verification run.
- Final Release builds passed with **zero warnings and errors**: Android **2:07.38**, Windows **38.90 seconds**.
- Final APK installed on Test: `belot-1.0-ui.apk`, SHA-256 **`ef7da6af32f43827698314a237149f89e3d9222b2fea723aad25e2c06bf05a7e`**.
- A complete native game ended **155:108**, rating **947 → 960**, after **66 human actions**. It ran on the immediately preceding APK (SHA-256 beginning `f259`); its game/table code is identical to the final APK, which adds only the home layout/helper reflow. The driver paused to fix result scrolling in the test harness; the app and match were not restarted.
- Whole-game headless tests verify legal actions, event order, rendered state, scoring, persistence, stop/restart and hints. XAML tests check wiring and layout contracts, not native dimensions. Android callback doubles do not emulate SP conversion.

Evidence directory: `artifacts/ui-release-20260929/` (ignored by Git). The table distinguishes final-APK checks from earlier captures of unchanged code. Earlier candidate matches requiring 116 and 42 human actions are not used as final whole-game evidence.

| Check | Result / evidence |
| --- | --- |
| Final Android Release build and APK hash | Passed; `android-final-build.log`, `final-apk-sha256.txt`; hash above |
| Final Windows Release build | Passed; `windows-final-build.log`, zero warnings/errors |
| Tests | UI **272/272**; engine **741**, AI **361** passed |
| Final APK: compact home and 2x text | Reviewed: `release-home-final-top.png`, `release-home-fixed-font2.png`, `release-home-fixed-pickers-font2.png`; title, selector labels and online button fit |
| Final APK: online browser handoff and return | `release-online-loaded.png` shows the actual Belot Random players/Friends chooser. Closing the browser tab returns to the app with names/levels retained in `release-home-final-normal.xml`. Launch-failure handling is covered by `OnlinePlayTests` |
| Settings at normal/2x text, speed options and English | Reviewed: `final-settings-phone.png`, `final-settings-font2.png`, `final-settings-speed-font2.png`, `final-settings-english.png`; these pages were unchanged by the final home/side-back edits |
| Statistics title and values at 2x text | Reviewed: `final-statistics-font2.png`, latest capture at 13:28; full title and values visible; Statistics unchanged afterward |
| Final APK: hint preserves the entire hand's bounds | `release-hint-before.png`, `release-hint-after.png` and `release-hint-bounds.json`; all eight before/after bounds identical and card faces readable |
| Preceding APK: font 2x → 1x and Home/resume preserve the replay | `release-state-retention.json` and `release-replay*.png`: the same five cards retain all five exact bounds after each change |
| Preceding APK: adaptive side backs at normal/2x text | Reviewed: `release-backs-normal.png`, `release-backs-font2.png`; full backs visible |
| Final APK: tablet-size table and home | Reviewed: `release-tablet-final.png` and `release-home-tablet-final.png` after restoring the original 1920 x 1080 size and font 1x; full backs and hand visible, no overlay. Two Pass bids followed by a human card turn were exercised |
| Preceding APK: declaration panel | `final-combinations-001.png`, `final-combinations-203.png`, `final-combinations-232.png` show bounded one-choice panels. Three-choice scrolling was not exercised natively; source-contract coverage only |
| Double/ReDouble controls | Shared-row layout and command coverage passed; these choices were not encountered during the native runs |
| Complete native game, result/continue and game over | Reviewed: `final-game-finished.png`; **155:108**, rating **947 → 960**. `final-game-smoke.log` plus `final-game-resume2.log` record **66 actions** on the preceding APK with identical game/table code |
| Replay and once-only history/rating | Preceding APK: leaving the unfinished replay adds no result; `release-history-once.xml` retains **4 games, 1 win, 3 losses**, rating **960**. Final APK: `release-home-tablet-final.xml` retains the same totals after a second unfinished game was left |
| Rules and rare native states | No new final-APK rules-page check or exhaustive native state coverage claimed; headless/source tests cover behavior and wiring |

## Reproduction

Run from the repository root in PowerShell:

```powershell
dotnet test src/Tests/Belot.UI.Tests/Belot.UI.Tests.csproj -c Release -p:TreatWarningsAsErrors=true
dotnet test src/Tests/Belot.Engine.Tests/Belot.Engine.Tests.csproj -c Release -p:TreatWarningsAsErrors=true
dotnet test src/Tests/Belot.AI.ClaudePlayer.Tests/Belot.AI.ClaudePlayer.Tests.csproj -c Release -p:TreatWarningsAsErrors=true
dotnet build src/UI/Belot.UI/Belot.UI.csproj -c Release -f net10.0-android -p:TreatWarningsAsErrors=true
dotnet build src/UI/Belot.UI/Belot.UI.csproj -c Release -f net10.0-windows10.0.19041.0 -p:TreatWarningsAsErrors=true
```

Focused logic checks:

```powershell
dotnet test src/Tests/Belot.UI.Tests/Belot.UI.Tests.csproj -c Release --filter 'FullyQualifiedName~GameFlowRegressionTests|FullyQualifiedName~AndroidConfigurationTests|FullyQualifiedName~PageActionsTests|FullyQualifiedName~LineupSelectionTests|FullyQualifiedName~PlayedCardOrderTests|FullyQualifiedName~HandDimmingTests|FullyQualifiedName~TableLayoutTests|FullyQualifiedName~SupportingPageTests|FullyQualifiedName~VerticalCardBackGeometryTests|FullyQualifiedName~SeatPresentationTests|FullyQualifiedName~StartPageLayoutTests'
```

Only **BlueStacks Test**, configuration **Pie64_2**, serial **127.0.0.1:5575**, Android **9 / API 28**, is authorized for device work in this audit. Recheck the instance mapping before reusing these commands in another session. **The Tower** (`Pie64`, port 5555 / `emulator-5554`) was not used, installed to, or modified.

At a human decision, record the hand and auction, then change the font scale on Test and confirm both state preservation and visible text resizing:

```powershell
$testDevice = '127.0.0.1:5575'
$originalFontScale = (adb -s $testDevice shell settings get system font_scale).Trim()
adb -s $testDevice shell settings put system font_scale 1.3
# Inspect the unchanged hand/auction and resized text.
adb -s $testDevice shell settings put system font_scale 2.0
# Check the capped table text, full-size Settings/Statistics, and scrollable results.
# Check double/redouble, three declaration choices, fixed hand bounds, and all actions.
if ($originalFontScale -eq 'null') {
    adb -s $testDevice shell settings delete system font_scale
} else {
    adb -s $testDevice shell settings put system font_scale $originalFontScale
}
```

Repeat with long custom seat names, switch English/Bulgarian, select different levels and verify saved choices after returning from Settings. Check large-font home title/name/picker reflow, Settings choices, Statistics values/title, three declaration choices, and unchanged hand bounds when Hint appears. Verify the online button opens the Belot lobby and returning from the browser preserves setup. Cancel/repeat reset and leave prompts; confirm one accepted action happens once. Use the final APK for release evidence.

## Scope and limits

The font fix retains the running activity for configuration changes; it does **not** restore a match after process death. Live table text follows system scaling up to 1.3x; supporting pages and result overlays keep full system scaling. The complete native game, replay retention and normal/large-text side-back checks used the preceding APK with identical game/table code; final-APK home, browser, hint and tablet checks are identified above. Three-choice declarations and Double/ReDouble were not exercised natively. Device coverage is Android 9 / API 28 on Test, not every Android version/device or complete TalkBack certification. The Test device was restored to its original 1920 x 1080 size and 1x font scale, with version 1.0 open on Home. Runtime logs contain BlueStacks EGL/MAUI Material warnings; no app crash or AndroidRuntime failure was observed. Zero-warning claims above apply to builds. No production publishing, release signing, store upload or rollout was performed.

Implementation references: [MAUI fonts and automatic scaling](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/fonts?view=net-maui-10.0), [MAUI 10 safe-area defaults and keyboard handling](https://learn.microsoft.com/en-us/dotnet/maui/user-interface/safe-area?view=net-maui-10.0), [Android configuration changes](https://developer.android.com/guide/topics/resources/runtime-changes), MAUI 10.0.70 [activity callback](https://github.com/dotnet/maui/blob/10.0.70/src/Core/src/Platform/Android/MauiAppCompatActivity.Lifecycle.cs), [SP/DIP font mapping](https://github.com/dotnet/maui/blob/10.0.70/src/Core/src/Fonts/FontManager.Android.cs), and [formatted-label refresh](https://github.com/dotnet/maui/blob/10.0.70/src/Controls/src/Core/Label/Label.Mapper.cs).
