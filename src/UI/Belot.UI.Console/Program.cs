namespace Belot.UI.Console
{
    using System;
    using System.Text;

    using Belot.Engine.Cards;

    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            //// Console.BufferHeight = Console.WindowHeight = 17;
            //// Console.BufferWidth = Console.WindowWidth = 50;
            Console.WriteLine("Belot Console 1.0");

            foreach (var card in new CardCollection(CardCollection.AllBelotCardsBitMask))
            {
                Console.Write(card + " ");
            }

            Console.WriteLine();
            Console.WriteLine(new string('-', 60));

            for (int i = 0; i < 30; i++)
            {
                var deck = new Deck();
                for (int j = 0; j < 32; j++)
                {
                    Console.Write(deck.GetNextCard() + " ");
                }

                Console.WriteLine();
            }
        }
    }
}
