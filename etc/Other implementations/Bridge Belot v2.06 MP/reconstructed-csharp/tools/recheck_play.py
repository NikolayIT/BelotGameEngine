#!/usr/bin/env python3
"""
Re-run recorded play decisions through the emulator as it is now, and check the routine still
makes every one of them. Run it after changing the harness (emu.py, emu_play.py, memory.py): a
vector the current harness no longer reproduces means the change altered what the routine sees.

  python recheck_play.py [vectors/golden_play.json] [limit]

Vectors recorded before the caller's pre-selection was recovered carry the seed the routine was
entered with and never read the slot they are handed, so they are replayed from that seed with
no caller draw. Newer ones carry callerSeed and are replayed the way the game plays them: the
card group's own default pick first, then player2BeforePlay.
"""
import json
import sys
import time

from emu import CLUBS, DIAMONDS, SPADES, HEARTS, EmuError
from emu_play import PlayEmu, RAND_SEED

SUIT = {"C": CLUBS, "D": DIAMONDS, "S": SPADES, "H": HEARTS}
CONTRACT = {"Clubs": 1, "Diamonds": 2, "Hearts": 3, "Spades": 4, "NoTrumps": 5, "AllTrumps": 6}


def position(rec):
    """hand, current trick, every card seen and the shown voids, as the vector records them."""
    hand = [(SUIT[s], r) for s, r in rec["hand"]]
    trick = [(p, (SUIT[s], r)) for p, s, r in rec["trick"]]
    history = [(p, (SUIT[s], r)) for p, s, r in rec["history"]]
    played = set(hand) | {c for _, c in history} | {c for _, c in trick}
    voids = {p: set() for p in (1, 2, 3, 4)}
    for plays in [history[i:i + 4] for i in range(0, len(history) - 3, 4)] + [trick]:
        if plays:
            led = plays[0][1][0]
            for p, (s, _) in plays:
                if s != led:
                    voids[p].add(led)
    return hand, trick, played, voids


def main():
    path = sys.argv[1] if len(sys.argv) > 1 else "vectors/golden_play.json"
    limit = int(sys.argv[2]) if len(sys.argv) > 2 else None
    with open(path, encoding="utf-8") as f:
        cases = json.load(f)["playai"]
    if limit:
        cases = cases[:limit]

    emu = PlayEmu()
    good = bad = 0
    started = time.time()
    for n, rec in enumerate(cases):
        st = rec.get("internals", {})
        hand, trick, played, voids = position(rec)
        emu.declarer = rec.get("declarer", 1)
        emu.round_memory = bytearray(st["mem"]) if "mem" in st else None
        fresh = "callerSeed" in st
        emu.w32(RAND_SEED, st["callerSeed"] if fresh else st.get("rngSeed", 0))
        try:
            got, _ = emu.choose_with_internals(CONTRACT[rec["contract"]], rec["seat"], hand,
                                               trick, played, voids,
                                               preselected=None if fresh else 0)
        except EmuError as e:
            got = f"fault: {e}"
        if got == rec["chosen"]:
            good += 1
            continue
        bad += 1
        if bad <= 10:
            print(f"  MISMATCH #{n} {rec['contract']} seat {rec['seat']} hand {rec['hand']} "
                  f"trick {rec['trick']}: recorded {rec['chosen']}, now {got}")

    print(f"{good}/{good + bad} recorded decisions reproduced ({time.time() - started:.0f}s)")
    return 1 if bad else 0


if __name__ == "__main__":
    sys.exit(main())
