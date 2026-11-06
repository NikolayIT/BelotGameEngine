#if STORE_SHOTS
namespace Belot.UI
{
    using System.Globalization;

    using Belot.Engine.Game;
    using Belot.UI.Game;
    using Belot.UI.Localization;
    using Belot.UI.Pages;

    /// <summary>
    /// Plays the real app on an iPhone simulator, for the App Store screenshots and as the check
    /// that the app works ahead-of-time compiled (.github/workflows/ios.yml): whole games against
    /// every level through the table's own commands, then the store's scenes on the real pages.
    /// It asks for each screenshot by writing its name to a file in the app's Documents, which the
    /// script outside takes with "simctl io" and deletes. Only built with -p:StoreShots=true,
    /// which also targets the simulator, so it can never reach a store build.
    /// </summary>
    internal static class StoreShotsDriver
    {
        private const string DoneFile = "shots-done.txt";

        private const string ErrorFile = "shots-error.txt";

        private const string LogFile = "shots-log.txt";

        private const string RequestFile = "shot-request.txt";

        private static readonly string Folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

        private static readonly Random Random = new(20261106);

        private static string? Language => Environment.GetEnvironmentVariable("BELOT_SHOT_LANGUAGE");

        private static bool IsBulgarian => Language == LocalizationManager.Bulgarian;

        private static IEnumerable<string> Scenes => (Environment.GetEnvironmentVariable("BELOT_SHOT_SCENES") ?? string.Empty)
            .Split(',', StringSplitOptions.RemoveEmptyEntries);

        // Before anything reads the settings: the start page shows them when it is built.
        public static void Prepare()
        {
            Log($"Device culture {CultureInfo.CurrentUICulture.Name}; preferred {string.Join(' ', Foundation.NSLocale.PreferredLanguages)}");
            Log($"Language without a saved choice: {LocalizationManager.Instance.Language}");
            if (string.IsNullOrEmpty(Language))
            {
                return;
            }

            LocalizationManager.Instance.SetLanguage(Language);
            AppSettings.HapticsEnabled = false;
            AppSettings.Speed = GameSpeed.Fast;
            AppSettings.PlayerName = IsBulgarian ? "Ники" : "Niki";
            AppSettings.PartnerName = IsBulgarian ? "Иван" : "Ivan";
            AppSettings.WestName = IsBulgarian ? "Петър" : "Petar";
            AppSettings.EastName = IsBulgarian ? "Мария" : "Maria";
            AppSettings.PartnerLevel = "expert";
            AppSettings.WestLevel = "claude";
            AppSettings.EastLevel = "claude";
        }

        public static void Start() => _ = Task.Run(() => MainThread.InvokeOnMainThreadAsync(RunAsync));

        private static async Task RunAsync()
        {
            try
            {
                await Until(() => Shell.Current?.CurrentPage is StartPage, "the start page");
                await Task.Delay(1000);

                // Each language plays its own games: the statistics scene shows them.
                var games = int.Parse(Environment.GetEnvironmentVariable("BELOT_SHOT_GAMES") ?? "0", CultureInfo.InvariantCulture);
                if (games > 0)
                {
                    await PlayEveryLevelAsync(games);
                }

                var scenes = new HashSet<string>(Scenes);
                await ShootPagesAsync(scenes);
                await ShootTableAsync(scenes);
                await CheckRatingIsSavedAsync();
                Log("Finished");
            }
            catch (Exception exception)
            {
                Log(exception.ToString());
                await File.WriteAllTextAsync(Path.Combine(Folder, ErrorFile), exception.ToString());
            }
            finally
            {
                await File.WriteAllTextAsync(Path.Combine(Folder, DoneFile), DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture));
            }
        }

        // Whole games at no pace through a real table, against every computer level, playing by the
        // app's own hints: the AI's code paths, and statistics like a real player's.
        private static async Task PlayEveryLevelAsync(int gamesPerLineup)
        {
            var names = SeatNames.Create(AppSettings.PlayerName, AppSettings.PartnerName, AppSettings.WestName, AppSettings.EastName);
            var levels = new[] { "random", "dummy", "smart", "expert", "claude" };
            foreach (var id in levels)
            {
                for (var game = 0; game < gamesPerLineup; game++)
                {
                    var started = DateTime.UtcNow;
                    var lineup = new Lineup(AiLevels.ById(id), AiLevels.ById(id), AiLevels.ById(id));
                    var session = new GameSession(names, lineup.CreatePlayer, GamePace.Instant);
                    Exception? error = null;
                    session.GameError += exception => error = exception;
                    using var table = new GameViewModel(session, lineup, new QuietHost());
                    table.StartGame();
                    var decisions = 0;
                    while (true)
                    {
                        await Until(() => table.IsGameOverlayVisible || table.IsRoundOverlayVisible || table.IsMyTurn || error != null, "a decision");
                        if (error != null)
                        {
                            throw new InvalidOperationException($"The game against {id} failed.", error);
                        }

                        if (table.IsGameOverlayVisible)
                        {
                            break;
                        }

                        if (table.IsRoundOverlayVisible)
                        {
                            table.RoundOverlayContinueCommand.Execute(null);
                            continue;
                        }

                        decisions++;
                        await HintAsync(table);
                        Move(table);
                        await Task.Yield();
                    }

                    Log($"Played a game against {id}: {decisions} decisions, {(DateTime.UtcNow - started).TotalSeconds:F1} s; rating {PlayerRatingStore.CurrentElo}");
                }
            }
        }

