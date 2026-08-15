#!/usr/bin/env python3
"""
Golden vectors for the two remaining behaviours, taken from the ORIGINAL code:

  trickwinner : FUN_004766A8   -> Rules.WinnerIndex
  playai      : player2BeforePlay @0x46F5C0 -> PlayAi.Play

Play positions are produced by letting the emulated original AI play complete rounds against
itself, recording every decision together with the exact information state it had.
"""
import json
import random
import sys

from emu import (SUITMAP, CLUBS, DIAMONDS, SPADES, HEARTS, EmuError,
                 C_CLUBS, C_DIAMONDS, C_HEARTS, C_SPADES, C_NOTRUMPS, C_ALLTRUMPS,
                 CONTRACT_TRUMP_SUIT)
from emu_play import PlayEmu, MATRIX, APP_VAR
import memory as mem_model

SUIT_LETTER = {CLUBS: "C", DIAMONDS: "D", SPADES: "S", HEARTS: "H"}
CONTRACT_NAME = {C_CLUBS: "Clubs", C_DIAMONDS: "Diamonds", C_HEARTS: "Hearts",
                 C_SPADES: "Spades", C_NOTRUMPS: "NoTrumps", C_ALLTRUMPS: "AllTrumps"}
CONTRACTS = [C_CLUBS, C_DIAMONDS, C_HEARTS, C_SPADES, C_NOTRUMPS, C_ALLTRUMPS]
RANKS = list(range(7, 15))
ALL_CARDS = [(s, r) for s in (CLUBS, DIAMONDS, SPADES, HEARTS) for r in RANKS]


def game_sort(hand):
    return sorted(hand, key=lambda c: (SUITMAP[c[0]], c[1]))


def emu_trick_winner(emu, contract, plays):
    """plays: list of (player, (suit,rank)) in play order -> winning player, via FUN_004766A8."""
    emu.reset_heap()
    emu.set_contract(contract)
    players = {p: emu.make_player(p) for p in (1, 2, 3, 4)}
    slots, owner_of_slot = {}, {}
    for i, (p, (s, r)) in enumerate(plays):
        slots[i] = emu.make_card(s, r, owner=players[p])
        owner_of_slot[i] = p
    led = plays[0][1][0]
    table = emu.make_table(led, CONTRACT_TRUMP_SUIT.get(contract, 4), slots)
    form = emu.make_form(table, [emu.make_group(p, []) for p in (1, 2, 3, 4)])
    idx = emu.call(0x4766A8, eax=form, edx=table) & 0xFF
    return owner_of_slot.get(idx)


def gen_trickwinner(emu, n, rng):
    cases = []
    while len(cases) < n:
        contract = rng.choice(CONTRACTS)
        deck = ALL_CARDS[:]
        rng.shuffle(deck)
        cards = deck[:4]
        leader = rng.randint(1, 4)
        plays = [(((leader - 1 + i) % 4) + 1, cards[i]) for i in range(4)]
        try:
            w = emu_trick_winner(emu, contract, plays)
        except EmuError as e:
            print(f"  [skip] trickwinner: {e}", file=sys.stderr)
            continue
        if w is None:
            continue
        cases.append({
            "contract": CONTRACT_NAME[contract],
            "plays": [[p, SUIT_LETTER[s], r] for (p, (s, r)) in plays],
            "winner": w,
        })
    return cases


