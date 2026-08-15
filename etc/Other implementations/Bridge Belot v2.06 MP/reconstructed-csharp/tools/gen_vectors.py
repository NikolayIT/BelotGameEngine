#!/usr/bin/env python3
"""
Generate golden test vectors by EXECUTING the original belot.exe code.

Produces JSON consumed by the C# `verify` command:
  validcards : FUN_004767F8  (legal-move predicate)  -> Rules.ValidCards
  announces  : FUN_00479EE8  (declaration detection) -> Announces.Detect
  cardtables : strength/points caches                -> Cards.*
"""
import json
import random
import sys

from emu import (BelotEmu, SUITMAP, CLUBS, DIAMONDS, SPADES, HEARTS,
                 C_CLUBS, C_DIAMONDS, C_HEARTS, C_SPADES, C_NOTRUMPS, C_ALLTRUMPS,
                 CONTRACT_TRUMP_SUIT, EmuError)

# canonical suit letters <-> the ORIGINAL card encoding (0=C 1=D 2=S 3=H)
SUIT_LETTER = {CLUBS: "C", DIAMONDS: "D", SPADES: "S", HEARTS: "H"}
CONTRACT_NAME = {C_CLUBS: "Clubs", C_DIAMONDS: "Diamonds", C_HEARTS: "Hearts",
                 C_SPADES: "Spades", C_NOTRUMPS: "NoTrumps", C_ALLTRUMPS: "AllTrumps"}
RANKS = list(range(7, 15))
ALL_CARDS = [(s, r) for s in (CLUBS, DIAMONDS, SPADES, HEARTS) for r in RANKS]
PARTNER = {1: 3, 2: 4, 3: 1, 4: 2}


def options_trump(contract):
    """Trump suit stored in GameOptions: a real suit for suit contracts, 4 (=none) otherwise."""
    return CONTRACT_TRUMP_SUIT.get(contract, 4)


def game_sort(hand):
    """The game keeps hands sorted (FUN_004852C8 after the deal); sequence detection
    scans the hand in stored order, so vectors must use the same ordering."""
    return sorted(hand, key=lambda c: (SUITMAP[c[0]], c[1]))


def gen_validcards(emu, n, rng):
    cases = []
    attempts = 0
    while len(cases) < n and attempts < n * 20:
        attempts += 1
        contract = rng.choice([C_CLUBS, C_DIAMONDS, C_HEARTS, C_SPADES, C_NOTRUMPS, C_ALLTRUMPS])
        deck = ALL_CARDS[:]
        rng.shuffle(deck)
        hands = {p: deck[(p - 1) * 8:p * 8] for p in (1, 2, 3, 4)}

        # advance the deal: every player has already played `done` cards
        done = rng.randint(0, 6)
        for p in hands:
            for _ in range(done):
                hands[p].pop(rng.randrange(len(hands[p])))

        me = rng.randint(1, 4)
        t = rng.randint(0, 3)            # cards already in the current trick
        # the t players immediately before me played, in order
        order = [((me - 1 - (t - i)) % 4) + 1 for i in range(t)]
        played = []
        ok = True
        for p in order:
            if not hands[p]:
                ok = False
                break
            played.append((p, hands[p].pop(rng.randrange(len(hands[p])))))
        if not ok or not hands[me]:
            continue

        led_suit = played[0][1][0] if played else 4
        my_hand = game_sort(hands[me])

        # ---- build the object graph and ask the ORIGINAL code ----
        emu.reset_heap()
        emu.set_contract(contract)
        emu.set_local_mode()
        players = {p: emu.make_player(p) for p in (1, 2, 3, 4)}
        my_cards = [emu.make_card(s, r) for (s, r) in my_hand]
        group = emu.make_group(me, my_cards)
        slots = {}
        for i, (p, (s, r)) in enumerate(played):
            slots[i] = emu.make_card(s, r, owner=players[p])
        table = emu.make_table(led_suit, options_trump(contract), slots)
        groups = [emu.make_group(p, []) for p in (1, 2, 3, 4)]
        groups[me - 1] = group
        form = emu.make_form(table, groups)

        try:
            legal = [i for i in range(len(my_hand))
                     if (emu.call(0x4767F8, eax=form, edx=group, ecx=i) & 0xFF) != 0]
        except EmuError as e:
            print(f"  [skip] emulation error: {e}", file=sys.stderr)
            continue

        cases.append({
            "contract": CONTRACT_NAME[contract],
            "seat": me,
            "hand": [[SUIT_LETTER[s], r] for (s, r) in my_hand],
            "trick": [[p, SUIT_LETTER[s], r] for (p, (s, r)) in played],
            "legal": legal,
        })
    return cases


