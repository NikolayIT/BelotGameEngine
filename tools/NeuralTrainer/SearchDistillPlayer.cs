namespace Belot.NeuralTrainer
{
    using System;
    using System.Collections.Generic;
    using System.Numerics;

    using Belot.AI.ClaudePlayer;
    using Belot.AI.ClaudePlayer.Neural;
    using Belot.AI.ClaudePlayer.Search;
    using Belot.Engine;
    using Belot.Engine.Cards;
    using Belot.Engine.Game;
    using Belot.Engine.GameMechanics;
    using Belot.Engine.Players;

    /// <summary>
    /// Records the neural search's values for every legal card. The teacher samples unseen
    /// hands from the player's context, exactly as it does in the app; engine truth is never
    /// passed to the teacher or encoder. Unlabelled decisions use the fast student; labelled
    /// decisions can also follow the student, to collect labels on the student's trajectories.
    /// </summary>
    internal sealed class SearchDistillPlayer : IPlayer
    {
        private readonly ClaudePlayerNeural teacher;
        private readonly ClaudePlayerNeural student;
        private readonly SampleBuffer[] buffers;
        private readonly Random random;
        private readonly double labelChance;
        private readonly double teacherPlayChance;
        private readonly BatchedNeuralSearch batched;
        private readonly BelotSimulator simulator = new BelotSimulator();
        private readonly int[] indices = new int[FeatureEncoder.MaxActive];
        private readonly float[] values = new float[FeatureEncoder.MaxActive];
        private readonly float[] labels = new float[FeatureEncoder.CardOutputs];
        private readonly float[] cardValues = new float[FeatureEncoder.CardOutputs];

        public SearchDistillPlayer(NeuralModels models, SampleBuffer[] buffers, TrainingSettings settings, int seed, IBatchedCardPolicy policy = null)
        {
            var endgameTeacher = settings.Teacher == "endgame";
            if (settings.SearchDeals <= 0 && !endgameTeacher)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "A neural search teacher needs --search-deals above zero.");
            }

            if (!double.IsFinite(settings.TeacherPlayChance) || settings.TeacherPlayChance < 0 || settings.TeacherPlayChance > 1)
            {
                throw new ArgumentOutOfRangeException(nameof(settings), "Teacher play chance must be between zero and one.");
            }

            this.teacher = new ClaudePlayerNeural(models)
            {
                SearchDeals = endgameTeacher ? 0 : settings.SearchDeals,
                UseEndgameSearch = endgameTeacher,
                EndgameUseDeclarations = settings.EndgameDeclarations,
                EndgameTricks = settings.EndgameTricks,
                Rng = new Random(seed),
            };
            this.student = new ClaudePlayerNeural(models);
            this.buffers = buffers;
            this.random = new Random(seed ^ 0x5EED);
            this.labelChance = settings.CardLabelChance;
            this.teacherPlayChance = settings.TeacherPlayChance;
            this.batched = policy == null || endgameTeacher ? null : new BatchedNeuralSearch(policy);
        }

        public BidType GetBid(PlayerGetBidContext context) => this.student.GetBid(context);

        public IList<Announce> GetAnnounces(PlayerGetAnnouncesContext context) => context.AvailableAnnounces;

        public PlayCardAction PlayCard(PlayerPlayCardContext context)
        {
            if (context.AvailableCardsToPlay.Count <= 1 || this.random.NextDouble() >= this.labelChance)
            {
                return this.student.PlayCard(context);
            }

            if (!NeuralDeal.FromPlayContext(context, this.simulator, out var deal))
            {
                throw new InvalidOperationException("Cannot encode a search-teacher decision.");
            }

            var legal = NeuralDeal.ToMask(context.AvailableCardsToPlay);
            if (this.batched == null || !this.batched.Evaluate(
                    context, in deal, legal, this.teacher.SearchDeals, this.simulator, this.teacher.Rng, this.cardValues))
            {
                foreach (var score in this.teacher.EvaluateCards(context))
                {
                    this.cardValues[score.Card.GetHashCode()] = (float)score.Value;
                }
            }

            var rotation = FeatureEncoder.Rotation(deal.Kind);
            var mask = 0u;
            var chosen = NeuralEvaluator.Best(this.cardValues, legal);
            Array.Clear(this.labels);
            for (var rest = legal; rest != 0; rest &= rest - 1)
            {
                var card = BitOperations.TrailingZeroCount(rest);
                var output = FeatureEncoder.ToNetwork(card, rotation);
                this.labels[output] = this.cardValues[card] / NeuralEvaluator.ValueScale;
                mask |= 1u << output;
            }

            var features = FeatureEncoder.EncodeCard(in deal, legal, this.indices, this.values);
            this.buffers[1 + FeatureEncoder.CardNetwork(deal.Kind)].Add(
                this.indices.AsSpan(0, features), this.values.AsSpan(0, features), this.labels, mask);
            return this.teacherPlayChance >= 1 || (this.teacherPlayChance > 0 && this.random.NextDouble() < this.teacherPlayChance)
                ? new PlayCardAction(Card.AllCards[chosen])
                : this.student.PlayCard(context);
        }

        public void EndOfTrick(IEnumerable<PlayCardAction> trickActions)
        {
        }

        public void EndOfRound(RoundResult roundResult)
        {
        }

        public void EndOfGame(GameResult gameResult)
        {
        }
    }
}
