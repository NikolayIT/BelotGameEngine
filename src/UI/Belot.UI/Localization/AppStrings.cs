namespace Belot.UI.Localization
{
    using System.Collections.Generic;

    /// <summary>
    /// The app's texts in English and Bulgarian, in code (no .resx: nothing to trim, the same on
    /// every platform). Every key must exist in both tables with the same placeholders (the UI tests
    /// check it). No MAUI types here: the UI tests compile this file.
    /// </summary>
    internal static class AppStrings
    {
        private static readonly Dictionary<string, string> En = new()
        {
            // Start page
            ["Start_Title"] = "BELOT",
            ["Start_Subtitle"] = "BRIDGE-BELOTE FOR FOUR",
            ["Start_Statistics"] = "Stats",
            ["Start_HowToPlay"] = "How to play",
            ["Start_YourName"] = "YOUR NAME",
            ["Start_DefaultName"] = "You",
            ["Start_Partner"] = "PARTNER · NORTH",
            ["Start_West"] = "LEFT RIVAL · WEST",
            ["Start_East"] = "RIGHT RIVAL · EAST",
            ["Start_Play"] = "Play",
            ["Start_RecentGames"] = "RECENT GAMES",
            ["Start_SeeAll"] = "See all",
            ["Start_NoHistory"] = "No games yet. Pick your partner and rivals and play!",
            ["Start_YourRating"] = "Your rating",
            ["Start_RecordFormat"] = "{0} games · {1} won · {2} lost",
            ["Start_NoGames"] = "Play a game to get rated",

            // Computer levels
            ["Level_Random_Name"] = "Random",
            ["Level_Random_Tag"] = "Plays any legal card, bids on a whim.",
            ["Level_Dummy_Name"] = "Beginner",
            ["Level_Dummy_Tag"] = "Simple rules: bids its strong suits, plays low.",
            ["Level_Smart_Name"] = "Skilled",
            ["Level_Smart_Tag"] = "Solid play, like a regular at the club.",
            ["Level_Claude_Name"] = "Master",
            ["Level_Claude_Tag"] = "Plays out thousands of deals before every card.",
            ["Level_RecordFormat"] = "Your record against this level: {0}W – {1}L",
            ["Diff_1"] = "Easy",
            ["Diff_2"] = "Medium",
            ["Diff_3"] = "Hard",
            ["Diff_4"] = "Expert",

            // Seats
            ["Seat_East"] = "East · {0}",
            ["Seat_West"] = "West · {0}",
            ["Seat_Partner"] = "Partner · {0}",

            // Cards
            ["Rank_Seven"] = "7",
            ["Rank_Eight"] = "8",
            ["Rank_Nine"] = "9",
            ["Rank_Ten"] = "10",
            ["Rank_Jack"] = "J",
            ["Rank_Queen"] = "Q",
            ["Rank_King"] = "K",
            ["Rank_Ace"] = "A",
            ["Rank_Seven_Plural"] = "sevens",
            ["Rank_Eight_Plural"] = "eights",
            ["Rank_Nine_Plural"] = "nines",
            ["Rank_Ten_Plural"] = "tens",
            ["Rank_Jack_Plural"] = "jacks",
            ["Rank_Queen_Plural"] = "queens",
            ["Rank_King_Plural"] = "kings",
            ["Rank_Ace_Plural"] = "aces",

            // Bids
            ["Bid_Pass"] = "Pass",
            ["Bid_Clubs"] = "Clubs",
            ["Bid_Diamonds"] = "Diamonds",
            ["Bid_Hearts"] = "Hearts",
            ["Bid_Spades"] = "Spades",
            ["Bid_NoTrumps"] = "No trumps",
            ["Bid_AllTrumps"] = "All trumps",
            ["Bid_Double"] = "Double",
            ["Bid_ReDouble"] = "Redouble",
            ["Bid_NoTrumpsShort"] = "NT",
            ["Bid_AllTrumpsShort"] = "AT",
            ["Bid_DoubleShort"] = "Double",
            ["Bid_ReDoubleShort"] = "Redouble",
            ["Bid_Current"] = "Now: {0} ({1})",
            ["Bid_NoneYet"] = "Nobody has bid yet",

            // Combinations
            ["Ann_Belote"] = "Belote",
            ["Ann_Tierce"] = "Tierce",
            ["Ann_Quarte"] = "Quarte",
            ["Ann_Quint"] = "Quint",
            ["Ann_Carre"] = "Four of a kind",
            ["Ann_CarreOf"] = "Four {0}",
            ["Ann_FourJacks"] = "Four jacks",
            ["Ann_FourNines"] = "Four nines",
            ["Ann_To"] = "{0} to {1}",

            // Game table
            ["Game_Us"] = "Us",
            ["Game_Them"] = "Them",
            ["Game_Hanging"] = "Hanging",
            ["Game_LastTrick"] = "Last trick",
            ["Game_Hint"] = "Hint",
            ["Game_Menu"] = "Menu",
            ["Game_YourBid"] = "Your bid",
            ["Game_YourCombinations"] = "Your combinations",
            ["Game_Declare"] = "Declare",
            ["Game_DeclareNothing"] = "Don't declare",
            ["Game_BeloteBadge"] = "Belote",
            ["Status_Thinking"] = "{0} is thinking…",
            ["Status_YourBid"] = "Your bid",
            ["Status_YourDeclaration"] = "Declare your combinations",
            ["Status_YourTurn"] = "Your turn",
            ["Toast_Belote"] = "Belote!",
            ["Toast_SeatBelote"] = "{0}: Belote!",

            // End of a deal
            ["Round_PassedOut"] = "Everybody passed",
            ["Round_NoContract"] = "No contract: the cards are dealt again",
            ["Round_Hanging"] = "Hanging",
            ["Round_WeMade"] = "Contract made!",
            ["Round_TheyMade"] = "They made it",
            ["Round_WeInside"] = "We're inside",
            ["Round_TheyInside"] = "They're inside!",
            ["Round_CapotBadge"] = "Capot!",
            ["Round_Cards"] = "Cards",
            ["Round_Combinations"] = "Combinations",
            ["Round_Capot"] = "Capot",
            ["Round_InDeal"] = "In the deal",
            ["Round_GamePoints"] = "Game points",
            ["Round_Score"] = "SCORE",
            ["Round_HangingFormat"] = "{0} points hang for the next played deal",

            // End of the game
            ["GameOver_Won"] = "We won!",
            ["GameOver_Lost"] = "We lost",
            ["GameOver_Score"] = "{0} – {1}",
            ["GameOver_PlayAgain"] = "Play again",
            ["GameOver_BackToMenu"] = "Back to menu",
            ["Rating_Change"] = "Rating {0} → {1} ({2})",
            ["Error_Title"] = "Something went wrong",
            ["Common_Continue"] = "Continue",
            ["Common_Cancel"] = "Cancel",
            ["Common_Series"] = "Series",

            // Leaving a game
            ["Leave_Title"] = "Leave the game?",
            ["Leave_Message"] = "The game in progress will be lost and not rated.",
            ["Leave_Confirm"] = "Leave",
            ["Leave_Cancel"] = "Keep playing",

            // History
            ["History_Win"] = "W",
            ["History_Loss"] = "L",
            ["History_With"] = "with {0} vs {1}",
            ["History_Pair"] = "{0} & {1}",

            // Settings
            ["Settings_Title"] = "Settings",
            ["Settings_Gameplay"] = "GAMEPLAY",
            ["Settings_Language"] = "Language",
            ["Settings_GameSpeed"] = "Game speed",
            ["Settings_GameSpeedHint"] = "How long the computer thinks and the cards stay on the table.",
            ["Speed_Relaxed"] = "Relaxed",
            ["Speed_Normal"] = "Normal",
            ["Speed_Fast"] = "Fast",
            ["Settings_Haptics"] = "Vibration",
            ["Settings_HapticsHint"] = "A short buzz on your turn, a long one at the end of a deal.",
            ["Settings_Assists"] = "Beginner assists",
            ["Settings_AssistsHint"] = "Belote badges on your cards and the Hint button.",
            ["Settings_Data"] = "DATA",
            ["Settings_ResetStats"] = "Reset statistics",
            ["Reset_Title"] = "Reset statistics?",
            ["Reset_Message"] = "Your rating, history and records will be erased.",
            ["Reset_Confirm"] = "Reset",
            ["Reset_Done"] = "Statistics reset.",
            ["Settings_About"] = "ABOUT",
            ["Settings_Version"] = "Belot {0}",
            ["Settings_EngineNote"] = "The game and its computer players run on your device, with no connection.",
            ["Settings_OpenSource"] = "Open source on GitHub",

            // Statistics
            ["Stats_Title"] = "Statistics",
            ["Stats_Rating"] = "RATING",
            ["Stats_Current"] = "Current",
            ["Stats_Peak"] = "Best",
            ["Stats_Games"] = "Games",
            ["Stats_WinRate"] = "Win rate",
            ["Stats_CurrentStreak"] = "Streak",
            ["Stats_BestStreak"] = "Best streak",
            ["Stats_Empty"] = "No games yet.",
            ["Stats_ByLevel"] = "AGAINST EACH LEVEL",
            ["Stats_GamesFormat"] = "{0} games",
            ["Stats_NotPlayed"] = "Not played yet",
            ["Stats_HistoryHeader"] = "HISTORY",

            // Rules
            ["Rules_Title"] = "How to play",
            ["Rules_Intro"] = "Belot is played by four players in two teams; partners sit opposite each other. You sit South and your partner North.",
            ["Rules_Cards_Title"] = "Cards and dealing",
            ["Rules_Cards_Body"] = "32 cards: 7, 8, 9, 10, J, Q, K, A in four suits. Everybody gets five cards, the auction decides the game, then everybody gets three more.",
            ["Rules_Bidding_Title"] = "Bidding",
            ["Rules_Bidding_Body"] = "In turn, each player passes or names a higher game: clubs, diamonds, hearts, spades (the trump suit), no trumps, all trumps. An opponent of the declarer may double; the declaring side may then redouble. Three passes in a row end the auction. If all four pass, the cards are dealt again.",
            ["Rules_Play_Title"] = "Playing the tricks",
            ["Rules_Play_Body"] = "The first bidder leads; the winner of a trick leads the next. You must follow suit. Trumps rank J 9 A 10 K Q 8 7, other suits A 10 K Q J 9 8 7. When trumps are led (and in all trumps) you must play higher if you can. Without the suit led you must trump while an opponent holds the trick, and overtrump an opponent's trump if you can. In no trumps you only follow suit.",
            ["Rules_Combinations_Title"] = "Combinations",
            ["Rules_Combinations_Body"] = "Declared with your first card (not in no trumps): tierce (three in a row) 20, quarte 50, quint (five or more) 100, four tens, queens, kings or aces 100, four nines 150, four jacks 200. A card counts in one combination only. The team with the best sequence scores all its sequences (none if the best are equal); the best four of a kind scores for its team.",
            ["Rules_Belote_Title"] = "Belote",
            ["Rules_Belote_Body"] = "The king and queen of trumps (of any suit in all trumps) are worth 20, claimed as the first of them is played. The app claims it for you.",
            ["Rules_Scoring_Title"] = "Scoring",
            ["Rules_Scoring_Body"] = "Trumps: J 20, 9 14, A 11, 10 10, K 4, Q 3. Other suits: A 11, 10 10, K 4, Q 3, J 2. The last trick is worth 10 and taking all eight tricks (capot) 90 more. No trumps points count double. Each team's points are divided by ten and rounded into game points.",
            ["Rules_Inside_Title"] = "Inside and hanging",
            ["Rules_Inside_Body"] = "If the declaring team makes fewer points than the other team, it is \"inside\": everything goes to the other team. With equal points, the other team scores its half and the declarers' half hangs until a team wins a later deal. A doubled deal goes to its winner times two (four when redoubled).",
            ["Rules_Winning_Title"] = "Winning",
            ["Rules_Winning_Body"] = "The first team to 151 or more wins, when it is ahead and scored in the last deal. A capot never ends the game.",
        };

        private static readonly Dictionary<string, string> Bg = new()
        {
            // Start page
            ["Start_Title"] = "БЕЛОТ",
            ["Start_Subtitle"] = "БРИДЖ-БЕЛОТ ЗА ЧЕТИРИМА",
            ["Start_Statistics"] = "Статистика",
            ["Start_HowToPlay"] = "Как се играе",
            ["Start_YourName"] = "ТВОЕТО ИМЕ",
            ["Start_DefaultName"] = "Ти",
            ["Start_Partner"] = "ПАРТНЬОР · СЕВЕР",
            ["Start_West"] = "ЛЯВ ПРОТИВНИК · ЗАПАД",
            ["Start_East"] = "ДЕСЕН ПРОТИВНИК · ИЗТОК",
            ["Start_Play"] = "Играй",
            ["Start_RecentGames"] = "ПОСЛЕДНИ ИГРИ",
            ["Start_SeeAll"] = "Всички",
            ["Start_NoHistory"] = "Още няма игри. Избери партньор и противници и играй!",
            ["Start_YourRating"] = "Твоят рейтинг",
            ["Start_RecordFormat"] = "{0} игри · {1} победи · {2} загуби",
            ["Start_NoGames"] = "Изиграй игра, за да получиш рейтинг",

            // Computer levels
            ["Level_Random_Name"] = "Случаен",
            ["Level_Random_Tag"] = "Играе произволна позволена карта и обявява наслуки.",
            ["Level_Dummy_Name"] = "Начинаещ",
            ["Level_Dummy_Tag"] = "Прости правила: обявява силните си бои, играе ниско.",
            ["Level_Smart_Name"] = "Опитен",
            ["Level_Smart_Tag"] = "Солидна игра, като редовен играч в клуба.",
            ["Level_Claude_Name"] = "Майстор",
            ["Level_Claude_Tag"] = "Разиграва хиляди раздавания преди всяка карта.",
            ["Level_RecordFormat"] = "Твоят резултат срещу това ниво: {0}П – {1}З",
            ["Diff_1"] = "Лесно",
            ["Diff_2"] = "Средно",
            ["Diff_3"] = "Трудно",
            ["Diff_4"] = "Експерт",

            // Seats
            ["Seat_East"] = "Изток · {0}",
            ["Seat_West"] = "Запад · {0}",
            ["Seat_Partner"] = "Партньор · {0}",

            // Cards
            ["Rank_Seven"] = "7",
            ["Rank_Eight"] = "8",
            ["Rank_Nine"] = "9",
            ["Rank_Ten"] = "10",
            ["Rank_Jack"] = "В",
            ["Rank_Queen"] = "Д",
            ["Rank_King"] = "Р",
            ["Rank_Ace"] = "А",
            ["Rank_Seven_Plural"] = "седмици",
            ["Rank_Eight_Plural"] = "осмици",
            ["Rank_Nine_Plural"] = "деветки",
            ["Rank_Ten_Plural"] = "десетки",
            ["Rank_Jack_Plural"] = "валета",
            ["Rank_Queen_Plural"] = "дами",
            ["Rank_King_Plural"] = "попове",
            ["Rank_Ace_Plural"] = "аса",

            // Bids
            ["Bid_Pass"] = "Пас",
            ["Bid_Clubs"] = "Спатия",
            ["Bid_Diamonds"] = "Каро",
            ["Bid_Hearts"] = "Купа",
            ["Bid_Spades"] = "Пика",
            ["Bid_NoTrumps"] = "Без коз",
            ["Bid_AllTrumps"] = "Всичко коз",
            ["Bid_Double"] = "Контра",
            ["Bid_ReDouble"] = "Реконтра",
            ["Bid_NoTrumpsShort"] = "БК",
            ["Bid_AllTrumpsShort"] = "ВК",
            ["Bid_DoubleShort"] = "Контра",
            ["Bid_ReDoubleShort"] = "Реконтра",
            ["Bid_Current"] = "Сега: {0} ({1})",
            ["Bid_NoneYet"] = "Още никой не е обявил",

            // Combinations
            ["Ann_Belote"] = "Белот",
            ["Ann_Tierce"] = "Терца",
            ["Ann_Quarte"] = "Кварта",
            ["Ann_Quint"] = "Квинта",
            ["Ann_Carre"] = "Каре",
            ["Ann_CarreOf"] = "Каре {0}",
            ["Ann_FourJacks"] = "Каре валета",
            ["Ann_FourNines"] = "Каре деветки",
            ["Ann_To"] = "{0} до {1}",

            // Game table
            ["Game_Us"] = "Ние",
            ["Game_Them"] = "Вие",
            ["Game_Hanging"] = "Висящи",
            ["Game_LastTrick"] = "Последна ръка",
            ["Game_Hint"] = "Подсказка",
            ["Game_Menu"] = "Меню",
            ["Game_YourBid"] = "Твоята обява",
            ["Game_YourCombinations"] = "Твоите комбинации",
            ["Game_Declare"] = "Обяви",
            ["Game_DeclareNothing"] = "Без обява",
            ["Game_BeloteBadge"] = "Белот",
            ["Status_Thinking"] = "{0} мисли…",
            ["Status_YourBid"] = "Твоя обява",
            ["Status_YourDeclaration"] = "Обяви комбинациите си",
            ["Status_YourTurn"] = "Твой ред",
            ["Toast_Belote"] = "Белот!",
            ["Toast_SeatBelote"] = "{0}: Белот!",

            // End of a deal
            ["Round_PassedOut"] = "Всички казаха пас",
            ["Round_NoContract"] = "Няма игра: картите се раздават отново",
            ["Round_Hanging"] = "Висяща игра",
            ["Round_WeMade"] = "Изкарахме я!",
            ["Round_TheyMade"] = "Изкараха я",
            ["Round_WeInside"] = "Вътре сме",
            ["Round_TheyInside"] = "Вкарахме ги!",
            ["Round_CapotBadge"] = "Капо!",
            ["Round_Cards"] = "Карти",
            ["Round_Combinations"] = "Комбинации",
            ["Round_Capot"] = "Капо",
            ["Round_InDeal"] = "Общо",
            ["Round_GamePoints"] = "Точки",
            ["Round_Score"] = "РЕЗУЛТАТ",
            ["Round_HangingFormat"] = "{0} точки висят за следващата изиграна игра",

            // End of the game
            ["GameOver_Won"] = "Победа!",
            ["GameOver_Lost"] = "Загуба",
            ["GameOver_Score"] = "{0} – {1}",
            ["GameOver_PlayAgain"] = "Нова игра",
            ["GameOver_BackToMenu"] = "Към менюто",
            ["Rating_Change"] = "Рейтинг {0} → {1} ({2})",
            ["Error_Title"] = "Нещо се обърка",
            ["Common_Continue"] = "Продължи",
            ["Common_Cancel"] = "Отказ",
            ["Common_Series"] = "Серия",

            // Leaving a game
            ["Leave_Title"] = "Да напуснеш ли играта?",
            ["Leave_Message"] = "Текущата игра ще се загуби и няма да се отчете.",
            ["Leave_Confirm"] = "Напусни",
            ["Leave_Cancel"] = "Продължи играта",

            // History
            ["History_Win"] = "П",
            ["History_Loss"] = "З",
            ["History_With"] = "с {0} срещу {1}",
            ["History_Pair"] = "{0} и {1}",

            // Settings
            ["Settings_Title"] = "Настройки",
            ["Settings_Gameplay"] = "ИГРА",
            ["Settings_Language"] = "Език",
            ["Settings_GameSpeed"] = "Скорост на играта",
            ["Settings_GameSpeedHint"] = "Колко мисли компютърът и колко стоят картите на масата.",
            ["Speed_Relaxed"] = "Спокойна",
            ["Speed_Normal"] = "Нормална",
            ["Speed_Fast"] = "Бърза",
            ["Settings_Haptics"] = "Вибрация",
            ["Settings_HapticsHint"] = "Кратка на твой ред, дълга в края на раздаването.",
            ["Settings_Assists"] = "Помощ за начинаещи",
            ["Settings_AssistsHint"] = "Означения за белот върху картите и бутонът Подсказка.",
            ["Settings_Data"] = "ДАННИ",
            ["Settings_ResetStats"] = "Нулирай статистиката",
            ["Reset_Title"] = "Да нулираш ли статистиката?",
            ["Reset_Message"] = "Рейтингът, историята и резултатите ти ще бъдат изтрити.",
            ["Reset_Confirm"] = "Нулирай",
            ["Reset_Done"] = "Статистиката е нулирана.",
            ["Settings_About"] = "ЗА ИГРАТА",
            ["Settings_Version"] = "Белот {0}",
            ["Settings_EngineNote"] = "Играта и компютърните играчи работят на устройството ти, без връзка.",
            ["Settings_OpenSource"] = "Отворен код в GitHub",

            // Statistics
            ["Stats_Title"] = "Статистика",
            ["Stats_Rating"] = "РЕЙТИНГ",
            ["Stats_Current"] = "Текущ",
            ["Stats_Peak"] = "Най-висок",
            ["Stats_Games"] = "Игри",
            ["Stats_WinRate"] = "Победи",
            ["Stats_CurrentStreak"] = "Серия",
            ["Stats_BestStreak"] = "Най-добра серия",
            ["Stats_Empty"] = "Още няма игри.",
            ["Stats_ByLevel"] = "СРЕЩУ ВСЯКО НИВО",
            ["Stats_GamesFormat"] = "{0} игри",
            ["Stats_NotPlayed"] = "Още не е играно",
            ["Stats_HistoryHeader"] = "ИСТОРИЯ",

            // Rules
            ["Rules_Title"] = "Как се играе",
            ["Rules_Intro"] = "Белотът се играе от четирима в два отбора; партньорите седят един срещу друг. Ти си на юг, партньорът ти на север.",
            ["Rules_Cards_Title"] = "Карти и раздаване",
            ["Rules_Cards_Body"] = "32 карти: 7, 8, 9, 10, В, Д, Р, А във всяка от четирите бои. Всеки получава пет карти, обявите решават играта, после всеки получава още три.",
            ["Rules_Bidding_Title"] = "Обявяване",
            ["Rules_Bidding_Body"] = "Поред всеки казва пас или обявява по-висока игра: спатия, каро, купа, пика (козът), без коз, всичко коз. Противник на обявилия може да каже контра, а обявилите след това реконтра. Три паса поред завършват обявяването. Ако и четиримата кажат пас, картите се раздават отново.",
            ["Rules_Play_Title"] = "Разиграване",
            ["Rules_Play_Body"] = "Първият обявяващ играе пръв, а който вземе ръката, играе следващата. Трябва да отговаряш на боята. Козовете са по сила В 9 А 10 Р Д 8 7, другите бои А 10 Р Д В 9 8 7. Когато се играе коз (и при всичко коз), трябва да качиш, ако можеш. Без исканата боя трябва да цакаш, когато ръката е на противник, и да надцакаш коза на противник, ако можеш. При без коз само отговаряш на боята.",
            ["Rules_Combinations_Title"] = "Комбинации",
            ["Rules_Combinations_Body"] = "Обявяват се с първата ти карта (не и при без коз): терца (три поред) 20, кварта 50, квинта (пет или повече) 100, каре десетки, дами, попове или аса 100, каре деветки 150, каре валета 200. Една карта участва само в една комбинация. Отборът с най-силната поредица записва всичките си поредици (никой, ако най-силните са равни); най-силното каре се записва за отбора си.",
            ["Rules_Belote_Title"] = "Белот",
            ["Rules_Belote_Body"] = "Поп и дама коз (от всяка боя при всичко коз) носят 20, обявени при играта на първата от двете карти. Приложението го обявява вместо теб.",
            ["Rules_Scoring_Title"] = "Точки",
            ["Rules_Scoring_Body"] = "Коз: В 20, 9 14, А 11, 10 10, Р 4, Д 3. Други бои: А 11, 10 10, Р 4, Д 3, В 2. Последната ръка носи 10, а взетите всички осем ръце (капо) още 90. При без коз точките се удвояват. Точките на всеки отбор се делят на десет и се закръглят.",
            ["Rules_Inside_Title"] = "Вътре и висящи",
            ["Rules_Inside_Body"] = "Ако обявилият отбор има по-малко точки от другия, той е \"вътре\": всичко отива при другия отбор. При равни точки другият отбор записва своите, а тези на обявилите висят, докато някой отбор спечели следваща игра. При контра всичко отива при спечелилия, удвоено (учетворено при реконтра).",
            ["Rules_Winning_Title"] = "Победа",
            ["Rules_Winning_Body"] = "Печели първият отбор със 151 или повече точки, който води и е записал в последното раздаване. С капо не се излиза.",
        };

        public static string Get(string language, string key)
        {
            var table = language == LocalizationManager.Bulgarian ? Bg : En;
            if (table.TryGetValue(key, out var value))
            {
                return value;
            }

            return En.TryGetValue(key, out var english) ? english : key;
        }

        /// <summary>The whole table of a language (the tests compare the two).</summary>
        internal static IReadOnlyDictionary<string, string> Table(string language) =>
            language == LocalizationManager.Bulgarian ? Bg : En;
    }
}
