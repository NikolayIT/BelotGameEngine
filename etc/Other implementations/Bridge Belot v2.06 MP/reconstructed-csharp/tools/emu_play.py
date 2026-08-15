#!/usr/bin/env python3
"""
Emulate the ORIGINAL card-play AI: TMainBelotForm_player2BeforePlay @0x46F5C0.

The routine is an event handler `(Self, CardGroup, var ChosenIndex)` — it writes the index of
the card it decides to play into its third parameter (~30 `*param_3 = idx` sites), so running it
under emulation yields the original AI's actual decision.

Extra state it needs beyond the object graph used for the rule functions:
  0x48BFEC : 4 x 32-byte matrix "player p may still hold card c"
             block = (player-1)*32, index = suit*8 + (rank-7)
  0x48B7CC : the global Application instance pointer (deref'd for a window check)
"""
import struct

from unicorn import UC_HOOK_MEM_WRITE
from unicorn.x86_const import UC_X86_REG_EBP

from emu import (BelotEmu, EmuError, SUITMAP, STUB_FUNCS,
                 CLUBS, DIAMONDS, SPADES, HEARTS,
                 C_CLUBS, C_DIAMONDS, C_HEARTS, C_SPADES, C_NOTRUMPS, C_ALLTRUMPS,
                 CONTRACT_TRUMP_SUIT)

MATRIX = 0x48BFEC
APP_VAR = 0x48B7CC

# Per-player AI memory, 1-based (index 1..4). choosegame resets these when a contract is
# settled; leaving them as zeroed BSS would tell the AI that everybody bid clubs.
MEM_A = 0x48BEB8   # init 0
MEM_B = 0x48BEC0   # init 4  (suit each player bid; reset unless all-trumps)
MEM_C = 0x48BEC4   # init 4
MEM_D = 0x48BEC8   # init 4
MEM_E = 0x48BECC   # init 0
MEM_F = 0x48BED0   # init 0
MEM_G = 0x48BED4   # init 0, 4 players x 4 suits of INTs (64 bytes)
DECLARER = 0x48BEBD

# UI / networking / announce-speech helpers that must not run under emulation.
PLAY_STUBS = {
    0x475CAC,   # say announces when the hand is still full
    0x46EA78,   # network: send to one peer
    0x46EB44,   # network: broadcast
    0x476150,   # SayNetAnons
    0x4776EC,   # showtxt
    0x474480,   # stoptimers
    0x475100, 0x47512C,   # ShowWait / HideWait
}



# Ghidra names this routine's locals `Stack_N`; empirically that is [ebp-(N-4)].
def _o(ghidra_off):
    return -(ghidra_off - 4)


FRAME = {
    "candCount":   _o(0x24),   "playersLeft": _o(0x34),
    "sureCount":   _o(0x78),   "middleCount": _o(0x7C), "loserCount": _o(0x80),
    "l1Count":     _o(0x5C),   "l3Count":     _o(0x60),  "l2Count":   _o(0x64),
    "l5Count":     _o(0x68),   "l4Count":     _o(0x6C),
    "trumpRanksOut": _o(0x70),
}
ARRAYS = {                     # (ghidra base, element count, 1-based?)
    "candidates": (0x210, 9, True),
    "buckets":    (0x120, 25, True),   # loser@1.. , middle@9.. , sure@0x11..
    "lists":      (0x1D0, 42, True),   # five lookahead lists at 1, 9, 0x11, 0x19, 0x21
    "suitCount":  (0x2D8, 12, False),  # [0..3] suits, [7..10] per-player win counts
    "sureBySuit": (0x2E8, 4, False),
    "highTrump":  (0x2B0, 9, False),   # [5..8] = strongest trump each player may hold
}
BYTES = {"trump": 0x48, "longSide": 0x47, "anyOppTrump": 0x85}

