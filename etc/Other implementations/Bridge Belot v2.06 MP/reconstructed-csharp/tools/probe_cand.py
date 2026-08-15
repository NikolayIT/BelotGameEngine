#!/usr/bin/env python3
"""Dump the lookahead's per-candidate working values from the real routine.

Hooks 0x4712EC — the instruction right after the triple enumeration loop — and reads the
frame: candidate index, the suit being simulated, the void cap and the win tally.
"""
import json
import struct
import sys

from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_EBP

from emu import CLUBS, DIAMONDS, SPADES, HEARTS, SUITMAP, EmuError
from emu_play import PlayEmu

SUIT = {"C": CLUBS, "D": DIAMONDS, "S": SPADES, "H": HEARTS}
NAME = {CLUBS: "C", DIAMONDS: "D", SPADES: "S", HEARTS: "H"}
CONTRACT = {"Clubs": 1, "Diamonds": 2, "Hearts": 3, "Spades": 4, "NoTrumps": 5, "AllTrumps": 6}
AFTER_LOOP = 0x4712EC


class CandProbe(PlayEmu):
    def __init__(self, **kw):
        super().__init__(**kw)
        self.rows = []
        self.watch = False

        def on_code(uc, address, size, _):
            if self.watch and address == AFTER_LOOP:
                ebp = uc.reg_read(UC_X86_REG_EBP)

                def w(off):
                    return struct.unpack("<i", uc.mem_read(ebp + off, 4))[0]

                def b(off):
                    return uc.mem_read(ebp + off, 1)[0]

                wins = [w(-0x2d4 + 4 * (p + 6)) for p in range(1, 5)]
                self.rows.append({
                    "cand": w(-0x1c), "simSuit": b(-0x42), "voidCap": w(-0x34),
                    "wins": wins, "threatened": b(-0x83),
                })

        self.uc.hook_add(UC_HOOK_CODE, on_code)

    def run_case(self, case):
        contract = CONTRACT[case["contract"]]
        me = case["seat"]
        hand = [(SUIT[c[0]], c[1]) for c in case["hand"]]
        trick = [(t[0], (SUIT[t[1]], t[2])) for t in case["trick"]]
        played = {(SUIT[h[1]], h[2]) for h in case["history"]}
        played |= {c for _, c in trick}
        voids = {p: set() for p in (1, 2, 3, 4)}
        hist = [(h[0], (SUIT[h[1]], h[2])) for h in case["history"]]
        for i in range(0, len(hist) - 3, 4):
            led = hist[i][1][0]
            for k in range(4):
                if hist[i + k][1][0] != led:
                    voids[hist[i + k][0]].add(led)
        if trick:
            led = trick[0][1][0]
            for p, (s, r) in trick:
                if s != led:
                    voids[p].add(led)

        self.rows = []
        self.watch = True
        try:
            idx = self.choose_card_ex(contract, me, hand, trick, played, voids)
        finally:
            self.watch = False
        return idx, self.rows


def main():
    path = sys.argv[1]
    want_contract = sys.argv[2] if len(sys.argv) > 2 else "Diamonds"
    data = json.load(open(path, encoding="utf-8"))
    e = CandProbe()
    shown = 0
    for case in data["playai"]:
        if case["contract"] != want_contract or "internals" not in case:
            continue
        idx, rows = e.run_case(case)
        print(f"--- {case['contract']} seat={case['seat']} "
              f"hand=[{','.join(c[0] + str(c[1]) for c in case['hand'])}] "
              f"trick=[{' '.join(str(t[0]) + ':' + t[1] + str(t[2]) for t in case['trick'])}] "
              f"chosen={idx}")
        for r in rows:
            print(f"    cand#{r['cand']} simSuit={NAME.get(r['simSuit'], r['simSuit'])} "
                  f"voidCap={r['voidCap']} threat={r['threatened']} wins={r['wins']}")
        shown += 1
        if shown >= 3:
            break


if __name__ == "__main__":
    main()
