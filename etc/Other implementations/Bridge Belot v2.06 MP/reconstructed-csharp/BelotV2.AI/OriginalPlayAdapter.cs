namespace BelotV2
{
    /// <summary>
    /// Bridges the engine's <see cref="PlayContext"/> onto <see cref="OriginalPlayAi"/>, which
    /// works in the binary's own encodings (suit 0=C 1=D 2=S 3=H, ranks 7..14, players 1..4).
    ///
    /// The card the original would play is reproduced exactly for the analysis and lookahead
    /// stages, and for most of the decision tree; see the README for the measured agreement.
    /// </summary>
    public static class OriginalPlayAdapter
    {
        // engine Suit -> the binary's suit index. Note spades and hearts are the other way round
        // from the bid ladder (where hearts outrank spades); the exe keeps the two orders in
        // separate tables and so does this port. Every index into the AI's own arrays — the
        // possible-card matrix, the per-suit counters, the memory block — uses THIS order.
        private static int Orig(Suit s) => s switch
        {
            Suit.Clubs => 0,
            Suit.Diamonds => 1,
            Suit.Spades => 2,
            _ => 3,   // Hearts
        };

        private static int ContractId(BidType t) => t switch
        {
            BidType.Clubs => 1,
            BidType.Diamonds => 2,
            BidType.Hearts => 3,
            BidType.Spades => 4,
            BidType.NoTrumps => 5,
            _ => 6,
        };

        /// <summary>
        /// The AI's memory as of this position. It carries across the tricks of a round — which
        /// suits have been led, who opened what, who has been discarding what — and nothing in
        /// the decision routine fills it in, so the round is replayed through the rules that
        /// maintain it (see <see cref="PlayMemory"/>).
        /// </summary>
        public static byte[] RoundMemory(PlayContext ctx)
        {
            var plays = new List<(int, int, int)>();
            foreach (Play p in ctx.PlayedHistory)
            {
                plays.Add((p.Seat + 1, Orig(p.Card.Suit), (int)p.Card.Rank));
            }

            foreach (Play p in ctx.CurrentTrick)
            {
                plays.Add((p.Seat + 1, Orig(p.Card.Suit), (int)p.Card.Rank));
            }

            int[]? bids = null;
            if (ctx.BidSuits != null)
            {
                bids = new int[5];
                for (int p = 1; p <= 4; p++)
                {
                    Suit? s = ctx.BidSuits[p - 1];
                    bids[p] = s.HasValue ? Orig(s.Value) : OriginalPlayAi.NoSuit;
                }
            }

            return PlayMemory.Replay(
                ContractId(ctx.Contract.Type), plays, bids,
                ctx.HumanSeat >= 0 ? ctx.HumanSeat + 1 : 0,
                level: ctx.Contract.Redoubled ? 4 : ctx.Contract.Doubled ? 2 : 1);
        }

        public static Card Play(PlayContext ctx, DelphiRandom rng)
        {
            if (ctx.Legal.Count == 1)
            {
                return ctx.Legal[0];
            }

            var hand = new List<Card>(ctx.Hand);
            Game.SortHand(hand);

            var ai = new OriginalPlayAi
            {
                Contract = ContractId(ctx.Contract.Type),
                Me = ctx.Seat + 1,
                Declarer = ctx.Contract.Declarer + 1,
                Rng = rng,
            };
            ai.Trump = ai.Contract <= 4 ? Orig(ctx.Contract.TrumpSuit!.Value) : OriginalPlayAi.NoSuit;

            for (int i = 0; i < hand.Count; i++)
            {
                ai.SlotSuit[i] = Orig(hand[i].Suit);
                ai.SlotRank[i] = (int)hand[i].Rank;
                ai.SlotPresent[i] = true;
                ai.SlotLegal[i] = ctx.Legal.Contains(hand[i]);
            }

            for (int i = 0; i < ctx.CurrentTrick.Count && i < 4; i++)
            {
                Play p = ctx.CurrentTrick[i];
                ai.TableSuit[i] = Orig(p.Card.Suit);
                ai.TableRank[i] = (int)p.Card.Rank;
                ai.TableOwner[i] = p.Seat + 1;
            }

            ai.LedSuit = ctx.CurrentTrick.Count > 0
                ? Orig(ctx.CurrentTrick[0].Card.Suit)
                : OriginalPlayAi.NoSuit;

            // "who may still hold what": everything unseen, minus the suits a seat has shown void in
            var seen = new HashSet<Card>(hand);
            foreach (Play p in ctx.PlayedHistory)
            {
                seen.Add(p.Card);
            }

            foreach (Play p in ctx.CurrentTrick)
            {
                seen.Add(p.Card);
            }

            var voids = new HashSet<Suit>[5];
            for (int p = 1; p <= 4; p++)
            {
                voids[p] = new HashSet<Suit>();
            }

            void ScanTrick(IReadOnlyList<Play> plays, int from, int count)
            {
                Suit led = plays[from].Card.Suit;
                for (int k = 0; k < count; k++)
                {
                    Play p = plays[from + k];
                    if (p.Card.Suit != led)
                    {
                        voids[p.Seat + 1].Add(led);
                    }
                }
            }

            for (int i = 0; i + 3 < ctx.PlayedHistory.Count; i += 4)
            {
                ScanTrick(ctx.PlayedHistory, i, 4);
            }

            if (ctx.CurrentTrick.Count > 0)
            {
                ScanTrick(ctx.CurrentTrick, 0, ctx.CurrentTrick.Count);
            }

            foreach (Suit s in Enum.GetValues<Suit>())
            {
                foreach (Rank r in Enum.GetValues<Rank>())
                {
                    bool unseen = !seen.Contains(new Card(s, r));
                    for (int p = 1; p <= 4; p++)
                    {
                        ai.Possible[p, Orig(s), (int)r - 7] = unseen && !voids[p].Contains(s);
                    }
                }
            }

            foreach (Card c in hand)
            {
                ai.Possible[ai.Me, Orig(c.Suit), (int)c.Rank - 7] = true;
            }

            byte[] mem = RoundMemory(ctx);
            Array.Copy(mem, ai.Mem, mem.Length);

            int slot = ai.Decide();
            if (slot < 0 || slot >= hand.Count)
            {
                return ctx.Legal[0];
            }

            Card chosen = hand[slot];
            return ctx.Legal.Contains(chosen) ? chosen : ctx.Legal[0];
        }
    }
}