def play_round(emu, contract, hands, leader, rng, cases, declarer=1, level=1):
    """Let the emulated original AI play a whole round; record every decision.

    `declarer` and `level` are the seat that won the contract and its doubling multiplier. Both
    are read by the decision tree, and pinning them (as this harness first did) hides whichever
    branches depend on them.
    """
    hands = {p: game_sort(list(hands[p])) for p in hands}
    history = []          # (player, card) in play order, completed tricks first
    voids = {p: set() for p in (1, 2, 3, 4)}
    played = set()
    # The AI's memory for this round. Seat 1 is the human, so it is the only seat whose discards
    # go through the click handler and can therefore signal.
    mem = mem_model.new_round(level)
    emu.declarer = declarer
    # A real round follows an auction, so the AI starts play already remembering which suit each
    # seat named. Without that memB stays empty and the branches that lead a partner's bid suit
    # are unreachable, so give it an auction's worth of state.
    bid_suits = {}
    for p in (1, 2, 3, 4):
        bid_suits[p] = rng.choice([mem_model.NO_SUIT, mem_model.NO_SUIT,
                                   CLUBS, DIAMONDS, SPADES, HEARTS])
        mem_model.set_bid_suit(mem, p, bid_suits[p])
    emu.round_memory = mem

    for _ in range(8):
        trick = []
        for k in range(4):
            me = ((leader - 1 + k) % 4) + 1
            hand = hands[me]
            if not hand:
                return
            state = {
                "contract": CONTRACT_NAME[contract],
                "seat": me,
                "hand": [[SUIT_LETTER[s], r] for (s, r) in hand],
                "trick": [[p, SUIT_LETTER[s], r] for (p, (s, r)) in trick],
                "history": [[p, SUIT_LETTER[s], r] for (p, (s, r)) in history],
                "bidSuits": [bid_suits[p] for p in (1, 2, 3, 4)],
                "declarer": declarer,
                "level": level,
            }
            # player2BeforePlay is the handler for seats 2..4 only (seat 1 is the human,
            # and the routine range-checks on that). Seat 1 just plays its first legal card.
            try:
                if me == 1:
                    legal = emu.legal_indices(contract, me, hand, trick, played, voids)
                    if not legal:
                        return
                    idx = legal[0]
                else:
                    idx, internals = emu.choose_with_internals(
                        contract, me, hand, trick, played, voids)
                    if internals is not None:
                        state["internals"] = {
                            k: internals[k] for k in
                            ("candCount", "playersLeft", "sureCount", "middleCount", "loserCount",
                             "l1Count", "l2Count", "l3Count", "l4Count", "l5Count",
                             "trump", "longSide", "anyOppTrump", "trumpRanksOut",
                             "candidates", "buckets", "lists", "suitCount", "sureBySuit",
                             "highTrump", "rngSeed")}
                        state["internals"]["mem"] = list(mem)
            except EmuError as e:
                print(f"  [skip] play: {e}", file=sys.stderr)
                return
            if idx is None or idx >= len(hand):
                return
            if me != 1:
                state["chosen"] = idx
                cases.append(state)

            card = hand.pop(idx)
            led_suit = mem_model.NO_SUIT if k == 0 else trick[0][1][0]
            mem_model.on_card_played(mem, contract, me, card[0], card[1], led_suit, me == 1)
            trick.append((me, card))
            played.add(card)
            if trick and card[0] != trick[0][1][0]:
                voids[me].add(trick[0][1][0])

        winner = emu_trick_winner(emu, contract, trick)
        if winner is None:
            return
        mem_model.on_trick_end(mem, contract, winner, trick[0][1][0], len(hands[winner]))
        history.extend(trick)
        leader = winner


def gen_playai(emu, rounds, rng):
    cases = []
    for _ in range(rounds):
        contract = rng.choice(CONTRACTS)
        deck = ALL_CARDS[:]
        rng.shuffle(deck)
        hands = {p: deck[(p - 1) * 8:p * 8] for p in (1, 2, 3, 4)}
        play_round(emu, contract, hands, rng.randint(1, 4), rng, cases,
                   declarer=rng.randint(1, 4), level=rng.choice([1, 1, 1, 2, 4]))
    return cases


def main():
    rounds = int(sys.argv[1]) if len(sys.argv) > 1 else 20
    seed = int(sys.argv[2]) if len(sys.argv) > 2 else 5
    path = sys.argv[3] if len(sys.argv) > 3 else "golden_play.json"
    rng = random.Random(seed)
    emu = PlayEmu()
    print("emulator ready")
    tw = gen_trickwinner(emu, 400, rng)
    print(f"  trickwinner: {len(tw)}")
    pa = gen_playai(emu, rounds, rng)
    print(f"  playai:      {len(pa)} decisions from {rounds} rounds")
    with open(path, "w", encoding="utf-8") as f:
        json.dump({"trickwinner": tw, "playai": pa}, f)
    print("written ->", path)


if __name__ == "__main__":
    main()
