namespace Belot.UI.Console
{
    using System;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    using Belot.Engine.Cards;

    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.Unicode;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Console.BufferHeight = Console.WindowHeight = 17;
            Console.BufferWidth = Console.WindowWidth = 50;
            Console.WriteLine("Belot Console 1.0");

            RandomCards();
        }

        private static void RandomCards()
        {
            for (var i = 0; i < 100; i++)
            {
                var deck = new Deck();
                deck.Shuffle();
                for (var j = 0; j < 32; j++)
                {
                    Console.Write(deck.GetNextCard() + " ");
                }

                Console.WriteLine();
            }
        }
    }
}
