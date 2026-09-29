namespace Belot.Engine.GameMechanics
{
    using System.Collections.Generic;

    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.Players;

    /// <summary>
    /// One deal as a step-by-step state machine: five cards each, the bidding
    /// (<see cref="Auction"/>), three more cards, the eight tricks (<see cref="TrickPlay"/>) and
    /// the score. It never asks anybody for anything: whoever drives it reads
    /// <see cref="ToMove"/> and <see cref="Decision"/> and answers with <see cref="ApplyBid"/>,
    /// <see cref="ApplyAnnounces"/> or <see cref="TryPlayCard"/>. The observers get EndOfTrick
    /// and EndOfRound (a passed-out deal too).
    /// </summary>
    internal sealed class Round
    {
        private static readonly ScoreManager ScoreManager = new ScoreManager();

        private readonly IPlayer[] observers;

        private readonly Deck deck;

        private readonly IReadOnlyList<CardCollection> playerCards;

        private readonly int roundNumber;

        private readonly PlayerPosition firstToPlay;

        private readonly int southNorthPoints;

        private readonly int eastWestPoints;

        private readonly int hangingPoints;

        private readonly PlayerPosition manualCardPlaySeats;

        /// <summary>
        /// Initializes a new instance of the <see cref="Round"/> class and deals: the deck is
        /// shuffled, the hands (which the round plays from) emptied, and five cards dealt to each
        /// seat in turn, South first.
        /// </summary>
        public Round(
            IPlayer[] observers,
            Deck deck,
            IReadOnlyList<CardCollection> playerCards,
            int roundNumber,
            PlayerPosition firstToPlay,
            int southNorthPoints,
            int eastWestPoints,
            int hangingPoints,
            bool recordDeal = false,
            PlayerPosition manualCardPlaySeats = PlayerPosition.Unknown)
        {
            this.observers = observers;
            this.deck = deck;
            this.playerCards = playerCards;
            this.roundNumber = roundNumber;
            this.firstToPlay = firstToPlay;
            this.southNorthPoints = southNorthPoints;
            this.eastWestPoints = eastWestPoints;
            this.hangingPoints = hangingPoints;
            this.manualCardPlaySeats = manualCardPlaySeats;

            deck.Shuffle();
            if (recordDeal)
            {
                this.DealOrder = deck.CopyOrder();
            }

            for (var seat = 0; seat < 4; seat++)
            {
                playerCards[seat].Clear();
            }

            this.Deal(5);
            this.Auction = new Auction(roundNumber, firstToPlay, southNorthPoints, eastWestPoints, playerCards, hangingPoints);
            this.AfterBid();
        }

        public Auction Auction { get; }

        /// <summary>Gets the tricks; null until the bidding has ended in a contract.</summary>
        public TrickPlay Tricks { get; private set; }

        public bool IsFinished => this.Result != null;

        /// <summary>Gets the result once the deal is over (passed out or played), else null.</summary>
        public RoundResult Result { get; private set; }

        public int RoundNumber => this.roundNumber;

        public PlayerPosition FirstToPlay => this.firstToPlay;

        public int HangingPointsBefore => this.hangingPoints;

        public int SouthNorthPointsBefore => this.southNorthPoints;

        public int EastWestPointsBefore => this.eastWestPoints;

        /// <summary>Gets the 32 cards as they came off the shuffled deck; null unless recorded.</summary>
        public Card[] DealOrder { get; }

        /// <summary>Gets the hands (live).</summary>
        public IReadOnlyList<CardCollection> Hands => this.playerCards;

        public PlayerPosition ToMove =>
            this.IsFinished ? PlayerPosition.Unknown :
            this.Tricks == null ? this.Auction.ToMove : this.Tricks.ToMove;

        public BelotDecision Decision =>
            this.IsFinished ? BelotDecision.None :
            this.Tricks == null ? BelotDecision.Bid : this.Tricks.Decision;

        /// <summary>Makes the bid of the seat to move (check it with Auction.Check first).</summary>
        public void ApplyBid(BidType bid)
        {
            this.Auction.Apply(bid);
            this.AfterBid();
        }

        public void ApplyAnnounces(IEnumerable<Announce> announces)
        {
            this.Tricks.ApplyAnnounces(announces);
            this.AfterPlay();
        }

        public bool TryPlayCard(PlayCardAction action)
        {
            if (!this.Tricks.TryPlayCard(action))
            {
                return false;
            }

            this.AfterPlay();
            return true;
        }

        public void AfterBid()
        {
            if (!this.Auction.IsFinished)
            {
                return;
            }

            var contract = this.Auction.Contract;
            if (contract.Type == BidType.Pass)
            {
                // All pass. Hanging points stay on the table for the winner of the next played deal.
                this.Finish(new RoundResult(contract) { HangingPoints = this.hangingPoints });
                return;
            }

            this.Deal(3);
            this.Tricks = new TrickPlay(
                this.observers,
                this.roundNumber,
                this.firstToPlay,
                this.southNorthPoints,
                this.eastWestPoints,
                this.playerCards,
                this.Auction.Bids,
                contract,
                this.hangingPoints,
                this.manualCardPlaySeats);
            this.AfterPlay();
        }

        public void AfterPlay()
        {
            if (!this.Tricks.IsFinished)
            {
                return;
            }

            this.Finish(ScoreManager.GetScore(
                this.Auction.Contract,
                this.Tricks.SouthNorthTricks,
                this.Tricks.EastWestTricks,
                this.Tricks.Announces,
                this.hangingPoints,
                this.Tricks.LastTrickWinner));
        }

        private void Finish(RoundResult result)
        {
            this.Result = result;
            for (var i = 0; i < 4; i++)
            {
                this.observers[i]?.EndOfRound(result);
            }
        }

        private void Deal(int cardsEach)
        {
            for (var i = 0; i < cardsEach; i++)
            {
                this.playerCards[0].Add(this.deck.GetNextCard());
                this.playerCards[1].Add(this.deck.GetNextCard());
                this.playerCards[2].Add(this.deck.GetNextCard());
                this.playerCards[3].Add(this.deck.GetNextCard());
            }
        }
    }
}
