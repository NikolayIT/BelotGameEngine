namespace Belot.UI.Console
{
    using System;
    using System.IO;
    using System.Runtime.Serialization.Formatters.Binary;
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

            for (var i = 0; i < 30; i++)
            {
                var deck = new Deck();
                for (var j = 0; j < 32; j++)
                {
                    Console.Write(deck.GetNextCard() + " ");
                }

                Console.WriteLine();
            }
        }

        private static long GetSize(this object obj)
        {
            using Stream stream = new MemoryStream();
            new BinaryFormatter().Serialize(stream, obj);
            return stream.Length;
        }
    }
}
