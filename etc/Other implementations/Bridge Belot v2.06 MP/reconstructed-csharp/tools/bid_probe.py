#!/usr/bin/env python3
"""
Run the ORIGINAL bidding decision, not just its scoring loop.

`choosegame` @0x4778CC works out what to bid, leaves it in EBX at 0x4791FD, and then records it
in the auction state before returning. Calling it once per seat therefore plays a whole auction
exactly the way the game does.

The state it works from:

    0x48BF14  the seat to act, 1..4
    0x48BE94  the seat holding the contract, 1..4 (starts at 1)
    0x48BE70  each seat's own last bid, indexed 0x48BE6F[seat]; 0 = none, so "nobody has bid"
              is bids[holder] == 0
    0x48C073  set to 1 to make the routine start a fresh auction (it clears the two above and
              resets the AI's memB)
    0x48BFDC  the match score per team, two int32s - the routine bids differently near 151
    0x48BFDA  1 or 2, indexes a string table; anything else trips a range check

A bid is encoded as `level * 10 + contract`, contract 1..6 = Clubs, Diamonds, Hearts, Spades,
No-trumps, All-trumps; 0 is Pass. Level 0 is a plain bid, higher levels are the double/redouble
steps.

    python bid_probe.py 5                      # show five auctions
    python bid_probe.py 200 7 vectors/bids.json  # write golden vectors
"""
import json
import random
import sys

from unicorn.x86_const import UC_X86_REG_EAX, UC_X86_REG_EBP, UC_X86_REG_EBX,     UC_X86_REG_EDX, UC_X86_REG_ESP

from emu import CLUBS, DIAMONDS, SPADES, HEARTS, STACK_BASE, STACK_SIZE, EmuError
from emu_play import PlayEmu
from gen_vectors import ALL_CARDS, game_sort

SUIT_LETTER_OF = {CLUBS: "C", DIAMONDS: "D", SPADES: "S", HEARTS: "H"}

BIDDER = 0x48BF14
HOLDER = 0x48BE94
BIDS = 0x48BE6F          # 1-based: BIDS + seat
NEW_AUCTION = 0x48C073
BOARD = 0x48BFDC
VARIANT = 0x48BFDA
OPENER = 0x48BF15        # the seat that starts the auction (set by wholegameinit @0x475AE4)
DECIDED_AT = 0x4791FD    # EBX holds the chosen bid here

CONTRACT_NAME = {0: "Pass", 1: "Clubs", 2: "Diamonds", 3: "Hearts", 4: "Spades",
                 5: "NoTrumps", 6: "AllTrumps"}


def name_of(bid):
    if bid == 0:
        return "Pass"
    level, contract = divmod(bid, 10)
    base = CONTRACT_NAME.get(contract, str(contract))
    return base if level == 0 else f"{base}+{level}"


MEM_B = 0x48BEBF          # 1-based: MEM_B + seat
CONTRACT_SUIT = {1: 0, 2: 1, 3: 3, 4: 2, 5: 4, 6: 4}    # @0x489DAC


