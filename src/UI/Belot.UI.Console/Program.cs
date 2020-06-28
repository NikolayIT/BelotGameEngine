namespace Belot.UI.Console
{
    using System;
    using System.Text;

    public static class Program
    {
        public static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.BufferHeight = Console.WindowHeight = 17;
            Console.BufferWidth = Console.WindowWidth = 50;
            Console.WriteLine("Belot Console 1.0");
        }
    }
}
