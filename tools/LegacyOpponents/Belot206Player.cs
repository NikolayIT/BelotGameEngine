namespace BelotLegacy
{
    using System.Collections.Generic;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;
    using BelotArena;

    /// <summary>The managed transcription of the 2001 AI, under the current engine's rules.</summary>
    public sealed class Belot206Player : IPlayer, ILegacyDiagnostics
    {
        private readonly OriginalEnginePlayer player;

        public Belot206Player(int seed = 1)
        {
            var random = new BelotV2.DelphiRandom(unchecked((uint)seed));
            this.player = new OriginalEnginePlayer(
                "Belot 2.06 (C# port)",
                context =>
                {
                    var card = BelotV2.OriginalPlayAdapter.Play(context, random, out var fallback);
                    if (fallback)
                    {
                        this.CardFallbacks++;
                    }

                    return card;
                },
                random);
        }

        public string Name => this.player.Name;

        public long BidDecisions { get; private set; }

        public long CardDecisions { get; private set; }

        public long RejectedBids => this.player.RejectedBids;

        public long CardFallbacks { get; private set; }

        public long LegalSetDifferences => 0;

        public BidType GetBid(PlayerGetBidContext context)
        {
            this.BidDecisions++;
            return this.player.GetBid(context);
        }

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            this.CardDecisions++;
            return this.player.PlayCard(context);
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(RoundResult roundResult)
        {
        }

        public void EndOfGame(GameResult gameResult)
        {
        }
    }
}
