namespace JustBelot.Common
{
    public struct Contract
    {
        public Contract(ContractType type, bool isDoubled = false, bool isReDoubled = false)
            : this()
        {
            this.Type = type;
            this.IsDoubled = isDoubled;
            this.IsReDoubled = isReDoubled;
        }

        public ContractType Type { get; private set; }

        public bool IsDoubled { get; private set; }

        public bool IsReDoubled { get; private set; }
    }
}
