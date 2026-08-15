#!/usr/bin/env python3
"""Report WHICH store site in player2BeforePlay writes the chosen slot.

Knowing the exit address pins the decision-tree branch the original really took, which is
much cheaper than reasoning about the decompilation.
"""
import json
import sys

from unicorn import UC_HOOK_MEM_WRITE
from unicorn.x86_const import UC_X86_REG_EIP

from emu import CLUBS, DIAMONDS, SPADES, HEARTS, SUITMAP
from emu_play import PlayEmu

SUIT = {"C": CLUBS, "D": DIAMONDS, "S": SPADES, "H": HEARTS}
CONTRACT = {"Clubs": 1, "Diamonds": 2, "Hearts": 3, "Spades": 4,
            "NoTrumps": 5, "AllTrumps": 6}


class ExitEmu(PlayEmu):
    def __init__(self, **kw):
        super().__init__(**kw)
        self.out_addr = None
        self.force_seed = None
        self.stores = []

        def on_write(uc, access, address, size, value, _):
            if self.out_addr is not None and address == self.out_addr:
                self.stores.append((uc.reg_read(UC_X86_REG_EIP), value))
            return True

        self.uc.hook_add(UC_HOOK_MEM_WRITE, on_write)

    def exits(self, contract, me, hand, trick, played, voids):
        self._build_state(contract, me, hand, trick, played, voids)
        out = self.alloc(16)
        self.w32(out, 0xFFFFFFFF)
        self.out_addr = out
        self.stores = []
        if self.force_seed is not None:
            self.w32(0x48B040, self.force_seed)
        self.call(0x46F5C0, eax=self._form, edx=self._group, ecx=out,
                  timeout_insns=80_000_000)
        return self.r32(out), self.stores


def run(rec):
    e = ExitEmu()
    hand = [(SUIT[s], r) for s, r in rec["hand"]]
    trick = [(p, (SUIT[s], r)) for p, s, r in rec["trick"]]
    played = set(hand) | {(SUIT[s], r) for _, s, r in rec["history"]} \
        | {c for _, c in trick}
    voids = {p: set() for p in (1, 2, 3, 4)}

    def scan(plays):
        if not plays:
            return
        led = plays[0][1][0]
        for p, (s, _) in plays:
            if s != led:
                voids[p].add(led)

    h = [(p, (SUIT[s], r)) for p, s, r in rec["history"]]
    for i in range(0, len(h) - 3, 4):
        scan(h[i:i + 4])
    scan(trick)

    e.force_seed = rec["internals"].get("rngSeed")
    idx, stores = e.exits(CONTRACT[rec["contract"]], rec["seat"], hand, trick,
                          played, voids)
    print("chosen:", idx)
    for eip, val in stores:
        print("   store at %08x  value=%d" % (eip, val if val < 0x80000000 else val - (1 << 32)))


def main():
    vectors = json.load(open("vectors/golden_play.json"))["playai"]
    want_hand = [list(x) for x in json.loads(sys.argv[1])]
    for rec in vectors:
        if rec["hand"] == want_hand and rec["contract"] == sys.argv[2]:
            print(rec["contract"], "seat", rec["seat"], rec["hand"],
                  "trick", rec["trick"])
            run(rec)
            return
    print("position not found")


if __name__ == "__main__":
    main()
