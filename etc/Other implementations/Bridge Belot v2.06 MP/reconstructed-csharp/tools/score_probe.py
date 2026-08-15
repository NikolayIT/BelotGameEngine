#!/usr/bin/env python3
"""
Try to run the ORIGINAL round-scoring routine, FUN_0047AC00 @0x47AC00.

It is called from TableAfterPartyFinish with nothing but the form, so every input comes from
globals and from the form's own objects, and it writes the match board back to 0x48BFDC/0x48BFE0.
Roughly four fifths of it is presentation -- 35 VCL calls, 22 integer-to-string conversions -- so
getting it to execute at all is mostly a matter of stubbing the UI it wants to update.

This is exploratory: it reports how far the routine gets and what it faults on, so the cost of
finishing the job is a measurement rather than a guess.
"""
import json
import random
import sys

from unicorn import UC_HOOK_CODE
from unicorn.x86_const import UC_X86_REG_EIP

from emu import (CLUBS, DIAMONDS, SPADES, HEARTS, SUITMAP, STACK_BASE, STACK_SIZE,
                 STUB_FUNCS, EmuError)
from emu_play import PlayEmu

BOARD = 0x48BFDC
CONTRACT = 0x48BEBC
VARIANT = 0x48BFDA
NETWORK = 0x48C074
FORM_PTR = 0x489FD4        # pointer-to-pointer the routine uses to reach its labels

# Everything the routine calls purely to paint the score dialogue. Each is register-convention or
# takes its arguments on the stack in a way a bare `ret` leaves consistent.
UI_STUBS = {
    0x42DBAC,   # set a component's caption
    0x42DA94,   # ditto, other overload
    0x416528,
    0x46122C,
    0x44B720,
    0x450D80,
    0x450DA0,
    0x4482C4,
    0x48242C,
    0x42D3BC,
    0x42D39C,
    0x42DB7C,
    0x482F4C,
    0x482F08,   # card UI reset, called once per won card
}


