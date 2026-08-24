# Verification tools — running the ORIGINAL belot.exe code

These scripts execute the real x86 code inside `belot.exe` under a Unicorn emulator and record
what it does. That is how the C# port is verified: golden vectors are produced by the original
binary, and `dotnet run --project ../BelotV2.UI -- verify <file>` replays them against the C#
implementation. This is the authority — if the C# and the binary disagree, the binary is right.

## Requirements

```bash
pip install unicorn pefile capstone
```

## Files

| file | what it does |
|---|---|
| `emu.py` | maps the PE, sets up stack/GDT/imports, builds the Delphi object graph (cards, collections, card groups, table, form), calls functions with Delphi's register convention. Also runs the game's own strength/points cache builder. |
| `emu_play.py` | drives the card-play AI `player2BeforePlay` @0x46F5C0, which writes its chosen card index into its third parameter. |
| `gen_vectors.py` | golden vectors for card tables, announcements, bidding scores, legal moves. |
| `gen_play_vectors.py` | golden vectors for trick winner + card-play decisions (the emulated AI plays whole rounds against itself). |
| `disasm.py` | small capstone disassembler used during analysis (annotates x87 constant loads). |
| `memory.py` | the AI's per-round memory block at 0x48BEB8 and the rules that maintain it, so the emulated AI plays a round with the memory it would really have. `BelotV2.AI/PlayMemory.cs` is the same thing in C#, and every vector carries the block so the two are compared byte for byte. |
| `bid_probe.py` | drives `choosegame`'s bidding decision (not just its scoring loop). Plays whole auctions, or with `synth` writes the auction state directly so every entry of the decision's jump table is exercised — the doubled and redoubled ones are otherwise unreachable. |
| `score_probe.py` | runs the round-scoring routine `FUN_0047AC00` and reads the match board back. Mostly a matter of stubbing the score dialogue it wants to paint; `vectors` mode writes `vectors/golden_score.json`. It also writes the declarations directly, which is how the contest between two competing declarations was settled — including pairings a fair deal reaches only rarely. It never deals the same four of a kind to two seats: that cannot happen in a real game, and what the routine does with it is not a rule worth copying. |
| `ai_server.py` | see below; takes an optional `seed` and reports the `RandSeed` each decision ran with, so a client can align its own RNG before comparing (some branches break ties with `Random()`). |
| `probe_exit.py` | replays one recorded position and reports **which store site** in the routine wrote the answer, which names the decision-tree branch the original actually took. |
| `vectors/golden_bids.json` | 17,499 bid decisions from emulated auctions plus a synthetic sweep, with the auction state, the match score, the seat that opened and the RNG seed each one ran with. The match scores are sampled deliberately: only a board of 146+ leaves a team needing five or fewer points, which is the only way the routine reaches its doubling branches. |
| `vectors/golden_score.json` | 2,500 scored rounds: the cards each seat took, each seat's declarations, the contract, declarer, doubling level, last trick, hanging points, and the two numbers the original added to the match board. |
| `vectors/golden.json` | 14,692 committed vectors: card tables, announces, bid scores, legal moves. |
| `vectors/golden_play.json` | 800 trick-winner + 5,160 card-play decisions, each with the routine's own internal state (`internals`), the AI's memory block, and the RNG seed it ran with. |

## Regenerate and verify

The committed sets are the union of several runs with different seeds; regenerating with one
seed produces a smaller file that verifies just as well.

```bash
python gen_vectors.py 2000 11 vectors/golden.json          # rules + bidding
python gen_play_vectors.py 110 23 vectors/golden_play.json # trick winner + play AI
python bid_probe.py 600 7 vectors/golden_bids.json         # what the AI bids
python bid_probe.py synth 90 5 vectors/synth_bids.json     # ...every standing bid, incl. doubles
python score_probe.py vectors 2500 23 vectors/golden_score.json  # round scoring

cd ../BelotV2.UI
dotnet run -c Release -- verify ../tools/vectors/golden.json
dotnet run -c Release -- verify ../tools/vectors/golden_play.json
dotnet run -c Release -- verifystate ../tools/vectors/golden_play.json   # stage-by-stage
dotnet run -c Release -- verify ../tools/vectors/golden_bids.json
dotnet run -c Release -- verifyscoring ../tools/vectors/golden_score.json
dotnet run -c Release -- bidscan ../tools/vectors/golden_bids.json      # decisions + scores, as CSV
```

## Using the emulator as a live oracle for your own engine

`emu_play.PlayEmu` can answer "what would the original play here?" for any position:

```python
from emu import C_CLUBS, CLUBS, DIAMONDS, SPADES, HEARTS, SUITMAP
from emu_play import PlayEmu

e = PlayEmu()
hand = sorted([(CLUBS, 7), (CLUBS, 14), (DIAMONDS, 9), (SPADES, 8)],
              key=lambda c: (SUITMAP[c[0]], c[1]))
idx = e.choose_card_ex(
    contract=C_CLUBS, me=2, my_hand=hand,
    played_in_trick=[],          # [(player, (suit, rank)), ...] already on the table
    all_played=set(hand),        # every card seen so far
    voids={1: set(), 2: set(), 3: set(), 4: set()})
print("original plays", hand[idx])
```

Suits use the binary's own encoding: **0=Clubs 1=Diamonds 2=Spades 3=Hearts**; ranks are 7..14
(J=11, Q=12, K=13, A=14); players are 1..4 where 1=South, 2=East, 3=North, 4=West and partners
are {1,3} / {2,4}. Contracts are 1..6 = Clubs, Diamonds, Hearts, Spades, No-trumps, All-trumps.

Note `player2BeforePlay` is the handler for seats **2, 3 and 4** only — seat 1 is the human in the
original, and the routine range-checks on that, so ask it for decisions as seat 2, 3 or 4.

## How the emulation is made to work

* **Segments** — a GDT is installed so Delphi's `fs:[0]` SEH chain resolves; CS is left alone and
  ring-0 flat descriptors are used for DS/ES/SS (loading SS requires DPL == CPL).
* **Imports** — every IAT slot is redirected to a synthetic address; the hook emulates the API
  (stdcall, callee-cleans) — notably `VirtualAlloc`, so the Delphi memory manager runs for real.
* **Stubs** — GUI/networking/announce-speech helpers are replaced with a plain return; none of
  them affect the card logic.
* **Runtime tables** — the game builds its per-card strength and points caches at startup; the
  harness executes that very loop (0x475799..0x475949) instead of assuming its contents.
* **Bounds checks** — Delphi's range/overflow raisers are hooked and turned into Python errors,
  so any bad input shows up immediately instead of silently corrupting a run.
* **Round memory** — `reset_round_memory` reproduces what `choosegame` does at the start of a
  round to the AI's memory globals at 0x48BEB8..0x48BF14. Getting this wrong is silent and
  expensive: the discard matrix at 0x48BED4 is 4 players × 4 suits of **ints** (64 bytes), and
  zeroing only the first 16 of them left the AI reading stale bytes for partners 2–4, which
  produced a handful of golden vectors the routine itself would not reproduce on a clean replay.
  `probe_exit.py` is what pinned that down.

## `ai_server.py` — the original AI as a service

Keeps the emulator warm and answers one position per line, so any language can ask the real AI
what to play. `BelotV2.AI.OriginalAiPlayer` is the C# client for it.

```bash
echo '{"contract":1,"me":2,"declarer":1,"hand":[["C",7],["C",14],["D",9]],"trick":[],"played":[["C",7],["C",14],["D",9]],"voids":{"1":[],"2":[],"3":[],"4":[]}}' | python ai_server.py
# -> {"index": 0, "seed": 1234567}
```

Roughly half a second per decision — built for comparison harnesses, not bulk simulation.

Feed the returned `seed` to your own implementation before comparing answers, or the two disagree
by coin flip wherever the routine breaks a tie at random. About 0.8% of live positions come back
as an error instead of an index: `player2BeforePlay` is an override hook that is handed the
caller's pre-selected card index by reference, and on those positions it hands the same index
straight back rather than choosing. The harness proves that by running the position once per
possible incoming index; with no pre-selection of its own it has nothing to report, and says so
rather than inventing a card.

## EngineDiff — differential check against the repository engine

`EngineDiff/` is a console harness that plays seeded random rounds and compares, at every
decision point, the repository's `Belot.Engine` against this reconstruction's `BelotV2.Rules`
(which is itself diff-tested against the original binary): the legal-card set offered to the
player on every turn, the winner of every trick, and the per-hand announce detection (normalized;
the 8-card-run representation differs by design, so whole-suit hands are skipped).

```bash
cd EngineDiff
dotnet run -c Release -- 100000 12345   # deals x seed; ~30s, prints ALL MATCH or the mismatches
```

Run it after any change to the repository's play rules (ValidCardsService, TrickWinnerService,
ValidAnnouncesService) to re-verify behavioral equivalence with the 2001 game. As of 2026-08-24
a 100,000-deal run (19.2M turn checks, 4.8M trick checks, 2M announce checks) matches completely.