        private static async Task ShootPagesAsync(HashSet<string> scenes)
        {
            if (scenes.Remove("06-setup"))
            {
                await ReopenStartPageAsync();
                await ShootAsync("06-setup");
            }

            if (scenes.Remove("07-statistics"))
            {
                await Shell.Current.GoToAsync("StatisticsPage");
                await Until(() => Shell.Current.CurrentPage is StatisticsPage, "the statistics page");
                await ShootAsync("07-statistics");
                await Shell.Current.GoToAsync("..");
            }

            if (scenes.Remove("08-rules"))
            {
                await Shell.Current.GoToAsync("RulesPage");
                await Until(() => Shell.Current.CurrentPage is RulesPage, "the rules page");
                await ShootAsync("08-rules");
                await Shell.Current.GoToAsync("..");
            }

            if (scenes.Remove("09-settings"))
            {
                await Shell.Current.GoToAsync("SettingsPage");
                await Until(() => Shell.Current.CurrentPage is SettingsPage, "the settings page");
                await ShootAsync("09-settings");
                await Shell.Current.GoToAsync("..");
            }
        }

        // The start page refreshes its rating when it appears.
        private static async Task ReopenStartPageAsync()
        {
            await Shell.Current.GoToAsync("RulesPage");
            await Until(() => Shell.Current.CurrentPage is RulesPage, "the rules page");
            await Shell.Current.GoToAsync("..");
            await Until(() => Shell.Current.CurrentPage is StartPage, "the start page");
        }

        private static async Task ShootTableAsync(HashSet<string> scenes)
        {
            var wanted = scenes.Where(x => x is "01-gameplay" or "02-bidding" or "03-hint" or "04-declarations" or "05-result").ToHashSet();
            if (wanted.Count == 0)
            {
                return;
            }

            var query = $"?name={Uri.EscapeDataString(AppSettings.PlayerName)}&partner={AppSettings.PartnerLevel}&west={AppSettings.WestLevel}&east={AppSettings.EastLevel}";
            await Shell.Current.GoToAsync($"GamePage{query}");
            await Until(() => Shell.Current.CurrentPage is GamePage { BindingContext: GameViewModel }, "the table");
            var page = (GamePage)Shell.Current.CurrentPage;
            var table = (GameViewModel)page.BindingContext;

            CheckEdgeSwipeIsRefused();

            var deadline = DateTime.UtcNow.AddMinutes(25);
            while (wanted.Count > 0)
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("Scenes never seen: " + string.Join(", ", wanted));
                }

                await Until(() => table.IsGameOverlayVisible || table.IsRoundOverlayVisible || table.IsMyTurn, "a decision or a result");
                if (table.IsGameOverlayVisible)
                {
                    table.PlayAgainCommand.Execute(null);
                    await Task.Delay(300);
                    continue;
                }

                if (table.IsRoundOverlayVisible)
                {
                    // A deal that was played, and won: not everybody passing.
                    if (wanted.Contains("05-result") && table.RoundResult is { IsPlayed: true } result && result.UsInDeal > result.ThemInDeal)
                    {
                        wanted.Remove("05-result");
                        await ShootAsync("05-result");
                    }

                    table.RoundOverlayContinueCommand.Execute(null);
                    await Task.Delay(100);
                    continue;
                }

                if (table.IsBidPanelVisible)
                {
                    if (wanted.Contains("02-bidding") && Biddable(table).Count >= 3)
                    {
                        wanted.Remove("02-bidding");
                        await ShootAsync("02-bidding");
                    }
                }
                else if (table.IsAnnouncePanelVisible)
                {
                    if (wanted.Contains("04-declarations") && table.AnnounceOptions.Count == 1)
                    {
                        wanted.Remove("04-declarations");
                        await ShootAsync("04-declarations");
                    }
                }
                else
                {
                    var playable = table.MyHand.Count(x => x.IsPlayable);
                    var onTable = new[] { table.North, table.West, table.East }.Count(x => x.HasPlayedCard);
                    if (wanted.Contains("01-gameplay") && playable > 0 && playable < table.MyHand.Count && table.MyHand.Count >= 6 && onTable >= 2)
                    {
                        wanted.Remove("01-gameplay");
                        await ShootAsync("01-gameplay");
                    }
                    else if (wanted.Contains("03-hint") && playable >= 3 && table.MyHand.Count >= 6)
                    {
                        wanted.Remove("03-hint");
                        await HintAsync(table);
                        await ShootAsync("03-hint");
                    }
                }