class ScoreEmu(PlayEmu):
    def __init__(self, **kw):
        STUB_FUNCS.update(UI_STUBS)
        super().__init__(**kw)
        self.reached = []

        def on_code(uc, address, size, _):
            if 0x47AC00 <= address < 0x47D200:
                self.reached.append(address)
                if len(self.reached) > 4000:
                    self.reached.pop(0)

        self.uc.hook_add(UC_HOOK_CODE, on_code, begin=0x47AC00, end=0x47D200)

    def make_taken(self, cards):
        """
        The per-seat list of cards taken, as the Delphi dynamic array the routine expects at
        group+0x174: refcount and length in the two dwords before the data, then one dword per
        card holding `SUITMAP[suit] * 13 + rank`. Note the suit goes through SUITMAP: the card
        index uses the display order (clubs, diamonds, hearts, spades), not the binary's own suit
        numbering, so spades and hearts swap places. That code indexes the runtime points cache,
        which the game builds at 0x48BC34 as six bytes per card, one per contract.
        """
        n = len(cards)
        block = self.alloc(8 + max(n, 1) * 4)
        self.w32(block, 1)              # refcount
        self.w32(block + 4, n)          # length
        data = block + 8
        for i, (suit, rank) in enumerate(cards):
            self.w32(data + i * 4, SUITMAP[suit] * 13 + rank)
        return data

    def build_world(self, taken=None):
        """
        The world the routine expects.

        The points come out of the four PLAYER groups on the form: each seat's group holds the
        cards that seat took during the round, and the sides are seats 1+3 against 2+4. The two
        32-slot piles hanging off **0x489FD4 at +0x398/+0x39C are only walked to clear the
        display, so their contents do not matter.

        `taken` is {seat: [(suit, rank), ...]} for seats 1..4.
        """
        self.reset_heap()
        self.set_local_mode()
        taken = taken or {p: [] for p in (1, 2, 3, 4)}
        groups = [self.make_group(p, []) for p in (1, 2, 3, 4)]
        for seat, group in zip((1, 2, 3, 4), groups):
            self.w32(group + 0x174, self.make_taken(taken[seat]))
        table = self.make_table(4, 4, {})
        form = self.make_form(table, groups)

        # The routine reaches its UI through four pointer-to-pointer globals. Each gets a big
        # zeroed object; every field it reads out of them is a component it only wants to paint.
        # The three virtual calls it makes get a vtable whose every slot is one of the stubbed
        # no-ops, so the dispatch lands somewhere harmless instead of on a null pointer.
        vtable = self.alloc(0x400)
        for off in range(0, 0x400, 4):
            self.w32(vtable + off, 0x450D80)

        app = None
        for g in (0x489FD4, 0x489F6C, 0x489EEC, 0x489F50):
            obj = self.alloc(0x800)
            self.w32(obj, vtable)
            # anything the object points at is another component; make them all valid too
            for off in range(4, 0x800, 4):
                child = self.alloc(0x40)
                self.w32(child, vtable)
                self.w32(obj + off, child)
            holder = self.alloc(4)
            self.w32(holder, obj)
            self.w32(g, holder)
            if g == 0x489FD4:
                # The cleanup loop walks 32 slots of each pile, so they must be that long.
                for team in (1, 2):
                    filler = [self.make_card(0, 7, hidden=1) for _ in range(32)]
                    self.w32(obj + 0x394 + team * 4, self.make_group(team, filler))
        return form

    def score(self, form, contract=1, board=(0, 0), declarer=1, level=1,
              last_trick_winner=1, hanging=0, quiet=False):
        self.w8(CONTRACT, contract)
        self.w8(0x48BEBD, declarer)            # the seat that won the contract
        self.w8(0x48BEBE, level)               # 1 plain, 2 doubled, 4 redoubled
        self.w8(0x48BEBF, last_trick_winner)   # who took the last trick (the +10)
        self.w32(0x48BFE4, hanging)            # points hanging from earlier failed contracts
        self.w8(VARIANT, 1)
        self.w8(NETWORK, 0)
        self.w32(BOARD, board[0])
        self.w32(BOARD + 4, board[1])
        frame = STACK_BASE + STACK_SIZE - 0x8000
        from unicorn.x86_const import UC_X86_REG_EAX, UC_X86_REG_EBP, UC_X86_REG_ESP
        self.uc.reg_write(UC_X86_REG_ESP, frame - 0x400)
        self.uc.reg_write(UC_X86_REG_EBP, frame)
        self.uc.reg_write(UC_X86_REG_EAX, form)
        self.reached = []
        self.last_addrs = []
        self.fault = None
        try:
            # Stop the moment both board adds have happened (entry+5933). Everything after that
            # is the score dialogue, which needs a real VCL to paint.
            self.uc.emu_start(0x47AC00, 0x47C32D, count=200_000_000)
            ok = True
        except Exception as ex:                       # noqa: BLE001
            ok = False
            print("  stopped:", type(ex).__name__, ex)
            print("  at eip = %08x" % self.uc.reg_read(UC_X86_REG_EIP))
        if quiet:
            return ok
        span = (min(self.reached), max(self.reached)) if self.reached else (0, 0)
        print("  instructions inside the routine: %d, reached up to %08x (entry+%d bytes)"
              % (len(self.reached), span[1], span[1] - 0x47AC00))
        if self.reached:
            print("  last inside the routine:", " ".join("%08x" % a for a in self.reached[-6:]))
        print("  board now:", self.r32(BOARD), self.r32(BOARD + 4))
        return ok




ALL = [(s, r) for s in (CLUBS, DIAMONDS, SPADES, HEARTS) for r in range(7, 15)]
CONTRACT_NAME = {1: "Clubs", 2: "Diamonds", 3: "Hearts", 4: "Spades",
                 5: "NoTrumps", 6: "AllTrumps"}


DECL = 0x48BE90          # per-seat declaration descriptor, 8 bytes: 0x48BE90 + seat*8


def set_declarations(e, decls):
    """
    Write each seat's declaration descriptor.

    FUN_00479EE8 (the announce detector, which BelotV2 matches 500/500) produces an 8-byte record
    per seat and 0x47A21A copies it here. The seven used bytes are the TOP RANK of each
    declaration, or 0 for none:

        [careta1, careta2, terca1, terca2, quarte1, quarte2, quinte]

    `decls` is {seat: [...7 ranks...]}; omitted seats declare nothing.
    """
    for seat in (1, 2, 3, 4):
        rec = list(decls.get(seat, [])) + [0] * 8
        for i in range(8):
            e.w8(DECL + seat * 8 + i, rec[i])