def gen_announces(emu, n, rng):
    """FUN_00479EE8(form, group, outbuf) fills an 8-byte descriptor of the hand's declarations."""
    cases = []
    for _ in range(n):
        deck = ALL_CARDS[:]
        rng.shuffle(deck)
        hand = game_sort(deck[:8])
        emu.reset_heap()
        emu.set_contract(C_ALLTRUMPS)
        cards = [emu.make_card(s, r) for (s, r) in hand]
        group = emu.make_group(1, cards)
        table = emu.make_table(4, 4, {})
        form = emu.make_form(table, [group] * 4)
        out = emu.alloc(32)
        try:
            emu.call(0x479EE8, eax=form, edx=group, ecx=out)
        except EmuError as e:
            print(f"  [skip] announce emulation error: {e}", file=sys.stderr)
            continue
        desc = [emu.r8(out + i) for i in range(8)]
        cases.append({"hand": [[SUIT_LETTER[s], r] for (s, r) in hand], "desc": desc})
    return cases


def gen_bidscores(emu, n, rng):
    """Run choosegame's own scoring loop over 4 five-card hands and record every score."""
    cases = []
    for _ in range(n):
        deck = ALL_CARDS[:]
        rng.shuffle(deck)
        hands = {p: game_sort(deck[(p - 1) * 5:p * 5]) for p in (1, 2, 3, 4)}
        emu.reset_heap()
        emu.set_local_mode()
        # At bid time a hand holds 8 slots: the 5 dealt cards plus 3 still-empty ones
        # (empty == card+0x23 != 0), which the announce scan skips.
        groups = []
        for p in (1, 2, 3, 4):
            slots = [emu.make_card(s, r) for (s, r) in hands[p]]
            slots += [emu.make_card(0, 7, hidden=1) for _ in range(3)]
            groups.append(emu.make_group(p, slots))
        table = emu.make_table(4, 4, {})
        form = emu.make_form(table, groups)
        try:
            scores = emu.bid_scores(form)
        except EmuError as e:
            print(f"  [skip] bid-score emulation error: {e}", file=sys.stderr)
            continue
        for p in (1, 2, 3, 4):
            cases.append({
                "hand": [[SUIT_LETTER[s], r] for (s, r) in hands[p]],
                "scores": {CONTRACT_NAME[c]: scores[p][c] for c in range(1, 7)},
            })
    return cases


def gen_cardtables(emu):
    rows = []
    for contract in (C_CLUBS, C_DIAMONDS, C_HEARTS, C_SPADES, C_NOTRUMPS, C_ALLTRUMPS):
        for s in (CLUBS, DIAMONDS, SPADES, HEARTS):
            for r in RANKS:
                rows.append({
                    "contract": CONTRACT_NAME[contract], "suit": SUIT_LETTER[s], "rank": r,
                    "strength": emu.strength(s, r, contract),
                    "points": emu.points(s, r, contract),
                })
    return rows


def main():
    n = int(sys.argv[1]) if len(sys.argv) > 1 else 500
    seed = int(sys.argv[2]) if len(sys.argv) > 2 else 7
    rng = random.Random(seed)
    emu = BelotEmu()
    print("emulator ready; generating vectors from the original code...")

    tables = gen_cardtables(emu)
    print(f"  cardtables: {len(tables)}")
    ann = gen_announces(emu, max(50, n // 4), rng)
    print(f"  announces:  {len(ann)}")
    bids = gen_bidscores(emu, max(50, n // 4), rng)
    print(f"  bidscores:  {len(bids)}")
    vc = gen_validcards(emu, n, rng)
    print(f"  validcards: {len(vc)}")

    out = {"cardtables": tables, "announces": ann, "bidscores": bids, "validcards": vc}
    path = sys.argv[3] if len(sys.argv) > 3 else "golden.json"
    with open(path, "w", encoding="utf-8") as f:
        json.dump(out, f)
    print("written ->", path)


if __name__ == "__main__":
    main()
