namespace Belot.GamesSimulator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Reflection;

    using Belot.Engine;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;

    public class SmartPlayerPreviousVersion : IPlayer
    {
        private static readonly string[] UrlsForSourceCode =
            {
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/SmartPlayer.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/GlobalCounters.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/AllTrumpsPlayingFirstPlayStrategy.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/AllTrumpsPlayingLastPlayStrategy.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/AllTrumpsPlayStrategy.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/IPlayStrategy.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/NoTrumpsPlayingFirstPlayStrategy.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/NoTrumpsPlayingLastPlayStrategy.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/NoTrumpsPlayStrategy.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/TrumpPlayingFirstPlayStrategy.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/TrumpPlayingLastPlayStrategy.cs",
                "https://raw.githubusercontent.com/NikolayIT/BelotGameEngine/master/src/AI/Belot.AI.SmartPlayer/Strategies/TrumpPlayStrategy.cs",
            };

        private static readonly Type CompiledPlayerType;

        private readonly IPlayer compiledPlayer;

        static SmartPlayerPreviousVersion()
        {
            var client = new HttpClient(new HttpClientHandler { Proxy = null, UseProxy = false });
            var codeFiles = UrlsForSourceCode.Select(
                url => client.GetStringAsync(url + "?nocache=" + DateTime.Now.Ticks).GetAwaiter().GetResult()).ToList();

            var syntaxTrees = new List<SyntaxTree>();
            foreach (var codeFile in codeFiles)
            {
                syntaxTrees.Add(CSharpSyntaxTree.ParseText(codeFile));
            }

            var referencedAssemblies = CollectAssemblies(Assembly.Load(new AssemblyName("netstandard")));
            var metadataReferences = new List<MetadataReference>(referencedAssemblies.Count + 1);
            foreach (var referencedAssembly in referencedAssemblies)
            {
                metadataReferences.Add(MetadataReference.CreateFromFile(referencedAssembly.Location));
            }

            metadataReferences.Add(MetadataReference.CreateFromFile(typeof(object).Assembly.Location));
            metadataReferences.Add(MetadataReference.CreateFromFile(Assembly.Load(new AssemblyName("mscorlib")).Location));
            metadataReferences.Add(MetadataReference.CreateFromFile(typeof(IPlayer).Assembly.Location));

            var compilation = CSharpCompilation.Create("PreviousVersionSmartPlayer")
                .WithOptions(new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary))
                .AddSyntaxTrees(syntaxTrees).AddReferences(metadataReferences);

            Assembly assembly = null;
            using (var ms = new MemoryStream())
            {
                var result = compilation.Emit(ms);
                if (!result.Success)
                {
                    var failures = result.Diagnostics.Where(
                        diagnostic => diagnostic.IsWarningAsError || diagnostic.Severity == DiagnosticSeverity.Error);

                    foreach (var diagnostic in failures)
                    {
                        Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                    }
                }
                else
                {
                    ms.Seek(0, SeekOrigin.Begin);
                    assembly = Assembly.Load(ms.ToArray());
                }
            }

            if (assembly == null)
            {
                throw new Exception("Remote assembly code cannot be compiled.");
            }

            CompiledPlayerType = assembly.GetType("Belot.AI.SmartPlayer.SmartPlayer");
        }

        public SmartPlayerPreviousVersion()
        {
            this.compiledPlayer = (IPlayer)Activator.CreateInstance(CompiledPlayerType);
        }

        public BidType GetBid(PlayerGetBidContext context) => this.compiledPlayer.GetBid(context);

        public IEnumerable<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => this.compiledPlayer.GetAnnounces(context);

        public PlayCardAction PlayCard(PlayerPlayCardContext context) => this.compiledPlayer.PlayCard(context);

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions) => this.compiledPlayer.EndOfTrick(trickActions);

        public void EndOfRound(RoundResult roundResult) => this.compiledPlayer.EndOfRound(roundResult);

        public void EndOfGame(GameResult gameResult) => this.compiledPlayer.EndOfGame(gameResult);

        private static IList<Assembly> CollectAssemblies(Assembly assembly)
        {
            var assemblies = new HashSet<Assembly> { assembly };

            var referencedAssemblyNames = assembly.GetReferencedAssemblies();

            foreach (var assemblyName in referencedAssemblyNames)
            {
                var loadedAssembly = Assembly.Load(assemblyName);
                assemblies.Add(loadedAssembly);
            }

            return assemblies.ToList();
        }
    }
}
