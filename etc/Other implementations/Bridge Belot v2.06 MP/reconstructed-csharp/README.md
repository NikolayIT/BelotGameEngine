# Bridge Belot v2.06 MP — C# reconstruction

A working, cross-platform C# reconstruction of **`belot.exe`** (the sibling file in this folder):
*"Бридж Белот v2.06 MP, Copyright (c) 2001, Валентин Цеков"* — a Bulgarian Belote / Bridge-Belot
game for Windows. The original is a 1.25 MB 32-bit **Delphi 5/6 VCL** application (native x86 PE).

This project reverse-engineers its **rules engine and AI** and reimplements them as a .NET
solution. The Win32 GUI, GDI card rendering, and TCP multiplayer are intentionally out of scope
(not portable and not the interesting part); a console UI stands in for the GUI.

## Solution layout (3 projects)

```
BelotV2.sln
├─ BelotV2.Rules   class library — model + full engine (no dependencies)
│    Cards, Contract, Deck, DelphiRandom, Seats, Rules(+ValidCards),
│    Announces, Scoring, Players (IPlayer + contexts), Game
├─ BelotV2.AI      class library — the reverse-engineered AI  (→ Rules)
│    AiTables, BiddingAi (choosegame), OriginalPlayAi (player2BeforePlay),
│    PlayMemory, OriginalPlayAdapter, AiPlayer, OriginalAiPlayer
└─ BelotV2.UI      console app                                 (→ Rules, AI)
     Program, ConsoleHumanPlayer, RandomPlayer
```

## Run it

```bash
dotnet run --project BelotV2.UI                 # interactive: you are South vs three AI
dotnet run --project BelotV2.UI -- sim 1000     # AI-vs-AI self-play, prints statistics
dotnet run --project BelotV2.UI -- vsrandom 300 # AI vs a random baseline (skill check)
dotnet run --project BelotV2.UI -- selftest     # checks of the recovered rules/tables
dotnet run --project BelotV2.UI -- oracle bids 20 1   # per-hand contract scores + opening bid
dotnet run --project BelotV2.UI -- oracle game 5  1   # full reproducible deal traces
dotnet run --project BelotV2.UI -- verify tools/vectors/golden.json     # diff vs the binary
dotnet run --project BelotV2.UI -- compare 20 ../tools/ai_server.py     # live vs the real AI
```

The AI beats the random baseline in **499 of 500** matches — a sanity check that bidding, play,
announces and scoring work together coherently.

## Using it as a reference oracle

`oracle` prints deterministic, line-oriented decisions so another Belot implementation can be run
over the same inputs and diffed. `oracle bids` dumps each hand's score for all six contracts plus
the opening bid — the fastest way to verify your bid **valuation** matches exactly. `oracle game`
dumps each deal's hands, auction, every trick (with the winner) and the round score. Each dump
prints the exact hands, so a comparison engine can be fed identical inputs without replicating the
shuffle.

## How it was reverse-engineered

Ghidra (headless) for decompilation + custom Python/`pefile` extractors that recovered the Delphi
VMT/RTTI class and method names, published field offsets, all CP1251 strings, and all 16 binary
`TPF0` form layouts. Data tables were located in the `DATA` section by matching Belot constants
and following code cross-references. Decompiler output for the two big AI routines was made
readable by marking the Delphi bounds-check raiser (`FUN_00402788`) `noreturn`, which collapses
the `if (out-of-range) raise` guards that otherwise flood the output with phantom variables.

## Verification: this port is diff-tested against the real binary

`tools/` contains a Unicorn-based harness that **executes the original x86 code inside
`belot.exe`** — it maps the PE, builds the Delphi object graph the routines expect, emulates the
Windows APIs the RTL calls, and runs the game's own functions. Golden vectors captured that way
are replayed against the C# implementation:

```bash
dotnet run --project BelotV2.UI -- verify tools/vectors/golden.json
dotnet run --project BelotV2.UI -- verify tools/vectors/golden_play.json
```

| behaviour | source of truth | result |
|---|---|---|
| card strength + point tables (every card × contract) | the caches the game builds at runtime | **192/192 (100%)** |
| announcement detection | `FUN_00479EE8` | **500/500 (100%)** |
| bidding valuation (all 6 contracts per hand) | `choosegame` scoring loop | **12000/12000 (100%)** |
| what the AI actually bids | `choosegame` decision | **17499/17499 (100%)** |
| round scoring | `FUN_0047AC00` | **2500/2500 (100%)** |
| legal-move rules | `FUN_004767F8` | **2000/2000 (100%)** |
| trick winner | `FUN_004766A8` | **800/800 (100%)** |
| card-play analysis + lookahead | `player2BeforePlay` internals | **100%** |
| the AI's per-round memory | the block at 0x48BEB8, byte for byte | **5160/5160 (100%)** |
| card-play final decision | `player2BeforePlay` | **5160/5160 (100%)** |

