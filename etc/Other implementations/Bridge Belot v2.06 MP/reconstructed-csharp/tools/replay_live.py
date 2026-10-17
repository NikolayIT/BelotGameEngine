#!/usr/bin/env python3
"""
Turn positions dumped by the arena into play vectors that `verifystate` checks stage by stage.

  python replay_live.py [--all] out.json dump.jsonl [more.jsonl ...]

The arena (BelotV2.Arena ... originalemu <games> <seed> dump.jsonl) appends every disagreement,
every position the emulator could not serve and every position where the game played the card
group's own pick, with the request exactly as the emulator got it (hand, trick, cards seen,
voids, the round memory the port built), the RandSeed the decision started from and the
completed tricks in play order. Each one is run through the routine the way the game runs it --
the card group's random pre-selection first, then player2BeforePlay -- with the routine's own
stack frame captured, so a vector records what the binary computed on the way and not only the
card it played.

Kept are the disagreements, the former failures and the positions whose answer depends on the
caller -- the routine stored nothing ("dependsOnCaller": "kept") or read the caller's slot back
before storing ("read-back") -- unless --all asks for everything. Repeats are kept once.
"""
import json
import sys

from emu import CLUBS, DIAMONDS, SPADES, HEARTS, EmuError
from emu_play import PlayEmu, RAND_SEED

SUIT = {"C": CLUBS, "D": DIAMONDS, "S": SPADES, "H": HEARTS}
CONTRACT_NAME = {1: "Clubs", 2: "Diamonds", 3: "Hearts", 4: "Spades", 5: "NoTrumps",
                 6: "AllTrumps"}
KEYS = ("candCount", "playersLeft", "sureCount", "middleCount", "loserCount",
        "l1Count", "l2Count", "l3Count", "l4Count", "l5Count",
        "trump", "longSide", "anyOppTrump", "trumpRanksOut",
        "candidates", "buckets", "lists", "suitCount", "sureBySuit",
        "highTrump", "rngSeed", "callerSeed", "preselected")


def main():
    args = sys.argv[1:]
    keep_all = "--all" in args
    args = [a for a in args if a != "--all"]
    out_path, dumps = args[0], args[1:]
    emu = PlayEmu()
    cases, seen, skipped, dropped = [], set(), 0, 0
    for path in dumps:
        with open(path, encoding="utf-8-sig") as f:
            records = [json.loads(line) for line in f if line.strip()]
        for rec in records:
            req = rec["request"]
            key = json.dumps([req, rec["seed"]], sort_keys=True)
            if key in seen:
                continue
            seen.add(key)

            hand = [(SUIT[s], int(r)) for s, r in req["hand"]]
            trick = [(int(p), (SUIT[s], int(r))) for p, s, r in req["trick"]]
            played = {(SUIT[s], int(r)) for s, r in req["played"]}
            voids = {int(k): {SUIT[s] for s in v} for k, v in req["voids"].items()}
            emu.declarer = int(req["declarer"])
            emu.round_memory = bytearray(req["mem"])
            emu.w32(RAND_SEED, rec["seed"])
            try:
                chosen, st = emu.choose_with_internals(int(req["contract"]), int(req["me"]),
                                                       hand, trick, played, voids)
            except EmuError as e:
                print(f"  [skip] {CONTRACT_NAME[req['contract']]} seat {req['me']}: {e}")
                skipped += 1
                continue
            if chosen is None or st is None:
                print(f"  [skip] {CONTRACT_NAME[req['contract']]} seat {req['me']}: no decision")
                skipped += 1
                continue

            depends = "kept" if not emu.stored else "read-back" if emu.read_back else None
            if not keep_all and depends is None and rec["kind"] == "preselection-played":
                dropped += 1      # the routine chose that very card itself
                continue

            internals = {k: st[k] for k in KEYS}
            internals["mem"] = list(req["mem"])
            cases.append({
                "contract": CONTRACT_NAME[req["contract"]],
                "seat": req["me"],
                "hand": req["hand"],
                "trick": rec["trick"],
                "history": rec["history"],
                "bidSuits": rec["bidSuits"],
                "declarer": rec["declarer"],
                "level": rec["level"],
                "humanSeat": rec["humanSeat"],
                "source": rec["kind"],
                "dependsOnCaller": depends,
                "chosen": chosen,
                "internals": internals,
            })

    with open(out_path, "w", encoding="utf-8") as f:
        json.dump({"playai": cases}, f)
    kinds = {k: sum(1 for c in cases if c["dependsOnCaller"] == k) for k in ("kept", "read-back")}
    print(f"{len(cases)} positions written to {out_path} ({skipped} skipped, {dropped} not "
          f"depending on the caller left out); the routine stored nothing in {kinds['kept']} "
          f"and read the caller's slot back first in {kinds['read-back']}")


if __name__ == "__main__":
    main()