class BidEmu(PlayEmu):
    last_seed = 0

    def r8(self, addr):
        return self.uc.mem_read(addr, 1)[0]

    def build_form(self, hands):
        """hands: {seat: [(suit, rank) x5]} -> a form holding the five dealt cards per seat."""
        self.reset_heap()
        self.set_local_mode()
        groups = []
        for p in (1, 2, 3, 4):
            slots = [self.make_card(s, r) for (s, r) in hands[p]]
            slots += [self.make_card(0, 7, hidden=1) for _ in range(3)]
            groups.append(self.make_group(p, slots))
        table = self.make_table(4, 4, {})
        return self.make_form(table, groups)

    def start_auction(self, board=(0, 0), first_seat=1):
        self.w32(BOARD, board[0])
        self.w32(BOARD + 4, board[1])
        self.w8(VARIANT, 1)
        self.w8(OPENER, first_seat)
        # The routine resets these itself on the first call of an auction (0x477949); do it up
        # front too so the state recorded alongside that first decision is the real one.
        self.w8(HOLDER, 1)
        for p in (1, 2, 3, 4):
            self.w8(BIDS + p, 0)
            self.w8(MEM_B + p, 4)
        self.w8(NEW_AUCTION, 1)

    def auction_state(self):
        return {"holder": self.r8(HOLDER),
                "bids": [self.r8(BIDS + p) for p in (1, 2, 3, 4)]}

    def bid(self, form, seat):
        """
        One call of choosegame, stopped the moment it has decided.

        Everything past 0x4791FD is captions and network traffic, which the harness has no form
        for, so the run stops there and the bookkeeping the routine would go on to do
        (0x4792A6..0x479364) is applied here instead.
        """
        self.w8(BIDDER, seat)
        self.last_seed = self.r32(0x48B040)      # Delphi RandSeed on entry
        frame = STACK_BASE + STACK_SIZE - 0x8000
        self.uc.reg_write(UC_X86_REG_ESP, frame - 0x400)
        self.uc.reg_write(UC_X86_REG_EBP, frame)
        self.uc.reg_write(UC_X86_REG_EAX, form)
        self.uc.reg_write(UC_X86_REG_EDX, 0xFFFFFFFF)
        self.last_addrs = []
        self.fault = None
        self.uc.emu_start(0x4778CC, DECIDED_AT, count=200_000_000)
        self.w8(NEW_AUCTION, 0)
        bid = self.uc.reg_read(UC_X86_REG_EBX)
        self._record(seat, bid)
        return bid

    def _record(self, seat, bid):
        """0x4792A6..0x479364: remember who holds the contract, and which suit each seat asked
        for. A suit two seats have both named belongs to neither."""
        if bid == 0:
            return
        self.w8(HOLDER, seat)
        self.w8(BIDS + seat, bid)
        contract = bid % 10
        if contract >= 5:
            return
        suit = CONTRACT_SUIT[contract]
        contested = False
        for p in (1, 2, 3, 4):
            if self.r8(MEM_B + p) == suit:
                self.w8(MEM_B + p, 4)
                contested = True
        if not contested:
            self.w8(MEM_B + seat, suit)


def play_auction(emu, hands, first_seat, board=(0, 0)):
    """Bid round-robin until the auction dies; record the state before every decision."""
    form = emu.build_form(hands)
    emu.start_auction(board, first_seat)
    out = []
    passes = 0
    seat = first_seat
    for _ in range(24):
        state = emu.auction_state()
        bid = emu.bid(form, seat)
        out.append({
            "seat": seat,
            "hand": [[SUIT_LETTER_OF[s], r] for (s, r) in hands[seat]],
            "holder": state["holder"],
            "bids": state["bids"],
            "board": list(board),
            "opener": first_seat,
            "rngSeed": emu.last_seed,
            "bid": bid,
        })
        passes = passes + 1 if bid == 0 else 0
        contested = any(b for b in emu.auction_state()["bids"])
        if passes >= (3 if contested else 4):
            break
        seat = seat % 4 + 1
    return out


ALL_STANDING = [0] + list(range(1, 7)) + list(range(11, 17)) + list(range(21, 27))


def synth_case(emu, hands, seat, holder, bids, board, opener):
    """
    Ask the routine one question from a state we set up directly.

    Natural auctions almost never reach the doubled and redoubled entries of the jump table -- a
    double needs a team within five points of 151 to begin with, and a redouble needs one on top
    of that -- so waiting for them to occur leaves those branches untested. The routine reads its
    auction state entirely from globals and does not sanity-check it, so the state can just be
    written. That is fair for a differential test: the binary and the port are asked the same
    question, whether or not a real auction would have posed it.
    """
    form = emu.build_form(hands)
    emu.w32(BOARD, board[0])
    emu.w32(BOARD + 4, board[1])
    emu.w8(VARIANT, 1)
    emu.w8(OPENER, opener)
    emu.w8(NEW_AUCTION, 0)
    emu.w8(HOLDER, holder)
    for p in (1, 2, 3, 4):
        emu.w8(BIDS + p, bids[p])
        emu.w8(MEM_B + p, 4)
    state = {
        "seat": seat,
        "hand": [[SUIT_LETTER_OF[s], r] for (s, r) in hands[seat]],
        "holder": holder,
        "bids": [bids[p] for p in (1, 2, 3, 4)],
        "board": list(board),
        "opener": opener,
    }
    emu.w8(BIDDER, seat)
    emu.last_seed = emu.r32(0x48B040)
    frame = STACK_BASE + STACK_SIZE - 0x8000
    emu.uc.reg_write(UC_X86_REG_ESP, frame - 0x400)
    emu.uc.reg_write(UC_X86_REG_EBP, frame)
    emu.uc.reg_write(UC_X86_REG_EAX, form)
    emu.uc.reg_write(UC_X86_REG_EDX, 0xFFFFFFFF)
    emu.last_addrs = []
    emu.fault = None
    emu.uc.emu_start(0x4778CC, DECIDED_AT, count=200_000_000)
    state["rngSeed"] = emu.last_seed
    state["bid"] = emu.uc.reg_read(UC_X86_REG_EBX)
    return state


