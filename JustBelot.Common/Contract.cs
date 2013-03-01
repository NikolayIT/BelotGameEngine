namespace JustBelot.Common
{
    using System.Collections.Generic;
    using System.Text;

    public struct Contract
    {
        public Contract(PlayerPosition playerPosition, ContractType type, bool isDoubled = false, bool isReDoubled = false)
            : this()
        {
            this.IsAvailable = true;
            this.Type = type;
            this.PlayerPosition = playerPosition;
            this.OriginalBidder = playerPosition;
            this.IsDoubled = isDoubled;
            this.IsReDoubled = isReDoubled;
        }

        public Contract(PlayerPosition playerPosition, ContractType type, PlayerPosition originalBidder, bool isDoubled, bool isReDoubled)
            : this()
        {
            this.IsAvailable = true;
            this.Type = type;
            this.PlayerPosition = playerPosition;
            this.OriginalBidder = originalBidder;
            this.IsDoubled = isDoubled;
            this.IsReDoubled = isReDoubled;
        }

        public bool IsAvailable { get; private set; }

        public ContractType Type { get; private set; }

        public PlayerPosition OriginalBidder { get; private set; }

        public PlayerPosition PlayerPosition { get; private set; }

        public bool IsDoubled { get; private set; }

        public bool IsReDoubled { get; private set; }

        public override string ToString()
        {
            if (!this.IsAvailable)
            {
                return "No contract";
            }

            var sb = new StringBuilder();

            switch (this.Type)
            {
                case ContractType.Clubs:
                    sb.Append("Clubs");
                    break;
                case ContractType.Diamonds:
                    sb.Append("Diamonds");
                    break;
                case ContractType.Hearts:
                    sb.Append("Hearts");
                    break;
                case ContractType.Spades:
                    sb.Append("Spades");
                    break;
                case ContractType.NoTrumps:
                    sb.Append("No trumps");
                    break;
                case ContractType.AllTrumps:
                    sb.Append("All trumps");
                    break;
            }

            if (this.IsDoubled)
            {
                sb.Append(" (doubled)");
            }
            else if (this.IsReDoubled)
            {
                sb.Append(" (re-doubled)");
            }

            return sb.ToString();
        }

        internal IList<BidType> GetAvailableBidsAfterThisContract(PlayerPosition teamMatePosition)
        {
            IList<BidType> availableBids = new List<BidType> { BidType.Pass };
            if (!this.IsAvailable)
            {
                availableBids = new List<BidType> { BidType.Pass, BidType.Clubs, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps };
            }
            else
            {
                switch (this.Type)
                {
                    case ContractType.Clubs:
                        availableBids = new List<BidType> { BidType.Pass, BidType.Diamonds, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps };
                        break;
                    case ContractType.Diamonds:
                        availableBids = new List<BidType> { BidType.Pass, BidType.Hearts, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps };
                        break;
                    case ContractType.Hearts:
                        availableBids = new List<BidType> { BidType.Pass, BidType.Spades, BidType.NoTrumps, BidType.AllTrumps };
                        break;
                    case ContractType.Spades:
                        availableBids = new List<BidType> { BidType.Pass, BidType.NoTrumps, BidType.AllTrumps };
                        break;
                    case ContractType.NoTrumps:
                        availableBids = new List<BidType> { BidType.Pass, BidType.AllTrumps };
                        break;
                    case ContractType.AllTrumps:
                        availableBids = new List<BidType> { BidType.Pass };
                        break;
                }

                if (teamMatePosition != this.PlayerPosition)
                {
                    // The contract is not from the team mate
                    if (this.IsDoubled)
                    {
                        availableBids.Add(BidType.ReDouble);
                    }
                    else if (!this.IsReDoubled)
                    {
                        availableBids.Add(BidType.Double);
                    }
                }
            }

            return availableBids;
        }
    }
}
