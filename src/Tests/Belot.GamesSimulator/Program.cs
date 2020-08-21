namespace Belot.GamesSimulator
{
    using System;
    using System.Diagnostics;
    using System.Globalization;
    using System.Text;
    using System.Threading;

    public static class Program
    {
        public const int LineLength = 70;

        public static void Main()
        {
            var parallelism = Environment.ProcessorCount / 2;
            Process.GetCurrentProcess().PriorityClass = ProcessPriorityClass.RealTime;
            Console.OutputEncoding = Encoding.Unicode;
            Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
            Console.WriteLine(new string('=', LineLength));
            Console.WriteLine("Belot Games Simulator");
            Console.Write(DateTime.Now.ToShortDateString());
#if DEBUG
            Console.Write(", Mode=Debug");
#elif RELEASE
            Console.Write(", Mode=Release");
#endif
            Console.Write(
                $", CPUs={Environment.ProcessorCount}({parallelism}), OS={Environment.OSVersion}, .NET={Environment.Version}");
            Console.WriteLine();
            Console.WriteLine(new string('=', LineLength));

            new GamesSimulatorService().Run(parallelism);
            //// new GamesSimulatorService().RunDetailedGames(2);
        }
    }
}