Diff-testing found and fixed two real bugs that reading the decompilation had missed:

1. **Over-trump obligation.** When void in the led suit with an opponent already trumping, you
   must over-trump *if you can* — but if you cannot, you are free to play anything. The port was
   wrongly forcing an under-trump.
2. **x87 precision in the bidding score.** The normalisation constants are 80-bit values very
   slightly below 1.16 / 1.06, so `75 × 1.06` is *just under* 79.5 and rounds to 79, where C#
   `double` gives exactly 79.5 → 80. The scaling is now done in exact integer arithmetic.

## Fidelity — exact vs. reconstructed

**Reverse-engineered from the binary (address-cited in the code):**

| Item | Source |
|---|---|
| Card model, hand layout, deal (5 then 3) | `avcard`, `dealother3` @0x4754F8 |
| Trump / no-trump strength orders | @0x489D7C / @0x489D84 |
| Trump / no-trump point values | @0x489D9C / @0x489DA4 |
| Four-of-a-kind values (4J=200,4×9=150,else=100) | @0x489DF0 |
| Valid-card rules (follow/raise/ruff/over-ruff, partner exemption) | `FUN_004767F8`, `FUN_00476BAC` |
| Trick-winner resolution | `TestCards` @0x46F49C, strength table @0x48BAED |
| Announcement detection | `FUN_00479EE8` |
| **Bidding valuation** — per-card weight tables + the full suit formula (+6/trump, −10/trump-below-3, +3 K+Q, +1 J+9), no-trump/all-trump sums, declaration bonuses | `choosegame` @0x4778CC, tables @0x489DB4/DBC/DC4 |
| First player = `Random(4)+1` then alternates by team | `wholegameinit` @0x475AE4 |
| Delphi RNG (LCG `seed*0x08088405+1`) | `FUN_00402B78` |
| Match target = **151** | `choosegame` (`151 − score`) |

**AI — what is exact vs. reconstructed:**

- **Bidding valuation — fully transcribed and verified 1:1** (12,000 vectors), including the x87
  normalisation the decompiler hid: suit score ×1.16 (`@0x479D30`), no-trumps ×1.06 (`@0x479D3C`),
  all-trumps unscaled, rounded to nearest-even (`fistp`) in exact integer arithmetic, then the
  +3 K+Q, +1 J+9 and +5 (trump-count > 3) bonuses and the declaration bonus.
- **Bidding decision — fully transcribed** from `choosegame`'s own control flow and matching the
  binary on every one of 17,499 recorded decisions:
  - The opener tries All-trumps → No-trumps → Hearts → Spades → Diamonds → Clubs (`@0x489D2A`
    read downwards) and takes the first contract scoring **≥ 50**. Because all-trumps comes first
    with an unscaled score, **the original opens all-trumps aggressively** — a faithful quirk, not
    a bug.
  - A scoring four of a kind names its suit; four jacks go straight to all-trumps and will double
    or redouble an all-trumps contract that is not the partner's. Four sevens or eights are not
    declarations and are ignored, which matters: treating them as one makes the AI open garbage.
  - Once a contract is on the table the routine dispatches through a jump table (`@0x4783BB`) on
    the standing bid — suit, no-trumps, all-trumps, and the doubled forms of each. Those cases
    read a per-seat table of the six contracts sorted by score, which the scoring loop builds at
    `@0x48BDB0` with a descending selection sort that swaps only on a strict improvement. Raising
    means taking the first entry in that table that genuinely outranks the standing bid; the test
    is either a score bar (40/65 depending on the case) or, when lifting a partner's suit,
    **holding that suit's Jack**.
  - Every entry of that jump table is covered, including the doubled and redoubled ones. Those
    are unreachable in ordinary play — a double needs a team within five points of 151, and a
    redouble needs one on top of that — so `bid_probe.py synth` sets the auction state directly
    and sweeps every standing bid from 0 to 26 rather than waiting for one to occur.
  - Near the end of a match the routine changes gear. Two `Random` draws set it up: `Random(3)+4`
    and `Random(5)+16` are each compared with how many points either team still needs, so
    reproducing these decisions requires the same RNG seed. A lopsided match near 151 raises the
    opener's bar to **65**; if a team is within a few points of going out and the margin is more
    than 6, a different branch walks the ladder the *other* way (Clubs first) with a bar of **75**
    and doubles the opponents when behind. The doubled-contract cases also flip a coin
    (`Random(2)`) before bidding on.

