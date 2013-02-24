namespace JustBelot.Common
{
    public interface IPlayer
    {
        string Name { get; }

        GameManager Game { set; }

        void StartNewGame(PlayerPosition position);

        void StartNewDeal();

        void AddCard(Card card);

        ContractType AskForContract();

        Card PlayCard();
    }
}
