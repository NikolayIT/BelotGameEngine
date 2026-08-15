#!/usr/bin/env python3
"""
Locate player2BeforePlay's locals empirically.

Ghidra names stack locals by an offset that is not literally EBP-relative, so instead of
trusting the names we stop the routine at the moment it writes its decision, dump the frame,
and correlate the words against values we already know (candidate count, candidate slots,
bucket contents...).
"""
import struct
import sys

from unicorn import UC_HOOK_MEM_WRITE
from unicorn.x86_const import UC_X86_REG_EBP, UC_X86_REG_ESP

from emu import CLUBS, DIAMONDS, SPADES, HEARTS, SUITMAP, C_CLUBS, C_ALLTRUMPS, EmuError
from emu_play import PlayEmu


class ProbeEmu(PlayEmu):
    def __init__(self, **kw):
        super().__init__(**kw)
        self.frame = None
        self.out_addr = None

        def on_write(uc, access, address, size, value, _):
            if self.out_addr is not None and address == self.out_addr and self.frame is None:
                ebp = uc.reg_read(UC_X86_REG_EBP)
                lo = ebp - 0x340
                self.frame = (ebp, bytes(uc.mem_read(lo, 0x340)))
            return True

        self.uc.hook_add(UC_HOOK_MEM_WRITE, on_write)

    def probe(self, contract, me, hand, trick, played, voids):
        self._build_state(contract, me, hand, trick, played, voids)
        out = self.alloc(16)
        self.w32(out, 0xFFFFFFFF)
        self.out_addr = out
        self.frame = None
        self.call(0x46F5C0, eax=self._form, edx=self._group, ecx=out,
                  timeout_insns=80_000_000)
        return self.r32(out), self.frame


def words(frame_bytes, ebp, off_lo=-0x340, off_hi=0):
    """yield (ebp_offset, dword) pairs"""
    for off in range(off_lo, off_hi, 4):
        idx = off - off_lo
        yield off, struct.unpack_from("<I", frame_bytes, idx)[0]


def main():
    e = ProbeEmu()
    hand = [(CLUBS, 7), (CLUBS, 10), (CLUBS, 14), (DIAMONDS, 9),
            (SPADES, 8), (SPADES, 13), (HEARTS, 11), (HEARTS, 12)]
    hand.sort(key=lambda c: (SUITMAP[c[0]], c[1]))
    idx, frame = e.probe(C_ALLTRUMPS, 2, hand, [], set(hand),
                         {p: set() for p in (1, 2, 3, 4)})
    print("hand:", [(("C", "D", "S", "H")[s], r) for s, r in hand])
    print("chosen index:", idx)
    if frame is None:
        print("no frame captured")
        return
    ebp, buf = frame
    print(f"ebp = 0x{ebp:x}")
    # candidate count should be 8 (all legal when leading); candidates should be 0..7
    print("\nwords that equal 8 (candidate count?):")
    for off, w in words(buf, ebp):
        if w == 8:
            print(f"   [ebp{off:+#06x}] = {w}")
    print("\nruns that look like the candidate list 0,1,2,...:")
    for off, w in words(buf, ebp):
        if w == 0:
            seq = []
            for k in range(9):
                o2 = off + 4 * k
                if o2 >= 0:
                    break
                seq.append(struct.unpack_from("<I", buf, o2 + 0x340)[0])
            if seq[:4] == [0, 1, 2, 3]:
                print(f"   [ebp{off:+#06x}] starts 0,1,2,3 -> {seq}")


if __name__ == "__main__":
    main()