### Want the original AI's exact card? Call it directly.

`OriginalAiPlayer` is an `IPlayer` whose card play is decided by **the real routine executing
inside belot.exe** (through `tools/ai_server.py`). It is exact by construction — not a
reimplementation — so it is the right reference when checking another engine:

```bash
dotnet run --project BelotV2.UI -- compare 20 ../tools/ai_server.py
```

That plays deals where every decision is put to both engines and reports agreement per situation.
Budget about half a second per decision, so it suits comparison harnesses rather than bulk
simulation.

### How the original card-play AI actually works

Fully reverse-engineered, and worth knowing if you are writing your own:

1. **Candidates** — the legal cards, via the game's own move predicate.
2. **Card tracking** — a 4×32 matrix "player *p* may still hold card *c*", narrowed by the cards
   seen and by the suits each player has shown void in.
3. **Classification** — each candidate is compared against the ranks an opponent might still hold,
   and lands in *sure winner* / *middle* / *loser* (a loser being a card with more possible
   beaters than the player has higher cards of that suit). Each bucket is sorted weakest-first.
4. **One-trick lookahead** — the real engine of the AI. For each candidate it enumerates *every
   plausible continuation of the trick*: each remaining player either follows with some rank they
   might hold, or is void — and a void player ruffs with their best possible trump (in a suit
   contract) or discards. The number of simultaneously-void players is capped. Every combination
   is scored for who wins the trick, giving per-player win counts.
5. **Ratio score** — `myTeamWins` vs `opponentWins` collapses to a single number: `+1000` when the
   opponents never win, `-1000` when the team never wins, otherwise `±(ratio − 1)`. Candidates are
   bucketed into five lists by that score (always-wins-with-trump, always-wins-for-partner,
   always-wins, mixed, never-wins).
6. **Decision tree** — roughly 1700 lines of nested cases keyed on how many players are still to
   play (4 = leading … 1 = last), the contract, who currently holds the trick, per-player memory
   of bid suits and shown voids, and the five lists. It contains many specific plays, e.g. *in
   all-trumps, if your partner is winning with the Jack, feed them your Nine*.

