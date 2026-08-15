#!/usr/bin/env python3
"""Log every simulated trick the lookahead evaluates, by hooking the TestCards call."""
import struct
import sys

from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_EBP, UC_X86_REG_EDX, UC_X86_REG_ECX

from emu import CLUBS, DIAMONDS, SPADES, HEARTS, EmuError
from emu_play import PlayEmu

SUIT = {"C": CLUBS, "D": DIAMONDS, "S": SPADES, "H": HEARTS}
NAME = {CLUBS: "C", DIAMONDS: "D", SPADES: "S", HEARTS: "H", 4: "-"}
TESTCARDS = 0x46F49C
AFTER_LOOP = 0x4712EC


class TrickProbe(PlayEmu):
    def __init__(self, **kw):
        super().__init__(**kw)
        self.log = []
        self.watch = False
        self.cand = 0

        def on_code(uc, address, size, _):
            if not self.watch:
                return
            if address == AFTER_LOOP:
                ebp = uc.reg_read(UC_X86_REG_EBP)
                self.cand = struct.unpack("<i", uc.mem_read(ebp - 0x1c, 4))[0]
                self.log.append(("END", self.cand))
            elif address == TESTCARDS:
                arr = uc.reg_read(UC_X86_REG_EDX)      # &rank of player 1
                entries = []
                for p in range(4):
                    rank = struct.unpack("<i", uc.mem_read(arr + 8 * p, 4))[0]
                    suit = uc.mem_read(arr + 8 * p + 4, 1)[0]
                    entries.append((suit, rank))
                self.log.append(("T", tuple(entries)))

        self.uc.hook_add(UC_HOOK_CODE, on_code)


def main():
    e = TrickProbe()
    # Diamonds, seat 4, one card already on the table (players 1 and 2 still to act)
    hand = [(CLUBS, 10), (DIAMONDS, 8), (DIAMONDS, 11), (HEARTS, 7),
            (HEARTS, 8), (HEARTS, 10), (SPADES, 7), (SPADES, 8)]
    trick = [(3, (SPADES, 14))]
    played = set(hand) | {(SPADES, 14)}
    voids = {p: set() for p in (1, 2, 3, 4)}
    e.watch = True
    idx = e.choose_card_ex(2, 4, hand, trick, played, voids)
    e.watch = False
    print("chosen:", idx)

    # only the first candidate
    tricks, seen_end = [], False
    for kind, payload in e.log:
        if kind == "END":
            break
        tricks.append(payload)
    print(f"candidate #1 evaluated {len(tricks)} continuations")
    for t in tricks:
        print("   " + "  ".join(f"p{p + 1}:{NAME.get(t[p][0], t[p][0])}{t[p][1]}" for p in range(4)))


if __name__ == "__main__":
    main()
