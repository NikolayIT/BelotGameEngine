using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JustBelot.Common.Extensions
{
    public static class ContractTypeExtensions
    {
        public static CardSuit ToCardSuit(this ContractType contractType)
        {
            switch (contractType)
            {
                case ContractType.Clubs:
                    return CardSuit.Clubs;
                case ContractType.Diamonds:
                    return CardSuit.Diamonds;
                case ContractType.Hearts:
                    return CardSuit.Hearts;
                case ContractType.Spades:
                    return CardSuit.Spades;
                default:
                    throw new ArgumentOutOfRangeException("contractType");
            }
        }

        public static bool IsTrump(this ContractType contractType)
        {
            switch (contractType)
            {
                case ContractType.Clubs:
                    return true;
                case ContractType.Diamonds:
                    return true;
                case ContractType.Hearts:
                    return true;
                case ContractType.Spades:
                    return true;
                case ContractType.NoTrumps:
                    return false;
                case ContractType.AllTrumps:
                    return false;
                default:
                    throw new ArgumentOutOfRangeException("contractType");
            }
        }
    }
}
