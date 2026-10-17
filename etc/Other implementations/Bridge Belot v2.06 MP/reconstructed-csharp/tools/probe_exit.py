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

    def exits(self, contract, me, hand, trick, played, voids, draw_default=True):
        """Run one position and list the store sites that wrote the slot. No store at all means
        the routine kept the card the group pre-selected."""
        self._build_state(contract, me, hand, trick, played, voids)
        if self.force_seed is not None:
            self.w32(0x48B040, self.force_seed)
        # A vector recorded at the routine's entry has no caller draw to repeat; its seed is
        # already the routine's, and it never reads the slot it is handed.
        pre = self.default_card() if draw_default else 0
        out = self.alloc(16)
        self.w32(out, pre)
        self.out_addr = out
        self.stores = []
        self.call(0x46F5C0, eax=self._form, edx=self._group, ecx=out,
                  timeout_insns=80_000_000)
        return self.r32(out), self.stores, pre


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

    st = rec["internals"]
    e.declarer = rec.get("declarer", 1)
    if "mem" in st:
        e.round_memory = bytearray(st["mem"])
    caller = "callerSeed" in st
    e.force_seed = st["callerSeed"] if caller else st.get("rngSeed")
    idx, stores, pre = e.exits(CONTRACT[rec["contract"]], rec["seat"], hand, trick,
                               played, voids, draw_default=caller)
    print("chosen:", idx, "  pre-selected:", pre if caller else "-")
    for eip, val in stores:
        print("   store at %08x  value=%d" % (eip, val if val < 0x80000000 else val - (1 << 32)))
    if not stores:
        print("   no store: the routine kept the group's pre-selected card")


def main():
    path = sys.argv[3] if len(sys.argv) > 3 else "vectors/golden_play.json"
    vectors = json.load(open(path))["playai"]
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
