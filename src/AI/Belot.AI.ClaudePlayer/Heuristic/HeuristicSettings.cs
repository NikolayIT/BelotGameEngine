namespace Belot.AI.ClaudePlayer.Heuristic
{
    using System;
    using System.Globalization;

    /// <summary>
    /// The heuristic player's tunable rules (see HEURISTIC_PLAYER.md): the hand strength each bid
    /// needs, when to double, and the risks the card play accepts. The defaults are the measured
    /// best; <see cref="Set"/> changes one by name for experiments ("heuristic+name=value").
    /// </summary>
    public sealed class HeuristicSettings
    {
        /// <summary>Gets or sets the hand strength a suit bid needs (see HeuristicBidding.SuitStrength).</summary>
        public double SuitThreshold { get; set; } = 9.5;

        public double AllTrumpsThreshold { get; set; } = 16.5;

        public double NoTrumpsThreshold { get; set; } = 14.5;

        /// <summary>Gets or sets the strength the first bidder gains in no trumps: it leads the first trick.</summary>
        public double FirstNoTrumpsBonus { get; set; } = 3;

        public double FirstAllTrumpsBonus { get; set; } = 2;

        /// <summary>Gets or sets the strength all trumps gains from the partner's suit bid (it shows that suit's jack or nine).</summary>
        public double PartnerSuitAllTrumpsBonus { get; set; } = 6;

        /// <summary>Gets or sets the strength needed above the threshold to take the contract from the partner with another suit.</summary>
        public double OverPartnerPenalty { get; set; } = 3;

        /// <summary>Gets or sets the strength a bid loses against each opponent's bid that it overcalls.</summary>
        public double OpponentBidPenalty { get; set; } = 1;

        /// <summary>Gets or sets a value indicating whether the score changes the strength needed (bolder behind, safer ahead).</summary>
        public bool ScoreAware { get; set; } = true;

        public bool MayDouble { get; set; } = true;

        /// <summary>Gets or sets the defensive strength in the opponents' trump suit (and side aces) that doubles a suit contract.</summary>
        public double DoubleSuitThreshold { get; set; } = 14;

        public double DoubleNoTrumpsThreshold { get; set; } = 17;

        public double DoubleAllTrumpsThreshold { get; set; } = 17;

        /// <summary>Gets or sets the strength above the bid's threshold that redoubles a doubled contract of the team.</summary>
        public double RedoubleMargin { get; set; } = 7;

        /// <summary>Gets or sets how much the value of keeping a card counts against giving it to the partner's trick.</summary>
        public double SmearKeepFactor { get; set; } = 1;

        /// <summary>Gets or sets the chance of the opponent beating the partner's card at which this player covers it.</summary>
        public double CoverChance { get; set; } = 1.1;

        /// <summary>Gets or sets the largest chance of being beaten at which this player takes a trick with a card that is not sure.</summary>
        public double TakeRiskChance { get; set; } = .25;

        /// <summary>Gets or sets the largest chance of the opponents ruffing at which a plain master is led in a suit contract.</summary>
        public double RuffRiskChance { get; set; } = .7;

        /// <summary>Gets or sets the trick before which no trumps keeps its aces and tens off tricks with few points (the academy).</summary>
        public int NoTrumpsSaveTricks { get; set; } = 5;

        /// <summary>Gets or sets the card points a no trumps trick needs before an ace or ten takes it early.</summary>
        public int NoTrumpsSavePoints { get; set; } = 4;

        /// <summary>Gets or sets a value indicating whether the declarer's partner leads a trump at the first chance.</summary>
        public bool PartnerLeadsTrump { get; set; } = true;

        /// <summary>Gets or sets the trumps the declarer needs to draw trumps without holding the highest.</summary>
        public int DrawWithoutMasterLength { get; set; } = 4;

        /// <summary>Gets or sets a value indicating whether a defender leads a singleton to ruff the suit's next round.</summary>
        public bool DefenderSingletonLead { get; set; } = true;

        /// <summary>Gets or sets the trumps a defender needs before leading trumps.</summary>
        public int DefenderTrumpLeadLength { get; set; } = 4;

        /// <summary>
        /// Gets or sets a value indicating whether a leader cashes the cards of a suit nobody else
        /// holds (they cannot lose), the most valuable first, before anything else.
        /// </summary>
        public bool CashExhausted { get; set; } = true;

        /// <summary>Gets or sets a value indicating whether no trumps leads its masters (the aces) instead of keeping them.</summary>
        public bool NoTrumpsLeadMasters { get; set; } = true;

        /// <summary>
        /// Gets or sets the tricks in which all trumps does not cash a lone master (avoid premature
        /// cashing): a jack is led early only with the nine behind it, from length or the partner's suit.
        /// </summary>
        public int AllTrumpsPatienceTricks { get; set; } = 3;

        /// <summary>
        /// Gets or sets a value indicating whether all trumps leads the lowest card of its top block
        /// below the opponents' masters (the king from ace-ten-king, the blog's example): everybody
        /// must beat it if they can, so whoever holds the jack or nine has to give it up.
        /// </summary>
        public bool ForcingLeads { get; set; }

        /// <summary>Gets or sets a value indicating whether the declarers draw trumps by control (more trumps than the opponents can hold) instead of whenever they hold the highest.</summary>
        public bool DrawByControl { get; set; } = true;

        /// <summary>Gets or sets the trumps with which a declarer holding the highest draws whatever the opponents hold.</summary>
        public int DrawMasterLength { get; set; } = 4;

        /// <summary>Gets or sets how many more trumps than its own the opponents may hold for a player with the highest to still draw.</summary>
        public int DrawMasterExcess { get; set; } = 1;

        /// <summary>
        /// Gets or sets the strength a bid gains when the opponents hold the contract: passing lets
        /// them play it, so the sources compete "even with relatively weak cards".
        /// </summary>
        public double CompeteBonus { get; set; } = 4;

        /// <summary>Gets or sets the strength a suit bid loses without the suit's jack (a nine and small cards).</summary>
        public double NoJackPenalty { get; set; }

        /// <summary>Gets or sets an ace's strength in no trumps.</summary>
        public double NoTrumpsAceValue { get; set; } = 5;

        /// <summary>Gets or sets how much more a margin over the threshold counts for all trumps than for a suit (26 game points a deal, not 16).</summary>
        public double AllTrumpsScale { get; set; } = 1;

        public double NoTrumpsScale { get; set; } = 1;

        /// <summary>Gets or sets the trick from which no trumps leads its masters (before it only an ace with the ten behind it).</summary>
        public int NoTrumpsCashTrick { get; set; } = 4;

        /// <summary>Gets or sets a value indicating whether no trumps leads the top of a sequence (the king from king-queen, the queen from queen-jack).</summary>
        public bool NoTrumpsSequenceLead { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether, when every legal card beats the trick so far, the
        /// card is chosen by its expected gain (the chance of keeping the trick against what it is
        /// worth to keep) rather than the cheapest card nobody after can beat.
        /// </summary>
        public bool ForcedByValue { get; set; } = true;

        /// <summary>
        /// Gets or sets a value indicating whether every following card is chosen by its expected
        /// gain: the chance the team takes the trick with it, times the points at stake, minus what
        /// keeping the card is worth (instead of the rules for giving, covering and taking).
        /// </summary>
        public bool FollowByValue { get; set; } = true;

        /// <summary>Gets or sets the card points each card still to come adds to a trick, as the expected-gain choice counts it.</summary>
        public double FollowRestPoints { get; set; } = 4;

        /// <summary>
        /// Gets or sets a value indicating whether a player unblocks the partner's suit: under the
        /// partner's jack (all trumps) or ace (no trumps) it gives the next card, the nine or the
        /// ten, when it holds another card of the suit; and in no trumps it takes the partner's
        /// low lead with its ace (the blog: release the stoppers in the partner's suits at once).
        /// </summary>
        public bool UnblockPartner { get; set; }

        /// <summary>Gets or sets a value indicating whether no trumps takes the partner's low lead with an ace or ten early (unblocking) though the trick has few points.</summary>
        public bool UnblockTakes { get; set; } = true;

        /// <summary>Gets or sets the trick from which a declarer leads its master trump while both opponents may still hold trumps (it pulls two).</summary>
        public int DrawLoneMasterTrick { get; set; } = 9;

        /// <summary>Gets or sets what keeping a card second to a single unseen higher one is worth, as a share of its points and the cards it takes.</summary>
        public double UnguardedSecondKeep { get; set; }

        /// <summary>Gets or sets how much a no trumps lead prefers a longer suit, per card.</summary>
        public double NoTrumpsLengthLead { get; set; } = .7;

        /// <summary>Gets or sets how much an all trumps lead prefers a longer suit, per card (negative: a shorter one).</summary>
        public double AllTrumpsLengthLead { get; set; } = .7;

        /// <summary>Gets or sets the support for the partner's doubled contract that redoubles it.</summary>
        public double RedoublePartnerSupport { get; set; } = 99;

        /// <summary>Gets or sets the strength all trumps loses per opponent's bid it overcalls.</summary>
        public double AllTrumpsOverOpponentPenalty { get; set; } = 1;

        /// <summary>Gets or sets a value indicating whether a player gives its jack (all trumps) or ace to the partner's trick to ask for that suit.</summary>
        public bool CallSignals { get; set; }

        /// <summary>Gets or sets how much more likely a bidder is taken to hold its suit's jack and nine.</summary>
        public double BidHonourWeight { get; set; } = 3;

        /// <summary>Gets or sets how likely a player is taken to hold an honour of a suit it threw away while its opponents held the trick (see CardMemory).</summary>
        public double DiscardHonourWeight { get; set; } = 1;

        /// <summary>
        /// Gets or sets the endgame horizon: the last tricks played out exactly over the deals the
        /// play allows (0: the rules decide every card).
        /// </summary>
        public int EndgameTricks { get; set; }

        /// <summary>Gets or sets the deals sampled for four- and five-trick endgames (three tricks enumerate all).</summary>
        public int EndgameWorlds { get; set; } = 48;

        public int EndgameNodeLimit { get; set; } = 150000;

        public int EndgameMilliseconds { get; set; } = 8;

        /// <summary>Gets or sets the game points within which the endgame lets the rules choose among the best cards.</summary>
        public double EndgameTolerance { get; set; } = .02;

        /// <summary>Gets or sets how much less likely a world is per honour it gives the partner in a suit it threw away (1: no reading).</summary>
        public double EndgameSignalWeight { get; set; } = .5;

        /// <summary>Gets or sets how much less likely an endgame world is per suit bidder holding neither its jack nor its nine (1: no reading).</summary>
        public double EndgameBidWeight { get; set; } = .3;

        /// <summary>
        /// Gets or sets the learned bidding's weights (see <see cref="LearnedBidding"/>), by default
        /// the embedded <see cref="BidModel.Default"/>; null bids by the written point counts of
        /// <see cref="HeuristicBidding"/> (the bidding until October 2026).
        /// </summary>
        public BidModel Bids { get; set; } = BidModel.Default;

        /// <summary>Gets or sets the game points over passing a learned bid must be expected to bring.</summary>
        public double BidMargin { get; set; }

        /// <summary>Gets or sets the game points more a learned double must be expected to bring.</summary>
        public double DoubleBidMargin { get; set; }

        public HeuristicSettings Clone() => (HeuristicSettings)this.MemberwiseClone();

        /// <summary>Changes one setting by its name (case-insensitive).</summary>
        public void Set(string name, string value)
        {
            var number = double.Parse(value, CultureInfo.InvariantCulture);
            switch (name.ToLowerInvariant())
            {
                case "suit":
                    this.SuitThreshold = number;
                    break;
                case "at":
                    this.AllTrumpsThreshold = number;
                    break;
                case "nt":
                    this.NoTrumpsThreshold = number;
                    break;
                case "firstnt":
                    this.FirstNoTrumpsBonus = number;
                    break;
                case "firstat":
                    this.FirstAllTrumpsBonus = number;
                    break;
                case "partnerat":
                    this.PartnerSuitAllTrumpsBonus = number;
                    break;
                case "overpartner":
                    this.OverPartnerPenalty = number;
                    break;
                case "overopp":
                    this.OpponentBidPenalty = number;
                    break;
                case "score":
                    this.ScoreAware = number != 0;
                    break;
                case "double":
                    this.MayDouble = number != 0;
                    break;
                case "dsuit":
                    this.DoubleSuitThreshold = number;
                    break;
                case "dnt":
                    this.DoubleNoTrumpsThreshold = number;
                    break;
                case "dat":
                    this.DoubleAllTrumpsThreshold = number;
                    break;
                case "redouble":
                    this.RedoubleMargin = number;
                    break;
                case "smearkeep":
                    this.SmearKeepFactor = number;
                    break;
                case "cover":
                    this.CoverChance = number;
                    break;
                case "risk":
                    this.TakeRiskChance = number;
                    break;
                case "ruffrisk":
                    this.RuffRiskChance = number;
                    break;
                case "ntsave":
                    this.NoTrumpsSaveTricks = (int)number;
                    break;
                case "ntsavepoints":
                    this.NoTrumpsSavePoints = (int)number;
                    break;
                case "partnertrump":
                    this.PartnerLeadsTrump = number != 0;
                    break;
                case "drawlength":
                    this.DrawWithoutMasterLength = (int)number;
                    break;
                case "singleton":
                    this.DefenderSingletonLead = number != 0;
                    break;
                case "deftrump":
                    this.DefenderTrumpLeadLength = (int)number;
                    break;
                case "calls":
                    this.CallSignals = number != 0;
                    break;
                case "exhausted":
                    this.CashExhausted = number != 0;
                    break;
                case "ntmasters":
                    this.NoTrumpsLeadMasters = number != 0;
                    break;
                case "atpatience":
                    this.AllTrumpsPatienceTricks = (int)number;
                    break;
                case "forcing":
                    this.ForcingLeads = number != 0;
                    break;
                case "control":
                    this.DrawByControl = number != 0;
                    break;
                case "drawmaster":
                    this.DrawMasterLength = (int)number;
                    break;
                case "drawexcess":
                    this.DrawMasterExcess = (int)number;
                    break;
                case "compete":
                    this.CompeteBonus = number;
                    break;
                case "nojack":
                    this.NoJackPenalty = number;
                    break;
                case "ntace":
                    this.NoTrumpsAceValue = number;
                    break;
                case "atscale":
                    this.AllTrumpsScale = number;
                    break;
                case "ntscale":
                    this.NoTrumpsScale = number;
                    break;
                case "ntcash":
                    this.NoTrumpsCashTrick = (int)number;
                    break;
                case "ntseq":
                    this.NoTrumpsSequenceLead = number != 0;
                    break;
                case "forcedvalue":
                    this.ForcedByValue = number != 0;
                    break;
                case "lonemaster":
                    this.DrawLoneMasterTrick = (int)number;
                    break;
                case "secondkeep":
                    this.UnguardedSecondKeep = number;
                    break;
                case "ntlength":
                    this.NoTrumpsLengthLead = number;
                    break;
                case "atlength":
                    this.AllTrumpsLengthLead = number;
                    break;
                case "followvalue":
                    this.FollowByValue = number != 0;
                    break;
                case "rest":
                    this.FollowRestPoints = number;
                    break;
                case "unblock":
                    this.UnblockPartner = number != 0;
                    break;
                case "unblocktake":
                    this.UnblockTakes = number != 0;
                    break;
                case "redoublepartner":
                    this.RedoublePartnerSupport = number;
                    break;
                case "atoveropp":
                    this.AllTrumpsOverOpponentPenalty = number;
                    break;
                case "bidweight":
                    this.BidHonourWeight = number;
                    break;
                case "discardweight":
                    this.DiscardHonourWeight = number;
                    break;
                case "tricks":
                    this.EndgameTricks = (int)number;
                    break;
                case "worlds":
                    this.EndgameWorlds = (int)number;
                    break;
                case "nodes":
                    this.EndgameNodeLimit = (int)number;
                    break;
                case "ms":
                    this.EndgameMilliseconds = (int)number;
                    break;
                case "etol":
                    this.EndgameTolerance = number;
                    break;
                case "sig":
                    this.EndgameSignalWeight = number;
                    break;
                case "bidsig":
                    this.EndgameBidWeight = number;
                    break;
                case "learned":
                    this.Bids = number != 0 ? BidModel.Default : null;
                    break;
                case "bidmargin":
                    this.BidMargin = number;
                    break;
                case "doublemargin":
                    this.DoubleBidMargin = number;
                    break;
                default:
                    throw new ArgumentException($"Unknown heuristic setting '{name}'.", nameof(name));
            }
        }
    }
}