- **Card play — `OriginalPlayAi` is a transcription of `player2BeforePlay` (0x46F5C0)**, written
  as a near-literal port (same arrays, counters and iteration orders) and validated stage by stage
  against the real routine's own stack frame captured under emulation (`verifystate`):

  | stage | result |
  |---|---|
  | candidate list, trump analysis, suit counts, longest side suit | **100%** |
  | sure / middle / loser buckets incl. contents and ordering | **100%** |
  | per-player lookahead win tallies | **100%** |
  | all five lookahead result lists incl. contents and ordering | **100%** |
  | the AI's per-round memory block | **100%** (5160/5160) |
  | final card chosen (decision tree) | **100%** (5160/5160) |

  Every stage matches, so the port is the original AI rather than an approximation of it. The
  final decision is also checked end-to-end through `OriginalPlayAdapter` — engine `PlayContext`
  in, `Card` out — at **5160/5160**, and holds up per seat and per situation.

  The positions come from independently generated sets, each produced only after the port already
  matched the previous ones, so they are out-of-sample checks rather than the data the code was
  tuned on. `verifystate` also reports **which decision-tree branch decided each position**, and
  every one of the seventeen is exercised — a branch nobody reaches is transcribed but unverified,
  and the totals above would look identical either way.

  That distinction is not academic. Three real bugs survived a "100%" reading of the totals
  because the harness held some input constant, making the branch that depended on it
  unreachable: the declarer seat (pinned to 1), the contract's doubling multiplier at 0x48BEBE
  (left 0, a value the game never produces), and the suit each seat bid, which only exists if a
  round follows an auction. The generator now varies all three, and the branch that reads the
  partner's bid suit — dead until then — turned out to be wrong: it exits straight to the common
  tail, skipping the "do not open with a trump" adjustment the rest of the mixed branch goes
  through, because leading the partner's suit is deliberate even when that suit is trump.

  Three things were needed to get the last few percent, and all are worth knowing if you compare
  your own engine against this one:

  - **The AI remembers things across the tricks of a round** — which suits have been led, which
    suit each seat bid or opened, who has been discarding what, and who is running short. The
    decision routine only *reads* that memory; the game fills it in from three other places (end
    of trick, the card-played handler, and the routine's own epilogue). `PlayMemory` reproduces
    all of it, and `OriginalPlayAdapter` rebuilds the block from the play history before every
    decision. It is checked byte for byte against the block the binary actually ran with.

  - **The five lookahead lists are each sorted weakest-first** before the tree reads them. Four of
    the five sorts use the order table picked for the trick; the trump-winners list always sorts
    by trump order.
  - **Some branches pick at random** (`Random(n)` over equally-rated candidates). Those are only
    reproducible if you run Delphi's LCG from the same seed, which is why the vectors record
    `rngSeed` and `DelphiRandom` reproduces `RandSeed * $08088405 + 1` exactly.

  **`OriginalAiPlayer` (via `tools/ai_server.py`) remains available when you want the card straight
  from the binary** — useful as an oracle when testing another engine.

### Round scoring

Also diffed against the exe now, at **2,500/2,500** scored rounds. `FUN_0047AC00` is called with
nothing but the form and writes the match board straight to 0x48BFDC/0x48BFE0, so the harness can
run it and read the answer back (`tools/score_probe.py`). About four fifths of it is the score
dialogue — 35 VCL calls, 22 integer-to-string conversions, three vtable dispatches — so it needs
a synthetic form and a table of no-op stubs before it will execute, and the run stops at
entry+5933, the moment both board figures have been written.

Doing this turned up four rules the canonical version had wrong:

- **Where the points come from.** Each seat's group carries a Delphi dynamic array at +0x174 of
  the cards that seat took, and the sides are seats 1+3 against 2+4. The entries are
  `SUITMAP[suit] * 13 + rank` — the *display* suit order, in which spades and hearts swap places
  relative to the binary's own suit numbering. Getting that wrong values the trump jack as a
  plain one and quietly shifts 18 points.
- **A double or an inside contract collapses the round.** The whole pot goes to whichever side
  scored more (level points to east-west), multiplied by the doubling coefficient, then nudged to
  an even number of tens — up in all-trumps, down in a suit contract, untouched in no-trumps.
  Hanging points follow the winner.
- **A made contract does not round both sides.** The leader's tens are rounded and the other side
  takes whatever is left of the round's total, so the two always add up. A side that took
  anything at all never rounds away to nothing: two points against a hundred and sixty still
  banks one, out of the leader's share.
- **Level points hang.** Neither side banks the round: the declaring side's half carries to the
  next deal and the other side takes its own half — and under a double the whole multiplied pot
  hangs and nobody scores.

Declarations are part of this too. `FUN_00479EE8` — the detector already verified at 500/500 —
writes an eight-byte descriptor per seat which 0x47A21A copies to 0x48BE90, seven bytes holding
the top rank of each declaration: `[careta1, careta2, terca1, terca2, quarte1, quarte2, quinte]`.
Feeding those in and re-scoring settled three more rules:

- **They are added to the raw team totals at face value, after the no-trumps doubling.** No-trumps
  doubles what the cards are worth, not what a terca is worth — and in no-trumps declarations do
  not count at all.
- **Competing declarations compare by VALUE first, not by kind.** Four nines (150) outrank four
  queens (100) even though the nine is the lower card, and four jacks (200) outrank both. Only
  when two are worth the same does the kind decide (a careta beats a sequence). Comparing by kind
  first — the intuitive reading, and what this port did — hands the round to the wrong side
  whenever two fours of a kind meet. The winning side then scores *all* of its declarations, and
  an exact tie cancels both sides.
- **The last tie-break runs in two different directions.** A sequence is ranked by its top card by
  rank, as you would expect: a terca to the king beats a terca to the ten. A four of a kind is
  ranked by that card's *strength* instead — the plain trick-taking order, in which a ten sits
  above a king — so four tens beats four kings, while four aces beats four tens. All twelve
  orderings of the four hundred-point caretas were put to the binary, in both seat orders, and the
  answer is exactly the game's own `NoTrumpOrder`; it does not depend on who declared first.
  Nines and jacks never reach this test, their values being unique.

Not recovered, and deliberately so: the deal/shuffle order. The original re-seeds from the clock,
so it cannot be reproduced even in principle; a Fisher-Yates on the same RNG is behaviourally
equivalent, and `oracle` prints the hands so comparisons never need it.

An internal detail that does not affect the port: the exe's suit **index** order is
Clubs=0, Diamonds=1, Spades=2, Hearts=3 (tables @0x489C5C/@0x489DAC), distinct from the bid-ladder
order; this reconstruction uses its own consistent `Suit` enum, so trump/trick logic is unaffected.