class PlayEmu(BelotEmu):
    def __init__(self, **kw):
        STUB_FUNCS.update(PLAY_STUBS)
        super().__init__(**kw)
        self.round_memory = None        # set to a 0x60-byte block to override the round reset
        app = self.alloc(0x400)
        self.w8(app + 0x8D, 1)          # "app already active" -> skips the activate call
        self.w32(APP_VAR, app)
        self._app = app
        self.declarer = 1
        self.capture = False
        self.frame = None
        self._out_addr = None

        def _on_write(uc, access, address, size, value, _):
            if self.capture and address == self._out_addr and self.frame is None:
                ebp = uc.reg_read(UC_X86_REG_EBP)
                self.frame = (ebp, bytes(uc.mem_read(ebp - 0x340, 0x340)))
            return True

        self.uc.hook_add(UC_HOOK_MEM_WRITE, _on_write)

    def _fw(self, off):
        """dword at [ebp+off] from the captured frame"""
        ebp, buf = self.frame
        return struct.unpack_from("<i", buf, off + 0x340)[0]

    def _fb(self, off):
        ebp, buf = self.frame
        return buf[off + 0x340]

    def internals(self):
        """Decode the routine's own working state from the captured frame."""
        if self.frame is None:
            return None
        st = {k: self._fw(off) for k, off in FRAME.items()}
        for name, (base, n, one_based) in ARRAYS.items():
            b = _o(base)
            st[name] = [self._fw(b + 4 * i) for i in range(n)]
        for name, gh in BYTES.items():
            st[name] = self._fb(_o(gh))
        return st

    def choose_with_internals(self, contract, me, my_hand, played_in_trick, all_played, voids):
        self._build_state(contract, me, my_hand, played_in_trick, all_played, voids)
        seed = self.r32(0x48B040)          # Delphi RandSeed, before the routine runs
        out = self.alloc(16)
        self.w32(out, 0xFFFFFFFF)
        self._out_addr = out
        self.frame = None
        self.capture = True
        try:
            self._call_routine(out)
        finally:
            self.capture = False
        idx = self.r32(out)
        st = self.internals()
        if st is not None:
            st["rngSeed"] = seed
        return (None if idx in (0xFFFFFFFF, 0xFFFFFFFB) else idx), st

    def _call_routine(self, out):
        """
        Run player2BeforePlay, naming the one position it cannot answer.

        Two sites in the routine (0x4731ad and 0x473bcf) read the caller's index back out of the
        var parameter -- `mov edx,[ebp-0xc]; mov edx,[edx]` -- and hand it to the accessor at
        0x484c88, which returns nil for an out-of-range index; the caller then dereferences that
        nil at +0x20 to read the chosen card's suit. In the real game the index is always valid
        there, so whatever calls this passes a card index in rather than a "nothing chosen yet"
        marker. What it passes is not recovered: the routine is a published method invoked
        indirectly, and no dispatch site for it survives in the binary to read the answer off.

        So the harness cannot invent one. Substituting a plausible index (0, say) does change the
        answers -- regenerating the vectors that way turns roughly ninety "no decision" positions
        into decisions -- and a guess dressed up as a measurement is worse than a gap.
        """
        try:
            self.call(0x46F5C0, eax=self._form, edx=self._group, ecx=out, timeout_insns=80_000_000)
        except EmuError as e:
            idx = self.r32(out)
            signed = idx - 0x100000000 if idx >= 0x80000000 else idx
            if "Invalid memory read" in str(e):
                raise EmuError(
                    f"[out={signed}] position depends on the caller's incoming card index: the "
                    "routine read it back (0x4731ad/0x473bcf) before choosing one, and what the "
                    f"game passes in is not recovered. Underlying fault: {e}") from None
            raise

    def install_round_memory(self, mem):
        """Write a whole 0x60-byte memory block at MEM_A (see memory.py).

        0x48BEBC/0x48BEBD fall inside that span but are the contract and its companion byte, not
        AI memory, so they are left alone.
        """
        for i, b in enumerate(mem):
            if MEM_A + i in (0x48BEBC, 0x48BEBD):
                continue
            self.w8(MEM_A + i, b)
        self.w8(DECLARER, self.declarer)

    def read_round_memory(self):
        return bytes(self.uc.mem_read(MEM_A, 0x60))

    def reset_round_memory(self, declarer=1):
        """Replicate choosegame's round-start reset of the AI's per-player memory."""
        for base, val in ((MEM_A, 0), (MEM_B, 4), (MEM_C, 4), (MEM_D, 4), (MEM_E, 0), (MEM_F, 0)):
            for i in range(4):
                self.w8(base + i, val)
        for i in range(64):          # 4 players x 4 suits, 4-byte entries
            self.w8(MEM_G + i, 0)
        self.w8(DECLARER, declarer)

    def set_matrix(self, possible):
        """possible[player][ (suit,rank) ] = 1/0  for players 1..4."""
        for p in (1, 2, 3, 4):
            for s in (CLUBS, DIAMONDS, SPADES, HEARTS):
                for r in range(7, 15):
                    self.w8(MATRIX + (p - 1) * 32 + s * 8 + (r - 7),
                            1 if possible[p].get((s, r), 0) else 0)


    def legal_indices(self, contract, me, my_hand, played_in_trick, all_played, voids):
        """Indices of the legal cards, via the original predicate FUN_004767F8."""
        self._build_state(contract, me, my_hand, played_in_trick, all_played, voids)
        return [i for i in range(len(my_hand))
                if (self.call(0x4767F8, eax=self._form, edx=self._group, ecx=i) & 0xFF) != 0]

    def choose_card_ex(self, contract, me, my_hand, played_in_trick, all_played, voids):
        """
        Same as choose_card but the 'who can hold what' matrix also encodes shown voids,
        so the original AI has exactly the knowledge the C# port derives from play history.
        """
        return self._run_choice(contract, me, my_hand, played_in_trick, all_played, voids)

    def choose_card(self, contract, me, my_hand, played_in_trick, all_played):
        """
        my_hand        : list of (suit, rank) still in hand (sorted game-style)
        played_in_trick: list of (player, (suit, rank)) already on the table this trick
        all_played     : set of (suit, rank) played in earlier tricks (plus this trick)
        Returns the index into my_hand that the ORIGINAL AI chooses.
        """
        return self._run_choice(contract, me, my_hand, played_in_trick, all_played, None)

    def _run_choice(self, contract, me, my_hand, played_in_trick, all_played, voids):
        self._build_state(contract, me, my_hand, played_in_trick, all_played, voids)
        out = self.alloc(16)
        self.w32(out, 0xFFFFFFFF)
        try:
            self._call_routine(out)
        except EmuError as e:
            if "incoming card index" not in str(e):
                raise
            return self._answer_regardless(contract, me, my_hand, played_in_trick, all_played, voids)

        idx = self.r32(out)
        return None if idx in (0xFFFFFFFF, 0xFFFFFFFB) else idx

    def _answer_regardless(self, contract, me, my_hand, played_in_trick, all_played, voids):
        """
        For the positions where the routine reads the caller's index before choosing one.

        What the game passes in is not recovered, but the answer does not have to depend on it:
        run the position once for every index the caller could plausibly have held, and if they
        all come out at the same card then that card is the routine's answer whatever the caller
        did. Only when they disagree is the position genuinely unanswerable, and then it says so
        rather than picking one.
        """
        got = []
        for start in range(len(my_hand)):
            self._build_state(contract, me, my_hand, played_in_trick, all_played, voids)
            out = self.alloc(16)
            self.w32(out, start)
            try:
                self.call(0x46F5C0, eax=self._form, edx=self._group, ecx=out,
                          timeout_insns=80_000_000)
            except EmuError as e:
                raise EmuError(f"incoming index {start} faults too: {e}") from None

            idx = self.r32(out)
            got.append(None if idx in (0xFFFFFFFF, 0xFFFFFFFB) else idx)

        if len(set(got)) == 1:
            return got[0]

        if got == list(range(len(my_hand))):
            raise EmuError(
                "the routine declines to override here: it hands back exactly the card index it "
                "was given (tried 0.." f"{len(my_hand) - 1}" ", got the same back each time). "
                "player2BeforePlay is an override hook -- the caller pre-selects a card and the "
                "AI changes it or leaves it -- and the harness has no pre-selection for it to "
                "leave alone, so there is no answer to report")

        raise EmuError(
            "the answer depends on the caller's incoming card index, which is not recovered: "
            f"indices 0..{len(my_hand) - 1} give {got}")

    def _build_state(self, contract, me, my_hand, played_in_trick, all_played, voids):
        self.reset_heap()
        self.set_contract(contract)
        self.set_local_mode()
        if self.round_memory is None:
            self.reset_round_memory(self.declarer)
        else:
            self.install_round_memory(self.round_memory)
        app = self.alloc(0x400)
        self.w8(app + 0x8D, 1)
        self.w32(APP_VAR, app)

        # A player may hold any card that has not been seen.
        possible = {p: {} for p in (1, 2, 3, 4)}
        for s in (CLUBS, DIAMONDS, SPADES, HEARTS):
            for r in range(7, 15):
                seen = (s, r) in all_played or (s, r) in my_hand
                for p in (1, 2, 3, 4):
                    blocked = seen or (voids is not None and s in voids.get(p, ()))
                    possible[p][(s, r)] = 0 if blocked else 1
        for (s, r) in my_hand:            # my own cards are certainly mine
            possible[me][(s, r)] = 1
        self.set_matrix(possible)

        players = {p: self.make_player(p) for p in (1, 2, 3, 4)}

        # my group: the remaining cards, padded to 8 slots with empty ones
        my_cards = [self.make_card(s, r) for (s, r) in my_hand]
        slots = my_cards + [self.make_card(0, 7, hidden=1) for _ in range(8 - len(my_hand))]
        group = self.make_group(me, slots)
        self.w8(group + 0x18A, 0)            # controller = computer

        table_slots = {}
        for i, (p, (s, r)) in enumerate(played_in_trick):
            table_slots[i] = self.make_card(s, r, owner=players[p])
        led = played_in_trick[0][1][0] if played_in_trick else 4
        table = self.make_table(led, CONTRACT_TRUMP_SUIT.get(contract, 4), table_slots)

        # How many cards each opponent still holds. This is not cosmetic: the routine reads
        # GetCount on the other players' groups (e.g. at 0x473171) and branches on it, so giving
        # everyone a full hand of eight all round sends it down paths the real game never takes --
        # including one that reads back a choice it has not made yet and dereferences nil.
        # Everyone has played one card per completed trick, plus one more if they are already in
        # the current trick.
        seen = set(all_played) | set(my_hand)
        completed = (len(seen) - len(my_hand) - len(played_in_trick)) // 4
        in_trick = {p for p, _ in played_in_trick}
        groups = []
        for p in (1, 2, 3, 4):
            if p == me:
                groups.append(group)
            else:
                left = max(0, min(8, 8 - completed - (1 if p in in_trick else 0)))
                g = self.make_group(p, [self.make_card(0, 7, hidden=1) for _ in range(left)])
                self.w8(g + 0x18A, 0)
                groups.append(g)
        self._form = self.make_form(table, groups)
        self._group = group


if __name__ == "__main__":
    e = PlayEmu()
    hand = [(CLUBS, 7), (CLUBS, 14), (DIAMONDS, 9), (DIAMONDS, 11),
            (SPADES, 8), (SPADES, 13), (HEARTS, 10), (HEARTS, 12)]
    hand.sort(key=lambda c: (SUITMAP[c[0]], c[1]))
    try:
        idx = e.choose_card(C_CLUBS, 2, hand, [], set(hand))
        print("hand:", hand)
        print("original AI leads index", idx, "=", hand[idx] if idx is not None else None)
    except EmuError as ex:
        print("ERR", ex)
