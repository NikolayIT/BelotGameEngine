namespace Belot.AI.ClaudePlayer.Search
{
    using System;
    using System.Numerics;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;

    /// <summary>
    /// The combinations (carres and sequences) of a deal: what a player who declares everything
    /// offered registers, and which of them score (ValidAnnouncesService.UpdateActiveAnnounces).
    /// The belote is not here: the simulator scores it when the card is played.
    /// </summary>
    internal static class AnnounceScorer
    {
        public const int MaxAnnounces = 16;

        private const int Nine = (int)CardType.Nine;
        private const int Jack = (int)CardType.Jack;

        // The trump order of each card type, which ranks carres of equal value.
        private static readonly int[] TrumpOrders = { 1, 2, 7, 5, 8, 3, 4, 6 };

        // The types a carre of unknown rank may have: ten, queen, king or ace.
        private static readonly int[] PlainCarreRanks = { 3, 5, 6, 7 };

        /// <summary>
        /// Appends what a player with this 8-card hand registers when declaring everything
        /// offered: the carres, then the sequences of the cards the carres leave free (a sequence
        /// through a carre card shares a card with the carre, so it is not registered).
        /// </summary>
        /// <returns>The new count.</returns>
        public static int AddDeclaredCombinations(uint hand, int seat, DeclaredAnnounce[] buffer, int count)
        {
            var clubs = hand & 0xFFu;
            var diamonds = (hand >> 8) & 0xFFu;
            var hearts = (hand >> 16) & 0xFFu;
            var spades = hand >> 24;
            var carres = clubs & diamonds & hearts & spades & 0b11111100u;
            for (var rest = carres; rest != 0; rest &= rest - 1)
            {
                var type = BitOperations.TrailingZeroCount(rest);
                var announceType = type == Jack ? AnnounceType.FourJacks :
                                   type == Nine ? AnnounceType.FourNines : AnnounceType.FourOfAKind;
                buffer[count++] = new DeclaredAnnounce(seat, announceType, type);
            }

            var free = ~carres & 0xFFu;
            count = AddSequences(clubs & free, seat, buffer, count);
            count = AddSequences(diamonds & free, seat, buffer, count);
            count = AddSequences(hearts & free, seat, buffer, count);
            return AddSequences(spades & free, seat, buffer, count);
        }

        /// <summary>Gives every combination of hidden rank (-1) a random rank it could have.</summary>
        public static void ResolveHiddenRanks(DeclaredAnnounce[] announces, int count, Random random)
        {
            for (var i = 0; i < count; i++)
            {
                if (announces[i].Rank >= 0)
                {
                    continue;
                }

                if (announces[i].Type == AnnounceType.FourOfAKind)
                {
                    announces[i].Rank = PlainCarreRanks[random.Next(PlainCarreRanks.Length)];
                }
                else
                {
                    // A sequence of n cards ends at the (n - 1)th type or higher.
                    var length = announces[i].Type - AnnounceType.SequenceOf3 + 3;
                    announces[i].Rank = random.Next(length - 1, 8);
                }
            }
        }

        /// <summary>
        /// The points of the combinations that score: the team with the best carre scores all its
        /// carres, the team with the best sequence all its sequences, and when the two teams tie
        /// for the best sequence no sequence scores. Every rank must be known.
        /// </summary>
        public static void GetPoints(DeclaredAnnounce[] announces, int count, out int southNorth, out int eastWest)
        {
            var bestCarre = -1;
            var bestSequence = -1;
            for (var i = 0; i < count; i++)
            {
                if (IsCarre(announces[i].Type))
                {
                    if (bestCarre < 0 || Compare(announces[i], announces[bestCarre]) > 0)
                    {
                        bestCarre = i;
                    }
                }
                else if (bestSequence < 0 || Compare(announces[i], announces[bestSequence]) > 0)
                {
                    bestSequence = i;
                }
            }

            var sequenceTeam = bestSequence >= 0 ? announces[bestSequence].Seat & 1 : -1;
            for (var i = 0; i < count && sequenceTeam >= 0; i++)
            {
                if (!IsCarre(announces[i].Type) && (announces[i].Seat & 1) != sequenceTeam
                                                && Compare(announces[i], announces[bestSequence]) == 0)
                {
                    sequenceTeam = -1;
                }
            }

            var carreTeam = bestCarre >= 0 ? announces[bestCarre].Seat & 1 : -1;
            southNorth = 0;
            eastWest = 0;
            for (var i = 0; i < count; i++)
            {
                var team = announces[i].Seat & 1;
                if (team != (IsCarre(announces[i].Type) ? carreTeam : sequenceTeam))
                {
                    continue;
                }

                if (team == 0)
                {
                    southNorth += Value(announces[i].Type);
                }
                else
                {
                    eastWest += Value(announces[i].Type);
                }
            }
        }

        public static int Value(AnnounceType type) =>
            type switch
                {
                    AnnounceType.SequenceOf3 => 20,
                    AnnounceType.SequenceOf4 => 50,
                    AnnounceType.FourNines => 150,
                    AnnounceType.FourJacks => 200,
                    AnnounceType.Belot => 20,
                    _ => 100,
                };

        public static bool IsCarre(AnnounceType type) =>
            type == AnnounceType.FourOfAKind || type == AnnounceType.FourNines || type == AnnounceType.FourJacks;

        // Announce.CompareTo: value, then type (a longer sequence), then the rank.
        private static int Compare(DeclaredAnnounce first, DeclaredAnnounce second)
        {
            var byValue = Value(first.Type).CompareTo(Value(second.Type));
            if (byValue != 0)
            {
                return byValue;
            }

            if (first.Type != second.Type)
            {
                return first.Type > second.Type ? 1 : -1;
            }

            return first.Type == AnnounceType.FourOfAKind
                ? TrumpOrders[first.Rank].CompareTo(TrumpOrders[second.Rank])
                : first.Rank.CompareTo(second.Rank);
        }

        private static int AddSequences(uint suitBits, int seat, DeclaredAnnounce[] buffer, int count)
        {
            var run = 0;
            for (var type = 0; type <= 8; type++)
            {
                if (type < 8 && ((suitBits >> type) & 1) != 0)
                {
                    run++;
                    continue;
                }

                if (run >= 3)
                {
                    var announceType = (AnnounceType)((int)AnnounceType.SequenceOf3 + run - 3);
                    buffer[count++] = new DeclaredAnnounce(seat, announceType, type - 1);
                }

                run = 0;
            }

            return count;
        }
    }
}
