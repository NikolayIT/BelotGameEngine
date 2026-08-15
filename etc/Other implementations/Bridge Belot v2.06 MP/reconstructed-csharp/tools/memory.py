#!/usr/bin/env python3
"""
The AI's per-round memory block at 0x48BEB8..0x48BF17, and the rules that maintain it.

The card-play routine reads this block but never fills it in: the game keeps it up to date from
three other places, so a harness that wants the AI to behave the way it does in a real round has
to do the same. Transcribed from

  * 0x46EBC8  end of a trick - memF, and memB/memC/memD/memE for the trick WINNER
  * 0x46EE6C  a card being played (the click handler, i.e. the human's cards) - memC when
              leading, memG on an off-suit discard, memD when that discard is the contract's
              boss card
  * 0x473E86  player2BeforePlay's own epilogue - the same memC and memG updates for the card the
              AI just chose; it does not do the memD signal

BelotV2.AI/PlayMemory.cs is the same thing in C#; `gen_play_vectors.py` records the block in each
vector so the two can be compared byte for byte.

Layout (bytes; players 1..4, suits 0..3 = C, D, S, H):

    0x48BEB8  memA[p]     reset to 0, never written again
    0x48BEBE  memBE       the contract's doubling multiplier: 1 plain, 2 doubled, 4 redoubled
    0x48BEC0  memB[p]     the suit p bid, or 4; cleared once that suit is led
    0x48BEC4  memC[p]     the suit p opened of their own accord, or 4
    0x48BEC8  memD[p]     the suit p signalled by discarding its boss card, or 4
    0x48BECC  memE[p]     1 once p's hand has run short
    0x48BED0  memF[s]     1 once suit s has been led
    0x48BED4  memG[p][s]  int32, how often p discarded suit s off-suit
"""

BASE = 0x48BEB8
SIZE = 0x60
NO_SUIT = 4

PARTNER_OF = {1: 3, 2: 4, 3: 1, 4: 2}
TRUMP_ORDER = (1, 2, 7, 5, 8, 3, 4, 6)      # @0x489D7C, indexed rank-7
NO_TRUMP_ORDER = (1, 2, 3, 7, 4, 5, 6, 8)   # @0x489D84
TRUMP_OF = {1: 0, 2: 1, 3: 3, 4: 2}         # contract -> suit index


def new_round(level=1):
    """What choosegame leaves behind once a contract is settled.

    `level` is the contract's doubling multiplier at 0x48BEBE (@0x4799B6): 1 plain, 2 doubled,
    4 redoubled. The decision tree reads it, so it is part of the state, not a detail.
    """
    m = bytearray(SIZE)
    m[0x48BEBE - BASE] = level
    for p in range(4):
        m[0x48BEC0 - BASE + p] = NO_SUIT
        m[0x48BEC4 - BASE + p] = NO_SUIT
        m[0x48BEC8 - BASE + p] = NO_SUIT
    return m


def mem_b(m, player):
    return m[0x48BEBF - BASE + player]


def mem_c(m, player):
    return m[0x48BEC3 - BASE + player]


def mem_d(m, player):
    return m[0x48BEC7 - BASE + player]


def mem_f(m, suit):
    return m[0x48BED0 - BASE + suit]


def mem_g(m, player, suit):
    i = 0x48BEC4 - BASE + player * 0x10 + suit * 4
    return int.from_bytes(m[i:i + 4], "little")


def set_bid_suit(m, player, suit):
    m[0x48BEBF - BASE + player] = suit


def on_card_played(m, contract, player, suit, rank, led_suit, signals):
    """led_suit is 4 when this card is the opening lead of the trick."""
    if led_suit == NO_SUIT:
        if suit != mem_b(m, PARTNER_OF[player]):
            m[0x48BEC3 - BASE + player] = suit
        return

    if suit == led_suit:
        return

    i = 0x48BEC4 - BASE + player * 0x10 + suit * 4
    m[i:i + 4] = (int.from_bytes(m[i:i + 4], "little") + 1).to_bytes(4, "little")

    if signals and ((contract == 6 and rank == 11) or (contract == 5 and rank == 14)):
        m[0x48BEC7 - BASE + player] = suit


def on_trick_end(m, contract, winner, led_suit, cards_left):
    """Only the winner's entries are touched."""
    m[0x48BED0 - BASE + led_suit] = 1
    if mem_b(m, winner) == led_suit:
        m[0x48BEBF - BASE + winner] = NO_SUIT
    if mem_c(m, winner) == led_suit:
        m[0x48BEC3 - BASE + winner] = NO_SUIT
    m[0x48BEC7 - BASE + winner] = NO_SUIT
    if cards_left < (6 if contract == 5 else 7):
        m[0x48BECB - BASE + winner] = 1


def trick_winner(contract, trick):
    """trick: [(player, (suit, rank)), ...] in play order."""
    trump = TRUMP_OF[contract] if contract <= 4 else NO_SUIT
    led = trick[0][1][0]
    best = 0
    for k in range(1, len(trick)):
        if _beats(trick[k][1], trick[best][1], led, trump, contract):
            best = k
    return trick[best][0]


def _beats(card, best, led, trump, contract):
    card_trump = card[0] == trump
    best_trump = best[0] == trump
    if card_trump != best_trump:
        return card_trump
    if card[0] != best[0]:
        return False
    if not card_trump and card[0] != led:
        return False
    order = TRUMP_ORDER if (card_trump or contract == 6) else NO_TRUMP_ORDER
    return order[card[1] - 7] > order[best[1] - 7]
