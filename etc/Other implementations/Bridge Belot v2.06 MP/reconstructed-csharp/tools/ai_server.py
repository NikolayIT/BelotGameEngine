#!/usr/bin/env python3
"""
Line protocol exposing the ORIGINAL belot.exe card-play AI.

Reads one JSON request per line on stdin, writes one JSON reply per line on stdout, keeping the
emulator warm between calls. This makes the real AI callable from any language — the decision is
produced by executing the game's own code, so it is exact by construction.

Request:
  {"contract":1, "me":2, "declarer":1,
   "hand":  [["C",7],["D",11], ...],              # my remaining cards, game-sorted
   "trick": [[1,"C",7], ...],                     # cards already on the table this trick
   "played":[["C",7], ...],                       # every card seen so far (incl. hand & trick)
   "voids": {"1":["C"],"2":[],"3":[],"4":[]},     # suits each player has shown void in
   "mem":   [0, 0, ...],                          # optional: the AI's 0x60-byte round memory
                                                  # (see memory.py); omitted = round-start state
   "seed":  12345}                                # optional: force Delphi's RandSeed
Reply:
  {"index": 2, "seed": 12345, "preselected": 1, "kept": false}
      index into "hand", and the RandSeed the decision started from. As in the game, the card
      group first takes its own default card with Random (0x484DAC, reported as "preselected"),
      then the routine runs and may break ties with further draws -- so seed your own
      implementation with "seed" and make the same draw first before comparing. "kept" says the
      routine stored nothing and the group's card was played as it stood.
  {"error": "...", "seed": 12345}

Contracts 1..6 = Clubs, Diamonds, Hearts, Spades, No-trumps, All-trumps.
Suits "C","D","S","H"; ranks 7..14. Players 1..4 (1=South); ask only for seats 2..4 —
player2BeforePlay is the computer players' handler and range-checks on that.
"""
import json
import sys

from emu import CLUBS, DIAMONDS, SPADES, HEARTS, EmuError
from emu_play import PlayEmu

SUIT = {"C": CLUBS, "D": DIAMONDS, "S": SPADES, "H": HEARTS}


def parse_card(entry):
    return SUIT[entry[0]], int(entry[1])


def main():
    emu = PlayEmu()
    sys.stderr.write("ai_server ready\n")
    sys.stderr.flush()
    for line in sys.stdin:
        line = line.strip()
        if not line:
            continue
        if line == "quit":
            break
        seed = None
        try:
            req = json.loads(line)
            hand = [parse_card(c) for c in req["hand"]]
            trick = [(int(t[0]), (SUIT[t[1]], int(t[2]))) for t in req["trick"]]
            played = {parse_card(c) for c in req.get("played", [])}
            voids = {int(k): {SUIT[s] for s in v} for k, v in req.get("voids", {}).items()}
            for p in (1, 2, 3, 4):
                voids.setdefault(p, set())
            emu.declarer = int(req.get("declarer", 1))
            mem = req.get("mem")
            emu.round_memory = bytearray(mem) if mem else None
            if "seed" in req:
                emu.w32(0x48B040, int(req["seed"]) & 0xFFFFFFFF)

            # Delphi's RandSeed as the caller holds it. The group's default pick and some of the
            # routine's branches draw from it, so a caller comparing its own implementation
            # against this one has to start from the same seed or it is comparing coin flips.
            seed = emu.r32(0x48B040)
            idx = emu.choose_card_ex(int(req["contract"]), int(req["me"]), hand,
                                     trick, played, voids)
            reply = ({"index": idx, "seed": seed, "preselected": emu.last_preselected,
                      "kept": not emu.stored}
                     if idx is not None else {"error": "no decision"})
        except EmuError as e:
            reply = {"error": f"emulation: {e}"}
        except Exception as e:                      # noqa: BLE001 - protocol robustness
            reply = {"error": f"{type(e).__name__}: {e}"}
        if "error" in reply and seed is not None:
            reply["seed"] = seed                    # so a failed position can be replayed too
        sys.stdout.write(json.dumps(reply) + "\n")
        sys.stdout.flush()


if __name__ == "__main__":
    main()