def gen_synthetic(rounds, seed, path):
    """Sweep every standing bid the jump table distinguishes, so no case goes untested."""
    rng = random.Random(seed)
    emu = BidEmu()
    cases = []
    for n in range(rounds):
        deck = ALL_CARDS[:]
        rng.shuffle(deck)
        hands = {p: game_sort(deck[(p - 1) * 5:p * 5]) for p in (1, 2, 3, 4)}
        board = (rng.choice([0, 60, 120, 140, 146, 148, 150]),
                 rng.choice([0, 60, 120, 140, 146, 148, 150]))
        opener = rng.randint(1, 4)
        for standing in ALL_STANDING:
            holder = rng.randint(1, 4)
            bids = {p: 0 for p in (1, 2, 3, 4)}
            bids[holder] = standing
            # give the other seats plausible history too, since the routine reads all four
            for p in (1, 2, 3, 4):
                if p != holder and rng.random() < 0.4:
                    bids[p] = rng.choice([1, 2, 3, 4, 5, 6])
            for seat in (1, 2, 3, 4):
                try:
                    cases.append(synth_case(emu, hands, seat, holder, bids, board, opener))
                except Exception as e:                      # noqa: BLE001
                    print(f"  [skip] {type(e).__name__}: {e}", file=sys.stderr)
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"bidchoice": cases}, f)
    print(f"{len(cases)} synthetic bid decisions -> {path}")


def main():
    if len(sys.argv) > 1 and sys.argv[1] == "synth":
        gen_synthetic(int(sys.argv[2]), int(sys.argv[3]), sys.argv[4])
        return

    rounds = int(sys.argv[1]) if len(sys.argv) > 1 else 5
    seed = int(sys.argv[2]) if len(sys.argv) > 2 else 7
    path = sys.argv[3] if len(sys.argv) > 3 else None
    rng = random.Random(seed)
    emu = BidEmu()
    cases = []
    for _ in range(rounds):
        deck = ALL_CARDS[:]
        rng.shuffle(deck)
        hands = {p: game_sort(deck[(p - 1) * 5:p * 5]) for p in (1, 2, 3, 4)}
        # 146+ leaves a team needing 5 or fewer, which is the only way the routine reaches
        # its doubling branches, so those states have to be sampled deliberately.
        board = (rng.choice([0, 0, 60, 120, 140, 146, 148, 150]),
                 rng.choice([0, 0, 60, 120, 140, 146, 148, 150]))
        try:
            got = play_auction(emu, hands, rng.randint(1, 4), board)
        except EmuError as e:
            print(f"  [skip] {e}", file=sys.stderr)
            continue
        cases.extend(got)
        if not path:
            for c in got:
                cards = " ".join(f"{s}{r}" for s, r in c["hand"])
                print(f"  seat {c['seat']} [{cards:22}] holder={c['holder']} "
                      f"bids={c['bids']} board={c['board']} -> {name_of(c['bid'])}")
            print()
    if path:
        with open(path, "w", encoding="utf-8") as f:
            json.dump({"bidchoice": cases}, f)
        print(f"{len(cases)} bid decisions -> {path}")


if __name__ == "__main__":
    main()