                await HintAsync(table);
                Move(table);
                await Task.Delay(50);
            }

            await LeaveTableAsync(table);
        }

        // What iOS's swipe from the edge of the screen asks of the Shell (ShellSectionRenderer.ShouldPop).
        private static void CheckEdgeSwipeIsRefused()
        {
            var shell = Shell.Current;
            var section = shell.CurrentItem?.CurrentItem;
            var stack = section?.Stack.ToList() ?? new List<Page>();
            stack.RemoveAt(stack.Count - 1);
            var allowed = ((IShellController)shell).ProposeNavigation(ShellNavigationSource.Pop, shell.CurrentItem, section, section?.CurrentItem, stack, true);
            Log($"Swipe from the edge on the table: {(allowed ? "ALLOWED" : "refused")}");
            if (allowed)
            {
                throw new InvalidOperationException("A swipe from the edge would leave the game without asking.");
            }
        }

        private static async Task LeaveTableAsync(GameViewModel table)
        {
            table.LeaveCommand.Execute(null);
            await Until(() => Shell.Current.CurrentPage is StartPage, "the start page after leaving the table");
        }

        private static async Task CheckRatingIsSavedAsync()
        {
            await Task.Yield();
            Log($"Saved: rating {PlayerRatingStore.CurrentElo}, games {PlayerRatingStore.GamesPlayed}, wins {PlayerRatingStore.Wins}");
        }

        private static List<BidOption> Biddable(GameViewModel table) =>
            table.BidOptions.Where(x => x.IsEnabled && x.IsVisible && x.Bid != BidType.Pass).ToList();

        // A hint may have nothing to mark (declaring nothing, say), so it is not waited for long.
        private static async Task HintAsync(GameViewModel table)
        {
            table.HintCommand.Execute(null);
            var deadline = DateTime.UtcNow.AddSeconds(3);
            while (DateTime.UtcNow < deadline
                && !table.MyHand.Any(x => x.IsHinted) && !table.BidOptions.Any(x => x.IsHinted) && !table.AnnounceOptions.Any(x => x.IsHinted))
            {
                await Task.Delay(10);
            }
        }

        // What the hint says, as a person following it would; without one, a pass or a legal card.
        private static void Move(GameViewModel table)
        {
            if (table.IsBidPanelVisible)
            {
                table.BidCommand.Execute(table.BidOptions.FirstOrDefault(x => x.IsHinted && x.IsEnabled)
                    ?? table.BidOptions.First(x => x.Bid == BidType.Pass));
            }
            else if (table.IsAnnouncePanelVisible)
            {
                foreach (var option in table.AnnounceOptions.Where(x => x.IsHinted && !x.IsSelected).ToList())
                {
                    table.ToggleAnnounceCommand.Execute(option);
                }

                table.DeclareCommand.Execute(null);
            }
            else
            {
                var playable = table.MyHand.Where(x => x.IsPlayable).ToList();
                table.TapCardCommand.Execute(playable.FirstOrDefault(x => x.IsHinted) ?? playable[Random.Next(playable.Count)]);
            }
        }

        private static async Task ShootAsync(string name)
        {
            // The animations settle first.
            await Task.Delay(1500);
            var request = Path.Combine(Folder, RequestFile);
            var temporary = request + ".tmp";
            await File.WriteAllTextAsync(temporary, name);
            File.Move(temporary, request);
            await Until(() => !File.Exists(request), "the screenshot " + name, 300);
            Log("Shot " + name);
        }

        private static async Task Until(Func<bool> condition, string what, int seconds = 120)
        {
            var deadline = DateTime.UtcNow.AddSeconds(seconds);
            while (!condition())
            {
                if (DateTime.UtcNow > deadline)
                {
                    throw new TimeoutException("Never got to " + what);
                }

                await Task.Delay(10);
            }
        }

        private static void Log(string line) =>
            File.AppendAllText(Path.Combine(Folder, LogFile), DateTime.UtcNow.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture) + " " + line + Environment.NewLine);

        private sealed class QuietHost : IGameTableHost
        {
            public void After(TimeSpan delay, Action action) => _ = MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await Task.Delay(delay);
                action();
            });

            public void Vibrate(bool isLong)
            {
            }

            public void Leave()
            {
            }
        }
    }
}
#endif