def clear_round_memory(e):
    """No declarations by anyone: the announce state shares the play AI's memory block."""
    for a in range(0x48BE98, 0x48BF20):
        e.w8(a, 0)


def split(team1, team2):
    """Team cards -> per-seat groups; the routine sums seats 1+3 against 2+4."""
    return {1: team1, 3: [], 2: team2, 4: []}


def gen_vectors(rounds, seed, path):
    """
    Random finished rounds, scored by the original.

    A round is eight tricks; each trick goes to some seat, so the cards each seat took are whole
    tricks rather than an arbitrary split. Declarations are left empty here: this isolates the
    card scoring, the last-ten, capot, the doubling multiplier and the tens rounding.
    """
    rng = random.Random(seed)
    e = ScoreEmu()
    cases = []
    for _ in range(rounds):
        deck = ALL[:]
        rng.shuffle(deck)
        taken = {p: [] for p in (1, 2, 3, 4)}
        winners = []
        for t in range(8):
            w = rng.randint(1, 4)
            winners.append(w)
            taken[w] += deck[t * 4:(t + 1) * 4]
        # Declarations: the descriptor slots are [careta1, careta2, terca1, terca2,
        # quarte1, quarte2, quinte], each a top rank or 0. Give roughly a third of the seats one,
        # so competing declarations between the sides come up often enough to matter.
        decls = {}
        used_caretas = set()
        for seat in (1, 2, 3, 4):
            if rng.random() >= 0.35:
                continue
            rec = [0] * 7
            slot = rng.choice([0, 2, 4, 6])
            rank = rng.randint(9, 14)
            if slot == 0:
                # Only one seat can hold four of a given rank, so never deal the same four of a
                # kind to two seats: that state cannot arise in a real game, and what the routine
                # does with it is not a rule worth reproducing.
                choices = [r for r in range(9, 15) if r not in used_caretas]
                if not choices:
                    continue
                rank = rng.choice(choices)
                used_caretas.add(rank)
            rec[slot] = rank
            decls[seat] = rec
        contract = rng.randint(1, 6)
        declarer = rng.randint(1, 4)
        level = rng.choice([1, 1, 1, 2, 4])
        hanging = rng.choice([0, 0, 0, 10, 30])
        form = e.build_world(taken)
        clear_round_memory(e)
        set_declarations(e, decls)
        try:
            if not e.score(form, contract=contract, declarer=declarer, level=level,
                           last_trick_winner=winners[-1], hanging=hanging, quiet=True):
                continue
        except Exception as ex:                       # noqa: BLE001
            print("  [skip]", ex, file=sys.stderr)
            continue
        cases.append({
            "contract": CONTRACT_NAME[contract],
            "declarer": declarer,
            "level": level,
            "lastTrick": winners[-1],
            "hanging": hanging,
            "taken": {str(p): [[s, r] for (s, r) in taken[p]] for p in (1, 2, 3, 4)},
            "decl": {str(p): decls.get(p, [0] * 7) for p in (1, 2, 3, 4)},
            "board": [e.r32(BOARD), e.r32(BOARD + 4)],
        })
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"scoring": cases}, f)
    print("%d scored rounds -> %s" % (len(cases), path))


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "vectors":
        gen_vectors(int(sys.argv[2]), int(sys.argv[3]), sys.argv[4])
        return
    e = ScoreEmu()
    half1 = [c for c in ALL if c[0] in (CLUBS, DIAMONDS)]
    half2 = [c for c in ALL if c[0] in (SPADES, HEARTS)]
    for label, contract, taken, declarer, last in [
        ("clubs, team1 takes C+D, last trick team1", 1, split(half1, half2), 1, 1),
        ("clubs, same but last trick team2", 1, split(half1, half2), 1, 2),
        ("clubs, capot for team1", 1, split(ALL, []), 1, 1),
        ("clubs, capot for team2, declarer team1", 1, split([], ALL), 1, 2),
        ("no-trumps, team1 takes C+D", 5, split(half1, half2), 1, 1),
        ("all-trumps, team1 takes C+D", 6, split(half1, half2), 1, 1),
    ]:
        form = e.build_world(taken)
        clear_round_memory(e)
        print(label)
        e.score(form, contract=contract, declarer=declarer, last_trick_winner=last)
        print()


if __name__ == "__main__":
    main()
