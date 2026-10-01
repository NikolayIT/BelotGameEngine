namespace Belot.AI.ClaudePlayer.Heuristic
{
    using System;
    using System.Numerics;

    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// The heuristic player's bids, by the rules of the belot.bg academy and the other sources in
    /// HEURISTIC_PLAYER.md. A suit is bid with its jack or nine and support: the jack, nine and
    /// ace, or the jack and two more trumps with a side ace, never a nine with small trumps
    /// alone. No trumps needs aces with their tens and is much better for the first bidder, who
    /// leads. All trumps needs two jacks and more than a bare nine. A partner's suit bid shows
    /// its jack or nine, which helps all trumps; another suit takes the contract from the partner
    /// only when it is clearly better. Ahead in the match the player bids more carefully,
    /// behind more boldly. It doubles only when its cards in the opponents' contract should
    /// beat it, and every bid is natural: a person reads it as what it is.
    /// </summary>
    internal static class HeuristicBidding
    {
        private const int Nine = CardMemory.Nine;
        private const int Ten = CardMemory.Ten;
        private const int Jack = CardMemory.Jack;
        private const int Queen = CardMemory.Queen;
        private const int King = CardMemory.King;
        private const int Ace = CardMemory.Ace;

        public static BidType Choose(PlayerGetBidContext context, HeuristicSettings settings)
        {
            var hand = CardMemory.ToMask(context.MyCards);
            var me = context.MyPosition.Index();
            var available = context.AvailableBids;
            var contract = context.CurrentContract;
            var contractType = contract.Type & ~(BidType.Double | BidType.ReDouble);
            var ourContract = contractType != BidType.Pass && ((contract.Player.Index() ^ me) & 1) == 0;
            var first = context.FirstToPlayInTheRound.Index() == me;

            var partnerSuits = 0;
            var opponentSuits = 0;
            var partnerNoTrumps = false;
            var opponentBids = 0;
            foreach (var bid in context.Bids)
            {
                var seat = bid.Player.Index();
                var type = bid.Type;
                if (type == BidType.Pass || type == BidType.Double || type == BidType.ReDouble)
                {
                    continue;
                }

                var partner = ((seat ^ me) & 1) == 0 && seat != me;
                if (!partner && ((seat ^ me) & 1) != 0)
                {
                    opponentBids++;
                }

                if (type == BidType.NoTrumps)
                {
                    partnerNoTrumps |= partner;
                    continue;
                }

                if (type == BidType.AllTrumps)
                {
                    continue;
                }

                var suitBit = 1 << (int)type.ToCardSuit();
                if (partner)
                {
                    partnerSuits |= suitBit;
                }
                else if (((seat ^ me) & 1) != 0)
                {
                    opponentSuits |= suitBit;
                }
            }

            var boldness = settings.ScoreAware ? Boldness(context, me) : 0;
            if (contractType != BidType.Pass && !ourContract)
            {
                boldness += settings.CompeteBonus;
            }

            var bestBid = BidType.Pass;
            var bestMargin = 0.0;
            for (var suit = 0; suit < 4; suit++)
            {
                var bid = (BidType)(1 << suit);
                if (!available.HasFlag(bid))
                {
                    continue;
                }

                var strength = SuitStrength(hand, suit);
                if (double.IsNegativeInfinity(strength))
                {
                    continue;
                }

                strength += boldness - (opponentBids * settings.OpponentBidPenalty);
                if (ourContract)
                {
                    strength -= settings.OverPartnerPenalty;
                }

                if ((hand & Bit(suit, Jack)) == 0)
                {
                    strength -= settings.NoJackPenalty;
                }

                Consider(bid, strength - settings.SuitThreshold, ref bestBid, ref bestMargin);
            }

            // Natural bids only: no trumps with an ace, all trumps with a jack (a suit needs its jack
            // or nine and another card), as a person reads them.
            if (available.HasFlag(BidType.NoTrumps) && (hand & 0x80808080u) != 0)
            {
                var strength = NoTrumpsStrength(hand, settings.NoTrumpsAceValue) + boldness - (opponentBids * settings.OpponentBidPenalty);
                if (first)
                {
                    strength += settings.FirstNoTrumpsBonus;
                }

                strength -= Unstopped(hand, opponentSuits, Ace) * 2;
                if (ourContract && !partnerNoTrumps)
                {
                    strength -= settings.OverPartnerPenalty;
                }

                Consider(BidType.NoTrumps, (strength - settings.NoTrumpsThreshold) * settings.NoTrumpsScale, ref bestBid, ref bestMargin);
            }

            if (available.HasFlag(BidType.AllTrumps) && (hand & 0x10101010u) != 0)
            {
                var strength = AllTrumpsStrength(hand) + boldness - (opponentBids * settings.AllTrumpsOverOpponentPenalty);
                if (first)
                {
                    strength += settings.FirstAllTrumpsBonus;
                }

                if (partnerSuits != 0)
                {
                    strength += settings.PartnerSuitAllTrumpsBonus;
                }

                if (partnerNoTrumps)
                {
                    strength += 1;
                }

                strength -= Unstopped(hand, opponentSuits, Jack) * 1.5;
                Consider(BidType.AllTrumps, (strength - settings.AllTrumpsThreshold) * settings.AllTrumpsScale, ref bestBid, ref bestMargin);
            }

            if (bestBid == BidType.Pass && settings.MayDouble)
            {
                if (available.HasFlag(BidType.Double) && Defence(hand, contractType, context.FirstToPlayInTheRound.Index() == me) >= DoubleThreshold(contractType, settings))
                {
                    return BidType.Double;
                }

                if (available.HasFlag(BidType.ReDouble) && contract.Player.Index() == me
                    && OwnStrength(hand, contractType, first, settings) >= settings.RedoubleMargin)
                {
                    return BidType.ReDouble;
                }

                // The opponents doubled the partner's contract: with support for it, redouble.
                if (available.HasFlag(BidType.ReDouble) && contract.Player.Index() == ((me + 2) & 3)
                    && Support(hand, contractType) >= settings.RedoublePartnerSupport)
                {
                    return BidType.ReDouble;
                }
            }

            return bestBid;
        }

        /// <summary>
        /// The strength of five cards for the suit as trumps, or negative infinity when the bid would
        /// not be natural (without the jack or nine, or with no other card of the suit).
        /// </summary>
        public static double SuitStrength(uint hand, int suit)
        {
            var trumps = hand & SimTables.SuitMasks[suit];
            var count = BitOperations.PopCount(trumps);
            var jack = Has(trumps, suit, Jack);
            var nine = Has(trumps, suit, Nine);
            if (count < 2 || (!jack && !nine))
            {
                return double.NegativeInfinity;
            }

            var strength = 0.0;
            strength += jack ? 6 : 0;
            strength += nine ? (jack ? 5 : 4) : 0;
            strength += Has(trumps, suit, Ace) ? (jack || nine ? 3 : 2.5) : 0;
            strength += Has(trumps, suit, Ten) ? 2 : 0;
            var king = Has(trumps, suit, King);
            var queen = Has(trumps, suit, Queen);
            strength += (king ? 1.5 : 0) + (queen ? 1.5 : 0) + (king && queen ? 2 : 0);
            strength += BitOperations.PopCount(trumps & (Bit(suit, 0) | Bit(suit, 1)));
            strength += count >= 3 ? 1.5 : 0;
            strength += count >= 4 ? 2 : 0;
            for (var side = 0; side < 4; side++)
            {
                if (side == suit)
                {
                    continue;
                }

                var cards = hand & SimTables.SuitMasks[side];
                var ace = Has(cards, side, Ace);
                strength += ace ? 3 : 0;
                strength += Has(cards, side, Ten) ? (ace ? 2 : BitOperations.PopCount(cards) >= 2 ? 1 : .5) : 0;
                strength += Has(cards, side, King) && ace ? .5 : 0;
                strength += cards == 0 && count >= 3 ? .5 : 0;
            }

            return strength + Combinations(hand);
        }

        /// <summary>The strength of five cards in all trumps: jacks, the nines with them, the aces behind.</summary>
        public static double AllTrumpsStrength(uint hand)
        {
            var strength = 0.0;
            var jacks = 0;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = hand & SimTables.SuitMasks[suit];
                if (cards == 0)
                {
                    continue;
                }

                var count = BitOperations.PopCount(cards);
                var jack = Has(cards, suit, Jack);
                var nine = Has(cards, suit, Nine);
                var ace = Has(cards, suit, Ace);
                jacks += jack ? 1 : 0;
                strength += jack ? 6 : 0;
                strength += nine ? (jack ? 5 : count >= 2 ? 2.5 : 1) : 0;
                strength += ace ? (jack && nine ? 3 : jack || nine ? 2 : 1) : 0;
                strength += Has(cards, suit, Ten) ? (jack && nine && ace ? 2 : (jack ? 1 : 0) + (nine ? 1 : 0) + (ace ? .5 : 0) > 1 ? 1 : .3) : 0;
                var king = Has(cards, suit, King);
                var queen = Has(cards, suit, Queen);
                strength += (king ? .3 : 0) + (queen ? .3 : 0) + (king && queen ? 2 : 0);
                strength += jack && count >= 3 ? 1 : 0;
            }

            if (jacks < 2)
            {
                // Both jacks are nearly a must (the academy and its sources).
                strength -= 4;
            }

            return strength + Combinations(hand);
        }

        /// <summary>The strength of five cards in no trumps: aces, the tens with them, long suits headed by them.</summary>
        public static double NoTrumpsStrength(uint hand, double aceValue = 5)
        {
            var strength = 0.0;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = hand & SimTables.SuitMasks[suit];
                if (cards == 0)
                {
                    continue;
                }

                var count = BitOperations.PopCount(cards);
                var ace = Has(cards, suit, Ace);
                var ten = Has(cards, suit, Ten);
                var king = Has(cards, suit, King);
                strength += ace ? aceValue : 0;
                strength += ten ? (ace ? 4 : count >= 2 ? 1 : .3) : 0;
                strength += king ? (ace && ten ? 2 : ace || ten ? .8 : .2) : 0;
                strength += king && Has(cards, suit, Queen) ? .5 : 0;
                strength += ace && count >= 3 ? 1 : 0;
            }

            return strength;
        }

        /// <summary>The declarations in five cards (sequences, carres), as strength.</summary>
        public static double Combinations(uint hand)
        {
            var strength = 0.0;
            var clubs = hand & 0xFFu;
            var diamonds = (hand >> 8) & 0xFFu;
            var hearts = (hand >> 16) & 0xFFu;
            var spades = hand >> 24;
            var carres = clubs & diamonds & hearts & spades;
            for (var rest = carres; rest != 0; rest &= rest - 1)
            {
                var type = BitOperations.TrailingZeroCount(rest);
                strength += type == Jack ? 20 : type == Nine ? 15 : type >= Ten ? 10 : 0;
            }

            for (var suit = 0; suit < 4; suit++)
            {
                var bits = (hand >> (suit * 8)) & 0xFFu & ~carres;
                var run = 0;
                for (var type = 0; type <= 8; type++)
                {
                    if (type < 8 && ((bits >> type) & 1) != 0)
                    {
                        run++;
                        continue;
                    }

                    strength += run >= 5 ? 10 : run == 4 ? 5 : run == 3 ? 2 : 0;
                    run = 0;
                }
            }

            return strength;
        }

        private static void Consider(BidType bid, double margin, ref BidType bestBid, ref double bestMargin)
        {
            if (margin >= 0 && (bestBid == BidType.Pass || margin > bestMargin))
            {
                bestBid = bid;
                bestMargin = margin;
            }
        }

        // Bolder when the opponents are close to winning or far ahead, more careful well ahead.
        private static double Boldness(PlayerGetBidContext context, int me)
        {
            var ours = (me & 1) == 0 ? context.SouthNorthPoints : context.EastWestPoints;
            var theirs = (me & 1) == 0 ? context.EastWestPoints : context.SouthNorthPoints;
            var boldness = 0.0;
            if (theirs >= 125 && theirs > ours)
            {
                boldness += 1.5;
            }
            else if (theirs - ours >= 50)
            {
                boldness += 1;
            }
            else if (ours - theirs >= 50)
            {
                boldness -= 1;
            }

            return boldness;
        }

        // The opponents' suits in which the hand holds no stopper (the ace for no trumps, the jack for all trumps).
        private static int Unstopped(uint hand, int suits, int stopper)
        {
            var count = 0;
            for (var suit = 0; suit < 4; suit++)
            {
                if ((suits & (1 << suit)) != 0 && !Has(hand, suit, stopper))
                {
                    count++;
                }
            }

            return count;
        }

        // How well the hand defends against the opponents' contract.
        private static double Defence(uint hand, BidType contract, bool leads)
        {
            var defence = 0.0;
            if (contract == BidType.NoTrumps)
            {
                for (var suit = 0; suit < 4; suit++)
                {
                    var cards = hand & SimTables.SuitMasks[suit];
                    var ace = Has(cards, suit, Ace);
                    defence += ace ? 4 : 0;
                    defence += Has(cards, suit, Ten) ? (ace ? 3 : BitOperations.PopCount(cards) >= 2 ? 1 : 0) : 0;
                }

                return defence + (leads ? 2 : 0);
            }

            if (contract == BidType.AllTrumps)
            {
                for (var suit = 0; suit < 4; suit++)
                {
                    var cards = hand & SimTables.SuitMasks[suit];
                    var jack = Has(cards, suit, Jack);
                    var nine = Has(cards, suit, Nine);
                    defence += jack ? 5 : 0;
                    defence += nine ? (jack ? 4 : BitOperations.PopCount(cards) >= 2 ? 2 : 0) : 0;
                    defence += Has(cards, suit, Ace) && (jack || nine) ? 2 : 0;
                }

                return defence + (leads ? 1 : 0);
            }

            var trumpSuit = (int)contract.ToCardSuit();
            var trumps = hand & SimTables.SuitMasks[trumpSuit];
            var trumpJack = Has(trumps, trumpSuit, Jack);
            defence += trumpJack ? 6 : 0;
            defence += Has(trumps, trumpSuit, Nine) ? (trumpJack ? 5 : 4) : 0;
            defence += Has(trumps, trumpSuit, Ace) ? 2 : 0;
            defence += Has(trumps, trumpSuit, Ten) ? 1.5 : 0;
            defence += BitOperations.PopCount(trumps & ~(Bit(trumpSuit, Jack) | Bit(trumpSuit, Nine) | Bit(trumpSuit, Ace) | Bit(trumpSuit, Ten)));
            for (var suit = 0; suit < 4; suit++)
            {
                if (suit != trumpSuit && Has(hand, suit, Ace))
                {
                    defence += 2;
                }
            }

            return defence;
        }

        // What the hand adds to the partner's contract: trumps' jack and nine, aces.
        private static double Support(uint hand, BidType contract)
        {
            var support = 0.0;
            for (var suit = 0; suit < 4; suit++)
            {
                var cards = hand & SimTables.SuitMasks[suit];
                if (contract == BidType.NoTrumps)
                {
                    support += Has(cards, suit, Ace) ? (Has(cards, suit, Ten) ? 5 : 3) : 0;
                }
                else if (contract == BidType.AllTrumps)
                {
                    support += Has(cards, suit, Jack) ? (Has(cards, suit, Nine) ? 5 : 3) : Has(cards, suit, Nine) ? 1 : 0;
                }
                else if (suit == (int)contract.ToCardSuit())
                {
                    support += (Has(cards, suit, Jack) ? 3 : 0) + (Has(cards, suit, Nine) ? 2 : 0) + (Has(cards, suit, Ace) ? 1 : 0);
                }
                else
                {
                    support += Has(cards, suit, Ace) ? 1 : 0;
                }
            }

            return support;
        }

        private static double DoubleThreshold(BidType contract, HeuristicSettings settings) =>
            contract == BidType.NoTrumps ? settings.DoubleNoTrumpsThreshold :
            contract == BidType.AllTrumps ? settings.DoubleAllTrumpsThreshold : settings.DoubleSuitThreshold;

        // The strength above its threshold for the team's own contract (to redouble).
        private static double OwnStrength(uint hand, BidType contract, bool first, HeuristicSettings settings)
        {
            if (contract == BidType.NoTrumps)
            {
                return NoTrumpsStrength(hand, settings.NoTrumpsAceValue) + (first ? settings.FirstNoTrumpsBonus : 0) - settings.NoTrumpsThreshold;
            }

            if (contract == BidType.AllTrumps)
            {
                return AllTrumpsStrength(hand) + (first ? settings.FirstAllTrumpsBonus : 0) - settings.AllTrumpsThreshold;
            }

            var strength = SuitStrength(hand, (int)contract.ToCardSuit());
            return double.IsNegativeInfinity(strength) ? double.NegativeInfinity : strength - settings.SuitThreshold;
        }

        private static bool Has(uint cards, int suit, int type) => (cards & Bit(suit, type)) != 0;

        private static uint Bit(int suit, int type) => 1u << ((suit * 8) + type);
    }
}
